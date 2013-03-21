namespace Com.Dianping.Cat.Message.Internals
{
    using Message;
    using System;

    public class DefaultEvent : AbstractMessage, IEvent
    {
        public DefaultEvent(String type, String name) : base(type, name)
        {
        }
    }
}