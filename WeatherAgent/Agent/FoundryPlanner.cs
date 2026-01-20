using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;


namespace WeatherAgent.Agent
{
    public interface IFoundryPlanner
    {
        Task<string> PlanAndExecuteAsync(string userQuery, Kernel kernel, CancellationToken cancellationToken, bool clearHistory = false);
    }

    public class FoundryPlanner : IFoundryPlanner
    {
        private const string SystemPrompt = @"You are a helpful weather assistant with access to various tools for weather, location, and time information.

    When a user asks a question:
    1. Analyze what information is needed
    2. Use the available tools to gather that information
    3. Provide a clear, friendly, and natural response

    Available tools will be automatically provided to you. Use them as needed to answer accurately.";

        public async Task<string> PlanAndExecuteAsync(string userQuery, Kernel kernel, CancellationToken cancellationToken, bool clearHistory = false)
        {
            try
            {
                IChatCompletionService chatService;
                try
                {
                    chatService = kernel.GetRequiredService<IChatCompletionService>();
                }
                catch (Exception)
                {
                    const string msg = "Foundry chat service is not configured. Check Foundry settings (ModelId, Endpoint, ApiKey) in appsettings.json.";
                    Console.WriteLine($"❌ {msg}");
                    return $"❌ Planning Error: {msg}";
                }

                var chatHistory = new ChatHistory(SystemPrompt);

                if (clearHistory)
                {
                    chatHistory.Clear(); // Optional: ensures no residual messages
                }

                chatHistory.AddUserMessage(userQuery);

                var executionSettings = new PromptExecutionSettings
                {
                    ExtensionData = new Dictionary<string, object>
                    {
                        { "max_tokens", 2048 },
                        { "temperature", 0.7 }
                    }
                };

                Console.WriteLine("🧠 Foundry model is analyzing your query and selecting tools...\n");

                var prompt = chatHistory.ToString() ?? string.Empty;

                var response = await chatService.GetChatMessageContentAsync(
                    chatHistory,
                    executionSettings,
                    kernel,
                    cancellationToken);

                return FormatResponse(response.Content ?? "No response generated.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR in PlanAndExecuteAsync:");
                Console.WriteLine($"   Message: {ex.Message}");
                Console.WriteLine($"   Type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
                }
                Console.WriteLine($"   Stack Trace:");
                Console.WriteLine($"   {ex.StackTrace}");

                if (ex.GetType().Name.Contains("HttpOperationException") || ex.Message.Contains("Status: 404"))
                {
                    var hint = "The chat service returned 404 Not Found. Verify the Foundry 'Endpoint' and 'ModelId' in WeatherAgent/appsettings.json match your model server's API (path and model name), and include an ApiKey if required.";
                    Console.WriteLine($"   Hint: {hint}");
                    return $"❌ Planning Error: {ex.Message}. {hint}";
                }

                return $"❌ Planning Error: {ex.Message}";
            }
        }

        private string FormatResponse(string rawResponse)
        {
            var sb = new StringBuilder();
            var lines = rawResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                sb.AppendLine(line.Trim());
            }

            return sb.ToString().Trim();
        }
    }
}