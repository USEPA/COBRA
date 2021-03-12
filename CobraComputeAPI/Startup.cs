using CobraCompute;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        public void ConfigureServices(IServiceCollection services)
        {
            var RedisOptions = new RedisConfig();
            Configuration.GetSection("RedisConfig").Bind(RedisOptions);
            var s3Options = new S3Config();
            Configuration.GetSection("S3Config").Bind(s3Options);
            var modelOptions = new ModelConfig();
            Configuration.GetSection("ModelConfig").Bind(modelOptions);


            CobraComputeCore core = new CobraComputeCore(RedisOptions,s3Options,modelOptions);
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddSingleton(core);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
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
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
