using System;

namespace RateLimitingMiddleware
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RateLimitAttribute : Attribute
    {
        public LimitTypeEnum LimitBy { get; set; }
    }
}