using Tinkoff.InvestApi.V1.Cache;

namespace Tinkoff.InvestApi.V1;

public partial class Currency : ICacheableByFigi, ICacheableByPositionUid, ICacheableByTicker, ICacheableByUid
{
}
