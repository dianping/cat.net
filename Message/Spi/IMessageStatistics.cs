namespace Com.Dianping.Cat.Message.Spi
{
    public interface IMessageStatistics
    {
        long Produced { get; set; }

        long Overflowed { get; set; }

        long Bytes { get; set; }

        void OnSending(IMessageTree tree);

        void OnOverflowed(IMessageTree tree);

        void OnBytes(int size);
    }
}