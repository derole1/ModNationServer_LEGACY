using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;

namespace ModNationServer
{
    static class SessionManager
    {
        //PSN Ticket data structure
        public struct NPTicket
        {
            public UInt32 version;
            public byte[] serial_vec;
            public UInt32 issuer_id;
            public DateTime issued_date;
            public DateTime expired_date;
            public UInt64 user_id;
            public string online_id;
            public string region;
            public string domain;
            public string service_id;
            public UInt32 status;
            public UInt32 status_duration;
            public UInt32 date_of_birth;
            public byte[] unk;
        }

        //Data structure for an online player
        public struct SessionPlayer
        {
            public string ip_address;
            public int player_id;
            public string username;
            public string platform;
            public string presence;
            public string encoding;
            public string console_id;
            public string language_code;
            public string timezone;
            public string viewing_platform;
            public string region_code;
            public byte[] last_request_made;
        }

        //List of players online
        public static Dictionary<string, SessionPlayer> players = new Dictionary<string, SessionPlayer>();
        public static Dictionary<string, NPTicket> playerTickets = new Dictionary<string, NPTicket>();

        public static bool CreateSession(string sessionid, string psnticket, SQLiteCommand sqlite_cmd)
        {
            //Decode PSN ticket data
            NPTicket ticket = DecodePSNTicket(psnticket);
            if (players.ContainsKey(sessionid)) { return false; }
            SessionPlayer player = new SessionPlayer();
            //player.player_id = onlineid;
            //player.username = uname;
            player.presence = "ONLINE";
            players.Add(sessionid, player);
            playerTickets.Add(sessionid, ticket);
            return true;
        }

        public static bool PingSession(string sessionid)
        {
            byte[] ticket = Convert.FromBase64String(sessionid);
            BinaryReader br = new BinaryReader(ticket);
            br.popArray(0x12);
            sessionid = Encoding.ASCII.GetString(br.popArray(0x20));
            return players.ContainsKey(sessionid);
        }

        public static void UpdatePresence(string sessionid, string presence)
        {
            SessionPlayer player = players[sessionid];
            player.presence = presence;
            players[sessionid] = player;
        }

        public static string GetPresence(string sessionid)
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
            sessionID = EncodeInitialSessionID(sessionID);
            return sessionID;
        }

        public static string EncodeInitialSessionID(string sessionID)
        {
            BinaryReader br = new BinaryReader(new byte[0]);
            br.pushUInt32(0x067B0804);
            br.pushUInt16(0x0F3A);
            br.pushArray(Encoding.ASCII.GetBytes("session_id"));
            br.pushUInt16(0x2522);
            br.pushArray(Encoding.ASCII.GetBytes(sessionID));
            return Convert.ToBase64String(br.getRes());
        }

        public static string AppendExtendSessionID(string sessionID, out string ID)
        {
            byte[] oldTicket = Convert.FromBase64String(sessionID);
            BinaryReader br = new BinaryReader(oldTicket);
            br.popArray(0x12);
            ID = Encoding.ASCII.GetString(br.popArray(0x20));
            br.pushArray(oldTicket);
            br.insertUInt32(0x117B0804, 0);
            //TODO: Properly figure out data structure
            AppendPlayerConnectParameter(br, "ip_address", "127.0.0.1", 0x0F3A, 0x1122);
            AppendPlayerConnectParameter(br, "player_id", "\x01\x00", 0x0F3A, 0x1122);
            AppendPlayerConnectParameter(br, "username", "test", 0x0D3A, 0x0D22);
            AppendPlayerConnectParameter(br, "platform", "PS3", 0x0D3A, 0x0822);
            AppendPlayerConnectParameter(br, "presenceI", "ONLINE", 0x0D3A, 0x0B22);   //???
            AppendPlayerConnectParameter(br, "encoding", "US-ASCII", 0x0D3A, 0x0D22);
            AppendPlayerConnectParameter(br, "console_id", "000:000:000:000:000:000", 0x0F3A, 0x1C22);
            AppendPlayerConnectParameter(br, "language_code", "en-us", 0x123A, 0x0A22);
            AppendPlayerConnectParameter(br, "timezone", "PST -0800", 0x0D3A, 0x0E22);
            AppendPlayerConnectParameter(br, "viewing_platform", "PS3", 0x153A, 0x0822);
            AppendPlayerConnectParameter(br, "region_code", "scea", 0x103A, 0x0922);
            AppendPlayerConnectParameter(br, "last_request_made", "\x38\xBD\x5B", 0x163A, 0x07E2);
            return Convert.ToBase64String(br.getRes());
        }

        static NPTicket DecodePSNTicket(string strTicket)
        {
            while (strTicket.Length % 4 != 0) { strTicket += "="; }
            byte[] binTicket = Convert.FromBase64String(strTicket);
            BinaryReader br = new BinaryReader(binTicket);
            NPTicket ticket = new NPTicket();
            ticket.version = br.popUInt32();
            br.popArray(0x08);  //Skip header
            br.popArray(0x04);
            ticket.serial_vec = br.popArray(0x14);
            br.popArray(0x04);
            ticket.issuer_id = br.popUInt32();
            br.popArray(0x04);
            ticket.issued_date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(br.popUInt64());
            br.popArray(0x04);
            ticket.expired_date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(br.popUInt64());
            br.popArray(0x04);
            ticket.user_id = br.popUInt64();
            br.popArray(0x04);
            ticket.online_id = Encoding.ASCII.GetString(br.popArray(0x20)).Trim('\0');
            br.popArray(0x04);
            ticket.region = Encoding.ASCII.GetString(br.popArray(0x04)).Trim('\0');
            br.popArray(0x04);
            ticket.domain = Encoding.ASCII.GetString(br.popArray(0x04)).Trim('\0');
            br.popArray(0x04);
            ticket.service_id = Encoding.ASCII.GetString(br.popArray(0x18)).Trim('\0');
            br.popArray(0x04);
            ticket.status = br.popUInt32();
            br.popArray(0x04);
            ticket.status_duration = br.popUInt32();
            br.popArray(0x04);
            ticket.date_of_birth = br.popUInt32();
            br.popArray(0x04);
            ticket.unk = br.popArray(0x44);
            return ticket;
        }

        static void AppendPlayerConnectParameter(BinaryReader br, string name, string value, UInt16 nameType, UInt16 valueType)
        {
            br.pushUInt16(nameType);
            br.pushArray(Encoding.ASCII.GetBytes("ip_address"));
            br.pushUInt16(valueType);
            br.pushArray(Encoding.ASCII.GetBytes("127.0.0.1"));
        }
    }
}
