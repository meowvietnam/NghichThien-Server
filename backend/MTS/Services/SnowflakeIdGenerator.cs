namespace MTS.Services;

public class SnowflakeIdGenerator
{
    private const long Epoch = 1767225600000L;

    private const int WorkerIdBits = 4;
    private const int SequenceBits = 10;

    private const long MaxWorkerId = (1L << WorkerIdBits) - 1;
    private const long SequenceMask = (1L << SequenceBits) - 1;

    private readonly long _workerId;

    private long _sequence = 0;
    private long _lastTimestamp = -1;

    private readonly object _lock = new();

    public SnowflakeIdGenerator(long workerId)
    {
        if (workerId < 0 || workerId > MaxWorkerId)
            throw new ArgumentOutOfRangeException(nameof(workerId));

        _workerId = workerId;
    }

    public long NextId()
    {
        lock (_lock)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (timestamp < _lastTimestamp)
                throw new InvalidOperationException("System clock moved backwards.");

            if (timestamp == _lastTimestamp)
            {
                _sequence = (_sequence + 1) & SequenceMask;

                if (_sequence == 0)
                {
                    timestamp = WaitNextMillisecond(timestamp);
                }
            }
            else
            {
                _sequence = 0;
            }

            _lastTimestamp = timestamp;

            return ((timestamp - Epoch) << (WorkerIdBits + SequenceBits))
                   | (_workerId << SequenceBits)
                   | _sequence;
        }
    }

    private static long WaitNextMillisecond(long timestamp)
    {
        while (true)
        {
            var current = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (current > timestamp)
                return current;

            Thread.SpinWait(1);
        }
    }
}
