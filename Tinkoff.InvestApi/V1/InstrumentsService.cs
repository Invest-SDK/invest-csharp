using Grpc.Core;
using Tinkoff.InvestApi.V1.Cache;

namespace Tinkoff.InvestApi.V1;

public partial class InstrumentsService
{
    public partial class InstrumentsServiceClient
    {
        private static readonly InstrumentsRequest InstrumentsRequest = new();
        private readonly IInstrumentCacheService cacheService;

        public InstrumentsServiceClient(CallInvoker callInvoker, IInstrumentCacheService cacheService)
            : this(callInvoker)
        {
            this.cacheService = cacheService;
        }

        public AsyncUnaryCall<BondsResponse> BondsAsync(CancellationToken cancellationToken = default)
        {
            return BondsAsync(InstrumentsRequest, null, null, cancellationToken);
        }

        public BondsResponse Bonds(CancellationToken cancellationToken = default)
        {
            return Bonds(InstrumentsRequest, null, null, cancellationToken);
        }

        public AsyncUnaryCall<CurrenciesResponse> CurrenciesAsync(CancellationToken cancellationToken = default)
        {
            return CurrenciesAsync(InstrumentsRequest, null, null, cancellationToken);
        }

        public CurrenciesResponse Currencies(CancellationToken cancellationToken = default)
        {
            return Currencies(InstrumentsRequest, null, null, cancellationToken);
        }

        public AsyncUnaryCall<FuturesResponse> FuturesAsync(CancellationToken cancellationToken = default)
        {
            return FuturesAsync(InstrumentsRequest, null, null, cancellationToken);
        }

        public FuturesResponse Futures(CancellationToken cancellationToken = default)
        {
            return Futures(InstrumentsRequest, null, null, cancellationToken);
        }

        public AsyncUnaryCall<EtfsResponse> EtfsAsync(CancellationToken cancellationToken = default)
        {
            return EtfsAsync(InstrumentsRequest, null, null, cancellationToken);
        }

        public EtfsResponse Etfs(CancellationToken cancellationToken = default)
        {
            return Etfs(InstrumentsRequest, null, null, cancellationToken);
        }

        public AsyncUnaryCall<OptionsResponse> OptionsAsync(CancellationToken cancellationToken = default)
        {
            return OptionsAsync(InstrumentsRequest, null, null, cancellationToken);
        }

        public OptionsResponse Options(CancellationToken cancellationToken = default)
        {
            return Options(InstrumentsRequest, null, null, cancellationToken);
        }

        public AsyncUnaryCall<SharesResponse> SharesAsync(CancellationToken cancellationToken = default)
        {
            return SharesAsync(InstrumentsRequest, null, null, cancellationToken);
        }

        public SharesResponse Shares(CancellationToken cancellationToken = default)
        {
            return Shares(InstrumentsRequest, null, null, cancellationToken);
        }

        public async Task<Bond> BondBy(InstrumentRequest request, CancellationToken cancellationToken = default)
        {
            return await cacheService.FindBy(request, async () =>
            {
                var response = await BondByAsync(request, null, null, cancellationToken);
                return response.Instrument;
            });
        }

        public async Task<Currency> CurrencyBy(InstrumentRequest request, CancellationToken cancellationToken = default)
        {
            return await cacheService.FindBy(request, async () =>
            {
                var response = await CurrencyByAsync(request, null, null, cancellationToken);
                return response.Instrument;
            });
        }

        public async Task<DfaResponse> DfaBy(InstrumentRequest request, CancellationToken cancellationToken = default)
        {
            return await cacheService.FindBy(request, async () =>
            {
                var response = await DfaByAsync(request, null, null, cancellationToken);
                return response;
            });
        }

        public async Task<Etf> EtfBy(InstrumentRequest request, CancellationToken cancellationToken = default)
        {
            return await cacheService.FindBy(request, async () =>
            {
                var response = await EtfByAsync(request, null, null, cancellationToken);
                return response.Instrument;
            });
        }

        public async Task<Future> FutureBy(InstrumentRequest request, CancellationToken cancellationToken = default)
        {
            return await cacheService.FindBy(request, async () =>
            {
                var response = await FutureByAsync(request, null, null, cancellationToken);
                return response.Instrument;
            });
        }

        public async Task<Instrument> GetInstrumentBy(InstrumentRequest request, CancellationToken cancellationToken = default)
        {
            return await cacheService.FindBy(request, async () =>
            {
                var response = await GetInstrumentByAsync(request, null, null, cancellationToken);
                return response.Instrument;
            });
        }

        public async Task<Option> OptionBy(InstrumentRequest request, CancellationToken cancellationToken = default)
        {
            return await cacheService.FindBy(request, async () =>
            {
                var response = await OptionByAsync(request, null, null, cancellationToken);
                return response.Instrument;
            });
        }

        public async Task<Share> ShareBy(InstrumentRequest request, CancellationToken cancellationToken = default)
        {
            return await cacheService.FindBy(request, async () =>
            {
                var response = await ShareByAsync(request, null, null, cancellationToken);
                return response.Instrument;
            });
        }

        public async Task<StructuredNote> StructuredNoteBy(InstrumentRequest request, CancellationToken cancellationToken = default)
        {
            return await cacheService.FindBy(request, async () =>
            {
                var response = await StructuredNoteByAsync(request, null, null, cancellationToken);
                return response.Instrument;
            });
        }
    }
}
