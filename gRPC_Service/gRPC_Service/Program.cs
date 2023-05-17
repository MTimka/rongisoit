using System.Net;
using gRPC_Service.Services;
using gRPC_Service.Tests;
using Microsoft.AspNetCore.Server.Kestrel.Core;

// run few tests at first
// DrawTracks.Test1();
// LinearTest.Test1();
// LinearTest.Test2();

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();

builder.WebHost.ConfigureKestrel(options =>
{
    // options.Listen(IPAddress.Parse("192.168.8.100"), 5001, o => o.Protocols = HttpProtocols.Http2);
    options.Listen(IPAddress.Parse("0.0.0.0"), 5001, o => o.Protocols = HttpProtocols.Http2);

});

builder.Services.AddCors(o => o.AddPolicy("MyPolicy", builder => {
    builder
        .SetIsOriginAllowed(_ => true)
        .AllowCredentials()
        .AllowAnyMethod()
        .AllowAnyHeader();
}));

var app = builder.Build();

app.UseCors("MyPolicy");

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

// run task
Task.Run(GreeterService.GetData);

app.Run();
