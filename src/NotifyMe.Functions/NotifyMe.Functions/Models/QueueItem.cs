using Newtonsoft.Json;

namespace NotifyMe.Functions.Models;
public class QueueItem
{
	[JsonProperty("id")]
	public string Id { get; set; } = Guid.NewGuid().ToString();

	[JsonProperty("pk")]
	public string PartitionKey { get; set; } = "QUEUE";

	[JsonProperty("userEmail")]
	public string UserEmail { get; set; }

	[JsonProperty("position")]
	public int Position { get; set; }

	[JsonProperty("timestamp")]
	public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
