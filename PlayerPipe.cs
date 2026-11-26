// PlayerPipe.cs: how SweWolf File Explorer (or another program) talks to a running SweWolf Media Player.
//
// A named pipe, one per Windows user (the name has the user's id, and the pipe is for the current user only). Messages are one line of JSON
// each. The explorer sends commands (play a file, pause, resume, release, front, close); the player sends events (hello, ended, closed, error).
// The explorer decides everything about a playlist (what comes next): the player only plays what it is told and says when a file has ended.
// The same record is in the explorer (Helpers/PlayerLink.cs): change both together, and raise Version when the messages change.
//
// Several programs can be connected at once: the explorer keeps its connection for as long as a playlist runs, and a second start of the player
// with a file connects for a moment to hand the file over (Program.cs: one player window per user). Events go to everyone connected.
// A connection that sent a playlist command is the "controller": when it goes away the playlist is over (ClientDisconnected).
//
// History:
//   2026-10-20  First version (protocol 1).

using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Text.Json;

namespace SwewolfMediaPlayer
{
    /// <summary>A command from the explorer. Names are in camelCase in the JSON ("cmd", "path", "libraryPath", ...).</summary>
    internal sealed record PipeCommand(string Cmd, string? Path = null, string? LibraryPath = null, long StartMs = 0,
                                       bool NoResume = false, bool Front = false, bool Playlist = false, bool StartPaused = false);

    internal sealed class PlayerPipe : IDisposable
    {
        public const int Version = 1;
        private const int MaxConnections = 8;

        internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        /// <summary>The pipe's name. SWEWOLF_PLAYER_PIPE gives a test copy a pipe of its own, so it never talks to the user's real player.</summary>
        public static string Name =>
            Environment.GetEnvironmentVariable("SWEWOLF_PLAYER_PIPE") is { Length: > 0 } custom
                ? custom
                : "SweWolfMediaPlayer." + (WindowsIdentity.GetCurrent().User?.Value ?? Environment.UserName);

        public static string MutexName => "Local\\" + Name;

        /// <summary>A command arrived (on a thread-pool thread).</summary>
        public event Action<PipeCommand>? CommandReceived;

        /// <summary>The program that ran a playlist is gone (closed, crashed): the playlist is over.</summary>
        public event Action? ClientDisconnected;

        private sealed class Connection
        {
            public required NamedPipeServerStream Stream;
            public bool Controller;
        }

        private readonly CancellationTokenSource _cts = new();
        private readonly object _gate = new();
        private readonly List<Connection> _connections = new();

        public void Start() => _ = Task.Run(Loop);

        // Always one pipe instance is waiting for the next program; each connection is looked after on its own.
        private async Task Loop()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    // With buffers: a pipe has none by default, and then a write waits for the other side to read it, so two programs
                    // that both write first (the hello of this side, the command of the other) would wait for each other for ever.
                    var server = new NamedPipeServerStream(Name, PipeDirection.InOut, MaxConnections, PipeTransmissionMode.Byte,
                                                           PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly, 64 * 1024, 64 * 1024);
                    await server.WaitForConnectionAsync(_cts.Token);
                    _ = Task.Run(() => Serve(new Connection { Stream = server }));
                }
                catch (OperationCanceledException) { break; }
                catch (Exception) { await Task.Delay(500); }
            }
        }

        private async Task Serve(Connection connection)
        {
            lock (_gate) { _connections.Add(connection); }
            try
            {
                Write(connection, new { @event = "hello", version = Version });

                using var reader = new StreamReader(connection.Stream, new UTF8Encoding(false));
                string? line;
                while ((line = await reader.ReadLineAsync(_cts.Token)) != null)
                {
                    PipeCommand? command = null;
                    try { command = JsonSerializer.Deserialize<PipeCommand>(line, Json); }
                    catch (JsonException) { /* a line that cannot be read is ignored */ }
                    if (command == null || string.IsNullOrEmpty(command.Cmd)) continue;

                    if (command.Playlist) connection.Controller = true;
                    CommandReceived?.Invoke(command);
                }
            }
            catch (OperationCanceledException) { }
            catch (IOException) { /* the other side went away */ }
            finally
            {
                lock (_gate) { _connections.Remove(connection); }
                try { connection.Stream.Dispose(); } catch { }
                if (connection.Controller) ClientDisconnected?.Invoke();
            }
        }

        private static void Write(Connection connection, object message)
        {
            try
            {
                if (!connection.Stream.IsConnected) return;
                var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, Json) + "\n");
                lock (connection) { connection.Stream.Write(bytes, 0, bytes.Length); connection.Stream.Flush(); }
            }
            catch (Exception) { /* gone */ }
        }

        /// <summary>Sends an event to every program that is connected.</summary>
        public void Send(object message)
        {
            Connection[] all;
            lock (_gate) { all = _connections.ToArray(); }
            foreach (var connection in all) Write(connection, message);
        }

        /// <summary>
        /// Hands a command to the player that is running already (a second start with a file). False if no player answers within the time:
        /// the caller then goes on as a player of its own.
        /// </summary>
        public static bool TrySend(PipeCommand command, int waitMs = 5000)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", Name, PipeDirection.InOut, PipeOptions.CurrentUserOnly);
                client.Connect(waitMs);
                var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(command, Json) + "\n");
                client.Write(bytes, 0, bytes.Length);
                client.Flush();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            Connection[] all;
            lock (_gate) { all = _connections.ToArray(); }
            foreach (var connection in all) { try { connection.Stream.Dispose(); } catch { } }
        }
    }
}
