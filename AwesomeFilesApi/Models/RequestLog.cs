using System;

public class RequestLog
{
    public long Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public long DurationMs { get; set; }
}
