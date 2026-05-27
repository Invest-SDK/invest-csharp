namespace Tinkoff.InvestApi.V1.History;

public class HistoryArchiveNotFoundException(string message) : HttpRequestException(message) {}
