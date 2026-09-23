using System.Security.Claims;
using System.Text;
using GraceS3.AdminProxy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;

var cache = new TestCache();
var protection = new EphemeralDataProtectionProvider();
var first = new DistributedCacheTicketStore(cache, protection);
var second = new DistributedCacheTicketStore(cache, protection);
var expires = DateTimeOffset.UtcNow.AddHours(1);
var properties = new AuthenticationProperties { ExpiresUtc = expires };
properties.StoreTokens([new AuthenticationToken { Name = "refresh_token", Value = "secret-refresh-token" }]);
var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(
	[new Claim(ClaimTypes.NameIdentifier, "admin")], "Cookies")), properties, "Cookies");
var key = await first.StoreAsync(ticket);
Check((await second.RetrieveAsync(key))?.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value == "admin", "another store reads the session");
Check((await second.RetrieveAsync(key))?.Properties.GetTokenValue("refresh_token") == "secret-refresh-token", "tokens round trip");
Check(cache.Expirations[key] == properties.ExpiresUtc, "TTL follows cookie expiration");
Check(!Encoding.UTF8.GetString(cache.Values[key]).Contains("secret-refresh-token"), "tokens are protected in cache");
var otherKey = await first.StoreAsync(ticket);
Check(key != otherKey, "session IDs are unique");
properties.ExpiresUtc = expires.AddHours(1);
await second.RenewAsync(key, ticket);
Check((await first.RetrieveAsync(key))?.Properties.ExpiresUtc == properties.ExpiresUtc, "renewal is visible across stores");
await second.RemoveAsync(key);
Check(await first.RetrieveAsync(key) is null, "logout removes shared session");
properties.ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
await second.RenewAsync(otherKey, ticket);
Check(await first.RetrieveAsync(otherKey) is null, "expired renewal removes session");
cache.Values["broken"] = [1, 2, 3];
Check(await first.RetrieveAsync("broken") is null, "invalid protected ticket is rejected");
Check(!cache.Values.ContainsKey("broken"), "invalid ticket is removed");
cache.FailReads = true;
try { await first.RetrieveAsync("unavailable"); throw new Exception("Cache failure was hidden"); }
catch (IOException) { Console.WriteLine("PASS: cache outage is not treated as logout"); }
foreach (var bad in new string?[] { null, "", "https://other.test", "//other.test", "/\\other.test", "/files\r\n", "/auth/login" })
	Check(LoginReturnUrl.GetLocalPath(bad) == "/", "reject invalid return URL");
Check(LoginReturnUrl.GetLocalPath("/files?sort=name") == "/files?sort=name", "preserve local return URL");
Console.WriteLine("All AdminProxy checks passed without Redis.");

static void Check(bool condition, string description)
{
	if (!condition) throw new Exception(description);
	Console.WriteLine("PASS: " + description);
}

sealed class TestCache : IDistributedCache
{
	public Dictionary<string, byte[]> Values { get; } = [];
	public Dictionary<string, DateTimeOffset?> Expirations { get; } = [];
	public bool FailReads { get; set; }
	public byte[]? Get(string key) => FailReads ? throw new IOException("Cache unavailable") : Values.GetValueOrDefault(key);
	public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));
	public void Set(string key, byte[] value, DistributedCacheEntryOptions options) { Values[key] = value; Expirations[key] = options.AbsoluteExpiration; }
	public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) { Set(key, value, options); return Task.CompletedTask; }
	public void Remove(string key) { Values.Remove(key); Expirations.Remove(key); }
	public Task RemoveAsync(string key, CancellationToken token = default) { Remove(key); return Task.CompletedTask; }
	public void Refresh(string key) { }
	public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
}
