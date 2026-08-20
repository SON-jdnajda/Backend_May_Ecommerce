using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Shop.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Fail at startup, not on the first request that happens to need a
        // token. A null secret would otherwise surface as a confusing 401.
        var secretKey = configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Configuration 'Jwt:SecretKey' is missing.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),

                    // Default is 5 minutes of leeway, which silently keeps
                    // expired tokens working. Zero makes ExpiryMinutes mean
                    // exactly what it says.
                    ClockSkew = TimeSpan.Zero
                };
            });

        return services;
    }
}
