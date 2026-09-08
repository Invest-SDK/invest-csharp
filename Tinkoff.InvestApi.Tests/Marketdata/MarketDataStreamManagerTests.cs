using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Grpc.Core;
using Tinkoff.InvestApi.Stream;
using Tinkoff.InvestApi.Stream.Marketdata;
using Tinkoff.InvestApi.V1;
using Xunit;

namespace Tinkoff.InvestApi.Tests.Marketdata;

public class MarketDataStreamManagerTests
{

    [Fact]
    void TestSubscribeTradesDoesNotDuplicateAcrossSeparateCalls()
    {
        
    }

    [Fact]
    void TestSubscribeCandlesWithDifferentIntervalsForSameInstrument()
    {
        
    }

    [Fact]
    void TestSubscribeTradingStatusesDeliversToListener()
    {
        
    }

    [Fact]
    void TestSubscribeTradingStatusesWithSharedIntervalInstrumentStillDelivers()
    {
        
    }

    [Fact]
    void TestSubscribeTradingStatusesTwiceRegistersBothListeners()
    {
        
    }

    [Fact]
    void TestSubscribeOrderBookWithAllTypeDeliversToListener()
    {
        
    }

    [Fact]
    void TestSubscribeOrderBookWithExplicitTypeIgnoresOtherType()
    {
        
    }
}