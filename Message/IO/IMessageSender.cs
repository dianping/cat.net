namespace Com.Dianping.Cat.Message.Io
{
    using Spi;

    public interface IMessageSender
    {
        bool HasSendingMessage { get; }
        void Initialize();

        void Send(IMessageTree tree);

        void Shutdown();
    }
}