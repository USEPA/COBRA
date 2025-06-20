using CsvHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cobra_console.units
{
    public class Dataloader
    {
        public List<string> messages = new List<string>();

        private SQLiteConnection _dbConn;

        public Dataloader(SQLiteConnection dbConn)
        {
            _dbConn = dbConn;
        }

        public void executeQuery(string query)
        {
            SQLiteCommand cmd = _dbConn.CreateCommand();
            SQLiteTransaction transaction = _dbConn.BeginTransaction();
            cmd.CommandText = query;
            cmd.ExecuteNonQuery();
            transaction.Commit();

        }

        private void clearTable(string tablename)
        {
            executeQuery("delete from " + tablename);
        }


        public void loadpopulationfile(string filename)
        {
            // load to staging
            clearTable("SYS_POP");

            SQLiteBulkLoader loader = new SQLiteBulkLoader(_dbConn, "SYS_POP");
            loader.AddParameter("ID", DbType.Object);
            loader.AddParameter("Year", DbType.Int32);
            loader.AddParameter("DestinationID", DbType.Int32);
            loader.AddParameter("FIPS", DbType.Int32);
            for (int yearidx = 0; yearidx < 100; yearidx = yearidx + 1)
            {
                loader.AddParameter("Age"+yearidx.ToString(), DbType.Double);
            }

            using (TextReader fileReader = File.OpenText(filename))
            {
                CsvReader csv = new CsvReader(fileReader,System.Globalization.CultureInfo.CurrentCulture);
                List<Core_Population> records = csv.GetRecords<Core_Population>().ToList();
                loader.CommitMax = 10000;
                int i = 0;
                foreach (var record in records)
                {
                    object[] data = new object[] { null, record.Year, record.DestinationID, record.FIPS, record.Age0, record.Age1, record.Age2, record.Age3, record.Age4, record.Age5, record.Age6, record.Age7, record.Age8, record.Age9, record.Age10, record.Age11, record.Age12, record.Age13, record.Age14, record.Age15, record.Age16, record.Age17, record.Age18, record.Age19, record.Age20, record.Age21, record.Age22, record.Age23, record.Age24, record.Age25, record.Age26, record.Age27, record.Age28, record.Age29, record.Age30, record.Age31, record.Age32, record.Age33, record.Age34, record.Age35, record.Age36, record.Age37, record.Age38, record.Age39, record.Age40, record.Age41, record.Age42, record.Age43, record.Age44, record.Age45, record.Age46, record.Age47, record.Age48, record.Age49, record.Age50, record.Age51, record.Age52, record.Age53, record.Age54, record.Age55, record.Age56, record.Age57, record.Age58, record.Age59, record.Age60, record.Age61, record.Age62, record.Age63, record.Age64, record.Age65, record.Age66, record.Age67, record.Age68, record.Age69, record.Age70, record.Age71, record.Age72, record.Age73, record.Age74, record.Age75, record.Age76, record.Age77, record.Age78, record.Age79, record.Age80, record.Age81, record.Age82, record.Age83, record.Age84, record.Age85, record.Age86, record.Age87, record.Age88, record.Age89, record.Age90, record.Age91, record.Age92, record.Age93, record.Age94, record.Age95, record.Age96, record.Age97, record.Age98, record.Age99 };
                    loader.Insert(data);
                    i++;
                    if (i % loader.CommitMax == 0)
                    {
                        loader.Flush();
                    }
                }
                loader.Flush();
            }
        }

        public void loadincidencefile(string filename)
        {
            // load to staging
            clearTable("SYS_Incidence");

            SQLiteBulkLoader loader = new SQLiteBulkLoader(_dbConn, "SYS_Incidence");
            loader.AddParameter("ID", DbType.Object);
            loader.AddParameter("Year", DbType.Int32);
            loader.AddParameter("DestinationID", DbType.Int32);
            loader.AddParameter("FIPS", DbType.Int32);
            loader.AddParameter("Endpoint", DbType.String);
            for (int yearidx = 0; yearidx < 100; yearidx = yearidx + 1)
            {
                loader.AddParameter("Age" + yearidx.ToString(), DbType.Double);
            }

            using (TextReader fileReader = File.OpenText(filename))
            {
                CsvReader csv = new CsvReader(fileReader,System.Globalization.CultureInfo.CurrentCulture);
                List<Core_Incidence> records = csv.GetRecords<Core_Incidence>().ToList();
                loader.CommitMax = 10000;
                int i = 0;
                foreach (var record in records)
                {
                    object[] data = new object[] { null, record.Year, record.DestinationID, record.FIPS, record.Endpoint, record.Age0, record.Age1, record.Age2, record.Age3, record.Age4, record.Age5, record.Age6, record.Age7, record.Age8, record.Age9, record.Age10, record.Age11, record.Age12, record.Age13, record.Age14, record.Age15, record.Age16, record.Age17, record.Age18, record.Age19, record.Age20, record.Age21, record.Age22, record.Age23, record.Age24, record.Age25, record.Age26, record.Age27, record.Age28, record.Age29, record.Age30, record.Age31, record.Age32, record.Age33, record.Age34, record.Age35, record.Age36, record.Age37, record.Age38, record.Age39, record.Age40, record.Age41, record.Age42, record.Age43, record.Age44, record.Age45, record.Age46, record.Age47, record.Age48, record.Age49, record.Age50, record.Age51, record.Age52, record.Age53, record.Age54, record.Age55, record.Age56, record.Age57, record.Age58, record.Age59, record.Age60, record.Age61, record.Age62, record.Age63, record.Age64, record.Age65, record.Age66, record.Age67, record.Age68, record.Age69, record.Age70, record.Age71, record.Age72, record.Age73, record.Age74, record.Age75, record.Age76, record.Age77, record.Age78, record.Age79, record.Age80, record.Age81, record.Age82, record.Age83, record.Age84, record.Age85, record.Age86, record.Age87, record.Age88, record.Age89, record.Age90, record.Age91, record.Age92, record.Age93, record.Age94, record.Age95, record.Age96, record.Age97, record.Age98, record.Age99 };
                    loader.Insert(data);
                    i++;
                    if (i % loader.CommitMax == 0)
                    {
                        loader.Flush();
                    }
                }
                loader.Flush();
            }
        }


        public void loadcrfile(string filename)
        {
            // load to staging
            clearTable("SYS_CR");

            SQLiteBulkLoader loader = new SQLiteBulkLoader(_dbConn, "SYS_CR");
            loader.AddParameter("ID", DbType.Object);

            loader.AddParameter("FunctionID", DbType.Int32);
            loader.AddParameter("Endpoint", DbType.String);
            loader.AddParameter("PoolingWeight", DbType.Double);
            loader.AddParameter("Seasonal_Metric", DbType.String);
            loader.AddParameter("Study_Author", DbType.String);
            loader.AddParameter("Study_Year", DbType.Int32);
            loader.AddParameter("Start_Age", DbType.Int32);
            loader.AddParameter("End_Age", DbType.Int32);
            loader.AddParameter("Function", DbType.String);
            loader.AddParameter("Beta", DbType.Double);
            loader.AddParameter("Adjusted", DbType.String);
            loader.AddParameter("Parameter_1_Beta", DbType.Double);
            loader.AddParameter("A", DbType.Double);
            loader.AddParameter("Name_A", DbType.String);
            loader.AddParameter("B", DbType.Double);
            loader.AddParameter("Name_B", DbType.String);
            loader.AddParameter("C", DbType.Double);
            loader.AddParameter("Name_C", DbType.String);
            loader.AddParameter("Cases", DbType.Int32);
            loader.AddParameter("IncidenceEndpoint", DbType.String);

            using (TextReader fileReader = File.OpenText(filename))
            {
                CsvReader csv = new CsvReader(fileReader, System.Globalization.CultureInfo.CurrentCulture);
                List<Core_CR> records = csv.GetRecords<Core_CR>().ToList();
                loader.CommitMax = 10000;
                int i = 0;
                foreach (var record in records)
                {
                    object[] data = new object[] { null, record.FunctionID, record.Endpoint, record.PoolingWeight, record.Seasonal_Metric, record.Study_Author, record.Study_Year, record.Start_Age, record.End_Age, record.Function, record.Beta, record.Adjusted, record.Parameter_1_Beta, record.A, record.Name_A, record.B, record.Name_B, record.C, record.Name_C, record.Cases, record.IncidenceEndpoint};
                    loader.Insert(data);
                    i++;
                    if (i % loader.CommitMax == 0)
                    {
                        loader.Flush();
                    }
                }
                loader.Flush();
            }
        }

        public void loadvaluationfile(string filename)
        {
            // load to staging
            clearTable("SYS_Valuation");

            SQLiteBulkLoader loader = new SQLiteBulkLoader(_dbConn, "SYS_Valuation");
            loader.AddParameter("ID", DbType.Object);

            loader.AddParameter("CRFunctionID", DbType.Int32);
            loader.AddParameter("Endpoint", DbType.String);
            loader.AddParameter("PoolingWeight", DbType.Double);
            loader.AddParameter("Seasonal_Metric", DbType.String);
            loader.AddParameter("Study_Author", DbType.String);
            loader.AddParameter("Study_Year", DbType.Int32);
            loader.AddParameter("Start_Age", DbType.Int32);
            loader.AddParameter("End_Age", DbType.Int32);
            loader.AddParameter("Function", DbType.String);
            loader.AddParameter("Beta", DbType.Double);
            loader.AddParameter("Adjusted", DbType.String);
            loader.AddParameter("Parameter_1_Beta", DbType.Double);
            loader.AddParameter("A", DbType.Double);
            loader.AddParameter("Name_A", DbType.String);
            loader.AddParameter("B", DbType.Double);
            loader.AddParameter("Name_B", DbType.String);
            loader.AddParameter("C", DbType.Double);
            loader.AddParameter("Name_C", DbType.String);
            loader.AddParameter("Cases", DbType.Int32);

            loader.AddParameter("HealthEffect", DbType.String);
            loader.AddParameter("ValuationMethod", DbType.String);
            loader.AddParameter("Value", DbType.Double);
            loader.AddParameter("ApplyDiscount", DbType.String);

            loader.AddParameter("IncidenceEndpoint", DbType.String);

            using (TextReader fileReader = File.OpenText(filename))
            {
                CsvReader csv = new CsvReader(fileReader, System.Globalization.CultureInfo.CurrentCulture);
                List<Core_Valuation> records = csv.GetRecords<Core_Valuation>().ToList();
                loader.CommitMax = 10000;
                int i = 0;
                foreach (var record in records)
                {
                    object[] data = new object[] { null, record.CRFunctionID, record.Endpoint, record.PoolingWeight, record.Seasonal_Metric, record.Study_Author, record.Study_Year, record.Start_Age, record.End_Age, record.Function, record.Beta, record.Adjusted, record.Parameter_1_Beta, record.A, record.Name_A, record.B, record.Name_B, record.C, record.Name_C, record.Cases, record.HealthEffect, record.ValuationMethod, record.Value, record.ApplyDiscount, record.IncidenceEndpoint };
                    loader.Insert(data);
                    i++;
                    if (i % loader.CommitMax == 0)
                    {
                        loader.Flush();
                    }
                }
                loader.Flush();
            }
        }

        public void loadfileintoemissions(string filename, string tablename)
        {
            // load to staging
            clearTable("SYS_Emissions_Staging");

            SQLiteBulkLoader loader = new SQLiteBulkLoader(_dbConn, "SYS_Emissions_Staging");
            loader.AddParameter("typeindx", DbType.Int32);
            loader.AddParameter("sourceindx", DbType.Int32);
            loader.AddParameter("stid", DbType.Int32);
            loader.AddParameter("cyid", DbType.Int32);
            loader.AddParameter("TIER1", DbType.Int32);
            loader.AddParameter("TIER2", DbType.Int32);
            loader.AddParameter("TIER3", DbType.Int32);
            loader.AddParameter("NOx", DbType.Double);
            loader.AddParameter("SO2", DbType.Double);
            loader.AddParameter("NH3", DbType.Double);
            loader.AddParameter("PM25", DbType.Double);
            loader.AddParameter("VOC", DbType.Double);

            using (TextReader fileReader = File.OpenText(filename))
            {
                CsvReader csv = new CsvReader(fileReader, System.Globalization.CultureInfo.CurrentCulture);
                List<Core_EmissionsRecord> records = csv.GetRecords<Core_EmissionsRecord>().ToList();
                loader.CommitMax = 10000;
                int i = 0;
                foreach (var record in records)
                {
                    loader.Insert(new object[] { record.typeindx, record.sourceindx, record.stid, record.cyid, record.TIER1, record.TIER2, record.TIER3, record.NOx.GetValueOrDefault(0), record.SO2.GetValueOrDefault(0), record.NH3.GetValueOrDefault(0), record.PM25.GetValueOrDefault(0), record.VOC.GetValueOrDefault(0) });
                    i++;
                    if (i % loader.CommitMax == 0)
                    {
                        loader.Flush();
                    }
                }
                loader.Flush();
            }

            clearTable(tablename);
            executeQuery("Insert into " + tablename + " SELECT null as ID, SYS_Emissions_Staging.typeindx, SYS_Emissions_Staging.sourceindx, SYS_Emissions_Staging.stid, SYS_Emissions_Staging.cyid, SYS_Emissions_Staging.TIER1, SYS_Emissions_Staging.TIER2, SYS_Emissions_Staging.TIER3, SYS_Emissions_Staging.NOx, SYS_Emissions_Staging.SO2, SYS_Emissions_Staging.NH3, SYS_Emissions_Staging.VOC* SYS_voc2soa.FACTOR as SOA, SYS_Emissions_Staging.PM25, SYS_Emissions_Staging.VOC FROM SYS_Emissions_Staging INNER JOIN SYS_voc2soa ON(SYS_Emissions_Staging.TIER1 = SYS_voc2soa.TIER1  and SYS_Emissions_Staging.TIER2 = SYS_voc2soa.TIER2 and SYS_Emissions_Staging.TIER3 = SYS_voc2soa.TIER3)");
        }

        public void loadscenariofileintoemissions(string filename)
        {
            // load to staging
            clearTable("SYS_ScenarioEmissions_Staging");

            SQLiteBulkLoader loader = new SQLiteBulkLoader(_dbConn, "SYS_ScenarioEmissions_Staging");
            loader.AddParameter("typeindx", DbType.Int32);
            loader.AddParameter("sourceindx", DbType.Int32);
            loader.AddParameter("stid", DbType.Int32);
            loader.AddParameter("cyid", DbType.Int32);
            loader.AddParameter("TIER1", DbType.Int32);
            loader.AddParameter("TIER2", DbType.Int32);
            loader.AddParameter("TIER3", DbType.Int32);
            loader.AddParameter("BASE_NOx", DbType.Double);
            loader.AddParameter("BASE_SO2", DbType.Double);
            loader.AddParameter("BASE_NH3", DbType.Double);
            loader.AddParameter("BASE_PM25", DbType.Double);
            loader.AddParameter("BASE_VOC", DbType.Double);
            loader.AddParameter("CTRL_NOx", DbType.Double);
            loader.AddParameter("CTRL_SO2", DbType.Double);
            loader.AddParameter("CTRL_NH3", DbType.Double);
            loader.AddParameter("CTRL_PM25", DbType.Double);
            loader.AddParameter("CTRL_VOC", DbType.Double);

            using (TextReader fileReader = File.OpenText(filename))
            {
                CsvReader csv = new CsvReader(fileReader, System.Globalization.CultureInfo.CurrentCulture);
                List<Core_ScenarioEmissionsRecord> records = csv.GetRecords<Core_ScenarioEmissionsRecord>().ToList();
                loader.CommitMax = 10000;
                int i = 0;
                foreach (var record in records)
                {
                    loader.Insert(new object[] { record.typeindx, record.sourceindx, record.stid, record.cyid, record.TIER1, record.TIER2, record.TIER3, record.BASE_NOx.GetValueOrDefault(0), record.BASE_SO2.GetValueOrDefault(0), record.BASE_NH3.GetValueOrDefault(0), record.BASE_PM25.GetValueOrDefault(0), record.BASE_VOC.GetValueOrDefault(0), record.CTRL_NOx.GetValueOrDefault(0), record.CTRL_SO2.GetValueOrDefault(0), record.CTRL_NH3.GetValueOrDefault(0), record.CTRL_PM25.GetValueOrDefault(0), record.CTRL_VOC.GetValueOrDefault(0) });
                    i++;
                    if (i % loader.CommitMax == 0)
                    {
                        loader.Flush();
                    }
                }
                loader.Flush();
            }

            //clearTable(tablename);
            //executeQuery("Insert into " + tablename + " SELECT null as ID, SYS_Emissions_Staging.typeindx, SYS_Emissions_Staging.sourceindx, SYS_Emissions_Staging.stid, SYS_Emissions_Staging.cyid, SYS_Emissions_Staging.TIER1, SYS_Emissions_Staging.TIER2, SYS_Emissions_Staging.TIER3, SYS_Emissions_Staging.NOx, SYS_Emissions_Staging.SO2, SYS_Emissions_Staging.NH3, SYS_Emissions_Staging.VOC* SYS_voc2soa.FACTOR as SOA, SYS_Emissions_Staging.PM25, SYS_Emissions_Staging.VOC, SYS_Emissions_Staging.CTRL_NOx, SYS_Emissions_Staging.CTRL_SO2, SYS_Emissions_Staging.CTRL_NH3, SYS_Emissions_Staging.CTRL_VOC* SYS_voc2soa.FACTOR, SYS_Emissions_Staging.CTRL_PM25, SYS_Emissions_Staging.CTRL_VOC FROM SYS_Emissions_Staging INNER JOIN SYS_voc2soa ON(SYS_Emissions_Staging.TIER1 = SYS_voc2soa.TIER1  and SYS_Emissions_Staging.TIER2 = SYS_voc2soa.TIER2 and SYS_Emissions_Staging.TIER3 = SYS_voc2soa.TIER3)");
        }

        public void loadfileintocombinedemissions(string filename)
        {
            clearTable("SYS_Emissions");
            clearTable("SYS_Emissions_Base");

            loadfileintoemissions(filename, "SYS_Emissions_Base");

            string query = "insert into SYS_Emissions SELECT null, "
              + " SYS_Emissions_Base.typeindx,"
              + " SYS_Emissions_Base.sourceindx,"
              + " SYS_Emissions_Base.stid,"
              + " SYS_Emissions_Base.cyid,"
              + " SYS_Emissions_Base.TIER1,"
              + " SYS_Emissions_Base.TIER2,"
              + " SYS_Emissions_Base.TIER3,"
              + " SYS_Emissions_Base.NOx,"
              + " SYS_Emissions_Base.SO2,"
              + " SYS_Emissions_Base.NH3,"
              + " SYS_Emissions_Base.PM25,"
              + " SYS_Emissions_Base.VOC,"
              + " SYS_Emissions_Base.NOx,"
              + " SYS_Emissions_Base.SO2,"
              + " SYS_Emissions_Base.NH3,"
              + " SYS_Emissions_Base.PM25,"
              + " SYS_Emissions_Base.VOC,"
              + " SYS_Tiers.TIER1NAME,"
              + " SYS_Tiers.TIER2NAME,"
              + " SYS_Tiers.TIER3NAME,"
              + " SYS_Dict.fips,"
              + " SYS_Dict.stname,"
              + " SYS_Dict.cyname,"
              + " SYS_Stacks.NAME,"
              + " 0,0,0,0,0,0"
              + " FROM "
              + " SYS_Emissions_Base"
              + " LEFT JOIN SYS_Tiers ON (SYS_Emissions_Base.TIER1 = SYS_Tiers.TIER1 and SYS_Emissions_Base.TIER2 = SYS_Tiers.TIER2 and SYS_Emissions_Base.TIER3 = SYS_Tiers.TIER3)"
              + " INNER JOIN SYS_Dict ON (SYS_Emissions_Base.sourceindx = SYS_Dict.sourceindx)"
              + " INNER JOIN SYS_Stacks ON(SYS_Emissions_Base.typeindx = SYS_Stacks.typeindx)";
            executeQuery(query);
        }

        public void loadinventoryintocombinedemissions(int item)
        {
            clearTable("SYS_Emissions");

            string query = "insert into SYS_Emissions SELECT null, "
              + " SYS_Emissions_INVENTORY.typeindx,"
              + " SYS_Emissions_INVENTORY.sourceindx,"
              + " SYS_Emissions_INVENTORY.stid,"
              + " SYS_Emissions_INVENTORY.cyid,"
              + " SYS_Emissions_INVENTORY.TIER1,"
              + " SYS_Emissions_INVENTORY.TIER2,"
              + " SYS_Emissions_INVENTORY.TIER3,"
              + " SYS_Emissions_INVENTORY.NOx,"
              + " SYS_Emissions_INVENTORY.SO2,"
              + " SYS_Emissions_INVENTORY.NH3,"
              + " SYS_Emissions_INVENTORY.PM25,"
              + " SYS_Emissions_INVENTORY.VOC,"
              + " SYS_Emissions_INVENTORY.NOx,"
              + " SYS_Emissions_INVENTORY.SO2,"
              + " SYS_Emissions_INVENTORY.NH3,"
              + " SYS_Emissions_INVENTORY.PM25,"
              + " SYS_Emissions_INVENTORY.VOC,"
              + " SYS_Tiers.TIER1NAME,"
              + " SYS_Tiers.TIER2NAME,"
              + " SYS_Tiers.TIER3NAME,"
              + " SYS_Dict.fips,"
              + " SYS_Dict.stname,"
              + " SYS_Dict.cyname,"
              + " SYS_Stacks.NAME,"
              + " 0,0,0,0,0,0"
              + " FROM "
              + " SYS_Emissions_INVENTORY"
              + " LEFT JOIN SYS_Tiers ON (SYS_Emissions_INVENTORY.TIER1 = SYS_Tiers.TIER1 and SYS_Emissions_INVENTORY.TIER2 = SYS_Tiers.TIER2 and SYS_Emissions_INVENTORY.TIER3 = SYS_Tiers.TIER3)"
              + " INNER JOIN SYS_Dict ON (SYS_Emissions_INVENTORY.sourceindx = SYS_Dict.sourceindx)"
              + " INNER JOIN SYS_Stacks ON(SYS_Emissions_INVENTORY.typeindx = SYS_Stacks.typeindx)"
              + " where SYS_Emissions_INVENTORY.ID="+item.ToString();
            executeQuery(query);
        }

        public void loadscenariofileintocombinedemissions(string filename)
        {
            clearTable("SYS_Emissions");

            loadscenariofileintoemissions(filename);

            string query = "insert into SYS_Emissions SELECT null, "
              + " SYS_ScenarioEmissions_Staging.typeindx,"
              + " SYS_ScenarioEmissions_Staging.sourceindx,"
              + " SYS_ScenarioEmissions_Staging.stid,"
              + " SYS_ScenarioEmissions_Staging.cyid,"
              + " SYS_ScenarioEmissions_Staging.TIER1,"
              + " SYS_ScenarioEmissions_Staging.TIER2,"
              + " SYS_ScenarioEmissions_Staging.TIER3,"
              + " SYS_ScenarioEmissions_Staging.BASE_NOx,"
              + " SYS_ScenarioEmissions_Staging.BASE_SO2,"
              + " SYS_ScenarioEmissions_Staging.BASE_NH3,"
              + " SYS_ScenarioEmissions_Staging.BASE_PM25,"
              + " SYS_ScenarioEmissions_Staging.BASE_VOC,"
              + " SYS_ScenarioEmissions_Staging.CTRL_NOx,"
              + " SYS_ScenarioEmissions_Staging.CTRL_SO2,"
              + " SYS_ScenarioEmissions_Staging.CTRL_NH3,"
              + " SYS_ScenarioEmissions_Staging.CTRL_PM25,"
              + " SYS_ScenarioEmissions_Staging.CTRL_VOC,"
              + " SYS_Tiers.TIER1NAME,"
              + " SYS_Tiers.TIER2NAME,"
              + " SYS_Tiers.TIER3NAME,"
              + " SYS_Dict.fips,"
              + " SYS_Dict.stname,"
              + " SYS_Dict.cyname,"
              + " SYS_Stacks.NAME,"
              + " SYS_ScenarioEmissions_Staging.BASE_NOx - SYS_ScenarioEmissions_Staging.CTRL_NOx,"
              + " SYS_ScenarioEmissions_Staging.BASE_SO2 - SYS_ScenarioEmissions_Staging.CTRL_SO2,"
              + " SYS_ScenarioEmissions_Staging.BASE_NH3 - SYS_ScenarioEmissions_Staging.CTRL_NH3,"
              + " SYS_ScenarioEmissions_Staging.BASE_PM25 - SYS_ScenarioEmissions_Staging.CTRL_PM25,"
              + " SYS_ScenarioEmissions_Staging.BASE_VOC - SYS_ScenarioEmissions_Staging.CTRL_VOC,"
              + " 0"
              + " FROM "
              + " SYS_ScenarioEmissions_Staging"
              + " LEFT JOIN SYS_Tiers ON(SYS_ScenarioEmissions_Staging.TIER1 = SYS_Tiers.TIER1 and SYS_ScenarioEmissions_Staging.TIER2 = SYS_Tiers.TIER2 and SYS_ScenarioEmissions_Staging.TIER3 = SYS_Tiers.TIER3)"
              + " INNER JOIN SYS_Dict ON (SYS_ScenarioEmissions_Staging.sourceindx = SYS_Dict.sourceindx)"
              + " INNER JOIN SYS_Stacks ON(SYS_ScenarioEmissions_Staging.typeindx = SYS_Stacks.typeindx)";
            executeQuery(query);

            executeQuery("update SYS_Emissions set MODIFIED=1 where (BASE_NOx<>CTRL_NOx) or (BASE_SO2<>CTRL_SO2) or (BASE_NH3<>CTRL_NH3) or (BASE_PM25<>CTRL_PM25) or (BASE_VOC<>CTRL_VOC)");
        }

    }
}
