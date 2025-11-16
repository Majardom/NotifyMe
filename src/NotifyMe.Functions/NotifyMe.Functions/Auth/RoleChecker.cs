using System.Security.Claims;
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
	public string? auth_typ { get; set; }
	public string? name_typ { get; set; }
	public string? role_typ { get; set; }
	public List<ClientPrincipalClaim> claims { get; set; } = new();
}

public class ClientPrincipalClaim
{
	public string typ { get; set; } = "";
	public string val { get; set; } = "";
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

		var principal = JsonConvert.DeserializeObject<ClientPrincipal>(json); ;

		var roleType = !string.IsNullOrEmpty(principal.role_typ)
			? principal.role_typ
			: "roles";

		var roles = principal.claims
			.Where(c => c.typ == roleType || c.typ == "roles")
			.Select(c => c.val)
			.ToHashSet();

		message = $"x-ms-client-principal was found with following roles claims {string.Join(',', roles)} ";

		return roles.Contains(role.ToLower());
	}
}
