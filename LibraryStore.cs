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
///   2026-10-24  SetItemInfo keeps only http / https addresses of other computers ("about:internet" and localhost are not stored).
///   2026-10-24  Schema 8: table ItemInfo (per file: where it came from, HostUrl and ReferrerUrl, copied from the Mark of the Web when the
///               explorer shows the file, or written by an importer). SetItemInfo / GetItemInfo; Forget and Move carry the rows. An older
///               copy ignores the table.
///   2026-10-19  Schema 7: table FolderPicture (per folder: the name of the picture file in it that the user chose as the picture of
///               the folder). SetFolderPicture / GetFolderPicture; Forget and Move carry the rows. An older copy ignores the table.
///   2026-10-18A fixed drive with a folder .SweWolfFileExplorerData in its root counts as portable (VeraCrypt volume, any drive the bus
///               type cannot identify): its data goes to the USB file there. MarkDrivePortable creates the folder (Hidden attribute).
///   2026-10-18  Schema 6: table LastOpened (per folder: the name of the file opened last, for "Jump to Last Opened File").
///               SetLastOpened / GetLastOpened; Forget and Move carry the rows. An older copy ignores the table.
///   2026-10-18  Schema 5: table PreviewPick (per file: the picture the user chose as its preview picture, or "none").
///               GetPreviewChoice / SetPreviewChoice; Forget and Move carry the rows. An older copy ignores the table.
///   2026-10-17  Schema 4: table FolderPrefs (per folder: the sort order of its list and of its subfolders in the tree).
///               GetFolderPrefs / SetFolderSort / SetFolderTreeSort; Forget and Move carry the rows. An older copy ignores the table.
///   2026-10-17  Forget (what the app deleted) and Move (what the app renamed or moved): the rows of a file, or of a folder and
///               everything below it, are removed or follow it to the new path (also to another database file).
///   2026-10-17  Schema 3: table Rated (rating 1-10 and expectation 1-10, for files and folders, in a table of its own so
///               that the File rows keep meaning "opened"). GetRatings / GetMark / SetRating / SetExpectation. An older copy
///               of this file ignores the table and is not harmed by it.
///   2026-10-16  A damaged file is also found when a call fails (RepairAfter), not only by the check when it is opened.
///   2026-10-16  A damaged file is found (quick_check, once per file and run), moved aside as library.corrupt-<date>.db and
///               replaced (the main file by the newest good backup); event Repaired. BackUpMainFile: one backup a day,
///               the last 7 kept, in a Backups folder next to the main file.
///   2026-10-16  Split out of the explorer's LibraryService. Schema 2: File.Position and File.Length (resume position, ms).
///               The main file moved to %AppData%\SweWolfSoftware\Library\library.db (shared by the apps; an old file in
///               the explorer's folder is moved there once).
///   2026-10-15  Routing (main file, USB file by drive type or bus type, volume key for CD/read-only), schema 1, read marks.
/// </summary>
public static class LibraryStore
{
    public const string UsbFolderName = ".SweWolfFileExplorerData";
    public const string DbFileName = "library.db";
    private const int SchemaVersion = 8;

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

    /// <summary>
    /// Marks a drive as portable by creating the hidden data folder in its root (see DriveTypeOf); false if that failed
    /// (write-protected). From now on this run, and every later run of an app with this file, sends its data to that drive.
    /// </summary>
    public static bool MarkDrivePortable(string root)
    {
        if (!TryCreateUsbFolder(Path.Combine(root, UsbFolderName))) return false;
        DriveTypes[root] = DriveRemovable;
        return true;
    }

    /// <summary>Only for tests: is this drive treated as removable (USB stick, USB hard disk, card)?</summary>
    public static bool IsPortableDrive(string root) => DriveTypeOf(root) == DriveRemovable;

    private static uint DriveTypeOf(string root)
    {
        if (!DriveTypes.TryGetValue(root, out var type))
        {
            type = GetDriveTypeW(root);
            // An external USB hard disk reports itself as a fixed disk: only the bus it hangs on tells the truth.
            if (type == DriveFixed && IsOnPortableBus(root)) type = DriveRemovable;
            // A drive the bus cannot identify (a VeraCrypt volume mounted from a file on a stick) is marked by the user (or by the
            // TeraTool converter): the hidden data folder in its root says "this drive is portable".
            if (type == DriveFixed && Directory.Exists(Path.Combine(root, UsbFolderName))) type = DriveRemovable;
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

    /// <summary>
    /// Opens the file of the route (null if it is not there and <paramref name="create"/> is false, or cannot be opened).
    /// A file that turns out to be damaged is moved aside and replaced first (see <see cref="Repair"/>).
    /// </summary>
    public static SqliteConnection? Open(LibraryRoute route, bool create) => Open(route, create, allowRepair: true);

    private static SqliteConnection? Open(LibraryRoute route, bool create, bool allowRepair)
    {
        if (!File.Exists(route.DbFile))
        {
            if (!create) return null;
            try { Directory.CreateDirectory(Path.GetDirectoryName(route.DbFile)!); }
            catch { return null; }
        }

        SqliteConnection? connection = null;
        try
        {
            connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = route.DbFile,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false,           // the file is released as soon as the work is done (a stick must be removable)
                DefaultTimeout = 5,        // the other app may be writing
            }.ToString());
            connection.Open();
            CheckOnce(connection, route.DbFile);
            EnsureSchema(connection, route.DbFile);
            return connection;
        }
        catch (Exception ex) when (allowRepair && IsDamaged(ex))
        {
            connection?.Dispose();
            return Repair(route.DbFile) ? Open(route, create: true, allowRepair: false) : null;
        }
        catch
        {
            connection?.Dispose();
            return null;
        }
    }

    // ── A damaged file ───────────────────────────────────────────────────────

    private sealed class DamagedFileException : Exception { }

    private static readonly HashSet<string> CheckedFiles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// A file was found damaged, moved aside and replaced (raised on whatever thread found it): the old file's new name,
    /// and what was done instead ("restored from the backup of ..." or "a new, empty file was started"). The app tells the user.
    /// </summary>
    public static event Action<string, string>? Repaired;

    private static bool IsDamaged(Exception ex) =>
        ex is DamagedFileException || ex is SqliteException { SqliteErrorCode: 11 or 26 };   // SQLITE_CORRUPT, SQLITE_NOTADB

    /// <summary>
    /// A database call failed with <paramref name="ex"/>. If the file turns out to be damaged it is replaced (see
    /// <see cref="Repair"/>) and true is returned; the call itself is lost. Call it from a catch block, after the connection of
    /// the failed call is closed (an open file cannot be moved).
    /// </summary>
    public static bool RepairAfter(Exception ex, string path)
    {
        if (!IsDamaged(ex)) return false;

        lock (Gate)
        {
            var route = Resolve(path, create: false);
            return route != null && Repair(route.DbFile);
        }
    }

    // SQLite's own quick check, once per file and run: a small file takes a few milliseconds.
    private static void CheckOnce(SqliteConnection connection, string file)
    {
        if (CheckedFiles.Contains(file)) return;

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA quick_check(1)";
            if (!"ok".Equals(command.ExecuteScalar() as string, StringComparison.OrdinalIgnoreCase))
                throw new DamagedFileException();
        }
        CheckedFiles.Add(file);
    }

    /// <summary>
    /// Moves the damaged file aside (library.corrupt-yyyyMMdd-HHmmss.db, in the same folder: nothing is deleted) and puts a
    /// working one in its place: for the main file the newest backup that is good, otherwise nothing (a new file is made).
    /// </summary>
    private static bool Repair(string file)
    {
        try
        {
            var asideBase = Path.Combine(Path.GetDirectoryName(file)!, $"library.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}");
            var aside = asideBase + ".db";
            for (int n = 2; File.Exists(aside); n++) aside = $"{asideBase}-{n}.db";   // two repairs in the same second
            File.Move(file, aside);
            foreach (var extra in new[] { "-journal", "-wal", "-shm" })
                try { File.Delete(file + extra); } catch { }
            SchemaDone.Remove(file);
            CheckedFiles.Remove(file);

            string how = "A new, empty file was started.";
            if (string.Equals(file, MainDbFile, StringComparison.OrdinalIgnoreCase) && RestoreNewestBackup(file) is { } day)
                how = $"It was restored from the backup of {day}.";

            Repaired?.Invoke(aside, how);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ── Backup of the main file ──────────────────────────────────────────────

    private static string BackupFolder(string file) => Path.Combine(Path.GetDirectoryName(file)!, "Backups");

    private static bool IsGood(string file)
    {
        try
        {
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = file, Mode = SqliteOpenMode.ReadOnly, Pooling = false, DefaultTimeout = 5,
            }.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA quick_check(1)";
            return "ok".Equals(command.ExecuteScalar() as string, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>The newest good backup is copied to <paramref name="file"/>. Returns its date as text (yyyy-MM-dd), or null.</summary>
    private static string? RestoreNewestBackup(string file)
    {
        try
        {
            var folder = BackupFolder(file);
            if (!Directory.Exists(folder)) return null;

            foreach (var backup in Directory.GetFiles(folder, "library-*.db").OrderByDescending(f => f, StringComparer.OrdinalIgnoreCase))
            {
                if (!IsGood(backup)) continue;
                File.Copy(backup, file, overwrite: false);

                var name = Path.GetFileNameWithoutExtension(backup);   // library-20261016
                var day = name.Substring(name.IndexOf('-') + 1);
                return day.Length == 8 ? $"{day[..4]}-{day[4..6]}-{day[6..]}" : day;
            }
        }
        catch { }
        return null;
    }

    /// <summary>
    /// Once a day (the first time an app starts that day): a copy of the main file in the Backups folder next to it
    /// (library-yyyyMMdd.db); the last <paramref name="keep"/> are kept. Done with SQLite's backup, so it is a consistent copy
    /// even while the other app is writing. Call it in the background at startup. The USB files are not backed up: they
    /// travel with the drive.
    /// </summary>
    public static void BackUpMainFile(int keep = 7)
    {
        try
        {
            lock (Gate)
            {
                var file = MainDbFile;
                if (!File.Exists(file)) return;

                var folder = BackupFolder(file);
                var today = Path.Combine(folder, $"library-{DateTime.Now:yyyyMMdd}.db");
                if (File.Exists(today)) return;

                // Opening checks the file (and repairs it): a damaged file must not become today's backup.
                using var source = Open(new LibraryRoute(file, "", false, ""), create: false);
                if (source == null) return;

                Directory.CreateDirectory(folder);
                var temp = today + ".tmp";
                File.Delete(temp);
                using (var target = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = temp, Pooling = false }.ToString()))
                {
                    target.Open();
                    source.BackupDatabase(target);
                }
                File.Move(temp, today);

                foreach (var old in Directory.GetFiles(folder, "library-*.db").OrderByDescending(f => f, StringComparer.OrdinalIgnoreCase).Skip(keep))
                    try { File.Delete(old); } catch { }
            }
        }
        catch { /* no backup today: tried again at the next start */ }
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
                ) WITHOUT ROWID;
                CREATE TABLE IF NOT EXISTS Rated (
                    Folder      TEXT NOT NULL COLLATE NOCASE,
                    Name        TEXT NOT NULL COLLATE NOCASE,
                    Rating      INTEGER,
                    Expectation REAL,
                    PRIMARY KEY (Folder, Name)
                ) WITHOUT ROWID;
                CREATE TABLE IF NOT EXISTS FolderPrefs (
                    Path              TEXT NOT NULL COLLATE NOCASE PRIMARY KEY,
                    SortColumn        TEXT,
                    SortAscending     INTEGER,
                    TreeSortColumn    TEXT,
                    TreeSortAscending INTEGER
                ) WITHOUT ROWID;
                CREATE TABLE IF NOT EXISTS FolderPicture (
                    Path TEXT NOT NULL COLLATE NOCASE PRIMARY KEY,
                    Name TEXT NOT NULL
                ) WITHOUT ROWID;
                CREATE TABLE IF NOT EXISTS LastOpened (
                    Path TEXT NOT NULL COLLATE NOCASE PRIMARY KEY,
                    Name TEXT NOT NULL
                ) WITHOUT ROWID;
                CREATE TABLE IF NOT EXISTS ItemInfo (
                    Folder      TEXT NOT NULL COLLATE NOCASE,
                    Name        TEXT NOT NULL COLLATE NOCASE,
                    HostUrl     TEXT,
                    ReferrerUrl TEXT,
                    PRIMARY KEY (Folder, Name)
                ) WITHOUT ROWID;
                CREATE TABLE IF NOT EXISTS PreviewPick (
                    Folder  TEXT NOT NULL COLLATE NOCASE,
                    Name    TEXT NOT NULL COLLATE NOCASE,
                    Picture TEXT NOT NULL,
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
            bool written = false;
            lock (Gate)
            {
                var route = Resolve(group.Key, create: true);
                if (route == null) continue;

                Exception? failure = null;
                using (var connection = Open(route, create: true))
                {
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
                        written = true;
                    }
                    catch (Exception ex) { failure = ex; }   // write-protected, busy or damaged
                }

                if (failure != null) RepairAfter(failure, group.Key);
            }

            if (written) FolderWritten?.Invoke(group.Key);
        }
    }

    // ── Rating and expectation ───────────────────────────────────────────────

    /// <summary>A rating (whole number 1-10) and an expectation (1-10, decimals allowed), either or both, of a file or folder.</summary>
    public readonly record struct Marks(int? Rating, double? Expectation);

    // What a path is stored under in Rated: the key of the folder that holds it, and its name. A drive's root has no
    // folder above it: it is stored with an empty folder and its own key as the name.
    private static (string Folder, string Name) RatedKey(LibraryRoute route, string path)
    {
        var trimmed = path.TrimEnd('\\', '/');
        var parent = Path.GetDirectoryName(trimmed);
        return string.IsNullOrEmpty(parent)
            ? ("", KeyOf(route, path))
            : (KeyOf(route, parent), Path.GetFileName(trimmed));
    }

    /// <summary>The ratings and expectations of the items (files and folders) directly in a folder, by name.</summary>
    public static Dictionary<string, Marks> GetRatings(string folderPath)
    {
        var result = new Dictionary<string, Marks>(StringComparer.OrdinalIgnoreCase);
        lock (Gate)
        {
            try
            {
                var route = Resolve(folderPath, create: false);
                if (route == null) return result;

                using var connection = Open(route, create: false);
                if (connection == null) return result;

                using var command = Command(connection, "SELECT Name, Rating, Expectation FROM Rated WHERE Folder = $f",
                    ("$f", KeyOf(route, folderPath)));
                using var reader = command.ExecuteReader();
                while (reader.Read())
                    result[reader.GetString(0)] = new Marks(
                        reader.IsDBNull(1) ? null : (int)reader.GetInt64(1),
                        reader.IsDBNull(2) ? null : reader.GetDouble(2));
            }
            catch (Exception ex)
            {
                RepairAfter(ex, folderPath);
            }
        }
        return result;
    }

    /// <summary>The rating and expectation of one file or folder (both null if it has none).</summary>
    public static Marks GetMark(string path)
    {
        var trimmed = path.TrimEnd('\\', '/');
        lock (Gate)
        {
            try
            {
                var route = Resolve(trimmed, create: false);
                if (route == null) return default;

                using var connection = Open(route, create: false);
                if (connection == null) return default;

                var (folder, name) = RatedKey(route, trimmed);
                using var command = Command(connection, "SELECT Rating, Expectation FROM Rated WHERE Folder = $f AND Name = $n",
                    ("$f", folder), ("$n", name));
                using var reader = command.ExecuteReader();
                if (!reader.Read()) return default;
                return new Marks(reader.IsDBNull(0) ? null : (int)reader.GetInt64(0), reader.IsDBNull(1) ? null : reader.GetDouble(1));
            }
            catch (Exception ex)
            {
                RepairAfter(ex, trimmed);
                return default;
            }
        }
    }

    /// <summary>Sets (or, with null, removes) the rating of files and folders. False if something could not be saved (a write-protected drive).</summary>
    public static bool SetRating(IEnumerable<string> paths, int? rating) => SetMark(paths, "Rating", rating);

    /// <summary>Sets (or, with null, removes) the expectation of files and folders. False if something could not be saved.</summary>
    public static bool SetExpectation(IEnumerable<string> paths, double? expectation) => SetMark(paths, "Expectation", expectation);

    private static bool SetMark(IEnumerable<string> paths, string column, object? value)
    {
        bool allSaved = true;

        lock (Gate)
        {
            foreach (var path in paths.Select(p => p.TrimEnd('\\', '/')).Where(p => p.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    // Removing something never creates a file on a removable drive.
                    var route = Resolve(path, create: value != null);
                    if (route == null) continue;

                    using var connection = Open(route, create: value != null);
                    if (connection == null) { if (value != null) allSaved = false; continue; }

                    var (folder, name) = RatedKey(route, path);
                    using var transaction = connection.BeginTransaction();

                    using (var set = value == null
                        ? Command(connection, $"UPDATE Rated SET {column} = NULL WHERE Folder = $f AND Name = $n", ("$f", folder), ("$n", name))
                        : Command(connection,
                            $"INSERT INTO Rated (Folder, Name, {column}) VALUES ($f, $n, $v) ON CONFLICT(Folder, Name) DO UPDATE SET {column} = $v",
                            ("$f", folder), ("$n", name), ("$v", value)))
                    {
                        set.Transaction = transaction;
                        set.ExecuteNonQuery();
                    }

                    // A row with neither value is not kept.
                    using (var tidy = Command(connection,
                        "DELETE FROM Rated WHERE Folder = $f AND Name = $n AND Rating IS NULL AND Expectation IS NULL",
                        ("$f", folder), ("$n", name)))
                    {
                        tidy.Transaction = transaction;
                        tidy.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    allSaved = false;
                    RepairAfter(ex, path);   // otherwise write-protected or busy
                }
            }
        }

        return allSaved;
    }

    // ── The picture the user chose for a folder ──────────────────────────────

    /// <summary>The name (not the path) of the picture file, in the folder itself, that the user chose as the picture of the folder; null if none.</summary>
    public static string? GetFolderPicture(string folderPath)
    {
        var folder = folderPath.TrimEnd('\\', '/');
        if (folder.Length == 0) return null;

        lock (Gate)
        {
            try
            {
                var route = Resolve(folder, create: false);
                if (route == null) return null;

                using var connection = Open(route, create: false);
                if (connection == null) return null;

                using var command = Command(connection, "SELECT Name FROM FolderPicture WHERE Path = $p", ("$p", KeyOf(route, folder)));
                return command.ExecuteScalar() as string;
            }
            catch (Exception ex)
            {
                RepairAfter(ex, folder);
                return null;
            }
        }
    }

    /// <summary>Remembers the picture file (by name) as the picture of the folder; with null the choice is removed (back to automatic). False if it could not be saved.</summary>
    public static bool SetFolderPicture(string folderPath, string? fileName)
    {
        var folder = folderPath.TrimEnd('\\', '/');
        if (folder.Length == 0) return false;

        lock (Gate)
        {
            try
            {
                // Removing never creates a file on a removable drive.
                var route = Resolve(folder, create: fileName != null);
                if (route == null) return fileName == null;

                using var connection = Open(route, create: fileName != null);
                if (connection == null) return fileName == null;

                using var command = fileName == null
                    ? Command(connection, "DELETE FROM FolderPicture WHERE Path = $p", ("$p", KeyOf(route, folder)))
                    : Command(connection, "INSERT INTO FolderPicture (Path, Name) VALUES ($p, $n) ON CONFLICT(Path) DO UPDATE SET Name = $n",
                        ("$p", KeyOf(route, folder)), ("$n", fileName));
                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                RepairAfter(ex, folder);
                return false;
            }
        }
    }

    // ── The file opened last in a folder ─────────────────────────────────────

    /// <summary>Remembers that this file was the last one opened in its folder (Jump to Last Opened File). Call when the user opens a file.</summary>
    public static void SetLastOpened(string filePath)
    {
        var path = filePath.TrimEnd('\\', '/');
        var folder = Path.GetDirectoryName(path);
        var name = Path.GetFileName(path);
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
                    "INSERT INTO LastOpened (Path, Name) VALUES ($p, $n) ON CONFLICT(Path) DO UPDATE SET Name = $n",
                    ("$p", KeyOf(route, folder)), ("$n", name));
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                RepairAfter(ex, folder);
            }
        }
    }

    /// <summary>The name of the file opened last in the folder, or null if none is remembered (or nothing can be read).</summary>
    public static string? GetLastOpened(string folderPath)
    {
        var folder = folderPath.TrimEnd('\\', '/');
        if (folder.Length == 0) return null;

        lock (Gate)
        {
            try
            {
                var route = Resolve(folder, create: false);
                if (route == null) return null;

                using var connection = Open(route, create: false);
                if (connection == null) return null;

                using var command = Command(connection, "SELECT Name FROM LastOpened WHERE Path = $p", ("$p", KeyOf(route, folder)));
                return command.ExecuteScalar() as string;
            }
            catch (Exception ex)
            {
                RepairAfter(ex, folder);
                return null;
            }
        }
    }

    // ── Preview picture of a file ────────────────────────────────────────────

    /// <summary>What the user chose as the preview picture of a file: none at all, or a picture (full path, not checked for existence).</summary>
    public readonly record struct PreviewChoice(bool None, string? Picture);

    // How the picture is written in PreviewPick.Picture: "" = none; a path that is not rooted = from the folder of the file (the
    // picture lies in that folder or below it, so it follows the folder when it is renamed or moved); "\x\y" (one backslash) =
    // from the root of the drive, on a USB drive (the letter can change); anything else = the full path.
    private static string EncodePicture(LibraryRoute route, string folder, string picture)
    {
        var full = Path.GetFullPath(picture);
        var folderFull = Path.GetFullPath(folder).TrimEnd('\\', '/') + "\\";
        if (full.StartsWith(folderFull, StringComparison.OrdinalIgnoreCase))
            return full.Substring(folderFull.Length);

        if (route.Relative && full.StartsWith(route.Root, StringComparison.OrdinalIgnoreCase))
            return "\\" + full.Substring(route.Root.TrimEnd('\\', '/').Length).TrimStart('\\');

        return full;
    }

    private static string DecodePicture(LibraryRoute route, string folder, string stored)
    {
        if (stored.StartsWith("\\") && !stored.StartsWith("\\\\") && route.Relative)
            return route.Root.TrimEnd('\\', '/') + stored;
        if (Path.IsPathRooted(stored)) return stored;
        return Path.Combine(folder, stored);
    }

    /// <summary>The preview picture the user chose for a file, or null if nothing was chosen (then the program looks for one itself).</summary>
    public static PreviewChoice? GetPreviewChoice(string filePath)
    {
        var path = filePath.TrimEnd('\\', '/');
        var folder = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(folder)) return null;

        lock (Gate)
        {
            try
            {
                var route = Resolve(folder, create: false);
                if (route == null) return null;

                using var connection = Open(route, create: false);
                if (connection == null) return null;

                var (parentKey, name) = RatedKey(route, path);
                using var command = Command(connection, "SELECT Picture FROM PreviewPick WHERE Folder = $f AND Name = $n",
                    ("$f", parentKey), ("$n", name));
                if (command.ExecuteScalar() is not string stored) return null;

                return stored.Length == 0 ? new PreviewChoice(true, null) : new PreviewChoice(false, DecodePicture(route, folder, stored));
            }
            catch (Exception ex)
            {
                RepairAfter(ex, path);
                return null;
            }
        }
    }

    /// <summary>
    /// Saves the choice (none, or a picture) or, with null, removes it (back to automatic). False if it could not be saved
    /// (a write-protected drive). Removing never creates a file on a removable drive.
    /// </summary>
    public static bool SetPreviewChoice(string filePath, PreviewChoice? choice)
    {
        var path = filePath.TrimEnd('\\', '/');
        var folder = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(folder)) return false;

        lock (Gate)
        {
            try
            {
                var route = Resolve(folder, create: choice != null);
                if (route == null) return choice == null;

                using var connection = Open(route, create: choice != null);
                if (connection == null) return choice == null;

                var (parentKey, name) = RatedKey(route, path);
                using var command = choice is not { } c
                    ? Command(connection, "DELETE FROM PreviewPick WHERE Folder = $f AND Name = $n", ("$f", parentKey), ("$n", name))
                    : Command(connection,
                        "INSERT INTO PreviewPick (Folder, Name, Picture) VALUES ($f, $n, $p) ON CONFLICT(Folder, Name) DO UPDATE SET Picture = $p",
                        ("$f", parentKey), ("$n", name),
                        ("$p", c.None || string.IsNullOrEmpty(c.Picture) ? "" : EncodePicture(route, folder, c.Picture)));
                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                RepairAfter(ex, path);
                return false;
            }
        }
    }

    // ── Where a file came from ───────────────────────────────────────────────

    /// <summary>Where a file was downloaded from (the address of the file) and the page that linked to it; either may be null.</summary>
    public sealed record ItemInfoRow(string? HostUrl, string? ReferrerUrl);

    /// <summary>What the library remembers about where a file came from, or null if nothing (or nothing can be read). Creates nothing.</summary>
    public static ItemInfoRow? GetItemInfo(string filePath)
    {
        var path = filePath.TrimEnd('\\', '/');
        var folder = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(folder)) return null;

        lock (Gate)
        {
            try
            {
                var route = Resolve(folder, create: false);
                if (route == null) return null;

                using var connection = Open(route, create: false);
                if (connection == null) return null;

                var (parentKey, name) = RatedKey(route, path);
                using var command = Command(connection, "SELECT HostUrl, ReferrerUrl FROM ItemInfo WHERE Folder = $f AND Name = $n",
                    ("$f", parentKey), ("$n", name));
                using var reader = command.ExecuteReader();
                if (!reader.Read()) return null;
                return new ItemInfoRow(reader.IsDBNull(0) ? null : reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1));
            }
            catch (Exception ex)
            {
                RepairAfter(ex, path);
                return null;
            }
        }
    }

    /// <summary>
    /// Remembers where a file came from. An empty value never replaces a stored one (the Mark of the Web may carry only the site, or
    /// nothing, after the file was edited). Never creates a file on a removable drive: it writes only where the library file exists
    /// already. Returns true if the row is there afterwards.
    /// </summary>
    public static bool SetItemInfo(string filePath, string? hostUrl, string? referrerUrl)
    {
        // Only real web addresses are worth keeping: not "about:internet" (Edge, when it does not know the address) and not this computer's
        // own server (localhost, 127.x.x.x, ::1: a local program such as Pinokio or a Gradio page, not a download from the internet).
        static bool IsWeb(string? url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) return false;
            var host = uri.Host;
            return !(host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
                     || (System.Net.IPAddress.TryParse(host, out var ip) && System.Net.IPAddress.IsLoopback(ip)));
        }
        if (!IsWeb(hostUrl?.Trim())) hostUrl = null;
        if (!IsWeb(referrerUrl?.Trim())) referrerUrl = null;
        if (hostUrl == null && referrerUrl == null) return false;

        var path = filePath.TrimEnd('\\', '/');
        var folder = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(folder)) return false;

        lock (Gate)
        {
            try
            {
                var route = Resolve(folder, create: false);
                if (route == null) return false;

                using var connection = Open(route, create: false);
                if (connection == null) return false;

                var (parentKey, name) = RatedKey(route, path);
                string? oldHost = null, oldReferrer = null;
                using (var read = Command(connection, "SELECT HostUrl, ReferrerUrl FROM ItemInfo WHERE Folder = $f AND Name = $n",
                           ("$f", parentKey), ("$n", name)))
                using (var reader = read.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        oldHost     = reader.IsDBNull(0) ? null : reader.GetString(0);
                        oldReferrer = reader.IsDBNull(1) ? null : reader.GetString(1);
                    }
                }

                var newHost     = hostUrl ?? oldHost;
                var newReferrer = referrerUrl ?? oldReferrer;
                if (newHost == oldHost && newReferrer == oldReferrer && (oldHost != null || oldReferrer != null)) return true;   // nothing new

                using var write = Command(connection,
                    "INSERT INTO ItemInfo (Folder, Name, HostUrl, ReferrerUrl) VALUES ($f, $n, $h, $r) "
                    + "ON CONFLICT(Folder, Name) DO UPDATE SET HostUrl = $h, ReferrerUrl = $r",
                    ("$f", parentKey), ("$n", name), ("$h", newHost), ("$r", newReferrer));
                write.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                RepairAfter(ex, path);
                return false;
            }
        }
    }

    // ── Sort order of a folder ───────────────────────────────────────────────

    /// <summary>
    /// What the user chose for a folder: how its list is sorted, and how its subfolders are sorted in the tree. The column is the
    /// name of the explorer's SortColumn (a text, so a newer app can add columns). Null where nothing was chosen.
    /// </summary>
    public sealed record FolderPrefs(string? SortColumn, bool SortAscending, string? TreeSortColumn, bool TreeSortAscending);

    /// <summary>The saved sort orders of a folder, or null if none was ever chosen for it (or nothing can be read).</summary>
    public static FolderPrefs? GetFolderPrefs(string folderPath)
    {
        var path = folderPath.TrimEnd('\\', '/');
        if (path.Length == 0) return null;

        lock (Gate)
        {
            try
            {
                var route = Resolve(path, create: false);   // browsing leaves no trace on a removable drive
                if (route == null) return null;

                using var connection = Open(route, create: false);
                if (connection == null) return null;

                using var command = Command(connection,
                    "SELECT SortColumn, SortAscending, TreeSortColumn, TreeSortAscending FROM FolderPrefs WHERE Path = $p",
                    ("$p", KeyOf(route, path)));
                using var reader = command.ExecuteReader();
                if (!reader.Read()) return null;

                return new FolderPrefs(
                    reader.IsDBNull(0) ? null : reader.GetString(0), reader.IsDBNull(1) || reader.GetInt64(1) != 0,
                    reader.IsDBNull(2) ? null : reader.GetString(2), reader.IsDBNull(3) || reader.GetInt64(3) != 0);
            }
            catch (Exception ex)
            {
                RepairAfter(ex, path);
                return null;
            }
        }
    }

    /// <summary>Saves how the list of a folder is sorted. False if it could not be saved (a write-protected drive).</summary>
    public static bool SetFolderSort(string folderPath, string column, bool ascending) =>
        SetPrefs(folderPath, "SortColumn", "SortAscending", column, ascending);

    /// <summary>Saves how the subfolders of a folder are sorted in the tree. False if it could not be saved.</summary>
    public static bool SetFolderTreeSort(string folderPath, string column, bool ascending) =>
        SetPrefs(folderPath, "TreeSortColumn", "TreeSortAscending", column, ascending);

    private static bool SetPrefs(string folderPath, string columnField, string ascendingField, string column, bool ascending)
    {
        var path = folderPath.TrimEnd('\\', '/');
        if (path.Length == 0) return false;

        lock (Gate)
        {
            try
            {
                var route = Resolve(path, create: true);
                if (route == null) return false;

                using var connection = Open(route, create: true);
                if (connection == null) return false;

                using var command = Command(connection,
                    $"INSERT INTO FolderPrefs (Path, {columnField}, {ascendingField}) VALUES ($p, $c, $a) " +
                    $"ON CONFLICT(Path) DO UPDATE SET {columnField} = $c, {ascendingField} = $a",
                    ("$p", KeyOf(route, path)), ("$c", column), ("$a", ascending ? 1 : 0));
                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                RepairAfter(ex, path);   // otherwise write-protected or busy
                return false;
            }
        }
    }

    // ── Forgetting and moving what is remembered ─────────────────────────────

    // Opens each database file at most once, in one transaction that is committed when the session ends: forgetting or moving
    // 500 files costs one write, not 500.
    private sealed class Session : IDisposable
    {
        private readonly Dictionary<string, (SqliteConnection Connection, SqliteTransaction Transaction)> _open =
            new(StringComparer.OrdinalIgnoreCase);

        public SqliteConnection? Get(LibraryRoute route, bool create)
        {
            if (_open.TryGetValue(route.DbFile, out var existing)) return existing.Connection;

            var connection = Open(route, create);
            if (connection == null) return null;
            _open[route.DbFile] = (connection, connection.BeginTransaction());
            return connection;
        }

        public SqliteCommand Command(SqliteConnection connection, string sql, params (string Name, object? Value)[] parameters)
        {
            var command = LibraryStore.Command(connection, sql, parameters);
            command.Transaction = _open.Values.First(v => v.Connection == connection).Transaction;
            return command;
        }

        public void Dispose()
        {
            foreach (var (connection, transaction) in _open.Values)
            {
                try { transaction.Commit(); } catch { /* write-protected or busy: nothing was changed */ }
                connection.Dispose();
            }
            _open.Clear();
        }
    }

    // The rows below a folder have its key plus a backslash in front of them: "K\..." lies between "K\" and "K]".
    private static (string Below, string Above) SubtreeRange(string key) => (key + "\\", key + "]");

    // Removes the rows of the item itself (a file, or a folder as an item of its parent) and of everything below it.
    private static void DeleteRows(Session session, SqliteConnection connection, string parentKey, string name, string key)
    {
        var (below, above) = SubtreeRange(key);

        foreach (var table in new[] { "File", "Rated", "PreviewPick", "ItemInfo" })
        {
            using (var item = session.Command(connection, $"DELETE FROM {table} WHERE Folder = $p AND Name = $n", ("$p", parentKey), ("$n", name)))
                item.ExecuteNonQuery();
            using (var sub = session.Command(connection, $"DELETE FROM {table} WHERE Folder = $k OR (Folder > $a AND Folder < $b)",
                       ("$k", key), ("$a", below), ("$b", above)))
                sub.ExecuteNonQuery();
        }

        foreach (var table in new[] { "Folder", "FolderPrefs", "LastOpened", "FolderPicture" })
        {
            using var folders = session.Command(connection, $"DELETE FROM {table} WHERE Path = $k OR (Path > $a AND Path < $b)",
                ("$k", key), ("$a", below), ("$b", above));
            folders.ExecuteNonQuery();
        }

        // The item itself, if it was the file opened last in its folder, or the picture chosen for its folder.
        foreach (var table in new[] { "LastOpened", "FolderPicture" })
        {
            using var last = session.Command(connection, $"DELETE FROM {table} WHERE Path = $p AND Name = $n COLLATE NOCASE",
                ("$p", parentKey), ("$n", name));
            last.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Forgets everything remembered about files and folders that were deleted: read marks, resume positions, ratings and
    /// expectations, and for a folder (or a packed file) everything below it too. Call it for what the app deleted itself.
    /// </summary>
    public static void Forget(IEnumerable<string> paths)
    {
        lock (Gate)
        {
            try
            {
                using var session = new Session();
                foreach (var raw in paths)
                {
                    var path = raw.TrimEnd('\\', '/');
                    if (path.Length == 0) continue;

                    try
                    {
                        var route = Resolve(path, create: false);   // nothing is created just to forget something
                        if (route == null) continue;
                        var connection = session.Get(route, create: false);
                        if (connection == null) continue;

                        var (parentKey, name) = RatedKey(route, path);
                        DeleteRows(session, connection, parentKey, name, KeyOf(route, path));
                    }
                    catch { /* this one stays remembered; harmless */ }
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// What is remembered about a file or folder that was renamed or moved follows it to its new path (for a folder: its own
    /// state and rating and everything below it), also to another drive. Call it for what the app moved itself.
    /// </summary>
    public static void Move(IEnumerable<(string From, string To)> moves)
    {
        lock (Gate)
        {
            try
            {
                using var session = new Session();
                foreach (var (rawFrom, rawTo) in moves)
                {
                    var from = rawFrom.TrimEnd('\\', '/');
                    var to   = rawTo.TrimEnd('\\', '/');
                    if (from.Length == 0 || to.Length == 0) continue;

                    try { MoveRows(session, from, to); }
                    catch { /* it keeps its marks under the old path; harmless */ }
                }
            }
            catch { }
        }
    }

    private static List<object?[]> ReadAll(Session session, SqliteConnection connection, string sql, params (string Name, object? Value)[] parameters)
    {
        var rows = new List<object?[]>();
        using var command = session.Command(connection, sql, parameters);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var row = new object?[reader.FieldCount];
            for (int i = 0; i < row.Length; i++) row[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }
        return rows;
    }

    private static void MoveRows(Session session, string from, string to)
    {
        var fromRoute = Resolve(from, create: false);
        if (fromRoute == null) return;
        var fromConnection = session.Get(fromRoute, create: false);
        if (fromConnection == null) return;

        var fromKey = KeyOf(fromRoute, from);
        var (fromParent, fromName) = RatedKey(fromRoute, from);
        var (below, above) = SubtreeRange(fromKey);

        // Everything remembered about the item and below it, read before anything is changed.
        var p = new (string, object?)[] { ("$p", fromParent), ("$n", fromName), ("$k", fromKey), ("$a", below), ("$b", above) };
        var files   = ReadAll(session, fromConnection,
            "SELECT Folder, Name, ReadAt, Position, Length FROM File WHERE (Folder = $p AND Name = $n) OR Folder = $k OR (Folder > $a AND Folder < $b)", p);
        var rated   = ReadAll(session, fromConnection,
            "SELECT Folder, Name, Rating, Expectation FROM Rated WHERE (Folder = $p AND Name = $n) OR Folder = $k OR (Folder > $a AND Folder < $b)", p);
        var folders = ReadAll(session, fromConnection,
            "SELECT Path, OwnUnread, Complete FROM Folder WHERE Path = $k OR (Path > $a AND Path < $b)", p);
        var prefs   = ReadAll(session, fromConnection,
            "SELECT Path, SortColumn, SortAscending, TreeSortColumn, TreeSortAscending FROM FolderPrefs WHERE Path = $k OR (Path > $a AND Path < $b)", p);
        var picks   = ReadAll(session, fromConnection,
            "SELECT Folder, Name, Picture FROM PreviewPick WHERE (Folder = $p AND Name = $n) OR Folder = $k OR (Folder > $a AND Folder < $b)", p);
        var infos   = ReadAll(session, fromConnection,
            "SELECT Folder, Name, HostUrl, ReferrerUrl FROM ItemInfo WHERE (Folder = $p AND Name = $n) OR Folder = $k OR (Folder > $a AND Folder < $b)", p);
        var lasts   = ReadAll(session, fromConnection,
            "SELECT Path, Name FROM LastOpened WHERE Path = $k OR (Path > $a AND Path < $b)", p);   // folders: the item and below
        var lastOfItem = ReadAll(session, fromConnection,
            "SELECT Path, Name FROM LastOpened WHERE Path = $p AND Name = $n COLLATE NOCASE", p);   // the item as a file of its folder
        var folderPics = ReadAll(session, fromConnection,
            "SELECT Path, Name FROM FolderPicture WHERE Path = $k OR (Path > $a AND Path < $b)", p);
        var folderPicOfItem = ReadAll(session, fromConnection,
            "SELECT Path, Name FROM FolderPicture WHERE Path = $p AND Name = $n COLLATE NOCASE", p);
        if (files.Count == 0 && rated.Count == 0 && folders.Count == 0 && prefs.Count == 0 && picks.Count == 0 && infos.Count == 0
            && lasts.Count == 0 && lastOfItem.Count == 0 && folderPics.Count == 0 && folderPicOfItem.Count == 0) return;

        var toRoute = Resolve(to, create: true);
        if (toRoute == null) return;
        var toConnection = session.Get(toRoute, create: true);
        if (toConnection == null) return;

        var toKey = KeyOf(toRoute, to);
        var (toParent, toName) = RatedKey(toRoute, to);

        // A change of the case of the name only: the keys are the same (they ignore case), nothing to move.
        if (string.Equals(fromRoute.DbFile, toRoute.DbFile, StringComparison.OrdinalIgnoreCase)
            && string.Equals(fromKey, toKey, StringComparison.OrdinalIgnoreCase))
            return;

        // The old rows go first (they may be in the same file); then the rows are written under their new keys.
        DeleteRows(session, fromConnection, fromParent, fromName, fromKey);

        // The key of a folder below the item: the old key of the item becomes the new one.
        string Remap(string key) =>
            key.Equals(fromKey, StringComparison.OrdinalIgnoreCase) ? toKey : toKey + key.Substring(fromKey.Length);

        bool IsTheItem(object? folder, object? name) =>
            string.Equals((string?)folder, fromParent, StringComparison.OrdinalIgnoreCase)
            && string.Equals((string?)name, fromName, StringComparison.OrdinalIgnoreCase);

        foreach (var r in files)
        {
            var (folder, name) = IsTheItem(r[0], r[1]) ? (toParent, toName) : (Remap((string)r[0]!), (string)r[1]!);
            using var insert = session.Command(toConnection,
                "INSERT OR REPLACE INTO File (Folder, Name, ReadAt, Position, Length) VALUES ($f, $n, $t, $p, $l)",
                ("$f", folder), ("$n", name), ("$t", r[2]), ("$p", r[3]), ("$l", r[4]));
            insert.ExecuteNonQuery();
        }

        foreach (var r in rated)
        {
            var (folder, name) = IsTheItem(r[0], r[1]) ? (toParent, toName) : (Remap((string)r[0]!), (string)r[1]!);
            using var insert = session.Command(toConnection,
                "INSERT OR REPLACE INTO Rated (Folder, Name, Rating, Expectation) VALUES ($f, $n, $r, $e)",
                ("$f", folder), ("$n", name), ("$r", r[2]), ("$e", r[3]));
            insert.ExecuteNonQuery();
        }

        foreach (var r in infos)
        {
            var (folder, name) = IsTheItem(r[0], r[1]) ? (toParent, toName) : (Remap((string)r[0]!), (string)r[1]!);
            using var insert = session.Command(toConnection,
                "INSERT OR REPLACE INTO ItemInfo (Folder, Name, HostUrl, ReferrerUrl) VALUES ($f, $n, $h, $r)",
                ("$f", folder), ("$n", name), ("$h", r[2]), ("$r", r[3]));
            insert.ExecuteNonQuery();
        }

        foreach (var r in lasts)
        {
            using var insert = session.Command(toConnection,
                "INSERT OR REPLACE INTO LastOpened (Path, Name) VALUES ($p, $n)", ("$p", Remap((string)r[0]!)), ("$n", r[1]));
            insert.ExecuteNonQuery();
        }

        foreach (var r in lastOfItem)   // the file renamed or moved was the last opened one of its folder: the new folder remembers the new name
        {
            using var insert = session.Command(toConnection,
                "INSERT OR REPLACE INTO LastOpened (Path, Name) VALUES ($p, $n)", ("$p", toParent), ("$n", toName));
            insert.ExecuteNonQuery();
        }

        foreach (var r in folderPics)
        {
            using var insert = session.Command(toConnection,
                "INSERT OR REPLACE INTO FolderPicture (Path, Name) VALUES ($p, $n)", ("$p", Remap((string)r[0]!)), ("$n", r[1]));
            insert.ExecuteNonQuery();
        }

        foreach (var r in folderPicOfItem)   // the chosen picture was renamed: its folder remembers the new name; moved to another folder: the choice is gone
        {
            if (!string.Equals(toParent, fromParent, StringComparison.OrdinalIgnoreCase)) break;
            using var insert = session.Command(toConnection,
                "INSERT OR REPLACE INTO FolderPicture (Path, Name) VALUES ($p, $n)", ("$p", toParent), ("$n", toName));
            insert.ExecuteNonQuery();
        }

        foreach (var r in picks)
        {
            bool item = IsTheItem(r[0], r[1]);
            var (folder, name) = item ? (toParent, toName) : (Remap((string)r[0]!), (string)r[1]!);

            // The file itself may have moved to another folder: the picture is written again from where it is now.
            var picture = (string)r[2]!;
            if (item && picture.Length > 0)
            {
                var oldFolder = Path.GetDirectoryName(from)!;
                var newFolder = Path.GetDirectoryName(to)!;
                picture = EncodePicture(toRoute, newFolder, DecodePicture(fromRoute, oldFolder, picture));
            }

            using var insert = session.Command(toConnection,
                "INSERT OR REPLACE INTO PreviewPick (Folder, Name, Picture) VALUES ($f, $n, $p)",
                ("$f", folder), ("$n", name), ("$p", picture));
            insert.ExecuteNonQuery();
        }

        foreach (var r in folders)
        {
            using var insert = session.Command(toConnection,
                "INSERT OR REPLACE INTO Folder(Path, OwnUnread, Complete) VALUES ($p, $o, $c)",
                ("$p", Remap((string)r[0]!)), ("$o", r[1]), ("$c", r[2]));
            insert.ExecuteNonQuery();
        }

        foreach (var r in prefs)
        {
            using var insert = session.Command(toConnection,
                "INSERT OR REPLACE INTO FolderPrefs (Path, SortColumn, SortAscending, TreeSortColumn, TreeSortAscending) VALUES ($p, $c, $a, $tc, $ta)",
                ("$p", Remap((string)r[0]!)), ("$c", r[1]), ("$a", r[2]), ("$tc", r[3]), ("$ta", r[4]));
            insert.ExecuteNonQuery();
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
            catch (Exception ex)
            {
                RepairAfter(ex, folder);
                return null;
            }
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
            catch (Exception ex)
            {
                RepairAfter(ex, folder);   // otherwise write-protected or busy: the next save tries again
            }
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
            catch (Exception ex)
            {
                RepairAfter(ex, folder);
            }
        }
    }
}
