using FacilitiesMgmt.Application.Common;
using FacilitiesMgmt.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FacilitiesMgmt.WebApi.Infrastructure;

/// <summary>
/// One place that decides how a domain or application failure becomes an HTTP response.
/// </summary>
/// <remarks>
/// Centralising this is what lets the Domain layer throw meaningful exceptions without
/// ever referencing a status code, and it means the 404-not-403 decision for
/// cross-tenant access is made once, here, rather than being re-litigated in every
/// controller action — where one inconsistent 403 would be enough to turn the API into
/// an existence oracle for other tenants' data.
/// </remarks>
public sealed class DomainExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = Map(exception);
        if (problem is null)
        {
            // Unrecognised: let the default pipeline produce a 500 and log the detail
            // server-side. Never leak an unexpected exception message to the client.
            logger.LogError(exception, "Unhandled exception processing {Path}", httpContext.Request.Path);
            return false;
        }

        // Expected failures are information, not incidents.
        logger.LogInformation(
            "{Status} on {Path}: {Title}", problem.Status, httpContext.Request.Path, problem.Title);

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem
        });
    }

    private static ProblemDetails? Map(Exception exception) => exception switch
    {
        // 404, never 403. A 403 would confirm the resource exists, letting one tenant
        // enumerate another's ids by watching status codes. See NotFoundException.
        NotFoundException e => Problem(StatusCodes.Status404NotFound, "Not found", e.Message),

        AuthenticationFailedException e => Problem(StatusCodes.Status401Unauthorized, "Authentication failed", e.Message),

        // The caller may not do this at all: wrong role, or approving their own request.
        BusinessRuleViolationException { IsAuthorizationFailure: true } e =>
            Problem(StatusCodes.Status403Forbidden, "Not permitted", e.Message),

        // The caller may do this, but not to a resource in this state.
        BusinessRuleViolationException e => Problem(StatusCodes.Status409Conflict, "Request rejected", e.Message),

        IllegalStateTransitionException e => Problem(StatusCodes.Status409Conflict, "Illegal state transition", e.Message),

        ValidationException e => ValidationProblem(e),

        _ => null
    };

    private static ProblemDetails Problem(int status, string title, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail
    };

    private static ProblemDetails ValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Detail = "One or more fields are invalid."
        };

        problem.Extensions["errors"] = errors;
        return problem;
    }
}
