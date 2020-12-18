using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Web;
using System.Xml;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Data.SQLite;
using HttpMultipartParser;

namespace ModNationServer
{
    static class Processors
    {
        //List of xml schema files so were not constantly reading from disk for each request
        public static Dictionary<string,  byte[]> xmlSchemas = new Dictionary<string, byte[]>();

        public static void MainServerProcessor(HttpListenerContext context)
        {
            //Sets up various things for http request and response
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            System.IO.Stream input = request.InputStream;
            System.IO.Stream output = response.OutputStream;
            //Reads the http data
            byte[] recvBuffer = new byte[request.ContentLength64];
            if (request.ContentLength64 > 0)
            {
                int recvBytes = 0;
                MemoryStream ms = new MemoryStream();
                do
                {
                    byte[] tempBuf = new byte[20000];
                    int bytes = input.Read(tempBuf, 0, tempBuf.Length);
                    ms.Write(tempBuf, 0, bytes);
                    recvBytes += bytes;
                } while (recvBytes < request.ContentLength64);
                recvBuffer = ms.ToArray();
            }
            byte[] buffer = recvBuffer;
            //Creates receiving and response xml documents
            XmlDocument recDoc = new XmlDocument();
            XmlDocument resDoc = new XmlDocument();
            //Sets up response with proper values
            XmlElement result = AppendCommon(resDoc);
            resDoc.AppendChild(result);
            //if (request.HttpMethod == "POST")
            //{
            //    recDoc.LoadXml(Encoding.UTF8.GetString(recvBuffer));
            //}
            Console.WriteLine("Request URL: {0}", request.RawUrl.Split('?')[0]);
            Dictionary<string, string> urlEncodedData = new Dictionary<string, string>();
            //Decode url encoding if urlencoded data is sent
            if (request.ContentType == "application/x-www-form-urlencoded")
            {
                DecodeURLEncoding(Encoding.UTF8.GetString(recvBuffer), urlEncodedData);
            }
            //Open a database connection (TODO: Put into DatabaseManager?)
            SQLiteConnection sqlite_conn = new SQLiteConnection("Data Source=database.sqlite;Version=3;");
            sqlite_conn.Open();
            SQLiteCommand sqlite_cmd = sqlite_conn.CreateCommand();
            bool respond = false;
            bool isXml = true;
            string[] url = request.RawUrl.Substring(1, request.RawUrl.Length - 1).Split('/');
            //Decide if the server wants resources, player creations or wants to access database stuff
            switch (url[0])
            {
                case "resources":
                    respond = true;
                    isXml = false;
                    if (File.Exists(string.Join("\\", url)))
                    {
                        response.ContentType = "application/xml; charset=utf-8";
                        buffer = xmlSchemas[string.Join("\\", url)];
                    }
                    break;
                case "player_avatars":
                    Console.WriteLine("AVATAR REQUEST!!!");
                    isXml = false;
                    break;
                case "player_creations":
                    Console.WriteLine("CREATIION REQUEST!!!");
                    respond = true;
                    isXml = false;
                    if (File.Exists(string.Join("\\", url)))
                    {
                        //Decide what the content is and set the mime type accordingly
                        if (Path.GetExtension(string.Join("\\", url)) == ".png")
                        {
                            response.ContentType = "image/png";
                        }
                        else
                        {
                            response.ContentType = "application/octet-stream";
                        }
                        buffer = File.ReadAllBytes(string.Join("\\", url));
                    }
                    break;
                default:
                    //This probably isnt needed, but was just put in for debugging purposes
                    response.AddHeader("X-Rack-Cache", "pass");
                    response.AddHeader("X-Runtime", "18");
                    response.AddHeader("Last-Modified", "Thu, 31 Dec 2037 23:55:55 GMT");
                    response.AddHeader("Expires", "Thu, 31 Dec 2037 23:55:55 GMT");
                    response.AddHeader("Cache-Control", "max-age=315360000");
                    //
                    try
                    {
                        //response.SetCookie(new Cookie("playerconnect_session_id", request.Cookies["playerconnect_session_id"].Value));
                    } catch { }
                    //response.SetCookie(new Cookie("path", "/"));
                    break;
            }
            if (isXml)
            {
                //response.ContentType = "text/xml; charset=utf-8";
                //Game requires the "charset=utf-8" part to properly decode some xml responses
                response.ContentType = "application/xml; charset=utf-8";
                int paramStart = request.RawUrl.IndexOf('?') + 1;
                //BIG switch statement that decides what handler the data goes to
                switch (url[0].Split('?')[0])
                {
                    case "preferences.xml":
                        respond = Handlers.PreferencesUpdateHandler(request, response, urlEncodedData, resDoc);
                        break;
                    case "policy.view.xml":
                        DecodeURLEncoding(request.RawUrl.Substring(paramStart, request.RawUrl.Length - paramStart), urlEncodedData);
                        respond = Handlers.PolicyViewHandler(request, response, urlEncodedData, resDoc);
                        break;
                    case "policy.accept.xml":
                        respond = Handlers.PolicyAcceptHandler(request, response, urlEncodedData, resDoc);
                        break;
                    case "session.login_np.xml":
                        respond = Handlers.SessionLoginHandler(request, response, urlEncodedData, resDoc, null);
                        break;
                    case "session.ping.xml":
                        respond = Handlers.SessionPingHandler(request, response, urlEncodedData, resDoc);
                        break;
                    case "profanity_filter.list.xml":
                        respond = Handlers.ProfanityFilterListHandler(request, response, urlEncodedData, resDoc);
                        break;
                    default:
                        //Check if session exists for requests that require auth
                        if (SessionManager.PingSession(request.Cookies["playerconnect_session_id"].Value))
                        {
                            Console.WriteLine("SESSION ID: {0}", request.Cookies["playerconnect_session_id"].Value);
                            switch (url[0].Split('?')[0])
                            {
                                case "session.set_presence.xml":
                                    respond = Handlers.SessionSetPresenceHandler(request, response, urlEncodedData, resDoc);
                                    break;
                                case "content_url.list.xml":
                                    respond = Handlers.ContentUrlListHandler(request, response, urlEncodedData, resDoc);
                                    break;
                                case "skill_level.list.xml":
                                    respond = Handlers.SkillLevelListHandler(request, response, urlEncodedData, resDoc);
                                    break;
                                case "player_creation.mine.xml":
                                    DecodeURLEncoding(request.RawUrl.Substring(paramStart, request.RawUrl.Length - paramStart), urlEncodedData);
                                    respond = Handlers.PlayerCreationMineHandler(request, response, urlEncodedData, resDoc, sqlite_cmd);
                                    break;
                                case "player_creation.show.xml":
                                    DecodeURLEncoding(request.RawUrl.Substring(paramStart, request.RawUrl.Length - paramStart), urlEncodedData);
                                    respond = Handlers.PlayerCreationShowHandler(request, response, urlEncodedData, resDoc, sqlite_cmd);
                                    break;
                                case "player_creation.download.xml":
                                    respond = Handlers.PlayerCreationDownloadHandler(request, response, urlEncodedData, resDoc, sqlite_cmd);
                                    break;
                                case "player_creation.list.xml":
                                    DecodeURLEncoding(request.RawUrl.Substring(paramStart, request.RawUrl.Length - paramStart), urlEncodedData);
                                    respond = Handlers.PlayerCreationListHandler(request, response, urlEncodedData, resDoc);
                                    break;
                                case "player_creation.verify.xml":
                                    respond = Handlers.PlayerCreationVerifyHandler(request, response, urlEncodedData, resDoc);
                                    break;
                                case "player_creation.create.xml":
                                    File.WriteAllBytes("recvcreation.bin", recvBuffer);
                                    Console.WriteLine(request.ContentLength64);
                                    respond = Handlers.PlayerCreationCreateHandler(request, response, MultipartFormDataParser.Parse(new MemoryStream(recvBuffer)), resDoc, sqlite_cmd);
                                    break;
                                case "player_creation_complaint.create.xml":
                                    respond = Handlers.PlayerCreationComplaintCreateHandler(request, response, urlEncodedData, resDoc, sqlite_cmd);
                                    break;
                                case "player_creation_rating.view.xml":
                                    DecodeURLEncoding(request.RawUrl.Substring(paramStart, request.RawUrl.Length - paramStart), urlEncodedData);
                                    respond = Handlers.PlayerCreationRatingViewHandler(request, response, urlEncodedData, resDoc, sqlite_cmd);
                                    break;
                                case "player.to_id.xml":
                                    DecodeURLEncoding(request.RawUrl.Substring(paramStart, request.RawUrl.Length - paramStart), urlEncodedData);
                                    respond = Handlers.PlayerToIdHandler(request, response, urlEncodedData, resDoc, sqlite_cmd);
                                    break;
                                case "mail_message.create.xml":
                                    respond = Handlers.MailMessageCreateHandler(request, response, urlEncodedData, resDoc, sqlite_cmd);
                                    break;
                                case "tag.list.xml":
                                    respond = Handlers.TagListHandler(request, response, urlEncodedData, resDoc);
                                    break;
                                case "player_metric.show.xml":
                                    respond = Handlers.PlayerMetricShowHandler(request, response, urlEncodedData, resDoc);
                                    break;
                                case "player_metric.update.xml":
                                    respond = Handlers.PlayerMetricUpdateHandler(request, response, urlEncodedData, resDoc);
                                    break;
                                case "mail_message.list.xml":
                                    DecodeURLEncoding(request.RawUrl.Substring(paramStart, request.RawUrl.Length - paramStart), urlEncodedData);
                                    respond = Handlers.MailMessageListHandler(request, response, urlEncodedData, resDoc);
                                    break;
                                case "achievement.list.xml":
                                    DecodeURLEncoding(request.RawUrl.Substring(paramStart, request.RawUrl.Length - paramStart), urlEncodedData);
                                    respond = Handlers.AchievementListHandler(request, response, urlEncodedData, resDoc);
                                    break;
                                case "content_update.latest.xml":
                                    DecodeURLEncoding(request.RawUrl.Substring(paramStart, request.RawUrl.Length - paramStart), urlEncodedData);
                                    respond = Handlers.ContentUpdateLatestHandler(request, response, urlEncodedData, resDoc);
                                    break;
                                case "leaderboard.view.xml":
                                    DecodeURLEncoding(request.RawUrl.Substring(paramStart, request.RawUrl.Length - paramStart), urlEncodedData);
                                    respond = Handlers.LeaderboardViewHandler(request, response, urlEncodedData, resDoc);
                                    break;
                                case "leaderboard.player_stats.xml":
                                    DecodeURLEncoding(request.RawUrl.Substring(paramStart, request.RawUrl.Length - paramStart), urlEncodedData);
                                    respond = Handlers.LeaderboardPlayerStatsHandler(request, response, urlEncodedData, resDoc);
                                    break;
                                case "announcement.list.xml":
                                    respond = Handlers.AnnouncementListHandler(request, response, urlEncodedData, resDoc);
                                    break;
                                case "player.info.xml":
                                    respond = Handlers.PlayerInfoHandler(request, response, urlEncodedData, resDoc);
                                    break;
                                case "server.select.xml":
                                    respond = Handlers.ServerSelectHandler(request, response, urlEncodedData, resDoc);
                                    break;
                                default:
                                    respond = Handlers.DefaultHandler(request, response, urlEncodedData, resDoc);
                                    Console.WriteLine("Unimplemented request!");
                                    break;
                            }
                        }
                        else
                        {
                            respond = true;
                            resDoc.ChildNodes[0].ChildNodes[0].ChildNodes[0].InnerText = "-105";
                            resDoc.ChildNodes[0].ChildNodes[0].ChildNodes[1].InnerText = "NP Auth Failed: ticket is expired";
                        }
                        break;
                }
            }
            sqlite_conn.Close();
            //Determine if we need to respond and if the data is xml
            if (respond)
            {
                if (isXml)
                {
                    Console.WriteLine("Response XML: {0}", resDoc.InnerXml);
                    buffer = Encoding.UTF8.GetBytes(resDoc.InnerXml);
                }
                //Calculate an S3 ETag (Required for image previews), its just an MD5 hash
                response.AddHeader("ETag", "\"" + BitConverter.ToString(MD5.Create().ComputeHash(buffer)).Replace("-", "").ToLower() + "\"");
                response.KeepAlive = false;
                response.ContentLength64 = buffer.Length;
                output.Write(buffer, 0, buffer.Length);
            }
            output.Close();
        }

        public static void SessionServerProcessor(TcpClient client, X509Certificate2 cert)
        {
            //Here we process the client and send the data to the proper handler
            NetworkStream stream = client.GetStream();
            SslStream ssl = new SslStream(stream, false);
            ssl.AuthenticateAsServer(cert, false, System.Security.Authentication.SslProtocols.Default, false);
            Console.WriteLine("New client authenticated");
            byte[] responseBuffer = new byte[255];
            while (true)
            {
                XmlDocument recDoc = GetXmlDoc(ReadData(ssl));
                XmlDocument resDoc = InitResXml(recDoc);
                switch (recDoc.GetElementsByTagName("method")[0].InnerText.Split(' ')[1])
                {
                    case "startConnect":
                        MatchingHandlers.StartConnectHandler(recDoc, resDoc);
                        break;
                    default:
                        MatchingHandlers.DefaultHandler(recDoc, resDoc);
                        break;
                }
                Console.WriteLine("Lobbying server response: {0}", resDoc.InnerXml);
                WriteData(ssl, resDoc);
            }
            ssl.Close();
            client.Close();
        }

        static byte[] ReadData(SslStream ssl)
        {
            //Need to be able to read stream multiple times to get all data
            byte[] buffer = new byte[0];
            int count = 0;
            do
            {
                byte[] tempBuf = new byte[1024];
                int bytes = ssl.Read(tempBuf, 0, tempBuf.Length);
                buffer = buffer.Concat(tempBuf.Take(bytes).ToArray()).ToArray();
                if (bytes < 1023) { break; }
                count++;
            } while (true);
            return buffer;
        }

        static void WriteData(SslStream ssl, XmlDocument resDoc)
        {
            //Packet logs indicate the server would usually recieve some packets in 2 parts like on read, but im not sure how to
            //implement this or if it even needs to be implemented to work
            byte[] data = Encoding.UTF8.GetBytes(resDoc.InnerXml);
            data = data.Concat(new byte[] { 0x00 }).ToArray();  //Add null terminator to end of xml data
            BinaryReader br = new BinaryReader(new byte[0]);
            br.pushInt32(data.Length + 20);  //Length of xml data
            br.pushArray(new byte[16]); //Padding
            br.pushUInt32(0xFFFFFE64);  //??? (Checksum maybe, taken from request)
            byte[] buffer = br.getRes();
            buffer = buffer.Concat(data.ToArray()).ToArray();
            int bytesRead = 0;
            do
            {
                int toWrite = Math.Min(buffer.Length - bytesRead, 1024);
                ssl.Write(buffer, bytesRead, toWrite);
                bytesRead += toWrite;
            } while (bytesRead < buffer.Length);
            Console.WriteLine("Done!");
        }


        static XmlDocument GetXmlDoc(byte[] data)
        {
            //Decodes xml data from lobby server
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(Encoding.UTF8.GetString(data, 24, data.Length - 24));
            Console.WriteLine("Lobbying server request: {0}", doc.InnerXml);
            return doc;
        }

        static XmlDocument InitResXml(XmlDocument recDoc)
        {
            //Creates response body
            //Outside of TRANSACTION_TYPE_REPLY this might ALL be wrong
            XmlDocument doc = new XmlDocument();
            XmlElement service = doc.CreateElement("service");
            service.SetAttribute("name", "directory");    //Dont know what to set this to as of now
            XmlElement transaction = doc.CreateElement("transaction");
            transaction.SetAttribute("id", recDoc.ChildNodes[0].ChildNodes[0].Attributes["id"].InnerText);
            transaction.SetAttribute("type", "TRANSACTION_TYPE_REPLY");
            service.AppendChild(transaction);
            doc.AppendChild(service);
            return doc;
        }

        public static XmlElement AppendCommon(XmlDocument resDoc)
        {
            //Appends common response data to the response xml document
            XmlElement result = resDoc.CreateElement("result");
            XmlElement status = resDoc.CreateElement("status");
            XmlElement id = resDoc.CreateElement("id");
            XmlElement message = resDoc.CreateElement("message");
            id.InnerText = "0";
            message.InnerText = "Successful completion";
            status.AppendChild(id);
            status.AppendChild(message);
            result.AppendChild(status);
            return result;
        }

        public static void DecodeURLEncoding(string url, Dictionary<string, string> dict)
        {
            //Function to add url encoded data to a dictionary
            url = HttpUtility.UrlDecode(url);
            Console.WriteLine("URL Encoded data! Printing...");
            foreach (string entry in url.Split('&'))
            {
                string[] urlSplit = entry.Split('=');
                Console.WriteLine("Key={0}, Value={1}", urlSplit[0], urlSplit[1]);
                dict.Add(urlSplit[0], urlSplit[1]);
            }
            Console.WriteLine("End of URL Encoded data");
        }

        //Was going to decode multi part data but I gave up and just used a library instead
        //public static void DecodeBoundaryEncoding(string data, string contentType, Dictionary<string, string> dict)
        //{
        //    Console.WriteLine("Boundary Encoded data! Printing...");
        //    string boundary = contentType.Split("; ".ToCharArray())[1];
        //    Console.WriteLine("Boundary: {0}", boundary);
        //    foreach (string entry in data.Split(boundary.ToCharArray()))
        //    {
        //        string[] lineSplit = entry.Split('\n');
        //        foreach (string line in lineSplit)
        //        {
        //            Console.WriteLine("Printing boundary data...");
        //            if (line[0] == 'C')
        //            {
        //                Dictionary<string, string> headerList = new Dictionary<string, string>();
        //                foreach (string header in line.Split(';'))
        //                {
        //                    string[] headerSplit = header.Split('=');
        //                    headerList.Add(headerSplit[0].Replace(" ", ""), headerSplit[1]);
        //                    Console.WriteLine("Key={0}, Value={1}", headerSplit[0].Replace(" ", ""), headerSplit[1]);
        //                }
        //            }
        //        }
        //        Console.WriteLine("Key={0}, Value={1}", entry.Substring(entry.IndexOf("name=\""), entry.IndexOf("\";") - entry.IndexOf("name=\"")), entry.Substring(entry.Length));
        //        dict.Add(urlSplit[0], urlSplit[1]);
        //    }
        //    Console.WriteLine("End of Boundary Encoded data");
        //}
    }
}
