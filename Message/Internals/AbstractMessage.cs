using System;
using System.Text;
using Com.Dianping.Cat.Message.Spi.Codec;
using Com.Dianping.Cat.Util;

namespace Com.Dianping.Cat.Message.Internals
{
    public abstract class AbstractMessage : IMessage
    {
        private readonly String _mName;
        private readonly String _mType;
        private bool _mCompleted;
        private StringBuilder _mData;

        private String _mStatus = "unset";

        protected AbstractMessage(String type, String name)
        {
            _mType = type;
            _mName = name;
            TimestampInMicros = MilliSecondTimer.CurrentTimeMicros();
        }

        /// <summary>
        ///   其实是Ticks除以10
        /// </summary>
        protected long TimestampInMicros { get; private set; }

        #region IMessage Members

        public String Data
        {
            get { return _mData == null ? "" : _mData.ToString(); }
        }

        public String Name
        {
            get { return _mName; }
        }

        public String Status
        {
            get { return _mStatus; }

            set { _mStatus = value; }
        }

        /// <summary>
        ///   其实是Ticks除以10000
        /// </summary>
        public long Timestamp
        {
            get { return TimestampInMicros/1000L; }
            set { TimestampInMicros = value*1000L; }
        }

        public String Type
        {
            get { return _mType; }
        }

        public void AddData(String keyValuePairs)
        {
            if (_mData == null)
            {
                _mData = new StringBuilder(keyValuePairs);
            }
            else
            {
                _mData.Append(keyValuePairs);
            }
        }

        public void AddData(String key, Object value)
        {
            if (_mData == null)
            {
                _mData = new StringBuilder();
            }
            else if (_mData.Length > 0)
            {
                _mData.Append('&');
            }

            _mData.Append(key).Append('=').Append(value);
        }

        public virtual void Complete()
        {
            SetCompleted(true);
        }

        public bool IsCompleted()
        {
            return _mCompleted;
        }

        public bool IsSuccess()
        {
            return "0".Equals(_mStatus);
        }

        public void SetStatus(Exception e)
        {
            _mStatus = e.GetType().FullName;
        }

        #endregion

        protected void SetCompleted(bool completed)
        {
            _mCompleted = completed;
        }

        public override String ToString()
        {
            PlainTextMessageCodec codec = new PlainTextMessageCodec();
            ChannelBuffer buf = new ChannelBuffer(8192);

            codec.EncodeMessage(this, buf);
            buf.Reset();

            return buf.ToString();
        }
    }
}