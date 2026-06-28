using MedFund.Application.Common;
using MedFund.Domain.Entities;
using MedFund.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MedFund.Infrastructure.Persistence;

public interface IDevelopmentDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken);
}

public sealed class DevelopmentDataSeeder : IDevelopmentDataSeeder
{
    private readonly MedFundDbContext db;
    private readonly IPasswordHasher passwordHasher;
    private readonly ILogger<DevelopmentDataSeeder> logger;

    public DevelopmentDataSeeder(MedFundDbContext db, IPasswordHasher passwordHasher, ILogger<DevelopmentDataSeeder> logger)
    {
        this.db = db;
        this.passwordHasher = passwordHasher;
        this.logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("{Service}.{Function} received seed request.", nameof(DevelopmentDataSeeder), nameof(SeedAsync));
        if (await db.Users.AnyAsync(cancellationToken))
        {
            logger.LogInformation("{Service}.{Function} skipped seed because users already exist.", nameof(DevelopmentDataSeeder), nameof(SeedAsync));
            return;
        }

        var hospital = new Organization
        {
            Type = OrganizationType.Hospital,
            Name = "CityCare Multispeciality Hospital",
            City = "Bengaluru",
            RegistrationNumber = "HOSP-CITYCARE-001",
            ContactEmail = "admin@citycare.example",
            ContactPhone = "+91 98765 43001"
        };
        var insurer = new Organization
        {
            Type = OrganizationType.InsuranceCompany,
            Name = "Aegis Health Insurance",
            City = "Mumbai",
            RegistrationNumber = "INS-AEGIS-001",
            ContactEmail = "claims@aegis.example",
            ContactPhone = "+91 98765 43002"
        };
        var patient = new Patient
        {
            FullName = "Aarav Sharma",
            Mobile = "+91 98765 43210",
            Email = "patient@medfund.local",
            DateOfBirth = new DateOnly(1988, 4, 12),
            KycStatus = KycStatus.Verified
        };

        var patientUser = new User
        {
            Email = "patient@medfund.local",
            PasswordHash = passwordHasher.HashPassword("Password@123"),
            FirstName = "Aarav",
            LastName = "Sharma",
            PhoneNumber = patient.Mobile,
            Role = UserRole.Patient,
            Patient = patient,
            PatientId = patient.Id,
            EmailVerifiedAt = DateTimeOffset.UtcNow
        };
        patient.User = patientUser;
        patient.UserId = patientUser.Id;

        var hospitalUser = new User
        {
            Email = "hospital@medfund.local",
            PasswordHash = passwordHasher.HashPassword("Password@123"),
            FirstName = "Priya",
            LastName = "Nair",
            PhoneNumber = "+91 98765 43211",
            Role = UserRole.Hospital,
            Organization = hospital,
            OrganizationId = hospital.Id,
            EmailVerifiedAt = DateTimeOffset.UtcNow
        };
        var insuranceUser = new User
        {
            Email = "insurance@medfund.local",
            PasswordHash = passwordHasher.HashPassword("Password@123"),
            FirstName = "Rahul",
            LastName = "Mehta",
            PhoneNumber = "+91 98765 43212",
            Role = UserRole.InsuranceCompany,
            Organization = insurer,
            OrganizationId = insurer.Id,
            EmailVerifiedAt = DateTimeOffset.UtcNow
        };

        var policy = new InsurancePolicy
        {
            Patient = patient,
            PatientId = patient.Id,
            InsuranceCompany = insurer,
            InsuranceCompanyId = insurer.Id,
            ProviderName = insurer.Name,
            PolicyNumberMasked = "AEGIS-XXXX-4521",
            SumInsured = 500000m,
            ApprovedAmount = 140000m,
            Status = InsurancePolicyStatus.Active
        };

        var requestOne = NewRequest("MF-2026-0001", patient, hospital, insurer, hospitalUser.Id, FinancingRequestStatus.AwaitingPatientConsent, 200000m, 140000m, 60000m);
        var requestTwo = NewRequest("MF-2026-0002", patient, hospital, insurer, hospitalUser.Id, FinancingRequestStatus.InsuranceReview, 180000m, 100000m, 80000m);
        var requestThree = NewRequest("MF-2026-0003", patient, hospital, insurer, hospitalUser.Id, FinancingRequestStatus.Approved, 150000m, 110000m, 40000m);
        requestTwo.InsuranceReviewStatus = InsuranceReviewStatus.Pending;
        requestThree.InsuranceReviewStatus = InsuranceReviewStatus.Approved;

        db.Organizations.AddRange(hospital, insurer);
        db.Patients.Add(patient);
        db.Users.AddRange(patientUser, hospitalUser, insuranceUser);
        db.InsurancePolicies.Add(policy);
        db.FinancingRequests.AddRange(requestOne, requestTwo, requestThree);
        db.Settlements.Add(new Settlement
        {
            FinancingRequest = requestThree,
            FinancingRequestId = requestThree.Id,
            Hospital = hospital,
            HospitalId = hospital.Id,
            Amount = requestThree.RequestedFinanceAmount,
            Status = SettlementStatus.Pending,
            ExpectedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))
        });
        db.EmiScheduleItems.AddRange(
            new EmiScheduleItem { FinancingRequest = requestThree, FinancingRequestId = requestThree.Id, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)), Amount = 10000m },
            new EmiScheduleItem { FinancingRequest = requestThree, FinancingRequestId = requestThree.Id, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), Amount = 10000m });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("{Service}.{Function} completed development seed data.", nameof(DevelopmentDataSeeder), nameof(SeedAsync));
    }

    private static FinancingRequest NewRequest(
        string caseNumber,
        Patient patient,
        Organization hospital,
        Organization insurer,
        Guid createdByUserId,
        FinancingRequestStatus status,
        decimal bill,
        decimal approved,
        decimal requested)
    {
        return new FinancingRequest
        {
            CaseNumber = caseNumber,
            Patient = patient,
            PatientId = patient.Id,
            Hospital = hospital,
            HospitalId = hospital.Id,
            InsuranceCompany = insurer,
            InsuranceCompanyId = insurer.Id,
            AdmissionDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            Treatment = "Cardiac procedure",
            EstimatedBillAmount = bill,
            InsuranceApprovedAmount = approved,
            RequestedFinanceAmount = requested,
            Status = status,
            CreatedByUserId = createdByUserId,
            ConsentReceivedAt = status >= FinancingRequestStatus.ConsentReceived ? DateTimeOffset.UtcNow.AddDays(-1) : null
        };
    }
}
