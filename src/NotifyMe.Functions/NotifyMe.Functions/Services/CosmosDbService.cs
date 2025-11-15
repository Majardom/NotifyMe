using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using NotifyMe.Functions.Models;

namespace NotifyMe.Functions.Services;
public class CosmosDbService
{
	private readonly Container _container;

	public CosmosDbService(
		string endpointUri,
		string primaryKey,
		string databaseName,
		string containerName)
	{
		var client = new CosmosClient(endpointUri, primaryKey);
		_container = client.GetContainer(databaseName, containerName);
	}

	public async Task<IEnumerable<QueueItem>> GetAllQueueItemsAsync()
	{
		var queryable = _container.GetItemLinqQueryable<QueueItem>(allowSynchronousQueryExecution: false);

		using var iterator = queryable.ToFeedIterator();

		List<QueueItem> results = new();

		while (iterator.HasMoreResults)
		{
			var response = await iterator.ReadNextAsync();
			results.AddRange(response);
		}

		return results;
	}

	public async Task<bool> AddUserToQueueAsync(QueueItem item)
	{
		var exists = await QueueItemExistsAsync(item);

		if (exists)
			return exists;

		var all = await GetAllQueueItemsAsync();

		int nextPosition = all.Any() ? all.Max(x => x.Position) + 1 : 1;

		item.Position = nextPosition;

		await _container.CreateItemAsync(item, new PartitionKey(item.PartitionKey));

		return false;
	}

	public async Task<QueueItem?> PopNextUserAsync()
	{
		var allItems = await GetAllQueueItemsAsync();

		if (!allItems.Any())
			return null;

		var first = allItems.OrderBy(x => x.Position).First();

		await _container.DeleteItemAsync<QueueItem>(first.Id, new PartitionKey(first.PartitionKey));

		var remaining = allItems.Where(x => x.Id != first.Id).OrderBy(x => x.Position).ToList();

		foreach (var item in remaining)
		{
			item.Position -= 1;
			await _container.UpsertItemAsync(item, new PartitionKey(item.PartitionKey));
		}

		return first;
	}

	public async Task<QueueItem?> GetUserStatusAsync(string email)
	{
		var queryable = _container
			.GetItemLinqQueryable<QueueItem>()
			.Where(x => x.UserEmail == email);

		using var iterator = queryable.ToFeedIterator();

		if (!iterator.HasMoreResults)
			return null;

		var response = await iterator.ReadNextAsync();
		return response.FirstOrDefault();
	}

	public async Task<bool> RemoveUserAsync(string email)
	{
		var allItems = await GetAllQueueItemsAsync();

		var user = allItems.FirstOrDefault(x => x.UserEmail == email);

		if (user == null)
			return false;

		await _container.DeleteItemAsync<QueueItem>(user.Id, new PartitionKey(user.PartitionKey));

		var remaining = allItems
			.Where(x => x.Position > user.Position)
			.OrderBy(x => x.Position)
			.ToList();

		foreach (var item in remaining)
		{
			item.Position -= 1;
			await _container.UpsertItemAsync(item, new PartitionKey(item.PartitionKey));
		}

		return true;
	}

	public async Task ClearQueueAsync()
	{
		var allItems = await GetAllQueueItemsAsync();
		
		foreach (var item in allItems)
		{
			await _container.DeleteItemAsync<QueueItem>(item.Id, new PartitionKey(item.PartitionKey));
		}
	}

	private async Task<bool> QueueItemExistsAsync(QueueItem toCheck)
	{
		var query = _container.GetItemLinqQueryable<QueueItem>(allowSynchronousQueryExecution: false)
		  .Where(q => q.PartitionKey == toCheck.PartitionKey && q.UserEmail == toCheck.UserEmail)
		  .Take(1)                                   // LIMIT 1
		  .ToFeedIterator();

		while (query.HasMoreResults)
		{
			var response = await query.ReadNextAsync();
			if (response.Any())
				return true;
		}

		return false;
	}
}
