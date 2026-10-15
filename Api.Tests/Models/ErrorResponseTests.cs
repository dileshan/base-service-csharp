using Api.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Api.Tests.Models;

public sealed class ErrorResponseTests
{
    [Fact]
    public void FromModelState_CombinesErrorsAndCamelCasesTargets()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Latitude", "Latitude is required.");
        modelState.AddModelError("ForecastDays", "ForecastDays must be between 1 and 16.");

        var error = ErrorResponse.FromModelState(modelState);

        Assert.Equal(400, error.Code);
        Assert.Equal("Latitude is required. ForecastDays must be between 1 and 16.", error.Message);
        Assert.Equal("latitude,forecastDays", error.Target);
    }

    [Fact]
    public void FromModelState_ErrorWithoutMessage_UsesDefault()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError(
            string.Empty,
            new Exception("binder failure"),
            new EmptyModelMetadataProvider().GetMetadataForType(typeof(string))
        );

        var error = ErrorResponse.FromModelState(modelState);

        Assert.Equal("The request is invalid.", error.Message);
        Assert.Equal(string.Empty, error.Target);
    }
}
