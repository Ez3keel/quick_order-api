using Ordering.Domain.Common;

namespace Ordering.Domain.Orders;

/// <summary>
/// Snapshot of a catalog item at order time: name and price are copied, not referenced,
/// so a later price change in Catalog never retroactively changes a placed order.
/// </summary>
public sealed record OrderItem
{
    public Guid MenuItemId { get; }
    public string ProductName { get; }
    public Money UnitPrice { get; }
    public int Quantity { get; }

    public Money Subtotal => UnitPrice * Quantity;

    public OrderItem(Guid menuItemId, string productName, Money unitPrice, int quantity)
    {
        if (menuItemId == Guid.Empty)
            throw new ArgumentException("MenuItemId is required.", nameof(menuItemId));

        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name is required.", nameof(productName));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");

        MenuItemId = menuItemId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }
}
