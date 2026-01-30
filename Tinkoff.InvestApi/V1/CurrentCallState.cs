using System;
using Grpc.Core;

namespace Tinkoff.InvestApi.V1;

public class CurrentCallState<T>
{
    public AsyncUnaryCall<T> Call { get; set; }

    public CurrentCallState(AsyncUnaryCall<T> call)
    {
        Call = call;
    }
}
