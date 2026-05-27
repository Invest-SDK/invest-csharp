namespace TTech.InvestApi.Examples.HistoryData.Exceptions;

public class DuplicateArchiveException(string message, Exception innerException) : Exception(message, innerException) { }
