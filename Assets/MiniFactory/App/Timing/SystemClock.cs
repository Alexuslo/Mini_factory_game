using System;
using MiniFactory.Core.Contracts;

namespace MiniFactory.App.Timing
{
    /// <summary>Реальные часы (UTC). В тестах подменяется FakeClock.</summary>
    public sealed class SystemClock : IClock
    {
        public long NowUnix => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
