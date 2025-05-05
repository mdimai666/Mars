namespace Mars.Host.Services.MarsSSOClient;

public class OpenIDAuthException : Exception
{
    /// <summary>
    /// </summary>
    /// <see cref="OpenIdErrorCodesForAuthorizationEndpointErrors"/>
    public string Error { get; set; }
    public string ErrorDescription { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="error">
    /// An error code string that can be used to classify types of errors, and to react to errors. This part of the error is provided so that the app can react appropriately to the error, but doesn't explain in depth why an error occurred.
    /// <see cref="OpenIdErrorCodesForAuthorizationEndpointErrors"/>
    /// </param>
    /// <param name="error_description">
    /// A specific error message that can help a developer identify the cause of an authentication error. This part of the error contains most of the useful information about why the error occurred.
    /// </param>
    /// <param name="innerException"></param>
    public OpenIDAuthException(string error, string error_description, Exception? innerException = null) : base(error, innerException)
    {
        this.Error = error;
        this.ErrorDescription = error_description;
    }
}
