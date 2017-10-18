using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace DarkStatsCore.Models
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class TotalGraphModel
    {
        private readonly RequestDelegate _next;

        public TotalGraphModel(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext httpContext)
        {

            return _next(httpContext);
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class TotalGraphModelExtensions
    {
        public static IApplicationBuilder UseMiddlewareClassTemplate(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TotalGraphModel>();
        }
    }
}
