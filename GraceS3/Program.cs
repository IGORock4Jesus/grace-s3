using GraceS3.Buckets.Endpoints;
using GraceS3.Configs;
using GraceS3.Data;
using GraceS3.Files;
using GraceS3.Services;
using Keycloak.AuthServices.Authorization;
using Keycloak.AuthServices.Common;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration().WriteTo.Console().WriteTo.Debug().CreateLogger();
builder.Host.UseSerilog(Log.Logger);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
	options.AddDocumentTransformer(
		(document, context, cancellationToken) =>
		{
			SwaggerConfig config = context
				.ApplicationServices.GetRequiredService<IOptions<SwaggerConfig>>()
				.Value;

			string authUrl =
				$"{config.OAuthUri.TrimEnd('/')}/realms/{config.OAuthRealm}/protocol/openid-connect/auth";
			string tokenUrl =
				$"{config.OAuthUri.TrimEnd('/')}/realms/{config.OAuthRealm}/protocol/openid-connect/token";

			OpenApiSecurityScheme scheme = new()
			{
				Type = SecuritySchemeType.OAuth2,
				Flows = new OpenApiOAuthFlows
				{
					AuthorizationCode = new OpenApiOAuthFlow
					{
						AuthorizationUrl = new Uri(authUrl),
						TokenUrl = new Uri(tokenUrl),
						Scopes = new Dictionary<string, string>
						{
							{ "openid", "OpenID Connect область" },
							{ "profile", "Доступ к профилю" },
						},
					},
				},
			};

			document.Components ??= new OpenApiComponents();
			document.Components.SecuritySchemes ??=
				new Dictionary<string, IOpenApiSecurityScheme>();
			document.Components.SecuritySchemes?.Add("KeycloakOAuth", scheme);

			OpenApiSecurityRequirement requirement = new()
			{
				{
					new OpenApiSecuritySchemeReference("KeycloakOAuth", document),
					["openid", "profile"]
				},
			};

			document.Security ??= [];
			document.Security.Add(requirement);

			return Task.CompletedTask;
		}
	);
});

builder.Services.Configure<AuthConfig>(builder.Configuration.GetSection("Auth"));

builder.Services.AddKeycloakWebApiAuthentication(
	x => ConfigureKeycloak(x, builder),
	options =>
	{
		AuthConfig config =
			builder.Configuration.GetSection("Auth").Get<AuthConfig>()
			?? throw new InvalidProgramException("Auth configuration is not defined");
		string issuer = $"{config.Uri.TrimEnd('/')}/realms/{config.Realm}";

		// Keycloak token issuers have no trailing slash after the realm name.
		options.Authority = issuer;
		options.TokenValidationParameters.ValidIssuer = issuer;
	}
);
builder.Services.AddAuthorization().AddKeycloakAuthorization(x => ConfigureKeycloak(x, builder));

static void ConfigureKeycloak(KeycloakInstallationOptions x, WebApplicationBuilder builder)
{
	AuthConfig config =
		builder.Configuration.GetSection("Auth").Get<AuthConfig>()
		?? throw new InvalidProgramException("Auth configuration is not defined");

	x.AuthServerUrl = config.Uri;
	x.Realm = config.Realm;
	x.Resource = config.ClientID;
	x.Credentials.Secret = config.ClientSecret;
	x.VerifyTokenAudience = false; // TODO: enable on prod
}

builder.Services.Configure<DatabaseConfig>(builder.Configuration.GetSection("Database"));

builder.Services.AddScoped<UserService>().AddSingleton<Database>();

builder.Services.Configure<SwaggerConfig>(builder.Configuration.GetSection("Swagger"));

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.MapSwaggerUI(
		null,
		options =>
		{
			options.SwaggerEndpoint("/openapi/v1.json", "My API V1");
			options.DisplayOperationId();

			using IServiceScope scope = app.Services.CreateScope();
			SwaggerConfig config = scope
				.ServiceProvider.GetRequiredService<IOptions<SwaggerConfig>>()
				.Value;

			options.OAuthClientId(config.OAuthClientID);
			options.OAuthAppName("GraceS3 Swagger");
			options.OAuthUsePkce();
			options.OAuthScopes("openid", "profile");
			options.EnablePersistAuthorization();

			// Swagger UI derives /swagger/oauth2-redirect.html from its browser URL.
			// Register that callback on the public Keycloak client.
		}
	);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapBuckets();
app.MapFiles();

await app.RunAsync();
