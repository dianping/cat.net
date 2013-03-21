namespace Com.Dianping.Cat.Message.Internals
{
    using Message;
    using System.Collections.Generic;

    public class NullTransaction : AbstractMessage, ITransaction
    {
        private IList<IMessage> _mChildren;

        public NullTransaction() : base(null, null)
        {
        }

        #region ITransaction Members

        public IList<IMessage> Children
        {
            get { return _mChildren ?? (_mChildren = new List<IMessage>()); }
        }

        public long DurationInMicros
        {
            get { return 0; }
            set
            {
                //do nothing here
            }
        }

        public long DurationInMillis
        {
            get { return 0; }
            set
            {
                //do nothing here
            }
        }

        public bool Standalone
        {
            get { return true; }
            set
            {
                //do nothing here
            }
        }

        public ITransaction AddChild(IMessage message)
        {
            // do nothing here
            return this;
        }

        public override void Complete()
        {
            // do nothing here
        }

        public bool HasChildren()
        {
            return false;
        }

        #endregion
    }
}