using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using WeatherAgent.Agent;
using WeatherAgent.Config;
using System.Net.Http;

namespace WeatherAgent
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.Configure<PluginConfiguration>(
                        context.Configuration.GetSection("Plugins"));

                   services.Configure<FoundryConfiguration>(
                        context.Configuration.GetSection("Foundry"));

                    services.AddSingleton<Kernel>(sp =>
                    {
                        var kernelBuilder = Kernel.CreateBuilder();
                        var foundryConfig = context.Configuration
                            .GetSection("Foundry")
                            .Get<FoundryConfiguration>();

                        kernelBuilder.AddOpenAIChatCompletion(
                            modelId: foundryConfig!.ModelId,
                            endpoint: new Uri(foundryConfig.Endpoint),
                            apiKey: foundryConfig.ApiKey
                        );

                        return kernelBuilder.Build();
                    });

                    services.AddSingleton<IFoundryPlanner, FoundryPlanner>();
                    services.AddSingleton<IWeatherAgent, Agent.WeatherAgent>();
                    services.AddHostedService<WeatherAgentService>();

                })
                .Build();

            await host.RunAsync();
        }
    }
}