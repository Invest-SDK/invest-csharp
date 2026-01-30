using Grpc.Core;
using Tinkoff.InvestApi.V1;

namespace Tinkoff.InvestApi;

public class InvestApiClient
{
    public InvestApiClient(CallInvoker callInvoker)
    {
        var retriableInvoker = new RetriableCallInvoker(callInvoker);
        Instruments = new InstrumentsService.InstrumentsServiceClient(retriableInvoker);
        MarketData = new MarketDataService.MarketDataServiceClient(retriableInvoker);
        MarketDataStream = new MarketDataStreamService.MarketDataStreamServiceClient(retriableInvoker);
        Operations = new OperationsService.OperationsServiceClient(retriableInvoker);
        OperationsStream = new OperationsStreamService.OperationsStreamServiceClient(retriableInvoker);
        Orders = new OrdersService.OrdersServiceClient(retriableInvoker);
        OrdersStream = new OrdersStreamService.OrdersStreamServiceClient(retriableInvoker);
        Sandbox = new SandboxService.SandboxServiceClient(retriableInvoker);
        StopOrders = new StopOrdersService.StopOrdersServiceClient(retriableInvoker);
        Users = new UsersService.UsersServiceClient(retriableInvoker);
    }

    public InstrumentsService.InstrumentsServiceClient Instruments { get; }
    public MarketDataService.MarketDataServiceClient MarketData { get; }
    public MarketDataStreamService.MarketDataStreamServiceClient MarketDataStream { get; }
    public OperationsService.OperationsServiceClient Operations { get; }
    public OperationsStreamService.OperationsStreamServiceClient OperationsStream { get; }
    public OrdersService.OrdersServiceClient Orders { get; }
    public OrdersStreamService.OrdersStreamServiceClient OrdersStream { get; set; }
    public SandboxService.SandboxServiceClient Sandbox { get; }
    public StopOrdersService.StopOrdersServiceClient StopOrders { get; }
    public UsersService.UsersServiceClient Users { get; }
}