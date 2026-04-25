using System.ComponentModel;
using ModelContextProtocol.Server;

class WeatherTools
{
    [McpServerTool]
    [Description("Informs the weather in the given city.")]
    public string GetWeather(
        [Description("City name like New York, Lahore")] string city)
        => $"Its raining in {city}";
}
