namespace THMS.Data.Stores
{
    public static class FinanceDataRevision
    {
        private static int _current;

        public static int Current => Volatile.Read(ref _current);

        public static void NoteChanged() => Interlocked.Increment(ref _current);
    }
}
