using Tinkoff.InvestApi.V1.Cache;

namespace Tinkoff.InvestApi.V1;

public partial class Option : ICacheableByPositionUid, ICacheableByTicker, ICacheableByUid
{
}
