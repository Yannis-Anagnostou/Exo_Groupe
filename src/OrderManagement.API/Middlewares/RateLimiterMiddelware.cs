namespace OrderManagement.API.Middlewares
{
    public class RateLimiterMiddelware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public RateLimiterMiddelware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            if (context.Response.StatusCode == 429)
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var path = context.Request.Path;

                _logger.LogWarning("Rate limit atteint — IP {Ip} | endpoint {Path}", ip, path);

                
            }
        }
    }


}

