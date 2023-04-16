using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace GloryToHoChiMin {
    class DatabaseConnector : IDisposable {
        private MySqlConnection cnct;
        public delegate void Log(string s);
        public event Log ErrorLog;
        public DatabaseConnector(string host, int port, string database, string username, string password){ 
         cnct = new MySqlConnection("Server=" + host + ";Database=" + database + ";port=" + port + ";User Id=" + username + ";password=" + password);
        }
        public bool Start() {
            try {
                cnct.Open();
            }catch(MySqlException e) {
                ErrorLog(e.Message);
                return false;
            }
            return true;
        }
        public bool Register(string Login,string Password) {
            MySqlCommand command = new MySqlCommand("SELECT Login FROM Accounts WHERE Login = '"+Login+"';", cnct);
            if (command.ExecuteScalar() != null)
                return false;
            else
            {
                command = new MySqlCommand("INSERT INTO Accounts(Login, Password) values('" + Login + "', '" + Password + "');", cnct);
                command.ExecuteNonQuery();
                command = new MySqlCommand("CREATE TABLE "+Login+"Contacts (Contact VARCHAR(50));", cnct);
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
        public bool CreateNewChat(string Login1, string Login2) {
            MySqlCommand command = new MySqlCommand("SELECT Login FROM Accounts WHERE Login = '" + Login1 + "';", cnct);
            if (command.ExecuteScalar() != null) {
                command = new MySqlCommand("SELECT Login FROM Accounts WHERE Login = '" + Login2 + "';", cnct);
                if (command.ExecuteScalar() != null) {
                    if (String.Compare(Login1, Login2) > 0) {
                        command = new MySqlCommand("CREATE TABLE Dialog" + Login1 + "And" + Login2 + "(id INT AUTO_INCREMENT PRIMARY KEY,Login VARCHAR(50),Message Varchar(500);", cnct);
                        command.ExecuteNonQuery();
                        command = new MySqlCommand("INSERT INTO "+Login1+"Contacts (Contact) values('"+ Login2 + "');", cnct);
                        command.ExecuteNonQuery();
                        command = new MySqlCommand("INSERT INTO " + Login2 + "Contacts (Contact) values('" + Login1 + "');", cnct);
                        command.ExecuteNonQuery();
                    }
                    else {
                        command = new MySqlCommand("CREATE TABLE Dialog" + Login2 + "And" + Login1 + "(id INT AUTO_INCREMENT PRIMARY KEY,Login VARCHAR(50),Message Varchar(500);", cnct);
                        command.ExecuteNonQuery();
                        command = new MySqlCommand("INSERT INTO " + Login1 + "Contacts (Contact) values('" + Login2 + "');", cnct);
                        command.ExecuteNonQuery();
                        command = new MySqlCommand("INSERT INTO " + Login2 + "Contacts (Contact) values('" + Login1 + "');", cnct);
                        command.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            return false;
        }
        public void Dispose(){
            cnct.Close();
        }
        ~DatabaseConnector(){
            Dispose();
        }
    }
}
