using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace Notification.IntegrationTests;

public sealed class HubAuthorizationTests(NotificationApiFactory factory) : IClassFixture<NotificationApiFactory>
{
    [Fact]
    public async Task Connecting_WithoutToken_Fails()
    {
        _ = factory.Server;

        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/order-tracking", options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            })
            .Build();

        var act = async () => await connection.StartAsync();

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task SubscribeToCourier_ForSomeoneElsesCourierId_ThrowsHubException()
    {
        _ = factory.Server;

        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/order-tracking", options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.AccessTokenProvider = () =>
                    Task.FromResult<string?>(TestJwtTokenFactory.Create("Courier", Guid.NewGuid()));
            })
            .Build();

        await connection.StartAsync();

        var act = async () => await connection.InvokeAsync("SubscribeToCourier", Guid.NewGuid());

        await act.Should().ThrowAsync<HubException>();

        await connection.DisposeAsync();
    }
}
