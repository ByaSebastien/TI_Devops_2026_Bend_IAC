using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TI_Devops_2026_Bend_IAC.contexts;
using TI_Devops_2026_Bend_IAC.Dtos;
using TI_Devops_2026_Bend_IAC.Entities;

namespace TI_Devops_2026_Bend_IAC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController(
        MyDbContext context,
        BlobContainerClient container,
        ServiceBusClient bus,
        IConfiguration configuration
        ) : ControllerBase
    {

        [HttpPost]
        public async Task<IActionResult> AddAsync([FromForm] ProductForm form)
        {

            var product = new Product
            {
                Name = form.Name
            };

            var added = (await context.Products.AddAsync(product)).Entity;
            await context.SaveChangesAsync();

            var stream = new MemoryStream();

            await form.Image.CopyToAsync(stream);

            stream.Position = 0;

            var blobName = Guid.NewGuid().ToString();

            var blobClient = container.GetBlobClient(blobName);

            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = form.Image.ContentType
                },
                Metadata = new Dictionary<string, string>
                {
                    { "productId", added.Id.ToString() }
                }
            };

            await blobClient.UploadAsync(stream, uploadOptions);

            var sender = bus.CreateSender("devops-2026-queue");

            var message = new ServiceBusMessage(JsonSerializer.Serialize(added));

            await sender.SendMessageAsync(message);

            return Ok(added);
        }

        [HttpGet("image/{productId}")]
        public async Task<IActionResult> GetImage([FromRoute] string productId)
        {
            var blob = await container.GetBlobsAsync().FirstOrDefaultAsync(b =>
            {
                var blobClient = container.GetBlobClient(b.Name);
                var properties = blobClient.GetPropertiesAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                return properties.Value.Metadata.TryGetValue("productId", out var id) && id.ToString() == productId;
            });

            if(blob == null)
            {
                return NotFound();
            }

            var blobClient = container.GetBlobClient(blob.Name);
            var properties = await blobClient.GetPropertiesAsync();
            var contentType = properties.Value.ContentType ?? "application/octet-stream";
            var stream = await blobClient.OpenReadAsync();

            return File(stream, contentType, blob.Name);
        }
    }
}
