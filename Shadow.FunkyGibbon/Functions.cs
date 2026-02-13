using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Shadow.FunkyGibbon;

public class Functions
{
    private readonly ILogger<Functions> _logger;
    private readonly WalkthroughOptions _options;

    public Functions(ILogger<Functions> logger, IOptions<WalkthroughOptions> options)
    {
        _logger = logger;
        _options = options?.Value ?? new WalkthroughOptions();
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

        // Correlation id for tracing in Application Insights
        var correlationId = Guid.NewGuid().ToString("D");
        _logger.LogInformation("Walkthrough invoked. CorrelationId: {CorrelationId}, Method: {Method}, ContentLength: {Length}",
            correlationId, req.Method, content?.Length ?? 0);

        // Blob upload
        var storageConnection = _options.StorageConnection;
        // Fallback to environment variable at runtime if options were not populated
        if (string.IsNullOrWhiteSpace(storageConnection))
        {
            storageConnection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        }
        var containerName = string.IsNullOrWhiteSpace(_options.BlobContainer) ? "walkthrough" : _options.BlobContainer;
        var blobName = $"walkthrough-{DateTime.UtcNow:yyyyMMddHHmmssfff}.txt";

        if (string.IsNullOrWhiteSpace(storageConnection))
        {
            _logger.LogError("Azure storage connection string is not configured (AzureWebJobsStorage).");
            var errRes = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errRes.WriteAsJsonAsync(new { error = "Storage configuration is missing." });
            return errRes;
        }

        try
        {
            var container = new BlobContainerClient(storageConnection, containerName);
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlobClient(blobName);
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
            await blob.UploadAsync(ms, overwrite: true);
            _logger.LogInformation("Uploaded blob {BlobName} to container {Container}", blobName, containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload blob");
            var errRes = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errRes.WriteAsJsonAsync(new { error = "Failed to upload blob." });
            return errRes;
        }

        // Send an email notification using SendGrid if configured
        var sendGridKey = _options.SendGridApiKey;
        var toAddress = _options.EmailTo;
        // Fallback to environment variables at runtime if options were not populated
        if (string.IsNullOrWhiteSpace(sendGridKey))
        {
            sendGridKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
        }
        if (string.IsNullOrWhiteSpace(toAddress))
        {
            toAddress = Environment.GetEnvironmentVariable("EMAIL_TO");
        }
        var emailSent = false;
        if (!string.IsNullOrEmpty(sendGridKey) && !string.IsNullOrWhiteSpace(toAddress))
        {
            try
            {
                var client = new SendGridClient(sendGridKey);
                var from = new EmailAddress(_options.EmailFrom ?? "no-reply@example.com");
                var to = new EmailAddress(toAddress);
                var subject = "Walkthrough: new blob uploaded";
                var plainText = $"A new blob '{blobName}' was uploaded to container '{containerName}'.";
                var html = $"<p>A new blob '<strong>{blobName}</strong>' was uploaded to container '<strong>{containerName}</strong>'.</p>";
                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainText, html);
                var response = await client.SendEmailAsync(msg);
                emailSent = response.IsSuccessStatusCode;
                _logger.LogInformation("SendGrid email send result: {StatusCode}", response.StatusCode);
                if (!emailSent)
                {
                    // Log response body from SendGrid to aid diagnosis (no secrets are logged)
                    try
                    {
                        var sgBody = response.Body;
                        _logger.LogWarning("SendGrid reported non-success status: {Status} - Body: {Body}", response.StatusCode, sgBody);
                    }
                    catch (Exception bgEx)
                    {
                        _logger.LogWarning(bgEx, "Failed to read SendGrid response body");
                    }

                    var errRes = req.CreateResponse((HttpStatusCode)502);
                    await errRes.WriteAsJsonAsync(new { error = "Failed to send notification email." });
                    return errRes;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email via SendGrid");
                var errRes = req.CreateResponse((HttpStatusCode)502);
                await errRes.WriteAsJsonAsync(new { error = "Failed to send notification email." });
                return errRes;
            }
        }

        var res = req.CreateResponse(HttpStatusCode.Created);
        // Return the created file name, email (if sent), and correlation id for tracing
        await res.WriteAsJsonAsync(new
        {
            fileName = blobName,
            email = emailSent ? toAddress : null,
            correlationId
        });
        return res;
    }
}

// Strongly-typed options for the walkthrough function
public class WalkthroughOptions
{
    public string? StorageConnection { get; set; }
    public string BlobContainer { get; set; } = "walkthrough";
    public string? SendGridApiKey { get; set; }
    public string? EmailTo { get; set; }
    public string? EmailFrom { get; set; } = "no-reply@example.com";
}
