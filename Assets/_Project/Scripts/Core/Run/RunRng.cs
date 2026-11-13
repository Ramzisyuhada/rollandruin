namespace RollAndRuin.Core
{
    public sealed class RunRng
    {
        private ulong _state;
        public RunRng(int seed)
        {
            _state = (ulong)seed;
        }

        public ulong State
        {
            get => _state;
            set => _state = value;
        }
        private ulong NextULong()
        {
            _state += 0x9E3779B97F4A7C15;                 
            ulong z = _state;                             
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9;     
            z = (z ^ (z >> 27)) * 0x94D049BB133111EB;     
            return z ^ (z >> 31);
        }

        public int Next(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                throw new System.ArgumentOutOfRangeException(
                    nameof(maxExclusive), "maxExclusive harus lebih besar dari minInclusive.");

            ulong range = (ulong)((long)maxExclusive - minInclusive);
            return (int)(minInclusive + (long)(NextULong() % range));
        }
    }
}