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
                sqlite_cmd.CommandText = "CREATE TABLE Users(player_id int, username varchar, city varchar, country varchar, created_at datetime, creator_points varchar" +
                    ", creator_points_last_week varchar, creator_points_this_week varchar, experience_ponts varchar, experience_points_last_week varchar" +
                    ", experience_points_this_week varchar, longest_drift varchar, longest_hang_time datetime, longest_win_streak varchar, online_disconnected varchar" +
                    ", online_finished varchar, online_finished_last_week varchar, online_finished_this_week varchar, online_forfeit varchar, online_races varchar" +
                    ", online_races_last_week varchar, online_races_this_week varchar, online_wins varchar, online_wins_last_week varchar, online_wins_this_week varchar" +
                    ", player_creation_quota varchar, points int, province varchar, quote varchar, rank int, rating int, skill_level varchar, skill_level_id int" +
                    ", skill_level_name int, star_rating varchar, state varchar, total_characters int, total_karts int, total_player_creations int, total_tracks int" +
                    ", win_streak varchar, UNIQUE(player_id));";
                sqlite_cmd.ExecuteNonQuery();
            }
            catch { }
            try
            {
                sqlite_cmd.CommandText = "CREATE TABLE Player_Creations(id int, player_id int, name varchar, description varchar, created_at datetime, rating real, points real, points_today real, points_last_week real" +
                    ", points_this_week real, downloads int, downloads_last_week int, downloads_this_week int, version int, views int, views_last_week int, views_this_week int, tags varchar, player_creation_type varchar, parent_creation_id int" +
                    ", parent_player_id int, original_player_id int, requires_dlc bool, dlc_keys varchar, platform varchar, is_remixable bool, longest_hang_time real, longest_drift float" +
                    ", races_started int, races_won int, votes int, races_finished int, best_lap_time real, track_theme int, auto_reset bool, ai bool, UNIQUE(id));";
                sqlite_cmd.ExecuteNonQuery();
            }
            catch { }
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
            sqlite_conn.Close();
        }
    }
}
