using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Tip4Gen.Api.Auth;

public static class AuthExtensions
{
    public const string AdminPolicy = "RequireAdmin";

    public static IServiceCollection AddAuth0(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(Auth0Options.SectionName).Get<Auth0Options>() ?? new Auth0Options();
        services.AddSingleton(options);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                if (options.IsConfigured)
                {
                    jwt.Authority = options.Authority;
                    jwt.Audience = options.Audience;
                    jwt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = options.Authority,
                        ValidateAudience = true,
                        ValidAudience = options.Audience,
                        ValidateLifetime = true,
                        NameClaimType = "sub",
                    };
                }
                else
                {
                    // Auth0 not yet configured — every request is rejected with 401.
                    jwt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = false,
                        SignatureValidator = (_, _) => throw new SecurityTokenException("Auth0 not configured"),
                    };
                }
            });

        services.AddAuthorization(auth =>
        {
            auth.AddPolicy(AdminPolicy, policy =>
            {
                policy.RequireAuthenticatedUser();
                if (!string.IsNullOrWhiteSpace(options.AdminSub))
                {
                    policy.RequireClaim("sub", options.AdminSub);
                }
                else
                {
                    policy.RequireAssertion(_ => false);
                }
            });
        });

        return services;
    }
}
