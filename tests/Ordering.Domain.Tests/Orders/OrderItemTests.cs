using Ordering.Domain.Common;
using Ordering.Domain.Orders;

namespace Ordering.Domain.Tests.Orders;

public class OrderItemTests
{
    [Fact]
    public void Subtotal_MultipliesUnitPriceByQuantity()
    {
        var item = new OrderItem(Guid.NewGuid(), "X-Burger", new Money(10.00m), 3);

        Assert.Equal(new Money(30.00m), item.Subtotal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveQuantity_ThrowsArgumentOutOfRangeException(int quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OrderItem(Guid.NewGuid(), "X-Burger", new Money(10.00m), quantity));
    }

    [Fact]
    public void Constructor_WithEmptyMenuItemId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new OrderItem(Guid.Empty, "X-Burger", new Money(10.00m), 1));
    }
}
