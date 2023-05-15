using System;
using System.IO;
using System.Net;
using System.Text;
namespace Crsch_3 {
    class Program {
        static void Logger(string s){
            Console.WriteLine(s);
        }
        static void Main(string[] args) {
            if (File.Exists("configuration.conf")) {
                using (StreamReader rd = File.OpenText("configuration.conf")) {
                    int port = Int32.Parse(rd.ReadLine());
                    string dbip = rd.ReadLine();
                    int dbport = Int32.Parse(rd.ReadLine());
                    string dbname = rd.ReadLine();
                    string dblgn = rd.ReadLine();
                    string dbpass = rd.ReadLine();
                    HttpServer srv;
                    while (true) {
                        try {
                            srv = new HttpServer(port, dbip, dbport, dbname, dblgn, dbpass);
                            srv.Log += Logger;
                            if (srv.Start()) {
                                Console.WriteLine("Сервер запущен");
                                while (true) {
                                    string command = Console.ReadLine();
                                    if (command == "stop") {
                                        srv.Log -= Logger;
                                        srv.Dispose();
                                        break;
                                    }
                                    else Console.WriteLine("Неверная комманда");
                                }
                            }
                        }
                        catch (Exception e) {
                            Console.WriteLine("Критическая ошибка: " + e.Message);
                            Console.WriteLine("Сервер перезапускается");
                        }
                    }
                }
            }
            else {
                Console.WriteLine("отсудствует файл конфигурвции сервера.");
            }
        }
    }
}
