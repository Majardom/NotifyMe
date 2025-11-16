using System.Text;
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

		if (!req.Headers.TryGetValues("x-ms-client-principal", out var headerValues)) {
			message = "Header x-ms-client-principal is not found.";
			return false;
		}

		var encoded = headerValues.FirstOrDefault();
		var decodedBytes = Convert.FromBase64String(encoded!);
		var json = Encoding.UTF8.GetString(decodedBytes);

		var principal = JsonConvert.DeserializeObject<ClientPrincipal>(json);

		var roles = principal?.Claims
			.Where(c => c.Type.EndsWith("/claims/role"))
			.Select(c => c.Value)
			.ToList();
			
		var rolesString = roles != null ? string.Join(',', roles) : "Principal null";
		message = $"x-ms-client-principal was found with following roles claims {rolesString} ";

		return roles == null ? false : roles.Contains(role.ToLower());
	}
}
