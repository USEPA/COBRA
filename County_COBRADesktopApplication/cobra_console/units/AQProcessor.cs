using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.IO;


namespace cobra_console.units
{
    public class AQProcessor
    {
        public List<string> messages = new List<string>();
        public List<string> components = new List<string>();

        private SQLiteConnection _dbConn;
        private string _EF_connectionstring;

        public AQProcessor(SQLiteConnection dbConn, string EF_connectionstring)
        {
            _dbConn = dbConn;
            _EF_connectionstring = EF_connectionstring;
            //do pragmas
            //Synchronous = OFF; Journal Mode = OFF; Cache Size = -40000;
            executeQuery("PRAGMA synchronous = OFF");
            executeQuery("PRAGMA journal_mode = OFF");
            executeQuery("PRAGMA cache_size = 400000");
        }

        private void executeQuery(string query)
        {
            SQLiteCommand cmd = _dbConn.CreateCommand();
            cmd.CommandText = query;
            cmd.ExecuteNonQuery();
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
              + " sum(NH3) as NH3,"
              + " sum(SOA) as SOA,"
              + " sum(PM25) as PM25,"
              + " sum(VOC) as VOC"
              + " FROM "
              + emissions + " group by typeindx, sourceindx;";

            executeQuery(query);

            clearTable(destination);
            query = ""
               + " Insert into " + destination
               + " SELECT"
               + " SYS_Srmatrix.destindx,"
               + " sum(" + summarizedemissions + ".NOx * SYS_Srmatrix.c_NO3) as NOx,"
               + " sum(" + summarizedemissions + ".SO2 * SYS_Srmatrix.c_SO4) as SO2,"
               + " 0 as NH3,"
               + " sum(" + summarizedemissions + ".SOA * SYS_Srmatrix.c_PM25) * 28778 as SOA,"
               + " sum(" + summarizedemissions + ".PM25 * SYS_Srmatrix.c_PM25) as PM25,"
               + " sum(" + summarizedemissions + ".VOC * SYS_Srmatrix.c_O3V) as VOC,"
               + " sum(" + summarizedemissions + ".NOx * SYS_Srmatrix.c_O3N) as O3N"
               + " FROM"
               + " " + summarizedemissions
               + " INNER JOIN SYS_Srmatrix ON (" + summarizedemissions + ".sourceindx = SYS_Srmatrix.sourceindx and " + summarizedemissions + ".typeindx = SYS_Srmatrix.typeindx)"
               + " group by"
               + " SYS_Srmatrix.destindx";

            executeQuery(query);
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
            clearTable("Sys_Destination");

            //populate, also include adjustment factor
            string query = "";

            query = "Insert into SYS_Destination "
                    + "SELECT "
                    + "null as ID,  "
                    + destinationbase + ".destindx as destindx, "
                    + destinationbase + ".NOx as BASE_NOx, "
                    + destinationbase + ".SO2 as BASE_SO2, "
                    + destinationbase + ".NH3 as BASE_NH3, "
                    + destinationbase + ".SOA as BASE_SOA, "
                    + destinationbase + ".PM25 as BASE_PM25, "
                    + destinationbase + ".VOC as BASE_VOC, "
                    + destinationbase + ".O3N as BASE_O3N, "
                    + destinationcontrol + ".NOx as BASE_NOx, "
                    + destinationcontrol + ".SO2 BASE_SO2, "
                    + destinationcontrol + ".NH3 BASE_NH3, "
                    + destinationcontrol + ".SOA BASE_SOA, "
                    + destinationcontrol + ".PM25 BASE_PM25, "
                    + destinationcontrol + ".VOC BASE_VOC, "
                    + destinationcontrol + ".O3N BASE_O3N, "
                    + "SYS_ADJ.F1 as F, "
                    + "0 as BASE_FINAL_PM, "
                    + "0 as CTRL_FINAL_PM, "
                    + "0 as DELTA_FINAL_PM, "
                    + "0 as BASE_FINAL_O3, "
                    + "0 as CTRL_FINAL_O3, "
                    + "0 as DELTA_FINAL_O3 "
                    + "FROM  "
                    + "SYS_Destination_Base "
                    + "INNER JOIN " + destinationcontrol + " ON (" + destinationbase + ".destindx = " + destinationcontrol + ".destindx)  "
                    + "INNER JOIN SYS_ADJ ON (" + destinationbase + ".destindx = SYS_ADJ.indx) ";


            executeQuery(query);

            components.Add(@"FIPS,BASE_PM25,BASE_NO3,BASE_SO4,CTRL_PM5,CTRL_NO3,CTRL_SO4,DELTA_PM,BASE_O3N,BASE_VOC,CTRL_O3N,CTRL_VOC,DELTA_O3");



            //now do the PM stuff
            using (cobraEntities context = new cobraEntities(_EF_connectionstring))
            {
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
