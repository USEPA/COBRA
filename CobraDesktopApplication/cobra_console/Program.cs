using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EntityFramework.BulkInsert.Extensions;
using System.Data.SQLite;
using cobra_console.units;
using System.Configuration;
using System.Data.Entity.Core.EntityClient;
using CommandLine;
using CommandLine.Text;

namespace cobra_console
{
    class Program
    {
        //cobra_console.exe -db 'Path to SQLite' -b 'Baseline' -c 'Control' -p 'Population' –i 'Incidence' -o 'Outputfile' –pct3 'YES/NO' -y '2016/2023/2028'
        class Options
        {
            [Option('d', "db", Required = true, HelpText = "SQLITE database to be used.")]
            public string Database { get; set; }

            [Option('b', "baseline", Required = true, HelpText = "Baseline file to be used.")]
            public string BaselineFile { get; set; }

            [Option('c', "control", Required = true, HelpText = "Control file to be used.")]
            public string ControlFile { get; set; }

            [Option('p', "population", Required = true, Default = "", HelpText = "Population file to be used.")]
            public string PopulationFile { get; set; }

            [Option('i', "PM Incidence", Required = true, Default = "", HelpText = "PM Incidence file to be used.")]
            public string IncidenceFile { get; set; }

            [Option('v', "Valuation", Required = true, Default = "", HelpText = "Valuation file to be used.")]
            public string ValuationFile { get; set; }

            [Option('o', "output", Required = true, HelpText = "Output file to be written.")]
            public string OutputFile { get; set; }

            [Option("discountrate", Required = true, HelpText = "Enter discount rate from 1-100 (%)?")]
            public string DiscountRate { get; set; }

        }

        public class FormatHelper
        {
            public static Boolean StringToBoolean(String str)
            {
                return StringToBoolean(str, false);
            }

            public static Boolean StringToBoolean(String str, Boolean bDefault)
            {
                String[] BooleanStringOff = { "0", "off", "no" };

                if (String.IsNullOrEmpty(str))
                    return bDefault;
                else if (BooleanStringOff.Contains(str, StringComparer.InvariantCultureIgnoreCase))
                    return false;

                Boolean result;
                if (!Boolean.TryParse(str, out result))
                    result = true;

                return result;
            }
        }

        static void Main(string[] args)
        {
            try
            {
                //var res = CommandLine.Parser.Default.ParseArguments<Options>(args);


                CommandLine.Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       Console.WriteLine("Beginning run at: " + System.DateTime.Now.ToString());
                       Console.WriteLine("Parameters are: " + String.Join(" ", args));

                       //bool usePct3 = FormatHelper.StringToBoolean(o.Pct3.ToLower());

                       double discountRate = 2;

                       if (double.TryParse(o.DiscountRate, out double doubleValue))
                       {

                           // Conversion succeeded, and doubleValue contains the parsed double
                           discountRate = doubleValue;
                       }
                       else
                       {
                           // Conversion failed, handle the case where the string cannot be converted to double
                           Console.WriteLine("Cannot convert inputted discountrate to double, using default 2% rate");
                       }

                       string connString = ConfigurationManager.ConnectionStrings["cobraEntities"].ConnectionString;
                       var connectionStringBuilder = new EntityConnectionStringBuilder(connString);
                       connectionStringBuilder.ProviderConnectionString = String.Format("data source={0}", o.Database);
                       string EF_connectionstring = connectionStringBuilder.ConnectionString;

                       string ConnectionString = new SQLiteConnectionStringBuilder() { DataSource = o.Database, Version = 3 }.ConnectionString;
                       Globals.connectionstring = ConnectionString;

                       SQLiteConnection dbConn = new SQLiteConnection(ConnectionString);
                       dbConn.Open();

                       /* load emissions files into the appropriate db tables */
                       Console.WriteLine("Loading baseline data at: " + System.DateTime.Now.ToString());
                       Dataloader loader = new Dataloader(dbConn);
                       loader.loadfileintoemissions(o.BaselineFile, "SYS_Emissions_Base");
                       Console.WriteLine("Loading control data at: " + System.DateTime.Now.ToString());
                       loader.loadfileintoemissions(o.ControlFile, "SYS_Emissions_Control");
                       loader = null;


                       Console.WriteLine("Preparing to compute air quality at: " + System.DateTime.Now.ToString());
                       AQProcessor processor = new AQProcessor(dbConn, EF_connectionstring);

                       /* summarize emissions */
                       Console.WriteLine("Computing air quality step 1 at: " + System.DateTime.Now.ToString());
                       processor.summarize("SYS_Emissions_Base", "SYS_Emissions_Base_Summarized", "SYS_Destination_Base");
                       Console.WriteLine("Computing air quality step 2 at: " + System.DateTime.Now.ToString());
                       processor.summarize("SYS_Emissions_Control", "SYS_Emissions_Control_Summarized", "SYS_Destination_Control");
                       Console.WriteLine("Computing air quality step 3 at: " + System.DateTime.Now.ToString());
                       processor.finalize("SYS_Destination_Base", "SYS_Destination_Control");

                       Console.WriteLine("Computing impacts at: " + System.DateTime.Now.ToString());
                       ImpactProcessor impactprocessor = new ImpactProcessor(dbConn, EF_connectionstring);
                       // modelyear doesnt actually matter in batch/console mode because all data will be provided by user and we are assuming the data is already properly subset by year
                       impactprocessor.ComputeImpactsForYear(2016, false, discountRate, o.PopulationFile, o.IncidenceFile, "", o.ValuationFile, o.OutputFile);

                       dbConn.Close();
                       Console.WriteLine("Completed at at: " + System.DateTime.Now.ToString());
                       Console.WriteLine("Output file is: " + o.OutputFile);
                       Environment.Exit(0);

                   })
                   .WithNotParsed<Options>((errs) =>
                   {
                       Console.WriteLine("Terminated at at: " + System.DateTime.Now.ToString());
                       Environment.Exit(1);

                   });

            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
                Console.WriteLine("Terminated at at: " + System.DateTime.Now.ToString());
                Environment.Exit(1);
            }
        }
    }
}
