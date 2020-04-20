using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json.Linq;
using System.IO;
using MSIGS.Server;

namespace MSIGS.Server
{
    class Program
    {
        const string authKey = "7b94551b-fc0c-434f-9691-bdf4a2aa593e";

        static void Main(string[] args)
        {
            var program = new Program();
            program.Start();
        }

        public Program()
        {
            //Install: read reg + copy cfg
        }

        private void Start()
        {
            var gameState = new GameState(authKey);
            LedOrchestrator orchestrator = new LedOrchestrator(gameState);

            //TODO: Move to config file
            Webhook webhook = new Webhook()
            {
                Address = "http://127.0.0.1:9000/"
            };

            webhook.OnPush += (object sender, WebhookEventArgs e) => {
                gameState.Read(e.Data);
            };

            webhook.Start();
        }
    }
}
