using MedFund.Application.Common;
using MedFund.Application.Partnerships;
using MedFund.Domain.Entities;
using MedFund.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MedFund.Infrastructure.Services;

public sealed class PartnershipService : IPartnershipService
{
    private const string Source = "WEBSITE_PARTNERSHIP_FORM";
    private const string SuccessMessage = "We will reach out shortly.";

    private readonly IApplicationDbContext db;
    private readonly ILogger<PartnershipService> logger;

    public PartnershipService(IApplicationDbContext db, ILogger<PartnershipService> logger)
    {
        this.db = db;
        this.logger = logger;
    }

    public async Task<CreatePartnershipLeadResponse> CreateAsync(
        CreatePartnershipLeadRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var normalized = Normalize(request);
        logger.LogInformation(
            "{Service}.{Function} received partnership lead. Email={Email}, PartnerType={PartnerType}, OrganizationName={OrganizationName}, IpAddress={IpAddress}",
            nameof(PartnershipService),
            nameof(CreateAsync),
            normalized.Email,
            normalized.PartnerType,
            normalized.OrganizationName,
            ipAddress);

        var lead = new PartnershipLead
        {
            FirstName = normalized.FirstName,
            LastName = normalized.LastName,
            PhoneNumber = normalized.PhoneNumber,
            Email = normalized.Email,
            PartnerType = normalized.PartnerType,
            OrganizationName = normalized.OrganizationName,
            Status = PartnershipLeadStatus.New,
            Source = Source,
            IpAddress = Truncate(ipAddress, 64),
            UserAgent = Truncate(userAgent, 512)
        };

        db.PartnershipLeads.Add(lead);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "{Service}.{Function} saved partnership lead. PartnershipLeadId={PartnershipLeadId}, Status={Status}",
            nameof(PartnershipService),
            nameof(CreateAsync),
            lead.Id,
            lead.Status);

        return new CreatePartnershipLeadResponse(lead.Id, SuccessMessage);
    }

    private static NormalizedPartnershipLead Normalize(CreatePartnershipLeadRequest request)
    {
        return new NormalizedPartnershipLead(
            request.FirstName.Trim(),
            request.LastName.Trim(),
            request.PhoneNumber.Trim(),
            request.Email.Trim().ToLowerInvariant(),
            ParsePartnerType(request.PartnerType.Trim()),
            request.OrganizationName.Trim());
    }

    private static PartnershipPartnerType ParsePartnerType(string partnerType)
    {
        return partnerType switch
        {
            "HEALTHCARE_PROVIDER" => PartnershipPartnerType.HealthcareProvider,
            "NBFC" => PartnershipPartnerType.Nbfc,
            _ => throw new MedFundException("Partner type must be HEALTHCARE_PROVIDER or NBFC.")
        };
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private sealed record NormalizedPartnershipLead(
        string FirstName,
        string LastName,
        string PhoneNumber,
        string Email,
        PartnershipPartnerType PartnerType,
        string OrganizationName);
}
