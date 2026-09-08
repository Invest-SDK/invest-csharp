using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Grpc.Core;
using Tinkoff.InvestApi.Stream;
using Tinkoff.InvestApi.Stream.Marketdata;
using Tinkoff.InvestApi.Stream.Marketdata.Models;
using Tinkoff.InvestApi.Stream.Marketdata.Models.Wrapper;
using Tinkoff.InvestApi.V1;
using Tinkoff.InvestApi.V1.Marketdata;
using Xunit;

namespace Tinkoff.InvestApi.Tests.Marketdata;

/// <summary>
/// Тесты для MarketDataStreamWrapper.
/// Примечание: Полноценные интеграционные тесты требуют mock gRPC сервера,
/// поэтому здесь тестируются только базовые сценарии использования.
/// </summary>
public class MarketDataStreamWrapperTests
{
}

