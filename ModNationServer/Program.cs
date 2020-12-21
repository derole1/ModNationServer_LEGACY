using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Data.SQLite;

namespace ModNationServer
{
    class Program
    {
        /*
         * ModNation server by derole
         * 
         * I apologise in advance for any messy code, this is far from winning any awards
         * 
         * This server is in no way affiliated with sony, UFG, or san diego studios
         * 
         * List of things that need doing:
         * - Matching responses
         * - Creation search fixing (Currently complains about disconnecting from playerconnect even though no request is sent?)
         * - Fix multipart form decoder from bugging out with large data
         * - Finish mail system
         * - Finish profiles etc
         * - Database stuff barely works at all so needs to be properly implemented at some point
         */

        public static string ip = "127.0.0.1";
        public static int port = 10050;
        public static int matchingPort = 10501;

        static void Main(string[] args)
        {
            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "-upgradedb":
                        DatabaseManager.performDBUpgrade();
                        break;
                }
            }
            LoadSchemas();
            ServerValues.Init();
            if (!File.Exists("database.sqlite"))
            {
                DatabaseManager.performDBUpgrade();
            }
            //if (!Directory.Exists("creations"))
            //{
            //    Directory.CreateDirectory("creations");
            //}
            //Set up server threads
            //Thread ms = new Thread(() => MainServer("http://" + ip + ":" + port.ToString() + "/"));
            Thread ms = new Thread(() => MainServer("http://*:" + port.ToString() + "/"));
            Thread ss = new Thread(() => SessionServer(IPAddress.Any, matchingPort, "output.pfx", "1234"));
            ms.Start();
            ss.Start();
            //Start up statistic thread (To update downloads/views this/last week etc)
            Thread st = new Thread(() => StaticticThread());
            st.Start();
            //Listen for console commands
            while (true)
            {
                string[] command = Console.ReadLine().Split(' ');
                switch (command[0])
                {
                    case "reloadschemas":
                        Processors.xmlSchemas.Clear();
                        LoadSchemas();
                        break;
                }
            }
        }

        static void MainServer(string domain)
        {
            //Initialize a HTTPListener (Requires admin)
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(domain);
            Console.WriteLine("Server URL: {0}", domain);
            listener.Start();
            Console.WriteLine("Listening...");
            while (true)
            {
                try
                {
                    //Get listener context and pass to a new thread
                    HttpListenerContext context = listener.GetContext();
                    Console.WriteLine("New request!");
                    new Thread(() => Processors.MainServerProcessor(context)).Start();
                } catch { }
            }
        }

        static void SessionServer(IPAddress ip, int port, string certPath, string certPass)
        {
            //Load the certificate to use
            X509Certificate2 serverCertificate = new X509Certificate2(certPath, certPass);
            //Set up a TCPListener
            TcpListener listener = new TcpListener(ip, port);
            listener.Start();
            Console.WriteLine("Session server listening on {0}:{1}", ip, port);
            while (true)
            {
                try {
                    //Get TCPClient and pass to a new thread
                    TcpClient client = listener.AcceptTcpClient();
                    new Thread(() => Processors.SessionServerProcessor(client, serverCertificate)).Start();
                } catch { }
            }
        }

        //Caches schemas into memory
        static void LoadSchemas()
        {
            Console.WriteLine("Loading schemas");
            foreach (string file in Directory.GetFiles("resources"))
            {
                Console.WriteLine("Loaded {0}", "resources\\" + Path.GetFileName(file));
                Processors.xmlSchemas.Add("resources\\" + Path.GetFileName(file), File.ReadAllBytes(file));
            }
        }

        //Checks and updates statistics
        static void StaticticThread()
        {
            DayOfWeek lastCheckDay = DateTime.Now.DayOfWeek;
            while (true)
            {
                //Sleep for an hour
                Thread.Sleep(3600000);
                Console.WriteLine("Statistics checkup!");
                if (DateTime.Now.DayOfWeek == DayOfWeek.Monday && lastCheckDay == DayOfWeek.Sunday)
                {
                    SQLiteConnection sqlite_conn = new SQLiteConnection(DatabaseManager.connectionString);
                    sqlite_conn.Open();
                    SQLiteCommand sqlite_cmd = sqlite_conn.CreateCommand();
                    sqlite_cmd.CommandText = "UPDATE Player_Creations SET downloads_last_week=downloads_this_week, downloads_this_week=0, views_last_week=views_this_week, views_this_week=0;";
                    sqlite_cmd.ExecuteNonQuery();
                    sqlite_conn.Close();
                }
                lastCheckDay = DateTime.Now.DayOfWeek;
            }
        }
    }
}
