namespace Com.Dianping.Cat.Message.Internals
{
    using Message;

    public class NullEvent : AbstractMessage, IEvent
    {
        public NullEvent() : base(null, null)
        {
        }
    }
}