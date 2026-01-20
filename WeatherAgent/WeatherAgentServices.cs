using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using WeatherAgent.Config;
using WeatherAgent.Agent;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using System.Net.Http;

namespace WeatherAgent
{
    public class WeatherAgentService : IHostedService
    {
        private readonly IWeatherAgent _agent;
        private readonly IConfiguration _configuration;
        private readonly Kernel _kernel;
        private readonly IHostApplicationLifetime _lifetime;

        public WeatherAgentService(
            IWeatherAgent agent,
            IConfiguration configuration,
            Kernel kernel,
            IHostApplicationLifetime lifetime)
        {
            _agent = agent;
            _configuration = configuration;
            _kernel = kernel;
            _lifetime = lifetime;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("🚀 Weather Agent Starting...\n");

            await LoadPluginsFromConfiguration();

            Console.WriteLine("✅ All plugins loaded successfully!\n");
            Console.WriteLine("═══════════════════════════════════════════");
            Console.WriteLine("Weather Agent Ready!");
            Console.WriteLine("═══════════════════════════════════════════\n");

            await RunInteractiveMode(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("\n🛑 Weather Agent Shutting Down...");
            return Task.CompletedTask;
        }

        private async Task RunInteractiveMode(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Console.Write("💬 You: ");
                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("quit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("\n👋 Goodbye!");
                    _lifetime.StopApplication();
                    break;
                }

                Console.WriteLine();
                await _agent.ProcessQueryAsync(input);
                Console.WriteLine("\n" + new string('─', 60) + "\n");
            }
        }

        private async Task LoadPluginsFromConfiguration()
        {
            var pluginConfig = _configuration
                .GetSection("Plugins")
                .Get<PluginConfiguration>();

            if (pluginConfig?.PluginEndpoints == null || !pluginConfig.PluginEndpoints.Any())
            {
                throw new InvalidOperationException("No plugins configured in appsettings.json");
            }

            Console.WriteLine($"📦 Loading {pluginConfig.PluginEndpoints.Count} plugin(s) from remote OpenAPI URLs...\n");

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            var httpClient = new HttpClient(handler);

            foreach (var plugin in pluginConfig.PluginEndpoints)
            {
                Console.WriteLine($"  ⏳ Importing plugin: {plugin.PluginName}");
                Console.WriteLine($"     URL: {plugin.SwaggerUrl}");

                try
                {
                    var testResponse = await httpClient.GetAsync(plugin.SwaggerUrl);

                    if (!testResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"  ❌ URL returned {testResponse.StatusCode}");
                        continue;
                    }

                    var swaggerContent = await testResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"  ✅ URL accessible, content length: {swaggerContent.Length} bytes");

                    var importedPlugin = await _kernel.ImportPluginFromOpenApiAsync(
                        pluginName: plugin.PluginName,
                        uri: new Uri(plugin.SwaggerUrl),
                        new OpenApiFunctionExecutionParameters
                        {
                            HttpClient = httpClient,
                            IgnoreNonCompliantErrors = true
                        });

                    Console.WriteLine($"  ✅ Plugin '{plugin.PluginName}' imported successfully");
                    Console.WriteLine($"     Functions imported: {importedPlugin.Count()}");

                    foreach (var function in importedPlugin)
                    {
                        Console.WriteLine($"        • {function.Name}");
                        Console.WriteLine($"          Description: {function.Description ?? "N/A"}");
                        Console.WriteLine($"          Parameters: {function.Metadata.Parameters.Count}");
                    }

                    Console.WriteLine();
                }
                catch (HttpRequestException httpEx)
                {
                    Console.WriteLine($"  ❌ HTTP Error loading plugin '{plugin.PluginName}':");
                    Console.WriteLine($"     {httpEx.Message}");
                    Console.WriteLine($"     Make sure your API is running and accessible!\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ❌ Error loading plugin '{plugin.PluginName}':");
                    Console.WriteLine($"     Type: {ex.GetType().Name}");
                    Console.WriteLine($"     Message: {ex.Message}");

                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"     Inner: {ex.InnerException.Message}");
                    }

                    Console.WriteLine();
                }
            }
        }
    }
}