using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Weather Agent API",
        Version = "v1",
        Description = "Unified API providing weather, location, and time information",
        Contact = new OpenApiContact
        {
            Name = "Weather Agent Team",
            Email = "support@weatheragent.com"
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure middleware
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Weather Agent API v1");
    c.RoutePrefix = string.Empty;
});

app.UseCors();

// ==================== WEATHER ENDPOINTS ====================

app.MapGet("/weather/{city}", (string city) =>
{
    var weather = WeatherService.GetWeatherData(city);
    return Results.Ok(weather);
})
.WithName("GetWeather")
.WithOpenApi(operation =>
{
    operation.Summary = "Get current weather for a city";
    operation.Description = "Returns comprehensive weather information including temperature, conditions, humidity, and wind data";
    return operation;
})
.Produces<WeatherResponse>(StatusCodes.Status200OK);

app.MapGet("/weather/temperature/{city}", (string city) =>
{
    var temp = WeatherService.GetTemperature(city);
    return Results.Ok(temp);
})
.WithName("GetTemperature")
.WithOpenApi(operation =>
{
    operation.Summary = "Get temperature for a city";
    operation.Description = "Returns current temperature in Celsius and Fahrenheit";
    return operation;
})
.Produces<TemperatureResponse>(StatusCodes.Status200OK);

app.MapGet("/weather/forecast/{city}", (string city, [FromQuery] int days = 5) =>
{
    var forecast = WeatherService.GetForecast(city, days);
    return Results.Ok(forecast);
})
.WithName("GetForecast")
.WithOpenApi(operation =>
{
    operation.Summary = "Get weather forecast";
    operation.Description = "Returns weather forecast for specified number of days (1-10)";
    return operation;
})
.Produces<ForecastResponse>(StatusCodes.Status200OK);

// ==================== LOCATION ENDPOINTS ====================

app.MapGet("/location/{city}", (string city) =>
{
    var location = WeatherService.GetLocationInfo(city);
    return Results.Ok(location);
})
.WithName("GetLocation")
.WithOpenApi(operation =>
{
    operation.Summary = "Get location information for a city";
    operation.Description = "Returns geographic coordinates, timezone, country, and region information";
    return operation;
})
.Produces<LocationResponse>(StatusCodes.Status200OK);

app.MapGet("/location/coordinates/{city}", (string city) =>
{
    var coords = WeatherService.GetCoordinates(city);
    return Results.Ok(coords);
})
.WithName("GetCoordinates")
.WithOpenApi(operation =>
{
    operation.Summary = "Get geographic coordinates";
    operation.Description = "Returns latitude and longitude for a city";
    return operation;
})
.Produces<CoordinatesResponse>(StatusCodes.Status200OK);

app.MapGet("/location/timezone/{city}", (string city) =>
{
    var timezone = WeatherService.GetTimezone(city);
    return Results.Ok(timezone);
})
.WithName("GetTimezone")
.WithOpenApi(operation =>
{
    operation.Summary = "Get timezone information";
    operation.Description = "Returns timezone details for a city";
    return operation;
})
.Produces<TimezoneResponse>(StatusCodes.Status200OK);

app.MapPost("/location/distance", ([FromBody] DistanceRequest request) =>
{
    var distance = WeatherService.CalculateDistance(request);
    return Results.Ok(distance);
})
.WithName("CalculateDistance")
.WithOpenApi(operation =>
{
    operation.Summary = "Calculate distance between two cities";
    operation.Description = "Returns the distance in kilometers and miles";
    return operation;
})
.Produces<DistanceResponse>(StatusCodes.Status200OK);

// ==================== TIME ENDPOINTS ====================

app.MapGet("/time/current", () =>
{
    var time = WeatherService.GetCurrentTime();
    return Results.Ok(time);
})
.WithName("GetCurrentTime")
.WithOpenApi(operation =>
{
    operation.Summary = "Get current UTC time";
    operation.Description = "Returns current date and time in UTC and various formats";
    return operation;
})
.Produces<TimeResponse>(StatusCodes.Status200OK);

app.MapGet("/time/timezone/{timezone}", (string timezone) =>
{
    var time = WeatherService.GetTimeInTimezone(timezone);
    return Results.Ok(time);
})
.WithName("GetTimeInTimezone")
.WithOpenApi(operation =>
{
    operation.Summary = "Get current time in specific timezone";
    operation.Description = "Returns current time for specified timezone (e.g., America/New_York, Europe/London)";
    return operation;
})
.Produces<TimezoneTimeResponse>(StatusCodes.Status200OK);

app.MapGet("/time/city/{city}", (string city) =>
{
    var time = WeatherService.GetTimeForCity(city);
    return Results.Ok(time);
})
.WithName("GetTimeForCity")
.WithOpenApi(operation =>
{
    operation.Summary = "Get current time for a city";
    operation.Description = "Returns current local time for specified city";
    return operation;
})
.Produces<CityTimeResponse>(StatusCodes.Status200OK);

app.MapGet("/time/unix", () =>
{
    var unix = WeatherService.GetUnixTime();
    return Results.Ok(unix);
})
.WithName("GetUnixTimestamp")
.WithOpenApi(operation =>
{
    operation.Summary = "Get Unix timestamp";
    operation.Description = "Returns current Unix timestamp in seconds and milliseconds";
    return operation;
})
.Produces<UnixTimeResponse>(StatusCodes.Status200OK);

Console.WriteLine("🚀 Weather Agent API Starting...");
Console.WriteLine("📍 Swagger UI: https://localhost:44315/");
Console.WriteLine("📄 Swagger JSON: https://localhost:44315/swagger/v1/swagger.json");

app.Run();

// ==================== DATA MODELS ====================

// Weather Models
public record WeatherResponse(string City, double Temperature, string TemperatureUnit, string Condition, int Humidity, WindInfo Wind, string LastUpdated);
public record TemperatureResponse(string City, double Celsius, double Fahrenheit, string FeelsLike);
public record WindInfo(double Speed, string SpeedUnit, string Direction);
public record ForecastResponse(string City, int Days, List<DailyForecast> Forecast);
public record DailyForecast(string Date, double HighTemp, double LowTemp, string Condition, int ChanceOfRain);

// Location Models
public record LocationResponse(string City, string Country, string Region, Coordinates Coordinates, string Timezone, int Population, double Elevation);
public record Coordinates(double Latitude, double Longitude);
public record CoordinatesResponse(string City, double Latitude, double Longitude, string Format);
public record TimezoneResponse(string City, string Timezone, string UtcOffset, bool IsDST);
public record DistanceRequest(string FromCity, string ToCity);
public record DistanceResponse(string FromCity, string ToCity, double Kilometers, double Miles);

// Time Models
public record TimeResponse(string Utc, string Iso8601, long UnixTimestamp, DateComponents Date, TimeComponents Time);
public record DateComponents(int Year, int Month, int Day, string DayOfWeek);
public record TimeComponents(int Hour, int Minute, int Second, int Millisecond);
public record TimezoneTimeResponse(string Timezone, string LocalTime, string UtcOffset, bool IsDaylightSaving);
public record CityTimeResponse(string City, string LocalTime, string Timezone, string UtcOffset, string Formatted);
public record UnixTimeResponse(long Seconds, long Milliseconds, string Iso8601);

// ==================== MOCK DATA FUNCTIONS ====================

public class WeatherService
{
    public static WeatherResponse GetWeatherData(string city)
    {
        var random = new Random(city.GetHashCode());
        var conditions = new[] { "Sunny", "Cloudy", "Rainy", "Partly Cloudy", "Clear", "Overcast" };
        var directions = new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        return new WeatherResponse(
            City: city,
            Temperature: Math.Round(15 + random.NextDouble() * 20, 1),
            TemperatureUnit: "Celsius",
            Condition: conditions[random.Next(conditions.Length)],
            Humidity: 40 + random.Next(50),
            Wind: new WindInfo(
                Speed: Math.Round(5 + random.NextDouble() * 20, 1),
                SpeedUnit: "km/h",
                Direction: directions[random.Next(directions.Length)]
            ),
            LastUpdated: DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
        );
    }
     public static TemperatureResponse GetTemperature(string city)
    {
        var random = new Random(city.GetHashCode());
        var celsius = Math.Round(15 + random.NextDouble() * 20, 1);
        var fahrenheit = Math.Round(celsius * 9 / 5 + 32, 1);

        return new TemperatureResponse(
            City: city,
            Celsius: celsius,
            Fahrenheit: fahrenheit,
            FeelsLike: $"{Math.Round(celsius + random.NextDouble() * 5 - 2.5, 1)}°C"
        );
    }

    public static ForecastResponse GetForecast(string city, int days)
    {
        var random = new Random(city.GetHashCode());
        var conditions = new[] { "Sunny", "Cloudy", "Rainy", "Partly Cloudy" };
        var forecast = new List<DailyForecast>();

        for (int i = 0; i < Math.Min(days, 10); i++)
        {
            forecast.Add(new DailyForecast(
                Date: DateTime.UtcNow.AddDays(i).ToString("yyyy-MM-dd"),
                HighTemp: Math.Round(20 + random.NextDouble() * 15, 1),
                LowTemp: Math.Round(10 + random.NextDouble() * 10, 1),
                Condition: conditions[random.Next(conditions.Length)],
                ChanceOfRain: random.Next(0, 101)
            ));
        }

        return new ForecastResponse(city, days, forecast);
    }

    public static LocationResponse GetLocationInfo(string city)
    {
        var data = GetCityData(city);
        return new LocationResponse(
            City: city,
            Country: data.Country,
            Region: data.Region,
            Coordinates: new Coordinates(data.Lat, data.Lon),
            Timezone: data.Timezone,
            Population: data.Population,
            Elevation: data.Elevation
        );
    }

    public static CoordinatesResponse GetCoordinates(string city)
    {
        var data = GetCityData(city);
        return new CoordinatesResponse(
            City: city,
            Latitude: data.Lat,
            Longitude: data.Lon,
            Format: $"{data.Lat}°N, {data.Lon}°E"
        );
    }

    public static TimezoneResponse GetTimezone(string city)
    {
        var data = GetCityData(city);
        return new TimezoneResponse(
            City: city,
            Timezone: data.Timezone,
            UtcOffset: data.UtcOffset,
            IsDST: false
        );
    }

    public static DistanceResponse CalculateDistance(DistanceRequest request)
    {
        var random = new Random((request.FromCity + request.ToCity).GetHashCode());
        var km = Math.Round(500 + random.NextDouble() * 5000, 2);
        var miles = Math.Round(km * 0.621371, 2);

        return new DistanceResponse(
            FromCity: request.FromCity,
            ToCity: request.ToCity,
            Kilometers: km,
            Miles: miles
        );
    }

    public static TimeResponse GetCurrentTime()
    {
        var now = DateTime.UtcNow;
        return new TimeResponse(
            Utc: now.ToString("yyyy-MM-dd HH:mm:ss"),
            Iso8601: now.ToString("o"),
            UnixTimestamp: ((DateTimeOffset)now).ToUnixTimeSeconds(),
            Date: new DateComponents(now.Year, now.Month, now.Day, now.DayOfWeek.ToString()),
            Time: new TimeComponents(now.Hour, now.Minute, now.Second, now.Millisecond)
        );
    }

    public static TimezoneTimeResponse GetTimeInTimezone(string timezone)
    {
        var now = DateTime.UtcNow;
        return new TimezoneTimeResponse(
            Timezone: timezone,
            LocalTime: now.ToString("yyyy-MM-dd HH:mm:ss"),
            UtcOffset: "+00:00",
            IsDaylightSaving: false
        );
    }

    public static CityTimeResponse GetTimeForCity(string city)
    {
        var data = GetCityData(city);
        var offsetHours = int.Parse(data.UtcOffset.Split(':')[0]);
        var localTime = DateTime.UtcNow.AddHours(offsetHours);

        return new CityTimeResponse(
            City: city,
            LocalTime: localTime.ToString("yyyy-MM-dd HH:mm:ss"),
            Timezone: data.Timezone,
            UtcOffset: data.UtcOffset,
            Formatted: localTime.ToString("dddd, MMMM dd, yyyy h:mm:ss tt")
        );
    }

    public static UnixTimeResponse GetUnixTime()
    {
        var now = DateTime.UtcNow;
        return new UnixTimeResponse(
            Seconds: ((DateTimeOffset)now).ToUnixTimeSeconds(),
            Milliseconds: ((DateTimeOffset)now).ToUnixTimeMilliseconds(),
            Iso8601: now.ToString("o")
        );
    }

    private static (string Country, string Region, double Lat, double Lon, string Timezone, string UtcOffset, int Population, double Elevation) GetCityData(string city)
    {
        var cityLower = city.ToLower();

        return cityLower switch
        {
            "london" => ("United Kingdom", "England", 51.5074, -0.1278, "Europe/London", "+00:00", 9000000, 11),
            "paris" => ("France", "Île-de-France", 48.8566, 2.3522, "Europe/Paris", "+01:00", 2200000, 35),
            "tokyo" => ("Japan", "Kanto", 35.6762, 139.6503, "Asia/Tokyo", "+09:00", 14000000, 40),
            "new york" => ("United States", "New York", 40.7128, -74.0060, "America/New_York", "-05:00", 8400000, 10),
            "sydney" => ("Australia", "New South Wales", -33.8688, 151.2093, "Australia/Sydney", "+10:00", 5300000, 3),
            "dubai" => ("UAE", "Dubai", 25.2048, 55.2708, "Asia/Dubai", "+04:00", 3400000, 5),
            "moscow" => ("Russia", "Central Russia", 55.7558, 37.6173, "Europe/Moscow", "+03:00", 12500000, 156),
            "berlin" => ("Germany", "Berlin", 52.5200, 13.4050, "Europe/Berlin", "+01:00", 3700000, 34),
            _ => ("Unknown", "Unknown", 0, 0, "UTC", "+00:00", 0, 0)
        };
    }

}
