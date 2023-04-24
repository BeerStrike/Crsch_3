using Crsch_3.Jsonstructs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using WebSocketSharp.Server;
using WebSocketSharp;
namespace Crsch_3 {
    class HttpServer:IDisposable{
        private HttpListener srv;
        private DatabaseConnector db;
        public delegate void Logs(string s);
        public event Logs Log;
        private WebSocketServer wsrw;
        private WebSocket wbsct;
        private WbscktBehavior wbscktController;
        public HttpServer(int webSocketPort,int port,string dbadr,int dbport,string dbnme,string dblogin,string dbpass)   {
            srv= new HttpListener();
            WebSocketServer wsrw=new WebSocketServer("ws://127.0.0.1:" + webSocketPort.ToString() + "/");
            wbscktController = new WbscktBehavior();
            wsrw.AddWebSocketService<WbscktBehavior>("/",()=>wbscktController);
            wsrw.ReuseAddress = true;
            wsrw.Start();
            srv.Prefixes.Add("http://*:" + port.ToString()+"/");
          db=new DatabaseConnector(dbadr, dbport, dbnme, dblogin, dbpass);
            db.ErrorLog += DbLogReciver;
        }
        private void DbLogReciver(string s) {
            Log("Ошибка базы данных: " + s);
        }
        public bool Start() {
            try {
                srv.Start();
            } catch(HttpListenerException e) {
                Log("Ошибка при запуске сервера: " + e.Message);
                return false;
            }
            if (db.Start())
                Reciver();
            else return false;
            return true;
        }
        private async void Reciver()  {
            while (true) {
                try {
                    var ctxt = await srv.GetContextAsync();
                   // switch (ctxt.Request.Headers["Content-Type"]) {
                   //  case "application/json": {
                        switch (ctxt.Request.HttpMethod) {
                                    case "POST": {
                                        try {
                                            string jsonstr;
                                            using (var reader = new StreamReader(ctxt.Request.InputStream, ctxt.Request.ContentEncoding)) {
                                                jsonstr = reader.ReadToEnd();
                                            }
                                            Reqtype rt = JsonSerializer.Deserialize<Reqtype>(jsonstr);
                                            switch (rt.RequestType) {
                                                case "Registration": {
                                                    Acount ac = JsonSerializer.Deserialize<Acount>(jsonstr);
                                                    using (Stream output = ctxt.Response.OutputStream) {
                                                        if (db.Register(ac.Login, ac.Password)) {
                                                            ctxt.Response.StatusCode = 200;
                                                            output.Write((Encoding.UTF8.GetBytes("OK")));
                                                        }
                                                        else {
                                                            ctxt.Response.StatusCode = 418;
                                                            output.Write((Encoding.UTF8.GetBytes("Account alredy exist")));
                                                        }
                                                        output.Flush();
                                                        ctxt.Response.Close();
                                                    }
                                                }
                                                break;
                                                case "Autorization": {
                                                    Acount ac = JsonSerializer.Deserialize<Acount>(jsonstr);
                                                    using (Stream output = ctxt.Response.OutputStream) {
                                                        if (db.Autorize(ac.Login, ac.Password)) {
                                                            ctxt.Response.StatusCode = 200;
                                                            output.Write((Encoding.UTF8.GetBytes("OK")));
                                                        }
                                                        else {
                                                            ctxt.Response.StatusCode = 418;
                                                            output.Write((Encoding.UTF8.GetBytes("Login and password do not match")));
                                                        }
                                                        output.Flush();
                                                        ctxt.Response.Close();
                                                    }
                                                }
                                                break;
                                                case "CreateDialog": {
                                                    Acount ac = JsonSerializer.Deserialize<Acount>(jsonstr);
                                                    using (Stream output = ctxt.Response.OutputStream) {
                                                        if (db.Autorize(ac.Login, ac.Password)) {
                                                            ReciveUser rc = JsonSerializer.Deserialize<ReciveUser>(jsonstr);
                                                            if (db.CreateNewChat(ac.Login, rc.LoginRcv)) {
                                                                ctxt.Response.StatusCode = 200;
                                                                output.Write(Encoding.UTF8.GetBytes("OK"));
                                                            }
                                                            else {
                                                                ctxt.Response.StatusCode = 419;
                                                                output.Write((Encoding.UTF8.GetBytes("Error")));
                                                            }
                                                        }
                                                        else {
                                                            ctxt.Response.StatusCode = 418;
                                                            output.Write((Encoding.UTF8.GetBytes("Login and password do not match")));
                                                        }
                                                    }
                                                }
                                                break;
                                                case "SendMessage": {
                                                    Acount ac = JsonSerializer.Deserialize<Acount>(jsonstr);
                                                    using (Stream output = ctxt.Response.OutputStream) {
                                                        if (db.Autorize(ac.Login, ac.Password)) {
                                                            ReciveUser rc = JsonSerializer.Deserialize<ReciveUser>(jsonstr);
                                                            ChatMsg msg = JsonSerializer.Deserialize<ChatMsg>(jsonstr);
                                                            if (db.SendMessage(ac.Login, rc.LoginRcv, msg.Message)) {
                                                                 wbscktController.BroadcastTo(rc.LoginRcv, msg.Message);
                                                                ctxt.Response.StatusCode = 200;
                                                                output.Write(Encoding.UTF8.GetBytes("OK"));
                                                            }
                                                            else {
                                                                ctxt.Response.StatusCode = 419;
                                                                output.Write((Encoding.UTF8.GetBytes("Error")));
                                                            }
                                                        }
                                                        else {
                                                            ctxt.Response.StatusCode = 418;
                                                            output.Write((Encoding.UTF8.GetBytes("Login and password do not match")));
                                                        }
                                                    }
                                                }
                                                break;
                                                case "GetDialog": {
                                                    Acount ac = JsonSerializer.Deserialize<Acount>(jsonstr);
                                                    using (Stream output = ctxt.Response.OutputStream) {
                                                        if (db.Autorize(ac.Login, ac.Password)) {
                                                            ReciveUser rc = JsonSerializer.Deserialize<ReciveUser>(jsonstr);
                                                            Dialog dlg = db.GetDialog(ac.Login, rc.LoginRcv);
                                                            ctxt.Response.StatusCode = 200;
                                                            output.Write(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dlg)));
                                                        }
                                                        else {
                                                            ctxt.Response.StatusCode = 418;
                                                            output.Write((Encoding.UTF8.GetBytes("Login and password do not match")));
                                                        }
                                                    }
                                                }
                                                break;
                                                case "GetDialogsList": {
                                                    Acount ac = JsonSerializer.Deserialize<Acount>(jsonstr);
                                                    using (Stream output = ctxt.Response.OutputStream) {
                                                        if (db.Autorize(ac.Login, ac.Password)) {
                                                            ReciveUser rc = JsonSerializer.Deserialize<ReciveUser>(jsonstr);
                                                            DialogsList dlg = db.GetDialogsList(ac.Login);
                                                            ctxt.Response.StatusCode = 200;
                                                            output.Write(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dlg)));
                                                        }
                                                        else {
                                                            ctxt.Response.StatusCode = 418;
                                                            output.Write((Encoding.UTF8.GetBytes("Login and password do not match")));
                                                        }
                                                    }
                                                }
                                                break;
                                                default:
                                                    ctxt.Response.StatusCode = 400;
                                                    ctxt.Response.Close();
                                                    Log("Ошибка: неправильный тип запроса");
                                                    break;
                                            }
                                        }
                                        catch (JsonException e) {
                                            Log("Ошибка  в формате тела запроса: " + e.Message);
                                            ctxt.Response.StatusCode = 400;
                                            ctxt.Response.Close();
                                        }
                                    }
                                    break;
                                    case "GET": {
                                        switch (ctxt.Request.QueryString["RequestData"]) {
                                            case "Avatar": {
                                                if (File.Exists("Avatars/" + ctxt.Request.QueryString["Login"] + ".png")) {
                                                    using (var stream = File.Open("Avatars/" + ctxt.Request.QueryString["Login"] + ".png", FileMode.Open)) {
                                                        using (Stream output = ctxt.Response.OutputStream) {
                                                            long i = 0;
                                                            byte[] buf = new byte[256];
                                                            while (i < stream.Length) {
                                                                long l = stream.Length - i > 256 ? 256 : (stream.Length - i);
                                                                stream.Read(buf, 0, (int)l);
                                                                output.Write(buf);
                                                                i += 256;
                                                            }
                                                            output.Flush();
                                                            ctxt.Response.Close();
                                                        }

                                                    }
                                                }
                                                else {
                                                    using (var stream = File.Open("Avatars/NoAvatar.png", FileMode.Open)) {
                                                        using (Stream output = ctxt.Response.OutputStream) {
                                                            long i = 0;
                                                            byte[] buf = new byte[256];
                                                            while (i < stream.Length) {
                                                                long l = stream.Length - i > 256 ? 256 : (stream.Length - i);
                                                                stream.Read(buf, 0, (int)l);
                                                                output.Write(buf);
                                                                i += 256;
                                                            }
                                                            output.Flush();
                                                            ctxt.Response.Close();
                                                        }
                                                    }
                                                }
                                            }
                                            break;
                                            default: {
                                                using (Stream output = ctxt.Response.OutputStream) {
                                                    output.Write((Encoding.UTF8.GetBytes("2323")));
                                                    output.Flush();
                                                    ctxt.Response.Close();
                                                }
                                            }
                                            break;
                                        }
                                     }
                                    break;
                                    default:
                                        Log("Ошибка: полученный запрос имеет неправильный HTTP метод");
                                          ctxt.Response.StatusCode = 400;
                                          ctxt.Response.Close();
                                    break;
                                }
                        //}break;
                    //    case "multipart/form-data": {
                    //
                     //   }break;

                     //   default:
                     //      Log("Ошибка в заголовке");
                     //       ctxt.Response.StatusCode = 400;
                     //       ctxt.Response.Close();
                     //       break;
                   // }
                }catch(Exception e) {
                    Log("Ошибка: " + e.Message);
                }
            }
        }
        public void Dispose(){
            db.Dispose();
            srv.Close();
        }
        ~HttpServer() {
            Dispose();
        }
    }   
}
