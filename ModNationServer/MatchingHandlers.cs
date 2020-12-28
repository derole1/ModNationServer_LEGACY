using System;
using System.Xml;
using System.IO;
using System.Text;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;

namespace ModNationServer
{
    //Handles various directory server specific packets
    static class MatchingHandlers
    {
        public static bool StartConnectHandler(XmlDocument recDoc, XmlDocument resDoc)
        {
            //((XmlElement)resDoc.ChildNodes[0]).SetAttribute("name", "login");
            XmlElement method = resDoc.CreateElement("method");
            method.InnerText = " netcodeCommand ";
            string[] paramLst = new string[] { "bombd_version", "bombd_builddate", "bombd_OS", "serveruuid", "username", "userid", "serverTime" };
            string[] valueLst = new string[] { "0.0", "2020-01-01 11:00:00", "0", "00000000-0000-0000-0000-000000000000", "test", "1", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
            for (int i=0; i<paramLst.Length; i++)
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
            resDoc.ChildNodes[0].ChildNodes[0].AppendChild(method);
            return true;
        }

        public static bool TimeSyncRequestHandler(XmlDocument recDoc, XmlDocument resDoc)
        {
            XmlElement method = resDoc.CreateElement("method");
            method.InnerText = " netcodeCommand ";
            string[] paramLst = new string[] { "serverTime" };
            string[] valueLst = new string[] { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
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
            resDoc.ChildNodes[0].ChildNodes[0].AppendChild(method);
            return true;
        }

        public static bool GetServiceListHandler(XmlDocument recDoc, XmlDocument resDoc)
        {
            //System.Threading.Thread.Sleep(59000);
            XmlElement method = resDoc.CreateElement("method");
            method.InnerText = " netcodeCommand ";
            string[] paramLst = new string[] { "CluseterUUID", "servicesList", "serverTime" };
            //TODO: Figure out the binary protocol for services list
            BinaryReader br = new BinaryReader(new byte[0]);
            br.pushInt32(0);
            br.pushString("gamebrowser=TCP 127.0.0.1:10501", Encoding.ASCII);
            //br.pushUInt32(0x7F000001);
            //br.pushUInt16(10501);
            //br.pushInt16(1);
            //br.pushString("127.0.0.1:10501", Encoding.ASCII);
            //br.pushArray(System.Net.IPAddress.Parse("127.0.0.1").GetAddressBytes());
            //br.pushUInt16(10501);
            //br.pushString("matchmaking", Encoding.ASCII);
            //br.pushArray(System.Net.IPAddress.Parse("127.0.0.1").GetAddressBytes());
            //br.pushUInt16(10501);
            //br.pushString("textcomm", Encoding.ASCII);
            //br.pushArray(System.Net.IPAddress.Parse("127.0.0.1").GetAddressBytes());
            //br.pushUInt16(10501);
            //br.pushString("login", Encoding.ASCII);
            //br.pushArray(System.Net.IPAddress.Parse("127.0.0.1").GetAddressBytes());
            //br.pushUInt16(10501);
            br.insertUInt32((uint)br.getResLen() - 4, 0);
            br.pushArray(new byte[255]);
            Console.WriteLine("Response: {0}", Encoding.ASCII.GetString(br.getRes()));
            string[] valueLst = new string[] { DirectorySessionManager.RandomSessionUUID(), Convert.ToBase64String(br.getRes()), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
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
            //XmlElement error = resDoc.CreateElement("error");
            //error.InnerText = " noDirectoryInfoAvailable ";
            //method.AppendChild(error);
            resDoc.ChildNodes[0].ChildNodes[0].AppendChild(method);
            return true;
        }

        public static bool DefaultHandler(XmlDocument recDoc, XmlDocument resDoc)
        {
            XmlElement method = resDoc.CreateElement("method");
            method.InnerText = " netcodeCommand ";
            XmlElement error = resDoc.CreateElement("error");
            error.InnerText = " noDirectoryInfoAvailable ";
            method.AppendChild(error);
            resDoc.ChildNodes[0].ChildNodes[0].AppendChild(method);
            return true;
        }
    }
}
