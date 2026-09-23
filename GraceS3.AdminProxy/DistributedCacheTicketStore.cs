using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;

namespace GraceS3.AdminProxy;

public sealed class DistributedCacheTicketStore(
	IDistributedCache cache, IDataProtectionProvider protectionProvider) : ITicketStore
{
	private readonly IDataProtector protector = protectionProvider.CreateProtector(
		"GraceS3.AdminProxy.AuthenticationTickets.v1");

	public async Task<string> StoreAsync(AuthenticationTicket ticket)
	{
		string key = "session:" + Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
		await RenewAsync(key, ticket);
		return key;
	}

	public async Task RenewAsync(string key, AuthenticationTicket ticket)
	{
		var expires = ticket.Properties.ExpiresUtc ?? DateTimeOffset.UtcNow.AddHours(8);
		if (expires <= DateTimeOffset.UtcNow)
		{
			await RemoveAsync(key);
			return;
		}
		await cache.SetAsync(key, protector.Protect(TicketSerializer.Default.Serialize(ticket)),
			new DistributedCacheEntryOptions { AbsoluteExpiration = expires });
	}

	public async Task<AuthenticationTicket?> RetrieveAsync(string key)
	{
		byte[]? data = await cache.GetAsync(key);
		if (data is null) return null;
		AuthenticationTicket? ticket;
		try { ticket = TicketSerializer.Default.Deserialize(protector.Unprotect(data)); }
		catch (CryptographicException)
		{
			await RemoveAsync(key);
			return null;
		}
		if (ticket is null || ticket.Properties.ExpiresUtc <= DateTimeOffset.UtcNow)
		{
			await RemoveAsync(key);
			return null;
		}
		return ticket;
	}

	public Task RemoveAsync(string key) => cache.RemoveAsync(key);
}
