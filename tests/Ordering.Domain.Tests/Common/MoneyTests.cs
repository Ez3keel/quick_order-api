using Ordering.Domain.Common;

namespace Ordering.Domain.Tests.Common;

public class MoneyTests
{
    [Fact]
    public void Add_WithSameCurrency_SumsAmounts()
    {
        var result = new Money(10.50m) + new Money(5.25m);

        Assert.Equal(new Money(15.75m), result);
    }

    [Fact]
    public void Add_WithDifferentCurrencies_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => new Money(10m, "BRL") + new Money(10m, "USD"));
    }

    [Fact]
    public void Constructor_WithNegativeAmount_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(-1m));
    }

    [Fact]
    public void Multiply_ByInteger_ScalesAmount()
    {
        var result = new Money(9.99m) * 3;

        Assert.Equal(new Money(29.97m), result);
    }
}
