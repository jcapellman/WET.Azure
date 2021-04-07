using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;

using WET.lib.Containers;
using WET.lib.Interfaces;

namespace WET.Azure.lib
{
    public class AzureCosmosEventStorage : IEventStorage
    {
        private readonly CosmosClient _cosmosClient;
        
        private readonly Database _database;
        
        private readonly Container _container;

        private readonly PropertyInfo _propertyKey;

        public AzureCosmosEventStorage(string uri, string containerId, string databaseId, string primaryKey, string partitionKey)
        {
            _cosmosClient = new CosmosClient(uri, primaryKey);

            _database = _cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId).Result;
            _container = _database.CreateContainerIfNotExistsAsync(containerId, $"/{partitionKey}").Result;

            var partitionProperty = typeof(ETWEventContainerItem).GetProperties()
                .FirstOrDefault(a => a.Name == partitionKey);

            _propertyKey = partitionProperty;
        }

        public async Task<bool> WriteEventAsync(ETWEventContainerItem item)
        {
            var partitionKeyValue = _propertyKey.GetValue(item);

            if (partitionKeyValue == null)
            {
                throw new ArgumentOutOfRangeException(nameof(item),
                    $"Could not extract {_propertyKey.Name} from the item");
            }

            var result = await _container.CreateItemAsync(item, new PartitionKey(partitionKeyValue.ToString()));

            return result.StatusCode == HttpStatusCode.OK;
        }

        public void Shutdown()
        {
            _cosmosClient.Dispose();
        }

        public async Task<bool> WriteBatchEventAsync(List<ETWEventContainerItem> batch)
        {
            var error = false;

            foreach (var item in batch)
            {
                var partitionKeyValue = _propertyKey.GetValue(item);

                if (partitionKeyValue == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(item),
                        $"Could not extract {_propertyKey.Name} from the item");
                }

                var result = await _container.CreateItemAsync(item, new PartitionKey(partitionKeyValue.ToString()));

                if (result.StatusCode != HttpStatusCode.OK)
                {
                    error = true;
                }
            }

            return error;
        }
    }
}