using FluentValidation;
using Mars.SSO.Host.OAuth.Dto;
using Mars.SSO.Host.OAuth.interfaces;

namespace Mars.SSO.Host.OAuth.Validators;

public class AuthorizeRequestValidator : AbstractValidator<OAuthAuthorizeRequest>
{
    public AuthorizeRequestValidator(IOAuthClientStore clients)
    {
        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("client_id is required")
            .Must(id => clients.FindClientById(id) != null)
            .WithMessage("Unknown client_id");

        RuleFor(x => x.RedirectUri)
            .NotEmpty().WithMessage("redirect_uri is required")
            .Must((req, uri) =>
            {
                var client = clients.FindClientById(req.ClientId);
                return client != null &&
                        (client.RedirectUris == "*"
                        || client.RedirectUris.Split(';', StringSplitOptions.TrimEntries).Contains(uri!, StringComparer.OrdinalIgnoreCase));
            })
            .WithMessage("Invalid redirect_uri");

        RuleFor(x => x.ResponseType)
            .NotEmpty().WithMessage("response_type is required")
            .Must(rt => new[] { "code", "token", "id_token" }.Contains(rt))
            .WithMessage("Unsupported response_type");

        //RuleFor(x => x.Scope)
        //    .Custom((scope, context) =>
        //    {
        //        if (string.IsNullOrWhiteSpace(scope)) return;

        //        var req = context.InstanceToValidate;
        //        var client = clients.FindClientById(req.ClientId);
        //        if (client == null) return;

        //        var requested = scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        //        var invalid = requested.Except(client.AllowedScopes, StringComparer.OrdinalIgnoreCase).ToList();
        //        if (invalid.Any())
        //        {
        //            context.AddFailure($"Invalid scope(s): {string.Join(", ", invalid)}");
        //        }
        //    });

        //RuleFor(x => x.Nonce)
        //    .NotEmpty()
        //    .When(x => x.ResponseType != null && x.ResponseType.Contains("id_token"))
        //    .WithMessage("nonce is required for OpenID Connect (id_token flow)");
    }
}
