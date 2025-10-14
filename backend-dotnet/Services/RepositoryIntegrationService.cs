using backend_dotnet.Models;
using Microsoft.Azure.Cosmos;
using System.Threading.Tasks;

namespace backend_dotnet.Services
{
    public class RepositoryIntegrationService
    {
        private readonly Container _container;
        public RepositoryIntegrationService(CosmosClient cosmosClient, string dbName, string containerName)
        {
            _container = cosmosClient.GetContainer(dbName, containerName);
        }

        public async Task<RepositoryIntegration> GetIntegrationAsync(string repoId)
        {
            var response = await _container.ReadItemAsync<RepositoryIntegration>(repoId, new PartitionKey(repoId));
            return response.Resource;
        }

        public async Task<RepositoryIntegration> UpsertIntegrationAsync(RepositoryIntegration integration)
        {
            var response = await _container.UpsertItemAsync(integration, new PartitionKey(integration.RepoId));
            return response.Resource;
        }

        public async Task DeleteIntegrationAsync(string repoId)
        {
            await _container.DeleteItemAsync<RepositoryIntegration>(repoId, new PartitionKey(repoId));
        }

        public async Task<IReadOnlyList<RepositoryIntegration>> ListIntegrationsAsync()
        {
            var query = _container.GetItemQueryIterator<RepositoryIntegration>("SELECT * FROM c");
            var results = new List<RepositoryIntegration>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response);
            }
            return results;
        }

        public async Task<RepositoryIntegration> UpdateIntegrationAsync(string repoId, RepositoryIntegration integration)
        {
            // Partial update: get existing, update fields, upsert
            var existing = await GetIntegrationAsync(repoId);
            if (integration.Token != null) existing.Token = integration.Token;
            if (integration.BaseUrl != null) existing.BaseUrl = integration.BaseUrl;
            if (integration.Provider != null) existing.Provider = integration.Provider;
            var response = await _container.UpsertItemAsync(existing, new PartitionKey(repoId));
            return response.Resource;
        }
    }
}
