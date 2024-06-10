using CobraComputeAPI;
using CsvHelper;
//using DocumentFormat.OpenXml.Drawing.Charts;
using FastMember;
using ICSharpCode.SharpZipLib.Zip;
using MathNet.Numerics.Data.Text;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra;
using Minio;
using Minio.Exceptions;
using NCalc;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace CobraCompute
{
    public class CobraComputeCore
    {

        public bool initilized = false;

        private string datapath = "";

        private Matrix<double>[] SR_dp = new Matrix<double>[4];
        private Matrix<double>[] SR_NOx = new Matrix<double>[4];
        private Matrix<double>[] SR_SO4 = new Matrix<double>[4];
        //private Matrix<double>[] SR_nh3 = new Matrix<double>[4];

        private Matrix<double>[] SR_O3N = new Matrix<double>[4];
        private Matrix<double>[] SR_O3V = new Matrix<double>[4];

        public System.Data.DataTable EmissionsInventory;
        public System.Data.DataTable SummarizedEmissionsInventory;

        public ScenarioManager_Redis Scenarios;
        private UserScenario currentscenario;

        private List<Cobra_POP> Populations = new List<Cobra_POP>();
        private List<Cobra_Incidence> Incidence = new List<Cobra_Incidence>();
        private List<Cobra_CR> CRfunctions = new List<Cobra_CR>();
        private List<Cobra_Valuation> Valuationfunctions = new List<Cobra_Valuation>();

        public List<Cobra_Dict_State> dict_state = new List<Cobra_Dict_State>();
        public List<Cobra_Dict_Tier> dict_tier = new List<Cobra_Dict_Tier>();

        private Vector<double> Adjustment = Vector<double>.Build.Dense(3108);
        private Dictionary<string, double> VOC2SOA = new Dictionary<string, double>();

        private Vector<double>[] aqbase;
        private Vector<double> pm_base;
        private Vector<double> o3_base;

        public StringBuilder statuslog = new StringBuilder();

        private RedisConfig redisOptions;

        private MinioClient minioClient;
        private S3Config s3Config;

        private ModelConfig modelConfig;

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

        public void reset_userscenario(Guid token)
        {
            Scenarios.resetUserScenario(token);
        }


        public void store_userscenario()
        {
            Scenarios.store(currentscenario);
        }
        public void delete_userscenario(Guid token)
        {
            Scenarios.deleteUserScenario(token);
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

        public bool initialize(string path = "data/")
        {
            Console.WriteLine("beginning initialize");
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
                LoadS3Pop().Wait();

                //load incidence
                LoadS3Incidence().Wait();

                //load cr
                LoadS3CR().Wait();

                //load valuation
                LoadS3Value().Wait();

                //load dictionar(ies)
                Console.WriteLine("loading state and tier dictionaries");
                statuslog.Append("loading state and tier dictionaries");
                LoadS3Dictionary_State().Wait();
                LoadS3Dictionary_Tier().Wait();
                Console.WriteLine("done waits for state and tier");
                statuslog.Append("done waits for state and tier");

                //load adjustment factors
                LoadS3Adjustments().Wait();


                //load voc2soa
                LoadS3VOC2SOA().Wait();

                //load emissions
                LoadS3Emissions().Wait();

                //debug
                recno = SummarizeEmissions();

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                //load matrix data
                //LoadS3SRfrommtx().Wait();
                Console.WriteLine("loading SR MATRIX");
                statuslog.Append("loading SR MATRIX");

                InitBlankSR();
                LoadS3SR().Wait();
                //LoadSR(path);

                Console.WriteLine("garbage collection");
                statuslog.Append("garbage collection");

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // compute baseline AQ components

                aqbase = Vectorize(SummarizedEmissionsInventory);
                //aqbase looks like:   [PM, NOx, SOA, SO2, VOC, O3N ];
                pm_base = computePM(aqbase[0], aqbase[1], aqbase[2], aqbase[3]);
                o3_base = computeO3(aqbase[4], aqbase[5]);

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

        private void InitBlankSR()
        {
            for (int i = 1; i < 5; i++)
            {
                SR_dp[i - 1] = Matrix<double>.Build.Dense(3108, 3108);
                SR_NOx[i - 1] = Matrix<double>.Build.Dense(3108, 3108);
                SR_SO4[i - 1] = Matrix<double>.Build.Dense(3108, 3108);
                SR_O3N[i - 1] = Matrix<double>.Build.Dense(3108, 3108);
                SR_O3V[i - 1] = Matrix<double>.Build.Dense(3108, 3108);
                //SR_nh3[i - 1] = Matrix<double>.Build.Dense(3108, 3108);
            }
        }

        private void SaveSR2mtx(string path)
        {
            for (int i = 1; i < 5; i++)
            {
                MatrixMarketWriter.WriteMatrix(path + "matrix_dp_" + i.ToString() + ".mtx", SR_dp[i - 1], Compression.GZip);
                MatrixMarketWriter.WriteMatrix(path + "matrix_NOx_" + i.ToString() + ".mtx", SR_NOx[i - 1], Compression.GZip);
                MatrixMarketWriter.WriteMatrix(path + "matrix_O3N_" + i.ToString() + ".mtx", SR_O3N[i - 1], Compression.GZip);
                MatrixMarketWriter.WriteMatrix(path + "matrix_O3V_" + i.ToString() + ".mtx", SR_O3V[i - 1], Compression.GZip);
                MatrixMarketWriter.WriteMatrix(path + "matrix_so2_" + i.ToString() + ".mtx", SR_SO4[i - 1], Compression.GZip);
                //MatrixMarketWriter.WriteMatrix(path + "matrix_nh3_" + i.ToString() + ".mtx", SR_nh3[i - 1], Compression.GZip);
            }
        }

        private void SaveSR2mtx_nocomp(string path)
        {
            for (int i = 1; i < 5; i++)
            {
                MatrixMarketWriter.WriteMatrix(path + "matrix_dp_" + i.ToString() + ".mtx_nocomp", SR_dp[i - 1]);
                MatrixMarketWriter.WriteMatrix(path + "matrix_NOx_" + i.ToString() + ".mtx_nocomp", SR_NOx[i - 1]);
                MatrixMarketWriter.WriteMatrix(path + "matrix_so2_" + i.ToString() + ".mtx_nocomp", SR_SO4[i - 1]);
                //MatrixMarketWriter.WriteMatrix(path + "matrix_nh3_" + i.ToString() + ".mtx_nocomp", SR_nh3[i - 1]);
                MatrixMarketWriter.WriteMatrix(path + "matrix_O3N" + i.ToString() + ".mtx_nocomp", SR_O3N[i - 1]);
                MatrixMarketWriter.WriteMatrix(path + "matrix_O3V" + i.ToString() + ".mtx_nocomp", SR_O3V[i - 1]);
            }
        }

        private void LoadSRfrommtx(string path)
        {
            for (int i = 1; i < 5; i++)
            {
                SR_dp[i - 1] = MatrixMarketReader.ReadMatrix<double>(path + "matrix_dp_" + i.ToString() + ".mtx", Compression.GZip);
                SR_NOx[i - 1] = MatrixMarketReader.ReadMatrix<double>(path + "matrix_NOx_" + i.ToString() + ".mtx", Compression.GZip);
                SR_SO4[i - 1] = MatrixMarketReader.ReadMatrix<double>(path + "matrix_so2_" + i.ToString() + ".mtx", Compression.GZip);
                //SR_nh3[i - 1] = MatrixMarketReader.ReadMatrix<double>(path + "matrix_nh3_" + i.ToString() + ".mtx", Compression.GZip);

                SR_O3N[i - 1] = MatrixMarketReader.ReadMatrix<double>(path + "matrix_O3N_" + i.ToString() + ".mtx", Compression.GZip);
                SR_O3V[i - 1] = MatrixMarketReader.ReadMatrix<double>(path + "matrix_O3V_" + i.ToString() + ".mtx", Compression.GZip);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        private async Task LoadS3SRfrommtx()
        {
            try
            {
                // Check whether the object exists using statObject().
                // If the object is not found, statObject() throws an exception,
                // else it means that the object exists.
                // Execution is successful.
                for (int i = 1; i < 5; i++)
                {
                    await minioClient.StatObjectAsync(this.s3Config.bucket, "matrix_dp_" + i.ToString() + ".mtx_nocomp");
                    await minioClient.StatObjectAsync(this.s3Config.bucket, "matrix_NOx_" + i.ToString() + ".mtx_nocomp");
                    await minioClient.StatObjectAsync(this.s3Config.bucket, "matrix_so2_" + i.ToString() + ".mtx_nocomp");
                    //await minioClient.StatObjectAsync(this.s3Config.bucket, "matrix_nh3_" + i.ToString() + ".mtx_nocomp");
                    await minioClient.StatObjectAsync(this.s3Config.bucket, "matrix_O3N_" + i.ToString() + ".mtx_nocomp");
                    await minioClient.StatObjectAsync(this.s3Config.bucket, "matrix_O3V_" + i.ToString() + ".mtx_nocomp");

                    await minioClient.GetObjectAsync(this.s3Config.bucket, "matrix_dp_" + i.ToString() + ".mtx_nocomp",
                                                     (stream) =>
                                                     {
                                                         SR_dp[i - 1] = MatrixMarketReader.ReadMatrix<double>(stream);
                                                     });
                    await minioClient.GetObjectAsync(this.s3Config.bucket, "matrix_NOx_" + i.ToString() + ".mtx_nocomp",
                                                     (stream) =>
                                                     {
                                                         SR_NOx[i - 1] = MatrixMarketReader.ReadMatrix<double>(stream);
                                                     });
                    await minioClient.GetObjectAsync(this.s3Config.bucket, "matrix_so2_" + i.ToString() + ".mtx_nocomp",
                                                     (stream) =>
                                                     {
                                                         SR_SO4[i - 1] = MatrixMarketReader.ReadMatrix<double>(stream);
                                                     });

                    await minioClient.GetObjectAsync(this.s3Config.bucket, "matrix_O3N_" + i.ToString() + ".mtx_nocomp",
                                                     (stream) =>
                                                     {
                                                         SR_O3N[i - 1] = MatrixMarketReader.ReadMatrix<double>(stream);
                                                     });
                    await minioClient.GetObjectAsync(this.s3Config.bucket, "matrix_O3V_" + i.ToString() + ".mtx_nocomp",
                                                     (stream) =>
                                                     {
                                                         SR_O3V[i - 1] = MatrixMarketReader.ReadMatrix<double>(stream);
                                                     });
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
            }
            catch (MinioException e)
            {
                statuslog.Append("Error occurred: " + e);
            }
        }

        private int LoadSR(string path)
        {
            int recno = 1;
            try
            {

                var zip = new ZipInputStream(File.OpenRead(path + "sr_matrix_" + modelConfig.srdatayear + ".zip"));
                var filestream = new FileStream(path + "sr_matrix_" + modelConfig.srdatayear + ".zip", FileMode.Open, FileAccess.Read);
                ZipFile zipfile = new ZipFile(filestream);
                ZipEntry item;

                while ((item = zip.GetNextEntry()) != null)
                {
                    using (TextReader fileReader = new StreamReader(zipfile.GetInputStream(item)))
                    {
                        CsvReader csv = new CsvReader(fileReader, System.Globalization.CultureInfo.CurrentCulture);
                        foreach (srrecord sr_record in csv.GetRecords<srrecord>())
                        {
                            if (sr_record.typeindx <= 4 && sr_record.destindx <= 3108 && sr_record.sourceindx <= 3108)
                            {
                                var index2use = sr_record.typeindx - 1;
                                SR_dp[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.c_PM25.GetValueOrDefault(0);
                                SR_NOx[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.c_NO3.GetValueOrDefault(0);
                                SR_SO4[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.c_SO4.GetValueOrDefault(0);
                                SR_O3N[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.c_O3N.GetValueOrDefault(0);
                                SR_O3V[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.c_O3V.GetValueOrDefault(0);
                                //SR_nh3[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.tx_nh3.GetValueOrDefault(0);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                statuslog.Append("Error occurred: " + e);
            }
            return recno;
        }

        private async Task LoadS3SR()
        {
            try
            {
                // Check whether the object exists using statObject().
                // If the object is not found, statObject() throws an exception,
                // else it means that the object exists.
                // Execution is successful.
                await minioClient.StatObjectAsync(this.s3Config.bucket, "sr_matrix.csv");

                await minioClient.GetObjectAsync(this.s3Config.bucket, "sr_matrix.csv",
                                                 (stream) =>
                                                 {
                                                     using (TextReader textReader = new StreamReader(stream))
                                                     {
                                                         CsvReader csv = new CsvReader(textReader, System.Globalization.CultureInfo.CurrentCulture);
                                                         foreach (srrecord sr_record in csv.GetRecords<srrecord>())
                                                         {
                                                             var index2use = sr_record.typeindx - 1;
                                                             SR_dp[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.c_PM25.GetValueOrDefault(0);
                                                             SR_NOx[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.c_NO3.GetValueOrDefault(0);
                                                             SR_SO4[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.c_SO4.GetValueOrDefault(0);
                                                             SR_O3N[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.c_O3N.GetValueOrDefault(0);
                                                             SR_O3V[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.c_O3V.GetValueOrDefault(0);

                                                             //SR_nh3[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.tx_nh3.GetValueOrDefault(0);
                                                         }
                                                     }

                                                 });
            }
            catch (MinioException e)
            {
                statuslog.Append("Error occurred: " + e);
            }
        }

        private async Task LoadS3SRZIPPED()
        {
            try
            {
                // Check whether the object exists using statObject().
                // If the object is not found, statObject() throws an exception,
                // else it means that the object exists.
                // Execution is successful.
                await minioClient.StatObjectAsync(this.s3Config.bucket, "sr_matrix_" + modelConfig.srdatayear + ".zip");

                await minioClient.GetObjectAsync(this.s3Config.bucket, "sr_matrix_" + modelConfig.srdatayear + ".zip",
                                                 (stream) =>
                                                 {
                                                     var zip = new ZipInputStream(stream);
                                                     ZipFile zipfile = new ZipFile(stream);
                                                     ZipEntry item;

                                                     while ((item = zip.GetNextEntry()) != null)
                                                     {
                                                         using (TextReader fileReader = new StreamReader(zipfile.GetInputStream(item)))
                                                         {
                                                             CsvReader csv = new CsvReader(fileReader, System.Globalization.CultureInfo.CurrentCulture);
                                                             foreach (srrecord sr_record in csv.GetRecords<srrecord>())
                                                             {
                                                                 var index2use = sr_record.typeindx - 1;
                                                                 SR_dp[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.c_PM25.GetValueOrDefault(0);
                                                                 SR_NOx[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.c_NO3.GetValueOrDefault(0);
                                                                 SR_SO4[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.c_SO4.GetValueOrDefault(0);
                                                                 SR_O3N[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.c_O3N.GetValueOrDefault(0);
                                                                 SR_O3V[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.c_O3V.GetValueOrDefault(0);


                                                                 //SR_nh3[index2use][sr_record.destindx - 1, sr_record.sourceindx - 1] = sr_record.tx_nh3.GetValueOrDefault(0);
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


        private int LoadEmissions(string path)
        {
            int recno;


            EmissionsInventory.Clear();
            recno = 1;
            using (TextReader fileReader = File.OpenText(path + "emissions_inventory_" + modelConfig.emissionsdatayear + ".csv"))
            {
                CsvReader csv = new CsvReader(fileReader, System.Globalization.CultureInfo.CurrentCulture);
                foreach (EmissionsRecord record in csv.GetRecords<EmissionsRecord>())
                {
                    if (record.NOx > 0.0 || record.NH3 > 0.0 || record.SOA > 0.0 || record.SO2 > 0.0 || record.PM25 > 0.0 || record.VOC > 0.0)
                    {
                        EmissionsInventory.Rows.Add(new object[] { recno, record.typeindx, record.sourceindx, record.stid, record.cyid, record.TIER1, record.TIER2, record.TIER3, record.NOx.GetValueOrDefault(0), record.SO2.GetValueOrDefault(0), record.NH3.GetValueOrDefault(0), ComputeSOAfromVOC(record.TIER1 + "|" + record.TIER2 + "|" + record.TIER3, record.VOC.GetValueOrDefault(0)), record.PM25.GetValueOrDefault(0), record.VOC.GetValueOrDefault(0) });
                        recno++;
                    }
                }
            }

            return recno;
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
                                                             VOC2SOA.Add(record.TIER1 + "|" + record.TIER2 + "|" + record.TIER3, record.FACTOR);
                                                         }
                                                     }

                                                 });
            }
            catch (MinioException e)
            {
                statuslog.Append("Error occurred: " + e);
            }
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


        private int LoadDictionary_Tier(string path)
        {
            int recno = 1;

            // Creates a TextInfo based on the "en-US" culture.
            TextInfo myTI = new CultureInfo("en-US", false).TextInfo;

            using (TextReader fileReader = File.OpenText(path + "sys_tiers.csv"))
            {
                CsvReader csv = new CsvReader(fileReader, System.Globalization.CultureInfo.CurrentCulture);
                foreach (Cobra_Dict_Tier record in csv.GetRecords<Cobra_Dict_Tier>())
                {
                    record.TIER1NAME = record.TIER1NAME;
                    record.TIER2NAME = record.TIER2NAME;
                    record.TIER3NAME = record.TIER3NAME;
                    dict_tier.Add(record);
                    recno++;
                }
            }

            return recno;
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

        private int LoadIncidence(string path)
        {
            int recno = 1;
            using (TextReader fileReader = File.OpenText(path + "incidence_inventory_" + modelConfig.incidencedatayear + ".csv"))
            {
                CsvReader csv = new CsvReader(fileReader, System.Globalization.CultureInfo.CurrentCulture);
                foreach (Cobra_Incidence record in csv.GetRecords<Cobra_Incidence>())
                {
                    Incidence.Add(record);
                    recno++;
                }
            }

            return recno;
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

        private int LoadPop(string path)
        {
            int recno = 1;
            using (TextReader fileReader = File.OpenText(path + "pop_inventory_" + modelConfig.populationdatayear + ".csv"))
            {
                CsvReader csv = new CsvReader(fileReader, System.Globalization.CultureInfo.CurrentCulture);
                foreach (Cobra_POP record in csv.GetRecords<Cobra_POP>())
                {
                    Populations.Add(record);
                    recno++;
                }
            }

            return recno;
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
                                                             if (record.DestinationID <= 3108)
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
                summarizedemissionsData.Rows.Add(new object[] { recno, rowentry.typeindx, rowentry.sourceindx, rowentry.stid, rowentry.cyid, 0, 0, 0, rowentry.NOx.GetValueOrDefault(0), rowentry.SO2.GetValueOrDefault(0), rowentry.NH3.GetValueOrDefault(0), rowentry.SOA.GetValueOrDefault(0), rowentry.PM25.GetValueOrDefault(0), rowentry.VOC.GetValueOrDefault(0) });
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

        private Vector<double>[] Vectorize(DataTable emissions)
        {
            Vector<double>[] Vctr_pm_partial = new Vector<double>[4];
            Vector<double>[] Vctr_NOx_partial = new Vector<double>[4];
            Vector<double>[] Vctr_so2_partial = new Vector<double>[4];
            //Vector<double>[] Vctr_nh3_partial = new Vector<double>[4];
            Vector<double>[] Vctr_voc_partial = new Vector<double>[4];
            Vector<double>[] Vctr_soa_partial = new Vector<double>[4];
            Vector<double>[] Vctr_O3N_partial = new Vector<double>[4];

            //populated from summarizedEmissions
            Vector<double>[] Vctr_pm = new Vector<double>[4];
            Vector<double>[] Vctr_NOx = new Vector<double>[4];
            Vector<double>[] Vctr_so2 = new Vector<double>[4];
            //Vector<double>[] Vctr_nh3 = new Vector<double>[4];
            Vector<double>[] Vctr_voc = new Vector<double>[4];
            Vector<double>[] Vctr_soa = new Vector<double>[4];

            for (int i = 1; i < 5; i++)
            {
                //populated from summarizedEmissions
                Vctr_pm[i - 1] = Vector<double>.Build.Dense(3108, 0);
                Vctr_NOx[i - 1] = Vector<double>.Build.Dense(3108, 0);
                Vctr_so2[i - 1] = Vector<double>.Build.Dense(3108, 0);
                //Vctr_nh3[i - 1] = Vector<double>.Build.Dense(3108, 0);
                Vctr_voc[i - 1] = Vector<double>.Build.Dense(3108, 0);
                Vctr_soa[i - 1] = Vector<double>.Build.Dense(3108, 0);
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
                    Vctr_soa[typeindex2use][sourceindex2use] = row.Field<double?>("SOA").GetValueOrDefault(0);
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
                Vctr_soa_partial[i - 1] = SR_dp[i - 1].Multiply(Vctr_soa[i - 1]) * 28778; //transfers like pm
                Vctr_O3N_partial[i - 1] = SR_O3N[i - 1].Multiply(Vctr_NOx[i - 1]);   //voc short range

            }

            Vctr_pm_partial[0] = Vctr_pm_partial[0] + Vctr_pm_partial[1] + Vctr_pm_partial[2] + Vctr_pm_partial[3];
            Vctr_NOx_partial[0] = Vctr_NOx_partial[0] + Vctr_NOx_partial[1] + Vctr_NOx_partial[2] + Vctr_NOx_partial[3];
            Vctr_so2_partial[0] = Vctr_so2_partial[0] + Vctr_so2_partial[1] + Vctr_so2_partial[2] + Vctr_so2_partial[3];
            //Vctr_nh3_partial[0] = Vctr_nh3_partial[0] + Vctr_nh3_partial[1] + Vctr_nh3_partial[2] + Vctr_nh3_partial[3];
            Vctr_voc_partial[0] = Vctr_voc_partial[0] + Vctr_voc_partial[1] + Vctr_voc_partial[2] + Vctr_voc_partial[3];
            Vctr_soa_partial[0] = Vctr_soa_partial[0] + Vctr_soa_partial[1] + Vctr_soa_partial[2] + Vctr_soa_partial[3];
            Vctr_O3N_partial[0] = Vctr_O3N_partial[0] + Vctr_O3N_partial[1] + Vctr_O3N_partial[2] + Vctr_O3N_partial[3];


            return new Vector<double>[6] { Vctr_pm_partial[0], Vctr_NOx_partial[0], Vctr_soa_partial[0], Vctr_so2_partial[0], /*Vctr_nh3_partial[0],*/ Vctr_voc_partial[0], Vctr_O3N_partial[0] };
        }

        public List<Cobra_ResultDetail> GetResults(double discountrate)
        {
            ComputeDeltaPM(discountrate);
            return currentscenario.Impacts;
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


        public Vector<double> computePM(Vector<double> value_PM25, Vector<double> value_NO3, Vector<double> value_SOA, /*Vector<double> value_NH4,*/ Vector<double> value_SO4)
        {
            Vector<double> result_pm = Vector<double>.Build.Dense(3108, 0);

            for (int i = 0; i < 3108; i++)
            {
                result_pm[i] = computePM(value_PM25[i], value_NO3[i], value_SOA[i], /*value_NH4[i],*/ value_SO4[i], Adjustment[i]);
            }

            return result_pm;
        }

        public double computePM(double value_PM25, double value_NO3, double value_SOA, /*double value_NH4,*/ double value_SO4, double adjustment = 1.0)
        {
            double result_pm = 0;
            //no longer need chemistry now that it is baked in with the SR Matrix
            /*double NO3 = 62.0049;
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
            result_pm = adjustment * (Amm_Sulfate + Amm_Bisulfate + SO4 + Amm_Nitrate + Direct_PM25 + SOA);*/
            result_pm = value_PM25 + value_NO3 + value_SO4;

            return result_pm;
        }


        public bool ComputeDeltaPM(double discountrate)
        {
            currentscenario.EmissionsData.AcceptChanges();

            DataTable controlemissions = SummarizeEmissionsbyType(currentscenario.EmissionsData);

            var aqcontrol = Vectorize(controlemissions);

            //           0   1    2    3    4    5    
            //aqcontrol looks like:   [PM, NOx, SOA, SO2, VOC, O3N ];
            Vector<double> pm_control = computePM(aqcontrol[0], aqcontrol[1], aqcontrol[2], aqcontrol[3]);
            Vector<double> o3_control = computeO3(aqcontrol[4], aqcontrol[5]);
            var pm_delta = this.pm_base - pm_control;
            var o3_delta = this.o3_base - o3_control;


            List<Cobra_Destination> Destinations = new List<Cobra_Destination>();

            //populate
            for (int i = 0; i < 3108; i++)
            {
                Cobra_Destination dest = new Cobra_Destination();
                dest.destindx = i + 1;
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
                dest.BASE_FINAL_PM = pm_base[i];
                dest.CTRL_FINAL_PM = pm_control[i];
                dest.DELTA_FINAL_PM = pm_delta[i];
                dest.BASE_FINAL_O3 = o3_base[i];
                dest.CTRL_FINAL_O3 = o3_control[i];
                dest.DELTA_FINAL_O3 = o3_delta[i];
                Destinations.Add(dest);
            }

            //compute part 2
            Stopwatch stopwatch = new Stopwatch();
            // Start timing
            stopwatch.Start();
            Console.WriteLine("******* STARTING COMPUTE IMPACTS");
            currentscenario.Impacts = ComputeImpacts(Destinations, discountrate);
            stopwatch.Stop();
            Console.WriteLine("Time taken to COMPUTE IMPACTS: {0}", stopwatch.Elapsed);

            currentscenario.isDirty = false;

            return true;
        }

        private double crfunc(string rawfunction, string compfunction, double Incidence, double Beta, double DELTAQ, double DELTAO, double POP, double A, double B, double C)
        {
            switch (compfunction)
            {
                case "(1-(1/((1-INCIDENCE)*EXP(BETA*DELTAQ)+INCIDENCE)))*INCIDENCE*POP":
                    return (1 - (1 / ((1 - Incidence) * Math.Exp(Beta * DELTAQ) + Incidence))) * Incidence * POP;
                case "(1-(1/((1-INCIDENCE)*EXP(BETA*DELTAQ)+INCIDENCE)))*INCIDENCE*A*POP": //this one from CR function is probablyt incorrect
                    return (1 - (1 / ((1 - Incidence) * Math.Exp(Beta * DELTAQ) + Incidence))) * Incidence * A * POP;
                case "(1-(1/((1-INCIDENCE)*EXP(BETA*DELTAQ)+INCIDENCE)))*INCIDENCE*POP*A": //this one from CR function is probablyt incorrect
                    return (1 - (1 / ((1 - Incidence) * Math.Exp(Beta * DELTAQ) + Incidence))) * Incidence * A * POP;
                case "(1-(1/((1-INCIDENCE*A)*EXP(BETA*DELTAQ)+INCIDENCE*A)))*INCIDENCE*A*POP": //this one from CR function is probablyt incorrect
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
                case "(1-(1/EXP(BETA*DELTAQ)))*A*POP*INCIDENCE":
                    return (1 - (1 / Math.Exp(Beta * DELTAQ))) * A * Incidence * POP;
                case "(1-(1/EXP(BETA*DELTAQ)))*INCIDENCE*POP*(1-A)":
                    return (1 - (1 / Math.Exp(Beta * DELTAQ))) * Incidence * POP * (1 - A);
                case "(1-(1/((1-INCIDENCE)*EXP(BETA*DeltaQ)+INCIDENCE)))*INCIDENCE*POP":
                    return (1 - (1 / ((1 - Incidence) * Math.Exp(Beta * DELTAQ) + Incidence))) * Incidence * POP;
                case "(1-(1/((1-INCIDENCE)*EXP(BETA*A*DeltaQ)+INCIDENCE)))*INCIDENCE*POP":
                    return (1 - (1 / ((1 - Incidence) * Math.Exp(Beta * A * DELTAQ) + Incidence))) * Incidence * POP;
                //DELTAO PARSING
                case "(1-(1/EXP(BETA*DELTAO)))*INCIDENCE*POP":
                    return (1 - (1 / Math.Exp(Beta * DELTAO))) * Incidence * POP;
                case "(1-(1/((1-A)*EXP(BETA*DELTAO)+A)))*A*POP*INCIDENCE":
                    return (1 - (1 / ((1 - A) * Math.Exp(Beta * DELTAO) + A))) * A * POP * Incidence;
                case "(1-(1/((1-INCIDENCE)*EXP(BETA*A*DeltaO)+INCIDENCE)))*INCIDENCE*POP":
                    return (1 - (1 / ((1 - Incidence) * Math.Exp(Beta * A * DELTAO) + Incidence))) * Incidence * POP;
                case "(1-(1/EXP(BETA*DELTAO)))*INCIDENCE*A*POP*(1-A)":
                    return (1 - (1 / Math.Exp(Beta * DELTAO))) * Incidence * POP * (1 - A);
                case "(1-(1/((1-INCIDENCE)*EXP(BETA*DeltaO)+INCIDENCE)))*INCIDENCE*POP":
                    return (1 - (1 / ((1 - Incidence) * Math.Exp(Beta * DELTAO) + Incidence))) * Incidence * POP;
                case "(1-(1/EXP(BETA*DELTAO)))*INCIDENCE*POP*A*B":
                    return (1 - (1 / Math.Exp(Beta * DELTAO))) * Incidence * POP * A * B;
                default:
                    Expression e = new Expression(rawfunction);
                    e.Parameters["A"] = A;
                    e.Parameters["B"] = B;
                    e.Parameters["C"] = C;
                    e.Parameters["Beta"] = Beta;
                    e.Parameters["DELTAQ"] = DELTAQ;
                    e.Parameters["DELTAO"] = DELTAO;
                    e.Parameters["Incidence"] = Incidence;
                    e.Parameters["POP"] = POP;
                    return (double)e.Evaluate();
            }



        }

        private double perannumvalue(int year, double weight, double factor)
        {
            return weight / Math.Pow(1 + factor, year);
        }

        private double adjustmentfactorfromdiscountrate(double factor)
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

        public List<Cobra_ResultDetail> ComputeImpacts(List<Cobra_Destination> Destinations, double valat)
        {
            statuslog.Clear();
            statuslog.AppendLine("crfunc.FunctionID.ToString(),age.ToString(),POP.ToString(),Incidence.ToString(),rawresult.ToString(),result.ToString()");

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
                else if (crfunc.Seasonal_Metric.ToUpper() == "OZONE")
                {
                    metric_adjustment = 152;
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
                    double DELTAO = destination.DELTA_FINAL_O3.GetValueOrDefault(0);
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

                        //Stopwatch stopwatch = new Stopwatch();
                        // Start timing
                        //stopwatch.Start();
                        double rawresult = this.crfunc(function, cleanfunction, Incidence, Beta, DELTAQ, DELTAO, POP, A, B, C);
                        //stopwatch.Stop();
                        //Console.WriteLine("Time taken for crFUNC evaluation: {0}", stopwatch.Elapsed);
                        double result = rawresult * poolingweight * metric_adjustment;
                        if ((destination.destindx == 1797) && (crfunc.FunctionID == 12 || crfunc.FunctionID == 13 || crfunc.FunctionID == 31))
                        {
                            statuslog.AppendLine(crfunc.FunctionID.ToString() + "," + age.ToString() + "," + POP.ToString() + "," + Incidence.ToString() + "," + rawresult.ToString() + "," + result.ToString());
                        }


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
                else if (valuefunc.Seasonal_Metric.ToUpper() == "OZONE")
                {
                    metric_adjustment = 152;
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
                    double DELTAO = destination.DELTA_FINAL_O3.GetValueOrDefault(0);
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

                        double numCases = this.crfunc(function, cleanfunction, Incidence, Beta, DELTAQ, DELTAO, POP, A, B, C) * poolingweight * metric_adjustment;
                        double valueCases = 0;

                        if (valat == 0 || valuefunc.ApplyDiscount == "NO") //the default, just go with original
                        {
                            valueCases = numCases * valuefunc.Value.GetValueOrDefault(0) * 1.1225;
                        }
                        else
                        { //apply custom discount rate
                          //we are assuming that valuefunc.Value is the UNDISCOUNTED rate so now we are going to apply the discount rate
                            double valtarget = adjustmentfactorfromdiscountrate(valat / 100);

                            valueCases = numCases * valuefunc.Value.GetValueOrDefault(0) * valtarget * 1.1225;

                            //result = result * valuefunc.ApplyDiscount.GetValueOrDefault(0) * 1.1225;
                        }

                        // check if there is an entry already to make pooling work
                        if (results_valuation.TryGetValue(destination.destindx + "|" + valuefunc.Endpoint, out result_valuation))
                        {
                            result_valuation.Value += valueCases;
                        }
                        else
                        {
                            results_valuation.Add(destination.destindx + "|" + valuefunc.Endpoint, new Result { Destinationindex = destination.destindx.GetValueOrDefault(0), Endpoint = valuefunc.Endpoint, Value = valueCases });
                        }
                        // add to national totals as well
                        if (results_valuation.TryGetValue("nation|" + valuefunc.Endpoint, out result_valuation))
                        {
                            result_valuation.Value = result_valuation.Value + valueCases;
                        }
                        else
                        {
                            results_valuation.Add("nation|" + valuefunc.Endpoint, new Result { Destinationindex = destination.destindx.GetValueOrDefault(0), Endpoint = valuefunc.Endpoint, Value = valueCases });
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
                result_record.BASE_FINAL_O3 = destination.BASE_FINAL_O3;
                result_record.CTRL_FINAL_O3 = destination.CTRL_FINAL_O3;
                result_record.DELTA_FINAL_O3 = destination.DELTA_FINAL_O3;
                var loc = dict_state.Where(d => d.SOURCEINDX == result_record.destindx).First();
                result_record.FIPS = loc.FIPS;
                result_record.STATE = loc.STNAME;
                result_record.COUNTY = loc.CYNAME;


                result_record.PM_Acute_Myocardial_Infarction_Nonfatal = results_cr[destination.destindx + "|" + "PM Acute Myocardial Infarction, Nonfatal"].Value;


                result_record.PM_HA_All_Respiratory = results_cr[destination.destindx + "|" + "PM HA, All Respiratory"].Value;
                result_record.PM_Minor_Restricted_Activity_Days = results_cr[destination.destindx + "|" + "PM Minor Restricted Activity Days"].Value;
                result_record.PM_Mortality_All_Cause__low_ = results_cr[destination.destindx + "|" + "PM Mortality, All Cause (low)"].Value;
                result_record.PM_Mortality_All_Cause__high_ = results_cr[destination.destindx + "|" + "PM Mortality, All Cause (high)"].Value;
                result_record.PM_Infant_Mortality = results_cr[destination.destindx + "|" + "PM Infant Mortality"].Value;
                result_record.PM_Work_Loss_Days = results_cr[destination.destindx + "|" + "PM Work Loss Days"].Value;
                result_record.PM_Incidence_Lung_Cancer = results_cr[destination.destindx + "|" + "PM Incidence, Lung Cancer"].Value;

                result_record.PM_Incidence_Hay_Fever_Rhinitis = results_cr[destination.destindx + "|" + "PM Incidence, Hay Fever/Rhinitis"].Value;
                result_record.PM_Incidence_Asthma = results_cr[destination.destindx + "|" + "PM Incidence, Asthma"].Value;
                result_record.PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease = results_cr[destination.destindx + "|" + "PM HA, Cardio-, Cerebro- and Peripheral Vascular Disease"].Value;
                result_record.PM_HA_Alzheimers_Disease = results_cr[destination.destindx + "|" + "PM HA, Alzheimers Disease"].Value;
                result_record.PM_HA_Parkinsons_Disease = results_cr[destination.destindx + "|" + "PM HA, Parkinsons Disease"].Value;
                result_record.PM_Incidence_Stroke = results_cr[destination.destindx + "|" + "PM Incidence, Stroke"].Value;
                result_record.PM_Incidence_Out_of_Hospital_Cardiac_Arrest = results_cr[destination.destindx + "|" + "PM Incidence, Out of Hospital Cardiac Arrest"].Value;
                result_record.PM_Asthma_Symptoms_Albuterol_use = results_cr[destination.destindx + "|" + "PM Asthma Symptoms, Albuterol use"].Value;
                result_record.PM_HA_Respiratory2 = results_cr[destination.destindx + "|" + "PM HA, Respiratory-2"].Value;
                result_record.PM_ER_visits_respiratory = results_cr[destination.destindx + "|" + "PM ER visits, respiratory"].Value;
                result_record.PM_ER_visits_All_Cardiac_Outcomes = results_cr[destination.destindx + "|" + "PM ER visits, All Cardiac Outcomes"].Value;

                result_record.O3_ER_visits_respiratory = results_cr[destination.destindx + "|" + "O3 ER visits, respiratory"].Value;
                result_record.O3_HA_All_Respiratory = results_cr[destination.destindx + "|" + "O3 HA, All Respiratory"].Value;
                result_record.O3_Incidence_Hay_Fever_Rhinitis = results_cr[destination.destindx + "|" + "O3 Incidence, Hay Fever/Rhinitis"].Value;
                result_record.O3_Incidence_Asthma = results_cr[destination.destindx + "|" + "O3 Incidence, Asthma"].Value;
                result_record.O3_Asthma_Symptoms_Chest_Tightness = results_cr[destination.destindx + "|" + "O3 Asthma Symptoms, Chest Tightness"].Value;
                result_record.O3_Asthma_Symptoms_Cough = results_cr[destination.destindx + "|" + "O3 Asthma Symptoms, Cough"].Value;
                result_record.O3_Asthma_Symptoms_Shortness_of_Breath = results_cr[destination.destindx + "|" + "O3 Asthma Symptoms, Shortness of Breath"].Value;
                result_record.O3_Asthma_Symptoms_Wheeze = results_cr[destination.destindx + "|" + "O3 Asthma Symptoms, Wheeze"].Value;
                result_record.O3_ER_Visits_Asthma = results_cr[destination.destindx + "|" + "O3 Emergency Room Visits, Asthma"].Value;
                result_record.O3_School_Loss_Days = results_cr[destination.destindx + "|" + "O3 School Loss Days, All Cause"].Value;
                result_record.O3_Mortality_Longterm_exposure = results_cr[destination.destindx + "|" + "O3 Mortality, Long-term exposure"].Value;
                result_record.O3_Mortality_Shortterm_exposure = results_cr[destination.destindx + "|" + "O3 Mortality, Short-term exposure"].Value;


                result_record.C__PM_Acute_Myocardial_Infarction_Nonfatal = results_valuation[destination.destindx + "|" + "PM Acute Myocardial Infarction, Nonfatal"].Value;

                result_record.C__PM_Resp_Hosp_Adm = results_valuation[destination.destindx + "|" + "PM HA, All Respiratory"].Value;
                result_record.C__PM_Minor_Restricted_Activity_Days = results_valuation[destination.destindx + "|" + "PM Minor Restricted Activity Days"].Value;
                result_record.C__PM_Mortality_All_Cause__low_ = results_valuation[destination.destindx + "|" + "PM Mortality, All Cause (low)"].Value;
                result_record.C__PM_Mortality_All_Cause__high_ = results_valuation[destination.destindx + "|" + "PM Mortality, All Cause (high)"].Value;
                result_record.C__PM_Infant_Mortality = results_valuation[destination.destindx + "|" + "PM Infant Mortality"].Value;

                result_record.C__PM_Work_Loss_Days = results_valuation[destination.destindx + "|" + "PM Work Loss Days"].Value;
                result_record.C__PM_Incidence_Lung_Cancer = results_valuation[destination.destindx + "|" + "PM Incidence, Lung Cancer"].Value;

                result_record.C__PM_Incidence_Hay_Fever_Rhinitis = results_valuation[destination.destindx + "|" + "PM Incidence, Hay Fever/Rhinitis"].Value;
                result_record.C__PM_Incidence_Asthma = results_valuation[destination.destindx + "|" + "PM Incidence, Asthma"].Value;
                result_record.C__PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease = results_valuation[destination.destindx + "|" + "PM HA, Cardio-, Cerebro- and Peripheral Vascular Disease"].Value;
                result_record.C__PM_HA_Alzheimers_Disease = results_valuation[destination.destindx + "|" + "PM HA, Alzheimers Disease"].Value;
                result_record.C__PM_HA_Parkinsons_Disease = results_valuation[destination.destindx + "|" + "PM HA, Parkinsons Disease"].Value;
                result_record.C__PM_Incidence_Stroke = results_valuation[destination.destindx + "|" + "PM Incidence, Stroke"].Value;
                result_record.C__PM_Incidence_Out_of_Hospital_Cardiac_Arrest = results_valuation[destination.destindx + "|" + "PM Incidence, Out of Hospital Cardiac Arrest"].Value;
                result_record.C__PM_Asthma_Symptoms_Albuterol_use = results_valuation[destination.destindx + "|" + "PM Asthma Symptoms, Albuterol use"].Value;
                result_record.C__PM_HA_Respiratory2 = results_valuation[destination.destindx + "|" + "PM HA, Respiratory-2"].Value;
                result_record.C__PM_ER_visits_respiratory = results_valuation[destination.destindx + "|" + "PM ER visits, respiratory"].Value;
                result_record.C__PM_ER_visits_All_Cardiac_Outcomes = results_valuation[destination.destindx + "|" + "PM ER visits, All Cardiac Outcomes"].Value;

                result_record.C__O3_ER_visits_respiratory = results_valuation[destination.destindx + "|" + "O3 ER visits, respiratory"].Value;
                result_record.C__O3_HA_All_Respiratory = results_valuation[destination.destindx + "|" + "O3 HA, All Respiratory"].Value;
                result_record.C__O3_Incidence_Hay_Fever_Rhinitis = results_valuation[destination.destindx + "|" + "O3 Incidence, Hay Fever/Rhinitis"].Value;
                result_record.C__O3_Incidence_Asthma = results_valuation[destination.destindx + "|" + "O3 Incidence, Asthma"].Value;
                result_record.C__O3_Asthma_Symptoms_Chest_Tightness = results_valuation[destination.destindx + "|" + "O3 Asthma Symptoms, Chest Tightness"].Value;
                result_record.C__O3_Asthma_Symptoms_Cough = results_valuation[destination.destindx + "|" + "O3 Asthma Symptoms, Cough"].Value;
                result_record.C__O3_Asthma_Symptoms_Shortness_of_Breath = results_valuation[destination.destindx + "|" + "O3 Asthma Symptoms, Shortness of Breath"].Value;
                result_record.C__O3_Asthma_Symptoms_Wheeze = results_valuation[destination.destindx + "|" + "O3 Asthma Symptoms, Wheeze"].Value;
                result_record.C__O3_ER_Visits_Asthma = results_valuation[destination.destindx + "|" + "O3 Emergency Room Visits, Asthma"].Value;
                result_record.C__O3_School_Loss_Days = results_valuation[destination.destindx + "|" + "O3 School Loss Days, All Cause"].Value;
                result_record.C__O3_Mortality_Longterm_exposure = results_valuation[destination.destindx + "|" + "O3 Mortality, Long-term exposure"].Value;
                result_record.C__O3_Mortality_Shortterm_exposure = results_valuation[destination.destindx + "|" + "O3 Mortality, Short-term exposure"].Value;

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

                results.Add(result_record);
            }
            return results;
        }

        public List<Cobra_ResultDetail> ComputeGenericImpacts(double delta_pm, double base_pm, double control_pm, double delta_o3, double base_o3, double control_o3, Cobra_POP population, Cobra_Incidence[] incidence, double discountRate)
        {
            List<Cobra_Destination> Destinations = new List<Cobra_Destination>();
            Cobra_Destination dest = new Cobra_Destination();
            dest.destindx = 1797;
            Destinations.Add(dest);

            Dictionary<string, Result> results_cr = new Dictionary<string, Result>();
            Dictionary<string, Result> results_valuation = new Dictionary<string, Result>();


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

            /*
                    Dictionary<long?, Cobra_POP> dict_pop = new Dictionary<long?, Cobra_POP>(this.Populations.Count());
                    dict_pop.Add(1797, population);

                    Dictionary<string, Cobra_Incidence> dict_incidence = new Dictionary<string, Cobra_Incidence>();
                    foreach (var item in incidence)
                    {
                        dict_incidence.Add("1797" + "|" + item.Endpoint, item);
                    }
            */

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
                else if (crfunc.Seasonal_Metric.ToUpper() == "OZONE")
                {
                    metric_adjustment = 152;
                }

                foreach (var destination in Destinations)
                {
                    //fixed params
                    Expression e = new Expression(function);
                    double A = crfunc.A.GetValueOrDefault(0);
                    double B = crfunc.B.GetValueOrDefault(0);
                    double C = crfunc.C.GetValueOrDefault(0);
                    double Beta = crfunc.Beta.GetValueOrDefault(0);
                    double DELTAQ = delta_pm;
                    double DELTAO = delta_o3;
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

                        double result = this.crfunc(function, cleanfunction, Incidence, Beta, DELTAQ, DELTAO, POP, A, B, C) * poolingweight * metric_adjustment;

                        // check if there is an entry already to make pooling work
                        if (results_cr.TryGetValue("99999" + "|" + crfunc.Endpoint, out result_cr))
                        {
                            result_cr.Value = result_cr.Value + result;
                        }
                        else
                        {
                            results_cr.Add("99999" + "|" + crfunc.Endpoint, new Result { Destinationindex = destination.destindx.GetValueOrDefault(0), Endpoint = crfunc.Endpoint, Value = result });
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
                else if (valuefunc.Seasonal_Metric.ToUpper() == "OZONE")
                {
                    metric_adjustment = 152;
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
                    double DELTAO = destination.DELTA_FINAL_O3.GetValueOrDefault(0);
                    double Incidence = 0;

                    //year dependent but with the twist that pop and incidence are containing all year data
                    Cobra_POP poprow = dict_pop[99999];
                    if (dict_incidence.TryGetValue("99999" + "|" + valuefunc.IncidenceEndpoint, out value))
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

                        double numCases = this.crfunc(function, cleanfunction, Incidence, Beta, DELTAQ, DELTAO, POP, A, B, C) * poolingweight * metric_adjustment;
                        double valueCases = 0;

                        valueCases = numCases * valuefunc.Value.GetValueOrDefault(0) * 1.1225;


                        // check if there is an entry already to make pooling work
                        if (results_valuation.TryGetValue("99999" + "|" + valuefunc.Endpoint, out result_valuation))
                        {
                            result_valuation.Value += valueCases;
                        }
                        else
                        {
                            results_valuation.Add("99999" + "|" + valuefunc.Endpoint, new Result { Destinationindex = destination.destindx.GetValueOrDefault(0), Endpoint = valuefunc.Endpoint, Value = valueCases });
                        }
                        // add to national totals as well
                        if (results_valuation.TryGetValue("nation|" + valuefunc.Endpoint, out result_valuation))
                        {
                            result_valuation.Value = result_valuation.Value + valueCases;
                        }
                        else
                        {
                            results_valuation.Add("nation|" + valuefunc.Endpoint, new Result { Destinationindex = destination.destindx.GetValueOrDefault(0), Endpoint = valuefunc.Endpoint, Value = valueCases });
                        }
                    }
                }
            }

            List<Cobra_ResultDetail> results = new List<Cobra_ResultDetail>();
            foreach (var destination in Destinations)
            {
                Cobra_ResultDetail result_record = new Cobra_ResultDetail();
                result_record.destindx = destination.destindx;
                result_record.BASE_FINAL_PM = 0;
                result_record.CTRL_FINAL_PM = 0;
                result_record.DELTA_FINAL_PM = delta_pm;
                result_record.BASE_FINAL_O3 = 0;
                result_record.CTRL_FINAL_O3 = 0;
                result_record.DELTA_FINAL_O3 = delta_o3;
                var loc = dict_state.Where(d => d.SOURCEINDX == result_record.destindx).FirstOrDefault();
                result_record.FIPS = loc.FIPS;
                result_record.STATE = loc.STNAME;
                result_record.COUNTY = loc.CYNAME;

                result_record.PM_Acute_Myocardial_Infarction_Nonfatal = results_cr[destination.destindx + "|" + "PM Acute Myocardial Infarction, Nonfatal"].Value;


                result_record.PM_HA_All_Respiratory = results_cr[destination.destindx + "|" + "PM HA, All Respiratory"].Value;
                result_record.PM_Minor_Restricted_Activity_Days = results_cr[destination.destindx + "|" + "PM Minor Restricted Activity Days"].Value;
                result_record.PM_Mortality_All_Cause__low_ = results_cr[destination.destindx + "|" + "PM Mortality, All Cause (low)"].Value;
                result_record.PM_Mortality_All_Cause__high_ = results_cr[destination.destindx + "|" + "PM Mortality, All Cause (high)"].Value;
                result_record.PM_Infant_Mortality = results_cr[destination.destindx + "|" + "PM Infant Mortality"].Value;
                result_record.PM_Work_Loss_Days = results_cr[destination.destindx + "|" + "PM Work Loss Days"].Value;
                result_record.PM_Incidence_Lung_Cancer = results_cr[destination.destindx + "|" + "PM Incidence, Lung Cancer"].Value;

                result_record.PM_Incidence_Hay_Fever_Rhinitis = results_cr[destination.destindx + "|" + "PM Incidence, Hay Fever/Rhinitis"].Value;
                result_record.PM_Incidence_Asthma = results_cr[destination.destindx + "|" + "PM Incidence, Asthma"].Value;
                result_record.PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease = results_cr[destination.destindx + "|" + "PM HA, Cardio-, Cerebro- and Peripheral Vascular Disease"].Value;
                result_record.PM_HA_Alzheimers_Disease = results_cr[destination.destindx + "|" + "PM HA, Alzheimers Disease"].Value;
                result_record.PM_HA_Parkinsons_Disease = results_cr[destination.destindx + "|" + "PM HA, Parkinsons Disease"].Value;
                result_record.PM_Incidence_Stroke = results_cr[destination.destindx + "|" + "PM Incidence, Stroke"].Value;
                result_record.PM_Incidence_Out_of_Hospital_Cardiac_Arrest = results_cr[destination.destindx + "|" + "PM Incidence, Out of Hospital Cardiac Arrest"].Value;
                result_record.PM_Asthma_Symptoms_Albuterol_use = results_cr[destination.destindx + "|" + "PM Asthma Symptoms, Albuterol use"].Value;
                result_record.PM_HA_Respiratory2 = results_cr[destination.destindx + "|" + "PM HA, Respiratory-2"].Value;
                result_record.PM_ER_visits_respiratory = results_cr[destination.destindx + "|" + "PM ER visits, respiratory"].Value;
                result_record.PM_ER_visits_All_Cardiac_Outcomes = results_cr[destination.destindx + "|" + "PM ER visits, All Cardiac Outcomes"].Value;

                result_record.O3_ER_visits_respiratory = results_cr[destination.destindx + "|" + "O3 ER visits, respiratory"].Value;
                result_record.O3_HA_All_Respiratory = results_cr[destination.destindx + "|" + "O3 HA, All Respiratory"].Value;
                result_record.O3_Incidence_Hay_Fever_Rhinitis = results_cr[destination.destindx + "|" + "O3 Incidence, Hay Fever/Rhinitis"].Value;
                result_record.O3_Incidence_Asthma = results_cr[destination.destindx + "|" + "O3 Incidence, Asthma"].Value;
                result_record.O3_Asthma_Symptoms_Chest_Tightness = results_cr[destination.destindx + "|" + "O3 Asthma Symptoms, Chest Tightness"].Value;
                result_record.O3_Asthma_Symptoms_Cough = results_cr[destination.destindx + "|" + "O3 Asthma Symptoms, Cough"].Value;
                result_record.O3_Asthma_Symptoms_Shortness_of_Breath = results_cr[destination.destindx + "|" + "O3 Asthma Symptoms, Shortness of Breath"].Value;
                result_record.O3_Asthma_Symptoms_Wheeze = results_cr[destination.destindx + "|" + "O3 Asthma Symptoms, Wheeze"].Value;
                result_record.O3_ER_Visits_Asthma = results_cr[destination.destindx + "|" + "O3 Emergency Room Visits, Asthma"].Value;
                result_record.O3_School_Loss_Days = results_cr[destination.destindx + "|" + "O3 School Loss Days, All Cause"].Value;
                result_record.O3_Mortality_Longterm_exposure = results_cr[destination.destindx + "|" + "O3 Mortality, Long-term exposure"].Value;
                result_record.O3_Mortality_Shortterm_exposure = results_cr[destination.destindx + "|" + "O3 Mortality, Short-term exposure"].Value;


                result_record.C__PM_Acute_Myocardial_Infarction_Nonfatal = results_valuation[destination.destindx + "|" + "PM Acute Myocardial Infarction, Nonfatal"].Value;


                result_record.C__PM_Resp_Hosp_Adm = results_valuation[destination.destindx + "|" + "PM HA, All Respiratory"].Value;
                result_record.C__PM_Minor_Restricted_Activity_Days = results_valuation[destination.destindx + "|" + "PM Minor Restricted Activity Days"].Value;
                result_record.C__PM_Mortality_All_Cause__low_ = results_valuation[destination.destindx + "|" + "PM Mortality, All Cause (low)"].Value;
                result_record.C__PM_Mortality_All_Cause__high_ = results_valuation[destination.destindx + "|" + "PM Mortality, All Cause (high)"].Value;
                result_record.C__PM_Infant_Mortality = results_valuation[destination.destindx + "|" + "PM Infant Mortality"].Value;

                result_record.C__PM_Work_Loss_Days = results_valuation[destination.destindx + "|" + "PM Work Loss Days"].Value;
                result_record.C__PM_Incidence_Lung_Cancer = results_valuation[destination.destindx + "|" + "PM Incidence, Lung Cancer"].Value;

                result_record.C__PM_Incidence_Hay_Fever_Rhinitis = results_valuation[destination.destindx + "|" + "PM Incidence, Hay Fever/Rhinitis"].Value;
                result_record.C__PM_Incidence_Asthma = results_valuation[destination.destindx + "|" + "PM Incidence, Asthma"].Value;
                result_record.C__PM_HA_Cardio_Cerebro_and_Peripheral_Vascular_Disease = results_valuation[destination.destindx + "|" + "PM HA, Cardio-, Cerebro- and Peripheral Vascular Disease"].Value;
                result_record.C__PM_HA_Alzheimers_Disease = results_valuation[destination.destindx + "|" + "PM HA, Alzheimers Disease"].Value;
                result_record.C__PM_HA_Parkinsons_Disease = results_valuation[destination.destindx + "|" + "PM HA, Parkinsons Disease"].Value;
                result_record.C__PM_Incidence_Stroke = results_valuation[destination.destindx + "|" + "PM Incidence, Stroke"].Value;
                result_record.C__PM_Incidence_Out_of_Hospital_Cardiac_Arrest = results_valuation[destination.destindx + "|" + "PM Incidence, Out of Hospital Cardiac Arrest"].Value;
                result_record.C__PM_Asthma_Symptoms_Albuterol_use = results_valuation[destination.destindx + "|" + "PM Asthma Symptoms, Albuterol use"].Value;
                result_record.C__PM_HA_Respiratory2 = results_valuation[destination.destindx + "|" + "PM HA, Respiratory-2"].Value;
                result_record.C__PM_ER_visits_respiratory = results_valuation[destination.destindx + "|" + "PM ER visits, respiratory"].Value;
                result_record.C__PM_ER_visits_All_Cardiac_Outcomes = results_valuation[destination.destindx + "|" + "PM ER visits, All Cardiac Outcomes"].Value;

                result_record.C__O3_ER_visits_respiratory = results_valuation[destination.destindx + "|" + "O3 ER visits, respiratory"].Value;
                result_record.C__O3_HA_All_Respiratory = results_valuation[destination.destindx + "|" + "O3 HA, All Respiratory"].Value;
                result_record.C__O3_Incidence_Hay_Fever_Rhinitis = results_valuation[destination.destindx + "|" + "O3 Incidence, Hay Fever/Rhinitis"].Value;
                result_record.C__O3_Incidence_Asthma = results_valuation[destination.destindx + "|" + "O3 Incidence, Asthma"].Value;
                result_record.C__O3_Asthma_Symptoms_Chest_Tightness = results_valuation[destination.destindx + "|" + "O3 Asthma Symptoms, Chest Tightness"].Value;
                result_record.C__O3_Asthma_Symptoms_Cough = results_valuation[destination.destindx + "|" + "O3 Asthma Symptoms, Cough"].Value;
                result_record.C__O3_Asthma_Symptoms_Shortness_of_Breath = results_valuation[destination.destindx + "|" + "O3 Asthma Symptoms, Shortness of Breath"].Value;
                result_record.C__O3_Asthma_Symptoms_Wheeze = results_valuation[destination.destindx + "|" + "O3 Asthma Symptoms, Wheeze"].Value;
                result_record.C__O3_ER_Visits_Asthma = results_valuation[destination.destindx + "|" + "O3 Emergency Room Visits, Asthma"].Value;
                result_record.C__O3_School_Loss_Days = results_valuation[destination.destindx + "|" + "O3 School Loss Days, All Cause"].Value;
                result_record.C__O3_Mortality_Longterm_exposure = results_valuation[destination.destindx + "|" + "O3 Mortality, Long-term exposure"].Value;
                result_record.C__O3_Mortality_Shortterm_exposure = results_valuation[destination.destindx + "|" + "O3 Mortality, Short-term exposure"].Value;

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

                //totalPM 
                result_record.C__Total_PM_Low_Value = (lowvals + result_record.C__PM_Mortality_All_Cause__high_.GetValueOrDefault(0));
                result_record.C__Total_PM_High_Value = (lowvals + result_record.C__PM_Mortality_All_Cause__low_.GetValueOrDefault(0));

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

                result_record.C__Total_O3_Value = (result_record.C__O3_Asthma_Symptoms_Cough.GetValueOrDefault(0)
                    + result_record.C__O3_Asthma_Symptoms_Shortness_of_Breath.GetValueOrDefault(0)
                    + result_record.C__O3_Asthma_Symptoms_Wheeze.GetValueOrDefault(0)
                    + result_record.C__O3_ER_Visits_Asthma.GetValueOrDefault(0)
                + result_record.C__O3_School_Loss_Days.GetValueOrDefault(0)
                + result_record.C__O3_Mortality_Longterm_exposure.GetValueOrDefault(0)
                + result_record.C__O3_Mortality_Shortterm_exposure.GetValueOrDefault(0)
                + result_record.C__O3_ER_visits_respiratory.GetValueOrDefault(0)
                 + result_record.C__O3_HA_All_Respiratory.GetValueOrDefault(0)
                 + result_record.C__O3_Incidence_Hay_Fever_Rhinitis.GetValueOrDefault(0)
                 + result_record.C__O3_Incidence_Asthma.GetValueOrDefault(0)
                + result_record.C__O3_Asthma_Symptoms_Chest_Tightness.GetValueOrDefault(0));


                result_record.C__Total_Health_Benefits_Low_Value = lowvals;

                //add low to high this works
                result_record.C__Total_Health_Benefits_High_Value = lowvals;

                //add the endpoints with different high/low vals (in this case only PM_mortality)
                result_record.C__Total_Health_Benefits_High_Value += result_record.C__PM_Mortality_All_Cause__high_.GetValueOrDefault(0);
                result_record.C__Total_Health_Benefits_Low_Value += result_record.C__PM_Mortality_All_Cause__low_.GetValueOrDefault(0);

                results.Add(result_record);



            }
            return results;
        }

        public List<Custom_ResultDetail> CustomComputeGenericImpacts(double delta_pm, double base_pm, double control_pm, Cobra_POP population, Cobra_Incidence[] incidence, bool valat3, Cobra_CR_Core[] CustomCRFunctions, Cobra_Valuation_Core[] CustomValuationFunctions)
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

                        double result = this.crfunc(function, cleanfunction, Incidence, Beta, DELTAQ, 0, POP, A, B, C) * poolingweight * metric_adjustment;

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

                        double result = this.crfunc(function, cleanfunction, Incidence, Beta, DELTAQ, 0, POP, A, B, C) * poolingweight * metric_adjustment;

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
                foreach (var key in results_cr.Keys)
                {

                    result_record.SetDynamicProperty(key, results_cr.GetValueOrDefault(key, new Result { Value = 0 }).Value, false);
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

        public queueSubmission GetChangeQueueSubmission()
        {
            return currentscenario.queueSubmission;
        }

        public void SetChangeQueueSubmission(queueSubmission submission)
        {
            currentscenario.queueSubmission = submission;
        }

    }

}

