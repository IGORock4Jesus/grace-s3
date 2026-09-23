using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using GraceS3.AdminProxy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.StackExchangeRedis;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using StackExchange.Redis;

const string CookieName = "__Host-SPA-Auth";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddOpenApi();

builder.Services.Configure<AdminProxyConfiguration>(builder.Configuration);
AdminProxyConfiguration configuration =
	builder.Configuration.Get<AdminProxyConfiguration>()
	?? throw new InvalidProgramException("Admin proxy configuration is not defined");

if (string.IsNullOrWhiteSpace(configuration.RedisConnectionString))
	throw new InvalidOperationException(
		"Set REDIS_CONNECTION_STRING for the AdminProxy session store."
	);
if (string.IsNullOrWhiteSpace(configuration.RedisKeyPrefix))
	throw new InvalidOperationException("REDIS_KEY_PREFIX must not be empty.");

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
	ConnectionMultiplexer.Connect(configuration.RedisConnectionString)
);
builder.Services.AddStackExchangeRedisCache(options =>
{
	options.Configuration = configuration.RedisConnectionString;
	options.InstanceName = configuration.RedisKeyPrefix;
});
builder.Services.AddDataProtection().SetApplicationName(configuration.RedisKeyPrefix);
builder
	.Services.AddOptions<KeyManagementOptions>()
	.Configure<IConnectionMultiplexer>(
		(options, redis) =>
		{
			options.XmlRepository = new RedisXmlRepository(
				() => redis.GetDatabase(),
				configuration.RedisKeyPrefix + "data-protection-keys"
			);
		}
	);
builder.Services.AddHttpClient(); // Нужно для бэк-канал запроса к Keycloak

// Регистрируем наш синглтон для хранения сессий
builder.Services.AddSingleton<ITicketStore, DistributedCacheTicketStore>();

builder
	.Services.AddAuthentication(x =>
	{
		x.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
		x.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
	})
	.AddCookie(
		CookieAuthenticationDefaults.AuthenticationScheme,
		x =>
		{
			x.Cookie.Name = CookieName;
			x.Cookie.HttpOnly = true;
			x.Cookie.SecurePolicy = CookieSecurePolicy.Always;
			x.Cookie.SameSite = SameSiteMode.Strict;

			x.Events.OnRedirectToLogin = context =>
			{
				context.Response.StatusCode = StatusCodes.Status401Unauthorized;
				return Task.CompletedTask;
			};

			// ГЛАВНОЕ: Логика автоматического обновления токена при каждом запросе
			x.Events.OnValidatePrincipal = async context =>
			{
				string? expiresAtStr = context.Properties.GetTokenValue("expires_at");
				if (
					!DateTimeOffset.TryParse(
						expiresAtStr,
						CultureInfo.InvariantCulture,
						DateTimeStyles.RoundtripKind,
						out DateTimeOffset expiresAt
					) || string.IsNullOrEmpty(context.Properties.GetTokenValue("access_token"))
				)
				{
					context.RejectPrincipal();
					await context.HttpContext.SignOutAsync(
						CookieAuthenticationDefaults.AuthenticationScheme
					);
					return;
				}

				// Если до истечения Access Token осталось меньше 30 секунд — обновляем
				if (expiresAt.Subtract(DateTimeOffset.UtcNow).TotalSeconds < 30)
				{
					string? refreshToken = context.Properties.GetTokenValue("refresh_token");
					if (string.IsNullOrEmpty(refreshToken))
					{
						context.RejectPrincipal();
						await context.HttpContext.SignOutAsync(
							CookieAuthenticationDefaults.AuthenticationScheme
						);
						return;
					}

					// Делаем запрос к эндпоинту Keycloak /token
					IHttpClientFactory httpClientFactory =
						context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
					HttpClient client = httpClientFactory.CreateClient();

					string tokenEndpoint =
						$"{configuration.OAuthAuthority}/protocol/openid-connect/token";

					Dictionary<string, string> requestBody = new()
					{
						{ "grant_type", "refresh_token" },
						{ "client_id", configuration.OAuthClientId },
						{ "client_secret", configuration.OAuthClientSecret },
						{ "refresh_token", refreshToken },
					};

					HttpResponseMessage response = await client.PostAsync(
						tokenEndpoint,
						new FormUrlEncodedContent(requestBody)
					);

					if (response.IsSuccessStatusCode)
					{
						using JsonDocument jsonDocument = await JsonDocument.ParseAsync(
							await response.Content.ReadAsStreamAsync()
						);
						JsonElement root = jsonDocument.RootElement;

						// Извлекаем новые токены
						string? newAccessToken = root.GetProperty("access_token").GetString();
						string? newRefreshToken = root.GetProperty("refresh_token").GetString();
						int expiresIn = root.GetProperty("expires_in").GetInt32();

						DateTimeOffset newExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);

						// Записываем обновленные токены обратно в свойства аутентификации
						context.Properties.UpdateTokenValue("access_token", newAccessToken!);
						context.Properties.UpdateTokenValue("refresh_token", newRefreshToken!);
						context.Properties.UpdateTokenValue(
							"expires_at",
							newExpiresAt.ToString("o", CultureInfo.InvariantCulture)
						);

						// Уведомляем систему, что нужно обновить куку/сессию
						context.ShouldRenew = true;
					}
					else
					{
						// Если refresh token устарел или невалиден — разлогиниваем пользователя
						context.RejectPrincipal();
						await context.HttpContext.SignOutAsync(
							CookieAuthenticationDefaults.AuthenticationScheme
						);
					}
				}
			};
		}
	)
	.AddOpenIdConnect(
		OpenIdConnectDefaults.AuthenticationScheme,
		x =>
		{
			x.Authority = configuration.OAuthAuthority;
			x.ClientId = configuration.OAuthClientId;
			x.ClientSecret = configuration.OAuthClientSecret;
			x.ResponseType = OpenIdConnectResponseType.Code;

			x.SaveTokens = true;
			x.GetClaimsFromUserInfoEndpoint = true;

			x.TokenValidationParameters = new()
			{
				NameClaimType = "preferred_username",
				RoleClaimType = "roles",
			};

			// Запрашиваем offline_access, если вам нужны долгоживущие Refresh-токены
			x.Scope.Add("openid");
			x.Scope.Add("offline_access");
		}
	);

builder
	.Services.AddOptions<CookieAuthenticationOptions>(
		CookieAuthenticationDefaults.AuthenticationScheme
	)
	.Configure<ITicketStore>((options, store) => options.SessionStore = store);
builder.Services.AddAuthorization();

WebApplication app = builder.Build();

app.UseRouting();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

// 4. Эндпоинты для логина / логаута (для React)
app.MapGet(
	"/auth/login",
	async context =>
	{
		// Триггерит OIDC Challenge и перенаправляет в Keycloak
		await context.ChallengeAsync(
			OpenIdConnectDefaults.AuthenticationScheme,
			new AuthenticationProperties
			{
				RedirectUri = LoginReturnUrl.GetLocalPath(context.Request.Query["returnUrl"]),
			}
		);
	}
);

app.MapGet(
	"/auth/logout",
	async context =>
	{
		await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
		await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
	}
);

app.MapGet(
		"/auth/me",
		(HttpContext context) =>
		{
			// Позволяет React узнать, авторизован ли пользователь
			if (context.User.Identity?.IsAuthenticated == true)
			{
				return Results.Ok(
					new GetMeResponse(
						context.User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value,
						context.User.Identity.Name ?? string.Empty,
						context.User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty
					)
				);
			}

			return Results.Unauthorized();
		}
	)
	.Produces<GetMeResponse>(StatusCodes.Status200OK)
	.Produces(StatusCodes.Status401Unauthorized);

app.MapReverseProxy(proxyPipeline =>
{
	proxyPipeline.Use(
		async (context, next) =>
		{
			if (
				context.Request.Path.StartsWithSegments("/api")
				&& context.User.Identity?.IsAuthenticated != true
			)
			{
				context.Response.StatusCode = StatusCodes.Status401Unauthorized;
				return;
			}

			// Извлекаем токен из Cookie-сессии текущего запроса
			string? accessToken = await context.GetTokenAsync("access_token");
			if (!string.IsNullOrWhiteSpace(accessToken))
			{
				// Добавляем Bearer токен в заголовок для downstream API
				context.Request.Headers.Authorization = $"Bearer {accessToken}";
			}

			await next(context);
		}
	);
});

await app.RunAsync();
