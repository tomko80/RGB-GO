using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MSIGS.Server
{
    class Webhook
    {
        private volatile bool run;

        public Webhook()
        {
            this.ContentType = "application/json";
            this.HttpMethod = "POST";
            this.Address = "http://127.0.0.1:9000/";
        }

        public string ContentType { get; set; }
        public string HttpMethod { get; set; }
        public string Address { get; set; }

        public void Start()
        {
            HttpListener server = new HttpListener();
            server.Prefixes.Add(this.Address);  

            server.Start();

            //TODO: Add started event
            Console.WriteLine(string.Format("Webhook started, listening to {0}", this.Address));

            run = true;
            while (run)
            {
                HttpListenerContext context = server.GetContext();

                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                if (request.HttpMethod != this.HttpMethod || request.ContentType != this.ContentType || !request.HasEntityBody)
                {
                    response.StatusCode = 400;
                    response.StatusDescription = "Invalid request";
                    response.Close();
                    continue;
                }

                try
                {
                    string payload = ReadPayload(request);
                    OnPushEvent(new WebhookEventArgs(payload));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR: {0}", ex.Message);

                    response.StatusCode = 500;
                    response.StatusDescription = ex.Message;
                }

            }
        }

        public void Stop()
        {
            run = false;
        }

        private string ReadPayload(HttpListenerRequest request)
        {
            string payload;

            using (Stream body = request.InputStream)
            {
                using (StreamReader reader = new StreamReader(body, request.ContentEncoding))
                {
                    payload = reader.ReadToEnd();
                }
            }

            return payload;
        }

        public event EventHandler<WebhookEventArgs> OnPush;

        protected virtual void OnPushEvent(WebhookEventArgs e)
        {
            OnPush?.Invoke(this, e);
        }
    }

    public class WebhookEventArgs: EventArgs
    {
        public WebhookEventArgs(string data)
        {
            this.Data = data;
        }

        public string Data { get; private set; }
    }
}
