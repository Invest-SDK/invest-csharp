using Grpc.Core;
using Tinkoff.InvestApi.V1;

namespace Tinkoff.InvestApi;

public class InvestApiClient
{
    public InvestApiClient(CallInvoker callInvoker)
    {
        RetriableInvoker = new RetriableCallInvoker(callInvoker);
        Instruments = new InstrumentsService.InstrumentsServiceClient(RetriableInvoker);
        MarketData = new MarketDataService.MarketDataServiceClient(RetriableInvoker);
        MarketDataStream = new MarketDataStreamService.MarketDataStreamServiceClient(RetriableInvoker);
        Operations = new OperationsService.OperationsServiceClient(RetriableInvoker);
        OperationsStream = new OperationsStreamService.OperationsStreamServiceClient(RetriableInvoker);
        Orders = new OrdersService.OrdersServiceClient(RetriableInvoker);
        OrdersStream = new OrdersStreamService.OrdersStreamServiceClient(RetriableInvoker);
        Sandbox = new SandboxService.SandboxServiceClient(RetriableInvoker);
        StopOrders = new StopOrdersService.StopOrdersServiceClient(RetriableInvoker);
        Users = new UsersService.UsersServiceClient(RetriableInvoker);
    }
    public RetriableCallInvoker RetriableInvoker { get; }
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