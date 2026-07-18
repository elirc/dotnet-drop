using NetDropbox.Application.Abstractions;

namespace NetDropbox.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

