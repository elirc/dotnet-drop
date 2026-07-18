using Microsoft.AspNetCore.Mvc;
using NetDropbox.Application.Common;
using NetDropbox.Domain.Common;

namespace NetDropbox.Api.Support;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var status = ex switch
            {
                AppException app => (int)app.StatusCode,
                DomainException => StatusCodes.Status400BadRequest,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status500InternalServerError
            };

            if (status >= 500) logger.LogError(ex, "Unhandled request failure");
            var problem = new ProblemDetails
            {
                Title = status >= 500 ? "An unexpected error occurred." : ex.Message,
                Detail = status >= 500 ? null : ex.Message,
                Status = status,
                Instance = context.Request.Path
            };
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}

