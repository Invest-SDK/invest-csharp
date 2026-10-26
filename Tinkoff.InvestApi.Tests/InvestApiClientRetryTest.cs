using System.Threading.Tasks;
using Grpc.Core;
using Moq;
using Tinkoff.InvestApi.V1;

namespace Tinkoff.InvestApi.Tests;

public class InvestApiClientRetryTest
{

    [Fact]
    public async Task TestTwoAttemptsForBlockingUnaryCall()
    {
        var mockCallInvoker = new Mock<CallInvoker>();
        var expectedResponse = new GetInfoResponse();
        var metadata =  new Metadata();
        metadata.Add(new Metadata.Entry("x-ratelimit-reset", "1"));
        mockCallInvoker.SetupSequence(x => x.BlockingUnaryCall(
            It.IsAny<Method<GetInfoRequest, GetInfoResponse>>(),
            It.IsAny<string>(),
            It.IsAny<CallOptions>(),
            It.IsAny<GetInfoRequest>()))
        .Throws(new RpcException(new Status(StatusCode.ResourceExhausted, "test1"), metadata))
        .Returns(expectedResponse);
        var client = new UsersService.UsersServiceClient(new RetriableCallInvoker(mockCallInvoker.Object));
        Assert.Equal(expectedResponse, client.GetInfo(new GetInfoRequest()));
    }

    [Fact]
    public async Task TestTwoAttemptsForAsyncUnaryCall()
    {
        var mockCallInvoker = new Mock<CallInvoker>();
        var metadata =  new Metadata();
        metadata.Add(new Metadata.Entry("x-ratelimit-reset", "1"));
        var failtureResponse = new AsyncUnaryCall<GetInfoResponse>(
            throwException(metadata),
            Task.FromResult(metadata),
            () => new Status(StatusCode.ResourceExhausted, "test1"),
            () => metadata,
            null);
        var expectedResponse = new AsyncUnaryCall<GetInfoResponse>(
            Task.FromResult(new GetInfoResponse()),
            Task.FromResult(new Metadata()),
            () => new Status(StatusCode.OK, "ok"),
            () => new Metadata(),
            () => { }
        );
        mockCallInvoker.SetupSequence(x => x.AsyncUnaryCall(
            It.IsAny<Method<GetInfoRequest, GetInfoResponse>>(),
            It.IsAny<string>(),
            It.IsAny<CallOptions>(),
            It.IsAny<GetInfoRequest>()))
        .Returns(failtureResponse)
        .Returns(expectedResponse);
        var client = new UsersService.UsersServiceClient(new RetriableCallInvoker(mockCallInvoker.Object));
        var response = await client.GetInfoAsync(new GetInfoRequest()).ResponseAsync;
        Assert.Equal(await expectedResponse.ResponseAsync, response);
    }

    public async Task<GetInfoResponse> throwException(Metadata metadata)
    {
        throw new RpcException(new Status(StatusCode.ResourceExhausted, "test1"), metadata);
    }
}