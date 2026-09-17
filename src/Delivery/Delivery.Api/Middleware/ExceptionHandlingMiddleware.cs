using Delivery.Application.Couriers.Exceptions;
using Delivery.Domain.Couriers.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Delivery.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var problem = Map(exception);

            if (problem.Status >= 500)
                logger.LogError(exception, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);

            context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(problem);
        }
    }

    private static ProblemDetails Map(Exception exception) => exception switch
    {
        ValidationException validationException => new ProblemDetails
        {
            Title = "Validation failed",
            Status = StatusCodes.Status400BadRequest,
            Detail = string.Join("; ", validationException.Errors.Select(e => e.ErrorMessage)),
        },
        CourierNotFoundException => new ProblemDetails
        {
            Title = "Resource not found",
            Status = StatusCodes.Status404NotFound,
            Detail = exception.Message,
        },
        CourierNotAvailableException => new ProblemDetails
        {
            Title = "Conflict",
            Status = StatusCodes.Status409Conflict,
            Detail = exception.Message,
        },
        ArgumentException => new ProblemDetails
        {
            Title = "Invalid request",
            Status = StatusCodes.Status400BadRequest,
            Detail = exception.Message,
        },
        _ => new ProblemDetails
        {
            Title = "An unexpected error occurred",
            Status = StatusCodes.Status500InternalServerError,
        },
    };
}
