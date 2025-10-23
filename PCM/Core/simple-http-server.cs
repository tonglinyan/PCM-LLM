using System.Net;
using System.Text;

namespace PCM.Core
{
    public class SimpleHttpServer
    {
        private readonly int _port;
        Thread T;
        HttpListener server;
        private string _message = "";
        public SimpleHttpServer(int port = 7500)
        {
            _port = port;
        }
        public void Start()
        {
            T = new Thread(Run);
            T.Start();
        }
        public void Stop()
        {
            server.Close();
            Console.WriteLine("Web server stopped.");
        }
        public void SetMessage(string newMessage)
        {
            _message = newMessage;
        }

        private void Run()
        {
            server = new HttpListener();
            server.Prefixes.Add("http://127.0.0.1:" + _port + "/");
            server.Prefixes.Add("http://localhost:" + _port + "/");
            server.Start();
            server.BeginGetContext(OnContext, null);
            Console.WriteLine("Web Server listening...");
        }
        private void OnContext(IAsyncResult ar)
        {
            var context = server.EndGetContext(ar);
            server.BeginGetContext(OnContext, null);
            var request = context.Request;
            var response = context.Response;
            byte[] buffer;
            if (request.Url.LocalPath == "/data")
            {

                buffer = Encoding.UTF8.GetBytes(_message);
                response.ContentLength64 = buffer.Length;
            }
            else
            {
                string page = Directory.GetCurrentDirectory() + "/web-display" + context.Request.Url.LocalPath;

                if (context.Request.Url.LocalPath == "/")
                    page += "index.html";
                try
                {
                    TextReader tr = new StreamReader(page);
                    string msg = tr.ReadToEnd();
                    buffer = Encoding.UTF8.GetBytes(msg);
                }
                catch (Exception)
                {
                    buffer = Encoding.UTF8.GetBytes("404");
                }
            }
            Stream st = response.OutputStream;
            st.Write(buffer, 0, buffer.Length);
            st.Close();
            context.Response.Close();
        }
    }
}