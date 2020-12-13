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
            LoadSchemas();
            if (!File.Exists("database.sqlite"))
            {
                DatabaseManager.performDBUpgrade();
            }
            if (!Directory.Exists("creations"))
            {
                Directory.CreateDirectory("creations");
            }
            //Set up server threads
            Thread ms = new Thread(() => MainServer("http://" + ip + ":" + port.ToString() + "/"));
            Thread ss = new Thread(() => SessionServer(IPAddress.Any, matchingPort, "output.pfx", "1234"));
            ms.Start();
            ss.Start();
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
                //Get listener context and pass to a new thread
                HttpListenerContext context = listener.GetContext();
                Console.WriteLine("New request!");
                new Thread(() => Processors.MainServerProcessor(context)).Start();
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
                //Get TCPClient and pass to a new thread
                TcpClient client = listener.AcceptTcpClient();
                new Thread(() => Processors.SessionServerProcessor(client, serverCertificate)).Start();
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
    }
}
