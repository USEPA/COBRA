using CobraComputeAPI;
using CsvHelper;
using DocumentFormat.OpenXml.Office2013.PowerPoint.Roaming;
using FastMember;
using MathNet.Numerics.LinearAlgebra;
using Minio;
using Minio.Exceptions;
using NCalc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Security.AccessControl;
using CobraComputeAPI.Controllers;


namespace CobraCompute
{
    public class CobraComputeInstance
    {
        private CobraComputeCore _core;

        public CobraComputeInstance(CobraComputeCore core)
        {
            _core = core;
        }

        private UserScenario currentscenario;
        /* get current scenario for instance */
        public async Task<Guid> create_userscenario()
        {
            return await _core.Scenarios.createUserScenario();
        }
        //move to instance
        public async Task<UserScenario> retrieve_userscenario(Guid token)
        {
            currentscenario = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Debug.WriteLine("RETRIEVING TOKEN FROM SCENARIOS");
            currentscenario = await _core.Scenarios.retrieve(token);
            return currentscenario;
        }
        //move to instance
        public async Task reset_userscenario(Guid token)
        {
            await _core.Scenarios.resetUserScenario(token);
        }

        public void clearUserScenario()
        {
            currentscenario.Impacts = null;
            currentscenario.EmissionsData = null;
            currentscenario = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        //move to instance
        public async Task store_userscenario()
        {
            await _core.Scenarios.store(currentscenario);
        }
        public async Task delete_userscenario(Guid token)
        {
            await _core.Scenarios.deleteUserScenario(token);
        }

        /**** queue submission functions ***/
        public queueSubmission GetChangeQueueSubmission()
        {
            return currentscenario.queueSubmission;
        }

        public void SetChangeQueueSubmission(queueSubmission submission)
        {
            currentscenario.queueSubmission = submission;
        }
        /**** end queue submission functions ***/


        /***** compute custom / compute Health Effect Controller ****/
        public List<Custom_ResultDetail> CustomComputeGenericImpacts(double delta_pm, double base_pm, double control_pm, Cobra_POP population, Cobra_Incidence[] incidence, bool valat3, Cobra_CR_Core[] CustomCRFunctions, Cobra_Valuation_Core[] CustomValuationFunctions)
        {
            Dictionary<string, Result> concurrentResultsCr = new Dictionary<string, Result>();
            Dictionary<string, Result> results_valuation = new Dictionary<string, Result>();

            Dictionary<string, Cobra_Incidence> dict_incidence = new Dictionary<string, Cobra_Incidence>();
            foreach (var item in incidence)
            {
                dict_incidence.Add(item.Endpoint, item);
            }


            Cobra_Incidence value;
            Result result_cr;
            Result result_valuation;
            Cobra_Incidence incidencerow;

            foreach (var crfunc in CustomCRFunctions)
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
                else if (crfunc.Seasonal_Metric.ToUpper() == "OZONE")
                {
                    metric_adjustment = 152;
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

                        double result = _core.crfunc(function, cleanfunction, Incidence, Beta, DELTAQ, 0, POP, A, B, C) * poolingweight * metric_adjustment;

                        // check if there is an entry already to make pooling work
                        if (concurrentResultsCr.TryGetValue(crfunc.Endpoint, out result_cr))
                        {
                            result_cr.Value = result_cr.Value + result;
                        }
                        else
                        {
                            concurrentResultsCr.Add(crfunc.Endpoint, new Result { TractID = "", DestinationIndex = 0, Endpoint = crfunc.Endpoint, Value = result });
                        }
                    }
                }
            }

            foreach (var valuefunc in CustomValuationFunctions)
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
                else if (valuefunc.Seasonal_Metric.ToUpper() == "OZONE")
                {
                    metric_adjustment = 152;
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

                        double result = _core.crfunc(function, cleanfunction, Incidence, Beta, DELTAQ, 0, POP, A, B, C) * poolingweight * metric_adjustment;

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
                            results_valuation.Add(valuefunc.Endpoint, new Result { TractID = "", DestinationIndex = 0, Endpoint = valuefunc.Endpoint, Value = result });
                        }
                        // add to national totals as well
                        if (results_valuation.TryGetValue("nation|" + valuefunc.Endpoint, out result_valuation))
                        {
                            result_valuation.Value = result_valuation.Value + result;
                        }
                        else
                        {
                            results_valuation.Add("nation|" + valuefunc.Endpoint, new Result { TractID = "", DestinationIndex = 0, Endpoint = valuefunc.Endpoint, Value = result });
                        }
                    }
                }
            }

            List<Custom_ResultDetail> results = new List<Custom_ResultDetail>();
            {
                Custom_ResultDetail result_record = new Custom_ResultDetail();
                result_record.destindx = 0;
                result_record.BASE_FINAL_PM = base_pm;
                result_record.CTRL_FINAL_PM = control_pm;
                result_record.DELTA_FINAL_PM = delta_pm;

                result_record.FIPS = "00000";
                result_record.STATE = "NA";
                result_record.COUNTY = "NA";

                //first get the impact result for each custom endpoint
                foreach (var key in concurrentResultsCr.Keys)
                {

                    result_record.SetDynamicProperty(key, concurrentResultsCr.GetValueOrDefault(key, new Result { Value = 0 }).Value, false);
                }


                //now get the valuation result for each custom endpoint
                foreach (var key in results_valuation.Keys)
                {
                    if (!key.ToLower().Contains("|"))
                    {
                        //last param in setDynamicProperty() set to true to signal that we are adding a valuation
                        result_record.SetDynamicProperty(key, results_valuation.GetValueOrDefault(key, new Result { Value = 0 }).Value, true);
                    }
                }

                //now do total health effect dollars

                double lowvals = 0;
                // Calculate lowvals
                lowvals = result_record.GetDynamicPropertyKeys()
            .Where(key => key.StartsWith("C__") && !key.ToLower().Contains("high"))
            .Sum(key => result_record.GetDynamicProperty(key));


                // Calculate highvals
                double highvals = 0;
                highvals = result_record.GetDynamicPropertyKeys()
            .Where(key => key.StartsWith("C__") && !key.ToLower().Contains("low"))
            .Sum(key => result_record.GetDynamicProperty(key));



                result_record.C__Total_Health_Benefits_Low_Value = lowvals;

                //add low to high this works
                result_record.C__Total_Health_Benefits_High_Value = highvals;

                results.Add(result_record);

            }
            return results;
        }


        /***** COMPUTING EMISSIONS ****/
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
                rec.NOx = (dr.ItemArray[8] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[8];
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
                    foundRow[8] = emission.NOx;
                    foundRow[9] = emission.SO2;
                    foundRow[10] = emission.NH3;
                    foundRow[11] = emission.SOA;
                    foundRow[12] = emission.PM25;
                    foundRow[13] = emission.VOC;
                }
                else
                {
                    emissionsData.Rows.Add(new object[] { emission.ID, emission.typeindx, emission.sourceindx, emission.stid, emission.cyid, emission.TIER1, emission.TIER2, emission.TIER3, emission.NOx.GetValueOrDefault(0), emission.SO2.GetValueOrDefault(0), emission.NH3.GetValueOrDefault(0), emission.SOA, emission.PM25.GetValueOrDefault(0), emission.VOC.GetValueOrDefault(0) });
                }
                //re-get
                DataRow foundRow2 = emissionsData.Rows.Find(emission.ID);
                if (foundRow2 != null)
                {
                    foundRow2[8] = emission.NOx;
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

            DataRow[] rows = _core.SummarizeEmissionsbyType(currentscenario.EmissionsData).Select(criteria);

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
                rec.NOx = (dr.ItemArray[8] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[8];
                rec.SO2 = (dr.ItemArray[9] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[9];
                rec.NH3 = (dr.ItemArray[10] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[10];
                rec.SOA = (dr.ItemArray[11] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[11];
                rec.PM25 = (dr.ItemArray[12] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[12];
                rec.VOC = (dr.ItemArray[13] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[13];
                result.Add(rec);
            }

            return result;
        }

        public EmissionsSums SummarizeBaseControlEmissionsWithCriteria_resulttable(string criteria)
        {
            EmissionsSums result = new EmissionsSums();
            result.baseline = _core.SummarizeEmissionsbyType_resulttable(_core.EmissionsInventory, criteria);
            result.control = _core.SummarizeEmissionsbyType_resulttable(currentscenario.EmissionsData, criteria);
            return result;
        }

        public EmissionsSums SummarizeBaseEmissionsWithCriteria(Func<DataRow, bool> criteria)
        {
            EmissionsSums result = new EmissionsSums();
            result.baseline = _core.SummarizeQueueEmissionsWithCriteria(_core.EmissionsInventory, criteria);
            return result;
        }

        public EmissionsSums SummarizeBaseControlEmissionsWithCriteria(string criteria)
        {
            EmissionsSums result = new EmissionsSums();
            result.baseline = _core.SummarizeEmissionsWithCriteria(_core.EmissionsInventory, criteria);
            result.control = _core.SummarizeEmissionsWithCriteria(currentscenario.EmissionsData, criteria);
            return result;
        }


        //move to instance
        public bool UpdateEmissionsWithCriteria(EmissionsDataUpdateRequest requestparams)
        {
            _core.UpdateEmissionsWithCriteria(currentscenario.EmissionsData, requestparams);
            Debug.WriteLine($"UPDATE EMISSIONS RETURNING TRUE");
            return true;
        }

        //move to instance
        public List<Cobra_ResultDetail> GetResults(double discountrate)
        {
            if (currentscenario == null || currentscenario.Impacts == null || currentscenario.Impacts.Count == 0)
            {
                ComputeDeltaPM(discountrate);
            }
            return currentscenario.Impacts;
        }
        public bool ComputeDeltaPM(double discountrate)
        {
            currentscenario.EmissionsData.AcceptChanges();

            DataTable controlemissions = _core.SummarizeEmissionsbyType(currentscenario.EmissionsData);

            var aqcontrol = _core.Vectorize(controlemissions);

            //                          0   1    2    3    4    
            //aqcontrol looks like:   [PM, NOx, SO2, VOC, O3N ];
            Vector<double> pm_control = _core.computePM(aqcontrol[0], aqcontrol[1], aqcontrol[2]);
            Vector<double> o3_control = _core.computeO3(aqcontrol[3], aqcontrol[4]);
            var pm_delta = _core.pm_base - pm_control;
            var o3_delta = _core.o3_base - o3_control;


            List<Cobra_Destination> Destinations = new List<Cobra_Destination>();

            //populate destinations
            for (int i = 0; i < 84660; i++)
            {



                //make sure the censusindex actually maps to a countyIndex/destindx
                if (_core.census_dict[i + 1].destindx != null && _core.census_dict[i + 1].destindx != 0)
                {

                    //get the tribe info to append to the destination
                    var TribeDict = new Dictionary<string, double>();

                    // Get the dest_tract key
                    var tract_id_key = _core.census_dict[i + 1].dest_tract;

                    // Check if the key exists before accessing it
                    if (_core.tribal_dict.TryGetValue(tract_id_key, out var existingTribeDict))
                    {
                        // Creating a **copy** of the dictionary to prevent modifying the original reference
                        TribeDict = new Dictionary<string, double>(existingTribeDict);
                    }


                    int countyIndex = _core.census_dict[i + 1].destindx.Value - 1;
                    Cobra_Destination dest = new Cobra_Destination();
                    dest.destindx = countyIndex + 1;
                    dest.tract_id = _core.census_dict[i + 1].dest_tract;
                    dest.BASE_NOx = 0;
                    dest.BASE_SO2 = 0;
                    dest.BASE_NH3 = 0;
                    dest.BASE_SOA = 0;
                    dest.BASE_PM25 = 0; //direct
                    dest.BASE_VOC = 0;
                    dest.CTRL_NOx = 0;
                    dest.CTRL_SO2 = 0;
                    dest.CTRL_NH3 = 0;
                    dest.CTRL_SOA = 0;
                    dest.CTRL_PM25 = 0;  //direct
                    dest.CTRL_VOC = 0;
                    dest.F = 0;
                    dest.BASE_FINAL_PM = _core.pm_base[i];
                    dest.CTRL_FINAL_PM = pm_control[i];
                    dest.DELTA_FINAL_PM = pm_delta[i];
                    dest.BASE_FINAL_O3 = _core.o3_base[countyIndex];
                    dest.CTRL_FINAL_O3 = o3_control[countyIndex];
                    dest.DELTA_FINAL_O3 = o3_delta[countyIndex];
                    dest.IRA_fraction = _core.census_dict[i + 1].IRA_fraction;
                    dest.CJEST = _core.census_dict[i + 1].CJEST;
                    dest.Tribes = TribeDict;
                    Destinations.Add(dest);
                }
                else
                {
                    //skip results that don't have a destindx
                }



            }

            //compute part 2
            Stopwatch stopwatch = new Stopwatch();
            // Start timing
            stopwatch.Start();
            Console.WriteLine("******* STARTING COMPUTE IMPACTS");
            currentscenario.Impacts = _core.ComputeImpacts(Destinations, discountrate);
            stopwatch.Stop();
            Console.WriteLine("Time taken to COMPUTE IMPACTS: {0}", stopwatch.Elapsed);

            currentscenario.isDirty = false;

            return true;
        }



    }



    /************************************************************************ END OF INSTANCE CLASS *********************************************************************************************/
    public class CobraComputeCore
    {

        public bool initilized = false;
        private static readonly SemaphoreSlim initializeSemaphore = new SemaphoreSlim(1, 1);


        private string datapath = "";

        private CSRSparseMatrix[] SR_dp = new CSRSparseMatrix[4];

        private CSRSparseMatrix[] SR_NOx = new CSRSparseMatrix[4];
        private CSRSparseMatrix[] SR_SO4 = new CSRSparseMatrix[4];
        private CSRSparseMatrix[] SR_O3N = new CSRSparseMatrix[4];
        private CSRSparseMatrix[] SR_O3V = new CSRSparseMatrix[4];

        //create a dictionary to map census tracts to counties:
        public Dictionary<int, CensusInfo> census_dict = new Dictionary<int, CensusInfo>();
        //mapscensus tract IDs to tribes[{name, tribe_fraction, population}]
        //public Dictionary<string, List<TribalInfo>> tribal_dict = new Dictionary<string, List<TribalInfo>>();

        //mapscensus tract IDs to tribe dicts with census_Tract_id: {tribe_name: tribe_fraction}
        public Dictionary<string, Dictionary<string,double>> tribal_dict = new Dictionary<string, Dictionary<string, double>>();

        //baseline emissions
        public System.Data.DataTable EmissionsInventory;
        public System.Data.DataTable SummarizedEmissionsInventory;

        public ScenarioManager_Redis Scenarios;

        public List<Cobra_POP> Populations = new List<Cobra_POP>();
        private List<Cobra_Incidence> Incidence = new List<Cobra_Incidence>();
        private List<Cobra_CR> CRfunctions = new List<Cobra_CR>();
        private List<Cobra_Valuation> Valuationfunctions = new List<Cobra_Valuation>();

        public List<Cobra_Dict_State> dict_state = new List<Cobra_Dict_State>();
        public List<Cobra_Dict_Tier> dict_tier = new List<Cobra_Dict_Tier>();

        private Vector<double> Adjustment = Vector<double>.Build.Dense(3108);
        private Dictionary<string, double> VOC2SOA = new Dictionary<string, double>();

        private Vector<double>[] aqbase;
        public Vector<double> pm_base;
        public Vector<double> o3_base;

        public StringBuilder statuslog = new StringBuilder();

        private RedisConfig redisOptions;

        private MinioClient minioClient;
        private S3Config s3Config;

        private ModelConfig modelConfig;
        private readonly object lockObj = new object();
        public string simpleGeojson;
        public string fullGeojson;

        public CobraComputeCore(RedisConfig redisOptions, S3Config _s3Config, ModelConfig _modelConfig)
        {
            statuslog.Append("initializing configurations");

            this.redisOptions = redisOptions;
            this.s3Config = _s3Config;
            this.modelConfig = _modelConfig;


            statuslog.Append("initializing minio");

            try
            {
                if (s3Config.ssl)
                {
                    Console.WriteLine("Creating Minio Client WITH SSL");
                    minioClient = new MinioClient(s3Config.endpoint, s3Config.accessKey, s3Config.secretKey, s3Config.region).WithSSL();
                }
                else
                {
                    Console.WriteLine("Creating Minio Client WITHOUT SSL");
                    minioClient = new MinioClient(s3Config.endpoint, s3Config.accessKey, s3Config.secretKey, s3Config.region);
                }
            }
            catch (Exception ex)
            {
                statuslog.Append("initializing minio failed");
            }


            EmissionsInventory = new DataTable("EmissionsInventory");

            EmissionsInventory.Columns.Add("ID", typeof(int));
            EmissionsInventory.Columns.Add("typeindx", typeof(int));
            EmissionsInventory.Columns.Add("sourceindx", typeof(int));
            EmissionsInventory.Columns.Add("stid", typeof(int));
            EmissionsInventory.Columns.Add("cyid", typeof(int));
            EmissionsInventory.Columns.Add("TIER1", typeof(int));
            EmissionsInventory.Columns.Add("TIER2", typeof(int));
            EmissionsInventory.Columns.Add("TIER3", typeof(int));
            EmissionsInventory.Columns.Add("NOx", typeof(double));
            EmissionsInventory.Columns.Add("SO2", typeof(double));
            EmissionsInventory.Columns.Add("NH3", typeof(double));
            EmissionsInventory.Columns.Add("SOA", typeof(double));
            EmissionsInventory.Columns.Add("PM25", typeof(double));
            EmissionsInventory.Columns.Add("VOC", typeof(double));
            EmissionsInventory.PrimaryKey = new DataColumn[] { EmissionsInventory.Columns["ID"] }; //key on recno 

        }

        public CobraComputeInstance CreateInstance()
        {
            /*if (!initilized)
            {
                await initialize();
            }*/
            return new CobraComputeInstance(this);
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
            return "V1.3";
        }

        public static void ToCSV(DataTable dtDataTable, string strFilePath)
        {
            StreamWriter sw = new StreamWriter(strFilePath, false);
            //headers    
            for (int i = 0; i < dtDataTable.Columns.Count; i++)
            {
                sw.Write(dtDataTable.Columns[i]);
                if (i < dtDataTable.Columns.Count - 1)
                {
                    sw.Write(",");
                }
            }
            sw.Write(sw.NewLine);
            foreach (DataRow dr in dtDataTable.Rows)
            {
                for (int i = 0; i < dtDataTable.Columns.Count; i++)
                {
                    if (!Convert.IsDBNull(dr[i]))
                    {
                        string value = dr[i].ToString();
                        if (value.Contains(','))
                        {
                            value = String.Format("\"{0}\"", value);
                            sw.Write(value);
                        }
                        else
                        {
                            sw.Write(dr[i].ToString());
                        }
                    }
                    if (i < dtDataTable.Columns.Count - 1)
                    {
                        sw.Write(",");
                    }
                }
                sw.Write(sw.NewLine);
            }
            sw.Close();
        }

        public async Task<bool> initialize(string path = "data/")
        {
            await initializeSemaphore.WaitAsync();
            Debug.WriteLine("------------------------------------------------------------------ beginning initialize");
            statuslog.Append("beginning initialization " + path);

            if (this.initilized) { return true; };

            bool result = true;
            datapath = path;


            //proceed setting up
            try
            {
                Console.WriteLine("entering load data");
                statuslog.Append("entering load data");

                int recno = 1;

                //load pop
                await LoadS3Pop();

                //load census tract geographies
                Console.WriteLine("loading geo files");
                await GetGeoJsonDataAsync("cb2023_census_simplified_merged2020CT.geojson");
                Console.WriteLine("loaded simple geo");
                await GetGeoJsonDataAsync("cb2023_census_unsimplified_merged2020CT.geojson");
                Console.WriteLine("done loading census tract geos");



                //load incidence
                await LoadS3Incidence();

                //load cr
                await LoadS3CR();
                //load valuation
                await LoadS3Value();

                //load dictionar(ies)
                Console.WriteLine("loading state and tier dictionaries");
                statuslog.Append("loading state and tier dictionaries");
                await LoadS3Dictionary_State();
                await LoadS3Dictionary_Tier();
                Console.WriteLine("loading census dict");
                statuslog.Append("loading census dict");
                await LoadCensusDict();
                Console.WriteLine("loading tribal info");
                statuslog.Append("loading tribal info");
                await LoadTribalDict();
                Console.WriteLine("done waits for state and tier");
                statuslog.Append("done waits for state and tier");

                //load adjustment factors
                await LoadS3Adjustments();


                //load voc2soa
                await LoadS3VOC2SOA();

                //load emissions
                await LoadS3Emissions();

                //debug
                recno = SummarizeEmissions();

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                //load matrix data
                //LoadS3SRfrommtx().Wait();
                Console.WriteLine("loading county SR MATRIX");
                statuslog.Append("loading county SR MATRIX");

                //InitBlankSR();
                //await LoadS3SR();
                //LoadSR(path);

                await LoadCountySR_Census();
                Debug.WriteLine("loaded county level SR");
                statuslog.Append("garbage collection");

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                Console.WriteLine("loading CENSUS MATRIX");
                statuslog.Append("loading CENSUS MATRIX");

                await LoadS3SR_Census();
                //LoadSR(path);

                Debug.WriteLine("Loaded census level SR");
                statuslog.Append("garbage collection");

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // compute baseline AQ components
                Debug.WriteLine("getting aqbase");
                aqbase = Vectorize(SummarizedEmissionsInventory);

                Debug.WriteLine("computing pm...");
                //aqbase looks like:   [PM, NOx, SO2, VOC, O3N ];
                pm_base = computePM(aqbase[0], aqbase[1], aqbase[2]);
                Debug.WriteLine("computing O3...");
                o3_base = computeO3(aqbase[3], aqbase[4]);

                statuslog.Append("instantiating manager");
                Scenarios = new ScenarioManager_Redis(this.EmissionsInventory, this.redisOptions, this.minioClient, this.s3Config);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error initializing!");
                Console.WriteLine(e);
                statuslog.Append("Error initializing!");
                statuslog.Append(e);
                result = false;
            }
            finally
            {
                initializeSemaphore.Release();
            }
            if (!result)
            {
                datapath = "";
                EmissionsInventory.Clear();
            }
            this.initilized = result;
            Console.WriteLine("init done " + result.ToString());
            statuslog.Append("init done " + result.ToString());
            return result;
        }

        /*private void InitBlankSR()
        {
            //for each type index: (1-4):
            for (int i = 1; i < 5; i++)
            {

                //SR_dp[i - 1] = SparseMatrix.Build.Sparse(84660, 3108);
                SR_NOx[i - 1] = Matrix<double>.Build.Dense(3108, 3108);
                SR_SO4[i - 1] = Matrix<double>.Build.Dense(3108, 3108);
                SR_O3N[i - 1] = Matrix<double>.Build.Dense(3108, 3108);
                SR_O3V[i - 1] = Matrix<double>.Build.Dense(3108, 3108);
                //SR_nh3[i - 1] = Matrix<double>.Build.Dense(3108, 3108);
            }
            Console.WriteLine("Done init blank SR");
        }*/


        /*private async Task LoadS3SR()
        {
            try
            {
                // Check whether the object exists using statObject().
                await minioClient.StatObjectAsync(this.s3Config.bucket, "sr_matrix.csv");

                await minioClient.GetObjectAsync(this.s3Config.bucket, "sr_matrix.csv",
                                                 async (stream) =>
                                                 {
                                                     using (TextReader textReader = new StreamReader(stream))
                                                     {
                                                         var csvConfig = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.CurrentCulture)
                                                         {
                                                             HasHeaderRecord = true,
                                                             MissingFieldFound = null,
                                                         };

                                                         using (var csv = new CsvReader(textReader, csvConfig))
                                                         {
                                                             int recordCount = 0;
                                                             Debug.WriteLine("Starting to read records from sr_matrix.csv");

                                                             List<srrecord> batch = new List<srrecord>();
                                                             while (await csv.ReadAsync())
                                                             {
                                                                 var sr_record = csv.GetRecord<srrecord>();
                                                                 batch.Add(sr_record);

                                                                 recordCount++;
                                                                 if (recordCount % 10000 == 0)
                                                                 {
                                                                     ProcessBatch(batch);
                                                                     batch.Clear();

                                                                     GC.Collect();
                                                                     GC.WaitForPendingFinalizers();
                                                                     GC.Collect();
                                                                 }
                                                             }

                                                             // Process remaining records in the last batch
                                                             if (batch.Count > 0)
                                                             {
                                                                 ProcessBatch(batch);
                                                             }

                                                             Debug.WriteLine("Finished reading all records from sr_matrix.csv");
                                                         }
                                                     }
                                                 });
            }
            catch (MinioException e)
            {
                statuslog.Append("Error occurred: " + e);
                Debug.WriteLine("Error occurred: " + e);
            }
            catch (Exception e)
            {
                statuslog.Append("Unexpected error: " + e);
                Debug.WriteLine("Unexpected error: " + e);
            }
        }*/


        private async Task LoadS3SR_Census()
        {
            for (int i = 0; i < 4; i++)
            {
                string binaryFileName = $"SR_dp_matrix_{i + 1}.bin";
                Console.WriteLine($"Loading {binaryFileName} for SR_dp[{i}]...");
                Debug.WriteLine($"Loading {binaryFileName} for SR_dp[{i}]...");

                // Fetch the file from S3 and load it directly into SR_dp[i] without buffering
                await minioClient.GetObjectAsync(this.s3Config.bucket, binaryFileName,
                    async (stream) =>
                    {
                        SR_dp[i] = CSRSparseMatrix.LoadFromBinary(stream);
                    });

                Debug.WriteLine($"Loaded {binaryFileName} into SR_dp[{i}]");
            }

            Console.WriteLine("All SR_dp matrices initialized from binary files.");
            Debug.WriteLine("All SR_dp matrices initialized from binary files.");
        }

        private async Task LoadCountySR_Census()
        {
            // Define the pollutants
            string[] pollutants = { "NOx", "O3N", "O3V", "SO4" };

            // Create a list of tasks, one for each pollutant
            var loadTasks = new List<Task>();

            foreach (var pollutant in pollutants)
            {
                // Create a task for each pollutant
                var task = Task.Run(async () =>
                {
                    // Process files for this pollutant sequentially
                    for (int i = 0; i < 4; i++)
                    {
                        int index = i;  // Local copy to avoid closure issues
                        string binaryFileName = $"SR_{pollutant}_{index + 1}.bin";

                        Console.WriteLine($"Loading {binaryFileName} for SR_dp[{index}]...");
                        Debug.WriteLine($"Loading {binaryFileName} for SR_dp[{index}]...");

                        // Fetch the file from S3 and load it directly into SR_dp[index] without buffering
                        // Fetch the file from S3 and load it directly into the appropriate matrix without buffering
                        await minioClient.GetObjectAsync(this.s3Config.bucket, binaryFileName, async (stream) =>
                        {
                            var matrix = CSRSparseMatrix.LoadFromBinary(stream);

                            // Assign to the correct matrix based on the pollutant
                            switch (pollutant)
                            {
                                case "NOx":
                                    SR_NOx[index] = matrix;
                                    break;
                                case "O3N":
                                    SR_O3N[index] = matrix;
                                    break;
                                case "O3V":
                                    SR_O3V[index] = matrix;
                                    break;
                                case "SO4":
                                    SR_SO4[index] = matrix;
                                    break;
                                default:
                                    throw new InvalidOperationException($"Unknown pollutant type: {pollutant}");
                            }
                        });

                        Debug.WriteLine($"Loaded {binaryFileName} into SR_{pollutant}[{index}]");
                    }

                });

                loadTasks.Add(task);
            }

            // Run all pollutant loading tasks in parallel
            await Task.WhenAll(loadTasks);

            Console.WriteLine("All county matrices initialized from binary files.");
            Debug.WriteLine("All county matrices initialized from binary files.");
        }




        /* private void ProcessPMBatch(List<srCensusrecord> batch)
         {
             var groupedBatch = batch.GroupBy(record => record.typeindx - 1);

             // Sequential outer loop for each group
             foreach (var group in groupedBatch)
             {
                 int index2use = group.Key;

                 // Parallel inner loop without locks since there's no risk of concurrent writes
                 Parallel.ForEach(group, census_record =>
                 {
                     SR_dp[index2use][census_record.census_index - 1, census_record.sourceindx - 1] = census_record.c_PM25.GetValueOrDefault(0);
                 });
             }
         }*/


        /*private void ProcessBatch(List<srrecord> batch)
        {
            var groupedBatch = batch.GroupBy(record => record.typeindx - 1);

            // Sequential outer loop for each group
            foreach (var group in groupedBatch)
            {
                int index2use = group.Key;

                Parallel.ForEach(group, sr_record =>
                {
                    SR_NOx[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.c_NO3.GetValueOrDefault(0);
                    SR_SO4[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.c_SO4.GetValueOrDefault(0);
                    SR_O3N[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.c_O3N.GetValueOrDefault(0);
                    SR_O3V[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.c_O3V.GetValueOrDefault(0);
                });
            }
        }*/



        //part of initialization
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
                                 NOx = grp.Sum(r => r.Field<double?>("NOx")),
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
                if (rowentry.NOx.GetValueOrDefault(0) > 0 || rowentry.SO2.GetValueOrDefault(0) > 0 || rowentry.NH3.GetValueOrDefault(0) > 0 || rowentry.SOA.GetValueOrDefault(0) > 0 || rowentry.PM25.GetValueOrDefault(0) > 0 || rowentry.VOC.GetValueOrDefault(0) > 0)
                {
                    SummarizedEmissionsInventory.Rows.Add(new object[] { recno, rowentry.typeindx, rowentry.sourceindx, rowentry.stid, rowentry.cyid, 0, 0, 0, rowentry.NOx.GetValueOrDefault(0), rowentry.SO2.GetValueOrDefault(0), rowentry.NH3.GetValueOrDefault(0), rowentry.SOA.GetValueOrDefault(0), rowentry.PM25.GetValueOrDefault(0), rowentry.VOC.GetValueOrDefault(0) });
                    recno++;
                }
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
                                 NOx = grp.Sum(r => r.Field<double?>("NOx")),
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
                result.Rows.Add(new object[] { recno, rowentry.typeindx, rowentry.sourceindx, rowentry.stid, rowentry.cyid, 0, 0, 0, rowentry.NOx.GetValueOrDefault(0), rowentry.SO2.GetValueOrDefault(0), rowentry.NH3.GetValueOrDefault(0), rowentry.SOA.GetValueOrDefault(0), rowentry.PM25.GetValueOrDefault(0), rowentry.VOC.GetValueOrDefault(0) });
                recno++;
            }

            return result;
        }

        private async Task LoadS3Emissions()
        {
            int recno;
            EmissionsInventory.Clear();
            recno = 1;

            try
            {
                // Check whether the object exists using statObject().
                // If the object is not found, statObject() throws an exception,
                // else it means that the object exists.
                // Execution is successful.
                await minioClient.StatObjectAsync(this.s3Config.bucket, "emissions_inventory_" + modelConfig.emissionsdatayear + ".csv");

                await minioClient.GetObjectAsync(this.s3Config.bucket, "emissions_inventory_" + modelConfig.emissionsdatayear + ".csv",
                                                 (stream) =>
                                                 {
                                                     using (TextReader textReader = new StreamReader(stream))
                                                     {
                                                         CsvReader csv = new CsvReader(textReader, System.Globalization.CultureInfo.CurrentCulture);
                                                         foreach (EmissionsRecord record in csv.GetRecords<EmissionsRecord>())
                                                         {
                                                             if (record.NOx > 0.0 || record.NH3 > 0.0 || record.SOA > 0.0 || record.SO2 > 0.0 || record.PM25 > 0.0 || record.VOC > 0.0)
                                                             {
                                                                 if (record.sourceindx <= 3108)
                                                                 {
                                                                     EmissionsInventory.Rows.Add(new object[] { recno, record.typeindx, record.sourceindx, record.stid, record.cyid, record.TIER1, record.TIER2, record.TIER3, record.NOx.GetValueOrDefault(0), record.SO2.GetValueOrDefault(0), record.NH3.GetValueOrDefault(0), ComputeSOAfromVOC(record.TIER1 + "|" + record.TIER2 + "|" + record.TIER3, record.VOC.GetValueOrDefault(0)), record.PM25.GetValueOrDefault(0), record.VOC.GetValueOrDefault(0) });
                                                                     recno++;
                                                                 }
                                                             }
                                                         }
                                                     }

                                                 });
            }
            catch (MinioException e)
            {
                statuslog.Append("Error occurred: " + e);
            }
        }

        private async Task LoadS3VOC2SOA()
        {
            EmissionsInventory.Clear();
            try
            {
                await minioClient.StatObjectAsync(this.s3Config.bucket, "sys_voc2soa.csv");

                await minioClient.GetObjectAsync(this.s3Config.bucket, "sys_voc2soa.csv",
                                                 (stream) =>
                                                 {
                                                     using (TextReader textReader = new StreamReader(stream))
                                                     {
                                                         CsvReader csv = new CsvReader(textReader, System.Globalization.CultureInfo.CurrentCulture);

                                                         foreach (Cobra_Voc2Soa record in csv.GetRecords<Cobra_Voc2Soa>())
                                                         {
                                                             // Check if the key already exists in the dictionary
                                                             if (!VOC2SOA.ContainsKey(record.TIER1 + "|" + record.TIER2 + "|" + record.TIER3))
                                                             {

                                                                 VOC2SOA.Add(record.TIER1 + "|" + record.TIER2 + "|" + record.TIER3, record.FACTOR);
                                                             }
                                                         }
                                                     }

                                                 });
            }
            catch (MinioException e)
            {
                statuslog.Append("Error occurred: " + e);
            }
        }

        private async Task LoadS3Adjustments()
        {
            try
            {
                await minioClient.StatObjectAsync(this.s3Config.bucket, "sys_adj.csv");

                await minioClient.GetObjectAsync(this.s3Config.bucket, "sys_adj.csv",
                                                 (stream) =>
                                                 {
                                                     using (TextReader textReader = new StreamReader(stream))
                                                     {
                                                         CsvReader csv = new CsvReader(textReader, System.Globalization.CultureInfo.CurrentCulture);
                                                         foreach (Cobra_Adjustment record in csv.GetRecords<Cobra_Adjustment>())
                                                         {
                                                             Adjustment[record.indx.GetValueOrDefault(0) - 1] = record.F1.GetValueOrDefault(0);
                                                         }
                                                     }

                                                 });
            }
            catch (MinioException e)
            {
                statuslog.Append("Error occurred: " + e);
            }
        }



        /*
         * 
         *  try
                                                                 {
                                                                     // Get the value of sourceindx as a string (assuming column name is "sourceindx")
                                                                     var sourceindx = csv.GetField("sourceindx");

                                                                     // Check if sourceindx is empty or null, and skip if it is
                                                                     if (string.IsNullOrWhiteSpace(sourceindx))
                                                                     {
                                                                         Debug.WriteLine($"Skipping row {csv.Context.Row} due to empty sourceindx.");
                                                                         continue; // Skip to the next row
                                                                     }

                                                                     // If sourceindx is valid, proceed to create the record
                                                                     var sr_census_record = csv.GetRecord<srCensusrecord>();
                                                                     batch.Add(sr_census_record);

                                                                     recordCount++;
                                                                     if (recordCount % batchSize == 0)
                                                                     {
                                                                         // Process the batch asynchronously
                                                                         await ProcessPMBatchAsync(batch);
                                                                         batch.Clear();  // Clear batch after processing
                                                                     }
                                                                 }

        */
        private async Task LoadTribalDict()
        {
            try
            {
                await minioClient.StatObjectAsync(this.s3Config.bucket, "Tribal_Land_Fractions.csv");

                await minioClient.GetObjectAsync(this.s3Config.bucket, "Tribal_Land_Fractions.csv",
                                                 (stream) =>
                                                 {
                                                     using (TextReader textReader = new StreamReader(stream))
                                                     {
                                                         CsvReader csv = new CsvReader(textReader, System.Globalization.CultureInfo.CurrentCulture);
                                                         foreach (var record in csv.GetRecords<TribalRecord>())
                                                         {
                                                             // Check if tract_id exists in tribal_dict
                                                             if (!tribal_dict.TryGetValue(record.tract_id, out var TribeDict))
                                                             {
                                                                 // If not, initialize a new dictionary and add the first tribe
                                                                 TribeDict = new Dictionary<string, double>();
                                                                 tribal_dict[record.tract_id] = TribeDict;
                                                             }

                                                             // Check if the tribe_name already exists in the dictionary
                                                             if (TribeDict.ContainsKey(record.tribe_name))
                                                             {
                                                                 // Log the duplicate entry
                                                                // Console.WriteLine($"Duplicate found: Tract {record.tract_id} already contains tribe {record.tribe_name}. Adding fractions accross block groups.");

                                                                 // Sum the fractions to add tribe fractions accross census block groups
                                                                 TribeDict[record.tribe_name] += record.tribe_fraction;
                                                             }
                                                             else
                                                             {
                                                                 // If it's a new tribe, add directly
                                                                 TribeDict[record.tribe_name] = record.tribe_fraction;
                                                             }
                                                         }
                                                     }
                                                 });
            }
            catch (MinioException e)
            {
                statuslog.Append("Error occurred: " + e);
            }
        }

        private async Task LoadCensusDict()
        {
            try
            {
                await minioClient.StatObjectAsync(this.s3Config.bucket, "CENSUS_DICT.csv");

                await minioClient.GetObjectAsync(this.s3Config.bucket, "CENSUS_DICT.csv",
                                                 (stream) =>
                                                 {
                                                     using (TextReader textReader = new StreamReader(stream))
                                                     {
                                                         CsvReader csv = new CsvReader(textReader, System.Globalization.CultureInfo.CurrentCulture);
                                                         foreach (CensusRecord record in csv.GetRecords<CensusRecord>())
                                                         {
                                                             // Check if the key already exists in the dictionary
                                                             if (!census_dict.ContainsKey(record.census_index))
                                                             {
                                                                 // Add the record if the key doesn't already exist
                                                                 census_dict.Add(record.census_index, new CensusInfo
                                                                 {
                                                                     dest_tract = record.dest_tract.ToString(),
                                                                     FIPS = record.FIPS,
                                                                     destindx = record.destindx ?? 0,
                                                                     IRA = record.IRA,
                                                                     CJEST = record.CJEST,
                                                                     IRA_fraction = record.IRA_fraction
                                                                 });
                                                             }
                                                             else
                                                             {
                                                                 // Handle duplicate key, e.g., log a message or update the existing value
                                                                 statuslog.Append($"Duplicate key {record.census_index} found. Skipping entry.");
                                                             }
                                                         }
                                                     }
                                                 });
            }
            catch (MinioException e)
            {
                statuslog.Append("Error occurred: " + e);
            }
        }


        // Helper method to load slider_data.csv on demand from S3 when pop endpoint is called
        public async Task<Dictionary<string, SliderData>> LoadSliderDataAsync()
        {
            var sliderDataDict = new Dictionary<string, SliderData>();

            try
            {
                // Ensure the file exists (using your S3 client from computeCore)
                await minioClient.StatObjectAsync(s3Config.bucket, "slider_data.csv");

                // Load the CSV file from S3
                await minioClient.GetObjectAsync(s3Config.bucket, "slider_data.csv",
                    (stream) =>
                    {
                        using (TextReader textReader = new StreamReader(stream))
                        {
                            var csv = new CsvReader(textReader, CultureInfo.CurrentCulture);
                            foreach (var record in csv.GetRecords<SliderRecord>())
                            {
                                // Convert the ID to a string padded to 11 characters
                                string id = record.ID.ToString().PadLeft(11, '0');

                                if (!sliderDataDict.ContainsKey(id))
                                {
                                    sliderDataDict.Add(id, new SliderData
                                    {
                                        P_LOWINC = record.P_LOWINC ?? -1,
                                        P_LIFEXPCT = record.P_LIFEXPCT ?? -1,
                                        P_PM25 = record.P_PM25 ?? -1,
                                        P_OZONE = record.P_OZONE ?? -1,
                                        ENERGYBURDEN_LESS_6PCT = record.ENERGYBURDEN_LESS_6PCT ?? -1,
                                        ENERGYBURDEN_GRTR_EQL_6PCT = record.ENERGYBURDEN_GRTR_EQL_6PCT ?? -1,
                                        ENERGYBURDEN_GRTR_EQL_10PCT = record.ENERGYBURDEN_GRTR_EQL_10PCT ?? -1
                                    });
                                }
                                else
                                {
                                   
                                }
                            }
                        }
                    });
            }
            catch (MinioException e)
            {
                // Log the error if needed
            }

            return sliderDataDict;
        }

        private async Task LoadS3Dictionary_State()
        {
            try
            {
                await minioClient.StatObjectAsync(this.s3Config.bucket, "sys_dict.csv");

                await minioClient.GetObjectAsync(this.s3Config.bucket, "sys_dict.csv",
                                                 (stream) =>
                                                 {
                                                     using (TextReader textReader = new StreamReader(stream))
                                                     {
                                                         CsvReader csv = new CsvReader(textReader, System.Globalization.CultureInfo.CurrentCulture);

                                                         foreach (Cobra_Dict_State record in csv.GetRecords<Cobra_Dict_State>())
                                                         {
                                                             dict_state.Add(record);
                                                         }
                                                     }
                                                 });
            }
            catch (MinioException e)
            {
                statuslog.Append("Error occurred: " + e);
            }
        }

        private async Task LoadS3Dictionary_Tier()
        {
            try
            {
                TextInfo myTI = new CultureInfo("en-US", false).TextInfo;

                await minioClient.StatObjectAsync(this.s3Config.bucket, "sys_tiers.csv");

                await minioClient.GetObjectAsync(this.s3Config.bucket, "sys_tiers.csv",
                                                 (stream) =>
                                                 {
                                                     using (TextReader textReader = new StreamReader(stream))
                                                     {
                                                         CsvReader csv = new CsvReader(textReader, System.Globalization.CultureInfo.CurrentCulture);
                                                         foreach (Cobra_Dict_Tier record in csv.GetRecords<Cobra_Dict_Tier>())
                                                         {
                                                             record.TIER1NAME = record.TIER1NAME;
                                                             record.TIER2NAME = record.TIER2NAME;
                                                             record.TIER3NAME = record.TIER3NAME;
                                                             dict_tier.Add(record);
                                                         }
                                                     }
                                                 });
            }
            catch (MinioException e)
            {
                statuslog.Append("Error occurred: " + e);
            }
        }

        private async Task LoadS3Value()
        {
            try
            {
                await minioClient.StatObjectAsync(this.s3Config.bucket, "valuation_inventory_" + modelConfig.valuationdatayear + ".csv");

                await minioClient.GetObjectAsync(this.s3Config.bucket, "valuation_inventory_" + modelConfig.valuationdatayear + ".csv",
                                                 (stream) =>
                                                 {
                                                     using (TextReader textReader = new StreamReader(stream))
                                                     {
                                                         CsvReader csv = new CsvReader(textReader, System.Globalization.CultureInfo.CurrentCulture);
                                                         foreach (Cobra_Valuation record in csv.GetRecords<Cobra_Valuation>())
                                                         {
                                                             Valuationfunctions.Add(record);
                                                         }
                                                     }
                                                 });
            }
            catch (MinioException e)
            {
                statuslog.Append("Error occurred: " + e);
            }
        }

        private async Task LoadS3CR()
        {
            try
            {
                await minioClient.StatObjectAsync(this.s3Config.bucket, "sys_cr_inventory.csv");

                await minioClient.GetObjectAsync(this.s3Config.bucket, "sys_cr_inventory.csv",
                                                 (stream) =>
                                                 {
                                                     using (TextReader textReader = new StreamReader(stream))
                                                     {
                                                         CsvReader csv = new CsvReader(textReader, System.Globalization.CultureInfo.CurrentCulture);
                                                         foreach (Cobra_CR record in csv.GetRecords<Cobra_CR>())
                                                         {
                                                             if (record.ID == 1) //2025
                                                             {
                                                                 CRfunctions.Add(record);
                                                             }
                                                         }
                                                     }

                                                 });
            }
            catch (MinioException e)
            {
                statuslog.Append("Error occurred: " + e);
            }
        }

        private async Task LoadS3Incidence()
        {
            try
            {
                await minioClient.StatObjectAsync(this.s3Config.bucket, "incidence_inventory_" + modelConfig.incidencedatayear + ".csv");

                await minioClient.GetObjectAsync(this.s3Config.bucket, "incidence_inventory_" + modelConfig.incidencedatayear + ".csv",
                                                 (stream) =>
                                                 {
                                                     using (TextReader textReader = new StreamReader(stream))
                                                     {
                                                         CsvReader csv = new CsvReader(textReader, System.Globalization.CultureInfo.CurrentCulture);
                                                         foreach (Cobra_Incidence record in csv.GetRecords<Cobra_Incidence>())
                                                         {
                                                             if (record.DestinationID <= 3108)
                                                             {
                                                                 Incidence.Add(record);
                                                             }
                                                         }
                                                     }
                                                 });
            }
            catch (MinioException e)
            {
                statuslog.Append("Error occurred: " + e);
            }
        }


        private async Task LoadS3Pop()
        {
            try
            {
                await minioClient.StatObjectAsync(this.s3Config.bucket, "pop_inventory_" + modelConfig.populationdatayear + ".csv");

                await minioClient.GetObjectAsync(this.s3Config.bucket, "pop_inventory_" + modelConfig.populationdatayear + ".csv",
                                                 (stream) =>
                                                 {
                                                     using (TextReader textReader = new StreamReader(stream))
                                                     {
                                                         CsvReader csv = new CsvReader(textReader, System.Globalization.CultureInfo.CurrentCulture);
                                                         foreach (Cobra_POP record in csv.GetRecords<Cobra_POP>())
                                                         {
                                                             if (record.DestinationID != null && record.DestinationID <= 3108)
                                                             {
                                                                 Populations.Add(record);
                                                             }
                                                         }
                                                     }
                                                 });
            }
            catch (MinioException e)
            {
                statuslog.Append("Error occurred: " + e);
            }
        }
        public async Task GetGeoJsonDataAsync(string objectName)
        {
            if (minioClient == null)
            {
                throw new InvalidOperationException("MinIO client is not initialized.");
            }

            string bucketName = this.s3Config.bucket;

            try
            {
                Console.WriteLine($"Checking if object '{objectName}' exists in bucket '{bucketName}'.");
                await minioClient.StatObjectAsync(bucketName, objectName);
                Console.WriteLine($"Object '{objectName}' found. Starting download.");

                // Use a synchronous lambda here.
                await minioClient.GetObjectAsync(bucketName, objectName, (stream) =>
                {
                    try
                    {
                        if (stream == null)
                        {
                            Console.WriteLine($"Error: Stream for '{objectName}' is null.");
                            throw new NullReferenceException("MinIO returned a null stream.");
                        }

                        Console.WriteLine($"Reading GeoJSON data for '{objectName}'.");

                        using (TextReader reader = new StreamReader(stream))
                        {
                            // Synchronously read the entire stream.
                            string jsonData = reader.ReadToEnd();

                            if (string.IsNullOrWhiteSpace(jsonData))
                            {
                                Console.WriteLine($"Error: GeoJSON file '{objectName}' is empty.");
                                throw new Exception("GeoJSON file is empty.");
                            }

                            if (objectName == "cb2023_census_unsimplified_merged2020CT.geojson")
                            {
                                fullGeojson = jsonData; // Assign to instance variable
                            }
                            else
                            {
                                simpleGeojson = jsonData; // Assign to instance variable
                            }

                            Console.WriteLine($"Successfully read GeoJSON data for '{objectName}', length: {jsonData.Length} characters.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading GeoJSON data from '{objectName}': {ex.Message}");
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to retrieve GeoJSON '{objectName}': {ex.Message}");
                throw;
            }
        }





        public Func<DataRow, bool> BuildCriteria(EmissionsDataRetrievalRequest spec)
        {
            if (spec == null)
            {
                return row => true; // If spec is null, return a predicate that matches everything
            }

            List<Func<DataRow, bool>> tierPredicates = new List<Func<DataRow, bool>>();
            List<Func<DataRow, bool>> locationPredicates = new List<Func<DataRow, bool>>();

            // Handle tiers
            string[] thetiers = spec.tiers.Split(",");
            for (int i = 0; i < thetiers.Length; i++)
            {
                string tier = thetiers[i];
                int tierIndex = i + 1;

                // Use ToString() for comparison to avoid type issues
                tierPredicates.Add(row =>
                {
                    var columnValue = row[$"TIER{tierIndex}"];
                    return columnValue.ToString() == tier;
                });
            }

            // Build a list of states / two digit fips codes
            var specifiedStates = spec.fipscodes.Where(fips => fips.Length == 2).Distinct();

            // Build a list of counties / 5 digit fips codes
            var trueCounties = spec.fipscodes.Where(fips => fips.Length == 5).Distinct();

            var implicitStates = trueCounties.Select(fips => fips.Substring(0, 2)).Distinct();
            var actualStates = specifiedStates.Except(implicitStates);

            // State selection
            if (actualStates.Any())
            {
                locationPredicates.Add(row =>
                {
                    var stidValue = row["stid"];
                    string stidStr = stidValue.ToString();
                    return actualStates.Contains(stidStr);
                });
            }

            // County FIPS with some math
            foreach (var state in implicitStates)
            {
                var stateFipses = trueCounties
                    .Where(fips => fips.Substring(0, 2) == state)
                    .Select(fips => fips.Substring(2, 3))
                    .Distinct();

                locationPredicates.Add(row =>
                {
                    var stidValue = row["stid"];
                    var cyidValue = row["cyid"];
                    string stidStr = stidValue.ToString();
                    string cyidStr = cyidValue.ToString();
                    return stidStr == state && stateFipses.Contains(cyidStr);
                });
            }

            // Combining tier and location predicates
            return row => tierPredicates.All(pred => pred(row)) && locationPredicates.Any(pred => pred(row));
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
                rec.NOx = (dr.ItemArray[8] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[8];
                rec.SO2 = (dr.ItemArray[9] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[9];
                rec.NH3 = (dr.ItemArray[10] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[10];
                rec.SOA = (dr.ItemArray[11] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[11];
                rec.PM25 = (dr.ItemArray[12] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[12];
                rec.VOC = (dr.ItemArray[13] == System.DBNull.Value) ? 0 : (double)dr.ItemArray[13];
                result.Add(rec);
            }

            return result;
        }





        //keep here - move calling function to instance
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
                                     NOx = grp.Sum(r => r.Field<double?>("NOx")),
                                     SO2 = grp.Sum(r => r.Field<double?>("SO2")),
                                     NH3 = grp.Sum(r => r.Field<double?>("NH3")),
                                     SOA = grp.Sum(r => r.Field<double?>("SOA")),
                                     PM25 = grp.Sum(r => r.Field<double?>("PM25")),
                                     VOC = grp.Sum(r => r.Field<double?>("VOC"))
                                 };
                int recno = 1;
                foreach (var rowentry in summarized)
                {
                    summarizedemissionsData.Rows.Add(new object[] { recno, rowentry.typeindx, rowentry.sourceindx, rowentry.stid, rowentry.cyid, rowentry.tier1, rowentry.tier2, rowentry.tier3, rowentry.NOx.GetValueOrDefault(0), rowentry.SO2.GetValueOrDefault(0), rowentry.NH3.GetValueOrDefault(0), rowentry.SOA.GetValueOrDefault(0), rowentry.PM25.GetValueOrDefault(0), rowentry.VOC.GetValueOrDefault(0) });
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
                                      NOx = grp.Sum(r => r.Field<double?>("NOx")),
                                      SO2 = grp.Sum(r => r.Field<double?>("SO2")),
                                      NH3 = grp.Sum(r => r.Field<double?>("NH3")),
                                      SOA = grp.Sum(r => r.Field<double?>("SOA")),
                                      PM25 = grp.Sum(r => r.Field<double?>("PM25")),
                                      VOC = grp.Sum(r => r.Field<double?>("VOC"))
                                  };
                foreach (var rowentry in summarized2)
                {
                    summarizedemissionsData.Rows.Add(new object[] { recno, rowentry.typeindx, 0, rowentry.stid, rowentry.cyid, rowentry.tier1, rowentry.tier2, rowentry.tier3, rowentry.NOx.GetValueOrDefault(0), rowentry.SO2.GetValueOrDefault(0), rowentry.NH3.GetValueOrDefault(0), rowentry.SOA.GetValueOrDefault(0), rowentry.PM25.GetValueOrDefault(0), rowentry.VOC.GetValueOrDefault(0) });
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
                                 NOx = grp.Sum(r => r.Field<double?>("NOx")),
                                 SO2 = grp.Sum(r => r.Field<double?>("SO2")),
                                 NH3 = grp.Sum(r => r.Field<double?>("NH3")),
                                 SOA = grp.Sum(r => r.Field<double?>("SOA")),
                                 PM25 = grp.Sum(r => r.Field<double?>("PM25")),
                                 VOC = grp.Sum(r => r.Field<double?>("VOC"))
                             };

            int recno = 1;
            foreach (var rowentry in summarized)
            {
                summarizedemissionsData.Rows.Add(new object[] { recno, rowentry.typeindx, rowentry.sourceindx, rowentry.stid, 0, 0, 0, 0, rowentry.NOx.GetValueOrDefault(0), rowentry.SO2.GetValueOrDefault(0), rowentry.NH3.GetValueOrDefault(0), rowentry.SOA.GetValueOrDefault(0), rowentry.PM25.GetValueOrDefault(0), rowentry.VOC.GetValueOrDefault(0) });
                recno++;
            }

            return summarizedemissionsData;
        }


        //keep here
        public DataTable SummarizeQueueEmissionsWithCriteria(DataTable table2summarize, Func<DataRow, bool> criteria)
        {
            DataTable summarizedemissionsData = table2summarize.Clone();

            var subrows = table2summarize.AsEnumerable().Where(criteria).ToArray();

            // Fix up SOA from VOC
            foreach (var record in subrows)
            {
                if (record == null)
                {
                    continue;
                }
                record[11] = ComputeSOAfromVOC($"{record[5]}|{record[6]}|{record[7]}", record.Field<double>(13));
            }

            var summarized = from row in subrows
                             where row != null // Ensure no null rows are processed
                             group row by new { typeindx = 0, sourceindx = 0, stid = 0, cyid = 0 } into grp
                             select new
                             {
                                 typeindx = grp.Key.typeindx,
                                 sourceindx = grp.Key.sourceindx,
                                 stid = grp.Key.stid,
                                 cyid = grp.Key.cyid,
                                 NOx = grp.Sum(r => r.Field<double?>("NOx") ?? 0),
                                 SO2 = grp.Sum(r => r.Field<double?>("SO2") ?? 0),
                                 NH3 = grp.Sum(r => r.Field<double?>("NH3") ?? 0),
                                 SOA = grp.Sum(r => r.Field<double?>("SOA") ?? 0),
                                 PM25 = grp.Sum(r => r.Field<double?>("PM25") ?? 0),
                                 VOC = grp.Sum(r => r.Field<double?>("VOC") ?? 0)
                             };

            int recno = 1;
            foreach (var rowentry in summarized)
            {
                summarizedemissionsData.Rows.Add(new object[]
                {
            recno,
            rowentry.typeindx,
            rowentry.sourceindx,
            rowentry.stid,
            rowentry.cyid,
            0, 0, 0,
            rowentry.NOx,
            rowentry.SO2,
            rowentry.NH3,
            rowentry.SOA,
            rowentry.PM25,
            rowentry.VOC
                });
                recno++;
            }

            return summarizedemissionsData;
        }

        public DataTable SummarizeEmissionsWithCriteria(DataTable table2summarize, string criteria)
        {
            DataTable summarizedemissionsData = table2summarize.Clone();


            DataRow[] subrows = table2summarize.Select(criteria);

            //fix up SOA from VOC
            foreach (DataRow record in subrows)
            {
                if (record == null)
                {
                    continue;
                }
                record[11] = ComputeSOAfromVOC(record[5] + "|" + record[6] + "|" + record[7], record.Field<double>(13));
            }


            var summarized = from row in subrows.AsEnumerable()
                             where row != null // Ensure no null rows are processed
                             group row by new { typeindx = 0, sourceindx = 0, stid = 0, cyid = 0 } into grp
                             select new
                             {
                                 typeindx = grp.Key.typeindx,
                                 sourceindx = grp.Key.sourceindx,
                                 stid = grp.Key.stid,
                                 cyid = grp.Key.cyid,
                                 NOx = grp.Sum(r => r.Field<double?>("NOx") ?? 0),
                                 SO2 = grp.Sum(r => r.Field<double?>("SO2") ?? 0),
                                 NH3 = grp.Sum(r => r.Field<double?>("NH3") ?? 0),
                                 SOA = grp.Sum(r => r.Field<double?>("SOA") ?? 0),
                                 PM25 = grp.Sum(r => r.Field<double?>("PM25") ?? 0),
                                 VOC = grp.Sum(r => r.Field<double?>("VOC") ?? 0)
                             };

            int recno = 1;
            foreach (var rowentry in summarized)
            {
                summarizedemissionsData.Rows.Add(new object[] { recno, rowentry.typeindx, rowentry.sourceindx, rowentry.stid, rowentry.cyid, 0, 0, 0, rowentry.NOx, rowentry.SO2, rowentry.NH3, rowentry.SOA, rowentry.PM25, rowentry.VOC });
                recno++;
            }

            return summarizedemissionsData;
        }



        public bool UpdateEmissionsWithCriteria(DataTable table2summarize, EmissionsDataUpdateRequest requestparams)
        {
            Debug.WriteLine($"IN SINGLETON UDPATE EMISSIONS");
            bool allowNegative = false;
            if (requestparams.operationalMode != null && requestparams.operationalMode.ToUpper() == "AVERT")
            {
                allowNegative = true;
            }

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
                double current_NOx = current.control.Rows[0].Field<double?>("NOx").GetValueOrDefault(0);
                double current_so2 = current.control.Rows[0].Field<double?>("SO2").GetValueOrDefault(0);
                double current_nh3 = current.control.Rows[0].Field<double?>("NH3").GetValueOrDefault(0);
                double current_voc = current.control.Rows[0].Field<double?>("VOC").GetValueOrDefault(0);
                double current_soa = current.control.Rows[0].Field<double?>("SOA").GetValueOrDefault(0);

                //determine ratios
                double ratio_pm25 = current_pm25 == 0 ? 0 : requestparams.payload.PM25 / current_pm25;
                double ratio_NOx = current_NOx == 0 ? 0 : requestparams.payload.NOx / current_NOx;
                double ratio_so2 = current_so2 == 0 ? 0 : requestparams.payload.SO2 / current_so2;
                double ratio_nh3 = current_nh3 == 0 ? 0 : requestparams.payload.NH3 / current_nh3;
                double ratio_voc = current_voc == 0 ? 0 : requestparams.payload.VOC / current_voc;
                double ratio_soa = current_soa == 0 ? 0 : requestparams.payload.SOA / current_soa;

                foreach (DataRow record in subrows)
                {
                    //get current value
                    double thisrow_pm25 = record.Field<double?>("PM25").GetValueOrDefault(0);
                    double thisrow_NOx = record.Field<double?>("NOx").GetValueOrDefault(0);
                    double thisrow_so2 = record.Field<double?>("SO2").GetValueOrDefault(0);
                    double thisrow_nh3 = record.Field<double?>("NH3").GetValueOrDefault(0);
                    double thisrow_voc = record.Field<double?>("VOC").GetValueOrDefault(0);
                    double thisrow_soa = record.Field<double?>("SOA").GetValueOrDefault(0);
                    //and set, use ration if sum<>0 otherwise spread the increase over all
                    if (!allowNegative)
                    {
                        record["PM25"] = current_pm25 != 0 ? Math.Max(thisrow_pm25 * ratio_pm25, 0D) : Math.Max(requestparams.payload.PM25 / rowcount, 0D);
                        record["NOx"] = current_NOx != 0 ? Math.Max(thisrow_NOx * ratio_NOx, 0D) : Math.Max(requestparams.payload.NOx / rowcount, 0D);
                        record["SO2"] = current_so2 != 0 ? Math.Max(thisrow_so2 * ratio_so2, 0D) : Math.Max(requestparams.payload.SO2 / rowcount, 0D);
                        record["NH3"] = current_nh3 != 0 ? Math.Max(thisrow_nh3 * ratio_nh3, 0D) : Math.Max(requestparams.payload.NH3 / rowcount, 0D);
                        record["VOC"] = current_voc != 0 ? Math.Max(thisrow_voc * ratio_voc, 0D) : Math.Max(requestparams.payload.VOC / rowcount, 0D);
                        record["SOA"] = current_soa != 0 ? Math.Max(thisrow_soa * ratio_soa, 0D) : Math.Max(requestparams.payload.SOA / rowcount, 0D);
                    }
                    else
                    {
                        record["PM25"] = current_pm25 != 0 ? thisrow_pm25 * ratio_pm25 : requestparams.payload.PM25 / rowcount;
                        record["NOx"] = current_NOx != 0 ? thisrow_NOx * ratio_NOx : requestparams.payload.NOx / rowcount;
                        record["SO2"] = current_so2 != 0 ? thisrow_so2 * ratio_so2 : requestparams.payload.SO2 / rowcount;
                        record["NH3"] = current_nh3 != 0 ? thisrow_nh3 * ratio_nh3 : requestparams.payload.NH3 / rowcount;
                        record["VOC"] = current_voc != 0 ? thisrow_voc * ratio_voc : requestparams.payload.VOC / rowcount;
                        record["SOA"] = current_soa != 0 ? thisrow_soa * ratio_soa : requestparams.payload.SOA / rowcount;
                    }

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
                            if (!allowNegative)
                            {
                                table2summarize.Rows.Add(new object[] { maxID,
                                stackheight,
                                fips["SOURCEINDX"],
                                fips["STFIPS"],
                                fips["CNTYFIPS"],
                                tier["TIER1"],
                                tier["TIER2"],
                                tier["TIER3"],
                                Math.Max(requestparams.payload.NOx / numberofrowstoadd, 0D),   //watch order
                                Math.Max(requestparams.payload.SO2 / numberofrowstoadd, 0D),
                                Math.Max(requestparams.payload.NH3 / numberofrowstoadd, 0D),
                                Math.Max(requestparams.payload.SOA / numberofrowstoadd, 0D),
                                Math.Max(requestparams.payload.PM25 / numberofrowstoadd, 0D),
                                Math.Max(requestparams.payload.VOC / numberofrowstoadd, 0D) });
                            }
                            else
                            {
                                table2summarize.Rows.Add(new object[] { maxID,
                                stackheight,
                                fips["SOURCEINDX"],
                                fips["STFIPS"],
                                fips["CNTYFIPS"],
                                tier["TIER1"],
                                tier["TIER2"],
                                tier["TIER3"],
                                requestparams.payload.NOx / numberofrowstoadd,   //watch order
                                requestparams.payload.SO2 / numberofrowstoadd,
                                requestparams.payload.NH3 / numberofrowstoadd,
                                requestparams.payload.SOA / numberofrowstoadd,
                                requestparams.payload.PM25 / numberofrowstoadd,
                                requestparams.payload.VOC / numberofrowstoadd });
                            }

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

        //keep in singleton
        public Vector<double>[] Vectorize(DataTable emissions)
        {
            for (int i = 0; i < SR_dp.Length; i++)
            {
                Console.WriteLine($"SR_dp[{i}].RowCount = {SR_dp[i].RowCount}, ColumnCount = {SR_dp[i].ColumnCount}");
            }
            Vector<double>[] Vctr_pm_partial = new Vector<double>[4];
            Vector<double>[] Vctr_NOx_partial = new Vector<double>[4];
            Vector<double>[] Vctr_so2_partial = new Vector<double>[4];
            //Vector<double>[] Vctr_nh3_partial = new Vector<double>[4];
            Vector<double>[] Vctr_voc_partial = new Vector<double>[4];
            //Vector<double>[] Vctr_soa_partial = new Vector<double>[4];
            Vector<double>[] Vctr_O3N_partial = new Vector<double>[4];

            //populated from summarizedEmissions
            Vector<double>[] Vctr_pm = new Vector<double>[4];
            Vector<double>[] Vctr_NOx = new Vector<double>[4];
            Vector<double>[] Vctr_so2 = new Vector<double>[4];
            //Vector<double>[] Vctr_nh3 = new Vector<double>[4];
            Vector<double>[] Vctr_voc = new Vector<double>[4];
            //Vector<double>[] Vctr_soa = new Vector<double>[4];

            for (int i = 1; i < 5; i++)
            {
                //populated from summarizedEmissions which is county level
                Vctr_pm[i - 1] = Vector<double>.Build.Dense(3108, 0);
                Vctr_NOx[i - 1] = Vector<double>.Build.Dense(3108, 0);
                Vctr_so2[i - 1] = Vector<double>.Build.Dense(3108, 0);
                //Vctr_nh3[i - 1] = Vector<double>.Build.Dense(3108, 0);
                Vctr_voc[i - 1] = Vector<double>.Build.Dense(3108, 0);
                //Vctr_soa[i - 1] = Vector<double>.Build.Dense(3108, 0);
            }

            foreach (DataRow row in emissions.Rows)
            {
                int typeindex2use = row.Field<int>("typeindx") - 1;
                int sourceindex2use = row.Field<int>("sourceindx") - 1;
                try
                {
                    Vctr_pm[typeindex2use][sourceindex2use] = row.Field<double?>("PM25").GetValueOrDefault(0);
                    Vctr_NOx[typeindex2use][sourceindex2use] = row.Field<double?>("NOx").GetValueOrDefault(0);
                    Vctr_so2[typeindex2use][sourceindex2use] = row.Field<double?>("SO2").GetValueOrDefault(0);
                    //Vctr_nh3[typeindex2use][sourceindex2use] = row.Field<double?>("NH3").GetValueOrDefault(0);
                    Vctr_voc[typeindex2use][sourceindex2use] = row.Field<double?>("VOC").GetValueOrDefault(0);
                    // Vctr_soa[typeindex2use][sourceindex2use] = row.Field<double?>("SOA").GetValueOrDefault(0);
                }
                catch (Exception e)
                {
                    statuslog.Append("encountered unkown source or type index");
                }
            }

            for (int i = 1; i < 5; i++)
            {
                Vctr_pm_partial[i - 1] = SR_dp[i - 1].Multiply(Vctr_pm[i - 1]);
                Vctr_NOx_partial[i - 1] = SR_NOx[i - 1].Multiply(Vctr_NOx[i - 1]);
                Vctr_so2_partial[i - 1] = SR_SO4[i - 1].Multiply(Vctr_so2[i - 1]);
                //Vctr_nh3_partial[i - 1] = SR_nh3[i - 1].Multiply(Vctr_nh3[i - 1]) * 28778 * (18.03846 / 17.03052);
                Vctr_voc_partial[i - 1] = SR_O3V[i - 1].Multiply(Vctr_voc[i - 1]);   //voc short range
                //Vctr_soa_partial[i - 1] = SR_dp[i - 1].Multiply(Vctr_soa[i - 1]) * 28778; //transfers like pm
                Vctr_O3N_partial[i - 1] = SR_O3N[i - 1].Multiply(Vctr_NOx[i - 1]);   //voc short range

            }

            Vctr_pm_partial[0] = Vctr_pm_partial[0] + Vctr_pm_partial[1] + Vctr_pm_partial[2] + Vctr_pm_partial[3];
            Vctr_NOx_partial[0] = Vctr_NOx_partial[0] + Vctr_NOx_partial[1] + Vctr_NOx_partial[2] + Vctr_NOx_partial[3];
            Vctr_so2_partial[0] = Vctr_so2_partial[0] + Vctr_so2_partial[1] + Vctr_so2_partial[2] + Vctr_so2_partial[3];
            //Vctr_nh3_partial[0] = Vctr_nh3_partial[0] + Vctr_nh3_partial[1] + Vctr_nh3_partial[2] + Vctr_nh3_partial[3];
            Vctr_voc_partial[0] = Vctr_voc_partial[0] + Vctr_voc_partial[1] + Vctr_voc_partial[2] + Vctr_voc_partial[3];
            //Vctr_soa_partial[0] = Vctr_soa_partial[0] + Vctr_soa_partial[1] + Vctr_soa_partial[2] + Vctr_soa_partial[3];
            Vctr_O3N_partial[0] = Vctr_O3N_partial[0] + Vctr_O3N_partial[1] + Vctr_O3N_partial[2] + Vctr_O3N_partial[3];


            return new Vector<double>[5] { Vctr_pm_partial[0], Vctr_NOx_partial[0], /*Vctr_soa_partial[0],*/
        Vctr_so2_partial[0], /*Vctr_nh3_partial[0],*/ Vctr_voc_partial[0], Vctr_O3N_partial[0] };
        }


        public Vector<double> computeO3(Vector<double> value_VOC, Vector<double> value_O3N)
        {
            Vector<double> result_O3 = Vector<double>.Build.Dense(3108, 0);

            for (int i = 0; i < 3108; i++)
            {
                result_O3[i] = computeO3(value_VOC[i], value_O3N[i]);
            }

            return result_O3;
        }

        public double computeO3(double value_VOC, double value_O3N)
        {
            return value_VOC + value_O3N;
        }

        public Vector<double> computePM(Vector<double> value_PM25, Vector<double> value_NO3, /*Vector<double> value_SOA, /*Vector<double> value_NH4,*/ Vector<double> value_SO4)
        {
            //census level pm requires vector as large as number of census tract = 84660 
            Vector<double> result_pm = Vector<double>.Build.Dense(84660, 0);

            for (int i = 0; i < 84660; i++)
            {
                //make sure the censusindex actually maps to a countyIndex/destindx
                if (census_dict[i + 1].destindx != null && census_dict[i + 1].destindx != 0)
                {
                    int countyIndex = census_dict[i + 1].destindx.Value - 1;
                    result_pm[i] = computePM(value_PM25[i], value_NO3[countyIndex], /*value_SOA[i], value_NH4[i],*/ value_SO4[countyIndex]);
                }
                else
                {
                    result_pm[i] = computePM(value_PM25[i], 0, /*value_SOA[i], value_NH4[i],*/ 0);
                }




            }

            return result_pm;
        }

        public double computePM(double value_PM25, double value_NO3,/* double value_SOA, /*double value_NH4,*/ double value_SO4)
        {
            double result_pm = 0;
            result_pm = value_PM25 + value_NO3 + value_SO4;

            return result_pm;
        }


        public double crfunc(string rawfunction, string compfunction, double Incidence, double Beta, double DELTAQ, double DELTAO, double POP, double A, double B, double C)
        {

            try
            {



                switch (compfunction.ToUpperInvariant()) // Converting to uppercase for case-insensitive matching
                {
                    case "(1-(1/((1-INCIDENCE)*EXP(BETA*DELTAQ)+INCIDENCE)))*INCIDENCE*POP":
                        return (1 - (1 / ((1 - Incidence) * Math.Exp(Beta * DELTAQ) + Incidence))) * Incidence * POP;
                    case "(1-(1/((1-INCIDENCE)*EXP(BETA*DELTAQ)+INCIDENCE)))*INCIDENCE*A*POP":
                        return (1 - (1 / ((1 - Incidence) * Math.Exp(Beta * DELTAQ) + Incidence))) * Incidence * A * POP;
                    case "(1-(1/((1-INCIDENCE)*EXP(BETA*DELTAQ)+INCIDENCE)))*INCIDENCE*POP*A":
                        return (1 - (1 / ((1 - Incidence) * Math.Exp(Beta * DELTAQ) + Incidence))) * Incidence * A * POP;
                    case "(1-(1/((1-INCIDENCE*A)*EXP(BETA*DELTAQ)+INCIDENCE*A)))*INCIDENCE*A*POP":
                        return (1 - (1 / ((1 - Incidence * A) * Math.Exp(Beta * DELTAQ) + Incidence * A))) * Incidence * A * POP;
                    case "(1-(1/EXP(BETA*DELTAQ)))*INCIDENCE*A*POP":
                        return (1 - (1 / Math.Exp(Beta * DELTAQ))) * Incidence * POP * A;
                    case "(1-(1/EXP(BETA*DELTAQ)))*INCIDENCE*POP*A":
                        return (1 - (1 / Math.Exp(Beta * DELTAQ))) * Incidence * POP * A;
                    case "(1-EXP(-BETA*DELTAQ))*INCIDENCE*POP*A":
                        return (1 - Math.Exp(-Beta * DELTAQ)) * Incidence * POP * A;
                    case "(1-EXP(-BETA*DELTAQ))*INCIDENCE*A*POP":
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
                    case "(1-(1/EXP(BETA*DELTAQ)))*A*POP*INCIDENCE":
                        return (1 - (1 / Math.Exp(Beta * DELTAQ))) * A * Incidence * POP;
                    case "(1-(1/EXP(BETA*DELTAQ)))*INCIDENCE*POP*(1-A)":
                        return (1 - (1 / Math.Exp(Beta * DELTAQ))) * Incidence * POP * (1 - A);
                    case "(1-(1/((1-INCIDENCE)*EXP(BETA*A*DELTAQ)+INCIDENCE)))*INCIDENCE*POP":
                        return (1 - (1 / ((1 - Incidence) * Math.Exp(Beta * A * DELTAQ) + Incidence))) * Incidence * POP;
                    // DELTAO Parsing
                    case "(1-(1/EXP(BETA*DELTAO)))*INCIDENCE*POP":
                        return (1 - (1 / Math.Exp(Beta * DELTAO))) * Incidence * POP;
                    case "(1-(1/((1-A)*EXP(BETA*DELTAO)+A)))*A*POP*INCIDENCE":
                        return (1 - (1 / ((1 - A) * Math.Exp(Beta * DELTAO) + A))) * A * POP * Incidence;
                    case "(1-(1/((1-INCIDENCE)*EXP(BETA*A*DELTAO)+INCIDENCE)))*INCIDENCE*POP":
                        return (1 - (1 / ((1 - Incidence) * Math.Exp(Beta * A * DELTAO) + Incidence))) * Incidence * POP;
                    case "(1-(1/EXP(BETA*DELTAO)))*INCIDENCE*A*POP*(1-A)":
                        return (1 - (1 / Math.Exp(Beta * DELTAO))) * Incidence * POP * (1 - A);
                    case "(1-(1/((1-INCIDENCE)*EXP(BETA*DELTAO)+INCIDENCE)))*INCIDENCE*POP":
                        return (1 - (1 / ((1 - Incidence) * Math.Exp(Beta * DELTAO) + Incidence))) * Incidence * POP;
                    case "(1-(1/EXP(BETA*DELTAO)))*INCIDENCE*POP*A*B":
                        return (1 - (1 / Math.Exp(Beta * DELTAO))) * Incidence * POP * A * B;
                    default:
                        rawfunction = rawfunction.ToUpperInvariant();
                        rawfunction = rawfunction.Replace("EXP", "Exp", StringComparison.OrdinalIgnoreCase);
                        Expression e = new Expression(rawfunction);
                        e.Parameters["A"] = A;
                        e.Parameters["B"] = B;
                        e.Parameters["C"] = C;
                        e.Parameters["BETA"] = Beta;
                        e.Parameters["DELTAQ"] = DELTAQ;
                        e.Parameters["DELTAO"] = DELTAO;
                        e.Parameters["INCIDENCE"] = Incidence;
                        e.Parameters["POP"] = POP;

                        if (e.HasErrors())
                        {
                            Console.WriteLine($"Expression has errors: {rawfunction}");
                            throw new ArgumentException($"Invalid expression: {rawfunction}");
                        }

                        return (double)e.Evaluate();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in crfunc: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                Console.WriteLine($"Compfunction: {compfunction}, Rawfunction: {rawfunction}");
                Console.WriteLine($"rawfunction: {rawfunction}");
                Console.WriteLine($"compfunction: {compfunction}");
                Console.WriteLine($"Parameters - Incidence: {Incidence}, Beta: {Beta}, DELTAQ: {DELTAQ}, DELTAO: {DELTAO}, POP: {POP}, A: {A}, B: {B}, C: {C}");
                throw; // Re-throw the exception to maintain stack trace and propagate it
            }




        }

        public double perannumvalue(int year, double weight, double factor)
        {
            return weight / Math.Pow(1 + factor, year);
        }

        public double adjustmentfactorfromdiscountrate(double factor)
        {
            double result = 0;
            result = result + perannumvalue(0, 0.3, factor);
            result = result + perannumvalue(1, 0.1, factor);
            result = result + perannumvalue(2, 0.1, factor);
            result = result + perannumvalue(3, 0.1, factor);
            result = result + perannumvalue(4, 0.1, factor);
            result = result + perannumvalue(5, 0.1, factor);
            result = result + perannumvalue(6, 0.0142857142857143, factor);
            result = result + perannumvalue(7, 0.0142857142857143, factor);
            result = result + perannumvalue(8, 0.0142857142857143, factor);
            result = result + perannumvalue(9, 0.0142857142857143, factor);
            result = result + perannumvalue(10, 0.0142857142857143, factor);
            result = result + perannumvalue(11, 0.0142857142857143, factor);
            result = result + perannumvalue(12, 0.0142857142857143, factor);
            result = result + perannumvalue(13, 0.0142857142857143, factor);
            result = result + perannumvalue(14, 0.0142857142857143, factor);
            result = result + perannumvalue(15, 0.0142857142857143, factor);
            result = result + perannumvalue(16, 0.0142857142857143, factor);
            result = result + perannumvalue(17, 0.0142857142857143, factor);
            result = result + perannumvalue(18, 0.0142857142857143, factor);
            result = result + perannumvalue(19, 0.0142857142857143, factor);
            return result;
        }

        //move to instance
        public List<Cobra_ResultDetail> ComputeImpacts(List<Cobra_Destination> Destinations, double valat)
        {
            statuslog.Clear();
            statuslog.AppendLine("crfunc.FunctionID.ToString(),age.ToString(),POP.ToString(),Incidence.ToString(),rawresult.ToString(),result.ToString()");

            var concurrentResultsCr = new ConcurrentDictionary<string, Result>();
            var concurrentResultsValuation = new ConcurrentDictionary<string, Result>();

            List<string> Endpoints_cr = CRfunctions.Select(c => c.Endpoint).Distinct().ToList();
            List<string> Endpoints_val = Valuationfunctions.Select(c => c.Endpoint).Distinct().ToList();

            Dictionary<string, Cobra_POP> dict_pop = new Dictionary<string, Cobra_POP>(this.Populations.Count());
            foreach (var item in Populations)
            {
                dict_pop.Add(item.dest_tract, item);
            }

            Dictionary<string, Cobra_Incidence> dict_incidence = new Dictionary<string, Cobra_Incidence>();
            foreach (var item in Incidence)
            {
                dict_incidence.Add(item.DestinationID.ToString() + "|" + item.Endpoint, item);
            }

            Parallel.ForEach(Valuationfunctions, valuefunc =>
            {
                string function = valuefunc.Function.Replace("EXP", "Exp").Replace("exp", "Exp");
                string cleanfunction = function.ToUpper().Replace(" ", "");

                double metric_adjustment = valuefunc.Seasonal_Metric.ToUpper() switch
                {
                    "DAILY" => 365,
                    "OZONE" => 152,
                    _ => 1
                };

                double poolingweight = valuefunc.PoolingWeight.GetValueOrDefault(0);
                double valuefuncValue = valuefunc.Value.GetValueOrDefault(0);
                double discountFactor = valat == 0 || valuefunc.ApplyDiscount == "NO" ? 1.1225 : adjustmentfactorfromdiscountrate(valat / 100) * 1.1225;

                foreach (var destination in Destinations)
                {
                    Expression e = new Expression(function);
                    double A = valuefunc.A.GetValueOrDefault(0);
                    double B = valuefunc.B.GetValueOrDefault(0);
                    double C = valuefunc.C.GetValueOrDefault(0);
                    double Beta = valuefunc.Beta.GetValueOrDefault(0);
                    double DELTAQ = destination.DELTA_FINAL_PM.GetValueOrDefault(0);
                    double DELTAO = destination.DELTA_FINAL_O3.GetValueOrDefault(0);

                    var incidencerow = dict_incidence.TryGetValue(destination.destindx + "|" + valuefunc.IncidenceEndpoint, out var value)
                        ? value
                        : null;

                    for (long age = valuefunc.Start_Age.GetValueOrDefault(0); age <= valuefunc.End_Age.GetValueOrDefault(0); age++)
                    {
                        double Incidence = incidencerow?.incidenceat(age) ?? 0;
                        double POP = dict_pop.TryGetValue(destination.tract_id, out var poprow) ? poprow.popat(age) : 0;

                        double numCases = this.crfunc(function, cleanfunction, Incidence, Beta, DELTAQ, DELTAO, POP, A, B, C) * poolingweight * metric_adjustment;

                        // Update or add entry to concurrentResultsCr concurrently
                        string tractKey = destination.tract_id + "|" + valuefunc.Endpoint;
                        concurrentResultsCr.AddOrUpdate(tractKey,
                            new Result { TractID = destination.tract_id, DestinationIndex = destination.destindx.GetValueOrDefault(0), Endpoint = valuefunc.Endpoint, Value = numCases },
                            (key, existingResult) => { existingResult.Value += numCases; return existingResult; });

                        // Update national total in concurrentResultsCr
                        string nationKey = "nation|" + valuefunc.Endpoint;
                        concurrentResultsCr.AddOrUpdate(nationKey,
                            new Result { TractID = destination.tract_id, DestinationIndex = destination.destindx.GetValueOrDefault(0), Endpoint = valuefunc.Endpoint, Value = numCases },
                            (key, existingResult) => { existingResult.Value += numCases; return existingResult; });

                        double valueCases = numCases * valuefuncValue * discountFactor;

                        // Update or add entry to concurrentResultsValuation concurrently
                        concurrentResultsValuation.AddOrUpdate(tractKey,
                            new Result { TractID = destination.tract_id, DestinationIndex = destination.destindx.GetValueOrDefault(0), Endpoint = valuefunc.Endpoint, Value = valueCases },
                            (key, existingResult) => { existingResult.Value += valueCases; return existingResult; });

                        // Update national total in concurrentResultsValuation
                        concurrentResultsValuation.AddOrUpdate(nationKey,
                            new Result { TractID = destination.tract_id, DestinationIndex = destination.destindx.GetValueOrDefault(0), Endpoint = valuefunc.Endpoint, Value = valueCases },
                            (key, existingResult) => { existingResult.Value += valueCases; return existingResult; });
                    }
                }
            });




            var stateLookup = dict_state.ToDictionary(d => d.SOURCEINDX, d => d);
            // Convert census_dict to a dictionary keyed by dest_tract for efficient lookup
            var censusLookup = census_dict.Values.ToDictionary(c => c.dest_tract, c => c);

            // Use a ConcurrentBag for thread-safe adding of results
            var concurrentResults = new ConcurrentBag<Cobra_ResultDetail>();
            foreach (var destination in Destinations)
            {
                
                    Cobra_ResultDetail result_record = new Cobra_ResultDetail
                    {
                        destindx = destination.destindx,
                        BASE_FINAL_PM = destination.BASE_FINAL_PM,
                        CTRL_FINAL_PM = destination.CTRL_FINAL_PM,
                        DELTA_FINAL_PM = destination.DELTA_FINAL_PM,
                        BASE_FINAL_O3 = destination.BASE_FINAL_O3,
                        CTRL_FINAL_O3 = destination.CTRL_FINAL_O3,
                        DELTA_FINAL_O3 = destination.DELTA_FINAL_O3,
                        tract_id = destination.tract_id,
                        TRIBES = destination.Tribes
                    };

                    // Use pre-built dictionary lookup to get state and county info
                    if (stateLookup.TryGetValue(result_record.destindx, out var loc))
                    {
                        result_record.FIPS = loc.FIPS;
                        result_record.STATE = loc.STNAME;
                        result_record.COUNTY = loc.CYNAME;
                    }
                    /*add disadvantaged community vars to result record */
                    if (censusLookup.TryGetValue(destination.tract_id, out var censusRecord))
                    {
                        result_record.CJEST = censusRecord.CJEST;
                        result_record.IRA_fraction = censusRecord.IRA_fraction;
                    }
                    else
                    {
                        //default to 0
                        result_record.CJEST = 0;
                        result_record.IRA_fraction = 0;
                    }
        // base key for concurrentResultsCr and concurrentResultsValuation dictionaries
        string baseKey = destination.tract_id + "|";

                    result_record.PM_Acute_Myocardial_Infarction_Nonfatal = concurrentResultsCr[baseKey + "PM Acute Myocardial Infarction, Nonfatal"].Value;


                    result_record.PM_HA_All_Respiratory = concurrentResultsCr[baseKey + "PM HA, All Respiratory"].Value;
                    result_record.PM_Minor_Restricted_Activity_Days = concurrentResultsCr[baseKey + "PM Minor Restricted Activity Days"].Value;
                    result_record.PM_Mortality_All_Cause__low_ = concurrentResultsCr[baseKey + "PM Mortality, All Cause (low)"].Value;
                    result_record.PM_Mortality_All_Cause__high_ = concurrentResultsCr[baseKey + "PM Mortality, All Cause (high)"].Value;
                    result_record.PM_Infant_Mortality = concurrentResultsCr[baseKey + "PM Infant Mortality"].Value;
                    result_record.PM_Work_Loss_Days = concurrentResultsCr[baseKey + "PM Work Loss Days"].Value;
                    result_record.PM_Incidence_Lung_Cancer = concurrentResultsCr[baseKey + "PM Incidence, Lung Cancer"].Value;

                    result_record.PM_Incidence_Hay_Fever_Rhinitis = concurrentResultsCr[baseKey + "PM Incidence, Hay Fever/Rhinitis"].Value;
                    result_record.PM_Incidence_Asthma = concurrentResultsCr[baseKey + "PM Incidence, Asthma"].Value;
                    result_record.PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease = concurrentResultsCr[baseKey + "PM HA, Cardio-, Cerebro- and Peripheral Vascular Disease"].Value;
                    result_record.PM_HA_Alzheimers_Disease = concurrentResultsCr[baseKey + "PM HA, Alzheimers Disease"].Value;
                    result_record.PM_HA_Parkinsons_Disease = concurrentResultsCr[baseKey + "PM HA, Parkinsons Disease"].Value;
                    result_record.PM_Incidence_Stroke = concurrentResultsCr[baseKey + "PM Incidence, Stroke"].Value;
                    result_record.PM_Incidence_Out_of_Hospital_Cardiac_Arrest = concurrentResultsCr[baseKey + "PM Incidence, Out of Hospital Cardiac Arrest"].Value;
                    result_record.PM_Asthma_Symptoms_Albuterol_use = concurrentResultsCr[baseKey + "PM Asthma Symptoms, Albuterol use"].Value;
                    result_record.PM_HA_Respiratory2 = concurrentResultsCr[baseKey + "PM HA, Respiratory-2"].Value;
                    result_record.PM_ER_visits_respiratory = concurrentResultsCr[baseKey + "PM ER visits, respiratory"].Value;
                    result_record.PM_ER_visits_All_Cardiac_Outcomes = concurrentResultsCr[baseKey + "PM ER visits, All Cardiac Outcomes"].Value;

                    result_record.O3_ER_visits_respiratory = concurrentResultsCr[baseKey + "O3 ER visits, respiratory"].Value;
                    result_record.O3_HA_All_Respiratory = concurrentResultsCr[baseKey + "O3 HA, All Respiratory"].Value;
                    result_record.O3_Incidence_Hay_Fever_Rhinitis = concurrentResultsCr[baseKey + "O3 Incidence, Hay Fever/Rhinitis"].Value;
                    result_record.O3_Incidence_Asthma = concurrentResultsCr[baseKey + "O3 Incidence, Asthma"].Value;
                    result_record.O3_Asthma_Symptoms_Chest_Tightness = concurrentResultsCr[baseKey + "O3 Asthma Symptoms, Chest Tightness"].Value;
                    result_record.O3_Asthma_Symptoms_Cough = concurrentResultsCr[baseKey + "O3 Asthma Symptoms, Cough"].Value;
                    result_record.O3_Asthma_Symptoms_Shortness_of_Breath = concurrentResultsCr[baseKey + "O3 Asthma Symptoms, Shortness of Breath"].Value;
                    result_record.O3_Asthma_Symptoms_Wheeze = concurrentResultsCr[baseKey + "O3 Asthma Symptoms, Wheeze"].Value;
                    result_record.O3_ER_Visits_Asthma = concurrentResultsCr[baseKey + "O3 Emergency Room Visits, Asthma"].Value;
                    result_record.O3_School_Loss_Days = concurrentResultsCr[baseKey + "O3 School Loss Days, All Cause"].Value;
                    result_record.O3_Mortality_Longterm_exposure = concurrentResultsCr[baseKey + "O3 Mortality, Long-term exposure"].Value;
                    result_record.O3_Mortality_Shortterm_exposure = concurrentResultsCr[baseKey + "O3 Mortality, Short-term exposure"].Value;


                    result_record.C__PM_Acute_Myocardial_Infarction_Nonfatal = concurrentResultsValuation[baseKey + "PM Acute Myocardial Infarction, Nonfatal"].Value;

                    result_record.C__PM_Resp_Hosp_Adm = concurrentResultsValuation[baseKey + "PM HA, All Respiratory"].Value;
                    result_record.C__PM_Minor_Restricted_Activity_Days = concurrentResultsValuation[baseKey + "PM Minor Restricted Activity Days"].Value;
                    result_record.C__PM_Mortality_All_Cause__low_ = concurrentResultsValuation[baseKey + "PM Mortality, All Cause (low)"].Value;
                    result_record.C__PM_Mortality_All_Cause__high_ = concurrentResultsValuation[baseKey + "PM Mortality, All Cause (high)"].Value;
                    result_record.C__PM_Infant_Mortality = concurrentResultsValuation[baseKey + "PM Infant Mortality"].Value;

                    result_record.C__PM_Work_Loss_Days = concurrentResultsValuation[baseKey + "PM Work Loss Days"].Value;
                    result_record.C__PM_Incidence_Lung_Cancer = concurrentResultsValuation[baseKey + "PM Incidence, Lung Cancer"].Value;

                    result_record.C__PM_Incidence_Hay_Fever_Rhinitis = concurrentResultsValuation[baseKey + "PM Incidence, Hay Fever/Rhinitis"].Value;
                    result_record.C__PM_Incidence_Asthma = concurrentResultsValuation[baseKey + "PM Incidence, Asthma"].Value;
                    result_record.C__PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease = concurrentResultsValuation[baseKey + "PM HA, Cardio-, Cerebro- and Peripheral Vascular Disease"].Value;
                    result_record.C__PM_HA_Alzheimers_Disease = concurrentResultsValuation[baseKey + "PM HA, Alzheimers Disease"].Value;
                    result_record.C__PM_HA_Parkinsons_Disease = concurrentResultsValuation[baseKey + "PM HA, Parkinsons Disease"].Value;
                    result_record.C__PM_Incidence_Stroke = concurrentResultsValuation[baseKey + "PM Incidence, Stroke"].Value;
                    result_record.C__PM_Incidence_Out_of_Hospital_Cardiac_Arrest = concurrentResultsValuation[baseKey + "PM Incidence, Out of Hospital Cardiac Arrest"].Value;
                    result_record.C__PM_Asthma_Symptoms_Albuterol_use = concurrentResultsValuation[baseKey + "PM Asthma Symptoms, Albuterol use"].Value;
                    result_record.C__PM_HA_Respiratory2 = concurrentResultsValuation[baseKey + "PM HA, Respiratory-2"].Value;
                    result_record.C__PM_ER_visits_respiratory = concurrentResultsValuation[baseKey + "PM ER visits, respiratory"].Value;
                    result_record.C__PM_ER_visits_All_Cardiac_Outcomes = concurrentResultsValuation[baseKey + "PM ER visits, All Cardiac Outcomes"].Value;

                    result_record.C__O3_ER_visits_respiratory = concurrentResultsValuation[baseKey + "O3 ER visits, respiratory"].Value;
                    result_record.C__O3_HA_All_Respiratory = concurrentResultsValuation[baseKey + "O3 HA, All Respiratory"].Value;
                    result_record.C__O3_Incidence_Hay_Fever_Rhinitis = concurrentResultsValuation[baseKey + "O3 Incidence, Hay Fever/Rhinitis"].Value;
                    result_record.C__O3_Incidence_Asthma = concurrentResultsValuation[baseKey + "O3 Incidence, Asthma"].Value;
                    result_record.C__O3_Asthma_Symptoms_Chest_Tightness = concurrentResultsValuation[baseKey + "O3 Asthma Symptoms, Chest Tightness"].Value;
                    result_record.C__O3_Asthma_Symptoms_Cough = concurrentResultsValuation[baseKey + "O3 Asthma Symptoms, Cough"].Value;
                    result_record.C__O3_Asthma_Symptoms_Shortness_of_Breath = concurrentResultsValuation[baseKey + "O3 Asthma Symptoms, Shortness of Breath"].Value;
                    result_record.C__O3_Asthma_Symptoms_Wheeze = concurrentResultsValuation[baseKey + "O3 Asthma Symptoms, Wheeze"].Value;
                    result_record.C__O3_ER_Visits_Asthma = concurrentResultsValuation[baseKey + "O3 Emergency Room Visits, Asthma"].Value;
                    result_record.C__O3_School_Loss_Days = concurrentResultsValuation[baseKey + "O3 School Loss Days, All Cause"].Value;
                    result_record.C__O3_Mortality_Longterm_exposure = concurrentResultsValuation[baseKey + "O3 Mortality, Long-term exposure"].Value;
                    result_record.C__O3_Mortality_Shortterm_exposure = concurrentResultsValuation[baseKey + "O3 Mortality, Short-term exposure"].Value;

                    //now do total health effect dollars
                    double lowvals = 0;

                    //add all health effects to low vals that do not have high/low differences
                    lowvals += result_record.C__PM_Acute_Myocardial_Infarction_Nonfatal.GetValueOrDefault(0);
                    lowvals += result_record.C__PM_Resp_Hosp_Adm.GetValueOrDefault(0);
                    lowvals += result_record.C__PM_Minor_Restricted_Activity_Days.GetValueOrDefault(0);
                    lowvals += result_record.C__PM_Infant_Mortality.GetValueOrDefault(0);
                    lowvals += result_record.C__PM_Work_Loss_Days.GetValueOrDefault(0);
                    lowvals += result_record.C__PM_Incidence_Lung_Cancer.GetValueOrDefault(0);
                    lowvals += result_record.C__PM_Incidence_Hay_Fever_Rhinitis.GetValueOrDefault(0);
                    lowvals += result_record.C__PM_Incidence_Asthma.GetValueOrDefault(0);
                    lowvals += result_record.C__PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease.GetValueOrDefault(0);
                    lowvals += result_record.C__PM_HA_Alzheimers_Disease.GetValueOrDefault(0);
                    lowvals += result_record.C__PM_HA_Parkinsons_Disease.GetValueOrDefault(0);
                    lowvals += result_record.C__PM_Incidence_Stroke.GetValueOrDefault(0);
                    lowvals += result_record.C__PM_Incidence_Out_of_Hospital_Cardiac_Arrest.GetValueOrDefault(0);
                    lowvals += result_record.C__PM_Asthma_Symptoms_Albuterol_use.GetValueOrDefault(0);
                    lowvals += result_record.C__PM_HA_Respiratory2.GetValueOrDefault(0);
                    lowvals += result_record.C__PM_ER_visits_respiratory.GetValueOrDefault(0);
                    lowvals += result_record.C__PM_ER_visits_All_Cardiac_Outcomes.GetValueOrDefault(0);

                    //get all PM
                    result_record.C__Total_PM_Low_Value = lowvals;
                    result_record.C__Total_PM_High_Value = lowvals;
                    //separately add low or high mortality to appropriate total var
                    result_record.C__Total_PM_High_Value += result_record.C__PM_Mortality_All_Cause__high_.GetValueOrDefault(0);
                    result_record.C__Total_PM_Low_Value += result_record.C__PM_Mortality_All_Cause__low_.GetValueOrDefault(0);



                    lowvals += result_record.C__O3_ER_visits_respiratory.GetValueOrDefault(0);
                    lowvals += result_record.C__O3_HA_All_Respiratory.GetValueOrDefault(0);
                    lowvals += result_record.C__O3_Incidence_Hay_Fever_Rhinitis.GetValueOrDefault(0);
                    lowvals += result_record.C__O3_Incidence_Asthma.GetValueOrDefault(0);
                    lowvals += result_record.C__O3_Asthma_Symptoms_Chest_Tightness.GetValueOrDefault(0);

                    lowvals += result_record.C__O3_Asthma_Symptoms_Cough.GetValueOrDefault(0);
                    lowvals += result_record.C__O3_Asthma_Symptoms_Shortness_of_Breath.GetValueOrDefault(0);
                    lowvals += result_record.C__O3_Asthma_Symptoms_Wheeze.GetValueOrDefault(0);
                    lowvals += result_record.C__O3_ER_Visits_Asthma.GetValueOrDefault(0);
                    lowvals += result_record.C__O3_School_Loss_Days.GetValueOrDefault(0);
                    lowvals += result_record.C__O3_Mortality_Longterm_exposure.GetValueOrDefault(0);
                    lowvals += result_record.C__O3_Mortality_Shortterm_exposure.GetValueOrDefault(0);


                    //get all O3
                    result_record.C__Total_O3_Value = result_record.C__O3_Asthma_Symptoms_Cough.GetValueOrDefault(0)
                    + result_record.C__O3_Asthma_Symptoms_Shortness_of_Breath.GetValueOrDefault(0)
                    + result_record.C__O3_Asthma_Symptoms_Wheeze.GetValueOrDefault(0)
                    + result_record.C__O3_ER_Visits_Asthma.GetValueOrDefault(0)
                    + result_record.C__O3_School_Loss_Days.GetValueOrDefault(0)
                    + result_record.C__O3_Mortality_Longterm_exposure.GetValueOrDefault(0)
                    + result_record.C__O3_Mortality_Shortterm_exposure.GetValueOrDefault(0);

                    result_record.C__Total_Health_Benefits_Low_Value = lowvals;

                    //add low to high this works
                    result_record.C__Total_Health_Benefits_High_Value = lowvals;

                    //add the endpoints with different high/low vals (in this case only PM_mortality)
                    result_record.C__Total_Health_Benefits_High_Value += result_record.C__PM_Mortality_All_Cause__high_.GetValueOrDefault(0);
                    result_record.C__Total_Health_Benefits_Low_Value += result_record.C__PM_Mortality_All_Cause__low_.GetValueOrDefault(0);


                concurrentResults.Add(result_record);



            }
            // clear out resultscr and valuation
            concurrentResultsCr = null;
            concurrentResultsValuation = null;


            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            return concurrentResults.ToList();



        }

    }
}

