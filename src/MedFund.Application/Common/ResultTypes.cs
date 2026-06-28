namespace MedFund.Application.Common;

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)Math.Max(PageSize, 1));
}

public sealed record CurrentUser(
    Guid UserId,
    string Email,
    string Role,
    Guid? OrganizationId,
    Guid? PatientId);

public sealed record FileUploadDescriptor(
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content);

public sealed record StoredFile(
    string StorageKey,
    string FileName,
    string ContentType,
    long SizeBytes);

public sealed record EmailAddress(
    string Email,
    string? Name = null);

public sealed record EmailMessage(
    EmailAddress From,
    IReadOnlyCollection<EmailAddress> To,
    string Subject,
    string TextBody,
    string HtmlBody);
