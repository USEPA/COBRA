using CobraComputeAPI;
using CsvHelper;
using FastMember;
using ICSharpCode.SharpZipLib.Zip;
using MathNet.Numerics.Data.Text;
using MathNet.Numerics.LinearAlgebra;
using NCalc;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace CobraCompute
{
    public class CobraComputeCore
    {
        public bool initilized = false;

        private string datapath = "";

        private Matrix<double>[] SR_dp = new Matrix<double>[4];
        private Matrix<double>[] SR_no2 = new Matrix<double>[4];
        private Matrix<double>[] SR_so2 = new Matrix<double>[4];
        private Matrix<double>[] SR_nh3 = new Matrix<double>[4];

        public DataTable EmissionsInventory;
        public DataTable SummarizedEmissionsInventory;

        public ScenarioManager Scenarios;
        private UserScenario currentscenario;

        public Guid create_userscenario()
        {
            return Scenarios.createUserScenario();
        }
        public UserScenario retrieve_userscenario(Guid token)
        {
            currentscenario = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            currentscenario = Scenarios.retrieve(token);
            return currentscenario;
        }
        public void store_userscenario()
        {
            Scenarios.store(currentscenario);
        }
        public void delete_userscenario(Guid token)
        {
            Scenarios.deleteUserScenario(token);
        }

        private List<Cobra_POP> Populations = new List<Cobra_POP>();
        private List<Cobra_Incidence> Incidence = new List<Cobra_Incidence>();
        private List<Cobra_CR> CRfunctions = new List<Cobra_CR>();
        private List<Cobra_Valuation> Valuationfunctions = new List<Cobra_Valuation>();

        public List<Cobra_Dict_State> dict_state = new List<Cobra_Dict_State>();
        public List<Cobra_Dict_Tier> dict_tier = new List<Cobra_Dict_Tier>();

        private Vector<double> Adjustment = Vector<double>.Build.Dense(3080);
        private Dictionary<string, double> VOC2SOA = new Dictionary<string, double>();

        private Vector<double>[] aqbase;
        private Vector<double> pm_base;

        public StringBuilder statuslog;

        private CobraComputeAPI.RedisConfig redisOptions;

        public CobraComputeCore(CobraComputeAPI.RedisConfig redisOptions)
        {
            this.redisOptions = redisOptions;

            this.statuslog = new StringBuilder();

            EmissionsInventory = new DataTable("EmissionsInventory");

            EmissionsInventory.Columns.Add("ID", typeof(int));
            EmissionsInventory.Columns.Add("typeindx", typeof(int));
            EmissionsInventory.Columns.Add("sourceindx", typeof(int));
            EmissionsInventory.Columns.Add("stid", typeof(int));
            EmissionsInventory.Columns.Add("cyid", typeof(int));
            EmissionsInventory.Columns.Add("TIER1", typeof(int));
            EmissionsInventory.Columns.Add("TIER2", typeof(int));
            EmissionsInventory.Columns.Add("TIER3", typeof(int));
            EmissionsInventory.Columns.Add("NO2", typeof(double));
            EmissionsInventory.Columns.Add("SO2", typeof(double));
            EmissionsInventory.Columns.Add("NH3", typeof(double));
            EmissionsInventory.Columns.Add("SOA", typeof(double));
            EmissionsInventory.Columns.Add("PM25", typeof(double));
            EmissionsInventory.Columns.Add("VOC", typeof(double));
            EmissionsInventory.PrimaryKey = new DataColumn[] { EmissionsInventory.Columns["ID"] }; //key on recno 

        }

        private void CreateCSVFile(DataTable dt, string strFilePath)
        {
            StreamWriter sw = new StreamWriter(strFilePath, false);

            int iColCount = dt.Columns.Count;
            for (int i = 0; i < iColCount; i++)
            {
                sw.Write(dt.Columns[i]);
                if (i < iColCount - 1)
                {
                    sw.Write(",");
                }
            }
            sw.Write(sw.NewLine);

            foreach (DataRow dr in dt.Rows)
            {
                for (int i = 0; i < iColCount; i++)
                {
                    if (!Convert.IsDBNull(dr[i]))
                    {
                        sw.Write(dr[i].ToString());
                    }
                    if (i < iColCount - 1)
                    {
                        sw.Write(",");
                    }
                }
                sw.Write(sw.NewLine);
            }
            sw.Close();
        }

        private double ComputeSOAfromVOC(string key, double value)
        {
            double result = 0;
            double factor = 0;
            if (VOC2SOA.TryGetValue(key, out factor))
            {
                result = factor * value;
            }
            return result;
        }

        public string version()
        {
            return "V1.0";
        }
        public bool initialize(string path = "data/")
        {
            if (this.initilized) { return true; };

            bool result = true;
            datapath = path;

            //proceed setting up
            try
            {
                int recno = 1;

                //load pop
                recno = LoadPop(path);

                //load incidence
                recno = LoadIncidence(path);

                //load cr
                recno = LoadCR(path);

                //load valuation
                recno = LoadValue(path);

                //load dictionar(ies)
                recno = LoadDictionary_State(path);
                recno = LoadDictionary_Tier(path);

                //load adjustment factors
                recno = LoadAdjustments(path);

                //load voc2soa
                recno = LoadVOC2SOA(path);

                //load emissions
                recno = LoadEmissions(path);

                //debug
                recno = SummarizeEmissions();

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                //load matrix data
                LoadSRfrommtx(path);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // compute baseline AQ components
                aqbase = Vectorize(SummarizedEmissionsInventory);
                pm_base = computePM(aqbase[4], aqbase[0], aqbase[3], aqbase[2], aqbase[1]);

                Scenarios = new ScenarioManager(this.EmissionsInventory, this.redisOptions);
            }
            catch (Exception e)
            {
                statuslog.Append("error: " + e.Message);
                result = false;
            }
            if (!result)
            {
                datapath = "";
                EmissionsInventory.Clear();
            }
            this.initilized = result;
            return result;
        }

        private void InitBlankSR()
        {
            for (int i = 1; i < 5; i++)
            {
                SR_dp[i - 1] = Matrix<double>.Build.Dense(3080, 3080);
                SR_no2[i - 1] = Matrix<double>.Build.Dense(3080, 3080);
                SR_so2[i - 1] = Matrix<double>.Build.Dense(3080, 3080);
                SR_nh3[i - 1] = Matrix<double>.Build.Dense(3080, 3080);
            }
        }


        private void SaveSR2mtx(string path)
        {
            for (int i = 1; i < 5; i++)
            {
                MatrixMarketWriter.WriteMatrix(path + "matrix_dp_" + i.ToString() + ".mtx", SR_dp[i - 1], Compression.GZip);
                MatrixMarketWriter.WriteMatrix(path + "matrix_no2_" + i.ToString() + ".mtx", SR_no2[i - 1], Compression.GZip);
                MatrixMarketWriter.WriteMatrix(path + "matrix_so2_" + i.ToString() + ".mtx", SR_so2[i - 1], Compression.GZip);
                MatrixMarketWriter.WriteMatrix(path + "matrix_nh3_" + i.ToString() + ".mtx", SR_nh3[i - 1], Compression.GZip);
            }
        }


        private void LoadSRfrommtx(string path)
        {
            for (int i = 1; i < 5; i++)
            {
                SR_dp[i - 1] = MatrixMarketReader.ReadMatrix<double>(path + "matrix_dp_" + i.ToString() + ".mtx", Compression.GZip);
                SR_no2[i - 1] = MatrixMarketReader.ReadMatrix<double>(path + "matrix_no2_" + i.ToString() + ".mtx", Compression.GZip);
                SR_so2[i - 1] = MatrixMarketReader.ReadMatrix<double>(path + "matrix_so2_" + i.ToString() + ".mtx", Compression.GZip);
                SR_nh3[i - 1] = MatrixMarketReader.ReadMatrix<double>(path + "matrix_nh3_" + i.ToString() + ".mtx", Compression.GZip);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        private int LoadSR(string path)
        {
            int recno = 1;

            var zip = new ZipInputStream(File.OpenRead(path + "sys_srmatrix_2025.zip"));
            var filestream = new FileStream(path + "sys_srmatrix_2025.zip", FileMode.Open, FileAccess.Read);
            ZipFile zipfile = new ZipFile(filestream);
            ZipEntry item;

            while ((item = zip.GetNextEntry()) != null)
            {
                using (TextReader fileReader = new StreamReader(zipfile.GetInputStream(item)))
                {
                    CsvReader csv = new CsvReader(fileReader, System.Globalization.CultureInfo.CurrentCulture);
                    foreach (srrecord sr_record in csv.GetRecords<srrecord>())
                    {
                        var index2use = sr_record.typeindx - 1;
                        SR_dp[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.tx_dp.GetValueOrDefault(0);
                        SR_no2[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.tx_no2.GetValueOrDefault(0);
                        SR_so2[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.tx_so2.GetValueOrDefault(0);
                        SR_nh3[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.tx_nh3.GetValueOrDefault(0);
                    }
                }
            }
            return recno;
        }

        private int SummarizeEmissions()
        {
            int recno;
            var summarized = from row in EmissionsInventory.AsEnumerable()
                             group row by new { typeindx = row.Field<int>("typeindx"), sourceindx = row.Field<int>("sourceindx"), stid = row.Field<int>("stid"), cyid = row.Field<int>("cyid") } into grp
                             select new
                             {
                                 typeindx = grp.Key.typeindx,
                                 sourceindx = grp.Key.sourceindx,
                                 stid = grp.Key.stid,
                                 cyid = grp.Key.cyid,
                                 NO2 = grp.Sum(r => r.Field<double?>("NO2")),
                                 SO2 = grp.Sum(r => r.Field<double?>("SO2")),
                                 NH3 = grp.Sum(r => r.Field<double?>("NH3")),
                                 SOA = grp.Sum(r => r.Field<double?>("SOA")),
                                 PM25 = grp.Sum(r => r.Field<double?>("PM25")),
                                 VOC = grp.Sum(r => r.Field<double?>("VOC"))
                             };

            SummarizedEmissionsInventory = EmissionsInventory.Clone();

            recno = 1;
            foreach (var rowentry in summarized)
            {
                SummarizedEmissionsInventory.Rows.Add(new object[] { recno, rowentry.typeindx, rowentry.sourceindx, rowentry.stid, rowentry.cyid, 0, 0, 0, rowentry.NO2.GetValueOrDefault(0), rowentry.SO2.GetValueOrDefault(0), rowentry.NH3.GetValueOrDefault(0), rowentry.SOA.GetValueOrDefault(0), rowentry.PM25.GetValueOrDefault(0), rowentry.VOC.GetValueOrDefault(0) });
                recno++;
            }

            return recno;
        }

        public DataTable SummarizeEmissionsForExport(DataTable source)
        {
            int recno;
            var summarized = from row in source.AsEnumerable()
                             group row by new { typeindx = row.Field<int>("typeindx"), sourceindx = row.Field<int>("sourceindx"), stid = row.Field<int>("stid"), cyid = row.Field<int>("cyid") } into grp
                             select new
                             {
                                 typeindx = grp.Key.typeindx,
                                 sourceindx = grp.Key.sourceindx,
                                 stid = grp.Key.stid,
                                 cyid = grp.Key.cyid,
                                 NO2 = grp.Sum(r => r.Field<double?>("NO2")),
                                 SO2 = grp.Sum(r => r.Field<double?>("SO2")),
                                 NH3 = grp.Sum(r => r.Field<double?>("NH3")),
                                 SOA = grp.Sum(r => r.Field<double?>("SOA")),
                                 PM25 = grp.Sum(r => r.Field<double?>("PM25")),
                                 VOC = grp.Sum(r => r.Field<double?>("VOC"))
                             };

            DataTable result = source.Clone();

            recno = 1;
            foreach (var rowentry in summarized)
            {
                result.Rows.Add(new object[] { recno, rowentry.typeindx, rowentry.sourceindx, rowentry.stid, rowentry.cyid, 0, 0, 0, rowentry.NO2.GetValueOrDefault(0), rowentry.SO2.GetValueOrDefault(0), rowentry.NH3.GetValueOrDefault(0), rowentry.SOA.GetValueOrDefault(0), rowentry.PM25.GetValueOrDefault(0), rowentry.VOC.GetValueOrDefault(0) });
                recno++;
            }

            return result;
        }



        private int LoadEmissions(string path)
        {
            int recno;
            EmissionsInventory.Clear();
            recno = 1;
            using (TextReader fileReader = File.OpenText(path + "sys_emissions_inventory.csv"))
            {
                CsvReader csv = new CsvReader(fileReader, System.Globalization.CultureInfo.CurrentCulture);
                foreach (EmissionsRecord record in csv.GetRecords<EmissionsRecord>())
                {
                    if (record.ID == 1)
                    {
                        if (record.NO2 > 0.0 || record.NH3 > 0.0 || record.SOA > 0.0 || record.SO2 > 0.0 || record.PM25 > 0.0 || record.VOC > 0.0)
                        {
                            EmissionsInventory.Rows.Add(new object[] { recno, record.typeindx, record.sourceindx, record.stid, record.cyid, record.TIER1, record.TIER2, record.TIER3, record.NO2.GetValueOrDefault(0), record.SO2.GetValueOrDefault(0), record.NH3.GetValueOrDefault(0), ComputeSOAfromVOC(record.TIER1 + "|" + record.TIER2 + "|" + record.TIER3, record.VOC.GetValueOrDefault(0)), record.PM25.GetValueOrDefault(0), record.VOC.GetValueOrDefault(0) });
                            recno++;
                        }
                    }
                }
            }

            return recno;
        }

        private int LoadVOC2SOA(string path)
        {
            int recno;
            EmissionsInventory.Clear();
            recno = 1;
            using (TextReader fileReader = File.OpenText(path + "sys_voc2soa.csv"))
            {
                CsvReader csv = new CsvReader(fileReader, System.Globalization.CultureInfo.CurrentCulture);
                foreach (Cobra_Voc2Soa record in csv.GetRecords<Cobra_Voc2Soa>())
                {
                    VOC2SOA.Add(record.TIER1 + "|" + record.TIER2 + "|" + record.TIER3, record.FACTOR);
                    recno++;
                }
            }

            return recno;
        }

        private int LoadAdjustments(string path)
        {
            int recno = 1;
            using (TextReader fileReader = File.OpenText(path + "sys_adj.csv"))
            {
                CsvReader csv = new CsvReader(fileReader, System.Globalization.CultureInfo.CurrentCulture);
                foreach (Cobra_Adjustment record in csv.GetRecords<Cobra_Adjustment>())
                {
                    Adjustment[record.indx.GetValueOrDefault(0) - 1] = record.F1.GetValueOrDefault(0);
                    recno++;
                }
            }

            return recno;
        }

        private int LoadDictionary_State(string path)
        {
            int recno = 1;
            using (TextReader fileReader = File.OpenText(path + "sys_dict.csv"))
            {
                CsvReader csv = new CsvReader(fileReader, System.Globalization.CultureInfo.CurrentCulture);
                foreach (Cobra_Dict_State record in csv.GetRecords<Cobra_Dict_State>())
                {
                    dict_state.Add(record);
                    recno++;
                }
            }

            return recno;
        }

        private int LoadDictionary_Tier(string path)
        {
            int recno = 1;
            using (TextReader fileReader = File.OpenText(path + "sys_tiers.csv"))
            {
                CsvReader csv = new CsvReader(fileReader, System.Globalization.CultureInfo.CurrentCulture);
                foreach (Cobra_Dict_Tier record in csv.GetRecords<Cobra_Dict_Tier>())
                {
                    dict_tier.Add(record);
                    recno++;
                }
            }

            return recno;
        }


        private int LoadValue(string path)
        {
            int recno = 1;
            using (TextReader fileReader = File.OpenText(path + "sys_valuation_inventory.csv"))
            {
                CsvReader csv = new CsvReader(fileReader, System.Globalization.CultureInfo.CurrentCulture);
                foreach (Cobra_Valuation record in csv.GetRecords<Cobra_Valuation>())
                {
                    if (record.ID == 1) //2025
                    {
                        Valuationfunctions.Add(record);
                        recno++;
                    }
                }
            }

            return recno;
        }

        private int LoadCR(string path)
        {
            int recno = 1;
            using (TextReader fileReader = File.OpenText(path + "sys_cr_inventory.csv"))
            {
                CsvReader csv = new CsvReader(fileReader, System.Globalization.CultureInfo.CurrentCulture);
                foreach (Cobra_CR record in csv.GetRecords<Cobra_CR>())
                {
                    if (record.ID == 1) //2025
                    {
                        CRfunctions.Add(record);
                        recno++;
                    }
                }
            }

            return recno;
        }

        private int LoadIncidence(string path)
        {
            int recno = 1;
            using (TextReader fileReader = File.OpenText(path + "sys_incidence_inventory.csv"))
            {
                CsvReader csv = new CsvReader(fileReader, System.Globalization.CultureInfo.CurrentCulture);
                foreach (Cobra_Incidence record in csv.GetRecords<Cobra_Incidence>())
                {
                    if (record.ID == 1) //2025
                    {
                        Incidence.Add(record);
                        recno++;
                    }
                }
            }

            return recno;
        }

        private int LoadPop(string path)
        {
            int recno = 1;
            using (TextReader fileReader = File.OpenText(path + "sys_pop_inventory.csv"))
            {
                CsvReader csv = new CsvReader(fileReader, System.Globalization.CultureInfo.CurrentCulture);
                foreach (Cobra_POP record in csv.GetRecords<Cobra_POP>())
                {
                    if (record.ID == 1) //2025
                    {
                        Populations.Add(record);
                        recno++;
                    }
                }
            }

            return recno;
        }


        public string buildStringCriteria(EmissionsDataRetrievalRequest spec)
        {
            if (spec == null)
            {
                return "";
            }
            else
            {
                List<string> tierselector = new List<string>();
                List<string> locationselector = new List<string>();

                //do tiers first.
                string[] thetiers = spec.tiers.Split(",");
                for (int i = 0; i < thetiers.Length; i++)
                {
                    thetiers[i] = "TIER" + (i + 1).ToString() + "='" + thetiers[i] + "'";
                }

                //add tiers
                tierselector.Add("(" + String.Join(" and ", thetiers) + ")");

                //do FIPS codes

                //build a list of states / two digit fips codes
                string[] specifiedStates = (from rec in spec.fipscodes where rec.Length == 2 select rec).Distinct<string>().ToArray<string>();

                //build a list of counties / 5 digit fips codes
                string[] trueCounties = (from rec in spec.fipscodes where rec.Length == 5 select rec).Distinct<string>().ToArray<string>();

                string[] implicitStates = (from rec in trueCounties select rec.Substring(0, 2)).Distinct<string>().ToArray<string>();

                string[] actualStates = specifiedStates.Except(implicitStates).ToArray<string>();


                //do state selection selecting all per state as they are implicit by non expansion of the tree
                string stateselector = "(stid in ('" + String.Join("','", actualStates) + "'))";
                if (actualStates.Length > 0)
                {
                    locationselector.Add(stateselector);
                }

                //county  fipses with some math
                foreach (string state in implicitStates)
                {
                    string[] statefipses = (from rec in trueCounties where rec.Substring(0, 2).PadLeft(2, '0') == state.PadLeft(2, '0') select rec.Substring(2, 3)).Distinct<string>().ToArray<string>();
                    string fipsselector = "( stid ='" + state + "' and (cyid in ('" + String.Join("','", statefipses) + "')))";
                    locationselector.Add(fipsselector);
                }

                //locations are ORs
                string locationpart = "( " + String.Join(" or ", locationselector) + " )";
                //just join to tiers
                tierselector.Add(locationpart);

                string result = String.Join(" and ", tierselector);
                return result;
            }
        }

        public List<EmissionsRecord> GetBaseEmissions(string criteria)
        {
            List<EmissionsRecord> result = new List<EmissionsRecord>();

            DataRow[] rows = EmissionsInventory.Select(criteria);

            foreach (DataRow dr in rows)
            {
                EmissionsRecord rec = new EmissionsRecord();

                rec.ID = (dr.ItemArray[0] == System.DBNull.Value) ? 0 : (int)dr.ItemArray[0];
                rec.typeindx = (dr.ItemArray[1] == System.DBNull.Value) ? 0 : (int)dr.ItemArray[1];
                rec.sourceindx = (dr.ItemArray[2] == System.DBNull.Value) ? 0 : (int)dr.ItemArray[2];
                rec.stid = (dr.ItemArray[3] == System.DBNull.Value) ? 0 : (int)dr.ItemArray[3];
                rec.cyid = (dr.ItemArray[4] == System.DBNull.Value) ? 0 : (int)dr.ItemArray[4];
                rec.TIER1 = (dr.ItemArray[5] == System.DBNull.Value) ? 0 : (int)dr.ItemArray[5];
                rec.TIER2 = (dr.ItemArray[6] == System.DBNull.Value) ? 0 : (int)dr.ItemArray[6];
                rec.TIER3 = (dr.ItemArray[7] == System.DBNull.Value) ? 0 : (int)dr.ItemArray[7];
                rec.NO2 = (dr.ItemArray[8] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[8];
                rec.SO2 = (dr.ItemArray[9] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[9];
                rec.NH3 = (dr.ItemArray[10] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[10];
                rec.SOA = (dr.ItemArray[11] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[11];
                rec.PM25 = (dr.ItemArray[12] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[12];
                rec.VOC = (dr.ItemArray[13] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[13];
                result.Add(rec);
            }

            return result;
        }
        public List<EmissionsRecord> GetControlEmissions(string criteria)
        {
            List<EmissionsRecord> result = new List<EmissionsRecord>();

            DataRow[] rows = currentscenario.EmissionsData.Select(criteria);

            foreach (DataRow dr in rows)
            {
                EmissionsRecord rec = new EmissionsRecord();

                rec.ID = (dr.ItemArray[0] == System.DBNull.Value) ? 0 : (int)dr.ItemArray[0];
                rec.typeindx = (dr.ItemArray[1] == System.DBNull.Value) ? 0 : (int)dr.ItemArray[1];
                rec.sourceindx = (dr.ItemArray[2] == System.DBNull.Value) ? 0 : (int)dr.ItemArray[2];
                rec.stid = (dr.ItemArray[3] == System.DBNull.Value) ? 0 : (int)dr.ItemArray[3];
                rec.cyid = (dr.ItemArray[4] == System.DBNull.Value) ? 0 : (int)dr.ItemArray[4];
                rec.TIER1 = (dr.ItemArray[5] == System.DBNull.Value) ? 0 : (int)dr.ItemArray[5];
                rec.TIER2 = (dr.ItemArray[6] == System.DBNull.Value) ? 0 : (int)dr.ItemArray[6];
                rec.TIER3 = (dr.ItemArray[7] == System.DBNull.Value) ? 0 : (int)dr.ItemArray[7];
                rec.NO2 = (dr.ItemArray[8] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[8];
                rec.SO2 = (dr.ItemArray[9] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[9];
                rec.NH3 = (dr.ItemArray[10] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[10];
                rec.SOA = (dr.ItemArray[11] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[11];
                rec.PM25 = (dr.ItemArray[12] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[12];
                rec.VOC = (dr.ItemArray[13] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[13];
                result.Add(rec);
            }

            return result;
        }
        public bool SetControlEmissions(EmissionsRecord[] emissions)
        {
            DataTable emissionsData = currentscenario.EmissionsData;
            currentscenario.isDirty = true;

            foreach (EmissionsRecord emission in emissions)
            {
                //locate entry in
                DataRow foundRow = emissionsData.Rows.Find(emission.ID);
                if (foundRow != null)
                {
                    foundRow[8] = emission.NO2;
                    foundRow[9] = emission.SO2;
                    foundRow[10] = emission.NH3;
                    foundRow[11] = emission.SOA;
                    foundRow[12] = emission.PM25;
                    foundRow[13] = emission.VOC;
                }
                else
                {
                    emissionsData.Rows.Add(new object[] { emission.ID, emission.typeindx, emission.sourceindx, emission.stid, emission.cyid, emission.TIER1, emission.TIER2, emission.TIER3, emission.NO2.GetValueOrDefault(0), emission.SO2.GetValueOrDefault(0), emission.NH3.GetValueOrDefault(0), emission.SOA, emission.PM25.GetValueOrDefault(0), emission.VOC.GetValueOrDefault(0) });
                }
                //re-get
                DataRow foundRow2 = emissionsData.Rows.Find(emission.ID);
                if (foundRow2 != null)
                {
                    foundRow2[8] = emission.NO2;
                    foundRow2[9] = emission.SO2;
                    foundRow2[10] = emission.NH3;
                    foundRow2[11] = emission.SOA;
                    foundRow2[12] = emission.PM25;
                    foundRow2[13] = emission.VOC;
                }
            }
            return true;
        }

        private List<EmissionsRecord> GetSummarizedControlEmissions(Guid token, string criteria)
        {
            List<EmissionsRecord> result = new List<EmissionsRecord>();

            DataRow[] rows = SummarizeEmissionsbyType(currentscenario.EmissionsData).Select(criteria);

            foreach (DataRow dr in rows)
            {
                EmissionsRecord rec = new EmissionsRecord();

                rec.ID = (dr.ItemArray[0] == System.DBNull.Value) ? 0 : (int)dr.ItemArray[0];
                rec.typeindx = (dr.ItemArray[1] == System.DBNull.Value) ? 0 : (int)dr.ItemArray[1];
                rec.sourceindx = (dr.ItemArray[2] == System.DBNull.Value) ? 0 : (int)dr.ItemArray[2];
                rec.stid = (dr.ItemArray[3] == System.DBNull.Value) ? 0 : (int)dr.ItemArray[3];
                rec.cyid = (dr.ItemArray[4] == System.DBNull.Value) ? 0 : (int)dr.ItemArray[4];
                rec.TIER1 = (dr.ItemArray[5] == System.DBNull.Value) ? 0 : (int)dr.ItemArray[5];
                rec.TIER2 = (dr.ItemArray[6] == System.DBNull.Value) ? 0 : (int)dr.ItemArray[6];
                rec.TIER3 = (dr.ItemArray[7] == System.DBNull.Value) ? 0 : (int)dr.ItemArray[7];
                rec.NO2 = (dr.ItemArray[8] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[8];
                rec.SO2 = (dr.ItemArray[9] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[9];
                rec.NH3 = (dr.ItemArray[10] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[10];
                rec.SOA = (dr.ItemArray[11] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[11];
                rec.PM25 = (dr.ItemArray[12] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[12];
                rec.VOC = (dr.ItemArray[13] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[13];
                result.Add(rec);
            }

            return result;
        }
        public DataTable SummarizeEmissionsbyType_resulttable(DataTable table2summarize, String criteria)
        {
            DataTable summarizedemissionsData = table2summarize.Clone();

            DataRow[] subrows = table2summarize.Select(criteria);

            //fix up SOA from VOC
            foreach (DataRow record in subrows)
            {
                record[11] = ComputeSOAfromVOC(record[5] + "|" + record[6] + "|" + record[7], record.Field<double>(13));
            }

            if (criteria.IndexOf("cyid") > -1)
            {

                var summarized = from row in subrows.AsEnumerable()
                                 group row by new { typeindx = 0, sourceindx = row.Field<int>("sourceindx"), stid = row.Field<int>("stid"), cyid = row.Field<int>("cyid"), tier1 = row.Field<int>("TIER1"), tier2 = 0, tier3 = 0 } into grp
                                 select new
                                 {
                                     typeindx = grp.Key.typeindx,
                                     sourceindx = grp.Key.sourceindx,
                                     stid = grp.Key.stid,
                                     cyid = grp.Key.cyid,
                                     tier1 = grp.Key.tier1,
                                     tier2 = grp.Key.tier2,
                                     tier3 = grp.Key.tier3,
                                     NO2 = grp.Sum(r => r.Field<double?>("NO2")),
                                     SO2 = grp.Sum(r => r.Field<double?>("SO2")),
                                     NH3 = grp.Sum(r => r.Field<double?>("NH3")),
                                     SOA = grp.Sum(r => r.Field<double?>("SOA")),
                                     PM25 = grp.Sum(r => r.Field<double?>("PM25")),
                                     VOC = grp.Sum(r => r.Field<double?>("VOC"))
                                 };
                int recno = 1;
                foreach (var rowentry in summarized)
                {
                    summarizedemissionsData.Rows.Add(new object[] { recno, rowentry.typeindx, rowentry.sourceindx, rowentry.stid, rowentry.cyid, rowentry.tier1, rowentry.tier2, rowentry.tier3, rowentry.NO2.GetValueOrDefault(0), rowentry.SO2.GetValueOrDefault(0), rowentry.NH3.GetValueOrDefault(0), rowentry.SOA.GetValueOrDefault(0), rowentry.PM25.GetValueOrDefault(0), rowentry.VOC.GetValueOrDefault(0) });
                    recno++;
                }

            }
            else
            {
                int recno = 1;
                var summarized2 = from row in subrows.AsEnumerable()
                                  group row by new { typeindx = 0, sourceindx = 0, stid = row.Field<int>("stid"), cyid = 0, tier1 = row.Field<int>("TIER1"), tier2 = 0, tier3 = 0 } into grp
                                  select new
                                  {
                                      typeindx = grp.Key.typeindx,
                                      sourceindx = grp.Key.sourceindx,
                                      stid = grp.Key.stid,
                                      cyid = grp.Key.cyid,
                                      tier1 = grp.Key.tier1,
                                      tier2 = grp.Key.tier2,
                                      tier3 = grp.Key.tier3,
                                      NO2 = grp.Sum(r => r.Field<double?>("NO2")),
                                      SO2 = grp.Sum(r => r.Field<double?>("SO2")),
                                      NH3 = grp.Sum(r => r.Field<double?>("NH3")),
                                      SOA = grp.Sum(r => r.Field<double?>("SOA")),
                                      PM25 = grp.Sum(r => r.Field<double?>("PM25")),
                                      VOC = grp.Sum(r => r.Field<double?>("VOC"))
                                  };
                foreach (var rowentry in summarized2)
                {
                    summarizedemissionsData.Rows.Add(new object[] { recno, rowentry.typeindx, 0, rowentry.stid, rowentry.cyid, rowentry.tier1, rowentry.tier2, rowentry.tier3, rowentry.NO2.GetValueOrDefault(0), rowentry.SO2.GetValueOrDefault(0), rowentry.NH3.GetValueOrDefault(0), rowentry.SOA.GetValueOrDefault(0), rowentry.PM25.GetValueOrDefault(0), rowentry.VOC.GetValueOrDefault(0) });
                    recno++;
                }
            };


            return summarizedemissionsData;
        }
        public DataTable SummarizeEmissionsbyType(DataTable table2summarize)
        {
            table2summarize.AcceptChanges();

            DataTable summarizedemissionsData = table2summarize.Clone();

            //fix up SOA from VOC
            foreach (DataRow record in table2summarize.Rows)
            {
                record[11] = ComputeSOAfromVOC(record[5] + "|" + record[6] + "|" + record[7], record.Field<double>(13));
            }


            var summarized = from row in table2summarize.AsEnumerable()
                             group row by new { typeindx = row.Field<int>("typeindx"), sourceindx = row.Field<int>("sourceindx"), stid = row.Field<int>("stid"), cyid = row.Field<int>("cyid") } into grp
                             select new
                             {
                                 typeindx = grp.Key.typeindx,
                                 sourceindx = grp.Key.sourceindx,
                                 stid = grp.Key.stid,
                                 cyid = grp.Key.cyid,
                                 NO2 = grp.Sum(r => r.Field<double?>("NO2")),
                                 SO2 = grp.Sum(r => r.Field<double?>("SO2")),
                                 NH3 = grp.Sum(r => r.Field<double?>("NH3")),
                                 SOA = grp.Sum(r => r.Field<double?>("SOA")),
                                 PM25 = grp.Sum(r => r.Field<double?>("PM25")),
                                 VOC = grp.Sum(r => r.Field<double?>("VOC"))
                             };

            int recno = 1;
            foreach (var rowentry in summarized)
            {
                summarizedemissionsData.Rows.Add(new object[] { recno, rowentry.typeindx, rowentry.sourceindx, rowentry.stid, 0, 0, 0, 0, rowentry.NO2.GetValueOrDefault(0), rowentry.SO2.GetValueOrDefault(0), rowentry.NH3.GetValueOrDefault(0), rowentry.SOA.GetValueOrDefault(0), rowentry.PM25.GetValueOrDefault(0), rowentry.VOC.GetValueOrDefault(0) });
                recno++;
            }

            return summarizedemissionsData;
        }
        public EmissionsSums SummarizeBaseControlEmissionsWithCriteria_resulttable(string criteria)
        {
            EmissionsSums result = new EmissionsSums();
            result.baseline = SummarizeEmissionsbyType_resulttable(this.EmissionsInventory, criteria);
            result.control = SummarizeEmissionsbyType_resulttable(currentscenario.EmissionsData, criteria);
            return result;
        }

        public EmissionsSums SummarizeBaseControlEmissionsWithCriteria(string criteria)
        {
            EmissionsSums result = new EmissionsSums();
            result.baseline = SummarizeEmissionsWithCriteria(this.EmissionsInventory, criteria);
            result.control = SummarizeEmissionsWithCriteria(currentscenario.EmissionsData, criteria);
            return result;
        }


        public DataTable SummarizeEmissionsWithCriteria(DataTable table2summarize, string criteria)
        {
            DataTable summarizedemissionsData = table2summarize.Clone();


            DataRow[] subrows = table2summarize.Select(criteria);

            //fix up SOA from VOC
            foreach (DataRow record in subrows)
            {
                record[11] = ComputeSOAfromVOC(record[5] + "|" + record[6] + "|" + record[7], record.Field<double>(13));
            }


            var summarized = from row in subrows.AsEnumerable()
                             group row by new { typeindx = 0, sourceindx = 0, stid = 0, cyid = 0 } into grp
                             select new
                             {
                                 typeindx = grp.Key.typeindx,
                                 sourceindx = grp.Key.sourceindx,
                                 stid = grp.Key.stid,
                                 cyid = grp.Key.cyid,
                                 NO2 = grp.Sum(r => r.Field<double?>("NO2")),
                                 SO2 = grp.Sum(r => r.Field<double?>("SO2")),
                                 NH3 = grp.Sum(r => r.Field<double?>("NH3")),
                                 SOA = grp.Sum(r => r.Field<double?>("SOA")),
                                 PM25 = grp.Sum(r => r.Field<double?>("PM25")),
                                 VOC = grp.Sum(r => r.Field<double?>("VOC"))
                             };

            int recno = 1;
            foreach (var rowentry in summarized)
            {
                summarizedemissionsData.Rows.Add(new object[] { recno, rowentry.typeindx, rowentry.sourceindx, rowentry.stid, rowentry.cyid, 0, 0, 0, rowentry.NO2.GetValueOrDefault(0), rowentry.SO2.GetValueOrDefault(0), rowentry.NH3.GetValueOrDefault(0), rowentry.SOA.GetValueOrDefault(0), rowentry.PM25.GetValueOrDefault(0), rowentry.VOC.GetValueOrDefault(0) });
                recno++;
            }

            return summarizedemissionsData;
        }


        public bool UpdateEmissionsWithCriteria(EmissionsDataUpdateRequest requestparams)
        {
            UpdateEmissionsWithCriteria(currentscenario.EmissionsData, requestparams);
            return true;
        }

        public bool UpdateEmissionsWithCriteria(DataTable table2summarize, EmissionsDataUpdateRequest requestparams)
        {
            //built selection criteria
            string str_criteria = buildStringCriteria(requestparams.spec);

            //get current sums
            EmissionsSums current = new EmissionsSums();
            current.control = SummarizeEmissionsWithCriteria(table2summarize, str_criteria);

            //get the records that were used to create sums
            DataRow[] subrows = table2summarize.Select(str_criteria);

            int rowcount = subrows.Count();
            if (rowcount > 0)  //easy case, there are actually emissions on record
            {
                //get current values
                double current_pm25 = current.control.Rows[0].Field<double?>("PM25").GetValueOrDefault(0);
                double current_no2 = current.control.Rows[0].Field<double?>("NO2").GetValueOrDefault(0);
                double current_so2 = current.control.Rows[0].Field<double?>("SO2").GetValueOrDefault(0);
                double current_nh3 = current.control.Rows[0].Field<double?>("NH3").GetValueOrDefault(0);
                double current_voc = current.control.Rows[0].Field<double?>("VOC").GetValueOrDefault(0);
                double current_soa = current.control.Rows[0].Field<double?>("SOA").GetValueOrDefault(0);

                //determine ratios
                double ratio_pm25 = current_pm25 == 0 ? 0 : requestparams.payload.PM25 / current_pm25;
                double ratio_no2 = current_no2 == 0 ? 0 : requestparams.payload.NO2 / current_no2;
                double ratio_so2 = current_so2 == 0 ? 0 : requestparams.payload.SO2 / current_so2;
                double ratio_nh3 = current_nh3 == 0 ? 0 : requestparams.payload.NH3 / current_nh3;
                double ratio_voc = current_voc == 0 ? 0 : requestparams.payload.VOC / current_voc;
                double ratio_soa = current_soa == 0 ? 0 : requestparams.payload.SOA / current_soa;

                foreach (DataRow record in subrows)
                {
                    //get current value
                    double thisrow_pm25 = record.Field<double?>("PM25").GetValueOrDefault(0);
                    double thisrow_no2 = record.Field<double?>("NO2").GetValueOrDefault(0);
                    double thisrow_so2 = record.Field<double?>("SO2").GetValueOrDefault(0);
                    double thisrow_nh3 = record.Field<double?>("NH3").GetValueOrDefault(0);
                    double thisrow_voc = record.Field<double?>("VOC").GetValueOrDefault(0);
                    double thisrow_soa = record.Field<double?>("SOA").GetValueOrDefault(0);
                    //and set, use ration if sum<>0 otherwise spread the increase over all
                    record["PM25"] = current_pm25 != 0 ? Math.Max(thisrow_pm25 * ratio_pm25, 0D) : Math.Max(requestparams.payload.PM25 / rowcount, 0D);
                    record["NO2"] = current_no2 != 0 ? Math.Max(thisrow_no2 * ratio_no2, 0D) : Math.Max(requestparams.payload.NO2 / rowcount, 0D);
                    record["SO2"] = current_so2 != 0 ? Math.Max(thisrow_so2 * ratio_so2, 0D) : Math.Max(requestparams.payload.SO2 / rowcount, 0D);
                    record["NH3"] = current_nh3 != 0 ? Math.Max(thisrow_nh3 * ratio_nh3, 0D) : Math.Max(requestparams.payload.NH3 / rowcount, 0D);
                    record["VOC"] = current_voc != 0 ? Math.Max(thisrow_voc * ratio_voc, 0D) : Math.Max(requestparams.payload.VOC / rowcount, 0D);
                    record["SOA"] = current_soa != 0 ? Math.Max(thisrow_soa * ratio_soa, 0D) : Math.Max(requestparams.payload.SOA / rowcount, 0D);
                }
            }
            else
            { //no emissions records on file, this means we need to create them
                DataRow[] tiers = getmatchingtiers(requestparams.spec);
                DataRow[] fipscodes = getmatchingfipscodes(requestparams.spec);
                int[] stack_typeindex = new int[4] { 1, 2, 3, 4 };
                //supposedly this is the fastest....
                int minID = int.MaxValue;
                int maxID = int.MinValue;
                foreach (DataRow dr in table2summarize.Rows)
                {
                    int curID = dr.Field<int>("ID");
                    minID = Math.Min(minID, curID);
                    maxID = Math.Max(maxID, curID);
                }
                int numberofrowstoadd = tiers.Length * fipscodes.Length * stack_typeindex.Length;

                foreach (DataRow tier in tiers)
                {
                    foreach (DataRow fips in fipscodes)
                    {
                        foreach (int stackheight in stack_typeindex)
                        {
                            maxID++; //next id as primary key
                            table2summarize.Rows.Add(new object[] { maxID,
                                stackheight,
                                fips["SOURCEINDX"],
                                fips["STFIPS"],
                                fips["CNTYFIPS"],
                                tier["TIER1"],
                                tier["TIER2"],
                                tier["TIER3"],
                                Math.Max(requestparams.payload.NO2 / numberofrowstoadd, 0D),   //watch order
                                Math.Max(requestparams.payload.SO2 / numberofrowstoadd, 0D),
                                Math.Max(requestparams.payload.NH3 / numberofrowstoadd, 0D),
                                Math.Max(requestparams.payload.SOA / numberofrowstoadd, 0D),
                                Math.Max(requestparams.payload.PM25 / numberofrowstoadd, 0D),
                                Math.Max(requestparams.payload.VOC / numberofrowstoadd, 0D) });

                        }
                    }
                }

            }

            //fix up SOA from VOC
            foreach (DataRow record in subrows)
            {
                record[11] = ComputeSOAfromVOC(record[5] + "|" + record[6] + "|" + record[7], record.Field<double>(13));
            }

            return true;
        }

        private DataRow[] getmatchingfipscodes(EmissionsDataRetrievalRequest spec)
        {
            List<string> locationselector = new List<string>();

            //build a list of states / two digit fips codes
            string[] specifiedStates = (from rec in spec.fipscodes where rec.Length == 2 select rec).Distinct<string>().ToArray<string>();

            //build a list of counties / 5 digit fips codes
            string[] trueCounties = (from rec in spec.fipscodes where rec.Length == 5 select rec).Distinct<string>().ToArray<string>();
            string[] implicitStates = (from rec in trueCounties select rec.Substring(0, 2)).Distinct<string>().ToArray<string>();
            string[] actualStates = specifiedStates.Except(implicitStates).ToArray<string>();

            //do state selection selecting all per state as they are implicit by non expansion of the tree
            string stateselector = "(stfips in ('" + String.Join("','", actualStates) + "'))";
            if (actualStates.Length > 0)
            {
                locationselector.Add(stateselector);
            }

            //county  fipses with some math
            foreach (string state in implicitStates)
            {
                string[] statefipses = (from rec in trueCounties where rec.Substring(0, 2).PadLeft(2, '0') == state.PadLeft(2, '0') select rec.Substring(2, 3)).Distinct<string>().ToArray<string>();
                string fipsselector = "( stfips ='" + state + "' and (cntyfips in ('" + String.Join("','", statefipses) + "')))";
                locationselector.Add(fipsselector);
            }

            //locations are ORs
            string locationpart = "( " + String.Join(" or ", locationselector) + " )";


            DataTable table = new DataTable();
            using (var reader = ObjectReader.Create(dict_state.AsEnumerable<Cobra_Dict_State>()))
            {
                table.Load(reader);
            }

            return table.Select(locationpart);
        }

        private DataRow[] getmatchingtiers(EmissionsDataRetrievalRequest spec)
        {
            //do tiers first.
            string[] thetiers = spec.tiers.Split(",");
            for (int i = 0; i < thetiers.Length; i++)
            {
                thetiers[i] = "TIER" + (i + 1).ToString() + "='" + thetiers[i] + "'";
            }

            //add tiers
            string tierselector = "(" + String.Join(" and ", thetiers) + ")";

            DataTable table = new DataTable();
            using (var reader = ObjectReader.Create(dict_tier.AsEnumerable<Cobra_Dict_Tier>()))
            {
                table.Load(reader);
            }

            return table.Select(tierselector);
        }

        private Vector<double>[] Vectorize(DataTable emissions)
        {
            Vector<double>[] Vctr_pm_partial = new Vector<double>[4];
            Vector<double>[] Vctr_no2_partial = new Vector<double>[4];
            Vector<double>[] Vctr_so2_partial = new Vector<double>[4];
            Vector<double>[] Vctr_nh3_partial = new Vector<double>[4];
            Vector<double>[] Vctr_voc_partial = new Vector<double>[4];
            Vector<double>[] Vctr_soa_partial = new Vector<double>[4];

            Vector<double>[] Vctr_pm = new Vector<double>[4];
            Vector<double>[] Vctr_no2 = new Vector<double>[4];
            Vector<double>[] Vctr_so2 = new Vector<double>[4];
            Vector<double>[] Vctr_nh3 = new Vector<double>[4];
            Vector<double>[] Vctr_voc = new Vector<double>[4];
            Vector<double>[] Vctr_soa = new Vector<double>[4];

            for (int i = 1; i < 5; i++)
            {
                Vctr_pm[i - 1] = Vector<double>.Build.Dense(3080, 0);
                Vctr_no2[i - 1] = Vector<double>.Build.Dense(3080, 0);
                Vctr_so2[i - 1] = Vector<double>.Build.Dense(3080, 0);
                Vctr_nh3[i - 1] = Vector<double>.Build.Dense(3080, 0);
                Vctr_voc[i - 1] = Vector<double>.Build.Dense(3080, 0);
                Vctr_soa[i - 1] = Vector<double>.Build.Dense(3080, 0);
            }

            foreach (DataRow row in emissions.Rows)
            {
                int typeindex2use = row.Field<int>("typeindx") - 1;
                int sourceindex2use = row.Field<int>("sourceindx") - 1;
                Vctr_pm[typeindex2use][sourceindex2use] = row.Field<double?>("PM25").GetValueOrDefault(0);
                Vctr_no2[typeindex2use][sourceindex2use] = row.Field<double?>("NO2").GetValueOrDefault(0);
                Vctr_so2[typeindex2use][sourceindex2use] = row.Field<double?>("SO2").GetValueOrDefault(0);
                Vctr_nh3[typeindex2use][sourceindex2use] = row.Field<double?>("NH3").GetValueOrDefault(0);
                Vctr_voc[typeindex2use][sourceindex2use] = row.Field<double?>("VOC").GetValueOrDefault(0);
                Vctr_soa[typeindex2use][sourceindex2use] = row.Field<double?>("SOA").GetValueOrDefault(0);
            }

            for (int i = 1; i < 5; i++)
            {   //check these
                Vctr_pm_partial[i - 1] = SR_dp[i - 1].Multiply(Vctr_pm[i - 1]) * 28778;
                Vctr_no2_partial[i - 1] = SR_no2[i - 1].Multiply(Vctr_no2[i - 1]) * 28778 * (62.0049 / 46.0055);
                Vctr_so2_partial[i - 1] = SR_so2[i - 1].Multiply(Vctr_so2[i - 1]) * 28778 * (96.0626 / 64.0638);
                Vctr_nh3_partial[i - 1] = SR_nh3[i - 1].Multiply(Vctr_nh3[i - 1]) * 28778 * (18.03846 / 17.03052);
                Vctr_voc_partial[i - 1] = SR_dp[i - 1].Multiply(Vctr_voc[i - 1]) * 0;   //voc short range
                Vctr_soa_partial[i - 1] = SR_dp[i - 1].Multiply(Vctr_soa[i - 1]) * 28778; //transfers like pm
            }

            Vctr_pm_partial[0] = Vctr_pm_partial[0] + Vctr_pm_partial[1] + Vctr_pm_partial[2] + Vctr_pm_partial[3];
            Vctr_no2_partial[0] = Vctr_no2_partial[0] + Vctr_no2_partial[1] + Vctr_no2_partial[2] + Vctr_no2_partial[3];
            Vctr_so2_partial[0] = Vctr_so2_partial[0] + Vctr_so2_partial[1] + Vctr_so2_partial[2] + Vctr_so2_partial[3];
            Vctr_nh3_partial[0] = Vctr_nh3_partial[0] + Vctr_nh3_partial[1] + Vctr_nh3_partial[2] + Vctr_nh3_partial[3];
            Vctr_voc_partial[0] = Vctr_voc_partial[0] + Vctr_voc_partial[1] + Vctr_voc_partial[2] + Vctr_voc_partial[3];
            Vctr_soa_partial[0] = Vctr_soa_partial[0] + Vctr_soa_partial[1] + Vctr_soa_partial[2] + Vctr_soa_partial[3];

            //reshuffle oder to work with computepm
            // old order : destindx,NO2,SO2,NH3,SOA,PM25,VOC
            return new Vector<double>[6] { Vctr_no2_partial[0], Vctr_so2_partial[0], Vctr_nh3_partial[0], Vctr_soa_partial[0], Vctr_pm_partial[0], Vctr_voc_partial[0] };
        }

        public List<Cobra_ResultDetail> GetResults()
        {
            if (currentscenario.isDirty)
            {
                ComputeDeltaPM();
            }
            return currentscenario.Impacts;
        }


        public Vector<double> computePM(Vector<double> value_PM25, Vector<double> value_NO3, Vector<double> value_SOA, Vector<double> value_NH4, Vector<double> value_SO4)
        {
            Vector<double> result_pm = Vector<double>.Build.Dense(3080, 0);

            for (int i = 0; i < 3080; i++)
            {
                result_pm[i] = computePM(value_PM25[i], value_NO3[i], value_SOA[i], value_NH4[i], value_SOA[i], Adjustment[i]);
            }

            return result_pm;
        }

        public double computePM(double value_PM25, double value_NO3, double value_SOA, double value_NH4, double value_SO4, double adjustment = 1.0)
        {
            double result_pm = 0;
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
            result_pm = adjustment * (Amm_Sulfate + Amm_Bisulfate + SO4 + Amm_Nitrate + Direct_PM25 + SOA);

            return result_pm;
        }


        public bool ComputeDeltaPM()
        {
            currentscenario.EmissionsData.AcceptChanges();

            DataTable controlemissions = SummarizeEmissionsbyType(currentscenario.EmissionsData);

            var aqcontrol = Vectorize(controlemissions);

            // old order : destindx,NO2,SO2,NH3,SOA,PM25,VOC
            //                      0    1  2   3    4   5
            Vector<double> pm_control = computePM(aqcontrol[4], aqcontrol[0], aqcontrol[3], aqcontrol[2], aqcontrol[1]);
            var pm_delta = this.pm_base - pm_control;

            List<Cobra_Destination> Destinations = new List<Cobra_Destination>();

            //populate
            for (int i = 0; i < 3080; i++)
            {
                Cobra_Destination dest = new Cobra_Destination();
                dest.destindx = i + 1;
                dest.BASE_NO2 = 0;
                dest.BASE_SO2 = 0;
                dest.BASE_NH3 = 0;
                dest.BASE_SOA = 0;
                dest.BASE_PM25 = 0; //direct
                dest.BASE_VOC = 0;
                dest.CTRL_NO2 = 0;
                dest.CTRL_SO2 = 0;
                dest.CTRL_NH3 = 0;
                dest.CTRL_SOA = 0;
                dest.CTRL_PM25 = 0;  //direct
                dest.CTRL_VOC = 0;
                dest.F = 0;
                dest.BASE_FINAL_PM = pm_base[i];
                dest.CTRL_FINAL_PM = pm_control[i];
                dest.DELTA_FINAL_PM = pm_delta[i];
                Destinations.Add(dest);
            }

            //compute part 2
            currentscenario.Impacts = ComputeImpacts(Destinations, true);

            currentscenario.isDirty = false;

            return true;
        }

        private double crfunc(string rawfunction, string compfunction, double Incidence, double Beta, double DELTAQ, double POP, double A, double B, double C)
        {
            switch (compfunction)
            {
                case "(1-(1/((1-INCIDENCE)*EXP(BETA*DELTAQ)+INCIDENCE)))*INCIDENCE*POP":
                    return (1 - (1 / ((1 - Incidence) * Math.Exp(Beta * DELTAQ) + Incidence))) * Incidence * POP;
                case "(1-(1/((1-INCIDENCE)*EXP(BETA*DELTAQ)+INCIDENCE)))*INCIDENCE*A*POP":
                    return (1 - (1 / ((1 - Incidence) * Math.Exp(Beta * DELTAQ) + Incidence))) * Incidence * A * POP;
                case "(1-(1/((1-INCIDENCE*A)*EXP(BETA*DELTAQ)+INCIDENCE*A)))*INCIDENCE*A*POP":
                    return (1 - (1 / ((1 - Incidence * A) * Math.Exp(Beta * DELTAQ) + Incidence * A))) * Incidence * A * POP;
                case "(1-(1/EXP(BETA*DELTAQ)))*INCIDENCE*A*POP":
                    return (1 - (1 / Math.Exp(Beta * DELTAQ))) * Incidence * POP * A;
                case "(1-(1/EXP(BETA*DELTAQ)))*INCIDENCE*POP*A":
                    return (1 - (1 / Math.Exp(Beta * DELTAQ))) * Incidence * POP * A;
                case "(1-EXP(-BETA*DELTAQ))*INCIDENCE*POP*A":  //alternate notation
                    return (1 - Math.Exp(-Beta * DELTAQ)) * Incidence * POP * A;
                case "(1-EXP(-BETA*DELTAQ))*INCIDENCE*A*POP":  //alternate notation
                    return (1 - Math.Exp(-Beta * DELTAQ)) * Incidence * POP * A;
                case "(A-(A/((1-A)*EXP(BETA*DELTAQ)+A)))*POP*B":
                    return (A - (A / ((1 - A) * Math.Exp(Beta * DELTAQ) + A))) * POP * B;
                case "(1-(1/((1-A)*EXP(BETA*DELTAQ)+A)))*A*POP*B":
                    return (1 - (1 / ((1 - A) * Math.Exp(Beta * DELTAQ) + A))) * A * POP * B;
                case "(1-EXP(-BETA*DELTAQ))*INCIDENCE*POP":
                    return (1 - Math.Exp(-Beta * DELTAQ)) * Incidence * POP;
                case "(INCIDENCE-(INCIDENCE/((1-INCIDENCE)*EXP(BETA*DELTAQ)+INCIDENCE)))*POP":
                    return (Incidence - (Incidence / ((1 - Incidence) * Math.Exp(Beta * DELTAQ) + Incidence))) * POP;
                case "(1-(1/EXP(BETA*DELTAQ)))*INCIDENCE*POP":
                    return (1 - (1 / Math.Exp(Beta * DELTAQ))) * Incidence * POP;
                case "(1-(1/((1-A)*EXP(BETA*DELTAQ)+A)))*A*POP":
                    return (1 - (1 / ((1 - A) * Math.Exp(Beta * DELTAQ) + A))) * A * POP;
                case "(1-(1/EXP(BETA*DELTAQ)))*A*POP":
                    return (1 - (1 / Math.Exp(Beta * DELTAQ))) * A * POP;
                default:
                    Expression e = new Expression(rawfunction);
                    e.Parameters["A"] = A;
                    e.Parameters["B"] = B;
                    e.Parameters["C"] = C;
                    e.Parameters["Beta"] = Beta;
                    e.Parameters["DELTAQ"] = DELTAQ;
                    e.Parameters["Incidence"] = Incidence;
                    e.Parameters["POP"] = POP;
                    return (double)e.Evaluate();
            }
        }
        public List<Cobra_ResultDetail> ComputeImpacts(List<Cobra_Destination> Destinations, bool valat3)
        {
            Dictionary<string, Result> results_cr = new Dictionary<string, Result>();
            Dictionary<string, Result> results_valuation = new Dictionary<string, Result>();


            List<string> Endpoints_cr = CRfunctions.Select(c => c.Endpoint).Distinct().ToList();
            List<string> Endpoints_val = Valuationfunctions.Select(c => c.Endpoint).Distinct().ToList();

            Dictionary<long?, Cobra_POP> dict_pop = new Dictionary<long?, Cobra_POP>(this.Populations.Count());
            foreach (var item in Populations)
            {
                dict_pop.Add(item.DestinationID, item);
            }

            Dictionary<string, Cobra_Incidence> dict_incidence = new Dictionary<string, Cobra_Incidence>();
            foreach (var item in Incidence)
            {
                dict_incidence.Add(item.DestinationID.ToString() + "|" + item.Endpoint, item);
            }


            Cobra_Incidence value;
            Result result_cr;
            Result result_valuation;
            Cobra_Incidence incidencerow;

            foreach (var crfunc in CRfunctions)
            {
                string function = crfunc.Function;

                function = function.Replace("EXP", "Exp");
                function = function.Replace("exp", "Exp");

                string cleanfunction = function.ToUpper().Replace(" ", "");

                double metric_adjustment = 1;
                if (crfunc.Seasonal_Metric.ToUpper() == "DAILY")
                {
                    metric_adjustment = 365;
                }

                foreach (var destination in Destinations)
                {
                    //fixed params
                    Expression e = new Expression(function);
                    double A = crfunc.A.GetValueOrDefault(0);
                    double B = crfunc.B.GetValueOrDefault(0);
                    double C = crfunc.C.GetValueOrDefault(0);
                    double Beta = crfunc.Beta.GetValueOrDefault(0);
                    double DELTAQ = destination.DELTA_FINAL_PM.GetValueOrDefault(0);
                    double Incidence = 0;

                    //year dependent but with the twist that pop and incidence are containing all year data
                    Cobra_POP poprow = dict_pop[destination.destindx];
                    if (dict_incidence.TryGetValue(destination.destindx + "|" + crfunc.IncidenceEndpoint, out value))
                    {
                        incidencerow = value;
                    }
                    else
                    {
                        incidencerow = null;
                        Incidence = 0;
                    }

                    double poolingweight = crfunc.PoolingWeight.GetValueOrDefault(0);
                    for (long age = crfunc.Start_Age.GetValueOrDefault(0); age <= crfunc.End_Age.GetValueOrDefault(0); age++)
                    {
                        double POP = poprow.popat(age);
                        if (incidencerow != null)
                        {
                            Incidence = value.incidenceat(age);
                        }

                        double result = this.crfunc(function, cleanfunction, Incidence, Beta, DELTAQ, POP, A, B, C) * poolingweight * metric_adjustment;

                        // check if there is an entry already to make pooling work
                        if (results_cr.TryGetValue(destination.destindx + "|" + crfunc.Endpoint, out result_cr))
                        {
                            result_cr.Value = result_cr.Value + result;
                        }
                        else
                        {
                            results_cr.Add(destination.destindx + "|" + crfunc.Endpoint, new Result { Destinationindex = destination.destindx.GetValueOrDefault(0), Endpoint = crfunc.Endpoint, Value = result });
                        }

                        // add to national total
                        if (results_cr.TryGetValue("nation|" + crfunc.Endpoint, out result_cr))
                        {
                            result_cr.Value = result_cr.Value + result;
                        }
                        else
                        {
                            results_cr.Add("nation|" + crfunc.Endpoint, new Result { Destinationindex = destination.destindx.GetValueOrDefault(0), Endpoint = crfunc.Endpoint, Value = result });
                        }
                    }
                }
            }

            foreach (var valuefunc in Valuationfunctions)
            {
                string function = valuefunc.Function;

                function = function.Replace("EXP", "Exp");
                function = function.Replace("exp", "Exp");

                string cleanfunction = function.ToUpper().Replace(" ", "");

                double metric_adjustment = 1;
                if (valuefunc.Seasonal_Metric.ToUpper() == "DAILY")
                {
                    metric_adjustment = 365;
                }

                foreach (var destination in Destinations)
                {
                    //fixed params
                    Expression e = new Expression(function);
                    double A = valuefunc.A.GetValueOrDefault(0);
                    double B = valuefunc.B.GetValueOrDefault(0);
                    double C = valuefunc.C.GetValueOrDefault(0);
                    double Beta = valuefunc.Beta.GetValueOrDefault(0);
                    double DELTAQ = destination.DELTA_FINAL_PM.GetValueOrDefault(0);
                    double Incidence = 0;

                    //year dependent but with the twist that pop and incidence are containing all year data
                    Cobra_POP poprow = dict_pop[destination.destindx];
                    if (dict_incidence.TryGetValue(destination.destindx + "|" + valuefunc.IncidenceEndpoint, out value))
                    {
                        incidencerow = value;
                    }
                    else
                    {
                        incidencerow = null;
                        Incidence = 0;
                    }

                    double poolingweight = valuefunc.PoolingWeight.GetValueOrDefault(0);

                    for (long age = valuefunc.Start_Age.GetValueOrDefault(0); age <= valuefunc.End_Age.GetValueOrDefault(0); age++)
                    {
                        double POP = poprow.popat(age);
                        if (incidencerow != null)
                        {
                            Incidence = value.incidenceat(age);
                        }

                        double result = this.crfunc(function, cleanfunction, Incidence, Beta, DELTAQ, POP, A, B, C) * poolingweight * metric_adjustment;

                        if (valat3)
                        {
                            result = result * valuefunc.valat3pct.GetValueOrDefault(0) * 1.1225;
                        }
                        else
                        {
                            result = result * valuefunc.valat7pct.GetValueOrDefault(0) * 1.1225;
                        }

                        // check if there is an entry already to make pooling work
                        if (results_valuation.TryGetValue(destination.destindx + "|" + valuefunc.Endpoint, out result_valuation))
                        {
                            result_valuation.Value = result_valuation.Value + result;
                        }
                        else
                        {
                            results_valuation.Add(destination.destindx + "|" + valuefunc.Endpoint, new Result { Destinationindex = destination.destindx.GetValueOrDefault(0), Endpoint = valuefunc.Endpoint, Value = result });
                        }
                        // add to national totals as well
                        if (results_valuation.TryGetValue("nation|" + valuefunc.Endpoint, out result_valuation))
                        {
                            result_valuation.Value = result_valuation.Value + result;
                        }
                        else
                        {
                            results_valuation.Add("nation|" + valuefunc.Endpoint, new Result { Destinationindex = destination.destindx.GetValueOrDefault(0), Endpoint = valuefunc.Endpoint, Value = result });
                        }
                    }
                }
            }

            List<Cobra_ResultDetail> results = new List<Cobra_ResultDetail>();
            foreach (var destination in Destinations)
            {
                Cobra_ResultDetail result_record = new Cobra_ResultDetail();
                result_record.destindx = destination.destindx;
                result_record.BASE_FINAL_PM = destination.BASE_FINAL_PM;
                result_record.CTRL_FINAL_PM = destination.CTRL_FINAL_PM;
                result_record.DELTA_FINAL_PM = destination.DELTA_FINAL_PM;
                var loc = dict_state.Where(d => d.SOURCEINDX == result_record.destindx).First();
                result_record.FIPS = loc.FIPS;
                result_record.STATE = loc.STNAME;
                result_record.COUNTY = loc.CYNAME;

                result_record.Acute_Bronchitis = results_cr[destination.destindx + "|" + "Acute Bronchitis"].Value;
                result_record.Acute_Myocardial_Infarction_Nonfatal__high_ = results_cr[destination.destindx + "|" + "Acute Myocardial Infarction, Nonfatal (high)"].Value;
                result_record.Acute_Myocardial_Infarction_Nonfatal__low_ = results_cr[destination.destindx + "|" + "Acute Myocardial Infarction, Nonfatal (low)"].Value;
                result_record.Asthma_Exacerbation_Cough = results_cr[destination.destindx + "|" + "Asthma Exacerbation, Cough"].Value;
                result_record.Asthma_Exacerbation_Shortness_of_Breath = results_cr[destination.destindx + "|" + "Asthma Exacerbation, Shortness of Breath"].Value;
                result_record.Asthma_Exacerbation_Wheeze = results_cr[destination.destindx + "|" + "Asthma Exacerbation, Wheeze"].Value;
                result_record.Emergency_Room_Visits_Asthma = results_cr[destination.destindx + "|" + "Emergency Room Visits, Asthma"].Value;
                result_record.HA_All_Cardiovascular__less_Myocardial_Infarctions_ = results_cr[destination.destindx + "|" + "HA, All Cardiovascular (less Myocardial Infarctions)"].Value;
                result_record.HA_All_Respiratory = results_cr[destination.destindx + "|" + "HA, All Respiratory"].Value;
                result_record.HA_Asthma = results_cr[destination.destindx + "|" + "HA, Asthma"].Value;
                result_record.HA_Chronic_Lung_Disease = results_cr[destination.destindx + "|" + "HA, Chronic Lung Disease"].Value;
                result_record.Lower_Respiratory_Symptoms = results_cr[destination.destindx + "|" + "Lower Respiratory Symptoms"].Value;
                result_record.Minor_Restricted_Activity_Days = results_cr[destination.destindx + "|" + "Minor Restricted Activity Days"].Value;
                result_record.Mortality_All_Cause__low_ = results_cr[destination.destindx + "|" + "Mortality, All Cause (low)"].Value;
                result_record.Mortality_All_Cause__high_ = results_cr[destination.destindx + "|" + "Mortality, All Cause (high)"].Value;
                result_record.Infant_Mortality = results_cr[destination.destindx + "|" + "Infant Mortality"].Value;
                result_record.Upper_Respiratory_Symptoms = results_cr[destination.destindx + "|" + "Upper Respiratory Symptoms"].Value;
                result_record.Work_Loss_Days = results_cr[destination.destindx + "|" + "Work Loss Days"].Value;

                result_record.C__Acute_Bronchitis = results_valuation[destination.destindx + "|" + "Acute Bronchitis"].Value;
                result_record.C__Acute_Myocardial_Infarction_Nonfatal__high_ = results_valuation[destination.destindx + "|" + "Acute Myocardial Infarction, Nonfatal (high)"].Value;
                result_record.C__Acute_Myocardial_Infarction_Nonfatal__low_ = results_valuation[destination.destindx + "|" + "Acute Myocardial Infarction, Nonfatal (low)"].Value;
                result_record.C__Asthma_Exacerbation = results_valuation[destination.destindx + "|" + "Asthma Exacerbation"].Value;
                result_record.C__Emergency_Room_Visits_Asthma = results_valuation[destination.destindx + "|" + "Emergency Room Visits, Asthma"].Value;
                result_record.C__CVD_Hosp_Adm = results_valuation[destination.destindx + "|" + "CVD Hosp. Adm."].Value;
                result_record.C__Resp_Hosp_Adm = results_valuation[destination.destindx + "|" + "Resp. Hosp. Adm."].Value;
                result_record.C__Lower_Respiratory_Symptoms = results_valuation[destination.destindx + "|" + "Lower Respiratory Symptoms"].Value;
                result_record.C__Minor_Restricted_Activity_Days = results_valuation[destination.destindx + "|" + "Minor Restricted Activity Days"].Value;
                result_record.C__Mortality_All_Cause__low_ = results_valuation[destination.destindx + "|" + "Mortality, All Cause (low)"].Value;
                result_record.C__Mortality_All_Cause__high_ = results_valuation[destination.destindx + "|" + "Mortality, All Cause (high)"].Value;
                result_record.C__Infant_Mortality = results_valuation[destination.destindx + "|" + "Infant Mortality"].Value;
                result_record.C__Upper_Respiratory_Symptoms = results_valuation[destination.destindx + "|" + "Upper Respiratory Symptoms"].Value;
                result_record.C__Work_Loss_Days = results_valuation[destination.destindx + "|" + "Work Loss Days"].Value;

                //now do total health effect dollars
                double lowvals = 0;

                lowvals += result_record.C__Acute_Bronchitis.GetValueOrDefault(0);

                lowvals += result_record.C__Asthma_Exacerbation.GetValueOrDefault(0);
                lowvals += result_record.C__Emergency_Room_Visits_Asthma.GetValueOrDefault(0);
                lowvals += result_record.C__CVD_Hosp_Adm.GetValueOrDefault(0);
                lowvals += result_record.C__Resp_Hosp_Adm.GetValueOrDefault(0);
                lowvals += result_record.C__Lower_Respiratory_Symptoms.GetValueOrDefault(0);
                lowvals += result_record.C__Minor_Restricted_Activity_Days.GetValueOrDefault(0);

                lowvals += result_record.C__Infant_Mortality.GetValueOrDefault(0);
                lowvals += result_record.C__Upper_Respiratory_Symptoms.GetValueOrDefault(0);
                lowvals += result_record.C__Work_Loss_Days.GetValueOrDefault(0);

                result_record.C__Total_Health_Benefits_Low_Value = lowvals;

                //add low to high this works
                result_record.C__Total_Health_Benefits_High_Value = lowvals;

                //and here they diverge
                result_record.C__Total_Health_Benefits_High_Value += result_record.C__Acute_Myocardial_Infarction_Nonfatal__high_.GetValueOrDefault(0) + result_record.C__Mortality_All_Cause__high_.GetValueOrDefault(0);

                result_record.C__Total_Health_Benefits_Low_Value += result_record.C__Acute_Myocardial_Infarction_Nonfatal__low_.GetValueOrDefault(0) + result_record.C__Mortality_All_Cause__low_.GetValueOrDefault(0);

                results.Add(result_record);
            }
            return results;
        }

        public List<Cobra_ResultDetail> ComputeGenericImpacts(double delta_pm, double base_pm, double control_pm, Cobra_POP population, Cobra_Incidence[] incidence, bool valat3)
        {
            Dictionary<string, Result> results_cr = new Dictionary<string, Result>();
            Dictionary<string, Result> results_valuation = new Dictionary<string, Result>();


            List<string> Endpoints_cr = CRfunctions.Select(c => c.Endpoint).Distinct().ToList();
            List<string> Endpoints_val = Valuationfunctions.Select(c => c.Endpoint).Distinct().ToList();

            Dictionary<string, Cobra_Incidence> dict_incidence = new Dictionary<string, Cobra_Incidence>();
            foreach (var item in incidence)
            {
                dict_incidence.Add(item.Endpoint, item);
            }


            Cobra_Incidence value;
            Result result_cr;
            Result result_valuation;
            Cobra_Incidence incidencerow;

            foreach (var crfunc in CRfunctions)
            {
                string function = crfunc.Function;

                function = function.Replace("EXP", "Exp");
                function = function.Replace("exp", "Exp");

                string cleanfunction = function.ToUpper().Replace(" ", "");

                double metric_adjustment = 1;
                if (crfunc.Seasonal_Metric.ToUpper() == "DAILY")
                {
                    metric_adjustment = 365;
                }

                {
                    //fixed params
                    Expression e = new Expression(function);
                    double A = crfunc.A.GetValueOrDefault(0);
                    double B = crfunc.B.GetValueOrDefault(0);
                    double C = crfunc.C.GetValueOrDefault(0);
                    double Beta = crfunc.Beta.GetValueOrDefault(0);
                    double DELTAQ = delta_pm;
                    double Incidence = 0;

                    //year dependent but with the twist that pop and incidence are containing all year data
                    Cobra_POP poprow = population;
                    if (dict_incidence.TryGetValue(crfunc.IncidenceEndpoint, out value))
                    {
                        incidencerow = value;
                    }
                    else
                    {
                        incidencerow = null;
                        Incidence = 0;
                    }

                    double poolingweight = crfunc.PoolingWeight.GetValueOrDefault(0);
                    for (long age = crfunc.Start_Age.GetValueOrDefault(0); age <= crfunc.End_Age.GetValueOrDefault(0); age++)
                    {
                        double POP = poprow.popat(age);
                        if (incidencerow != null)
                        {
                            Incidence = value.incidenceat(age);
                        }

                        double result = this.crfunc(function, cleanfunction, Incidence, Beta, DELTAQ, POP, A, B, C) * poolingweight * metric_adjustment;

                        // check if there is an entry already to make pooling work
                        if (results_cr.TryGetValue(crfunc.Endpoint, out result_cr))
                        {
                            result_cr.Value = result_cr.Value + result;
                        }
                        else
                        {
                            results_cr.Add(crfunc.Endpoint, new Result { Destinationindex = 0, Endpoint = crfunc.Endpoint, Value = result });
                        }
                    }
                }
            }

            foreach (var valuefunc in Valuationfunctions)
            {
                string function = valuefunc.Function;

                function = function.Replace("EXP", "Exp");
                function = function.Replace("exp", "Exp");

                string cleanfunction = function.ToUpper().Replace(" ", "");

                double metric_adjustment = 1;
                if (valuefunc.Seasonal_Metric.ToUpper() == "DAILY")
                {
                    metric_adjustment = 365;
                }

                {
                    //fixed params
                    Expression e = new Expression(function);
                    double A = valuefunc.A.GetValueOrDefault(0);
                    double B = valuefunc.B.GetValueOrDefault(0);
                    double C = valuefunc.C.GetValueOrDefault(0);
                    double Beta = valuefunc.Beta.GetValueOrDefault(0);
                    double DELTAQ = delta_pm;
                    double Incidence = 0;

                    //year dependent but with the twist that pop and incidence are containing all year data
                    Cobra_POP poprow = population;
                    if (dict_incidence.TryGetValue(valuefunc.IncidenceEndpoint, out value))
                    {
                        incidencerow = value;
                    }
                    else
                    {
                        incidencerow = null;
                        Incidence = 0;
                    }

                    double poolingweight = valuefunc.PoolingWeight.GetValueOrDefault(0);

                    for (long age = valuefunc.Start_Age.GetValueOrDefault(0); age <= valuefunc.End_Age.GetValueOrDefault(0); age++)
                    {
                        double POP = poprow.popat(age);
                        if (incidencerow != null)
                        {
                            Incidence = value.incidenceat(age);
                        }

                        double result = this.crfunc(function, cleanfunction, Incidence, Beta, DELTAQ, POP, A, B, C) * poolingweight * metric_adjustment;

                        if (valat3)
                        {
                            result = result * valuefunc.valat3pct.GetValueOrDefault(0) * 1.1225;
                        }
                        else
                        {
                            result = result * valuefunc.valat7pct.GetValueOrDefault(0) * 1.1225;
                        }

                        // check if there is an entry already to make pooling work
                        if (results_valuation.TryGetValue(valuefunc.Endpoint, out result_valuation))
                        {
                            result_valuation.Value = result_valuation.Value + result;
                        }
                        else
                        {
                            results_valuation.Add(valuefunc.Endpoint, new Result { Destinationindex = 0, Endpoint = valuefunc.Endpoint, Value = result });
                        }
                        // add to national totals as well
                        if (results_valuation.TryGetValue("nation|" + valuefunc.Endpoint, out result_valuation))
                        {
                            result_valuation.Value = result_valuation.Value + result;
                        }
                        else
                        {
                            results_valuation.Add("nation|" + valuefunc.Endpoint, new Result { Destinationindex = 0, Endpoint = valuefunc.Endpoint, Value = result });
                        }
                    }
                }
            }

            List<Cobra_ResultDetail> results = new List<Cobra_ResultDetail>();
            {
                Cobra_ResultDetail result_record = new Cobra_ResultDetail();
                result_record.destindx = 0;
                result_record.BASE_FINAL_PM = base_pm;
                result_record.CTRL_FINAL_PM = control_pm;
                result_record.DELTA_FINAL_PM = delta_pm;

                result_record.FIPS = "00000";
                result_record.STATE = "NA";
                result_record.COUNTY = "NA";

                result_record.Acute_Bronchitis = results_cr["Acute Bronchitis"].Value;
                result_record.Acute_Myocardial_Infarction_Nonfatal__high_ = results_cr["Acute Myocardial Infarction, Nonfatal (high)"].Value;
                result_record.Acute_Myocardial_Infarction_Nonfatal__low_ = results_cr["Acute Myocardial Infarction, Nonfatal (low)"].Value;
                result_record.Asthma_Exacerbation_Cough = results_cr["Asthma Exacerbation, Cough"].Value;
                result_record.Asthma_Exacerbation_Shortness_of_Breath = results_cr["Asthma Exacerbation, Shortness of Breath"].Value;
                result_record.Asthma_Exacerbation_Wheeze = results_cr["Asthma Exacerbation, Wheeze"].Value;
                result_record.Emergency_Room_Visits_Asthma = results_cr["Emergency Room Visits, Asthma"].Value;
                result_record.HA_All_Cardiovascular__less_Myocardial_Infarctions_ = results_cr["HA, All Cardiovascular (less Myocardial Infarctions)"].Value;
                result_record.HA_All_Respiratory = results_cr["HA, All Respiratory"].Value;
                result_record.HA_Asthma = results_cr["HA, Asthma"].Value;
                result_record.HA_Chronic_Lung_Disease = results_cr["HA, Chronic Lung Disease"].Value;
                result_record.Lower_Respiratory_Symptoms = results_cr["Lower Respiratory Symptoms"].Value;
                result_record.Minor_Restricted_Activity_Days = results_cr["Minor Restricted Activity Days"].Value;
                result_record.Mortality_All_Cause__low_ = results_cr["Mortality, All Cause (low)"].Value;
                result_record.Mortality_All_Cause__high_ = results_cr["Mortality, All Cause (high)"].Value;
                result_record.Infant_Mortality = results_cr["Infant Mortality"].Value;
                result_record.Upper_Respiratory_Symptoms = results_cr["Upper Respiratory Symptoms"].Value;
                result_record.Work_Loss_Days = results_cr["Work Loss Days"].Value;

                result_record.C__Acute_Bronchitis = results_valuation["Acute Bronchitis"].Value;
                result_record.C__Acute_Myocardial_Infarction_Nonfatal__high_ = results_valuation["Acute Myocardial Infarction, Nonfatal (high)"].Value;
                result_record.C__Acute_Myocardial_Infarction_Nonfatal__low_ = results_valuation["Acute Myocardial Infarction, Nonfatal (low)"].Value;
                result_record.C__Asthma_Exacerbation = results_valuation["Asthma Exacerbation"].Value;
                result_record.C__Emergency_Room_Visits_Asthma = results_valuation["Emergency Room Visits, Asthma"].Value;
                result_record.C__CVD_Hosp_Adm = results_valuation["CVD Hosp. Adm."].Value;
                result_record.C__Resp_Hosp_Adm = results_valuation["Resp. Hosp. Adm."].Value;
                result_record.C__Lower_Respiratory_Symptoms = results_valuation["Lower Respiratory Symptoms"].Value;
                result_record.C__Minor_Restricted_Activity_Days = results_valuation["Minor Restricted Activity Days"].Value;
                result_record.C__Mortality_All_Cause__low_ = results_valuation["Mortality, All Cause (low)"].Value;
                result_record.C__Mortality_All_Cause__high_ = results_valuation["Mortality, All Cause (high)"].Value;
                result_record.C__Infant_Mortality = results_valuation["Infant Mortality"].Value;
                result_record.C__Upper_Respiratory_Symptoms = results_valuation["Upper Respiratory Symptoms"].Value;
                result_record.C__Work_Loss_Days = results_valuation["Work Loss Days"].Value;

                //now do total health effect dollars
                double lowvals = 0;

                lowvals += result_record.C__Acute_Bronchitis.GetValueOrDefault(0);

                lowvals += result_record.C__Asthma_Exacerbation.GetValueOrDefault(0);
                lowvals += result_record.C__Emergency_Room_Visits_Asthma.GetValueOrDefault(0);
                lowvals += result_record.C__CVD_Hosp_Adm.GetValueOrDefault(0);
                lowvals += result_record.C__Resp_Hosp_Adm.GetValueOrDefault(0);
                lowvals += result_record.C__Lower_Respiratory_Symptoms.GetValueOrDefault(0);
                lowvals += result_record.C__Minor_Restricted_Activity_Days.GetValueOrDefault(0);

                lowvals += result_record.C__Infant_Mortality.GetValueOrDefault(0);
                lowvals += result_record.C__Upper_Respiratory_Symptoms.GetValueOrDefault(0);
                lowvals += result_record.C__Work_Loss_Days.GetValueOrDefault(0);

                result_record.C__Total_Health_Benefits_Low_Value = lowvals;

                //add low to high this works
                result_record.C__Total_Health_Benefits_High_Value = lowvals;

                //and here they diverge
                result_record.C__Total_Health_Benefits_High_Value += result_record.C__Acute_Myocardial_Infarction_Nonfatal__high_.GetValueOrDefault(0) + result_record.C__Mortality_All_Cause__high_.GetValueOrDefault(0);

                result_record.C__Total_Health_Benefits_Low_Value += result_record.C__Acute_Myocardial_Infarction_Nonfatal__low_.GetValueOrDefault(0) + result_record.C__Mortality_All_Cause__low_.GetValueOrDefault(0);

                results.Add(result_record);
            }
            return results;
        }

    }

}

