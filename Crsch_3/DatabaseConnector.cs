using Crsch_3.Jsonstructs;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Crsch_3 {
    class DatabaseConnector : IDisposable {
        private MySqlConnection cnct;
        public delegate void Log(string s);
        public event Log ErrorLog;
        public DatabaseConnector(string host, int port, string database, string username, string password){ 
         cnct = new MySqlConnection("Server=" + host + ";Database=" + database + ";port=" + port + ";User Id=" + username + ";password=" + password+";charset = utf8;");
        }
        public bool Start() {
            try {
                cnct.Open();
                //cnct.StateChange += Reconnect;
            }catch(MySqlException e) {
                ErrorLog(e.Message);
                return false;
            }
            return true;
        }

     //   private void Reconnect(object sender, StateChangeEventArgs e) {
          //  cnct.Open();
        //}
        public bool Register(string Login,string Password) {
            MySqlCommand command = new MySqlCommand("SELECT Login FROM Accounts WHERE Login = '"+Login+"';", cnct);
            if (command.ExecuteScalar() != null)
                return false;
            else
            {
                command = new MySqlCommand("INSERT INTO Accounts(Login, Password) values('" + Login + "', '" + Password + "');", cnct);
                command.ExecuteNonQuery();
                command = new MySqlCommand("INSERT INTO UsersInfo(Login, FstName,LstName) values('" + Login + "', '" + Login + "', '');", cnct);
                command.ExecuteNonQuery();
                command = new MySqlCommand("CREATE TABLE "+Login+"Contacts (Contact VARCHAR(50),Unread INT);", cnct);
                command.ExecuteNonQuery();
                return true;
            }
        }
        public bool Autorize(string Login, string Password) {
            MySqlCommand command = new MySqlCommand("SELECT Password FROM Accounts WHERE Login = '" + Login + "';", cnct);
            if ((string)(command.ExecuteScalar())==Password)
                return true;
            else
                return false;
        }
        public bool UpdateUserInfo(string Login, string NewFstName,string NewLstName,string NewPass) {
            MySqlCommand command = new MySqlCommand("UPDATE Accounts SET Password='"+NewPass+"' WHERE Login='"+Login+"';", cnct);
            command.ExecuteNonQuery();
            command = new MySqlCommand("UPDATE UsersInfo SET FstName='" + NewFstName + "' WHERE Login='" + Login + "';", cnct);
            command.ExecuteNonQuery();
            command = new MySqlCommand("UPDATE UsersInfo SET LstName='" + NewLstName + "' WHERE Login='" + Login + "';", cnct);
            command.ExecuteNonQuery();
            return true;
        }
        public UserInfo GetUserInfo(string Login) {
            MySqlCommand command = new MySqlCommand("SELECT * FROM UsersInfo WHERE Login='"+Login+"';", cnct);
            UserInfo inf = new UserInfo();
            using (MySqlDataReader r = command.ExecuteReader()) {
                r.Read();
                inf.FirstName = r.GetString(1);
                inf.LastName = r.GetString(2);
            }
            return inf;
        }
        public bool CreateNewChat(string Login1, string Login2) {
            MySqlCommand command = new MySqlCommand("SELECT Login FROM Accounts WHERE Login = '" + Login1 + "';", cnct);
            if (command.ExecuteScalar() != null) {
                command = new MySqlCommand("SELECT Login FROM Accounts WHERE Login = '" + Login2 + "';", cnct);
                if (command.ExecuteScalar() != null) {
                    if (String.Compare(Login1, Login2) > 0) {
                        command = new MySqlCommand("CREATE TABLE Dialog" + Login1 + "And" + Login2 + "(Date VARCHAR(20),Login VARCHAR(50),Message Varchar(500));", cnct);
                        command.ExecuteNonQuery();
                        command = new MySqlCommand("INSERT INTO "+Login1+ "Contacts (Contact,Unread) values('" + Login2 + "',0);", cnct);
                        command.ExecuteNonQuery();
                        command = new MySqlCommand("INSERT INTO " + Login2 + "Contacts (Contact,Unread) values('" + Login1 + "',0);", cnct);
                        command.ExecuteNonQuery();
                    }
                    else {
                        command = new MySqlCommand("CREATE TABLE Dialog" + Login2 + "And" + Login1 + "(Date VARCHAR(20),Login VARCHAR(50),Message Varchar(500));", cnct);
                        command.ExecuteNonQuery();
                        command = new MySqlCommand("INSERT INTO " + Login1 + "Contacts (Contact,Unread) values('" + Login2 + "',0);", cnct);
                        command.ExecuteNonQuery();
                        command = new MySqlCommand("INSERT INTO " + Login2 + "Contacts (Contact,Unread) values('" + Login1 + "',0);", cnct);
                        command.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            return false;
        }
        public bool SendMessage(string from,string to,string messadge) {
            MySqlCommand command;
            if (String.Compare(from, to) > 0) 
                command = new MySqlCommand("INSERT INTO Dialog" + from + "And"+to+ " (Date,Login,Message) values('" + DateTime.Now + "','" + from + "','" + messadge + "');", cnct);
            else 
                command = new MySqlCommand("INSERT INTO Dialog" + to + "And" + from + " (Date,Login,Message) values('" + DateTime.Now + "','" + from + "','" + messadge + "');", cnct);
            command.ExecuteNonQuery();
            command = new MySqlCommand("UPDATE "+to + "Contacts SET Unread = Unread+1 WHERE Contact='"+from+"';",cnct);
            command.ExecuteNonQuery();
            return true;
        }
        public Dialog GetDialog(string Login1, string Login2) {
            MySqlCommand command;
            Dialog dlg = new Dialog();
            dlg.Messages = new List< Message>();
            if (String.Compare(Login1, Login2) > 0)
                command = new MySqlCommand("SELECT * FROM Dialog" + Login1 + "And" + Login2 + ";", cnct);
            else
                command = new MySqlCommand("SELECT * FROM Dialog" + Login2 + "And" + Login1 + ";", cnct);

            using (MySqlDataReader r = command.ExecuteReader()) {
                while (r.Read()) {
                    Message msg = new Message();
                    msg.Date = (string)r.GetValue(0);
                    msg.Login=(string)r.GetValue(1);
                    msg.MessageText=(string)r.GetValue(2);
                    dlg.Messages.Add(msg);
                }
            }
          List<Message> rw = new List<Message>();
            for (int i = dlg.Messages.Count - 1; i >= 0; i--)
                rw.Add(dlg.Messages[i]);
            dlg.Messages = rw;
            command = new MySqlCommand("UPDATE "+Login1+"Contacts SET Unread = 0 WHERE Contact='"+Login2+"';",cnct);
            command.ExecuteNonQuery();
            return dlg;
        }
        public DialogsList GetDialogsList(string Login) {
            DialogsList dl = new DialogsList();
            dl.dlgs = new List<DialogListElm>();
            MySqlCommand command = new MySqlCommand("SELECT * FROM " + Login  +"Contacts;", cnct);
            using (MySqlDataReader r = command.ExecuteReader()) {
                while (r.Read()) {
                    DialogListElm elm = new DialogListElm();
                    elm.Login=(string)r.GetValue(0);
                    elm.Unread=(int)r.GetValue(1);
                    dl.dlgs.Add(elm);
                }
            }
            for(int i=0;i<dl.dlgs.Count; i++) {
                command = new MySqlCommand("SELECT * FROM UsersInfo WHERE Login='" + dl.dlgs[i].Login + "';", cnct);
                using (MySqlDataReader r = command.ExecuteReader()) {
                    r.Read();
                    DialogListElm elm = dl.dlgs[i];
                    elm.FirstName = r.GetString(1);
                    elm.LastName = r.GetString(2);
                    dl.dlgs[i] = elm;
                }
            }
            return dl;
        }
        public void Dispose(){
            cnct.Close();
        }
        ~DatabaseConnector(){
            Dispose();
        }
    }
}
