﻿using Crsch_3.Jsonstructs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Crsch_3 {
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
            }
            catch (Exception e) {
                Log("Ошибка при запуске сервера: " + e.Message);
                return false;
            }
            if (db.Start())
                Reciver();
            else return false;
            return true;
        }
        private async void Reciver()  {
            HttpListenerContext ctxt=null;
            while (true) {
                try {
                    ctxt = await srv.GetContextAsync();
                    switch (ctxt.Request.HttpMethod) {
                        case "POST": {
                            string[] ctype = ctxt.Request.Headers["Content-Type"].Split(';');
                            switch (ctype[0]) {
                                case "application/json":
                                    PostProcessor(ctxt);
                                    break;
                                case "multipart/form-data":
                                    ImageLoadProcessor(ctxt);
                                    break;
                                default:
                                    PostProcessor(ctxt);
                                    break;

                            }
                        }
                        break;
                        case "GET": {
                            GetProcessor(ctxt);
                        }
                        break;
                        default:
                            Log("Ошибка: полученный запрос имеет неправильный HTTP метод");
                            ctxt.Response.StatusCode = 400;
                            ctxt.Response.Close();
                            break;
                    }
                }
                catch (Exception e) {
                    try {
                        ctxt.Response.StatusCode = 400;
                        ctxt.Response.Close();
                    }
                    catch (ObjectDisposedException ex) { }
                    Log("Ошибка: " + e.Message);
                }
            }
        }
        private void ImageLoadProcessor(HttpListenerContext ctxt) {
            int len = int.Parse(ctxt.Request.Headers["Content-Length"]);
            Stream rd = ctxt.Request.InputStream;
            byte[] buf = new byte[len+256];
            int p= 0;
            int r = 1;
            while (r>0) {
               r= rd.Read(buf, p, 256);
               p += r;
            }
            string stringBuffer = Encoding.ASCII.GetString(buf);
            string[] splitString = stringBuffer.Split('\n');
            if (db.Autorize((splitString[3].Split('\r'))[0], (splitString[7].Split('\r'))[0])) {
               int pl = splitString[0].Length + splitString[1].Length + splitString[2].Length + splitString[3].Length + splitString[4].Length + splitString[5].Length
                + splitString[6].Length + splitString[7].Length + splitString[8].Length + splitString[9].Length + splitString[10].Length + splitString[11].Length + 12;
                byte[] buf2 = new byte[len - pl];
                Array.Copy(buf, pl, buf2, 0, len - pl);
                using (Stream wr = File.OpenWrite("Avatars/" + (splitString[3].Split('\r'))[0] + ".png")) {
                    wr.Write(buf2);
                }
                ctxt.Response.StatusCode = 200;
            }
            else {
                using (Stream output = ctxt.Response.OutputStream) {
                    ctxt.Response.StatusCode = 418;
                    output.Write((Encoding.UTF8.GetBytes("Login and password do not match")));
                }
            }
            ctxt.Response.Close();
        }
        private void PostProcessor(HttpListenerContext ctxt) {
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
                            if (db.Register(ac.Login.ToLower(), ac.Password)) {
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
                            if (db.Autorize(ac.Login.ToLower(), ac.Password)) {
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
                            if (db.Autorize(ac.Login.ToLower(), ac.Password)) {
                                ReciveUser rc = JsonSerializer.Deserialize<ReciveUser>(jsonstr);
                                if (db.CreateNewChat(ac.Login.ToLower(), rc.LoginRcv)) {
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
                            if (db.Autorize(ac.Login.ToLower(), ac.Password)) {
                                ReciveUser rc = JsonSerializer.Deserialize<ReciveUser>(jsonstr);
                                ChatMsg msg = JsonSerializer.Deserialize<ChatMsg>(jsonstr);
                                if (db.SendMessage(ac.Login.ToLower(), rc.LoginRcv, msg.Message)) {
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
                            if (db.Autorize(ac.Login.ToLower(), ac.Password)) {
                                ReciveUser rc = JsonSerializer.Deserialize<ReciveUser>(jsonstr);
                                Dialog dlg = db.GetDialog(ac.Login.ToLower(), rc.LoginRcv);
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
                            if (db.Autorize(ac.Login.ToLower(), ac.Password)) {
                                ReciveUser rc = JsonSerializer.Deserialize<ReciveUser>(jsonstr);
                                DialogsList dlg = db.GetDialogsList(ac.Login.ToLower());
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
                    case "UpdateUserInfo": {
                        Acount ac = JsonSerializer.Deserialize<Acount>(jsonstr);
                        using (Stream output = ctxt.Response.OutputStream) {
                            if (db.Autorize(ac.Login.ToLower(), ac.Password)) {
                                NewUserInfo nw = JsonSerializer.Deserialize<NewUserInfo>(jsonstr);
                                db.UpdateUserInfo(ac.Login.ToLower(),nw.NewFstName,nw.NewLstName,nw.NewPassword);
                            }
                            else {
                                ctxt.Response.StatusCode = 418;
                                output.Write((Encoding.UTF8.GetBytes("Login and password do not match")));
                            }
                        }
                    }break;
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
        private void GetProcessor(HttpListenerContext ctxt) {
             switch (ctxt.Request.QueryString["RequestData"]) {
                case "Avatar": {
                    if (File.Exists("Avatars/" + ctxt.Request.QueryString["Login"].ToLower() + ".png")) {
                        using (var stream = File.Open("Avatars/" + ctxt.Request.QueryString["Login"].ToLower() + ".png", FileMode.Open)) {
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
                case "UserInfo": {
                    using (Stream output = ctxt.Response.OutputStream) {
                        output.Write(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(db.GetUserInfo(ctxt.Request.QueryString["Login"].ToLower()))));
                        ctxt.Response.StatusCode = 200;
                        output.Flush();
                        ctxt.Response.Close();

                    }
                }break;
                case "SearchUser": {
                    using (Stream output = ctxt.Response.OutputStream) {
                        output.Write(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(db.FindUser(ctxt.Request.QueryString["Querry"]))));
                        ctxt.Response.StatusCode = 200;
                        output.Flush();
                        ctxt.Response.Close();
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
        public void Dispose(){
            db.Dispose();
            srv.Close();
        }
        ~HttpServer() {
            Dispose();
        }
    }   
}
