using System;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Tinkoff.InvestApi.Tests.Integration.Wrapper;

public class GrpcTestServerFixture : IAsyncLifetime
{
    private IHost? _host;
    private TestServer? _testServer;

    public TestServer TestServer => _testServer ?? throw new InvalidOperationException("Server not started");

    public async Task InitializeAsync()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddGrpc();
                    services.AddLogging(logging =>
                    {
                        logging.AddConsole();
                        logging.SetMinimumLevel(LogLevel.Debug);
                    });
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGrpcService<TestStreamServiceImpl>();
                    });
                });
            });

        _host = builder.Build();
        await _host.StartAsync();
        
        _testServer = _host.GetTestServer();
    }

    public async Task DisposeAsync()
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        
        _testServer?.Dispose();
    }

    public GrpcChannel CreateChannel()
    {
        var handler = _testServer?.CreateHandler() ?? throw new InvalidOperationException("Server not started");
        var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpHandler = handler
        });
        return channel;
    }

    public void ResetService()
    {
        TestStreamServiceImpl.Reset();
    }
}
