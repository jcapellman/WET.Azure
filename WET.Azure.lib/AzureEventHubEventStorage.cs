using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

using WET.lib.Containers;
using WET.lib.Interfaces;

namespace WET.Azure.lib
{
    public class AzureEventHubEventStorage : IEventStorage
    {
        private readonly string _connectionString;

        private readonly string _eventHubName;

        public AzureEventHubEventStorage(string connectionString, string eventHubName)
        {
            _connectionString = connectionString;

            _eventHubName = eventHubName;
        }
        
        public async Task<bool> WriteEventAsync(ETWEventContainerItem item)
        {
            await using var producerClient = new EventHubProducerClient(_connectionString, _eventHubName);

            using var eventBatch = await producerClient.CreateBatchAsync();

            eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(item))));

            await producerClient.SendAsync(eventBatch);

            return true;
        }

        public void Shutdown()
        {
            // Not needed
        }

        public async Task<bool> WriteBatchEventAsync(List<ETWEventContainerItem> batch)
        {
            await using var producerClient = new EventHubProducerClient(_connectionString, _eventHubName);

            using var eventBatch = await producerClient.CreateBatchAsync();

            foreach (var item in batch)
            {
                eventBatch.TryAdd(
                    new EventData(Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(item))));
            }

            await producerClient.SendAsync(eventBatch);

            return true;
        }
    }
}