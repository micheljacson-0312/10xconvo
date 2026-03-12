namespace TenXConvo.API.Middleware;

// ═══════════════════════════════════════════════════════════════════════════
//  ERROR HANDLING MIDDLEWARE
//  Auto-logs every unhandled exception to the ErrorLog table
//  Matches ErrorLog screen: ActionName, ControllerName, Code, ErrorMessage
// ═══════════════════════════════════════════════════════════════════════════
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        // Parse controller/action from route data
        var routeData      = context.GetRouteData();
        var controllerName = routeData?.Values["controller"]?.ToString() ?? "Unknown";
        var actionName     = routeData?.Values["action"]?.ToString()     ?? "Unknown";

        // Log to ErrorLog table via scoped DbContext
        using var scope = context.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenXConvo.Infrastructure.Data.AppDbContext>();
        db.ErrorLogs.Add(new TenXConvo.Domain.Entities.ErrorLog
        {
            ActionName     = actionName,
            ControllerName = controllerName,
            Code           = 500,
            ErrorMessage   = ex.Message,
            StackTrace     = ex.StackTrace,
            RequestPath    = context.Request.Path,
            UserId         = context.User?.FindFirst("sub")?.Value,
            CreatedOn      = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        context.Response.StatusCode  = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            success = false,
            message = "An internal error occurred.",
            code    = 500
        });
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  TENANT MIDDLEWARE
//  Reads "connection" claim from JWT → switches DB connection string
//  This is how QA / Production switching works from Login Step 2
//
//  Login Step 2 dropdown: QA | Production
//  JWT claim: connection = "QA" | "Production"
//  This middleware reads that claim and sets the correct connection string
//  on the DbContext for the duration of the request
// ═══════════════════════════════════════════════════════════════════════════
public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IConfiguration config)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var connection   = context.User.FindFirst("connection")?.Value   ?? "Production";
            var locationId   = context.User.FindFirst("location")?.Value;
            var fiscalYearId = context.User.FindFirst("fiscalYear")?.Value;

            // Store in HttpContext.Items for use in controllers/services
            context.Items["Connection"]   = connection;
            context.Items["LocationId"]   = locationId;
            context.Items["FiscalYearId"] = fiscalYearId;

            // Override DbContext connection string based on "connection" claim
            // QA → ConnectionStrings:QA  |  Production → ConnectionStrings:Production
            var connString = config.GetConnectionString(connection)
                          ?? config.GetConnectionString("Default")!;
            context.Items["ConnectionString"] = connString;
        }

        await _next(context);
    }
}
