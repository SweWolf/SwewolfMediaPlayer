// InternetCheck.cs: finds out whether the computer really has an internet connection.
//
// Two small servers that exist for this are asked at the same time (short timeout): the one Windows itself uses for its network indicator
// (Microsoft) and the one Android uses (Google). Either answering correctly means "online". The answer must be the exact one: a login page of a
// hotel network (a "captive portal") answers 200 with its own page, which is not the internet. "A network adapter is up" (NetworkInterface) is not
// enough: it says nothing about the internet.
//
// History:
//   2026-11-18  First version (used by the channel history: a channel is not marked as failing while the computer is offline).

namespace SwewolfMediaPlayer
{
    internal static class InternetCheck
    {
        private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(3) };

        public static async Task<bool> IsOnlineAsync()
        {
            var checks = new List<Task<bool>>
            {
                Check("http://www.msftconnecttest.com/connecttest.txt", async r => (await r.Content.ReadAsStringAsync()).Trim() == "Microsoft Connect Test"),
                Check("http://connectivitycheck.gstatic.com/generate_204", r => Task.FromResult((int)r.StatusCode == 204)),
            };
            while (checks.Count > 0)
            {
                Task<bool> done = await Task.WhenAny(checks);
                checks.Remove(done);
                if (done.Result)
                {
                    return true;
                }
            }
            return false;
        }

        private static async Task<bool> Check(string url, Func<HttpResponseMessage, Task<bool>> isRight)
        {
            try
            {
                using HttpResponseMessage response = await Client.GetAsync(url, HttpCompletionOption.ResponseContentRead);
                return await isRight(response);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
            {
                return false;
            }
        }
    }
}
