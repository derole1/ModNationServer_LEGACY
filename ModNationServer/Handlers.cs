using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Xml;
using System.IO;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using HttpMultipartParser;

namespace ModNationServer
{
    //Handles all HTTP packets
    static class Handlers
    {
        public static bool PreferencesUpdateHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc)
        {
            XmlElement res = resDoc.CreateElement("response");
            XmlElement preferences = resDoc.CreateElement("preference");
            preferences.SetAttribute("domain", url["preference[domain]"]);
            preferences.SetAttribute("ip_address", "");
            preferences.SetAttribute("language_code", url["preference[language_code]"]);
            preferences.SetAttribute("region_code", url["preference[region_code]"]);
            preferences.SetAttribute("timezone", url["preference[timezone]"]);
            res.AppendChild(preferences);
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool PolicyViewHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc)
        {
            XmlElement res = resDoc.CreateElement("response");
            XmlElement preferences = resDoc.CreateElement("policy");
            preferences.SetAttribute("id", "1");
            //TODO: Implement read from user profile
            preferences.SetAttribute("is_accepted", "true");
            preferences.SetAttribute("name", "Online User Agreement");
            if (File.Exists("EULA.txt"))
            {
                preferences.InnerText = File.ReadAllText("EULA.txt");
            }
            res.AppendChild(preferences);
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool PolicyAcceptHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc)
        {
            XmlElement res = resDoc.CreateElement("response");
            //TODO: Record that the user accepted the policy
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool SessionLoginHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc, SQLiteCommand sqlite_cmd)
        {
            string sessionID = SessionManager.GetSessionID(request.Cookies["playerconnect_session_id"].Value);
            response.SetCookie(new Cookie("playerconnect_session_id", SessionManager.AppendExtendSessionID(request.Cookies["playerconnect_session_id"].Value)));
            bool respond = SessionManager.CreateSession(sessionID, url["ticket"], sqlite_cmd);
            XmlElement res = resDoc.CreateElement("response");
            XmlElement logindata = resDoc.CreateElement("login_data");
            logindata.SetAttribute("player_id", SessionManager.players[sessionID].player_id.ToString());
            logindata.SetAttribute("player_name", SessionManager.players[sessionID].username);
            logindata.SetAttribute("presence", "ONLINE");
            logindata.SetAttribute("platform", url["platform"]);
            //TODO
            logindata.SetAttribute("login_time", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss+00:00"));
            logindata.SetAttribute("ip_address", request.RemoteEndPoint.Address.ToString());
            res.AppendChild(logindata);
            resDoc.ChildNodes[0].AppendChild(res);
            return respond;
        }

        //TODO: Add these to a config file
        static string[] contentTypes = new string[] { "player_avatars", "player_creations", "content_updates", "ghost_car_data" };
        static string[] contentFormats = new string[] { ".png", "data.bin, preview_image.png", "data.bin", "data.bin" };
        static string[] contentUrls = new string[] { "http://" + Program.ip + ":" + Program.port.ToString() + "/player_avatars/"
            , "http://" + Program.ip + ":" + Program.port.ToString() + "/player_creations/"
            , "http://" + Program.ip + ":" + Program.port.ToString() + "/content_updates/"
            , "http://" + Program.ip + ":" + Program.port.ToString() + "/ghost_car_data/" };

        public static bool ContentUrlListHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc)
        {
            XmlElement res = resDoc.CreateElement("response");
            XmlElement curls = resDoc.CreateElement("content_urls");
            curls.SetAttribute("total", contentTypes.Length.ToString());
            for (int i = 0; i < contentTypes.Length; i++)
            {
                XmlElement curl = resDoc.CreateElement("content_url");
                curl.SetAttribute("name", contentTypes[i]);
                curl.SetAttribute("formats", contentFormats[i]);
                curl.InnerText = contentUrls[i];
                curls.AppendChild(curl);
            }
            res.AppendChild(curls);
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool SessionPingHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc)
        {
            //This is sort of redundant now the server checks the dictionary per packet
            SessionManager.PingSession(request.Cookies["playerconnect_session_id"].Value);
            XmlElement res = resDoc.CreateElement("response");
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool SessionSetPresenceHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc)
        {
            //Updates session presence in the dictionary
            SessionManager.UpdatePresence(request.Cookies["playerconnect_session_id"].Value, url["presence"]);
            XmlElement res = resDoc.CreateElement("response");
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool ProfanityFilterListHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc)
        {
            XmlElement res = resDoc.CreateElement("response");
            XmlDocumentFragment pfilter = resDoc.CreateDocumentFragment();
            //TODO: Get profanity list from file
            pfilter.InnerXml = ServerValues.profanity_filter;
            res.AppendChild(pfilter);
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool SkillLevelListHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc)
        {
            XmlElement res = resDoc.CreateElement("response");
            XmlElement levels = resDoc.CreateElement("skill_levels");
            int count = 0;
            foreach (KeyValuePair<string, int> item in ServerValues.skill_levels)
            {
                XmlElement level = resDoc.CreateElement("skill_level");
                level.SetAttribute("id", (count + 1).ToString());
                level.SetAttribute("name", item.Key);
                level.SetAttribute("points", item.Value.ToString());
                levels.AppendChild(level);
                count++;
            }
            levels.SetAttribute("total", count.ToString());
            res.AppendChild(levels);
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool PlayerCreationMineHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc, SQLiteCommand sqlite_cmd)
        {
            XmlElement res = resDoc.CreateElement("response");
            XmlElement creations = resDoc.CreateElement("player_creations");
            SQLiteDataReader sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT COUNT(*) FROM Player_Creations WHERE player_creation_type=@player_creation_type AND platform=@platform AND deleted='false';"
                , new SQLiteParameter("@player_creation_type", tryGetParameter(url, "filters[player_creation_type]"))
                , new SQLiteParameter("@platform", tryGetParameter(url, "platform")));
            int totalCreations = sqReader.GetInt32(0);
            sqReader.Close();
            //TODO: Column sort
            sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT * FROM Player_Creations WHERE player_id=@player_id AND player_creation_type=@player_creation_type AND platform=@platform AND deleted='false' LIMIT @per_page OFFSET @page_skip;"
                , new SQLiteParameter("@player_id", SessionManager.players[SessionManager.GetSessionID(request.Cookies["playerconnect_session_id"].Value)].player_id.ToString())
                , new SQLiteParameter("@page_skip", (int.Parse(url["per_page"]) * (int.Parse(url["page"]) - 1)).ToString())
                , new SQLiteParameter("@per_page", tryGetParameter(url, "per_page"))
                , new SQLiteParameter("@player_creation_type", tryGetParameter(url, "filters[player_creation_type]"))
                , new SQLiteParameter("@platform", tryGetParameter(url, "filters[platform]")));
            int count = 0;
            while (sqReader.HasRows)
            {
                XmlElement playercreation = resDoc.CreateElement("player_creation");
                playercreation.SetAttribute("id", DatabaseManager.GetValue(sqReader, "id").ToString());
                playercreation.SetAttribute("name", DatabaseManager.GetValue(sqReader, "name").ToString());
                playercreation.SetAttribute("description", DatabaseManager.GetValue(sqReader, "description").ToString());
                playercreation.SetAttribute("moderation_status", "APPROVED");
                playercreation.SetAttribute("created_at", ((DateTime)DatabaseManager.GetValue(sqReader, "created_at")).ToString("yyyy-MM-ddTHH:mm:ss+00:00"));
                playercreation.SetAttribute("updated_at", ((DateTime)DatabaseManager.GetValue(sqReader, "updated_at")).ToString("yyyy-MM-ddTHH:mm:ss+00:00"));
                //playercreation.SetAttribute("rating", DatabaseManager.GetValue(sqReader, "rating").ToString());
                //playercreation.SetAttribute("star_rating", DatabaseManager.GetValue(sqReader, "rating").ToString());    //???
                playercreation.SetAttribute("points", DatabaseManager.GetValue(sqReader, "points").ToString());
                playercreation.SetAttribute("downloads", DatabaseManager.GetValue(sqReader, "downloads").ToString());
                playercreation.SetAttribute("views", DatabaseManager.GetValue(sqReader, "views").ToString());
                playercreation.SetAttribute("player_creation_type", DatabaseManager.GetValue(sqReader, "player_creation_type").ToString());
                playercreation.SetAttribute("races_started", DatabaseManager.GetValue(sqReader, "races_started").ToString());
                creations.AppendChild(playercreation);
                sqReader.Read();
                count++;
            }
            creations.SetAttribute("total", count.ToString());
            creations.SetAttribute("row_start", (int.Parse(url["per_page"]) * (int.Parse(url["page"]) - 1)).ToString());
            creations.SetAttribute("row_end", (int.Parse(url["per_page"]) * (int.Parse(url["page"]) - 1) + sqReader.FieldCount).ToString());
            creations.SetAttribute("page", url["page"]);
            creations.SetAttribute("total_pages", Math.Ceiling((double)totalCreations / int.Parse(url["per_page"])).ToString());
            sqReader.Close();
            foreach (XmlElement element in creations.ChildNodes)
            {
                //Rating
                sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT rating FROM Player_Creation_Ratings WHERE id=@id"
                   , new SQLiteParameter("@id", element.Attributes["id"].InnerText));
                float rating = 0;
                count = 0;
                while (sqReader.HasRows)
                {
                    rating += sqReader.GetFloat(0);
                    count++;
                    sqReader.Read();
                }
                sqReader.Close();
                element.SetAttribute("star_rating", (rating / count).ToString());
                element.SetAttribute("rating", (rating / count).ToString());
            }
            res.AppendChild(creations);
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool PlayerCreationListHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc, SQLiteCommand sqlite_cmd)
        {
            XmlElement res = resDoc.CreateElement("response");
            XmlElement creations = resDoc.CreateElement("player_creations");
            //If theres a better way to do this let me know
            SQLiteDataReader sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT COUNT(*) FROM Player_Creations WHERE player_creation_type=@player_creation_type AND platform=@platform AND deleted='false';"
                , new SQLiteParameter("@player_creation_type", tryGetParameter(url, "filters[player_creation_type]"))
                , new SQLiteParameter("@platform", tryGetParameter(url, "platform")));
            int totalCreations = sqReader.GetInt32(0);
            sqReader.Close();
            string sort_column = DatabaseManager.SanitizeString(tryGetParameter(url, "sort_column"));
            if (sort_column != "") { sort_column = "ORDER BY " + sort_column + " "; }
            string sort_order = DatabaseManager.SanitizeString(tryGetParameter(url, "sort_order"));
            if (sort_order != "") { sort_order = sort_order + " "; }
            sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT * FROM Player_Creations WHERE player_creation_type=@player_creation_type AND platform=@platform AND deleted='false' " + sort_column + sort_order + "LIMIT @per_page OFFSET @page_skip;"
                , new SQLiteParameter("@page_skip", (int.Parse(url["per_page"]) * (int.Parse(url["page"]) - 1)).ToString())
                , new SQLiteParameter("@per_page", tryGetParameter(url, "per_page"))
                , new SQLiteParameter("@player_creation_type", tryGetParameter(url, "filters[player_creation_type]"))
                , new SQLiteParameter("@platform", tryGetParameter(url, "platform")));
            int count = 0;
            while (sqReader.HasRows)
            {
                XmlElement playercreation = resDoc.CreateElement("player_creation");
                playercreation.SetAttribute("id", DatabaseManager.GetValue(sqReader, "id").ToString());
                playercreation.SetAttribute("name", DatabaseManager.GetValue(sqReader, "name").ToString());
                playercreation.SetAttribute("description", DatabaseManager.GetValue(sqReader, "description").ToString());
                playercreation.SetAttribute("moderation_status", "APPROVED");
                playercreation.SetAttribute("created_at", ((DateTime)DatabaseManager.GetValue(sqReader, "created_at")).ToString("yyyy-MM-ddTHH:mm:ss+00:00"));
                playercreation.SetAttribute("updated_at", ((DateTime)DatabaseManager.GetValue(sqReader, "updated_at")).ToString("yyyy-MM-ddTHH:mm:ss+00:00"));
                //playercreation.SetAttribute("rating", DatabaseManager.GetValue(sqReader, "rating").ToString());
                //playercreation.SetAttribute("star_rating", "1.0");
                playercreation.SetAttribute("points", DatabaseManager.GetValue(sqReader, "points").ToString());
                playercreation.SetAttribute("downloads", DatabaseManager.GetValue(sqReader, "downloads").ToString());
                playercreation.SetAttribute("views", DatabaseManager.GetValue(sqReader, "views").ToString());
                playercreation.SetAttribute("player_creation_type", DatabaseManager.GetValue(sqReader, "player_creation_type").ToString());
                playercreation.SetAttribute("races_started", DatabaseManager.GetValue(sqReader, "races_started").ToString());
                creations.AppendChild(playercreation);
                sqReader.Read();
                count++;
            }
            creations.SetAttribute("total", count.ToString());
            creations.SetAttribute("row_start", (int.Parse(url["per_page"]) * (int.Parse(url["page"]) - 1)).ToString());
            creations.SetAttribute("row_end", (int.Parse(url["per_page"]) * (int.Parse(url["page"]) - 1) + sqReader.FieldCount).ToString());
            creations.SetAttribute("page", url["page"]);
            creations.SetAttribute("total_pages", Math.Ceiling((double)totalCreations / int.Parse(url["per_page"])).ToString());
            sqReader.Close();
            foreach (XmlElement element in creations.ChildNodes)
            {
                //Rating
                sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT rating FROM Player_Creation_Ratings WHERE id=@id"
                   , new SQLiteParameter("@id", element.Attributes["id"].InnerText));
                float rating = 0;
                count = 0;
                while (sqReader.HasRows)
                {
                    rating += sqReader.GetFloat(0);
                    count++;
                    sqReader.Read();
                }
                sqReader.Close();
                element.SetAttribute("star_rating", (rating / count).ToString());
                element.SetAttribute("rating", (rating / count).ToString());
            }

            res.AppendChild(creations);
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool PlayerCreationFriendsViewHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc, SQLiteCommand sqlite_cmd)
        {
            XmlElement res = resDoc.CreateElement("response");
            XmlElement creations = resDoc.CreateElement("player_creations");
            //If theres a better way to do this let me know
            SQLiteDataReader sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT COUNT(*) FROM Player_Creations WHERE player_creation_type=@player_creation_type AND platform=@platform AND player_id=@player_id AND deleted='false';"
                , new SQLiteParameter("@player_creation_type", tryGetParameter(url, "filters[player_creation_type]"))
                , new SQLiteParameter("@platform", tryGetParameter(url, "platform"))
                , new SQLiteParameter("@player_id", GetIDFromUserName(tryGetParameter(url, "filters[username]"), sqlite_cmd)));
            int totalCreations = sqReader.GetInt32(0);
            sqReader.Close();
            string sort_column = DatabaseManager.SanitizeString(tryGetParameter(url, "sort_column"));
            if (sort_column != "") { sort_column = "ORDER BY " + sort_column + " "; }
            string sort_order = DatabaseManager.SanitizeString(tryGetParameter(url, "sort_order"));
            if (sort_order != "") { sort_order = sort_order + " "; }
            string username = DatabaseManager.SanitizeString(tryGetParameter(url, "filters[username]"));
            if (username != "")
            {
                string[] usernameSplit = username.Split(',');
                username = "AND (";
                foreach (string name in usernameSplit)
                {
                    username += "player_id=" + GetIDFromUserName(name, sqlite_cmd) + " ";
                    username += "OR ";
                }
                username = username.Substring(0, username.Length - 4) + ") ";
            }
            sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT * FROM Player_Creations WHERE player_creation_type=@player_creation_type AND platform=@platform AND deleted='false' " + username + sort_column + sort_order + "LIMIT @per_page OFFSET @page_skip;"
                , new SQLiteParameter("@page_skip", (int.Parse(url["per_page"]) * (int.Parse(url["page"]) - 1)).ToString())
                , new SQLiteParameter("@per_page", tryGetParameter(url, "per_page"))
                , new SQLiteParameter("@player_creation_type", tryGetParameter(url, "filters[player_creation_type]"))
                , new SQLiteParameter("@platform", tryGetParameter(url, "platform")));
            int count = 0;
            while (sqReader.HasRows)
            {
                XmlElement playercreation = resDoc.CreateElement("player_creation");
                playercreation.SetAttribute("id", DatabaseManager.GetValue(sqReader, "id").ToString());
                playercreation.SetAttribute("name", DatabaseManager.GetValue(sqReader, "name").ToString());
                playercreation.SetAttribute("description", DatabaseManager.GetValue(sqReader, "description").ToString());
                playercreation.SetAttribute("moderation_status", "APPROVED");
                playercreation.SetAttribute("created_at", ((DateTime)DatabaseManager.GetValue(sqReader, "created_at")).ToString("yyyy-MM-ddTHH:mm:ss+00:00"));
                playercreation.SetAttribute("updated_at", ((DateTime)DatabaseManager.GetValue(sqReader, "updated_at")).ToString("yyyy-MM-ddTHH:mm:ss+00:00"));
                playercreation.SetAttribute("rating", DatabaseManager.GetValue(sqReader, "rating").ToString());
                playercreation.SetAttribute("star_rating", "1.0");
                playercreation.SetAttribute("points", DatabaseManager.GetValue(sqReader, "points").ToString());
                playercreation.SetAttribute("downloads", DatabaseManager.GetValue(sqReader, "downloads").ToString());
                playercreation.SetAttribute("views", DatabaseManager.GetValue(sqReader, "views").ToString());
                playercreation.SetAttribute("player_creation_type", DatabaseManager.GetValue(sqReader, "player_creation_type").ToString());
                playercreation.SetAttribute("races_started", DatabaseManager.GetValue(sqReader, "races_started").ToString());
                creations.AppendChild(playercreation);
                sqReader.Read();
                count++;
            }
            creations.SetAttribute("total", count.ToString());
            creations.SetAttribute("row_start", (int.Parse(url["per_page"]) * (int.Parse(url["page"]) - 1)).ToString());
            creations.SetAttribute("row_end", (int.Parse(url["per_page"]) * (int.Parse(url["page"]) - 1) + sqReader.FieldCount).ToString());
            creations.SetAttribute("page", url["page"]);
            creations.SetAttribute("total_pages", Math.Ceiling((double)totalCreations / int.Parse(url["per_page"])).ToString());
            sqReader.Close();
            foreach (XmlElement element in creations.ChildNodes)
            {
                //Rating
                sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT rating FROM Player_Creation_Ratings WHERE id=@id"
                   , new SQLiteParameter("@id", element.Attributes["id"].InnerText));
                float rating = 0;
                count = 0;
                while (sqReader.HasRows)
                {
                    rating += sqReader.GetFloat(0);
                    count++;
                    sqReader.Read();
                }
                sqReader.Close();
                element.SetAttribute("star_rating", (rating / count).ToString());
                element.SetAttribute("rating", (rating / count).ToString());
            }

            res.AppendChild(creations);
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool PlayerCreationSearchHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc, SQLiteCommand sqlite_cmd)
        {
            XmlElement res = resDoc.CreateElement("response");
            XmlElement creations = resDoc.CreateElement("player_creations");
            //If theres a better way to do this let me know
            SQLiteDataReader sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT COUNT(*) FROM Player_Creations WHERE player_creation_type=@player_creation_type AND platform=@platform AND deleted='false';"
                , new SQLiteParameter("@player_creation_type", tryGetParameter(url, "player_creation_type"))
                , new SQLiteParameter("@platform", tryGetParameter(url, "platform")));
            int totalCreations = sqReader.GetInt32(0);
            sqReader.Close();
            string search = DatabaseManager.SanitizeString(tryGetParameter(url, "search"));
            if (search != "") { search = "AND name LIKE '%" + search + "%' "; }
            string tags = DatabaseManager.SanitizeString(tryGetParameter(url, "search_tags"));
            if (tags != "") { tags = "AND tags LIKE '%" + tags + "%' "; }
            string username = DatabaseManager.SanitizeString(tryGetParameter(url, "username"));
            if (username != "")
            {
                string[] usernameSplit = username.Split(',');
                username = "";
                foreach (string name in usernameSplit)
                {
                    username += "AND player_id=" + GetIDFromUserName(name, sqlite_cmd) + " ";
                }
            }
            sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT * FROM Player_Creations WHERE player_creation_type=@player_creation_type AND platform=@platform AND deleted='false' " + search + tags + username + "LIMIT @per_page OFFSET @page_skip;"
                , new SQLiteParameter("@page_skip", (int.Parse(url["per_page"]) * (int.Parse(url["page"]) - 1)).ToString())
                , new SQLiteParameter("@per_page", tryGetParameter(url, "per_page"))
                , new SQLiteParameter("@player_creation_type", tryGetParameter(url, "player_creation_type"))
                , new SQLiteParameter("@platform", tryGetParameter(url, "platform")));
            int count = 0;
            while (sqReader.HasRows)
            {
                XmlElement playercreation = resDoc.CreateElement("player_creation");
                playercreation.SetAttribute("id", DatabaseManager.GetValue(sqReader, "id").ToString());
                playercreation.SetAttribute("name", DatabaseManager.GetValue(sqReader, "name").ToString());
                playercreation.SetAttribute("description", DatabaseManager.GetValue(sqReader, "description").ToString());
                playercreation.SetAttribute("moderation_status", "APPROVED");
                playercreation.SetAttribute("created_at", ((DateTime)DatabaseManager.GetValue(sqReader, "created_at")).ToString("yyyy-MM-ddTHH:mm:ss+00:00"));
                playercreation.SetAttribute("updated_at", ((DateTime)DatabaseManager.GetValue(sqReader, "updated_at")).ToString("yyyy-MM-ddTHH:mm:ss+00:00"));
                //playercreation.SetAttribute("rating", DatabaseManager.GetValue(sqReader, "rating").ToString());
                //playercreation.SetAttribute("star_rating", "1.0");
                playercreation.SetAttribute("points", DatabaseManager.GetValue(sqReader, "points").ToString());
                playercreation.SetAttribute("downloads", DatabaseManager.GetValue(sqReader, "downloads").ToString());
                playercreation.SetAttribute("views", DatabaseManager.GetValue(sqReader, "views").ToString());
                playercreation.SetAttribute("player_creation_type", DatabaseManager.GetValue(sqReader, "player_creation_type").ToString());
                playercreation.SetAttribute("races_started", DatabaseManager.GetValue(sqReader, "races_started").ToString());
                creations.AppendChild(playercreation);
                sqReader.Read();
                count++;
            }
            creations.SetAttribute("total", count.ToString());
            creations.SetAttribute("row_start", (int.Parse(url["per_page"]) * (int.Parse(url["page"]) - 1)).ToString());
            creations.SetAttribute("row_end", (int.Parse(url["per_page"]) * (int.Parse(url["page"]) - 1) + sqReader.FieldCount).ToString());
            creations.SetAttribute("page", url["page"]);
            creations.SetAttribute("total_pages", Math.Ceiling((double)totalCreations / int.Parse(url["per_page"])).ToString());
            sqReader.Close();
            foreach (XmlElement element in creations.ChildNodes)
            {
                //Rating
                sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT rating FROM Player_Creation_Ratings WHERE id=@id"
                   , new SQLiteParameter("@id", element.Attributes["id"].InnerText));
                float rating = 0;
                count = 0;
                while (sqReader.HasRows)
                {
                    rating += sqReader.GetFloat(0);
                    count++;
                    sqReader.Read();
                }
                sqReader.Close();
                element.SetAttribute("star_rating", (rating / count).ToString());
                element.SetAttribute("rating", (rating / count).ToString());
            }

            res.AppendChild(creations);
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool PlayerCreationShowHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc, SQLiteCommand sqlite_cmd)
        {
            XmlElement res = resDoc.CreateElement("response");
            XmlElement playercreation = resDoc.CreateElement("player_creation");
            if (url["is_counted"] == "true")
            {
                DatabaseManager.NonQuery(sqlite_cmd, "UPDATE Player_Creations SET views=views+1, views_this_week=views_this_week+1 WHERE id=@id;"
                , new SQLiteParameter("@id", url["id"]));
            }
            SQLiteDataReader sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT * FROM Player_Creations WHERE id=@id LIMIT 1;"
                , new SQLiteParameter("@id", url["id"]));
            //TODO: Sort through what actually needs to be returned for each packet, for now I just throw everything at it from the database about the creation
            if (sqReader.HasRows)
            {
                playercreation.SetAttribute("id", DatabaseManager.GetValue(sqReader, "id").ToString());
                playercreation.SetAttribute("name", DatabaseManager.GetValue(sqReader, "name").ToString());
                playercreation.SetAttribute("description", DatabaseManager.GetValue(sqReader, "description").ToString());
                playercreation.SetAttribute("moderation_status", "APPROVED");    //TODO
                playercreation.SetAttribute("moderation_status_id", "1");    //TODO
                playercreation.SetAttribute("created_at", ((DateTime)DatabaseManager.GetValue(sqReader, "created_at")).ToString("yyyy-MM-ddTHH:mm:ss+00:00"));
                playercreation.SetAttribute("updated_at", ((DateTime)DatabaseManager.GetValue(sqReader, "updated_at")).ToString("yyyy-MM-ddTHH:mm:ss+00:00"));
                playercreation.SetAttribute("downloads", DatabaseManager.GetValue(sqReader, "downloads").ToString());
                playercreation.SetAttribute("downloads_this_week", DatabaseManager.GetValue(sqReader, "downloads_this_week").ToString());
                playercreation.SetAttribute("downloads_last_week", DatabaseManager.GetValue(sqReader, "downloads_last_week").ToString());
                playercreation.SetAttribute("views", DatabaseManager.GetValue(sqReader, "views").ToString());
                playercreation.SetAttribute("views_this_week", DatabaseManager.GetValue(sqReader, "views_this_week").ToString());
                playercreation.SetAttribute("views_last_week", DatabaseManager.GetValue(sqReader, "views_last_week").ToString());
                playercreation.SetAttribute("points", DatabaseManager.GetValue(sqReader, "points").ToString());
                playercreation.SetAttribute("points_today", DatabaseManager.GetValue(sqReader, "points_today").ToString());
                playercreation.SetAttribute("points_yesterday", DatabaseManager.GetValue(sqReader, "points_yesterday").ToString());
                playercreation.SetAttribute("points_this_week", DatabaseManager.GetValue(sqReader, "points_this_week").ToString());
                playercreation.SetAttribute("points_last_week", DatabaseManager.GetValue(sqReader, "points_last_week").ToString());
                playercreation.SetAttribute("rating", DatabaseManager.GetValue(sqReader, "rating").ToString());
                playercreation.SetAttribute("version", DatabaseManager.GetValue(sqReader, "version").ToString());
                playercreation.SetAttribute("tags", DatabaseManager.GetValue(sqReader, "tags").ToString());
                playercreation.SetAttribute("player_creation_type", DatabaseManager.GetValue(sqReader, "player_creation_type").ToString());
                playercreation.SetAttribute("parent_creation_id", DatabaseManager.GetValue(sqReader, "parent_creation_id").ToString());
                playercreation.SetAttribute("parent_player_id", DatabaseManager.GetValue(sqReader, "parent_player_id").ToString());
                playercreation.SetAttribute("player_id", DatabaseManager.GetValue(sqReader, "player_id").ToString());
                playercreation.SetAttribute("original_player_id", DatabaseManager.GetValue(sqReader, "original_player_id").ToString());
                playercreation.SetAttribute("requires_dlc", DatabaseManager.GetValue(sqReader, "requires_dlc").ToString().ToLower());
                playercreation.SetAttribute("dlc_keys", DatabaseManager.GetValue(sqReader, "dlc_keys").ToString());
                playercreation.SetAttribute("platform", DatabaseManager.GetValue(sqReader, "platform").ToString());
                playercreation.SetAttribute("is_remixable", DatabaseManager.GetValue(sqReader, "is_remixable").ToString().ToLower());
                playercreation.SetAttribute("longest_hang_time", DatabaseManager.GetValue(sqReader, "longest_hang_time").ToString());
                playercreation.SetAttribute("longest_drift", DatabaseManager.GetValue(sqReader, "longest_drift").ToString());
                playercreation.SetAttribute("races_started", DatabaseManager.GetValue(sqReader, "races_started").ToString());
                playercreation.SetAttribute("races_won", DatabaseManager.GetValue(sqReader, "races_won").ToString());
                playercreation.SetAttribute("votes", DatabaseManager.GetValue(sqReader, "votes").ToString());
                playercreation.SetAttribute("races_finished", DatabaseManager.GetValue(sqReader, "races_finished").ToString());
                playercreation.SetAttribute("best_lap_time", DatabaseManager.GetValue(sqReader, "best_lap_time").ToString());
                playercreation.SetAttribute("track_theme", DatabaseManager.GetValue(sqReader, "track_theme").ToString());
                playercreation.SetAttribute("auto_reset", DatabaseManager.GetValue(sqReader, "auto_reset").ToString().ToLower());
                playercreation.SetAttribute("ai", DatabaseManager.GetValue(sqReader, "ai").ToString().ToLower());
            }
            sqReader.Close();
            playercreation.SetAttribute("username", GetUserNameFromID(playercreation.Attributes["player_id"].InnerText, sqlite_cmd));
            playercreation.SetAttribute("original_player_username", GetUserNameFromID(playercreation.Attributes["original_player_id"].InnerText, sqlite_cmd));
            playercreation.SetAttribute("parent_player_username", GetUserNameFromID(playercreation.Attributes["parent_player_id"].InnerText, sqlite_cmd));
            playercreation.SetAttribute("parent_creation_name", GetCreationNameFromID(playercreation.Attributes["parent_creation_id"].InnerText, sqlite_cmd));
            //Rating
            sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT rating FROM Player_Creation_Ratings WHERE id=@id"
               , new SQLiteParameter("@id", playercreation.Attributes["id"].InnerText));
            float rating = 0;
            int count = 0;
            while (sqReader.HasRows)
            {
                rating += sqReader.GetFloat(0);
                count++;
                sqReader.Read();
            }
            sqReader.Close();
            playercreation.SetAttribute("star_rating", (rating / count).ToString());
            playercreation.SetAttribute("rating", (rating / count).ToString());
            res.AppendChild(playercreation);
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool PlayerCreationDownloadHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc, SQLiteCommand sqlite_cmd)
        {
            XmlElement res = resDoc.CreateElement("response");
            XmlElement creations = resDoc.CreateElement("player_creations");
            if (url["is_counted"] == "true")
            {
                DatabaseManager.NonQuery(sqlite_cmd, "UPDATE Player_Creations SET downloads=downloads+1, downloads_this_week=downloads_this_week+1 WHERE id=@id;"
                , new SQLiteParameter("@id", url["id"]));
            }
            SQLiteDataReader sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT * FROM Player_Creations WHERE id=@id LIMIT 1;"
                , new SQLiteParameter("@id", url["id"]));
            if (sqReader.HasRows)
            {
                XmlElement playercreation = resDoc.CreateElement("player_creation");
                playercreation.SetAttribute("id", DatabaseManager.GetValue(sqReader, "id").ToString());
                playercreation.SetAttribute("name", DatabaseManager.GetValue(sqReader, "name").ToString());
                playercreation.SetAttribute("description", DatabaseManager.GetValue(sqReader, "description").ToString());
                playercreation.SetAttribute("moderation_status", DatabaseManager.GetValue(sqReader, "moderation_status").ToString());
                playercreation.SetAttribute("created_at", ((DateTime)DatabaseManager.GetValue(sqReader, "created_at")).ToString("yyyy-MM-ddTHH:mm:ss+00:00"));
                playercreation.SetAttribute("updated_at", ((DateTime)DatabaseManager.GetValue(sqReader, "updated_at")).ToString("yyyy-MM-ddTHH:mm:ss+00:00"));
                playercreation.SetAttribute("points", DatabaseManager.GetValue(sqReader, "points").ToString());
                playercreation.SetAttribute("points_today", DatabaseManager.GetValue(sqReader, "points_today").ToString());
                playercreation.SetAttribute("points_yesterday", DatabaseManager.GetValue(sqReader, "points_today").ToString());
                playercreation.SetAttribute("points_this_week", DatabaseManager.GetValue(sqReader, "points_this_week").ToString());
                playercreation.SetAttribute("points_last_week", DatabaseManager.GetValue(sqReader, "points_last_week").ToString());
                playercreation.SetAttribute("version", DatabaseManager.GetValue(sqReader, "version").ToString());
                playercreation.SetAttribute("tags", DatabaseManager.GetValue(sqReader, "tags").ToString());
                playercreation.SetAttribute("player_creation_type", DatabaseManager.GetValue(sqReader, "player_creation_type").ToString());
                playercreation.SetAttribute("parent_creation_id", DatabaseManager.GetValue(sqReader, "parent_creation_id").ToString());
                playercreation.SetAttribute("parent_player_id", DatabaseManager.GetValue(sqReader, "parent_player_id").ToString());
                playercreation.SetAttribute("player_id", DatabaseManager.GetValue(sqReader, "player_id").ToString());
                playercreation.SetAttribute("original_player_id", DatabaseManager.GetValue(sqReader, "original_player_id").ToString());
                playercreation.SetAttribute("is_remixable", DatabaseManager.GetValue(sqReader, "is_remixable").ToString().ToLower());
                byte[] creationData = File.ReadAllBytes("player_creations\\" + url["id"] + "\\data.bin");
                playercreation.SetAttribute("data_md5", BitConverter.ToString(MD5.Create().ComputeHash(creationData)).Replace("-", "").ToLower());
                playercreation.SetAttribute("data_size", creationData.Length.ToString());
                byte[] previewData = File.ReadAllBytes("player_creations\\" + url["id"] + "\\preview_image.png");
                playercreation.SetAttribute("preview_md5", BitConverter.ToString(MD5.Create().ComputeHash(previewData)).Replace("-", "").ToLower());
                playercreation.SetAttribute("preview_size", previewData.Length.ToString());
                creations.AppendChild(playercreation);
            }
            sqReader.Close();
            foreach (XmlElement element in creations)
            {
                element.SetAttribute("username", GetUserNameFromID(element.Attributes["player_id"].InnerText, sqlite_cmd));
                element.SetAttribute("original_player_username", GetUserNameFromID(element.Attributes["original_player_id"].InnerText, sqlite_cmd));
                element.SetAttribute("parent_player_username", GetUserNameFromID(element.Attributes["parent_player_id"].InnerText, sqlite_cmd));
                element.SetAttribute("parent_creation_name", GetCreationNameFromID(element.Attributes["parent_creation_id"].InnerText, sqlite_cmd));
            }
            res.AppendChild(creations);
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool PlayerCreationRatingViewHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc, SQLiteCommand sqlite_cmd)
        {
            XmlElement res = resDoc.CreateElement("response");
            XmlElement creations = resDoc.CreateElement("player_creation_ratings");
            //TODO
            //creations.SetAttribute("comments", "test");
            //creations.SetAttribute("rating", "1.0");
            //TODO: Get creation ratings
            res.AppendChild(creations);
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool PlayerCreationRatingCreateHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc, SQLiteCommand sqlite_cmd)
        {
            XmlElement res = resDoc.CreateElement("response");
            DatabaseManager.NonQuery(sqlite_cmd, "INSERT INTO Player_Creation_Ratings VALUES (@id,@player_id,@rating,@comments)"
                , new SQLiteParameter("@id", url["player_creation_rating[player_creation_id]"])
                , new SQLiteParameter("@player_id", SessionManager.players[SessionManager.GetSessionID(request.Cookies["playerconnect_session_id"].Value)].player_id.ToString())
                , new SQLiteParameter("@rating", url["player_creation_rating[rating]"])
                , new SQLiteParameter("@comments", tryGetParameter(url, "player_creation_rating[comments]")));
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool PlayerCreationRatingListHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc, SQLiteCommand sqlite_cmd)
        {
            XmlElement res = resDoc.CreateElement("response");
            XmlElement creations = resDoc.CreateElement("player_creation_ratings");
            SQLiteDataReader sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT COUNT(*) FROM Player_Creation_Ratings WHERE id=@id;"
                , new SQLiteParameter("@id", tryGetParameter(url, "player_creation_id")));
            int totalCreations = sqReader.GetInt32(0);
            sqReader.Close();
            sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT * FROM Player_Creation_Ratings WHERE id=@id ORDER BY (SELECT NULL) DESC LIMIT @per_page OFFSET @page_skip;"
                , new SQLiteParameter("@page_skip", (int.Parse(url["per_page"]) * (int.Parse(url["page"]) - 1)).ToString())
                , new SQLiteParameter("@per_page", tryGetParameter(url, "per_page"))
                , new SQLiteParameter("@id", tryGetParameter(url, "player_creation_id")));
            int count = 0;
            long playerid = 0;
            while (sqReader.HasRows)
            {
                XmlElement playercreation = resDoc.CreateElement("player_creation_rating");
                playercreation.SetAttribute("rating", DatabaseManager.GetValue(sqReader, "rating").ToString());
                playercreation.SetAttribute("comments", DatabaseManager.GetValue(sqReader, "comments").ToString());
                playerid = (long)DatabaseManager.GetValue(sqReader, "player_id");
                playercreation.SetAttribute("player_id", playerid.ToString());
                creations.AppendChild(playercreation);
                sqReader.Read();
                count++;
            }
            creations.SetAttribute("total", count.ToString());
            creations.SetAttribute("row_start", (int.Parse(url["per_page"]) * (int.Parse(url["page"]) - 1)).ToString());
            creations.SetAttribute("row_end", (int.Parse(url["per_page"]) * (int.Parse(url["page"]) - 1) + sqReader.FieldCount).ToString());
            creations.SetAttribute("page", url["page"]);
            creations.SetAttribute("total_pages", Math.Ceiling((double)totalCreations / int.Parse(url["per_page"])).ToString());
            sqReader.Close();
            foreach (XmlElement element in creations)
            {
                element.SetAttribute("username", GetUserNameFromID(playerid.ToString(), sqlite_cmd));
            }
            res.AppendChild(creations);
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        //public static bool PlayerCreationListHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc)
        //{
        //    XmlElement res = resDoc.CreateElement("response");
        //    XmlElement creations = resDoc.CreateElement("player_creations");
        //    //TODO
        //    creations.SetAttribute("total", "0");
        //    creations.SetAttribute("row_start", "0");
        //    creations.SetAttribute("row_end", "0");
        //    creations.SetAttribute("page", "1");
        //    creations.SetAttribute("total_pages", "0");
        //    //TODO: Get creations
        //    res.AppendChild(creations);
        //    resDoc.ChildNodes[0].AppendChild(res);
        //    return true;
        //}

        public static bool PlayerCreationVerifyHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc)
        {
            XmlElement res = resDoc.CreateElement("response");
            XmlElement creations = resDoc.CreateElement("player_creations");
            //TODO
            creations.SetAttribute("total", "0");
            creations.InnerText = "\n";
            //TODO: Get creations
            res.AppendChild(creations);
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool PlayerCreationCreateHandler(HttpListenerRequest request, HttpListenerResponse response, MultipartFormDataParser url, XmlDocument resDoc, SQLiteCommand sqlite_cmd)
        {
            SQLiteDataReader sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT id FROM Player_Creations ORDER BY id DESC LIMIT 1;");
            int id = 10000;
            if (sqReader.HasRows) { id = sqReader.GetInt32(0) + 1; }
            sqReader.Close();
            DatabaseManager.NonQuery(sqlite_cmd, "INSERT INTO Player_Creations VALUES(@id,@player_id,@name,@description,@created_at,@updated_at,@downloads,@downloads_this_week,@downloads_last_week" +
                ",@views,@views_this_week,@views_last_week,@points,@points_today,@points_yesterday,@points_this_week,@points_last_week,@rating,@version,@tags,@player_creation_type,@parent_creation_id" +
                ",@parent_player_id,@original_player_id,@requires_dlc,@dlc_keys,@platform,@is_remixable,@longest_hang_time,@longest_drift,@races_started,@races_won,@votes,@races_finished" +
                ",@best_lap_time,@track_theme,@auto_reset,@ai,'false')"
                , new SQLiteParameter("@id", id)
                , new SQLiteParameter("@player_id", SessionManager.players[SessionManager.GetSessionID(request.Cookies["playerconnect_session_id"].Value)].player_id.ToString())
                , new SQLiteParameter("@name", url.GetParameterValue("player_creation[name]"))
                , new SQLiteParameter("@description", url.GetParameterValue("player_creation[description]"))
                , new SQLiteParameter("@created_at", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss+00:00"))
                , new SQLiteParameter("@updated_at", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss+00:00"))
                , new SQLiteParameter("@downloads", "0")
                , new SQLiteParameter("@downloads_this_week", "0")
                , new SQLiteParameter("@downloads_last_week", "0")
                , new SQLiteParameter("@views", "0")
                , new SQLiteParameter("@views_this_week", "0")
                , new SQLiteParameter("@views_last_week", "0")
                , new SQLiteParameter("@points", "0")
                , new SQLiteParameter("@points_today", "0")
                , new SQLiteParameter("@points_yesterday", "0")
                , new SQLiteParameter("@points_this_week", "0")
                , new SQLiteParameter("@points_last_week", "0")
                , new SQLiteParameter("@rating", "0")
                , new SQLiteParameter("@version", "1")
                , new SQLiteParameter("@tags", url.GetParameterValue("player_creation[tags]"))
                , new SQLiteParameter("@player_creation_type", url.GetParameterValue("player_creation[player_creation_type]"))
                , new SQLiteParameter("@parent_creation_id", url.GetParameterValue("player_creation[parent_creation_id]"))
                , new SQLiteParameter("@parent_player_id", url.GetParameterValue("player_creation[parent_player_id]"))
                , new SQLiteParameter("@original_player_id", url.GetParameterValue("player_creation[original_player_id]"))
                , new SQLiteParameter("@requires_dlc", url.GetParameterValue("player_creation[requires_dlc]"))
                , new SQLiteParameter("@dlc_keys", url.GetParameterValue("player_creation[dlc_keys]"))
                , new SQLiteParameter("@platform", url.GetParameterValue("player_creation[platform]"))
                , new SQLiteParameter("@is_remixable", url.GetParameterValue("player_creation[is_remixable]"))
                , new SQLiteParameter("@longest_hang_time", url.GetParameterValue("player_creation[longest_hang_time]"))
                , new SQLiteParameter("@longest_drift", url.GetParameterValue("player_creation[longest_drift]"))
                , new SQLiteParameter("@races_started", url.GetParameterValue("player_creation[races_started]"))
                , new SQLiteParameter("@races_won", url.GetParameterValue("player_creation[races_won]"))
                , new SQLiteParameter("@votes", url.GetParameterValue("player_creation[votes]"))
                , new SQLiteParameter("@races_finished", url.GetParameterValue("player_creation[races_finished]"))
                , new SQLiteParameter("@best_lap_time", url.GetParameterValue("player_creation[best_lap_time]"))
                , new SQLiteParameter("@track_theme", url.GetParameterValue("player_creation[track_theme]"))
                , new SQLiteParameter("@auto_reset", url.GetParameterValue("player_creation[auto_reset]"))
                , new SQLiteParameter("@ai", url.GetParameterValue("player_creation[ai]")));
            Directory.CreateDirectory("player_creations\\" + id.ToString());
            byte[] fileBuffer = new byte[url.Files[0].Data.Length];
            url.Files[0].Data.Read(fileBuffer, 0, fileBuffer.Length);
            File.WriteAllBytes("player_creations\\" + id.ToString() + "\\" + url.Files[0].FileName + ".bin", fileBuffer);
            fileBuffer = new byte[url.Files[1].Data.Length];
            url.Files[1].Data.Read(fileBuffer, 0, fileBuffer.Length);
            File.WriteAllBytes("player_creations\\" + id.ToString() + "\\" + url.Files[1].FileName + "_image.png", fileBuffer);
            new Bitmap(Image.FromStream(new MemoryStream(fileBuffer)), 128, 128).Save("player_creations\\" + id.ToString() + "\\" + url.Files[1].FileName + "_image_128x128.png", ImageFormat.Png);
            XmlElement res = resDoc.CreateElement("response");
            XmlElement creations = resDoc.CreateElement("player_creations");
            string sqlcommand = "UPDATE Users SET total_player_creations=total_player_creations+1, ";
            switch (url.GetParameterValue("player_creation[player_creation_type]"))
            {
                case "TRACK":
                    sqlcommand += "total_tracks=total_tracks";
                    break;
                case "KART":
                    sqlcommand += "total_karts=total_karts";
                    break;
                case "CHARACTER":
                    sqlcommand += "total_characters=total_characters";
                    break;
                default:
                    return true;
            }
            sqlcommand += "+1 WHERE player_id=@player_id;";
            DatabaseManager.NonQuery(sqlite_cmd, sqlcommand, new SQLiteParameter("@player_id", SessionManager.playerTickets[SessionManager.GetSessionID(request.Cookies["playerconnect_session_id"].Value)].user_id));
            creations.SetAttribute("id", id.ToString());
            res.AppendChild(creations);
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool PlayerCreationDestroyHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string>url, XmlDocument resDoc, SQLiteCommand sqlite_cmd)
        {
            XmlElement res = resDoc.CreateElement("response");
            SQLiteDataReader sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT player_id, player_creation_type FROM Player_Creations WHERE id=@id", new SQLiteParameter("@id", url["id"]));
            if (sqReader.HasRows)
            {
                ulong playerId = (ulong)sqReader.GetInt64(0);
                string creationType = sqReader.GetString(1);
                sqReader.Close();
                try
                {
                    if (playerId == SessionManager.players[SessionManager.GetSessionID(request.Cookies["playerconnect_session_id"].Value)].player_id)
                    {
                        string sqlcommand = "UPDATE Users SET total_player_creations=total_player_creations-1, ";
                        switch (creationType)
                        {
                            case "TRACK":
                                sqlcommand += "total_tracks=total_tracks";
                                break;
                            case "KART":
                                sqlcommand += "total_karts=total_karts";
                                break;
                            case "CHARACTER":
                                sqlcommand += "total_characters=total_characters";
                                break;
                            default:
                                return true;
                        }
                        sqlcommand += "-1 WHERE player_id=@player_id;";
                        DatabaseManager.NonQuery(sqlite_cmd, sqlcommand, new SQLiteParameter("@player_id", playerId));
                        DatabaseManager.NonQuery(sqlite_cmd, "UPDATE Player_Creations SET deleted='true' WHERE id=@id", new SQLiteParameter("@id", url["id"]));
                        Directory.Delete("player_creations\\" + url["id"], true);
                    }
                }
                catch { }
            } else { sqReader.Close(); }
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool PlayerCreationComplaintCreateHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc, SQLiteCommand sqlite_cmd)
        {
            XmlElement res = resDoc.CreateElement("response");
            //Log a player complaint to the database
            DatabaseManager.NonQuery(sqlite_cmd, "INSERT INTO Player_Creations_Complaints VALUES (@owner_player_id,@player_comments,@player_complaint_reason,@player_creation_id)"
                , new SQLiteParameter("@owner_player_id", url["player_creation_complaint[owner_player_id]"])
                , new SQLiteParameter("@player_comments", url["player_creation_complaint[player_comments]"])
                , new SQLiteParameter("@player_complaint_reason", url["player_creation_complaint[player_complaint_reason]"])
                , new SQLiteParameter("@player_creation_id", url["player_creation_complaint[player_creation_id]"]));
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool PlayerToIdHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc, SQLiteCommand sqlite_cmd)
        {
            XmlElement res = resDoc.CreateElement("response");
            SQLiteDataReader sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT player_id FROM Users WHERE username=@username", new SQLiteParameter("@username", url["username"]));
            if (sqReader.HasRows)
            {
                //Get the users player id
                XmlElement pid = resDoc.CreateElement("player_id");
                pid.InnerText = DatabaseManager.GetValue(sqReader, "player_id").ToString();
                res.AppendChild(pid);
            }
            sqReader.Close();
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool MailMessageCreateHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc, SQLiteCommand sqlite_cmd)
        {
            XmlElement res = resDoc.CreateElement("response");
            int id = DatabaseManager.RandomID();
            //Log message to database
            DatabaseManager.NonQuery(sqlite_cmd, "INSERT INTO Mail_Messages VALUES(@id,@type,@reply_id,@recipient_list,@subject,@body,@attachment_reference)"
                , new SQLiteParameter("@id", id)
                , new SQLiteParameter("@type", tryGetParameter(url, "mail_message[mail_message_type]"))
                , new SQLiteParameter("@recipient_list", url["mail_message[recipient_list]"])
                , new SQLiteParameter("@subject", url["mail_message[subject]"])
                , new SQLiteParameter("@body", url["mail_message[body]"])
                , new SQLiteParameter("@attachment_reference", tryGetParameter(url, "mail_message[attachment_reference]"))
                , new SQLiteParameter("@reply_id", tryGetParameter(url, "reply_to_mail_message_id")));
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool TagListHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc)
        {
            XmlElement res = resDoc.CreateElement("response");
            XmlDocumentFragment tags = resDoc.CreateDocumentFragment();
            //TODO: Read this from file (Though unless you wanted extra tags can likely just be fixed)
            tags.InnerXml = ServerValues.tags;
            res.AppendChild(tags);
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool PlayerMetricShowHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc)
        {
            XmlElement res = resDoc.CreateElement("response");
            //XmlDocumentFragment metric = resDoc.CreateDocumentFragment();
            ////TODO: Store/get player metrics
            //metric.InnerXml = "<player_metrics total=\"1\"><player_metric points=\"1500.0\" volatility=\"0.06\" player_id=\"0\" deviation=\"350.0\" num_games=\"0\"/></player_metrics>";
            //res.AppendChild(metric);
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool PlayerMetricUpdateHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc)
        {
            XmlElement res = resDoc.CreateElement("response");
            //TODO: Store/get player metrics
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool MailMessageListHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc)
        {
            XmlElement res = resDoc.CreateElement("response");
            XmlElement mailmessages = resDoc.CreateElement("mail_messages");
            //TODO
            mailmessages.SetAttribute("page", "1");
            mailmessages.SetAttribute("player_id", "0");
            mailmessages.SetAttribute("row_end", "0");
            mailmessages.SetAttribute("row_start", "0");
            mailmessages.SetAttribute("total", "0");
            mailmessages.SetAttribute("total_pages", "0");
            mailmessages.SetAttribute("unread_count", "0");
            //TODO: Get messages
            res.AppendChild(mailmessages);
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool AchievementListHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc)
        {
            XmlElement res = resDoc.CreateElement("response");
            XmlElement mailmessages = resDoc.CreateElement("achievements");
            //TODO
            mailmessages.SetAttribute("total", "0");
            //TODO: Get messages
            res.AppendChild(mailmessages);
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool ContentUpdateLatestHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc)
        {
            XmlElement res = resDoc.CreateElement("response");
            //I dont know if this is needed for dlc to show up or not, might be for coming attractions stand
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool LeaderboardViewHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc)
        {
            XmlElement res = resDoc.CreateElement("response");
            //TODO: Leaderboards
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool LeaderboardPlayerStatsHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc)
        {
            XmlElement res = resDoc.CreateElement("response");
            //TODO: Leaderboards
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool AnnouncementListHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc, SQLiteCommand sqlite_cmd)
        {
            XmlElement res = resDoc.CreateElement("response");
            XmlElement announcementlist = resDoc.CreateElement("announcements");
            //TODO: Get annoucnements from database
            SQLiteDataReader sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT * FROM Announcements WHERE enabled='true'");
            int count = 0;
            while (sqReader.HasRows)
            {
                XmlElement announcement = resDoc.CreateElement("announcement");
                announcement.SetAttribute("id", DatabaseManager.GetValue(sqReader, "id").ToString());
                announcement.SetAttribute("subject", DatabaseManager.GetValue(sqReader, "subject").ToString());
                announcement.SetAttribute("language_code", DatabaseManager.GetValue(sqReader, "language_code").ToString());
                announcement.SetAttribute("created_at", ((DateTime)DatabaseManager.GetValue(sqReader, "created_at")).ToString("yyyy-MM-ddTHH:mm:ss+00:00"));
                announcement.InnerText = DatabaseManager.GetValue(sqReader, "body").ToString();
                announcementlist.AppendChild(announcement);
                sqReader.Read();
                count++;
            }
            announcementlist.SetAttribute("total", count.ToString());
            res.AppendChild(announcementlist);
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool PlayerInfoHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc, SQLiteCommand sqlite_cmd)
        {
            XmlElement res = resDoc.CreateElement("response");
            XmlElement playerinfo = resDoc.CreateElement("player");
            string sessionId = "";
            foreach (KeyValuePair<string, SessionManager.SessionPlayer> item in SessionManager.players)
            {
                if (item.Value.player_id.ToString() == url["id"])
                {
                    sessionId = item.Key;
                    break;
                }
            }
            SQLiteDataReader sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT * FROM Users WHERE player_id=@player_id;", new SQLiteParameter("@player_id", url["id"]));
            if (sqReader.HasRows)
            {
                playerinfo.SetAttribute("player_id", DatabaseManager.GetValue(sqReader, "player_id").ToString());
                playerinfo.SetAttribute("username", DatabaseManager.GetValue(sqReader, "username").ToString());
                //playerinfo.SetAttribute("rating", DatabaseManager.GetValue(sqReader, "rating").ToString());
                //playerinfo.SetAttribute("star_rating", DatabaseManager.GetValue(sqReader, "rating").ToString());    //???
                playerinfo.SetAttribute("created_at", DatabaseManager.GetValue(sqReader, "created_at").ToString());
                playerinfo.SetAttribute("experience_points", DatabaseManager.GetValue(sqReader, "experience_points").ToString());
                playerinfo.SetAttribute("experience_points_this_week", DatabaseManager.GetValue(sqReader, "experience_points_this_week").ToString());
                playerinfo.SetAttribute("experience_points_last_week", DatabaseManager.GetValue(sqReader, "experience_points_last_week").ToString());
                if (SessionManager.players.ContainsKey(sessionId)) { playerinfo.SetAttribute("presence", SessionManager.players[sessionId].presence); }
                else { playerinfo.SetAttribute("presence", "OFFLINE"); }
                playerinfo.SetAttribute("skill_level_id", DatabaseManager.GetValue(sqReader, "skill_level_id").ToString());
                playerinfo.SetAttribute("skill_level_name", DatabaseManager.GetValue(sqReader, "skill_level_name").ToString());
                playerinfo.SetAttribute("skill_level", DatabaseManager.GetValue(sqReader, "skill_level").ToString());
                playerinfo.SetAttribute("player_creation_quota", DatabaseManager.GetValue(sqReader, "player_creation_quota").ToString());
                playerinfo.SetAttribute("creator_points", DatabaseManager.GetValue(sqReader, "creator_points").ToString());
                playerinfo.SetAttribute("creator_points_this_week", DatabaseManager.GetValue(sqReader, "creator_points_this_week").ToString());
                playerinfo.SetAttribute("creator_points_last_week", DatabaseManager.GetValue(sqReader, "creator_points_last_week").ToString());
                playerinfo.SetAttribute("total_player_creations", DatabaseManager.GetValue(sqReader, "total_player_creations").ToString());
                playerinfo.SetAttribute("total_tracks", DatabaseManager.GetValue(sqReader, "total_tracks").ToString());
                playerinfo.SetAttribute("total_karts", DatabaseManager.GetValue(sqReader, "total_karts").ToString());
                playerinfo.SetAttribute("total_characters", DatabaseManager.GetValue(sqReader, "total_characters").ToString());
                playerinfo.SetAttribute("quote", DatabaseManager.GetValue(sqReader, "quote").ToString());
                playerinfo.SetAttribute("city", DatabaseManager.GetValue(sqReader, "city").ToString());
                playerinfo.SetAttribute("state", DatabaseManager.GetValue(sqReader, "state").ToString());
                playerinfo.SetAttribute("province", DatabaseManager.GetValue(sqReader, "province").ToString());
                playerinfo.SetAttribute("country", DatabaseManager.GetValue(sqReader, "country").ToString());
                playerinfo.SetAttribute("rank", DatabaseManager.GetValue(sqReader, "rank").ToString()); //???
                playerinfo.SetAttribute("points", DatabaseManager.GetValue(sqReader, "points").ToString());
                playerinfo.SetAttribute("online_races", DatabaseManager.GetValue(sqReader, "online_races").ToString());
                playerinfo.SetAttribute("online_races_this_week", DatabaseManager.GetValue(sqReader, "online_races_this_week").ToString());
                playerinfo.SetAttribute("online_races_last_week", DatabaseManager.GetValue(sqReader, "online_races_last_week").ToString());
                playerinfo.SetAttribute("online_wins", DatabaseManager.GetValue(sqReader, "online_wins").ToString());
                playerinfo.SetAttribute("online_wins_this_week", DatabaseManager.GetValue(sqReader, "online_wins_this_week").ToString());
                playerinfo.SetAttribute("online_wins_last_week", DatabaseManager.GetValue(sqReader, "online_wins_last_week").ToString());
                playerinfo.SetAttribute("online_finished", DatabaseManager.GetValue(sqReader, "online_finished").ToString());
                playerinfo.SetAttribute("online_finished_this_week", DatabaseManager.GetValue(sqReader, "online_finished_this_week").ToString());
                playerinfo.SetAttribute("online_finished_last_week", DatabaseManager.GetValue(sqReader, "online_finished_last_week").ToString());
                playerinfo.SetAttribute("online_forfiet", DatabaseManager.GetValue(sqReader, "online_forfiet").ToString());
                playerinfo.SetAttribute("online_disconnected", DatabaseManager.GetValue(sqReader, "online_disconnected").ToString());
                playerinfo.SetAttribute("win_streak", DatabaseManager.GetValue(sqReader, "win_streak").ToString());
                playerinfo.SetAttribute("longest_win_streak", DatabaseManager.GetValue(sqReader, "longest_win_streak").ToString());
                playerinfo.SetAttribute("longest_drift", DatabaseManager.GetValue(sqReader, "longest_drift").ToString());
                playerinfo.SetAttribute("longest_hang_time", DatabaseManager.GetValue(sqReader, "longest_hang_time").ToString());
            }
            sqReader.Close();
            //Rating
            sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT rating FROM User_Ratings WHERE player_id=@id"
               , new SQLiteParameter("@id", url["id"]));
            float rating = 0;
            int count = 0;
            while (sqReader.HasRows)
            {
                rating += sqReader.GetFloat(0);
                count++;
                sqReader.Read();
            }
            sqReader.Close();
            playerinfo.SetAttribute("star_rating", (rating / count).ToString());
            playerinfo.SetAttribute("rating", (rating / count).ToString());
            res.AppendChild(playerinfo);
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool PlayerRatingCreateHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc, SQLiteCommand sqlite_cmd)
        {
            XmlElement res = resDoc.CreateElement("response");
            DatabaseManager.NonQuery(sqlite_cmd, "INSERT INTO User_Ratings VALUES (@player_id,@rating)"
                , new SQLiteParameter("@player_id", url["player_rating[player_id]"])
                , new SQLiteParameter("@rating", url["player_rating[rating]"]));
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool PlayerComplaintCreateHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc, SQLiteCommand sqlite_cmd)
        {
            XmlElement res = resDoc.CreateElement("response");
            //Log a player complaint to the database
            DatabaseManager.NonQuery(sqlite_cmd, "INSERT INTO User_Complaints VALUES (@player_id,@player_comments,@player_complaint_reason)"
                , new SQLiteParameter("@player_id", url["player_complaint[player_id]"])
                , new SQLiteParameter("@player_comments", url["player_complaint[player_comments]"])
                , new SQLiteParameter("@player_complaint_reason", url["player_complaint[player_complaint_reason]"]));
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool FavoritePlayerCreateHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc, SQLiteCommand sqlite_cmd)
        {
            XmlElement res = resDoc.CreateElement("response");
            //Log a favorite player into the database
            DatabaseManager.NonQuery(sqlite_cmd, "INSERT INTO User_Favorite VALUES (@player_id,@favorite_player_id)"
                , new SQLiteParameter("@player_id", SessionManager.players[SessionManager.GetSessionID(request.Cookies["playerconnect_session_id"].Value)].player_id.ToString())
                , new SQLiteParameter("@favorite_player_id", GetIDFromUserName(url["favorite_player[username]"], sqlite_cmd)));
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool FavoritePlayerListHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc, SQLiteCommand sqlite_cmd)
        {
            XmlElement res = resDoc.CreateElement("response");
            XmlElement favplayers = resDoc.CreateElement("favorite_players");
            //Log a favorite player into the database
            SQLiteDataReader sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT * FROM User_Favorite WHERE player_id=@player_id"
                , new SQLiteParameter("@player_id", url["player_id"]));
            int count = 0;
            while (sqReader.HasRows)
            {
                XmlElement favplayer = resDoc.CreateElement("favorite_player");
                favplayer.SetAttribute("favorite_player_id", (count + 1).ToString());
                favplayer.SetAttribute("id", DatabaseManager.GetValue(sqReader, "favorite_player_id").ToString());
                favplayers.AppendChild(favplayer);
                sqReader.Read();
                count++;
            }
            sqReader.Close();
            foreach (XmlElement element in favplayers.ChildNodes)
            {
                element.SetAttribute("username", GetUserNameFromID(element.Attributes["id"].InnerText, sqlite_cmd).ToString());
            }
            favplayers.SetAttribute("total", count.ToString());
            res.AppendChild(favplayers);
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool FavoritePlayerRemoveHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc, SQLiteCommand sqlite_cmd)
        {
            XmlElement res = resDoc.CreateElement("response");
            //Log a favorite player into the database
            DatabaseManager.NonQuery(sqlite_cmd, "DELETE FROM User_Favorite WHERE player_id=@player_id AND favorite_player_id=@favorite_player_id"
                , new SQLiteParameter("@player_id", SessionManager.players[SessionManager.GetSessionID(request.Cookies["playerconnect_session_id"].Value)].player_id.ToString())
                , new SQLiteParameter("@favorite_player_id", GetIDFromUserName(url["favorite_player[username]"], sqlite_cmd)));
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool PlayerProfileUpdateHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc, SQLiteCommand sqlite_cmd)
        {
            XmlElement res = resDoc.CreateElement("response");
            //Log a favorite player into the database
            DatabaseManager.NonQuery(sqlite_cmd, "UPDATE Users SET quote=@quote WHERE player_id=@player_id"
                , new SQLiteParameter("@quote", url["player_profile[quote]"])
                , new SQLiteParameter("@player_id", SessionManager.players[SessionManager.GetSessionID(request.Cookies["playerconnect_session_id"].Value)].player_id.ToString()));
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool ServerSelectHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc)
        {
            XmlElement res = resDoc.CreateElement("response");
            XmlElement server = resDoc.CreateElement("server");
            XmlElement ticket = resDoc.CreateElement("ticket");
            //TODO: Calculate unique UUIDs and signature
            //TODO: What is "server_private_key"? Someone suggested it could be a NIST P-192 ECC private key
            //(But the SSL seems to work fine without it unless its response only)
            server.SetAttribute("server_type", "DIRECTORY");
            server.SetAttribute("address", Program.ip);
            server.SetAttribute("port", Program.matchingPort.ToString());
            server.SetAttribute("session_uuid", "00000000-0000-0000-0000-000000000000");
            server.SetAttribute("server_private_key", "");
            ticket.SetAttribute("session_uuid", "00000000-0000-0000-0000-000000000000");
            ticket.SetAttribute("player_id", SessionManager.players[SessionManager.GetSessionID(request.Cookies["playerconnect_session_id"].Value)].player_id.ToString());
            ticket.SetAttribute("username", "test");
            ticket.SetAttribute("expiration_date", "Tue Oct 09 23:25:57 +0000 2035");
            ticket.SetAttribute("signature", "");
            server.AppendChild(ticket);
            res.AppendChild(server);
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        public static bool DefaultHandler(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> url, XmlDocument resDoc)
        {
            //Used for requests that dont have a handler
            XmlElement res = resDoc.CreateElement("response");
            resDoc.ChildNodes[0].AppendChild(res);
            return true;
        }

        //public static bool PreferencesUpdateHandler(HttpListenerRequest request, HttpListenerResponse response, XmlElement root, XmlDocument recDoc, XmlDocument resDoc)
        //{
        //    XmlElement preferences = resDoc.CreateElement("preference");
        //    XmlElement domain = resDoc.CreateElement("domain");
        //    XmlElement ipaddress = resDoc.CreateElement("ip_address");
        //    XmlElement lcode = resDoc.CreateElement("language_code");
        //    XmlElement rcode = resDoc.CreateElement("region_code");
        //    XmlElement timezone = resDoc.CreateElement("timezone");
        //    domain.InnerText = "";
        //    ipaddress.InnerText = "::1";
        //    lcode.InnerText = "en-us";
        //    rcode.InnerText = "scea";
        //    timezone.InnerText = "000";
        //    preferences.AppendChild(domain);
        //    preferences.AppendChild(ipaddress);
        //    preferences.AppendChild(lcode);
        //    preferences.AppendChild(rcode);
        //    preferences.AppendChild(timezone);
        //    root.AppendChild(preferences);
        //    return true;
        //}

        static string tryGetParameter(Dictionary<string, string> dict, string value)
        {
            //Gets a parameter but returns an empty string instead of throwing an exception
            try
            {
                return dict[value];
            }
            catch
            {
                return "";
            }
        }

        public static DateTime StartOfWeek(DateTime dt, DayOfWeek startOfWeek)
        {
            DateTime firstDayInWeek = dt.Date;
            while (firstDayInWeek.DayOfWeek != startOfWeek)
                firstDayInWeek = firstDayInWeek.AddDays(-1);

            return firstDayInWeek;
        }

        public static DateTime EndOfWeek(this DateTime dt, DayOfWeek endOfWeek)
        {
            DateTime lastDayInWeek = dt.Date;
            while (lastDayInWeek.DayOfWeek != endOfWeek + 1)
                lastDayInWeek = lastDayInWeek.AddDays(1);

            return lastDayInWeek;
        }

        public static string GetUserNameFromID(string ID, SQLiteCommand sqlite_cmd)
        {
            SQLiteDataReader sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT username FROM Users WHERE player_id=@id", new SQLiteParameter("@id", ID));
            string username = "";
            if (sqReader.HasRows)
            {
                username = sqReader.GetString(0);
            }
            sqReader.Close();
            return username;
        }

        public static string GetIDFromUserName(string name, SQLiteCommand sqlite_cmd)
        {
            SQLiteDataReader sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT player_id FROM Users WHERE username=@name", new SQLiteParameter("@name", name));
            string ID = "";
            if (sqReader.HasRows)
            {
                ID = sqReader.GetInt64(0).ToString();
            }
            sqReader.Close();
            return ID;
        }

        public static string GetCreationNameFromID(string ID, SQLiteCommand sqlite_cmd)
        {
            SQLiteDataReader sqReader = DatabaseManager.GetReader(sqlite_cmd, "SELECT name FROM Player_Creations WHERE id=@id", new SQLiteParameter("@id", ID));
            string creationname = "";
            if (sqReader.HasRows)
            {
                creationname = sqReader.GetString(0);
            }
            sqReader.Close();
            return creationname;
        }
    }
}
