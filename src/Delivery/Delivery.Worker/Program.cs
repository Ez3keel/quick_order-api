using Delivery.Application;
using Delivery.Infrastructure;
using Delivery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDeliveryApplication();
builder.Services.AddDeliveryInfrastructure(builder.Configuration);
builder.Services.AddDeliveryAssignmentConsumer();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
    await dbContext.Database.MigrateAsync();
}

host.Run();

namespace Delivery.Worker
{
    public partial class Program;
}
