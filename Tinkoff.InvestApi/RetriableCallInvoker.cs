using Grpc.Core;
using Tinkoff.InvestApi.V1;

namespace Tinkoff.InvestApi;

public class RetriableCallInvoker : CallInvoker
{

    public int DefaultTimeout { get; set; }
    readonly CallInvoker _callInvoker;
    readonly int _maxRetries;
    readonly int _defaultDelay;

    public RetriableCallInvoker(CallInvoker callInvoker, int maxRetries = 3, int defaultDelay = 1, int defaultTimeout = 30000)
    {
        DefaultTimeout = defaultTimeout;
        _callInvoker = callInvoker;
        _maxRetries = maxRetries;
        _defaultDelay = defaultDelay;
    }

    public override TResponse BlockingUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request)
        where TRequest : class
        where TResponse : class
    {
        options = AddDeadline(options);
        for (var attempt = 0;; ++attempt)
        {
            options.CancellationToken.ThrowIfCancellationRequested();
            try
            {
                return _callInvoker.BlockingUnaryCall(method, host, options, request);
            }
            catch (RpcException ex) when (attempt < _maxRetries && ex.StatusCode is StatusCode.ResourceExhausted)
            {
                var trailers = ex.Trailers;
                if (trailers.GetValue("x-ratelimit-reset") is { } retryAfterValue)
                {
                    var delaySeconds = int.TryParse(retryAfterValue, out var seconds) ? seconds : _defaultDelay;
                    Task.Delay(TimeSpan.FromSeconds(delaySeconds), options.CancellationToken).GetAwaiter().GetResult();
                }
                else
                {
                    throw;
                }
            }
        }
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, 
        string? host, CallOptions options, TRequest request)
        where TRequest : class
        where TResponse : class
    {
        options = AddDeadline(options);
        var call = _callInvoker.AsyncUnaryCall(method, host, options, request);
        var state = new CurrentCallState<TResponse>(call);
        return new AsyncUnaryCall<TResponse>(RunWithRetry(),
            static state => ((CurrentCallState<TResponse>)state).Call.ResponseHeadersAsync,
            static state => ((CurrentCallState<TResponse>)state).Call.GetStatus(),
            static state => ((CurrentCallState<TResponse>)state).Call.GetTrailers(),
            static state => ((CurrentCallState<TResponse>)state).Call.Dispose(), state);

        async Task<TResponse> RunWithRetry()
        {
            for (var attempt = 0;; ++attempt)
            {
                options.CancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return await state.Call.ResponseAsync;
                }
                catch (RpcException ex) when (attempt < _maxRetries && ex.StatusCode is StatusCode.ResourceExhausted)
                {
                    var trailers = ex.Trailers;
                    if (trailers.GetValue("x-ratelimit-reset") is { } retryAfterValue)
                    {
                        var delaySeconds = int.TryParse(retryAfterValue, out var seconds) ? seconds : _defaultDelay;
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), options.CancellationToken);
                        state.Call = _callInvoker.AsyncUnaryCall(method, host, options, request);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request)
        where TRequest : class
        where TResponse : class
    {
        return _callInvoker.AsyncServerStreamingCall(method, host, options, request);
    }

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options)
        where TRequest : class
        where TResponse : class
    {
        return _callInvoker.AsyncClientStreamingCall(method, host, options);
    }

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options)
        where TRequest : class
        where TResponse : class
    {
        return _callInvoker.AsyncDuplexStreamingCall(method, host, options);
    }

    private CallOptions AddDeadline(CallOptions options)
    {
        if (!options.Deadline.HasValue)
        {
            options = options.WithDeadline(DateTime.UtcNow.AddMilliseconds(DefaultTimeout));
        }

        return options;
    }
}
