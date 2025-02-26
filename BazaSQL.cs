using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BazaRezystoryR
{
    internal class BazaSQL
    {
        //Postresql
        static NpgsqlConnection sqlConn;
        static String sqlQuery;
        static NpgsqlCommand sqlCmd;
        static NpgsqlDataReader sqlRd;


        static DataSet DS = new DataSet();

        static String server = "10.10.254.1";//"10.10.254.1";
        static String port = "5432";
        static String username = "actuators";
        static String password = "jsaL6QjkYj8VSa4Z";
        static String database = "actuators";
        static String timeout = "5";

        public static String MyConnectionString = $"Server={server};Port={port};User Id={username} ;Password={password};Database={database};Timeout={timeout}";


        public static bool CheckConnection()
        {
            sqlConn = new NpgsqlConnection(MyConnectionString);

            try
            {
                sqlConn.Open();
                sqlConn.Close();
                return true;
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("Serwer SQL niedostępny.", "Wiadomość", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            finally
            {
                sqlConn.Close();
            }
        }
    }
}
