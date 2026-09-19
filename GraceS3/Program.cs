using System.Security.Claims;
using System.Text.Json.Serialization;
using GraceS3;
using GraceS3.Data;
using GraceS3.Endpoints.Clients;
using GraceS3.Files.Endpoints;
using GraceS3.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Keycloak.AuthServices.Authorization;
using Keycloak.AuthServices.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration().WriteTo.Console().WriteTo.Debug().CreateLogger();
builder.Host.UseSerilog(Log.Logger);

builder.Services.ConfigureHttpJsonOptions(options =>
{
	options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});

ApplicationConfiguration config =
	builder.Configuration.Get<ApplicationConfiguration>()
	?? throw new InvalidProgramException("Application configuration is not defined");

builder.Services.Configure<ApplicationConfiguration>(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
	options.AddDocumentTransformer(
		(document, context, cancellationToken) =>
		{
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

builder.Services.AddKeycloakWebApiAuthentication(
	x => ConfigureKeycloak(x, builder),
	options =>
	{
		string issuer = $"{config.OAuthUri.TrimEnd('/')}/realms/{config.OAuthRealm}";

		options.Authority = issuer;
		options.TokenValidationParameters.ValidIssuer = issuer;
	}
);
builder.Services.AddAuthorization().AddKeycloakAuthorization(x => ConfigureKeycloak(x, builder));

void ConfigureKeycloak(KeycloakInstallationOptions x, WebApplicationBuilder builder)
{
	x.AuthServerUrl = config.OAuthUri;
	x.Realm = config.OAuthRealm;
	x.Resource = config.OAuthClientID;
	x.Credentials.Secret = config.OAuthClientSecret;
	x.VerifyTokenAudience = false; // TODO: enable on prod
}

builder.Services.AddScoped<UserService>().AddSingleton<Database>().AddScoped<DiskService>();

ConfigureHangfire();
void ConfigureHangfire()
{
	builder.Services.AddHangfire(configuration =>
		configuration
			.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
			.UseSimpleAssemblyNameTypeSerializer()
			.UseRecommendedSerializerSettings()
			.UsePostgreSqlStorage(x =>
			{
				x.UseNpgsqlConnection(config.DatabaseConnectionString);
			})
	);

	builder.Services.AddHangfireServer();
}

ConfigureDatabase();
void ConfigureDatabase()
{
	builder.Services.AddDbContextPool<Database>(x =>
	{
		x.UseNpgsql(config.DatabaseConnectionString);
	});
}

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

			options.OAuthClientId(config.SwaggerOAuthClientID);
			options.OAuthAppName("GraceS3 Swagger");
			options.OAuthUsePkce();
			options.OAuthScopes("openid", "profile");
			options.EnablePersistAuthorization();

			// Swagger UI derives /swagger/oauth2-redirect.html from its browser URL.
			// Register that callback on the public Keycloak client.
		}
	);

	app.UseHangfireDashboard();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapCliensEndpoints();
app.MapObjectsEndpoints();

await app.RunAsync();
