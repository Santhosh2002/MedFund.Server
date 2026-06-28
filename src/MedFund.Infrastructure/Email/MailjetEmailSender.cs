using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MedFund.Application.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MedFund.Infrastructure.Email;

public sealed class MailjetEmailSender : IEmailSender
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;
    private readonly MailjetOptions options;
    private readonly ILogger<MailjetEmailSender> logger;

    public MailjetEmailSender(HttpClient httpClient, IOptions<MailjetOptions> options, ILogger<MailjetEmailSender> logger)
    {
        this.httpClient = httpClient;
        this.options = options.Value;
        this.logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "{Service}.{Function} received email send request. Subject={Subject}, Recipients={Recipients}, Enabled={Enabled}",
            nameof(MailjetEmailSender),
            nameof(SendAsync),
            message.Subject,
            message.To.Select(x => x.Email).ToArray(),
            options.Enabled);

        if (!options.Enabled)
        {
            logger.LogWarning(
                "{Service}.{Function} skipped sending because Mailjet is disabled. Subject={Subject}, Recipients={Recipients}",
                nameof(MailjetEmailSender),
                nameof(SendAsync),
                message.Subject,
                message.To.Select(x => x.Email).ToArray());
            return;
        }

        EnsureConfigured();

        var request = new
        {
            Messages = new[]
            {
                new
                {
                    From = ToMailjetAddress(message.From),
                    To = message.To.Select(ToMailjetAddress).ToArray(),
                    Subject = message.Subject,
                    TextPart = message.TextBody,
                    HTMLPart = message.HtmlBody
                }
            }
        };

        var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{options.ApiKey}:{options.ApiSecret}"));
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v3.1/send")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "{Service}.{Function} Mailjet send failed. StatusCode={StatusCode}, Body={Body}",
                nameof(MailjetEmailSender),
                nameof(SendAsync),
                (int)response.StatusCode,
                responseBody);
            throw new MedFundException("Email delivery failed.");
        }

        logger.LogInformation(
            "{Service}.{Function} Mailjet send completed. StatusCode={StatusCode}, Recipients={Recipients}",
            nameof(MailjetEmailSender),
            nameof(SendAsync),
            (int)response.StatusCode,
            message.To.Select(x => x.Email).ToArray());
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey) ||
            string.IsNullOrWhiteSpace(options.ApiSecret) ||
            string.IsNullOrWhiteSpace(options.FromEmail))
        {
            throw new MedFundException("Mailjet email is enabled but ApiKey, ApiSecret, or FromEmail is missing.");
        }
    }

    private static object ToMailjetAddress(EmailAddress address)
    {
        return new
        {
            address.Email,
            Name = address.Name ?? address.Email
        };
    }
}
