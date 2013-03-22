namespace Com.Dianping.Cat.Message.Internals
{
    using System.Collections;
    using System.Threading;

    public class CatThreadLocal<T>
    {
        private readonly Hashtable _mValues = new Hashtable(1024);

        public T Value
        {
            get
            {
                int threadId = Thread.CurrentThread.ManagedThreadId;
                object value = _mValues[threadId];

                return (T) value;
            }
            set
            {
                int threadId = Thread.CurrentThread.ManagedThreadId;

                _mValues[threadId] = value;
            }
        }

        public void Dispose()
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;

            _mValues.Remove(threadId);
        }
    }
}