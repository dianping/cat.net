namespace Com.Dianping.Cat.Message.Internals
{
    using Message;

    public class NullHeartbeat : AbstractMessage, IHeartbeat
    {
        public NullHeartbeat() : base(null, null)
        {
        }
    }
}