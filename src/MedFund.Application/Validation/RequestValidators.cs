using FluentValidation;
using MedFund.Application.Auth;
using MedFund.Application.Financing;
using MedFund.Domain.Enums;

namespace MedFund.Application.Validation;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class SignupRequestValidator : AbstractValidator<SignupRequest>
{
    public SignupRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.PhoneNumber).MaximumLength(40);
    }
}

public sealed class UpdatePatientProfileRequestValidator : AbstractValidator<UpdatePatientProfileRequest>
{
    public UpdatePatientProfileRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Mobile).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public sealed class CreatePatientRequestValidator : AbstractValidator<CreatePatientRequest>
{
    public CreatePatientRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Mobile).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public sealed class CreateFinancingRequestRequestValidator : AbstractValidator<CreateFinancingRequestRequest>
{
    public CreateFinancingRequestRequestValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.InsuranceCompanyId).NotEmpty();
        RuleFor(x => x.Treatment).NotEmpty().MaximumLength(240);
        RuleFor(x => x.EstimatedBillAmount).GreaterThan(0);
        RuleFor(x => x.InsuranceApprovedAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.InsuranceApprovedAmount)
            .LessThanOrEqualTo(x => x.EstimatedBillAmount)
            .WithMessage("Insurance approved amount cannot exceed estimated bill amount.");
        RuleFor(x => x.RequestedFinanceAmount)
            .GreaterThan(0)
            .LessThanOrEqualTo(x => x.EstimatedBillAmount - x.InsuranceApprovedAmount)
            .WithMessage("Requested finance amount must be within the uninsured gap.");
    }
}

public sealed class ConsentRequestValidator : AbstractValidator<ConsentRequest>
{
    public ConsentRequestValidator()
    {
        RuleFor(x => x.Accepted).Equal(true);
        RuleFor(x => x.Purpose).NotEmpty().MaximumLength(240);
        RuleFor(x => x.AcceptedTermsVersion).NotEmpty().MaximumLength(40);
    }
}

public sealed class InsuranceDecisionRequestValidator : AbstractValidator<InsuranceDecisionRequest>
{
    public InsuranceDecisionRequestValidator()
    {
        RuleFor(x => x.ApprovedAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes)
            .NotEmpty()
            .When(x => x.ReviewStatus is InsuranceReviewStatus.Rejected or InsuranceReviewStatus.NeedsInfo);
    }
}
