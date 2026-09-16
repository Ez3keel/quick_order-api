using Catalog.Domain.Common;
using Catalog.Domain.Restaurants;
using Catalog.Domain.Restaurants.Events;
using Catalog.Domain.Restaurants.Exceptions;

namespace Catalog.Domain.Tests.Restaurants;

public class RestaurantTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Register_WithValidName_CreatesClosedRestaurant()
    {
        var restaurant = Restaurant.Register("Burger House");

        Assert.Equal("Burger House", restaurant.Name);
        Assert.False(restaurant.IsOpen);
        Assert.Empty(restaurant.Menu);
    }

    [Fact]
    public void Register_WithBlankName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Restaurant.Register("  "));
    }

    [Fact]
    public void AddMenuItem_WithNewProductName_AddsToMenu()
    {
        var restaurant = Restaurant.Register("Burger House");

        var item = restaurant.AddMenuItem("X-Burger", new Money(19.90m));

        Assert.Single(restaurant.Menu);
        Assert.True(item.IsAvailable);
        Assert.Equal(new Money(19.90m), item.Price);
    }

    [Fact]
    public void AddMenuItem_WithDuplicateName_ThrowsDuplicateMenuItemException()
    {
        var restaurant = Restaurant.Register("Burger House");
        restaurant.AddMenuItem("X-Burger", new Money(19.90m));

        Assert.Throws<DuplicateMenuItemException>(() =>
            restaurant.AddMenuItem("x-burger", new Money(21.90m)));
    }

    [Fact]
    public void ChangePrice_OnExistingItem_UpdatesPriceAndRaisesEvent()
    {
        var restaurant = Restaurant.Register("Burger House");
        var item = restaurant.AddMenuItem("X-Burger", new Money(19.90m));

        restaurant.ChangePrice(item.Id, new Money(21.90m), Now);

        Assert.Equal(new Money(21.90m), item.Price);
        var raised = Assert.Single(restaurant.DomainEvents);
        var priceChanged = Assert.IsType<MenuItemPriceChangedEvent>(raised);
        Assert.Equal(new Money(19.90m), priceChanged.PreviousPrice);
        Assert.Equal(new Money(21.90m), priceChanged.NewPrice);
    }

    [Fact]
    public void ChangePrice_OnUnknownItem_ThrowsMenuItemNotFoundException()
    {
        var restaurant = Restaurant.Register("Burger House");

        Assert.Throws<MenuItemNotFoundException>(() =>
            restaurant.ChangePrice(MenuItemId.New(), new Money(10m), Now));
    }

    [Fact]
    public void SetItemAvailability_TogglesFlag()
    {
        var restaurant = Restaurant.Register("Burger House");
        var item = restaurant.AddMenuItem("X-Burger", new Money(19.90m));

        restaurant.SetItemAvailability(item.Id, false);

        Assert.False(item.IsAvailable);
    }
}
