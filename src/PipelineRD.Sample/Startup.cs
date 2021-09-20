using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using PipelineRD.Extensions;
using PipelineRD.Settings;

using StackExchange.Redis;

using System.IO;

namespace PipelineRD.Sample
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Environment = env;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.UsePipelineRD(x =>
            {
                x.UseCacheInRedis(new RedisCacheSettings()
                {
                    ConnectionString = "sandbox-slug.redis.cache.windows.net:6380,ssl=true,password=hjFwEjYaVd4UQJNyFDIEykss4xuopNLIR3IdOc4Jezw=,defaultDatabase=11",
                    TTLInMinutes = 1,
                    KeyPreffix = "pipelineRD"
                });
                x.AddPipelineServices(x =>
                {
                    x.InjectContexts();
                    x.InjectSteps();
                    x.InjectPipelines();
                    x.InjectPipelineInitializers();
                    x.InjectPipelineBuilders();
                });

                // localhost:{PORT}/docs
                x.UseDocumentation(x =>
                {
                    var path = Path.Combine(Environment.ContentRootPath, "wwwroot", "docs");
                    x.UsePath(path);
                });
            });


            services.AddDirectoryBrowser();

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Need this to see the pipeline docs
            app.UseDefaultFiles();
            app.UseStaticFiles();
            // --

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
