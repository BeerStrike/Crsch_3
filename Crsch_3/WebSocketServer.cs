using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Crsch_3 {
    class WebSocketServer {
        Dictionary<string, WebSocket> d;
        public WebSocketServer() {
            d = new Dictionary<string, WebSocket>();
        }
        public void addLisener(string Login,WebSocket ws) {
            if (d.ContainsKey(Login)) {
                d[Login].Dispose();
                d[Login] = ws;
            }
            else
                d.Add(Login, ws);
        }
        public  async void sendMessageTo(string to,string msg) {
            if (d.ContainsKey(to)) {
                 CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
                await d[to].SendAsync(Encoding.UTF8.GetBytes(msg),WebSocketMessageType.Text,true,cancelTokenSource.Token);
            }
        }
    }
}
