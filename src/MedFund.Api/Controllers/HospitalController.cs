using MedFund.Api.Security;
using MedFund.Application.Common;
using MedFund.Application.Financing;
using MedFund.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedFund.Api.Controllers;

[ApiController]
[Authorize(Policy = PolicyNames.Hospital)]
[Route("api/hospital")]
public sealed class HospitalController : ControllerBase
{
    private readonly IHospitalService hospitalService;

    public HospitalController(IHospitalService hospitalService)
    {
        this.hospitalService = hospitalService;
    }

    [HttpGet("dashboard")]
    public Task<HospitalDashboardResponse> Dashboard(CancellationToken cancellationToken)
    {
        return hospitalService.GetDashboardAsync(cancellationToken);
    }

    [HttpPost("patients")]
    public Task<PatientDto> CreatePatient(CreatePatientRequest request, CancellationToken cancellationToken)
    {
        return hospitalService.CreateOrLinkPatientAsync(request, cancellationToken);
    }

    [HttpGet("patients")]
    public Task<IReadOnlyCollection<PatientDto>> Patients(CancellationToken cancellationToken)
    {
        return hospitalService.GetPatientsAsync(cancellationToken);
    }

    [HttpGet("patients/{id:guid}")]
    public Task<PatientDto> Patient(Guid id, CancellationToken cancellationToken)
    {
        return hospitalService.GetPatientAsync(id, cancellationToken);
    }

    [HttpPost("financing-requests")]
    public Task<CreateFinancingRequestResponse> CreateFinancingRequest(CreateFinancingRequestRequest request, CancellationToken cancellationToken)
    {
        return hospitalService.CreateFinancingRequestAsync(request, cancellationToken);
    }

    [HttpGet("financing-requests")]
    public Task<PagedResult<FinancingRequestDto>> FinancingRequests(
        [FromQuery] string? status,
        [FromQuery] Guid? patientId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return hospitalService.GetFinancingRequestsAsync(
            ParseEnum<FinancingRequestStatus>(status),
            patientId,
            fromDate,
            toDate,
            page,
            pageSize,
            cancellationToken);
    }

    [HttpGet("financing-requests/{id:guid}")]
    public Task<FinancingRequestDto> FinancingRequest(Guid id, CancellationToken cancellationToken)
    {
        return hospitalService.GetFinancingRequestAsync(id, cancellationToken);
    }

    [HttpPost("financing-requests/{id:guid}/documents")]
    [Consumes("multipart/form-data")]
    public Task<DocumentRecordDto> UploadDocument(Guid id, [FromForm] UploadDocumentForm request, CancellationToken cancellationToken)
    {
        if (request.File.Length <= 0)
        {
            throw new MedFundException("Uploaded file is empty.");
        }

        return hospitalService.AddDocumentAsync(
            id,
            ParseEnum<DocumentType>(request.DocumentType) ?? DocumentType.Other,
            new FileUploadDescriptor(request.File.FileName, request.File.ContentType, request.File.Length, request.File.OpenReadStream()),
            cancellationToken);
    }

    [HttpPost("financing-requests/{id:guid}/submit")]
    public Task<CreateFinancingRequestResponse> Submit(Guid id, CancellationToken cancellationToken)
    {
        return hospitalService.SubmitAsync(id, cancellationToken);
    }

    [HttpGet("settlements")]
    public Task<IReadOnlyCollection<SettlementDto>> Settlements(CancellationToken cancellationToken)
    {
        return hospitalService.GetSettlementsAsync(cancellationToken);
    }

    private static TEnum? ParseEnum<TEnum>(string? value)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Replace("_", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        return Enum.TryParse<TEnum>(normalized, ignoreCase: true, out var parsed)
            ? parsed
            : throw new MedFundException($"{value} is not a valid {typeof(TEnum).Name}.");
    }
}

public sealed class UploadDocumentForm
{
    public required string DocumentType { get; init; }

    public required IFormFile File { get; init; }
}
