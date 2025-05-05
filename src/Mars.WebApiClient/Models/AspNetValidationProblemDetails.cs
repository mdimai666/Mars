using System.Text.Json.Serialization;

namespace Mars.WebApiClient.Models;

public class AspNetValidationProblemDetails : AspNetResponseProblemDetails
{
    //
    // Summary:
    //     Gets the validation errors associated with this instance of Microsoft.AspNetCore.Http.HttpValidationProblemDetails.
    [JsonPropertyName("errors")]
    public IDictionary<string, string[]> Errors { get; set; }

    //
    // Summary:
    //     Initializes a new instance of Microsoft.AspNetCore.Mvc.ValidationProblemDetails.
    public AspNetValidationProblemDetails()
    {
        Errors = new Dictionary<string, string[]>();
    }
    //
    // Summary:
    //     Initializes a new instance of Microsoft.AspNetCore.Mvc.ValidationProblemDetails
    //     using the specified modelState.
    //
    // Parameters:
    //   modelState:
    //     Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary containing the validation
    //     errors.
    //public AspNetValidationProblemDetails(ModelStateDictionary modelState);
    //
    // Summary:
    //     Initializes a new instance of Microsoft.AspNetCore.Mvc.ValidationProblemDetails
    //     using the specified errors.
    //
    // Parameters:
    //   errors:
    //     The validation errors.
    public AspNetValidationProblemDetails(IDictionary<string, string[]> errors)
        => Errors = errors;


}
