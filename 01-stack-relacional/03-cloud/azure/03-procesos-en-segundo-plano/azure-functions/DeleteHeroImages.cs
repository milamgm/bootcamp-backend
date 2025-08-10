using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;

namespace azure_functions
{
    public class Payload
    {
        public string HeroName { get; set; }
        public string AlterEgoName { get; set; }
    }

    public static class DeleteHeroImages
    {
        [FunctionName("DeleteHeroImages")]
        public static async Task Run(
            [QueueTrigger("pics-to-delete", Connection = "AzureStorageConnection")] string message,
            ILogger log)
        {
            log.LogInformation($"Mensaje recibido: {message}");

            Payload payload;
            try
            {
                payload = JsonSerializer.Deserialize<Payload>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                log.LogWarning($"Error deserializando mensaje: {ex.Message}");
                return;
            }

            if (payload == null || string.IsNullOrEmpty(payload.HeroName) || string.IsNullOrEmpty(payload.AlterEgoName))
            {
                log.LogWarning("Mensaje JSON inválido o incompleto.");
                return;
            }

            string connectionString = Environment.GetEnvironmentVariable("AzureStorageConnection");
            var blobServiceClient = new BlobServiceClient(connectionString);

            var heroesContainer = blobServiceClient.GetBlobContainerClient("heroes");
            var alteregosContainer = blobServiceClient.GetBlobContainerClient("alteregos");

            var heroBlobClient = heroesContainer.GetBlobClient(payload.HeroName);
            bool heroDeleted = await heroBlobClient.DeleteIfExistsAsync();
            log.LogInformation(heroDeleted
                ? $"Deleted hero image: {payload.HeroName}"
                : $"Hero image not found or already deleted: {payload.HeroName}");

            var alterEgoBlobClient = alteregosContainer.GetBlobClient(payload.AlterEgoName);
            bool alterEgoDeleted = await alterEgoBlobClient.DeleteIfExistsAsync();
            log.LogInformation(alterEgoDeleted
                ? $"Deleted alter ego image: {payload.AlterEgoName}"
                : $"Alter ego image not found or already deleted: {payload.AlterEgoName}");
        }
    }
}
