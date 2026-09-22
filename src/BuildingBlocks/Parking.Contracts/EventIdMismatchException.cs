namespace Parking.Contracts;

public sealed class EventIdMismatchException : InvalidOperationException
{
    public EventIdMismatchException(string source, Guid expected, Guid actual)
        : base($"{source} 응답 EventId가 다릅니다. Expected={expected:N}, Actual={actual:N}")
    {
    }
}
