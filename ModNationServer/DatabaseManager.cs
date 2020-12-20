using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;

namespace ModNationServer
{
    static class DatabaseManager
    {
        //Using local database file
        public static string connectionString = "Data Source=database.sqlite;Version=3;";

        public static SQLiteDataReader GetReader(SQLiteCommand sqlite_cmd, string query, params SQLiteParameter[] queryParams)
        {
            sqlite_cmd.CommandText = query;
            foreach (SQLiteParameter param in queryParams)
            {
                sqlite_cmd.Parameters.Add(param);
            }
            Console.WriteLine(sqlite_cmd.CommandText);
            SQLiteDataReader sqReader = sqlite_cmd.ExecuteReader();
            sqReader.Read();
            return sqReader;
        }
        public static int NonQuery(SQLiteCommand sqlite_cmd, string query, params SQLiteParameter[] queryParams)
        {
            sqlite_cmd.CommandText = query;
            foreach (SQLiteParameter param in queryParams)
            {
                sqlite_cmd.Parameters.Add(param);
            }
            return sqlite_cmd.ExecuteNonQuery();
        }

        public static object GetValue(SQLiteDataReader sqReader, string columnName)
        {
            try
            {
                return sqReader.GetValue(sqReader.GetOrdinal(columnName));
            }
            catch
            {
                return 0;
            }
        }

        public static string SanitizeString(string input, bool likesearch = false)
        {
            string output = input.Replace("'", "''").Replace("\"", "\"\"");
            if (likesearch) { output.Replace("%", ""); }
            return output;
        }

        static Random random = new Random();
        public static int RandomID()
        {
            return random.Next(0, int.MaxValue);
        }

        //Would probably be better to use an SQL script for this
        public static void performDBUpgrade()
        {
            Console.WriteLine("Rebuilding/Upgrading database");
            SQLiteConnection sqlite_conn = new SQLiteConnection(connectionString);
            sqlite_conn.Open();
            SQLiteCommand sqlite_cmd = sqlite_conn.CreateCommand();
            try
            {
                sqlite_cmd.CommandText = "CREATE TABLE Users(player_id bigint, username varchar, city varchar, country varchar, created_at datetime, creator_points int" +
                    ", creator_points_last_week int, creator_points_this_week int, experience_ponts int, experience_points_last_week int" +
                    ", experience_points_this_week int, longest_drift real, longest_hang_time real, longest_win_streak int, online_disconnected int" +
                    ", online_finished int, online_finished_last_week int, online_finished_this_week int, online_forfeit int, online_races int" +
                    ", online_races_last_week int, online_races_this_week int, online_wins int, online_wins_last_week int, online_wins_this_week int" +
                    ", player_creation_quota int, points real, province varchar, quote varchar, rank int, rating real, skill_level varchar, skill_level_id int" +
                    ", skill_level_name varchar, state varchar, total_characters int, total_karts int, total_player_creations int, total_tracks int" +
                    ", win_streak int, UNIQUE(player_id));";
                sqlite_cmd.ExecuteNonQuery();
            }
            catch { }
            try
            {
                sqlite_cmd.CommandText = "CREATE TABLE User_Ratings(player_id bigint, rating real);";
                sqlite_cmd.ExecuteNonQuery();
            }
            catch { }
            try
            {
                sqlite_cmd.CommandText = "CREATE TABLE User_Favorite(player_id bigint, favorite_player_id bigint);";
                sqlite_cmd.ExecuteNonQuery();
            }
            catch { }
            try
            {
                sqlite_cmd.CommandText = "CREATE TABLE User_Complaints(player_id int, player_comments varchar, player_complaint_reason varchar);";
                sqlite_cmd.ExecuteNonQuery();
            }
            catch { }
            try
            {
                sqlite_cmd.CommandText = "CREATE TABLE Player_Creations(id int, player_id int, name varchar, description varchar, created_at datetime, updated_at datetime, downloads int, downloads_this_week int, downloads_last_week int" +
                    ", views int, views_this_week int, views_last_week int, points int, points_today int, points_yesterday int, points_this_week int, points_last_week int, rating real, version int, tags varchar, player_creation_type varchar" +
                    ", parent_creation_id int, parent_player_id int, original_player_id int, requires_dlc bool, dlc_keys varchar, platform varchar, is_remixable bool, longest_hang_time real, longest_drift real" +
                    ", races_started int, races_won int, votes int, races_finished int, best_lap_time real, track_theme int, auto_reset bool, ai bool, deleted bool, UNIQUE(id));";
                sqlite_cmd.ExecuteNonQuery();
            }
            catch { }
            try
            {
                sqlite_cmd.CommandText = "CREATE TABLE Player_Creation_Ratings(id int, player_id bigint, rating real, comments varchar);";
                sqlite_cmd.ExecuteNonQuery();
            }
            catch { }
            //try
            //{
            //    sqlite_cmd.CommandText = "CREATE TABLE Player_Creation_Views(id int, created_at datetime);";
            //    sqlite_cmd.ExecuteNonQuery();
            //}
            //catch { }
            //try
            //{
            //    sqlite_cmd.CommandText = "CREATE TABLE Player_Creation_Downloads(id int, created_at datetime);";
            //    sqlite_cmd.ExecuteNonQuery();
            //}
            //catch { }
            //try
            //{
            //    sqlite_cmd.CommandText = "CREATE TABLE Player_Creation_Points(id int, player_id int, created_at datetime);";
            //    sqlite_cmd.ExecuteNonQuery();
            //}
            //catch { }
            try
            {
                sqlite_cmd.CommandText = "CREATE TABLE Player_Metric(id int, metric varchar, UNIQUE(id));";
                sqlite_cmd.ExecuteNonQuery();
            }
            catch { }
            try
            {
                sqlite_cmd.CommandText = "CREATE TABLE Player_Creations_Complaints(owner_player_id int, player_comments varchar, player_complaint_reason varchar, player_creation_id int);";
                sqlite_cmd.ExecuteNonQuery();
            }
            catch { }
            try
            {
                sqlite_cmd.CommandText = "CREATE TABLE Mail_Messages(id int, type varchar, reply_id int, recipient_list varchar, subject varchar, body varchar, attachment_reference varchar, UNIQUE(id));";
                sqlite_cmd.ExecuteNonQuery();
            }
            catch { }
            try
            {
                sqlite_cmd.CommandText = "CREATE TABLE Announcements(id int, language_code varchar, subject varchar, body varchar, created_at datetime, enabled bool, UNIQUE(id));";
                sqlite_cmd.ExecuteNonQuery();
            }
            catch { }
            sqlite_conn.Close();
        }
    }
}
