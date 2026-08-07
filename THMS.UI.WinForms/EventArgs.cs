namespace THMS.UI.WinForms
{
    /// <summary>
    /// Generic EventArgs payload. Use with EventHandler&lt;EventArgs&lt;T&gt;&gt;
    /// because EventHandler&lt;T&gt; requires T : EventArgs.
    /// </summary>
    public sealed class EventArgs<T> : EventArgs
    {
        public T Value { get; }

        public EventArgs(T value)
        {
            Value = value;
        }
    }
}
