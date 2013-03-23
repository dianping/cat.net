namespace Com.Dianping.Cat.Message.Spi.Internals
{
    public class DefaultMessageStatistics : IMessageStatistics
    {
        #region IMessageStatistics Members

        public long Produced { get; set; }

        public long Overflowed { get; set; }

        public long Bytes { get; set; }

        public void OnSending(IMessageTree tree)
        {
            Produced++;
        }

        public void OnOverflowed(IMessageTree tree)
        {
            Overflowed++;
        }

        public void OnBytes(int size)
        {
            Bytes += size;
        }

        #endregion
    }
}