namespace WeatherAgent.Config
{
    public class PluginConfiguration
    {
        public List<PluginEndpoint> PluginEndpoints { get; set; } = new();
    }

    public class PluginEndpoint
    {
        public string PluginName { get; set; } = string.Empty;
        public string SwaggerUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}