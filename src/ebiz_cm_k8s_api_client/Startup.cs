using System;
using System.Collections.Generic;
using System.Linq;
using k8s;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ebiz_cm_k8s_api_client
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
            KubernetesClientConfiguration config = null;
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                // Ideally this config would be read from the .net core config constructs,
                // but that has not been implemented in the KubernetesClient library at
                // the time this sample was created.
                config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            }
            else
            {
                config = KubernetesClientConfiguration.InClusterConfig();
            }
            
            services.AddSingleton(config);
            
            services.AddHostedService<MonitorConfigMapsHostedService>();
            
            // Setup the http client
            services.AddHttpClient("K8s")
                .AddTypedClient<IKubernetes>((httpClient, serviceProvider) =>
                {
                    return new Kubernetes(
                        serviceProvider.GetRequiredService<KubernetesClientConfiguration>(),
                        httpClient);
                })
                .ConfigurePrimaryHttpMessageHandler(config.CreateDefaultHttpClientHandler)
                .AddHttpMessageHandler(KubernetesClientConfiguration.CreateWatchHandler);

            services.AddControllers();
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

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}