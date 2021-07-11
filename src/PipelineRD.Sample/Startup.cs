using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

using PipelineRD.Extensions;
using PipelineRD.Sample.Workflows.Bank;
using PipelineRD.Settings;

using System.IO;

namespace PipelineRD.Sample
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
            services.UsePipelineRD(x =>
            {
                x.UseCacheInMemory(new MemoryCacheSettings());
                x.AddPipelineServices();
                // localhost:{PORT}/docs
                x.UseDocumentation(x =>
                {
                    x.UseStatic("wwwroot/docs");
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

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
