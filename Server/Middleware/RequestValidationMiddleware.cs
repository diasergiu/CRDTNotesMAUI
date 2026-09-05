// Middleware/RequestValidationMiddleware.cs
public class RequestValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestValidationMiddleware> _logger;

    // Routes that don't require x-user-id verification
    private readonly string[] _excludedPaths = new[]
    {
        "/api/user/login",
        "/api/user/register",
        "/api/user"  // Exclude entire UserController if needed
    };

    public RequestValidationMiddleware(RequestDelegate next, ILogger<RequestValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Skip validation for excluded paths
            if (IsExcludedPath(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Verify x-user-id header
            if (!context.Request.Headers.TryGetValue("x-user-id", out var userIdHeader))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Missing x-user-id header" });
                return;
            }

            if (!Guid.TryParse(userIdHeader.ToString(), out var userId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid x-user-id format" });
                return;
            }

            // Store userId in HttpContext for use in controllers
            context.Items["UserId"] = userId;

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in request");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "An error occurred processing your request",
                message = ex.Message,
                stackTrace = context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment() ? ex.StackTrace : null
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }

    private bool IsExcludedPath(PathString path)
    {
        return _excludedPaths.Any(excludedPath =>
            path.StartsWithSegments(excludedPath, StringComparison.OrdinalIgnoreCase));
    }
}