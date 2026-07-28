using Tinkoff.InvestApi.V1.Cache;

namespace Tinkoff.InvestApi.V1;

public partial class Instrument : ICacheableByFigi, ICacheableByPositionUid, ICacheableByTicker, ICacheableByUid
{
}
