namespace Com.Dianping.Cat.Message.Internals
{
    using Message;
    using System;
    using System.Collections.Generic;

    public class DefaultTransaction : AbstractMessage, ITransaction
    {
        private readonly Action<ITransaction> _endCallBack;
        private IList<IMessage> _mChildren;
        private long _mDurationInMicro; // must be less than 0

        public DefaultTransaction(String type, String name, Action<ITransaction> endCallBack)
            : base(type, name)
        {
            _mDurationInMicro = -1;
            _endCallBack = endCallBack;
            Standalone = true;
        }

        #region ITransaction Members

        public IList<IMessage> Children
        {
            get { return _mChildren ?? (_mChildren = new List<IMessage>()); }
        }

        public long DurationInMicros
        {
            get
            {
                if (_mDurationInMicro >= 0)
                {
                    return _mDurationInMicro;
                }
                // if it's not completed explicitly
                long duration = 0;
                int len = (_mChildren == null) ? 0 : _mChildren.Count;

                if (len > 0)
                {
                    if (_mChildren != null)
                    {
                        IMessage lastChild = _mChildren[len - 1];

                        if (lastChild is ITransaction)
                        {
                            ITransaction trx = lastChild as ITransaction;

                            duration = trx.Timestamp*1000L + trx.DurationInMicros - TimestampInMicros;
                        }
                        else
                        {
                            duration = lastChild.Timestamp*1000L - TimestampInMicros;
                        }
                    }
                }

                return duration;
            }
            set { _mDurationInMicro = value; }
        }

        public long DurationInMillis
        {
            get { return DurationInMicros/1000L; }
            set { _mDurationInMicro = value*1000L; }
        }

        public bool Standalone { get; set; }

        public ITransaction AddChild(IMessage message)
        {
            if (_mChildren == null)
            {
                _mChildren = new List<IMessage>();
            }

            _mChildren.Add(message);
            return this;
        }

        public override void Complete()
        {
            if (IsCompleted())
            {
                // complete() was called more than once
                IMessage evt0 = new DefaultEvent("CAT", "BadInstrument") {Status = "TransactionAlreadyCompleted"};

                evt0.Complete();
                AddChild(evt0);
            }
            else
            {
                _mDurationInMicro = DateTime.Now.Ticks/10L - TimestampInMicros;

                SetCompleted(true);

                if (_endCallBack != null)
                {
                    _endCallBack(this);
                }
            }
        }

        public bool HasChildren()
        {
            return _mChildren != null && _mChildren.Count > 0;
        }

        #endregion
    }
}