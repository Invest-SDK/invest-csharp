using Grpc.Core;
using Tinkoff.InvestApi.V1;

namespace Tinkoff.InvestApi.Stream;

/// <summary>
/// Фабрика для создания gRPC стрим-соединений.
/// </summary>
public class StreamFactory
{

    /// <summary>
    /// Создать двусторонний стрим (duplex).
    /// </summary>
    public static AsyncDuplexStreamingCall<TRequest, TResponse> CreateDuplexStream<TClient, TRequest, TResponse>(
        ResilenceDuplexStreamConfigurer<TClient, TRequest, TResponse> streamConfigurer, 
        CancellationToken cancellationToken)
        where TRequest : class
        where TResponse : class
        where TClient : ClientBase<TClient>
    {
        var callOptions = streamConfigurer.CallOptions;
        var cts = CancellationTokenSource.CreateLinkedTokenSource(callOptions.CancellationToken, cancellationToken);
        return  streamConfigurer.CallMethod.Invoke(streamConfigurer.Client, callOptions.WithCancellationToken(cts.Token));
    }

    /// <summary>
    /// Создать односторонний стрим (server-side streaming).
    /// </summary>
    public static AsyncServerStreamingCall<TResponse> CreateServerSideStream<TClient, TRequest, TResponse>(
        ResilenceServerSideStreamConfigurer<TClient, TRequest, TResponse> streamConfigurer,
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : class
        where TResponse : class
        where TClient : ClientBase<TClient>
    {
        var callOptions = streamConfigurer.CallOptions;
        var cts = CancellationTokenSource.CreateLinkedTokenSource(callOptions.CancellationToken, cancellationToken);
        return streamConfigurer.CallMethod.Invoke(streamConfigurer.Client, request, callOptions.WithCancellationToken(cts.Token));
    }
}
