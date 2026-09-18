using FluentValidation;

namespace FacilitiesMgmt.Application.MaintenanceRequests;

/// <summary>
/// Validation lives at the API boundary, on the DTOs — not inside the domain.
/// <para>
/// FluentValidation over DataAnnotations: the rules that matter here are conditional
/// and comparative ("actual cost is required only when completing", "cost must fit
/// DECIMAL(12,2)"), which attributes express badly or not at all. It also keeps
/// validation out of the entity, so the domain stays about behaviour and the boundary
/// stays about shape. The trade-off is one more package and rules living apart from
/// the type they validate.
/// </para>
/// <para>
/// The domain still re-checks the rules it actually depends on (negative costs, state
/// legality). Boundary validation is for good error messages; it is not the safety net.
/// </para>
/// </summary>
public sealed class CreateMaintenanceRequestValidator : AbstractValidator<CreateMaintenanceRequestDto>
{
    // DECIMAL(12,2): ten digits before the point. Rejecting oversized values here
    // turns a SQL truncation error into a 400 with a readable message.
    private const decimal MaxCost = 9_999_999_999.99m;

    public CreateMaintenanceRequestValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(4000);

        RuleFor(x => x.EstimatedCost)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(MaxCost)
            .Must(HaveAtMostTwoDecimalPlaces)
            .WithMessage("Estimated cost cannot have more than two decimal places.");
    }

    internal static bool HaveAtMostTwoDecimalPlaces(decimal value) =>
        decimal.Round(value, 2) == value;
}

public sealed class CompleteMaintenanceRequestValidator : AbstractValidator<CompleteMaintenanceRequestDto>
{
    public CompleteMaintenanceRequestValidator()
    {
        RuleFor(x => x.ActualCost)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(9_999_999_999.99m)
            .Must(CreateMaintenanceRequestValidator.HaveAtMostTwoDecimalPlaces)
            .WithMessage("Actual cost cannot have more than two decimal places.");
    }
}

public sealed class RejectMaintenanceRequestValidator : AbstractValidator<RejectMaintenanceRequestDto>
{
    public RejectMaintenanceRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(1000);
    }
}
