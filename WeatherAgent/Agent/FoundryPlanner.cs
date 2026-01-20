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
        private readonly PluginFunctionRegistry _functionRegistry;

        public FoundryPlanner(PluginFunctionRegistry functionRegistry)
        {
            _functionRegistry = functionRegistry;
        }
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

                var raw = response.Content ?? string.Empty;

                // Try to detect a tool invocation instruction from the model.
                // Expecting a simple directive like: CALL_TOOL: <ToolName> | param1=value1;param2=value2
                var toolInvocation = ParseToolInvocation(raw);

                if (toolInvocation != null)
                {
                    var toolResult = await InvokeKernelFunctionAsync(kernel, toolInvocation, cancellationToken);
                    // Append tool result to the model response for final formatting.
                    var combined = new StringBuilder();
                    combined.AppendLine(raw.Trim());
                    combined.AppendLine();
                    combined.AppendLine("--- TOOL OUTPUT ---");
                    combined.AppendLine(toolResult);
                    return FormatResponse(combined.ToString());
                }

                return FormatResponse(raw == string.Empty ? "No response generated." : raw);
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

        private record ToolInvocation(string ToolName, Dictionary<string, string> Parameters);

        private ToolInvocation? ParseToolInvocation(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return null;

            // Normalize and look for a CALL_TOOL marker
            var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(l => l.Trim())
                                .ToArray();

            foreach (var line in lines)
            {
                if (line.StartsWith("CALL_TOOL:", StringComparison.OrdinalIgnoreCase))
                {
                    // Format: CALL_TOOL: ToolName | key1=val1;key2=val2
                    var parts = line.Substring("CALL_TOOL:".Length).Split('|', 2);
                    var toolName = parts[0].Trim();
                    var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    if (parts.Length > 1)
                    {
                        var paramPart = parts[1];
                        var pairs = paramPart.Split(';', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var pair in pairs)
                        {
                            var kv = pair.Split('=', 2);
                            if (kv.Length == 2)
                            {
                                parameters[kv[0].Trim()] = kv[1].Trim();
                            }
                        }
                    }

                    return new ToolInvocation(toolName, parameters);
                }
            }

            return null;
        }

        private async Task<string> InvokeKernelFunctionAsync(Kernel kernel, ToolInvocation invocation, CancellationToken cancellationToken)
        {
            try
            {
                if (!_functionRegistry.TryGet(invocation.ToolName, out var invoker) || invoker == null)
                {
                    return $"Tool '{invocation.ToolName}' not found among registered functions.";
                }

                var result = await invoker(invocation.Parameters, cancellationToken);

                return result ?? string.Empty;
            }
            catch (Exception ex)
            {
                return $"Error invoking tool '{invocation.ToolName}': {ex.Message}";
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