using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Shadow.FunkyGibbon;

public class Functions
{
    private readonly ILogger<Functions> _logger;

    public Functions(ILogger<Functions> logger)
    {
        _logger = logger;
    }

    [Function("ShadowTest")]
    public async Task<HttpResponseData> Run([HttpTrigger(
        AuthorizationLevel.Anonymous,
        "get", "post",
        Route = "walkthrough")]
        HttpRequestData req)
    {
        _logger.LogInformation("HTTP trigger function processed a request.");

        // Read request body for POST, otherwise create a simple message for GET
        string content;
        if (string.Equals(req.Method, "POST", StringComparison.OrdinalIgnoreCase))
        {
            using var sr = new StreamReader(req.Body, Encoding.UTF8);
            content = await sr.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                content = $"(empty body) - triggered at {DateTime.UtcNow:O}";
            }
        }
        else
        {
            content = $"Walkthrough trigger fired at {DateTime.UtcNow:O}";
        }

        // Blob upload
        var storageConnection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        var containerName = Environment.GetEnvironmentVariable("BLOB_CONTAINER") ?? "walkthrough";
        var blobName = $"walkthrough-{DateTime.UtcNow:yyyyMMddHHmmssfff}.txt";

        try
        {
            var container = new BlobContainerClient(storageConnection ?? string.Empty, containerName);
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlobClient(blobName);
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
            await blob.UploadAsync(ms, overwrite: true);
            _logger.LogInformation("Uploaded blob {BlobName} to container {Container}", blobName, containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload blob");
        }

        // Send an email notification using SendGrid if configured
        var sendGridKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
        var toAddress = Environment.GetEnvironmentVariable("EMAIL_TO");
        var emailSent = false;
        if (!string.IsNullOrEmpty(sendGridKey) && !string.IsNullOrWhiteSpace(toAddress))
        {
            try
            {
                var client = new SendGridClient(sendGridKey);
                var fromAddress = Environment.GetEnvironmentVariable("EMAIL_FROM") ?? "no-reply@example.com";
                var from = new EmailAddress(fromAddress);
                var to = new EmailAddress(toAddress);
                var subject = "Walkthrough: new blob uploaded";
                var plainText = $"A new blob '{blobName}' was uploaded to container '{containerName}'.";
                var html = $"<p>A new blob '<strong>{blobName}</strong>' was uploaded to container '<strong>{containerName}</strong>'.</p>";
                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainText, html);
                var response = await client.SendEmailAsync(msg);
                emailSent = response.IsSuccessStatusCode;
                _logger.LogInformation("SendGrid email send result: {StatusCode}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email via SendGrid");
            }
        }

        var res = req.CreateResponse(HttpStatusCode.OK);
        // Return only the created file name and the email it was sent to (if sent)
        await res.WriteAsJsonAsync(new
        {
            fileName = blobName,
            email = emailSent ? toAddress : null
        });
        return res;
    }
}
