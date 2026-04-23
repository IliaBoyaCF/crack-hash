using Manager.Abstractions.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Manager.Api.Exceptions;

public class ApiExceptionFilterAttribute : ExceptionFilterAttribute
{
    private readonly ILogger<ApiExceptionFilterAttribute> _logger;
    private readonly IHostEnvironment _environment;

    public ApiExceptionFilterAttribute(
        ILogger<ApiExceptionFilterAttribute> logger,
        IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public override void OnException(ExceptionContext context)
    {
        HandleException(context);
        context.ExceptionHandled = true;
    }

    private void HandleException(ExceptionContext context)
    {
        var exception = context.Exception;

        switch (exception)
        {
            case NotFoundException:
            case NoSuchElementException:
                _logger.LogWarning(exception, "Entity not found");
                context.Result = CreateProblemDetails(
                    exception,
                    StatusCodes.Status404NotFound,
                    exception.Message);
                break;

            case ValidationException validation:
                _logger.LogWarning(validation, "Validation failed");
                context.Result = CreateProblemDetails(
                    validation,
                    StatusCodes.Status400BadRequest,
                    validation.Message);
                break;
            case QueueOverflowException queueOverflow:
                _logger.LogWarning(queueOverflow, "Request queue overflowed.");
                context.Result = CreateProblemDetails(
                    queueOverflow,
                    StatusCodes.Status503ServiceUnavailable,
                    queueOverflow.Message);
                break;

            default:
                _logger.LogError(exception, "Unhandled exception");
                context.Result = CreateProblemDetails(
                    exception,
                    StatusCodes.Status500InternalServerError,
                    _environment.IsDevelopment()
                        ? exception.Message
                        : "An internal error occurred");
                break;
        }
    }

    private JsonResult CreateProblemDetails(
        Exception exception,
        int statusCode,
        string message)
    {
        var problem = new
        {
            title = GetTitleForStatusCode(statusCode),
            status = statusCode,
            detail = message,
            timestamp = DateTime.UtcNow,
        };

        return new JsonResult(problem) { StatusCode = statusCode };
    }

    private string GetTitleForStatusCode(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        404 => "Not Found",
        503 => "Service unavailable",
        500 => "Internal Server Error",
        _ => "Error"
    };
}