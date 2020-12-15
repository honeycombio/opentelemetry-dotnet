using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Honeycomb.OpenTelemetry;
using Honeycomb.Models;
using Honeycomb;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Microsoft.Extensions.Options;

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

            // OpenTelemetry Setup
            services.AddOpenTelemetryTracing((sp, builder) => {
                builder.AddHoneycombExporter(sp.GetRequiredService<IHoneycombService>(), hc =>
                    {
                        var hcApiSettings = sp.GetRequiredService<IOptions<HoneycombApiSettings>>();
                        hc.DefaultDataSet = hcApiSettings.Value.DefaultDataSet;
                        hc.TeamId = hcApiSettings.Value.TeamId;
                    })
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .SetResourceBuilder(ResourceBuilder
                        .CreateDefault()
                        .AddService(serviceName: "my-service-name"));
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
