namespace Delivery.Api.Contracts;

public sealed record RegisterCourierRequest(string Name);

public sealed record GoOnlineRequest(double Latitude, double Longitude);

public sealed record UpdateLocationRequest(double Latitude, double Longitude);
