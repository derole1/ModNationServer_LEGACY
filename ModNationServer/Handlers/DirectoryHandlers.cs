using System;
using System.Xml;
using System.IO;
using System.Text;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Net.Security;
using System.Linq;

namespace ModNationServer
{
    //Handles various directory server specific packets
    static class DirectoryHandlers
    {
        public static void StartConnectHandler(string service, SslStream ssl, XmlDocument recDoc, XmlDocument resDoc)
        {
            XmlElement method = resDoc.CreateElement("method");
            method.InnerText = " startConnect ";
            SetParamList(resDoc, method, new string[] { "bombd_version", "bombd_builddate", "bombd_OS", "serveruuid", "username", "userid" }
                , new string[] { "0.0", "2020-01-01 11:00:00", "0", Program.config["cluster_uuid"], "test", "1" });
            //For direct connection
            //SetParamList(resDoc, method, new string[] { "gameserver", "listenIP", "listenPort", "hashSalt", "sessionId" }
            //       , new string[] { "directGameServer", "127.0.0.1", "10501", "0", "0" });
            resDoc.ChildNodes[0].ChildNodes[0].AppendChild(method);
            WriteData(ssl, resDoc);
        }

        public static void TimeSyncRequestHandler(string service, SslStream ssl, XmlDocument recDoc, XmlDocument resDoc)
        {
            //TODO: Game falls quiet after this for matchmaking, gamemanager and gamebrowser. Need to figure out why (possibly requires directGameServer response in startConnect)
            XmlElement method = resDoc.CreateElement("method");
            method.InnerText = " timeSyncRequest ";
            SetParamList(resDoc, method, new string[] { "serverTime" }, new string[] { Math.Floor((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString() });
            resDoc.ChildNodes[0].ChildNodes[0].AppendChild(method);
            WriteData(ssl, resDoc);
        }

        static int count = 8;

        public static void GetServiceListHandler(string service, SslStream ssl, XmlDocument recDoc, XmlDocument resDoc)
        {
            XmlElement method = resDoc.CreateElement("method");
            method.InnerText = " getServiceList ";
            BinaryReader br = new BinaryReader(new byte[0]);

            //Services
            string[] services = new string[] { "matchmaking", "gamemanager", "gamebrowser", "textcomm", "playgroup" };
            string[] serviceProtocols = new string[] { "tcp", "tcp", "tcp", "tcp", "rudp" };

            //TODO: Update config to have these stored there
            //TODO: How to terminate the list (for now we just add a bunch of padding to the end to prevent an overflow)
            for (int i = 0; i < services.Length; i++)
            {
                br.pushInt32(services[i].Length + 1);
                br.pushString(services[i], Encoding.ASCII);

                br.pushInt32(1);    //Service status (Data below MUST be skipped if 0)

                br.pushInt32(Program.config["directory_ip"].Length + 1);
                br.pushString(Program.config["directory_ip"], Encoding.ASCII);
                br.pushInt32(0);    //Length for something???
                br.pushInt32(("1050" + (i + 2).ToString()).Length + 1);
                br.pushString("1050" + (i + 2).ToString(), Encoding.ASCII);
                br.pushInt32(serviceProtocols[i].Length + 1);
                br.pushString(serviceProtocols[i], Encoding.ASCII);
                br.pushInt32(1);    //??? (Does nothing it seems)
                br.pushInt32(i);    //SessionKey
            }

            br.pushArray(new byte[256]);
            Console.WriteLine("Response: {0}", Encoding.ASCII.GetString(br.getRes()));
            SetParamList(resDoc, method, new string[] { "CluseterUUID", "servicesList" }
                , new string[] { Program.config["cluster_uuid"], Convert.ToBase64String(br.getRes()) });
            resDoc.ChildNodes[0].ChildNodes[0].AppendChild(method);
            WriteData(ssl, resDoc);
        }

        public static void DefaultHandler(string service, SslStream ssl, XmlDocument recDoc, XmlDocument resDoc)
        {
            Console.WriteLine("Unknown method");
            XmlElement method = resDoc.CreateElement("method");
            method.InnerText = " netcodeCommand ";
            XmlElement error = resDoc.CreateElement("error");
            error.InnerText = " noDirectoryInfoAvailable ";
            method.AppendChild(error);
            resDoc.ChildNodes[0].ChildNodes[0].AppendChild(method);
            WriteData(ssl, resDoc);
        }

        //Encode the param\value pair into XML
        static void SetParamList(XmlDocument resDoc, XmlElement method, string[] paramLst, string[] valueLst)
        {
            for (int i = 0; i < paramLst.Length; i++)
            {
                XmlElement param = resDoc.CreateElement("param");
                XmlElement name = resDoc.CreateElement("name");
                name.InnerText = " " + paramLst[i] + " ";
                param.AppendChild(name);
                XmlElement value = resDoc.CreateElement("value");
                value.InnerText = " " + valueLst[i] + " ";
                param.AppendChild(value);
                method.AppendChild(param);
            }
        }

        static byte[] ReadData(SslStream ssl)
        {
            //Need to be able to read stream multiple times to get all data
            byte[] buffer = new byte[0];
            int bytesExpected = 0;
            int bytesRead = 0;
            int count = 0;
            do
            {
                byte[] tempBuf = new byte[1024];
                int bytes = ssl.Read(tempBuf, 0, tempBuf.Length);
                buffer = buffer.Concat(tempBuf.Take(bytes).ToArray()).ToArray();
                bytesRead += bytes;
                if (bytesExpected == 0)
                {
                    bytesExpected = BitConverter.ToInt32(tempBuf.Take(4).Reverse().ToArray(), 0);
                    Console.WriteLine("Expected {0}", bytesExpected);
                }
                //if (bytes < 1024) { break; }
                count++;
            } while (bytesRead < bytesExpected);
            count++;
            return buffer;
        }

        static void WriteData(SslStream ssl, XmlDocument resDoc)
        {
            byte[] data = Encoding.UTF8.GetBytes(resDoc.InnerXml);
            data = data.Concat(new byte[] { 0x00 }).ToArray();  //Add null terminator to end of xml data
            BinaryReader br = new BinaryReader(new byte[0]);
            br.pushInt32(data.Length + 20);  //Length of xml data
            br.pushArray(new byte[16]); //Padding
            br.pushUInt32(0x64FEFFFF);  //??? (Seems to be same for every packet in packet logs)
            byte[] buffer = br.getRes();
            buffer = buffer.Concat(data.ToArray()).ToArray();
            int bytesRead = 0;
            do
            {
                int toWrite = Math.Min(buffer.Length - bytesRead, 1024);
                ssl.Write(buffer, bytesRead, toWrite);
                bytesRead += toWrite;
            } while (bytesRead < buffer.Length);
            //Console.WriteLine("Done!");
        }
    }
}
