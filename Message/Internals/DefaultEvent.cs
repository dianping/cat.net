using System;

namespace Com.Dianping.Cat.Message.Internals
{
    public class DefaultEvent : AbstractMessage, IEvent
    {
        public DefaultEvent(String type, String name)
            : base(type, name)
        {
        }
    }
}