namespace Com.Dianping.Cat.Message.Internals
{
    using Message;
    using System;

    public class DefaultHeartbeat : AbstractMessage, IHeartbeat
    {
        public DefaultHeartbeat(String type, String name) : base(type, name)
        {
        }
    }
}