namespace MedFund.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string LocalRoot { get; init; } = "storage/documents";
}
