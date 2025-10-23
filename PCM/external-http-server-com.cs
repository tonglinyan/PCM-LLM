using System.Text;
namespace PCM
{
    public static class ExternalHttpServerCom
    {
        public static long _lastSent = 0;
        public static long _Delay = 30;
        private static readonly HttpClient client = new();

        public static async void PostUpdate(string JsonMessage)
        {
            var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (now - _lastSent <= _Delay)
            {
                return;
            }
            _lastSent = now;
            try
            {
                var response = await client.PostAsync("http://localhost:47500/data", new StringContent(JsonMessage, Encoding.UTF8, "application/json"));
                var responseString = await response.Content.ReadAsStringAsync();
            }
            catch (Exception) { }
        }
    }
}

