using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TI_Devops_2026_Bend_IAC.contexts;
using TI_Devops_2026_Bend_IAC.Dtos;
using TI_Devops_2026_Bend_IAC.Entities;

namespace TI_Devops_2026_Bend_IAC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController(
        MyDbContext context,
        BlobContainerClient container,
        ServiceBusClient bus,
        IConfiguration configuration
        ) : ControllerBase
    {

        [HttpPost]
        public async Task<IActionResult> AddAsync([FromForm] ProductForm request)
        {
            try
            {

                var p = new Product { Name = request.Name };
                var added = context.Add(p).Entity;
                await context.SaveChangesAsync();
                //var added = new Product { Name = request.Name, Id = 42 };

                var stream = new MemoryStream();
                await request.Image.CopyToAsync(stream);
                stream.Position = 0;

                var blobName = Guid.NewGuid().ToString();
                var blobClient = container.GetBlobClient(blobName);

                var uploadOptions = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = request.Image.ContentType
                    },
                    Metadata = new Dictionary<string, string>
                {
                    {
                        "productId", added.Id.ToString()
                    },
                }
                };

                await blobClient.UploadAsync(stream, uploadOptions);

                var sender = bus.CreateSender("seb-servicebusqueue-for-test-bicep");

                var message = new ServiceBusMessage(
                        JsonSerializer.Serialize(added)
                    );

                await sender.SendMessageAsync(message);

                return Accepted(added);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("image/{productId}")]
        public async Task<IActionResult> GetImage([FromRoute] string productId)
        {
            try
            {
                var blob = await container.GetBlobsAsync().FirstOrDefaultAsync(b =>
                {
                    var blobClient = container.GetBlobClient(b.Name);
                    var properties = blobClient.GetPropertiesAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    return properties.Value.Metadata.TryGetValue("productId", out var id) && id == productId;
                });

                if (blob is null) return NotFound();

                var blobClient = container.GetBlobClient(blob.Name);
                var properties = await blobClient.GetPropertiesAsync();
                var contentType = properties.Value.ContentType ?? "application/octet-stream";
                var stream = await blobClient.OpenReadAsync();

                return File(stream, contentType, blob.Name);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("connectionString")]
        public async Task<IActionResult> GetConnectionString()
        {
            try
            {
                var connectionStrings = new
                {
                    Main = configuration.GetConnectionString("Main"),
                    Blob = configuration.GetConnectionString("Blob"),
                    ServiceBus = configuration.GetConnectionString("Bus")
                };

                return Ok(connectionStrings);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
