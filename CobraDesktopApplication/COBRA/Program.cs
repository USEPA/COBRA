using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;
using System.Data.Entity.Core.EntityClient;
using cobra_console.units;

namespace COBRA
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool result;
            var mutex = new System.Threading.Mutex(true, "9836A7B9 - 6EB4 - 4E89 - 8DA7 - 72309593FBC8", out result);

            if (!result)
            {
                MessageBox.Show("Another instance of COBRA is already running. You can only run one instance at a time.");
                return;
            }



            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string appdblocation = AppDomain.CurrentDomain.BaseDirectory + "\\data\\cobra.db";

            //some more setup set path for EF, sqlite and type datasets
            //executeQuery("PRAGMA journal_mode = OFF");
            //executeQuery("PRAGMA synchronous = OFF");
            Globals.connectionstring = "data source=" + appdblocation + ";Version=3;Synchronous=OFF;Journal Mode=OFF;cache_size=400000";

            string connString = ConfigurationManager.ConnectionStrings["cobraEntities"].ConnectionString;
            var connectionStringBuilder = new EntityConnectionStringBuilder(connString);
            connectionStringBuilder.ProviderConnectionString = String.Format("data source={0};Version=3;Journal Mode=Off;Synchronous=OFF", appdblocation);
            Globals.connectionstring_EF = connectionStringBuilder.ConnectionString;

            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var connectionStringsSection = (ConnectionStringsSection)config.GetSection("connectionStrings");
            connectionStringsSection.ConnectionStrings["COBRA.Properties.Settings.cobraConnectionString"].ConnectionString = Globals.connectionstring;
            config.Save();
            ConfigurationManager.RefreshSection("connectionStrings");

            //some housekeeping
            SQLiteConnection dbConn = new SQLiteConnection(Globals.connectionstring);
            dbConn.Open();
            Dataloader loader = new Dataloader(dbConn);
            loader.executeQuery("DELETE from SYS_Emissions");
            loader.executeQuery("DELETE from SYS_Results");
            dbConn.Close();


            Application.Run(new MainGUI());

            GC.KeepAlive(mutex);
        }
    }
}
