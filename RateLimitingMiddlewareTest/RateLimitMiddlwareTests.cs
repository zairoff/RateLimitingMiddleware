using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RateLimitingMiddleware;
using RateLimitingMiddleware.Controllers;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Xunit;

namespace RateLimitingMiddlewareTest
{
    public class RateLimitMiddlwareTests
    {
        [Theory]
        [InlineData(11, HttpStatusCode.TooManyRequests)]
        [InlineData(9, HttpStatusCode.OK)]
        public async Task WeatherForecastController_GetMehtod(int apiRequestsCount, HttpStatusCode statusCode)
        {
            HttpStatusCode result = 0;
            using (var client = new HttpClient())
            {
                string url = "https://localhost:44350/WeatherForecast";

                for (int i = 0; i < apiRequestsCount; i++)
                {
                    using var response = await client.GetAsync(url);
                    result = response.StatusCode;
                }
            }

            Assert.Equal(statusCode, result);
        }
    }
}
