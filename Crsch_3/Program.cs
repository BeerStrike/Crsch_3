using System;
using System.IO;
using System.Net;
using System.Text;
namespace GloryToHoChiMin {
    class Program {
        static void Logger(string s){
            Console.WriteLine(s);
        }
        static void Main(string[] args) {
            HttpServer srv = new HttpServer(8889,8888, "195.19.114.66", 3306, "Che", "root", "VivaLaRevolution");
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
    }
}
