using Microsoft.SemanticKernel;
using System.Collections.Concurrent;

namespace WeatherAgent.Agent
{
    public class PluginFunctionRegistry
    {
        private readonly ConcurrentDictionary<string, Func<Dictionary<string, string>, CancellationToken, Task<string>>> _functions
            = new(StringComparer.OrdinalIgnoreCase);

        public void RegisterFunctions(IEnumerable<object> functions, Kernel kernel)
        {
            foreach (var func in functions)
            {
                try
                {
                    dynamic f = func;
                    string name = (f.Name as string) ?? string.Empty;
                    string skill = (f.SkillName as string) ?? string.Empty;

                    string key1 = name;
                    string key2 = string.IsNullOrWhiteSpace(skill) ? name : skill + "." + name;

                    // Create wrapper that will attempt to invoke the function dynamically.
                    Func<Dictionary<string, string>, CancellationToken, Task<string>> wrapper = async (parameters, ct) =>
                    {
                        try
                        {
                            // Many plugin function implementations accept an SKContext, but here we attempt
                            // a dynamic invoke with the parameters dictionary first. If that fails at runtime,
                            // the exception will be returned as invocation error text.
                            var res = await f.InvokeAsync(parameters, cancellationToken: ct);
                            // Try to pull a Result property if available
                            try
                            {
                                return res?.Result ?? (res is string s ? s : string.Empty);
                            }
                            catch
                            {
                                return res?.ToString() ?? string.Empty;
                            }
                        }
                        catch (Exception ex)
                        {
                            return $"Invocation error: {ex.Message}";
                        }
                    };

                    _functions.TryAdd(key1, wrapper);
                    if (key2 != key1)
                    {
                        _functions.TryAdd(key2, wrapper);
                    }
                }
                catch
                {
                    // ignore functions we can't register
                }
            }
        }

        public bool TryGet(string name, out Func<Dictionary<string, string>, CancellationToken, Task<string>>? invoker)
        {
            return _functions.TryGetValue(name, out invoker);
        }
    }
}
