using System.Xml;
using System.IO;
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
            XmlElement method = resDoc.CreateElement("method");
            method.InnerText = " startLogin ";
            //I sort of just put in what looked like to be response parameters
            string[] paramLst = new string[] { "ServerUUID", "gamename", "username", "StartBombdServerSwitch" };
            string[] valueLst = new string[] { "00000000-0000-0000-0000-000000000000", "testgame", "test", "False" };
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

        public static bool DefaultHandler(XmlDocument recDoc, XmlDocument resDoc)
        {
            XmlElement method = resDoc.CreateElement("method");
            method.InnerText = "  ";
            XmlElement error = resDoc.CreateElement("error");
            error.InnerText = " NO HANDLER FOR METHOD ";
            method.AppendChild(error);
            resDoc.ChildNodes[0].ChildNodes[0].AppendChild(method);
            return true;
        }
    }
}
