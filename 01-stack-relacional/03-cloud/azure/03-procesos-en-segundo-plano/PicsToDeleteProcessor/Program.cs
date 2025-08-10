using System;
using System.Text.Json;
using System.Threading;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;


Console.WriteLine("Hello to the QueueProcessor!");

var queueClient = new QueueClient(Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING"), "pics-to-delete");

queueClient.CreateIfNotExists();

while (true)
{
    QueueMessage message = queueClient.ReceiveMessage();

    if (message != null)
    {
        Console.WriteLine($"Message received {message.Body}");

        var task = JsonSerializer.Deserialize<Task>(message.Body);

        Console.WriteLine($"Let's delete hero with name, {task.heroName} and alterego, {task.heroAlterEgo}");

        if (task.heroName != null || task.heroAlterEgo != null)
        {
            //Create a Blob service client
            var blobClient = new BlobServiceClient(Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING"));

            //Get container client
            BlobContainerClient alterEgoContainer = blobClient.GetBlobContainerClient("alteregos");
            BlobContainerClient heroContainer = blobClient.GetBlobContainerClient("heroes");

            //Get blob with old name
            var heroImage = $"{task.heroName.Replace(' ', '-').ToLower()}.jpeg";
            var alteregoImage = $"{task.heroAlterEgo.Replace(' ', '-').ToLower()}.png";
            Console.WriteLine($"Deleting {heroImage} and {alteregoImage}");

            var heroBlob = heroContainer.GetBlobClient(heroImage);
            var alteregoBlob = alterEgoContainer.GetBlobClient(alteregoImage);

            if (heroBlob.Exists())
            {
                heroBlob.DeleteIfExists();
            }
            else
            {
                Console.WriteLine($"There is no hero image to delete.");
            }

            if (alteregoBlob.Exists())
            {
                alteregoBlob.DeleteIfExists();
            }
            else
            {
                Console.WriteLine($"There is no alterego image to delete.");
            }

            //Delete message from the queue
            queueClient.DeleteMessage(message.MessageId, message.PopReceipt);
        }
        else
        {
            Console.WriteLine($"Bad message. Delete it");
            //Delete message from the queue
            queueClient.DeleteMessage(message.MessageId, message.PopReceipt);

        }

    }
    else
    {
        Console.WriteLine($"Let's wait 5 seconds");
        Thread.Sleep(5000);
    }

}

class Task
{
    public string heroName { get; set; }
    public string heroAlterEgo { get; set; }
}