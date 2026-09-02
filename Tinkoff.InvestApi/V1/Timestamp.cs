namespace Tinkoff.InvestApi.V1;
using Google.Protobuf.WellKnownTypes;

public static class TimestampExtensions
{
    
    public static DateTime? ToDateTimeOrNull(this Timestamp timestamp)
    {
        return timestamp == null ? null : timestamp.ToDateTime();
    }

    public static Timestamp? ToTimestampOrNull(this DateTime? dateTime)
    {
        return dateTime == null ? null : dateTime.Value.ToTimestamp();
    }

    public static Timestamp ToTimestamp(this DateTime dateTime, DateTimeKind kind = DateTimeKind.Utc)
    {
        var dt = kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc) 
            : dateTime.ToUniversalTime();
        return Timestamp.FromDateTime(dt);
    }
}
