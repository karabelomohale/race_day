using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace race_day.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file, [FromServices] IConfiguration config)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                // Uses Azurite local emulator by default, or your real Azure connection string in appsettings.json
                string connString = config["AzureBlobStorage:ConnectionString"] ?? "UseDevelopmentStorage=true";
                var blobServiceClient = new BlobServiceClient(connString);
                var containerClient = blobServiceClient.GetBlobContainerClient("raceday-images");
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                string fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var blobClient = containerClient.GetBlobClient(fileName);

                using (var stream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, true);
                }

                return Ok(new { imageUrl = blobClient.Uri.ToString() });
            }
            catch (Exception)
            {
                // Fallback if Azurite/Azure is offline during local testing so the app never crashes
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                Directory.CreateDirectory(uploadsFolder);
                string fileName = $"{Guid.NewGuid()}_{file.FileName}";
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                return Ok(new { imageUrl = $"{baseUrl}/uploads/{fileName}" });
            }
        }
    }
}