using Crsch_3.Jsonstructs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Crsch_3 {
    class WbscktBehavior : WebSocketBehavior {
        public void BroadcastTo(string Lgn, string msg) {
            Sessions.Broadcast(Encoding.UTF8.GetBytes("{\"Login\": \"" + Lgn + "\"}"));
        }
        protected override void OnOpen() {

            base.OnOpen();
        }
        protected override void OnClose(CloseEventArgs e) {
           
            base.OnClose(e);
        }
    }
}
