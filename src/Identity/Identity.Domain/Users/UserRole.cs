namespace Identity.Domain.Users;

/// <summary>
/// Coarse-grained roles, one per kind of client this system has: the customer
/// ordering food, the restaurant managing its own menu, and the courier delivering.
/// Admin exists for operational access (support, dispute resolution) that doesn't fit
/// any of the other three.
/// </summary>
public enum UserRole
{
    Customer = 0,
    RestaurantOwner = 1,
    Courier = 2,
    Admin = 3,
}
