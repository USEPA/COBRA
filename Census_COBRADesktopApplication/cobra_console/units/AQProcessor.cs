using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.IO;
using System.Diagnostics;


namespace cobra_console.units
{
    public class AQProcessor
    {
        public List<string> messages = new List<string>();
        public List<string> components = new List<string>();

        private SQLiteConnection _dbConn;
        private string _EF_connectionstring;
        public bool usePreloadedEmissionsData { get; set; } = true;

        public AQProcessor(SQLiteConnection dbConn, string EF_connectionstring)
        {
            _dbConn = dbConn;
            _EF_connectionstring = EF_connectionstring;
            //do pragmas
            //Synchronous = OFF; Journal Mode = OFF; Cache Size = -40000;
            executeQuery("PRAGMA synchronous = OFF");
            executeQuery("PRAGMA journal_mode = OFF");
            executeQuery("PRAGMA cache_size = 2000000");
        }

        //using (cobraEntities context = new cobraEntities(_EF_connectionstring))

        private void executeQuery(string query)
        {
            //Console.WriteLine("Executing query:");
            //Console.WriteLine(query);
            try
            {
                using (var cmd = new SQLiteCommand(query, _dbConn))
                {
                    if (query.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            int rowCount = 0;
                            const int maxRows = 15;  // Limit to 15 rows
                            while (reader.Read() && rowCount < maxRows)
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    Console.Write($"{reader.GetName(i)}: {reader[i]} ");
                                }
                                Console.WriteLine();
                                rowCount++;
                            }
                            if (rowCount >= maxRows)
                            {
                                Console.WriteLine($"Output limited to {maxRows} rows.");
                            }
                        }
                    }
                    else
                    {
                        int affectedRows = cmd.ExecuteNonQuery();
                        //Console.WriteLine($"Query executed successfully. Rows affected: {affectedRows}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error executing query: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void clearTable(string tablename)
        {
            executeQuery("delete from " + tablename);
        }

        public void setup_from_GUI()
        {
            clearTable("SYS_Emissions_Base");
            clearTable("SYS_Emissions_Control");

            executeQuery("Insert into " + "SYS_Emissions_Base" + " SELECT null as ID, SYS_Emissions.typeindx, SYS_Emissions.sourceindx, SYS_Emissions.stid, SYS_Emissions.cyid, SYS_Emissions.TIER1, SYS_Emissions.TIER2, SYS_Emissions.TIER3, SYS_Emissions.BASE_NOx, SYS_Emissions.BASE_SO2, SYS_Emissions.BASE_NH3, SYS_Emissions.BASE_VOC*SYS_voc2soa.FACTOR as SOA, SYS_Emissions.BASE_PM25, SYS_Emissions.BASE_VOC FROM SYS_Emissions LEFT JOIN SYS_voc2soa ON (SYS_Emissions.TIER1 = SYS_voc2soa.TIER1  and SYS_Emissions.TIER2 = SYS_voc2soa.TIER2 and SYS_Emissions.TIER3 = SYS_voc2soa.TIER3)");
            executeQuery("Insert into " + "SYS_Emissions_Control" + " SELECT null as ID, SYS_Emissions.typeindx, SYS_Emissions.sourceindx, SYS_Emissions.stid, SYS_Emissions.cyid, SYS_Emissions.TIER1, SYS_Emissions.TIER2, SYS_Emissions.TIER3, SYS_Emissions.CTRL_NOx, SYS_Emissions.CTRL_SO2, SYS_Emissions.CTRL_NH3, SYS_Emissions.CTRL_VOC*SYS_voc2soa.FACTOR as SOA, SYS_Emissions.CTRL_PM25, SYS_Emissions.CTRL_VOC FROM SYS_Emissions LEFT JOIN SYS_voc2soa ON (SYS_Emissions.TIER1 = SYS_voc2soa.TIER1  and SYS_Emissions.TIER2 = SYS_voc2soa.TIER2 and SYS_Emissions.TIER3 = SYS_voc2soa.TIER3)");
        }

        public void summarize(string emissions, string summarizedemissions, string destination)
        {
            clearTable(summarizedemissions);

            //PRAGMA schema.journal_mode = DELETE | TRUNCATE | PERSIST | MEMORY | WAL | OFF
            //executeQuery("PRAGMA journal_mode = OFF");
            //executeQuery("PRAGMA synchronous = OFF");
            //executeQuery("PRAGMA synchronous = OFF");

            //clear prior repopulate
            string query = "insert into " + summarizedemissions
              + " SELECT"
              + " null,"
              + " typeindx,"
              + " sourceindx,"
              + " sum(NOx) as NOx,"
              + " sum(SO2) as SO2,"
              + " sum(NH3) as NH3," //can probably remove
              + " sum(SOA) as SOA," //can probably remove
              + " sum(PM25) as PM25,"
              + " sum(VOC) as VOC"
              + " FROM "
              + emissions + " group by typeindx, sourceindx;";

            executeQuery(query);

            clearTable(destination);


            // measuring entire merge operation
            Stopwatch overallStopwatch = new Stopwatch();
            overallStopwatch.Start();

            executeQuery("DROP TABLE IF EXISTS CountyLevelEmissions;");
            executeQuery("DROP TABLE IF EXISTS CensusPrep;");
            // Step 4: Cleanup temporary tables
            executeQuery("DROP TABLE IF EXISTS TempSYS_SR_PM;");
            executeQuery("DROP TABLE IF EXISTS CensusLevelEmissions;");
            executeQuery("DROP TABLE IF EXISTS CombinedEmissions;");

            executeQuery("BEGIN TRANSACTION");

            try
            {
                // temp table to accumulate batch results and groupby dest_tract before inserting into final destination table
                string createTempTableQuery = @"
            CREATE TEMP TABLE IF NOT EXISTS TempResults (
                census_index INTEGER,
destindx INTEGER,
                NOx REAL,
                SO2 REAL,
                PM25 REAL,
                VOC REAL,
                O3N REAL,
dest_tract TEXT
            );
        ";
                executeQuery(createTempTableQuery);
                int processedIndices = 0;
                // only doing 2 source indices at a time for now... taking about an hour
                for (int batchStart = 1; batchStart <= 3108; batchStart += 10)
                {
                    int batchEnd = batchStart + 9;  // Define batch range (e.g., process 2 at a time)

                    // Stopwatch for the current batch
                    Stopwatch batchStopwatch = new Stopwatch();
                    batchStopwatch.Start();

                    // Log the batch processing information
                    Console.WriteLine($"Processing merge for batch sourceindx from {batchStart} to {batchEnd}");

                    Console.WriteLine("Getting county level emissions........");

                    /*string createIntermediateTable = @"
                    CREATE TEMP TABLE IntermediateEmissions AS
                    SELECT 
                        " + summarizedemissions + @".sourceindx, 
                        " + summarizedemissions + @".typeindx,
                        " + summarizedemissions + @".NOx,
                        " + summarizedemissions + @".SO2,
                        " + summarizedemissions + @".VOC
                    FROM " + summarizedemissions + @" 
                    WHERE " + summarizedemissions + @".sourceindx BETWEEN " + batchStart + " AND " + batchEnd + ";";
                    executeQuery(createIntermediateTable);

                    string createIntermediateSYS_SrmatrixTable = @"
                    CREATE TEMP TABLE TempSYS_Srmatrix AS
                    SELECT 
                        sr.sourceindx,
                        sr.typeindx,
                        sr.c_NO3,
                        sr.c_SO4,
                        sr.c_O3V,
                        sr.c_O3N,
                        sr.destindx
                    FROM SYS_Srmatrix sr
                    WHERE sr.sourceindx BETWEEN " + batchStart + " AND " + batchEnd + ";";
                    executeQuery(createIntermediateSYS_SrmatrixTable);*/

                    //Join the intermediate emissions table with the intermediate SYS_Srmatrix table
                    string createCountyLevelEmissions = $@"
                    CREATE TEMP TABLE CountyLevelEmissions AS
                    SELECT 
                        sr.sourceindx, 
                        SUM(COALESCE(sr.c_NO3 * se.NOx, 0)) AS NO2Val,
                        SUM(COALESCE(sr.c_SO4 * se.SO2, 0)) AS SO2Val,
                        SUM(COALESCE(sr.c_O3V * se.VOC, 0)) AS VOCVal,
                        SUM(COALESCE(sr.c_O3N * se.NOx, 0)) AS O3NVal,
                        sr.destindx,
                        se.typeindx
                    FROM {summarizedemissions} se
                    INNER JOIN SYS_Srmatrix sr ON se.sourceindx = sr.sourceindx
                        AND se.typeindx = sr.typeindx
                    WHERE  sr.sourceindx BETWEEN {batchStart} AND {batchEnd}
                    GROUP BY sr.destindx;";
                    executeQuery(createCountyLevelEmissions);



                    // Clean up intermediate table
                    //executeQuery("DROP TABLE IF EXISTS IntermediateEmissions;");
                    //executeQuery("DROP TABLE IF EXISTS TempSYS_Srmatrix;");




                    Console.WriteLine("Getting census level emissions........");
                    /* string createTempSYS_SR_PM = @"
     CREATE TEMP TABLE TempSYS_SR_PM AS
     SELECT 
         SYS_SR_PM.sourceindx,
         SYS_SR_PM.typeindx,
         SYS_SR_PM.c_PM25,
         SYS_SR_PM.census_index
     FROM SYS_SR_PM
     WHERE SYS_SR_PM.sourceindx BETWEEN " + batchStart + " AND " + batchEnd + ";";
                     executeQuery(createTempSYS_SR_PM);*/

                    string createCensusLevelEmissions = $@"
                    CREATE TEMP TABLE CensusLevelEmissions AS
                    SELECT
                        pm.census_index,
                        SUM(COALESCE(pm.c_PM25 * se.PM25, 0)) AS PM25Val
                    FROM   SYS_SR_PM            AS pm
                    JOIN   {summarizedemissions} AS se
                      ON   se.sourceindx = pm.sourceindx
                     AND   se.typeindx   = pm.typeindx
                    WHERE  pm.sourceindx BETWEEN {batchStart} AND {batchEnd}
                    GROUP  BY pm.census_index;";
                    //INNER JOIN TempSYS_SR_PM ON CENSUS_DICT.census_index = TempSYS_SR_PM.census_index;";
                    executeQuery(createCensusLevelEmissions);
                    //executeQuery("DROP TABLE IF EXISTS TempSYS_SR_PM;");






                    //joining county and census level emissions

                    Console.WriteLine("CREATING COMBINED EMISSIONS....");
                    /*string prepCensus = "CREATE TEMP TABLE CensusPrep AS SELECT CensusLevelEmissions.census_index, CensusLevelEmissions.PM25Val, CENSUS_DICT.destindx, CENSUS_DICT.dest_tract FROM CensusLevelEmissions INNER JOIN CENSUS_DICT ON CensusLevelEmissions.census_index = CENSUS_DICT.census_index;";
                    executeQuery(prepCensus);
                    string combined = "CREATE TEMP TABLE CombinedEmissions AS SELECT CensusPrep.census_index, CountyLevelEmissions.NO2Val, CountyLevelEmissions.SO2Val, CensusPrep.PM25Val, CountyLevelEmissions.VOCVAL, CountyLevelEmissions.O3NVAL, CensusPrep.destindx, CensusPrep.dest_tract FROM CensusPrep INNER JOIN CountyLevelEmissions ON CountyLevelEmissions.destindx = CensusPrep.destindx;";
                    executeQuery(combined);

                    executeQuery("DROP TABLE IF EXISTS CensusPrep;");*/




                    string createTempCombinedEmissions = @"
                    CREATE TEMP TABLE TempCombinedEmissions AS
                   SELECT
                      pm.census_index,
                      cd.destindx,
                      SUM(cle.NO2Val) AS NO2Val,
                      SUM(cle.SO2Val) AS SO2Val,
                      pm.PM25Val     AS PM25Val,
                      SUM(cle.VOCVal) AS VOCVal,
                      SUM(cle.O3NVal) AS O3NVal,
                      cd.dest_tract
                  FROM   CountyLevelEmissions AS cle
                  JOIN   CENSUS_DICT          AS cd  ON cd.destindx     = cle.destindx
                  JOIN   CensusLevelEmissions AS pm  ON pm.census_index = cd.census_index
                  GROUP  BY pm.census_index, cd.destindx;";
                    executeQuery(createTempCombinedEmissions);

                    // Step 2: Perform the SUM operations and COALESCE with existing TempResults in another step
                    string insertIntoTempResults = @"
                    INSERT INTO TempResults (census_index, destindx, NOx, SO2, PM25, VOC, O3N, dest_tract)
                    SELECT
                        tce.census_index,
                        tce.destindx,  
                        SUM(tce.NO2Val) + COALESCE(tr.NOx, 0) AS NOx,
                        SUM(tce.SO2Val) + COALESCE(tr.SO2, 0) AS SO2,
                        SUM(tce.PM25Val) + COALESCE(tr.PM25, 0) AS PM25,
                        SUM(tce.VOCVal) + COALESCE(tr.VOC, 0) AS VOC,
                        SUM(tce.O3NVAL) + COALESCE(tr.O3N, 0) AS O3N,
                        tce.dest_tract AS dest_tract
                    FROM TempCombinedEmissions tce
                    LEFT JOIN TempResults tr ON tce.census_index = tr.census_index
                    GROUP BY tce.census_index, tce.destindx;";

                    try
                    {
                        executeQuery(insertIntoTempResults);

                        //refresh temp county table for next batch
                        executeQuery("DROP TABLE IF EXISTS CountyLevelEmissions;");

                        executeQuery("DROP TABLE IF EXISTS TempCombinedEmissions;");
                        // Step 4: Cleanup temporary tables

                        executeQuery("DROP TABLE IF EXISTS CensusLevelEmissions;");
                        executeQuery("DROP TABLE IF EXISTS CombinedEmissions;");



                        string DestInsertQuery = @"
                            INSERT OR REPLACE INTO " + destination + @" (destindx, NOx, SO2, NH3, SOA, PM25, VOC, O3N, dest_tract)
                            SELECT 
                                TempResults.destindx, 
                                SUM(TempResults.NOx) + COALESCE(dest.NOx, 0) AS NOx, 
                                SUM(TempResults.SO2) + COALESCE(dest.SO2, 0) AS SO2, 
                                0 AS NH3,  -- Placeholder value
                                0 AS SOA,  -- Placeholder value
                                SUM(TempResults.PM25) + COALESCE(dest.PM25, 0) AS PM25, 
                                SUM(TempResults.VOC) + COALESCE(dest.VOC, 0) AS VOC, 
                                SUM(TempResults.O3N) + COALESCE(dest.O3N, 0) AS O3N, 
                                TempResults.dest_tract 
                            FROM TempResults
                            LEFT JOIN " + destination + @" AS dest 
                            ON TempResults.destindx = dest.destindx 
                            AND TempResults.dest_tract = dest.dest_tract
                            GROUP BY TempResults.destindx, TempResults.dest_tract;";
                        executeQuery(DestInsertQuery);



                        // Clear TempResults for the next set of batches
                        executeQuery("DELETE FROM TempResults;");



                        // Commit after processing each batch
                        executeQuery("COMMIT");
                        Console.WriteLine($"Batch {batchStart} - {batchEnd} committed.");
                        batchStopwatch.Stop();

                        Console.WriteLine($"DONE BATCH: {batchStart} - {batchEnd}");
                        Console.WriteLine($"TIME TAKEN FOR BATCH: {batchStopwatch.ElapsedMilliseconds} ms");

                        // Start a new transaction for the next batch
                        executeQuery("BEGIN TRANSACTION");

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error executing merge for sourceindx range: " + batchStart + " - " + batchEnd + ": " + ex.Message);
                        throw;
                    }
                }

                // Drop temporary tables

                executeQuery("DROP TABLE IF EXISTS TempResults;");

                // transaction is committed only if everything succeeds
                executeQuery("COMMIT");

                overallStopwatch.Stop();
                Console.WriteLine($"TOTAL TIME TAKEN FOR ENTIRE OPERATION: {overallStopwatch.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                executeQuery("ROLLBACK");
                Console.WriteLine("Transaction rolled back due to error: " + ex.Message);
            }



        }


        public void summarize2map()
        {
            clearTable("SYS_SummarizedEmissions");

            //clear prior repopulate
            string query = "insert into " + "SYS_SummarizedEmissions"
              + " SELECT"
              + " null,"
              + " sourceindx,"
              + " sum(BASE_NOx) as BASE_NOx,"
              + " sum(BASE_SO2) as BASE_SO2,"
              + " sum(BASE_NH3) as BASE_NH3,"
              + " sum(BASE_PM25) as BASE_PM25,"
              + " sum(BASE_VOC) as BASE_VOC,"
              + " sum(CTRL_NOx) as CTRL_NOx,"
              + " sum(CTRL_SO2) as CTRL_SO2,"
              + " sum(CTRL_NH3) as CTRL_NH3,"
              + " sum(CTRL_PM25) as CTRL_PM25,"
              + " sum(CTRL_VOC) as CTRL_VOC,"
              + " sum(CTRL_NOx)-sum(BASE_NOx) as DELTA_NOx,"
              + " sum(CTRL_SO2)-sum(BASE_SO2) as DELTA_SO2,"
              + " sum(CTRL_NH3)-sum(BASE_NH3) as DELTA_NH3,"
              + " sum(CTRL_PM25)-sum(BASE_PM25) as DELTA_PM25,"
              + " sum(CTRL_VOC)-sum(BASE_VOC) as DELTA_VOC,"
              + " null,"
              + " null,"
              + " null,"
              + " FIPS,"
              + " STATE,"
              + " COUNTY "
              + "FROM SYS_Emissions" + " group by sourceindx, FIPS, STATE, COUNTY;";
            //+ "FROM SYS_Emissions" + " group by sourceindx, TIER1NAME, TIER2NAME, TIER3NAME, FIPS, STATE, COUNTY;";

            executeQuery(query);
        }


        public void finalize(string destinationbase, string destinationcontrol)
        {
            clearTable("SYS_Destination");

            //populate, also include adjustment factor
            string query = "";

            Console.WriteLine("INSERTING INTO SYS_DESTINATION");

            query = "Insert into SYS_Destination "
                    + "SELECT "
                    + "null as ID,  "
                    + destinationbase + ".destindx as destindx, "
                    + destinationbase + ".NOx as BASE_NOx, "
                    + destinationbase + ".SO2 as BASE_SO2, "
                    + destinationbase + ".NH3 as BASE_NH3, "
                    + destinationbase + ".SOA as BASE_SOA, " // not even used or needed
                    + destinationbase + ".PM25 as BASE_PM25, "
                    + destinationbase + ".VOC as BASE_VOC, "
                    + destinationbase + ".O3N as BASE_O3N, "
                    + destinationcontrol + ".NOx as CTRL_NOx, "
                    + destinationcontrol + ".SO2 as CTRL_SO2, "
                    + destinationcontrol + ".NH3 as CTRL_NH3, "
                    + destinationcontrol + ".SOA as CTRL_SOA, "
                    + destinationcontrol + ".PM25 as CTRL_PM25, "
                    + destinationcontrol + ".VOC as CTRL_VOC, "
                    + destinationcontrol + ".O3N as CTRL_O3N, "
                    + "SYS_ADJ.F1 as F, "
                    + "0 as BASE_FINAL_PM, "
                    + "0 as CTRL_FINAL_PM, "
                    + "0 as DELTA_FINAL_PM, "
                    + "0 as BASE_FINAL_O3, "
                    + "0 as CTRL_FINAL_O3, "
                    + "0 as DELTA_FINAL_O3, "
                    + destinationbase + ".dest_tract as dest_tract " // Include dest_tract
                    + "FROM  "
                    + "SYS_Destination_Base "
                    + " INNER JOIN " + destinationcontrol + " ON (" + destinationbase + ".destindx = " + destinationcontrol + ".destindx AND " + destinationbase + ".dest_tract = " + destinationcontrol + ".dest_tract) "
                    + "INNER JOIN SYS_ADJ ON (" + destinationbase + ".destindx = SYS_ADJ.indx) ";


            executeQuery(query);

            components.Add(@"FIPS,BASE_PM25,BASE_NO3,BASE_SO4,CTRL_PM5,CTRL_NO3,CTRL_SO4,DELTA_PM,BASE_O3N,BASE_VOC,CTRL_O3N,CTRL_VOC,DELTA_O3");




            //now do the PM stuff
            using (cobraEntities context = new cobraEntities(_EF_connectionstring))
            {
                Console.WriteLine("COMPUTING PM");

                List<SYS_Destination> destinations = context.SYS_Destination.ToList();

                List<SYS_Dict> FIPSES = context.SYS_Dict.ToList();

                foreach (var destination in destinations)
                {
                    string FIPS = FIPSES.Where(f => f.SOURCEINDX == destination.destindx).First().FIPS;
                    //computePM literally just adds up PM_25 BASE_NOx and BASE_SOA //
                    /* components.Add(fips + "," + mode + "," + Moles_SO4.ToString() + "," + Moles_NH4.ToString() + "," + Moles_Amm_Bisulfate.ToString() + "," + Moles_SO4_remaining.ToString() + "," + Moles_NH4_remaining_step_1.ToString() + "," +
                         Moles_Amm_Sulfate.ToString() + "," + Moles_Amm_Bisulfate_remaining.ToString() + "," + Moles_NH4_remaining_step_2.ToString() + "," + Moles_NO3.ToString() + "," + Moles_Amm_Nitrate.ToString() + "," +
                         Amm_Sulfate.ToString() + "," + Amm_Bisulfate.ToString() + "," + SO4.ToString() + "," + Amm_Nitrate.ToString() + "," + Direct_PM25.ToString() + "," +
                         SOA.ToString() + "," + (Amm_Sulfate + Amm_Bisulfate + SO4 + Amm_Nitrate + Direct_PM25 + SOA).ToString() + ',' + adjustment.ToString()); */
                    //computePM(double value_PM25, double value_NO3, double value_SOA, double value_NH4, double value_SO4, double adjustment, string fips, string mode)


                    //return value_PM25 + value_NO3 + value_SO4;
                    destination.BASE_FINAL_PM = computePM(destination.BASE_PM25.GetValueOrDefault(0), destination.BASE_NOx.GetValueOrDefault(0), destination.BASE_SOA.GetValueOrDefault(0), destination.BASE_NH3.GetValueOrDefault(0), destination.BASE_SO2.GetValueOrDefault(0), destination.F.GetValueOrDefault(0), FIPS, "base");
                    destination.CTRL_FINAL_PM = computePM(destination.CTRL_PM25.GetValueOrDefault(0), destination.CTRL_NOx.GetValueOrDefault(0), destination.CTRL_SOA.GetValueOrDefault(0), destination.CTRL_NH3.GetValueOrDefault(0), destination.CTRL_SO2.GetValueOrDefault(0), destination.F.GetValueOrDefault(0), FIPS, "control");
                    destination.DELTA_FINAL_PM = destination.BASE_FINAL_PM.GetValueOrDefault(0) - destination.CTRL_FINAL_PM.GetValueOrDefault(0);

                    destination.BASE_FINAL_O3 = destination.BASE_O3N.GetValueOrDefault(0) + destination.BASE_VOC.GetValueOrDefault(0);
                    destination.CTRL_FINAL_O3 = destination.CTRL_O3N.GetValueOrDefault(0) + destination.CTRL_VOC.GetValueOrDefault(0);
                    destination.DELTA_FINAL_O3 = destination.BASE_FINAL_O3.GetValueOrDefault(0) - destination.CTRL_FINAL_O3.GetValueOrDefault(0);
                    components.Add(FIPS + "," + destination.BASE_PM25.GetValueOrDefault(0).ToString() + "," + destination.BASE_NOx.GetValueOrDefault(0).ToString() + "," + destination.BASE_SO2.GetValueOrDefault(0).ToString() + "," + destination.CTRL_PM25.GetValueOrDefault(0).ToString() + "," + destination.CTRL_NOx.GetValueOrDefault(0).ToString() + "," +
                        destination.CTRL_SO2.GetValueOrDefault(0) + "," + (destination.BASE_FINAL_PM.GetValueOrDefault(0) - destination.CTRL_FINAL_PM.GetValueOrDefault(0)).ToString() + "," + destination.BASE_O3N.GetValueOrDefault(0).ToString() + "," + destination.BASE_VOC.GetValueOrDefault(0).ToString() + "," + destination.CTRL_O3N.GetValueOrDefault(0).ToString() + "," +
                        destination.CTRL_VOC.GetValueOrDefault(0).ToString() + "," + (destination.BASE_FINAL_O3.GetValueOrDefault(0) - destination.CTRL_FINAL_O3.GetValueOrDefault(0)).ToString());
                }
                context.SaveChanges();

            }

            //write out debug file
            //tring filePath = @"D:\Users\PerteaD\cobra\aqdetail" + DateTime.Now.ToString("dd-MM-yyyy-hh-mm-ss")  +  ".txt";
            //using (StreamWriter writetext = new StreamWriter(filePath))

            /* dont want aqdetaul file for release - only for debugging
             * using (StreamWriter writetext = new StreamWriter("aqdetail"+ DateTime.Now.ToString("dd-MM-yyyy-hh-mm-ss")+".txt"))
            {
                foreach (String s in components)
                    writetext.WriteLine(s);
            }*/


        }

        //public double computePM(double value_PM25, double value_NO3, double value_SOA, double value_NH4, double value_SO4, double adjustment, string fips, string mode)
        public double computePM(double value_PM25, double value_NO3, double value_SOA, double value_NH4, double value_SO4, double adjustment, string fips, string mode)
        {
            /*double H = 1.00794;
            double O = 15.9994;
            double N = 14.0067;
            double S = 32.065;
            double NO3 = 62.0049;
            double cSO4 = 96.0626;
            double NH4 = 18.03846;
            double NH4NO3 = 80.04336;
            double NH4HSO4 = 115.109;
            double NH42SO4 = 132.13952;

            double Moles_SO4 = (value_SO4) / cSO4;
            double Moles_NH4 = (value_NH4) / NH4;
            double Moles_Amm_Bisulfate = Math.Min(Moles_SO4, Moles_NH4);

            double Moles_SO4_remaining = Moles_SO4 - Moles_Amm_Bisulfate;
            double Moles_NH4_remaining_step_1 = Moles_NH4 - Moles_Amm_Bisulfate;

            double Moles_Amm_Sulfate = Math.Min(Moles_Amm_Bisulfate, Moles_NH4_remaining_step_1);
            double Moles_Amm_Bisulfate_remaining = Moles_Amm_Bisulfate - Moles_Amm_Sulfate;
            double Moles_NH4_remaining_step_2 = Moles_NH4_remaining_step_1 - Moles_Amm_Sulfate;
            double Moles_NO3 = (value_NO3) / NO3;

            double Moles_Amm_Nitrate = 0.25 * Math.Min(Moles_NH4_remaining_step_2, Moles_NO3);

            double Amm_Sulfate = Moles_Amm_Sulfate * NH42SO4;
            double Amm_Bisulfate = Moles_Amm_Bisulfate_remaining * NH4HSO4;
            double SO4 = Moles_SO4_remaining * cSO4;
            double Amm_Nitrate = Moles_Amm_Nitrate * NH4NO3;
            double Direct_PM25 = value_PM25;
            double SOA = value_SOA;

            components.Add(

            components.Add(fips + "," + mode + "," + Moles_SO4.ToString() + "," + Moles_NH4.ToString() + "," + Moles_Amm_Bisulfate.ToString() + "," + Moles_SO4_remaining.ToString() + "," + Moles_NH4_remaining_step_1.ToString() + "," +
                Moles_Amm_Sulfate.ToString() + "," + Moles_Amm_Bisulfate_remaining.ToString() + "," + Moles_NH4_remaining_step_2.ToString() + "," + Moles_NO3.ToString() + "," + Moles_Amm_Nitrate.ToString() + "," +
                Amm_Sulfate.ToString() + "," + Amm_Bisulfate.ToString() + "," + SO4.ToString() + "," + Amm_Nitrate.ToString() + "," + Direct_PM25.ToString() + "," +
                SOA.ToString() + "," + (Amm_Sulfate + Amm_Bisulfate + SO4 + Amm_Nitrate + Direct_PM25 + SOA).ToString() + ',' + adjustment.ToString() );*/


            return value_PM25 + value_NO3 + value_SO4;

        }


    }
}
