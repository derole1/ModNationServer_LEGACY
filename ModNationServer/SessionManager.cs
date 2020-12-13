using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;

namespace ModNationServer
{
    static class SessionManager
    {
        //Would probs make more sense just to compare strings tbh
        public enum presenceType
        {
            CAREER_CHALLENGE = 0,
            CASUAL_RACE = 1,
            IDLING = 2,
            INGAME = 3,
            IN_STUDIO = 4,
            KART_PARK_CHALLENGE = 5,
            LOBBY = 6,
            OFFLINE = 7,
            ONLINE = 8,
            RANKED_RACE = 9,
            ROAMING = 10,
            WEB = 11
        }

        //Data structure for an online player
        struct SessionPlayer
        {
            public int online_id;
            public string username;
            public presenceType presence;
        }

        //List of players online
        static Dictionary<string, SessionPlayer> players = new Dictionary<string, SessionPlayer>();

        public static bool CreateSession(string sessionid, string ticket, SQLiteCommand sqlite_cmd, out int playerid, out string username)
        {
            //Decode PSN ticket data
            //NOTE: I dont actually know where the PSN ID is, I just guessed
            //byte[] ticketData = Convert.FromBase64String(ticket);
            //BinaryReader br = new BinaryReader(ticketData);
            //br.popArray(16);
            if (players.ContainsKey(sessionid))
            {
                playerid = 0;
                username = null;
                return false;
            }
            int onlineid = 0;
            string uname = "derole";
            //Some database code
            //SQLiteDataReader sqReader = DatabaseManager.GetReader(sqlite_cmd
            //    , "SELECT player_id, username FROM Users WHERE psn_id=@psnid;"
            //    , new SQLiteParameter("@psnid", br.popString()));
            //string psnname = Encoding.UTF8.GetString(ticketData, 0x54, 0x64);
            //if (sqReader.HasRows)
            //{
            //    onlineid = (int)DatabaseManager.GetValue(sqReader, "player_id");
            //    uname = (string)DatabaseManager.GetValue(sqReader, "username");
            //}
            //else
            //{
            //    onlineid = DatabaseManager.RandomID();
            //    uname = psnname;
            //    DatabaseManager.NonQuery(sqlite_cmd, "INSERT INTO Users VALUES(@pid,@uname,)");
            //}
            SessionPlayer player = new SessionPlayer();
            player.online_id = onlineid;
            player.username = uname;
            player.presence = presenceType.ONLINE;
            players.Add(sessionid, player);
            playerid = onlineid;
            username = uname;
            return true;
        }

        public static bool PingSession(string sessionid)
        {
            return players.ContainsKey(sessionid);
        }

        public static void UpdatePresence(string sessionid, presenceType presence)
        {
            SessionPlayer player = players[sessionid];
            player.presence = presence;
            players[sessionid] = player;
        }

        public static presenceType GetPresence(string sessionid)
        {
            return players[sessionid].presence;
        }

        static Random random = new Random();
        public static string RandomSessionID(int length)
        {
            //Create a session ID
            //NOTE: The original server encoded the ID as base64 with some player details, for now we dont care about this, and I dont think the game requires it anyways
            string sessionID = "";
            do
            {
                const string chars = "abcdef0123456789";
                sessionID = new string(Enumerable.Repeat(chars, length)
                  .Select(s => s[random.Next(s.Length)]).ToArray());
            } while (players.ContainsKey(sessionID));
            return sessionID;
        }
    }
}
