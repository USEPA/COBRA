using Ceras;
using CobraCompute;
using Minio;
using Minio.DataModel;
using Minio.Exceptions;
using Newtonsoft.Json;
using ProtoBuf.Data;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using RedisConfig = CobraCompute.RedisConfig;

namespace CobraComputeAPI
{
    public class ScenarioManager_Redis
    {
        private TimeSpan redisCachingDuration = TimeSpan.FromMinutes(60);

        private MemoryStream GlobalEmissionsInventory;

        private ConnectionMultiplexer redis;
        private IDatabase db;

        private MinioClient minioClient;
        private S3Config s3Config;

        public ScenarioManager_Redis(DataTable Inventory, RedisConfig redisOptions, MinioClient _minioClient, S3Config _s3Config)
        {
            //storing the baseline emissions inventory. This will significantly increase performance
            GlobalEmissionsInventory = new MemoryStream();
            DataSerializer.Serialize(GlobalEmissionsInventory, Inventory);


            string configString = redisOptions.URI;
            var options = ConfigurationOptions.Parse(configString);

            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(options);
            db = redis.GetDatabase();

            this.minioClient = _minioClient;
            this.s3Config = _s3Config;
        }

        private async Task<MemoryStream> getS3Buffer_internal(String type, String sessionid)
        {
            MemoryStream result = new MemoryStream();
            try
            {
                String objectName = sessionid + "." + type;

                await minioClient.StatObjectAsync(s3Config.bucket, objectName);

                // Get input stream to have content of 'my-objectname' from 'my-bucketname'
                await minioClient.GetObjectAsync(s3Config.bucket, objectName,
                                                 (stream) =>
                                                 {
                                                     stream.CopyTo(result);
                                                 });
            }
            catch (MinioException e)
            {
            }

            result.Position = 0;
            return result;
        }

        public MemoryStream getBufferFromS3(String type, String token)
        {
            return getS3Buffer_internal(type, token).Result;
        }

        private async Task putS3Buffer_internal(String type, String sessionid, MemoryStream buffer)
        {
            try
            {
                String objectname = sessionid + "." + type;
                buffer.Position = 0;

                await minioClient.PutObjectAsync(s3Config.bucket, objectname, buffer, buffer.Length);

            }
            catch (MinioException e)
            {
            }
        }

        public void putBufferToS3(String type, String token, MemoryStream buffer)
        {
            putS3Buffer_internal(type, token, buffer).Wait();
        }

        public async void deleteS3Object(String type, String sessionid)
        {
            try
            {
                await minioClient.RemoveObjectAsync(s3Config.bucket, sessionid + "." + type);
            }
            catch (MinioException e)
            {
            }
        }

        public async Task<bool> probeS3Object(String type, String sessionid)
        {
            bool persistentImpacts = true;
            try
            {
                ObjectStat objectStat = await minioClient.StatObjectAsync(s3Config.bucket, sessionid + "." + type);
            }
            catch (MinioException e)
            {
                persistentImpacts = false;
            }
            return persistentImpacts;
        }


        public UserScenario retrieve(Guid token)
        {
            UserScenario scenario = new UserScenario();
            UserScenarioCore scenario_inredis = null;

            string json = db.StringGet(token.ToString());


            if (json != null && json != "")
            {
                //transfer to main object
                scenario_inredis = JsonConvert.DeserializeObject<UserScenarioCore>(json);
                scenario.Id = scenario_inredis.Id;
                scenario.createdOn = scenario_inredis.createdOn;
                scenario.isDirty = scenario_inredis.isDirty;
                scenario.isEmissionsDataDirty = scenario_inredis.isEmissionsDataDirty;
                scenario.Year = scenario_inredis.Year;
                scenario.queueSubmission = scenario_inredis.queueSubmission;

                // Deserialize DataTable
                MemoryStream buffer_emissions = getBufferFromS3("emissions", token.ToString());
                scenario.EmissionsData = DataSerializer.DeserializeDataTable(buffer_emissions);
                buffer_emissions = null;
                // Deserialize Impacts
                if (probeS3Object("impacts", token.ToString()).Result)
                {
                    var ceras = new CerasSerializer();
                    MemoryStream buffer_c = getBufferFromS3("impacts", token.ToString());
                    scenario.Impacts = ceras.Deserialize<List<Cobra_ResultDetail>>(buffer_c.ToArray());
                    buffer_c = null;
                    ceras = null;
                }
            }
            else
            {
                throw new System.ArgumentException("Guid not in REDIS.");
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            return scenario;
        }

        public void store(UserScenario value)
        {
            MemoryStream bufferEmissions = new MemoryStream();
            MemoryStream bufferImpacts = null;

            UserScenarioCore scenario_inredis = null;

            string json = db.StringGet(value.Id.ToString());
            if (json != null && json != "")
            {
                // Serialize DataTable to a buffer
                if (value.EmissionsData != null)
                {
                    value.EmissionsData.AcceptChanges();
                    DataSerializer.Serialize(bufferEmissions, value.EmissionsData);
                }
                // Serialize Impacts to a buffer
                if (value.Impacts != null)
                {
                    var ceras = new CerasSerializer();
                    bufferImpacts = new MemoryStream(ceras.Serialize(value.Impacts));

                    ceras = null;
                }
                //save, possibly conditional
                scenario_inredis = new UserScenarioCore();
                scenario_inredis.createdOn = value.createdOn;
                scenario_inredis.isDirty = value.isDirty;
                scenario_inredis.isEmissionsDataDirty = value.isEmissionsDataDirty;
                scenario_inredis.Year = value.Year;
                scenario_inredis.Id = value.Id;
                scenario_inredis.queueSubmission = value.queueSubmission;

                json = JsonConvert.SerializeObject(scenario_inredis);
                db.StringSet(scenario_inredis.Id.ToString(), json);

                putBufferToS3("emissions", value.Id.ToString(), bufferEmissions);
                if (bufferImpacts != null)
                {
                    putBufferToS3("impacts", value.Id.ToString(), bufferImpacts);
                }
            }
            else
            {
                throw new System.ArgumentException("Guid not in REDIS.");
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public Guid createUserScenario()
        {
            Guid token;

            bool go_on = false;
            int attemp = 0;
            //repeat until works
            do
            {
                attemp++;
                token = Guid.NewGuid();
                try
                {
                    go_on = !db.KeyExists(token.ToString());

                }
                catch (Exception)
                {
                    if (attemp >= 3)
                    {
                        throw new System.ArgumentException("REDIS fails key check.");
                    }
                    System.Threading.Thread.Sleep(50);
                }
            } while (!go_on);

            UserScenarioCore scenario = new UserScenarioCore()
            {
                Id = token,
                Year = 2025,
                createdOn = DateTime.Now,
                isEmissionsDataDirty = false,
            };
            scenario.isDirty = true;

            string json = JsonConvert.SerializeObject(scenario);
            db.StringSet(scenario.Id.ToString(), json);


            putBufferToS3("emissions", token.ToString(), this.GlobalEmissionsInventory);

            scenario = null;
            cleanCache();
            return token;
        }

        public Guid resetUserScenario(Guid token)
        {
            this.deleteUserScenario(token);

            UserScenarioCore scenario = new UserScenarioCore()
            {
                Id = token,
                Year = 2025,
                createdOn = DateTime.Now,
                isEmissionsDataDirty = false
            };
            scenario.isDirty = true;

            string json = JsonConvert.SerializeObject(scenario);
            db.StringSet(scenario.Id.ToString(), json);

            putBufferToS3("emissions", scenario.Id.ToString(), this.GlobalEmissionsInventory);

            return token;
        }


        public Guid renewUserScenario(Guid token)
        {
            Guid result = token;
            UserScenarioCore scenario_inredis;

            string json = db.StringGet(token.ToString());


            if (json != null && json != "")
            {
                //transfer to main object
                scenario_inredis = JsonConvert.DeserializeObject<UserScenarioCore>(json);
                scenario_inredis.createdOn = DateTime.Now;
                json = JsonConvert.SerializeObject(scenario_inredis);
                db.StringSet(scenario_inredis.Id.ToString(), json);
            }
            else
            {
                result = createUserScenario();
            }
            return result;
        }

        public void deleteUserScenario(Guid token)
        {
            db.KeyDelete(token.ToString());

            if (probeS3Object("emissions", token.ToString()).Result)
            {
                deleteS3Object("emissions", token.ToString());
            }
            if (probeS3Object("impacts", token.ToString()).Result)
            {
                deleteS3Object("impacts", token.ToString());
            }
        }

        public async void cleanCache()
        {
            try
            {
                // Check whether 'mybucket' exists or not.
                bool found = await minioClient.BucketExistsAsync(s3Config.bucket);
                if (found)
                {
                    // List objects from 'my-bucketname'
                    List<string> bucketKeys = new List<string>();

                    IObservable<Item> observable = minioClient.ListObjectsAsync(s3Config.bucket);

                    IDisposable subscription = observable.Subscribe(
                            item => bucketKeys.Add(item.Key),
                            ex => Console.WriteLine("OnError: {0}", ex.Message),
                            () => Console.WriteLine("OnComplete: {0}"));

                    observable.Wait();

                    try
                    {
                        foreach (var item in bucketKeys)
                        {

                            string ext = Path.GetExtension(item);
                            if (ext != null)
                            {
                                if (ext == ".emissions" || ext == ".impacts")
                                {
                                    //try to get redis scenario
                                    String token = Path.GetFileNameWithoutExtension(item);
                                    //not found easy - remove file;

                                    if (!db.KeyExists(token))
                                    {
                                        if (probeS3Object(ext.Remove(0, 1), token).Result)
                                        {
                                            deleteS3Object(ext.Remove(0, 1), token);
                                        }
                                    }
                                    else
                                    {
                                        //check if expired but still in cache
                                        string json = db.StringGet(token);
                                        if (json != null && json != "")
                                        {
                                            //transfer to main object
                                            UserScenarioCore scenario = JsonConvert.DeserializeObject<UserScenarioCore>(json);
                                            TimeSpan duration = DateTime.Now - scenario.createdOn;
                                            if (duration.TotalHours > 24)
                                            {
                                                //old, issue with redis cache, delete
                                                try
                                                {
                                                    deleteUserScenario(new Guid(token));
                                                }
                                                catch (Exception exe)
                                                {

                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //just ignoring the exception, bucketkeys can become inconsistent with redis state due timing, follow up iteration will take care of this
                    }
                }
            }
            catch (MinioException e)
            {
            }

        }



    }
}
