using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace OrderBookApi.ExceptionHandler;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "An unprocessed exception occurred");

        var response = httpContext.Response;
        response.ContentType = "application/json";

        var problemDetails = exception switch
        {
            JsonException jsonEx => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Invalid JSON format",
                Status = 400,
                Detail = ExtractJsonErrorMessage(jsonEx)
            },

            BadHttpRequestException badRequestEx
                when badRequestEx.InnerException is JsonException jsonEx => new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Invalid request format",
                    Status = 400,
                    Detail = ExtractJsonErrorMessage(jsonEx)
                },

            ArgumentException argEx => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Invalid argument",
                Status = 400,
                Detail = argEx.Message
            },

            _ => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.2",
                Title = "An error occurred",
                Status = 500,
                Detail = "Internal server error"
            }
        };

        response.StatusCode = problemDetails.Status ?? 500;
        await response.WriteAsync(JsonSerializer.Serialize(problemDetails), cancellationToken);

        return true;
    }

    private static string ExtractJsonErrorMessage(JsonException jsonEx)
    {
        var message = jsonEx.Message;

        // Extract enum-specific error messages
        if (message.Contains("could not be converted") && message.Contains("type"))
        {
            return "Invalid order type. Allowed values: Buy, Sell";
        }

        return "Invalid JSON format in request body";
    }
}
