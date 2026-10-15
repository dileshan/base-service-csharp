using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Api.Models;

/// <summary>Body returned for every error response (400, 404, 500).</summary>
public sealed record ErrorResponse(int Code, string Message, string Target)
{
    /// <summary>Builds a 400 response from failed model binding/validation (used by the [ApiController] filter).</summary>
    public static ErrorResponse FromModelState(ModelStateDictionary modelState)
    {
        // ModelStateDictionary never yields null entries; only fields with errors are reported.
        var invalid = modelState.Where(entry => entry.Value!.Errors.Count > 0).ToList();

        var message = string.Join(
            " ",
            invalid.SelectMany(entry => entry.Value!.Errors).Select(DescribeError)
        );
        var target = string.Join(",", invalid.Select(entry => ToCamelCase(entry.Key)));

        return new ErrorResponse(StatusCodes.Status400BadRequest, message, target);
    }

    private static string DescribeError(ModelError error) =>
        string.IsNullOrWhiteSpace(error.ErrorMessage)
            ? "The request is invalid."
            : error.ErrorMessage;

    private static string ToCamelCase(string key) =>
        string.IsNullOrEmpty(key) ? key : char.ToLowerInvariant(key[0]) + key[1..];
}
