using System.Globalization;
using System.Text;
using MedFund.Domain.Enums;

namespace MedFund.Application.Common;

public static class RoleNames
{
    public const string Patient = "PATIENT";
    public const string Hospital = "HOSPITAL";
    public const string InsuranceCompany = "INSURANCE_COMPANY";

    public static string ToApiValue(UserRole role)
    {
        return role switch
        {
            UserRole.Patient => Patient,
            UserRole.Hospital => Hospital,
            UserRole.InsuranceCompany => InsuranceCompany,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };
    }

    public static UserRole ParseRole(string role)
    {
        return role.Trim().ToUpperInvariant() switch
        {
            Patient => UserRole.Patient,
            Hospital => UserRole.Hospital,
            InsuranceCompany => UserRole.InsuranceCompany,
            _ => throw new ArgumentException($"Unsupported role '{role}'.", nameof(role))
        };
    }

    public static string ToUpperSnakeCase(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var builder = new StringBuilder(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            var character = value[i];
            if (char.IsUpper(character) && i > 0)
            {
                builder.Append('_');
            }

            builder.Append(char.ToUpper(character, CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }
}
