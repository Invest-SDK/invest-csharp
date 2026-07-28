using Tinkoff.InvestApi.V1.Cache;

namespace Tinkoff.InvestApi.V1;

public partial class Share : ICacheableByFigi, ICacheableByPositionUid, ICacheableByTicker, ICacheableByUid
{
}
