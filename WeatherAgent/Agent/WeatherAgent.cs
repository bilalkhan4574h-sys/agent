using Microsoft.SemanticKernel;

namespace WeatherAgent.Agent
{
    public interface IWeatherAgent
    {
        Task ProcessQueryAsync(string userQuery);
    }

    public class WeatherAgent : IWeatherAgent
    {
        private readonly IFoundryPlanner _planner;
        private readonly Kernel _kernel;

        public WeatherAgent(IFoundryPlanner planner, Kernel kernel)
        {
            _planner = planner;
            _kernel = kernel;
        }

        public async Task ProcessQueryAsync(string userQuery)
        {
            try
            {
                Console.WriteLine("🤖 Agent: Processing your request...\n");

                var result = await _planner.PlanAndExecuteAsync(userQuery, _kernel, System.Threading.CancellationToken.None);

                Console.WriteLine("📋 Result:");
                Console.WriteLine(new string('─', 60));
                Console.WriteLine(result);
                Console.WriteLine(new string('─', 60));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing query: {ex.Message}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner: {ex.InnerException.Message}");
                }
            }
        }
    }
}