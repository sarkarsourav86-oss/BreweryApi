using BreweryApi.Models;
using System.Net;
using System.Text.Json;

namespace BreweryApi.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
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
            await HandleExceptionAsync(context, ex, _logger);
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        ILogger logger)
    {
        var (statusCode, title) = GetStatusCodeAndTitle(exception);

        logger.LogError(
            exception,
            "Unhandled exception occurred. StatusCode: {StatusCode}, Path: {Path}",
            statusCode,
            context.Request.Path);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new ErrorResponse
        {
            Title = title,
            Status = statusCode,
            Detail = statusCode == StatusCodes.Status500InternalServerError ? "An unexpected error occurred." : exception.Message,
            TraceId = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(response);

        await context.Response.WriteAsync(json);
    }

    private static (int StatusCode, string Title) GetStatusCodeAndTitle(Exception exception)
    {
        return exception switch
        {
            ArgumentOutOfRangeException => ((int)HttpStatusCode.BadRequest, "Bad Request"),
            ArgumentException => ((int)HttpStatusCode.BadRequest, "Bad Request"),
            HttpRequestException => ((int)HttpStatusCode.BadGateway, "Bad Gateway"),
            _ => ((int)HttpStatusCode.InternalServerError, "Internal Server Error")
        };
    }
}