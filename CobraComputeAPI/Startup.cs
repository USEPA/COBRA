using CobraCompute;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;


namespace CobraComputeAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        [System.Obsolete]
        public void ConfigureServices(IServiceCollection services)
        {
            var RedisOptions = new RedisConfig();
            Configuration.GetSection("RedisConfig").Bind(RedisOptions);
            var s3Options = new S3Config();
            Configuration.GetSection("S3Config").Bind(s3Options);
            var modelOptions = new ModelConfig();
            Configuration.GetSection("ModelConfig").Bind(modelOptions);

            //check if in cloud.gov env rather than local and Update configurations based on VCAP_SERVICES
            var vcapServices = Configuration["VCAP_SERVICES"];
            if (!string.IsNullOrEmpty(vcapServices))
            {
                Console.WriteLine("IN VCAP SERVICES IF:");
                var boundServices = JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string, dynamic>>>>(vcapServices);

                // Update RedisConfig
                if (boundServices.ContainsKey("aws-elasticache-redis"))
                {                  
                    var redisCreds = boundServices["aws-elasticache-redis"][0]["credentials"];
                    string configUri = $"{redisCreds.hostname}:{redisCreds.port},ssl=true,password={redisCreds.password}";
                    RedisOptions.URI = configUri;
                    Console.WriteLine("CONFIGURED REDIS");
                }

                // Update S3Config
                if (boundServices.ContainsKey("s3"))
                {
                    var s3Creds = boundServices["s3"][0]["credentials"];
                    s3Options.endpoint = $"{s3Creds.endpoint}:443";
                    s3Options.bucket = s3Creds.bucket;
                    s3Options.region = s3Creds.region;
                    s3Options.accessKey = s3Creds.access_key_id;
                    s3Options.secretKey = s3Creds.secret_access_key;
                    s3Options.ssl = true;
                    Console.WriteLine("CONFIGURED S3");
                }
            }
            CobraComputeCore core = new CobraComputeCore(RedisOptions,s3Options,modelOptions);
            //core.initialize();
            _ = services.AddMvc(option => option.EnableEndpointRouting = false);
            //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddSingleton(core);
            services.AddControllers().AddNewtonsoftJson();
            //services.AddControllers();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            //adding swagger docs
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v5", new OpenApiInfo { Title = "COBRA API", Version = "v5" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseCors(builder =>
            {
                builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            });


            //app.UseHttpsRedirection();
            app.UseMvc();


            // adding swagger
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                var swaggerJsonBasePath = string.IsNullOrWhiteSpace(c.RoutePrefix) ? "." : "..";
                c.SwaggerEndpoint($"{swaggerJsonBasePath}/swagger/v5/swagger.json", "COBRA API");
            });

        }
    }
}
