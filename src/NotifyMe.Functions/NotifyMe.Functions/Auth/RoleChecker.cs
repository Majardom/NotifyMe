using System.Security.Claims;
using Microsoft.Azure.Functions.Worker.Http;

namespace NotifyMe.Functions.Auth;

public enum QueueRole 
{
	Manager,
	Member
}

public class RoleChecker
{
	public static bool HasRole(HttpRequestData req, string role)
	{
		var envVariable = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");

		if (string.IsNullOrEmpty(envVariable) || envVariable == "Development")
			return true;

		if (!req.Identities.Any())
			return false;

		var claims = req.Identities
		  .SelectMany(i => i.Claims)
		  .Where(c => c.Type == "roles");

		return claims.Any(c => c.Value == role);
	}
}
