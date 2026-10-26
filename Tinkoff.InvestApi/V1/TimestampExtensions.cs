namespace Tinkoff.InvestApi.V1;
using Google.Protobuf.WellKnownTypes;

public static class TimestampExtensions
{
    
    public static DateTime? ToDateTimeOrNull(this Timestamp timestamp)
    {
        return timestamp?.ToDateTime();
    }

    public static DateTimeOffset? ToDateTimeOffsetOrNull(this Timestamp timestamp)
    {
        return timestamp?.ToDateTimeOffset();
    }

    public static Timestamp? ToTimestampOrNull(this DateTime? dateTime)
    {
        return dateTime?.ToTimestamp();
    }

    public static Timestamp ToTimestamp(this DateTime dateTime, DateTimeKind kind = DateTimeKind.Utc)
    {
        var dt = kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc) 
            : dateTime.ToUniversalTime();
        return Timestamp.FromDateTime(dt);
    }

    public static Timestamp? ToTimestampOrNull(this DateTimeOffset? dateTime)
    {
        return dateTime?.ToTimestamp();
    }

    public static Timestamp ToTimestamp(this DateTimeOffset dateTime)
    {
        return Timestamp.FromDateTimeOffset(dateTime);
    }
}
