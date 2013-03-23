using Com.Dianping.Cat.Util;
using System;
using System.Text;

namespace Com.Dianping.Cat.Message.Spi.Internals
{
    /// <summary>
    ///   根据域名（配置指定的），系统IP（自动解析的，16进制字符串），时间戳（1970年到当前的小时数）和自增编号组成
    /// </summary>
    public class MessageIdFactory
    {
        private String _mDomain;
        private volatile int _mIndex;

        private String _mIpAddress;
        private long _mLastTimestamp;

        public MessageIdFactory()
        {
            _mLastTimestamp = Timestamp;
        }

        protected internal long Timestamp
        {
            get { return MilliSecondTimer.CurrentTimeHoursForJava(); }
        }

        public String Domain
        {
            set { _mDomain = value; }
        }

        public String IpAddress
        {
            set { _mIpAddress = value; }
        }

        public String GetNextId()
        {
            long timestamp = Timestamp;
            int index;

            lock (this)
            {
                if (timestamp != _mLastTimestamp)
                {
                    _mIndex = 0;
                    _mLastTimestamp = timestamp;
                }

                index = _mIndex++;
            }

            StringBuilder sb = new StringBuilder(_mDomain.Length + 32);

            sb.Append(_mDomain);
            sb.Append('-');
            sb.Append(_mIpAddress);
            sb.Append('-');
            sb.Append(timestamp);
            sb.Append('-');
            sb.Append(index);

            return sb.ToString();
        }

        public void Initialize(String domain)
        {
            _mDomain = domain;

            if (_mIpAddress != null) return;

            byte[] bytes = NetworkInterfaceManager.GetAddressBytes();

            StringBuilder sb = new StringBuilder();

            foreach (byte b  in  bytes)
            {
                sb.Append(((b >> 4) & 0x0F).ToString("x"));
                sb.Append((b & 0x0F).ToString("x"));
            }

            _mIpAddress = sb.ToString();
        }
    }
}