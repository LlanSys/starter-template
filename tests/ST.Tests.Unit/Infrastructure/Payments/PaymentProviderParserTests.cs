using ST.Infrastructure.Features.Shared.Payments.Contracts.Implementations;

namespace ST.Tests.Unit.Infrastructure.Payments;

public sealed class PaymentProviderParserTests
{
    [Theory]
    [InlineData(null, nameof(PaymentProviderKind.NoOp))]
    [InlineData("", nameof(PaymentProviderKind.NoOp))]
    [InlineData("NoOp", nameof(PaymentProviderKind.NoOp))]
    [InlineData("none", nameof(PaymentProviderKind.NoOp))]
    [InlineData("Stripe", nameof(PaymentProviderKind.Stripe))]
    [InlineData("card", nameof(PaymentProviderKind.Stripe))]
    [InlineData("card-payment", nameof(PaymentProviderKind.Stripe))]
    [InlineData("M-Pesa", nameof(PaymentProviderKind.Mpesa))]
    [InlineData("mpesa_stk", nameof(PaymentProviderKind.Mpesa))]
    [InlineData("cash", nameof(PaymentProviderKind.Invalid))]
    public void Parse_ReturnsExpectedProvider(string? provider, string expected)
    {
        var actual = PaymentProviderParser.Parse(provider);

        Assert.Equal(expected, actual.ToString());
    }
}
