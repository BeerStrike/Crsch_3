using GloryToHoChiMin.Jsonstructs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;

namespace GloryToHoChiMin {
    class HttpServer:IDisposable{
        private HttpListener srv;
        private DatabaseConnector db;
        public delegate void Logs(string s);
        public event Logs Log;
        public HttpServer(int port,string dbadr,int dbport,string dbnme,string dblogin,string dbpass)   {
          srv= new HttpListener();
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
                    switch (ctxt.Request.HttpMethod) {
                        case "POST": {
                            try {
                                string jsonstr;
                                using (var reader = new StreamReader(ctxt.Request.InputStream, ctxt.Request.ContentEncoding)) {
                                    jsonstr = reader.ReadToEnd();
                                }
                                Reqtype rt = JsonSerializer.Deserialize<Reqtype>(jsonstr);
                                switch  (rt.RequestType) {
                                    case "Registration": {
                                        Acount ac = JsonSerializer.Deserialize<Acount>(jsonstr);
                                        using (Stream output = ctxt.Response.OutputStream) {
                                            if (db.Register(ac.Login, ac.Password)) {
                                                ctxt.Response.StatusCode = 200;
                                                output.Write((Encoding.UTF8.GetBytes("Sucsefull")));
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
                                                output.Write((Encoding.UTF8.GetBytes("Sucsefull")));
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
                                    
                                    default:
                                        ctxt.Response.StatusCode = 400;
                                        ctxt.Response.Close();
                                        Log("Ошибка: неправильный тип запроса");
                                        break;
                                }
                            } catch (JsonException e) {
                                Log("Ошибка  в формате тела запроса: " + e.Message);
                                ctxt.Response.StatusCode = 400;
                                ctxt.Response.Close();
                            }
                        }
                        break;
                        case "GET": {
                            using (Stream output = ctxt.Response.OutputStream) {
                                output.Write((Encoding.UTF8.GetBytes("2323")));
                                output.Flush();
                                ctxt.Response.Close();
                            }
                        } break;
                        default:
                            Log("Ошибка: полученный запрос имеет неправильный HTTP метод");
                            break;
                    }
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
