using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Data.Sqlite;

namespace SwewolfMediaPlayer;

/// <summary>Where the data of a path is kept: which file, and how the path is written there (see <see cref="LibraryStore"/>).</summary>
/// <param name="Relative">The keys are paths from the root of the drive (USB file, or a volume key in the main file).</param>
public sealed record LibraryRoute(string DbFile, string Prefix, bool Relative, string Root);

/// <summary>
/// The core of the library that SweWolf File Explorer and SweWolf Media Player share: which database file holds the data of
/// a path, the keys, the schema, "read" marks and resume positions. Schema and rules: docs/library-db.md (the explorer's copy
/// is the master). Nothing here knows about folders as a whole (the explorer's LibraryService does).
///
/// Copy this file between the apps as it is, so both agree on the files. When copying: change the namespace, and nothing else.
/// Change the schema only by adding (new columns, with a higher SchemaVersion): an older app then still reads and writes
/// what it knows.
///
/// History:
///   2026-10-16  Split out of the explorer's LibraryService. Schema 2: File.Position and File.Length (resume position, ms).
///               The main file moved to %AppData%\SweWolfSoftware\Library\library.db (shared by the apps; an old file in
///               the explorer's folder is moved there once).
///   2026-10-15  Routing (main file, USB file by drive type or bus type, volume key for CD/read-only), schema 1, read marks.
/// </summary>
public static class LibraryStore
{
    public const string UsbFolderName = ".SweWolfFileExplorerData";
    public const string DbFileName = "library.db";
    private const int SchemaVersion = 2;

    /// <summary>One thing at a time on the database files (inside this app).</summary>
    public static readonly object Gate = new();

    /// <summary>Only for tests: use this file instead of the user's main database.</summary>
    public static string? MainDbFileOverride { get; set; }

    private static string MainFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SweWolfSoftware", "Library");

    private static string MainDbFile
    {
        get
        {
            if (MainDbFileOverride != null) return MainDbFileOverride;

            var file = Path.Combine(MainFolder, DbFileName);
            MoveOldMainFile(file);
            return file;
        }
    }

    private static bool _oldFileChecked;

    /// <summary>The first version of the explorer kept the file in its own folder: move it to the shared one, once.</summary>
    private static void MoveOldMainFile(string newFile)
    {
        if (_oldFileChecked) return;
        _oldFileChecked = true;

        try
        {
            var old = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SweWolfSoftware", "SweWolf File Explorer", DbFileName);
            if (!File.Exists(old) || File.Exists(newFile)) return;

            Directory.CreateDirectory(Path.GetDirectoryName(newFile)!);
            File.Move(old, newFile);
        }
        catch { /* the new file starts empty */ }
    }

    /// <summary>
    /// Some files of a folder were written (read marks), raised on a background thread after each folder:
    /// the explorer works out the state of the folder from it.
    /// </summary>
    public static event Action<string>? FolderWritten;

    // ── Which file holds the data of a path ──────────────────────────────────

    private static readonly Dictionary<string, uint> DriveTypes = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> UsbRoots = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> SchemaDone = new(StringComparer.OrdinalIgnoreCase);

    private const uint DriveRemovable = 2;
    private const uint DriveFixed = 3;
    private const uint DriveCdRom = 5;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern uint GetDriveTypeW(string rootPathName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetVolumeInformationW(string root, IntPtr volumeName, int volumeNameSize,
        out uint serialNumber, out uint maxComponentLength, out uint flags, IntPtr fileSystemName, int fileSystemNameSize);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern Microsoft.Win32.SafeHandles.SafeFileHandle CreateFileW(string fileName, uint access, uint share,
        IntPtr security, uint creation, uint flags, IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(Microsoft.Win32.SafeHandles.SafeFileHandle device, uint code,
        byte[] inBuffer, int inSize, byte[] outBuffer, int outSize, out int returned, IntPtr overlapped);

    /// <summary>Only for tests: treat a drive as another kind (2 removable, 3 fixed, 5 CD).</summary>
    public static void SetDriveTypeForTest(string root, uint type) => DriveTypes[root] = type;

    /// <summary>Only for tests: is this drive treated as removable (USB stick, USB hard disk, card)?</summary>
    public static bool IsPortableDrive(string root) => DriveTypeOf(root) == DriveRemovable;

    private static uint DriveTypeOf(string root)
    {
        if (!DriveTypes.TryGetValue(root, out var type))
        {
            type = GetDriveTypeW(root);
            // An external USB hard disk reports itself as a fixed disk: only the bus it hangs on tells the truth.
            if (type == DriveFixed && IsOnPortableBus(root)) type = DriveRemovable;
            DriveTypes[root] = type;
        }
        return type;
    }

    // STORAGE_BUS_TYPE: 4 = IEEE 1394, 7 = USB, 0xC = SD card, 0xD = MMC card.
    private static bool IsOnPortableBus(string root)
    {
        if (root.Length < 2 || root[1] != ':') return false;   // a network share has no bus

        const uint IoctlStorageQueryProperty = 0x002D1400;
        const uint FileShareReadWrite = 3, OpenExisting = 3;

        using var handle = CreateFileW(@"\\.\" + root.Substring(0, 2), 0, FileShareReadWrite, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
        if (handle.IsInvalid) return false;

        // STORAGE_PROPERTY_QUERY { PropertyId = StorageDeviceProperty (0), QueryType = PropertyStandardQuery (0), AdditionalParameters[1] }
        // (the adapter property says "unknown" for a USB disk, the device property says USB)
        var query = new byte[12];
        var result = new byte[1024];
        if (!DeviceIoControl(handle, IoctlStorageQueryProperty, query, query.Length, result, result.Length, out var returned, IntPtr.Zero)
            || returned < 32)
            return false;

        int busType = BitConverter.ToInt32(result, 28);   // STORAGE_DEVICE_DESCRIPTOR.BusType
        return busType is 4 or 7 or 0xC or 0xD;
    }

    /// <summary>
    /// The file that holds the data of <paramref name="path"/>. <paramref name="create"/> = false never creates anything
    /// on a removable drive (browsing alone leaves no trace there).
    /// </summary>
    public static LibraryRoute? Resolve(string path, bool create)
    {
        var root = Path.GetPathRoot(path);
        if (string.IsNullOrEmpty(root)) return null;

        var type = DriveTypeOf(root);
        if (type == DriveCdRom) return VolumeRoute(root);

        if (type != DriveRemovable)
            return new LibraryRoute(MainDbFile, "", false, root);

        var usbDir = Path.Combine(root, UsbFolderName);
        var usbDb = Path.Combine(usbDir, DbFileName);

        if (UsbRoots.Contains(root) || File.Exists(usbDb))
        {
            UsbRoots.Add(root);
            return new LibraryRoute(usbDb, "", true, root);
        }

        if (create && TryCreateUsbFolder(usbDir))
        {
            UsbRoots.Add(root);
            return new LibraryRoute(usbDb, "", true, root);
        }

        return VolumeRoute(root);   // nothing on the drive yet, or it cannot be written
    }

    private static LibraryRoute VolumeRoute(string root)
    {
        if (!GetVolumeInformationW(root, IntPtr.Zero, 0, out var serial, out _, out _, IntPtr.Zero, 0))
            return new LibraryRoute(MainDbFile, "", false, root);
        return new LibraryRoute(MainDbFile, "vol:" + serial.ToString("X8"), true, root);
    }

    private static bool TryCreateUsbFolder(string usbDir)
    {
        try
        {
            var dir = Directory.CreateDirectory(usbDir);
            dir.Attributes |= FileAttributes.Hidden;   // Windows ignores the leading dot: the attribute does the hiding
            return true;
        }
        catch
        {
            return false;   // write-protected
        }
    }

    /// <summary>The text a path is stored under (see <see cref="LibraryRoute"/>). The root of a drive is "" (relative) or "D:".</summary>
    public static string KeyOf(LibraryRoute route, string path)
    {
        var p = path.TrimEnd('\\', '/');
        if (route.Relative)
        {
            var rootLength = route.Root.TrimEnd('\\', '/').Length;
            p = p.Length >= rootLength ? p.Substring(rootLength) : "";
        }
        return route.Prefix + p;
    }

    // ── The database files ───────────────────────────────────────────────────

    /// <summary>Opens the file of the route (null if it is not there and <paramref name="create"/> is false, or cannot be opened).</summary>
    public static SqliteConnection? Open(LibraryRoute route, bool create)
    {
        if (!File.Exists(route.DbFile))
        {
            if (!create) return null;
            try { Directory.CreateDirectory(Path.GetDirectoryName(route.DbFile)!); }
            catch { return null; }
        }

        try
        {
            var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = route.DbFile,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false,           // the file is released as soon as the work is done (a stick must be removable)
                DefaultTimeout = 5,        // the other app may be writing
            }.ToString());
            connection.Open();
            EnsureSchema(connection, route.DbFile);
            return connection;
        }
        catch
        {
            return null;
        }
    }

    private static void EnsureSchema(SqliteConnection connection, string file)
    {
        if (!SchemaDone.Add(file)) return;

        try
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA user_version";
                if (Convert.ToInt32(command.ExecuteScalar()) >= SchemaVersion) return;
            }

            using var create = connection.CreateCommand();
            create.CommandText = @"
                CREATE TABLE IF NOT EXISTS Folder (
                    Path      TEXT NOT NULL COLLATE NOCASE PRIMARY KEY,
                    OwnUnread INTEGER,
                    Complete  INTEGER NOT NULL DEFAULT 0
                ) WITHOUT ROWID;
                CREATE TABLE IF NOT EXISTS File (
                    Folder   TEXT NOT NULL COLLATE NOCASE,
                    Name     TEXT NOT NULL COLLATE NOCASE,
                    ReadAt   TEXT,
                    Position INTEGER,
                    Length   INTEGER,
                    PRIMARY KEY (Folder, Name)
                ) WITHOUT ROWID;";
            create.ExecuteNonQuery();

            // Schema 1 had no Position and Length.
            foreach (var column in new[] { "Position", "Length" })
            {
                using var has = Command(connection, "SELECT COUNT(*) FROM pragma_table_info('File') WHERE name = $n", ("$n", column));
                if (Convert.ToInt32(has.ExecuteScalar()) == 0)
                {
                    using var add = connection.CreateCommand();
                    add.CommandText = $"ALTER TABLE File ADD COLUMN {column} INTEGER";
                    add.ExecuteNonQuery();
                }
            }

            using var version = connection.CreateCommand();
            version.CommandText = $"PRAGMA user_version = {SchemaVersion}";
            version.ExecuteNonQuery();
        }
        catch
        {
            SchemaDone.Remove(file);   // read-only: tried again next time
            throw;
        }
    }

    public static SqliteCommand Command(SqliteConnection connection, string sql, params (string Name, object? Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return command;
    }

    // ── Marking files read or unread (in the background) ─────────────────────

    private static readonly object QueueLock = new();
    private static List<(string Path, bool Read)> _pending = new();
    private static bool _workerRunning;

    /// <summary>
    /// Remembers that these files were opened (or not). Returns at once: the work is done in the background in bursts,
    /// so paging through 500 pictures costs a few writes, not 500.
    /// </summary>
    public static void SetRead(IEnumerable<string> paths, bool read)
    {
        lock (QueueLock)
        {
            foreach (var path in paths) _pending.Add((path, read));
            if (_pending.Count == 0 || _workerRunning) return;
            _workerRunning = true;
            Task.Run(Worker);
        }
    }

    private static void Worker()
    {
        while (true)
        {
            Thread.Sleep(100);   // gather a burst

            List<(string Path, bool Read)> batch;
            lock (QueueLock)
            {
                if (_pending.Count == 0)
                {
                    _workerRunning = false;
                    return;
                }
                batch = _pending;
                _pending = new();
            }

            try { Apply(batch); }
            catch { /* not remembering one file is not worth an error box */ }
        }
    }

    /// <summary>Waits (at most a few seconds) until everything queued is written: call when the app closes.</summary>
    public static void Flush(int timeoutMilliseconds = 3000)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < timeoutMilliseconds)
        {
            lock (QueueLock) if (!_workerRunning) return;
            Thread.Sleep(20);
        }
    }

    private static void Apply(List<(string Path, bool Read)> batch)
    {
        // The last word wins for a file named twice; one transaction per folder.
        var byFolder = batch
            .Select(item => (Folder: Path.GetDirectoryName(item.Path), Name: Path.GetFileName(item.Path), item.Read))
            .Where(item => !string.IsNullOrEmpty(item.Folder) && !string.IsNullOrEmpty(item.Name))
            .GroupBy(item => item.Folder!, StringComparer.OrdinalIgnoreCase);

        foreach (var group in byFolder)
        {
            lock (Gate)
            {
                var route = Resolve(group.Key, create: true);
                if (route == null) continue;

                using var connection = Open(route, create: true);
                if (connection == null) continue;
                var key = KeyOf(route, group.Key);

                try
                {
                    using var transaction = connection.BeginTransaction();
                    foreach (var file in group)
                    {
                        using var command = file.Read
                            ? Command(connection, "INSERT INTO File (Folder, Name, ReadAt) VALUES ($f, $n, $t) ON CONFLICT DO NOTHING",
                                ("$f", key), ("$n", file.Name), ("$t", DateTime.UtcNow.ToString("o")))
                            : Command(connection, "DELETE FROM File WHERE Folder = $f AND Name = $n",
                                ("$f", key), ("$n", file.Name));
                        command.Transaction = transaction;
                        command.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
                catch { continue; }   // write-protected
            }

            FolderWritten?.Invoke(group.Key);
        }
    }

    // ── Resume positions ─────────────────────────────────────────────────────

    /// <summary>The saved position and length (both in milliseconds) of a file, or null if there is none.</summary>
    public static (long Position, long Length)? GetPosition(string filePath)
    {
        var folder = Path.GetDirectoryName(filePath);
        var name = Path.GetFileName(filePath);
        if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(name)) return null;

        lock (Gate)
        {
            try
            {
                var route = Resolve(folder, create: false);
                if (route == null) return null;

                using var connection = Open(route, create: false);
                if (connection == null) return null;

                using var command = Command(connection, "SELECT Position, Length FROM File WHERE Folder = $f AND Name = $n",
                    ("$f", KeyOf(route, folder)), ("$n", name));
                using var reader = command.ExecuteReader();
                if (!reader.Read() || reader.IsDBNull(0)) return null;
                return (reader.GetInt64(0), reader.IsDBNull(1) ? 0 : reader.GetInt64(1));
            }
            catch { return null; }
        }
    }

    /// <summary>
    /// Saves where the user stopped (the file counts as read from then on). Done at once, not queued: the player calls it
    /// every few seconds and when it closes. A write-protected drive remembers nothing.
    /// </summary>
    public static void SetPosition(string filePath, long positionMilliseconds, long lengthMilliseconds)
    {
        var folder = Path.GetDirectoryName(filePath);
        var name = Path.GetFileName(filePath);
        if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(name)) return;

        lock (Gate)
        {
            try
            {
                var route = Resolve(folder, create: true);
                if (route == null) return;

                using var connection = Open(route, create: true);
                if (connection == null) return;

                using var command = Command(connection,
                    "INSERT INTO File (Folder, Name, ReadAt, Position, Length) VALUES ($f, $n, $t, $p, $l) " +
                    "ON CONFLICT(Folder, Name) DO UPDATE SET Position = $p, Length = $l",
                    ("$f", KeyOf(route, folder)), ("$n", name), ("$t", DateTime.UtcNow.ToString("o")),
                    ("$p", positionMilliseconds), ("$l", lengthMilliseconds));
                command.ExecuteNonQuery();
            }
            catch { /* write-protected, or the file is busy: the next save tries again */ }
        }
    }

    /// <summary>Forgets the position (the video was played to its end): the file stays read.</summary>
    public static void ClearPosition(string filePath)
    {
        var folder = Path.GetDirectoryName(filePath);
        var name = Path.GetFileName(filePath);
        if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(name)) return;

        lock (Gate)
        {
            try
            {
                var route = Resolve(folder, create: false);
                if (route == null) return;

                using var connection = Open(route, create: false);
                if (connection == null) return;

                using var command = Command(connection, "UPDATE File SET Position = NULL, Length = NULL WHERE Folder = $f AND Name = $n",
                    ("$f", KeyOf(route, folder)), ("$n", name));
                command.ExecuteNonQuery();
            }
            catch { }
        }
    }
}
