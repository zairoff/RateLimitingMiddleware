using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace RateLimitingMiddleware
{
    public class RateLimitMiddlware
    {
        private readonly RequestDelegate _next;
        static readonly ConcurrentDictionary<string, Queue<DateTime?>> RequestHistory = new();

        public RateLimitMiddlware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            var controllerActionDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();

            if (controllerActionDescriptor is null)
            {
                await _next(context);
                return;
            }

            var rateLimitAttribute = (RateLimitAttribute)controllerActionDescriptor.MethodInfo
                            .GetCustomAttributes(true)
                            .SingleOrDefault(w => w.GetType() == typeof(RateLimitAttribute));

            if (rateLimitAttribute is null)
            {
                await _next(context);
                return;
            }

            string key = GetCurrentClientKey(rateLimitAttribute, context);

            var previousRequest = GetPreviousRequestByKey(key);

            if(previousRequest == null)
            {
                InitializeRequestHistory(key);
                await _next(context);
                return;
            }

            if (previousRequest.TryPeek(out DateTime? lastVisit))
            {
                if (IsOverTimeRange(lastVisit))
                {
                    if (IsOverLimit(previousRequest))
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                        return;
                    }

                    AddRequest(previousRequest);
                }
                else
                {
                    UpdateRequest(previousRequest);
                }
            }

            await _next(context);
        }

        private static bool IsOverTimeRange(DateTime? time)
        {
            return time.Value.AddSeconds(1) > DateTime.Now;
        }

        private static bool IsOverLimit(Queue<DateTime?> requests)
        {
            // here is 10, 10 requests per second
            return requests.Count == 10;
        }

        private static void AddRequest(Queue<DateTime?> requests)
        {
            requests.Enqueue(DateTime.Now);
        }

        private static void UpdateRequest(Queue<DateTime?> requests)
        {
            requests.TryDequeue(out _);
            requests.Enqueue(DateTime.Now);
        }

        private static void InitializeRequestHistory(string key)
        {
            RequestHistory.TryAdd(key, new Queue<DateTime?>(10));
            var previousCall = GetPreviousRequestByKey(key);
            previousCall.Enqueue(DateTime.Now);
        }

        private static Queue<DateTime?> GetPreviousRequestByKey(string key)
        {
            RequestHistory.TryGetValue(key, out Queue<DateTime?> value);
            return value;
        }

        private static string GetCurrentClientKey(RateLimitAttribute apiDecorator, HttpContext context)
        {
            var keys = new List<string>
            {
                context.Request.Path
            };

            if (apiDecorator.LimitBy == LimitTypeEnum.IpAddress)
                keys.Add(GetClientIpAddress(context));

            return string.Join('_', keys);
        }

        private static string GetClientIpAddress(HttpContext context)
        {
            return context.Connection.RemoteIpAddress.ToString();
        }
    }
}
