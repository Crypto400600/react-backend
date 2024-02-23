using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace ReactwithDotnetCore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpGet("dummy")]
        public IActionResult GetDummyData()
        {
            // Replace this with your actual logic
            var dummyData = new List<string> { "Dummy1", "Dummy2", "Dummy3" };
            return Ok(dummyData);
        }

        [Authorize]
        [HttpGet("dummy2")]
        public IActionResult GetDummyData2()
        {
            // Replace this with your actual logic
            var dummyData = new List<string> { "Dummy1", "Dummy2", "Dummy3", "Dummy4" };
            return Ok(dummyData);
        }
    }
}
