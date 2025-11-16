using System.Text;
using Microsoft.Azure.Cosmos.Core;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;

namespace NotifyMe.Functions.Auth;

public enum QueueRole 
{
	Manager,
	Member
}

public class ClientPrincipal
{
	public string IdentityProvider { get; set; }
	public string UserId { get; set; }
	public string UserDetails { get; set; }

	// SWA built-in roles ("authenticated", "anonymous")
	public List<string> UserRoles { get; set; }

	// Real claims passed through AAD (including role)
	public List<ClientPrincipalClaim> Claims { get; set; }
}

public class ClientPrincipalClaim
{
	[JsonProperty("typ")]
	public string Type { get; set; }

	[JsonProperty("val")]
	public string Value { get; set; }
}

public class RoleChecker
{
	public static bool HasRole(HttpRequestData req, string role, out string message)
	{
		var envVariable = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");

		message = string.Empty;

		if (string.IsNullOrEmpty(envVariable) || envVariable == "Development")
			return true;

		var json = string.Empty;
		try {
			if (!req.Headers.TryGetValues("x-client-aad-roles", out var headerValues))
			{
				message = "Header x-client-aad-roles is not found.";
				return false;
			}

			var value = headerValues.FirstOrDefault();

			var roles = JsonConvert.DeserializeObject<string[]>(value!);

			var rolesString = roles != null ? string.Join(',', roles) : "Roles not found";
			message = $"x-client-aad-roles was found with following roles claims {rolesString} ";

			return roles == null ? false : roles.Contains(role.ToLower());
		}
		catch(Exception ex) {
			message = json + ex.ToString();
			return false;
		}
	
	}
}
