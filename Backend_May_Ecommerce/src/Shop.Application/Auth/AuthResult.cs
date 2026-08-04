using System.Text.Json.Serialization;

namespace Shop.Application.Auth;

public record AuthResult(
    string AccessToken,
    string FullName,
    string Email,
    [property: JsonIgnore] string RefreshToken
);
