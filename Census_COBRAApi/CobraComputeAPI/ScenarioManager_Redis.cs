using Ceras;
using CobraCompute;
using DocumentFormat.OpenXml.Spreadsheet;
using Minio;
using Minio.DataModel;
using Minio.Exceptions;
using Newtonsoft.Json;
using ProtoBuf.Data;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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

        public async Task<MemoryStream> getBufferFromS3(String type, String token)
        {
            return await getS3Buffer_internal(type, token);
        }

        private async Task ensureBucketExistsAsync(string bucketName)
        {
            bool found = await minioClient.BucketExistsAsync(bucketName);
            if (!found)
            {
                await minioClient.MakeBucketAsync(bucketName);
            }
        }

        private async Task putS3Buffer_internal(String type, String sessionid, MemoryStream buffer)
        {
            try
            {
                String objectname = sessionid + "." + type;
                buffer.Position = 0;

                await ensureBucketExistsAsync(s3Config.bucket);

                Debug.WriteLine($"Uploading to {s3Config.bucket}/{objectname} with buffer length: {buffer.Length}");
                await minioClient.PutObjectAsync(s3Config.bucket, objectname, buffer, buffer.Length);
            }
            catch (MinioException e)
            {
                Debug.WriteLine($"MinioException: {e.Message}");
                throw; // rethrow the exception to see the stack trace in debug output
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Exception: {e.Message}");
                throw; // rethrow the exception to see the stack trace in debug output
            }
        }

        public async Task putBufferToS3(String type, String token, MemoryStream buffer)
        {
            await putS3Buffer_internal(type, token, buffer);
        }

        public async Task deleteS3Object(String type, String sessionid)
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
                Debug.WriteLine($"------ minio exception in PROBES3 Object: {e}");
                persistentImpacts = false;
            }
            return persistentImpacts;
        }


        public async Task<UserScenario> retrieve(Guid token)
        {
            UserScenario scenario = new UserScenario();
            UserScenarioCore scenario_inredis = null;

            string json = await db.StringGetAsync(token.ToString());


            if (json != null && json != "")
            {
                // Transfer to main object
                scenario_inredis = JsonConvert.DeserializeObject<UserScenarioCore>(json);
                scenario.Id = scenario_inredis.Id;
                scenario.createdOn = scenario_inredis.createdOn;
                scenario.isDirty = scenario_inredis.isDirty;
                scenario.isEmissionsDataDirty = scenario_inredis.isEmissionsDataDirty;
                scenario.Year = scenario_inredis.Year;
                scenario.queueSubmission = scenario_inredis.queueSubmission;

                // Deserialize DataTable
                MemoryStream buffer_emissions = await getBufferFromS3("emissions", token.ToString());
                scenario.EmissionsData = DataSerializer.DeserializeDataTable(buffer_emissions);
                buffer_emissions = null;

                // Deserialize Impacts
                if (await probeS3Object("impacts", token.ToString()))
                {
                    var ceras = new CerasSerializer();
                    MemoryStream buffer_c = await getBufferFromS3("impacts", token.ToString());
                    scenario.Impacts = ceras.Deserialize<List<Cobra_ResultDetail>>(buffer_c.ToArray());
                    buffer_c = null;
                    ceras = null;
                }
                else Debug.WriteLine($"Did not find persistent impacts");
            }
            else
            {
                throw new System.ArgumentException("Guid not in REDIS.");
            }

            GC.Collect();
            Debug.WriteLine($"GC waiting for pending finalizers");
            GC.WaitForPendingFinalizers();
            GC.Collect();
            return scenario;
        }

        public async Task store(UserScenario value)
        {

            MemoryStream bufferEmissions = new MemoryStream();
            MemoryStream bufferImpacts = null;

            UserScenarioCore scenario_inredis = new UserScenarioCore()
            {
                createdOn = value.createdOn,
                isDirty = value.isDirty,
                isEmissionsDataDirty = value.isEmissionsDataDirty,
                Year = value.Year,
                Id = value.Id,
                queueSubmission = value.queueSubmission
            };

            string scenarioKey = value.Id.ToString();
            string scenarioJson = JsonConvert.SerializeObject(scenario_inredis);

            // Serialize DataTable to a buffer
            if (value.EmissionsData != null)
            {
                value.EmissionsData.AcceptChanges();
                DataSerializer.Serialize(bufferEmissions, value.EmissionsData);
                Debug.WriteLine($"SERIALZING EMISSIONS DATA");
            }

            // Serialize Impacts to a buffer
            if (value.Impacts != null)
            {
                var ceras = new CerasSerializer();
                bufferImpacts = new MemoryStream(ceras.Serialize(value.Impacts));
                Debug.WriteLine($"SETTING BUFFER IMPACTS");
            }

            bool transactionSucceeded = false;
            while (!transactionSucceeded)
            {
                try
                {
                    string existingScenario = await db.StringGetAsync(scenarioKey);

                    if (existingScenario != null)
                    {
                        var tran = db.CreateTransaction();
                        tran.AddCondition(Condition.StringEqual(scenarioKey, existingScenario));
                        var setString = tran.StringSetAsync(scenarioKey, scenarioJson);

                        transactionSucceeded = await tran.ExecuteAsync();

                        // If transaction succeeded, update S3 buffers
                        if (transactionSucceeded && setString.Result)
                        {
                            await putBufferToS3("emissions", scenarioKey, bufferEmissions);
                            if (bufferImpacts != null)
                            {
                                await putBufferToS3("impacts", scenarioKey, bufferImpacts);
                            }
                        }
                    }
                    else
                    {
                        throw new System.ArgumentException("Guid not in REDIS.");
                    }
                }
                catch (RedisException ex)
                {
                    // Handle the exception (e.g., log it)
                    transactionSucceeded = false;
                }
            }

            // Cleanup
            bufferEmissions.Dispose();
            bufferImpacts?.Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public async Task<Guid> createUserScenario()
        {
            Guid token;
            bool go_on = false;
            int attempt = 0;

            Debug.WriteLine("Starting createUserScenario");

            // Repeat until a unique token is generated
            do
            {
                attempt++;
                token = Guid.NewGuid();
                Debug.WriteLine($"Attempt {attempt}: Generated token {token}");
                try
                {
                    go_on = !await db.KeyExistsAsync(token.ToString());
                    Debug.WriteLine($"Token exists: {!go_on}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Exception during token check: {ex.Message}");
                    if (attempt >= 3)
                    {
                        throw new System.ArgumentException("Redis fails key check.");
                    }
                    await Task.Delay(50); // Use Task.Delay for async delay
                }
            } while (!go_on);

            UserScenarioCore scenario = new UserScenarioCore()
            {
                Id = token,
                Year = 2025,
                createdOn = DateTime.Now,
                isEmissionsDataDirty = false,
                isDirty = true
            };

            string json = JsonConvert.SerializeObject(scenario);
            Debug.WriteLine($"Serialized scenario: {json}");
            // Ensure atomicity using Redis transactions
            var tran = db.CreateTransaction();
            tran.AddCondition(Condition.KeyNotExists(token.ToString()));
            var setStringTask = tran.StringSetAsync(token.ToString(), json);

            bool transactionSucceeded = await tran.ExecuteAsync();

            Debug.WriteLine($"Transaction succeeded: {transactionSucceeded}, Set string success: {setStringTask.Result}");

            if (!transactionSucceeded)
            {
                throw new System.ArgumentException("Failed to create a unique scenario.");
            }

            // Assuming putBufferToS3 and cleanCache are thread-safe and async
            await putBufferToS3("emissions", token.ToString(), this.GlobalEmissionsInventory);
            await cleanCache();

            Debug.WriteLine($"Successfully created scenario with token: {token}");
            return token;
        }


        public async Task<Guid> resetUserScenario(Guid token)
        {
            await deleteUserScenario(token);

            UserScenarioCore scenario = new UserScenarioCore()
            {
                Id = token,
                Year = 2025,
                createdOn = DateTime.Now,
                isEmissionsDataDirty = false,
                isDirty = true
            };

            string json = JsonConvert.SerializeObject(scenario);

            var tran = db.CreateTransaction();
            tran.AddCondition(Condition.KeyNotExists(scenario.Id.ToString()));

            // Use transaction to ensure atomicity
            var setStringTask = tran.StringSetAsync(scenario.Id.ToString(), json);

            bool committed = await tran.ExecuteAsync();
            Debug.WriteLine($"Transaction succeeded/committed: {committed}, Set string success: {setStringTask.Result}");

            if (committed)
            {
                await putBufferToS3("emissions", scenario.Id.ToString(), this.GlobalEmissionsInventory);
            }
            else
            {
                throw new Exception("Failed to reset user scenario due to concurrent modification.");
            }

            return token;
        }


        public async Task<Guid> renewUserScenario(Guid token)
        {
            var db = redis.GetDatabase();
            string script = @"
        local exists = redis.call('EXISTS', KEYS[1])
        if exists == 1 then
            local json = redis.call('GET', KEYS[1])
            local scenario = cjson.decode(json)
            scenario.createdOn = ARGV[1]
            json = cjson.encode(scenario)
            redis.call('SET', KEYS[1], json)
            return KEYS[1]
        else
            return nil
        end
    ";

            var createdOn = DateTime.Now.ToString("o");
            var result = (string)await db.ScriptEvaluateAsync(script, new RedisKey[] { token.ToString() }, new RedisValue[] { createdOn });

            if (result == null)
            {
                result = (await createUserScenario()).ToString();
            }

            return Guid.Parse(result);
        }

        public async Task deleteUserScenario(Guid token)
        {
            db.KeyDelete(token.ToString());

            if (await probeS3Object("emissions", token.ToString()))
            {
                await deleteS3Object("emissions", token.ToString());
            }
            if (await probeS3Object("impacts", token.ToString()))
            {
                await deleteS3Object("impacts", token.ToString());
            }
        }

        public async Task cleanCache()
        {
            try
            {
                // Check whether 'mybucket' exists or not.
                bool found = await minioClient.BucketExistsAsync(s3Config.bucket);
                if (found)
                {
                    // List objects from 'my-bucketname'
                    List<string> bucketKeys = new List<string>();

                    IObservable<Minio.DataModel.Item> observable = minioClient.ListObjectsAsync(s3Config.bucket);

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
                                        if (await probeS3Object(ext.Remove(0, 1), token))
                                        {
                                            await deleteS3Object(ext.Remove(0, 1), token);
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
                                                    await deleteUserScenario(new Guid(token));
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
