using FluentValidation;

namespace ST.Application.Features.Shared.Payments.QueryHandlers;

public sealed class GetPaymentHistoryQueryValidator : AbstractValidator<GetPaymentHistoryQuery>
{
    public GetPaymentHistoryQueryValidator()
    {
        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);
    }
}
