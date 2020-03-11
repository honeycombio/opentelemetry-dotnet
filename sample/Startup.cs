using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Honeycomb.OpenTelemetry;
using Honeycomb.Models;
using Honeycomb;
using OpenTelemetry.Trace.Configuration;

namespace sample
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
            services.AddControllers();

            // Honeycomb Setup
            services.Configure<HoneycombApiSettings>(Configuration.GetSection("HoneycombSettings"));
            services.AddHttpClient("honeycomb");
            services.AddSingleton<IHoneycombService, HoneycombService>();
            services.AddSingleton<HoneycombExporter>();

            // OpenTelemetry Setup
            services.AddOpenTelemetry((sp, builder) => {
                builder.UseHoneycomb(sp)
                    .AddRequestCollector()
                    .AddDependencyCollector();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
