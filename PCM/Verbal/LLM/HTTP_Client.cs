using System.Text;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace PCM.Verbal.LLM
{
    public class HTTPClient
    {
        public string serverUrl = "http://127.0.0.1:8888";
        private static readonly HttpClient client = new ();

        public HTTPClient()
        {
            // Optionally configure the HttpClient here
        }

        /// <summary>
        /// Send input to server via HTTP POST
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <returns>The response from the server</returns>
        public async Task<string> SendInputAsync(string message)
        {
            try
            {
                var content = new StringContent(message, Encoding.UTF8, "text/plain");
                HttpResponseMessage response = await client.PostAsync(serverUrl, content);

                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return null;
            }
        }

        public async Task<string> InitAsync(string message)
        {
            try
            {
                var content = new StringContent(message, Encoding.UTF8, "text/plain");
                HttpResponseMessage response = await client.PostAsync(serverUrl + "/start", content);
                Console.WriteLine("*********************");
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return null;
            }
        }
    }
}
