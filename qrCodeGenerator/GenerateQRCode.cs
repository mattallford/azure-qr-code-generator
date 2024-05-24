using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using System.Drawing;
using System.Drawing.Imaging;
using QRCoder;

namespace qrCodeGenerator
{
    public static class GenerateQRCode
    {
        [FunctionName("GenerateQRCode")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string url = req.Query["url"];

            if (string.IsNullOrEmpty(url))
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                url = url ?? data?.url;
            }

            if (string.IsNullOrEmpty(url))
            {
                return new BadRequestObjectResult("Please pass a URL in the query string or in the request body");
            }

            try
            {
                // Generate QR code
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
                QRCoder.QRCode qrCode = new QRCoder.QRCode(qrCodeData);
                using Bitmap bitmap = qrCode.GetGraphic(20);

                // Convert QR code to a memory stream
                using MemoryStream stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Png);
                stream.Position = 0;

                // Upload to Azure Blob Storage
                string connectionString = Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING");
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("qr-codes");
                await containerClient.CreateIfNotExistsAsync();

                // Create a unique name for the blob
                string sanitizedUrl = url.Replace("https://", "").Replace("http://", "").Replace("/", "_").Replace("\\", "_");
                string blobName = sanitizedUrl + ".png";
                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                await blobClient.UploadAsync(stream, true);

                // Generate a SAS token for the blob
                BlobSasBuilder sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = containerClient.Name,
                    BlobName = blobClient.Name,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
                };
                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                Uri sasUri = blobClient.GenerateSasUri(sasBuilder);

                // Return the URL of the uploaded QR code with SAS token
                var response = new
                {
                    qr_code_url = sasUri.ToString()
                };

                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error generating and uploading QR code.");
                return new StatusCodeResult(500);
            }
        }
    }
}
