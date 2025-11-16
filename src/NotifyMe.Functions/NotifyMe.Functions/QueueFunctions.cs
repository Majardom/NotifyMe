using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NotifyMe.Functions.Auth;
using NotifyMe.Functions.Models;
using NotifyMe.Functions.Services;

namespace NotifyMe.Functions;
public class QueueFunctions
{
	private readonly ILogger _logger;
	private readonly CosmosDbService _cosmosService;

	public QueueFunctions(ILoggerFactory loggerFactory, CosmosDbService cosmosService)
	{
		_logger = loggerFactory.CreateLogger<QueueFunctions>();
		_cosmosService = cosmosService;
	}

	[Function("CreateQueue")]
	public async Task<HttpResponseData> CreateQueue([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "queues")] HttpRequestData req)
	{
		var response = await CheckRole(req, QueueRole.Member);
		if (response != null)
			return response;

		_logger.LogInformation("Received request to create queue item.");

		// Read request body
		string body = await new StreamReader(req.Body).ReadToEndAsync();

		// Deserialize JSON to model
		var requestData = JsonConvert.DeserializeObject<QueueItem>(body);

		if (requestData == null || string.IsNullOrWhiteSpace(requestData.UserEmail))
		{
			var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
			await badResponse.WriteStringAsync("Invalid request: userEmail is required.");
			return badResponse;
		}

		// Save to Cosmos DB
		var alreadyExists = await _cosmosService.AddUserToQueueAsync(requestData);

		if (alreadyExists) 
		{
			var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
			await badResponse.WriteStringAsync("Invalid request: userEmail is already registered.");
			return badResponse;
		}

		// Response
		response = req.CreateResponse(HttpStatusCode.Created);
		await response.WriteStringAsync("Queue item created successfully.");

		return response;
	}

	[Function("GetQueues")]
	public async Task<HttpResponseData> GetQueues(
	[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "queues")] HttpRequestData req)
	{
		var response = await CheckRole(req, QueueRole.Manager);
		if (response != null)
			return response;

		_logger.LogInformation("Fetching all queue items.");

		var items = await _cosmosService.GetAllQueueItemsAsync();

		response = req.CreateResponse(HttpStatusCode.OK);
		response.Headers.Add("Content-Type", "application/json; charset=utf-8");
		await response.WriteStringAsync(JsonConvert.SerializeObject(items));

		return response;
	}

	[Function("PopNextInQueue")]
	public async Task<HttpResponseData> NextInQueue(
	[HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "queues/pop")] HttpRequestData req)
	{
		var response = await CheckRole(req, QueueRole.Manager);
		if (response != null)
			return response;

		_logger.LogInformation("Processing next user in queue.");

		var nextUser = await _cosmosService.PopNextUserAsync();

		if (nextUser == null)
		{
			var empty = req.CreateResponse(HttpStatusCode.NoContent);
			return empty;
		}

		response = req.CreateResponse(HttpStatusCode.OK);
		response.Headers.Add("Content-Type", "application/json");
		await response.WriteStringAsync(JsonConvert.SerializeObject(nextUser));

		return response;
	}

	[Function("QueueStatus")]
	public async Task<HttpResponseData> QueueStatus(
	[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "queues/status/{email}")] HttpRequestData req,
	string email)
	{
		var response = await CheckRole(req, QueueRole.Member);
		if (response != null)
			return response;

		_logger.LogInformation($"Checking queue status for: {email}");

		var user = await _cosmosService.GetUserStatusAsync(email);

		if (user == null)
		{
			var notFound = req.CreateResponse(HttpStatusCode.NotFound);
			await notFound.WriteStringAsync("User not found in queue.");
			return notFound;
		}

		response = req.CreateResponse(HttpStatusCode.OK);
		response.Headers.Add("Content-Type", "application/json");
		await response.WriteStringAsync(JsonConvert.SerializeObject(user));

		return response;
	}

	[Function("DeleteFromQueue")]
	public async Task<HttpResponseData> DeleteFromQueue(
	[HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "queues/{email}")] HttpRequestData req,
	string email)
	{
		var response = await CheckRole(req, QueueRole.Manager);
		if (response != null)
			return response;

		_logger.LogInformation($"Attempting to remove {email} from queue.");

		bool removed = await _cosmosService.RemoveUserAsync(email);

		if (!removed)
		{
			var notFound = req.CreateResponse(HttpStatusCode.NotFound);
			await notFound.WriteStringAsync("User not found in queue.");
			return notFound;
		}

		response = req.CreateResponse(HttpStatusCode.OK);
		await response.WriteStringAsync("User removed from queue.");
		return response;
	}

	[Function("ClearQueue")]
	public async Task<HttpResponseData> ClearQueue(
	[HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "queues")] HttpRequestData req)
	{
		var response = await CheckRole(req, QueueRole.Manager);
		if (response != null)
			return response;

		_logger.LogInformation("Clearing entire queue...");

		await _cosmosService.ClearQueueAsync();

		response = req.CreateResponse(HttpStatusCode.OK);
		await response.WriteStringAsync("Queue cleared.");
		return response;
	}

	private static async Task<HttpResponseData> CheckRole(HttpRequestData req, QueueRole role) 
	{
		var roleString = role.ToString().ToLower();
		if (!RoleChecker.HasRole(req, roleString))
		{
			var forbidden = req.CreateResponse(System.Net.HttpStatusCode.Forbidden);
			await forbidden.WriteStringAsync($"Required role: {roleString}");
			return forbidden;
		}

		return null!;
	}
}
