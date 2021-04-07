using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Azure.Messaging.ServiceBus;

using WET.lib.Containers;
using WET.lib.Interfaces;

namespace WET.Azure.lib
{
    public class AzureServiceBusEventStorage : IEventStorage
    {
        private readonly string _connectionString;

        private readonly string _queueName;

        public AzureServiceBusEventStorage(string connectionString, string queueName)
        {
            _connectionString = connectionString;

            _queueName = queueName;
        }

        public async Task<bool> WriteEventAsync(ETWEventContainerItem item)
        {
            await using var client = new ServiceBusClient(_connectionString);

            var sender = client.CreateSender(_queueName);

            var message = new ServiceBusMessage(System.Text.Json.JsonSerializer.Serialize(item));
            
            await sender.SendMessageAsync(message);

            return true;
        }

        public void Shutdown()
        {
            // Nothing to do since tear down happens automatically
        }

        public async Task<bool> WriteBatchEventAsync(List<ETWEventContainerItem> batch)
        {
            await using var client = new ServiceBusClient(_connectionString);

            var sender = client.CreateSender(_queueName);

            foreach (var message in batch.Select(item => new ServiceBusMessage(System.Text.Json.JsonSerializer.Serialize(item))))
            {
                await sender.SendMessageAsync(message);
            }

            return true;
        }
    }
}