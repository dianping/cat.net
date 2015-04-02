﻿using Com.Dianping.Cat.Message.Internals;
using Com.Dianping.Cat.Util;
using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

namespace Com.Dianping.Cat.Message.Spi.Codec
{
    public class PlainTextMessageCodec : IMessageCodec
    {
        #region Policy enum

        public enum Policy
        {
            DEFAULT,

            WITHOUT_STATUS,

            WITH_DURATION
        }

        #endregion

        private const String ID = "PT1"; // plain text version 1

        private const byte TAB = (byte)'\t'; // tab character

        private const byte LF = (byte)'\n'; // line feed character

        private readonly BufferHelper _mBufferHelper;

        private readonly DateHelper _mDateHelper;

        public PlainTextMessageCodec()
        {
            _mBufferHelper = new BufferHelper();
            _mDateHelper = new DateHelper();
        }

        #region IMessageCodec Members

        public virtual IMessageTree Decode(ChannelBuffer buf)
        {
            DefaultMessageTree tree = new DefaultMessageTree();

            Decode(buf, tree);
            return tree;
        }

        public virtual void Decode(ChannelBuffer buf, IMessageTree tree)
        {
            DecodeHeader(buf, tree);

            if (buf.ReadableBytes() > 0)
            {
                DecodeMessage(buf, tree);
            }
        }

        public virtual void Encode(IMessageTree tree, ChannelBuffer buf)
        {
            int count = 0;

            buf.WriteInt(0); // place-holder
            count += EncodeHeader(tree, buf);

            if (tree.Message != null)
            {
                count += EncodeMessage(tree.Message, buf);
            }

            buf.SetInt(0, count);
        }

        #endregion

        protected internal void DecodeHeader(ChannelBuffer buf, IMessageTree tree)
        {
            BufferHelper helper = _mBufferHelper;
            String id = helper.Read(buf, TAB);
            String domain = helper.Read(buf, TAB);
            String hostName = helper.Read(buf, TAB);
            String ipAddress = helper.Read(buf, TAB);
            String threadGroupName = helper.Read(buf, TAB);
            String threadId = helper.Read(buf, TAB);
            String threadName = helper.Read(buf, TAB);
            String messageId = helper.Read(buf, TAB);
            String parentMessageId = helper.Read(buf, TAB);
            String rootMessageId = helper.Read(buf, TAB);
            String sessionToken = helper.Read(buf, LF);

            if (ID.Equals(id))
            {
                tree.Domain = domain;
                tree.HostName = hostName;
                tree.IpAddress = ipAddress;
                tree.ThreadGroupName = threadGroupName;
                tree.ThreadId = threadId;
                tree.ThreadName = threadName;
                tree.MessageId = messageId;
                tree.ParentMessageId = parentMessageId;
                tree.RootMessageId = rootMessageId;
                tree.SessionToken = sessionToken;
            }
            else
            {
                throw new Exception("Unrecognized id(" + id + ") for plain text message codec!");
            }
        }

        protected internal IMessage DecodeLine(ChannelBuffer buf, ITransaction parent,
                                               Stack<ITransaction> stack, IMessageTree tree)
        {
            BufferHelper helper = _mBufferHelper;
            char identifier = (char)buf.ReadByte();
            String timestamp = helper.Read(buf, TAB);
            String type = helper.Read(buf, TAB);
            String name = helper.Read(buf, TAB);
            switch (identifier)
            {
                case 't':
                    IMessage transaction = new DefaultTransaction(type, name, null);

                    helper.Read(buf, LF); // get rid of line feed
                    transaction.Timestamp = _mDateHelper.Parse(timestamp);

                    if (parent != null)
                    {
                        parent.AddChild(transaction);
                    }

                    stack.Push(parent);
                    return transaction;
                case 'A':
                    DefaultTransaction tran = new DefaultTransaction(type, name, null);
                    String status = helper.Read(buf, TAB);
                    String duration = helper.Read(buf, TAB);
                    String data = helper.ReadRaw(buf, TAB);

                    helper.Read(buf, LF); // get rid of line feed
                    tran.Timestamp = _mDateHelper.Parse(timestamp);
                    tran.Status = status;
                    tran.AddData(data);

                    long d = Int64.Parse(duration.Substring(0, duration.Length - 2), NumberStyles.Integer);

                    tran.DurationInMicros = d;

                    if (parent != null)
                    {
                        parent.AddChild(tran);
                        return parent;
                    }
                    return tran;
                case 'T':
                    String transactionStatus = helper.Read(buf, TAB);
                    String transactionDuration = helper.Read(buf, TAB);
                    String transactionData = helper.ReadRaw(buf, TAB);

                    helper.Read(buf, LF); // get rid of line feed
                    parent.Status = transactionStatus;
                    parent.AddData(transactionData);

                    long transactionD = Int64.Parse(transactionDuration.Substring(0, transactionDuration.Length - 2), NumberStyles.Integer);

                    parent.DurationInMicros = transactionD;

                    return stack.Pop();
                case 'E':
                    DefaultEvent evt = new DefaultEvent(type, name);
                    String eventStatus = helper.Read(buf, TAB);
                    String eventData = helper.ReadRaw(buf, TAB);

                    helper.Read(buf, LF); // get rid of line feed
                    evt.Timestamp = _mDateHelper.Parse(timestamp);
                    evt.Status = eventStatus;
                    evt.AddData(eventData);

                    if (parent != null)
                    {
                        parent.AddChild(evt);
                        return parent;
                    }
                    return evt;
                case 'M':
                    DefaultMetric metric = new DefaultMetric(type, name);
                    String metricStatus = helper.Read(buf, TAB);
                    String metricData = helper.ReadRaw(buf, TAB);

                    helper.Read(buf, LF);
                    metric.Timestamp = _mDateHelper.Parse(timestamp);
                    metric.Status = metricStatus;
                    metric.AddData(metricData);

                    if (parent != null)
                    {
                        parent.AddChild(metric);
                        return parent;
                    }
                    return metric;
                case 'H':
                    DefaultHeartbeat heartbeat = new DefaultHeartbeat(type, name);
                    String heartbeatStatus = helper.Read(buf, TAB);
                    String heartbeatData = helper.ReadRaw(buf, TAB);

                    helper.Read(buf, LF); // get rid of line feed
                    heartbeat.Timestamp = _mDateHelper.Parse(timestamp);
                    heartbeat.Status = heartbeatStatus;
                    heartbeat.AddData(heartbeatData);

                    if (parent != null)
                    {
                        parent.AddChild(heartbeat);
                        return parent;
                    }
                    return heartbeat;
            }

            Logger.Error("Unknown identifier(" + identifier + ") of message: " + buf);
            //throw new Exception("Unknown identifier int name"); //java版的抛出异常

            // unknown message, ignore it
            return parent;
        }

        protected internal void DecodeMessage(ChannelBuffer buf, IMessageTree tree)
        {
            Stack<ITransaction> stack = new Stack<ITransaction>();
            IMessage parent = DecodeLine(buf, null, stack, tree);

            tree.Message = parent;

            while (buf.ReadableBytes() > 0)
            {
                IMessage message = DecodeLine(buf, (ITransaction)parent, stack, tree);

                if (message is ITransaction)
                {
                    parent = message;
                }
                else
                {
                    break;
                }
            }
        }

        protected internal int EncodeHeader(IMessageTree tree, ChannelBuffer buf)
        {
            BufferHelper helper = _mBufferHelper;
            int count = 0;

            count += helper.Write(buf, ID);
            count += helper.Write(buf, TAB);
            count += helper.Write(buf, tree.Domain);
            count += helper.Write(buf, TAB);
            count += helper.Write(buf, tree.HostName);
            count += helper.Write(buf, TAB);
            count += helper.Write(buf, tree.IpAddress);
            count += helper.Write(buf, TAB);
            count += helper.Write(buf, tree.ThreadGroupName);
            count += helper.Write(buf, TAB);
            count += helper.Write(buf, tree.ThreadId);
            count += helper.Write(buf, TAB);
            count += helper.Write(buf, tree.ThreadName);
            count += helper.Write(buf, TAB);
            count += helper.Write(buf, tree.MessageId);
            count += helper.Write(buf, TAB);
            count += helper.Write(buf, tree.ParentMessageId);
            count += helper.Write(buf, TAB);
            count += helper.Write(buf, tree.RootMessageId);
            count += helper.Write(buf, TAB);
            count += helper.Write(buf, tree.SessionToken);
            count += helper.Write(buf, LF);

            return count;
        }

        protected internal int EncodeLine(IMessage message, ChannelBuffer buf, char type, Policy policy)
        {
            BufferHelper helper = _mBufferHelper;
            int count = 0;

            count += helper.Write(buf, (byte)type);

            if (type == 'T' && message is ITransaction)
            {
                long duration = ((ITransaction)message).DurationInMillis;

                count += helper.Write(buf, _mDateHelper.Format(message.Timestamp + duration));
            }
            else
            {
                count += helper.Write(buf, _mDateHelper.Format(message.Timestamp));
            }

            count += helper.Write(buf, TAB);
            count += helper.Write(buf, message.Type);
            count += helper.Write(buf, TAB);
            count += helper.Write(buf, message.Name);
            count += helper.Write(buf, TAB);

            if (policy != Policy.WITHOUT_STATUS)
            {
                count += helper.Write(buf, message.Status);
                count += helper.Write(buf, TAB);

                Object data = message.Data;

                if (policy == Policy.WITH_DURATION && message is ITransaction)
                {
                    long duration0 = ((ITransaction)message).DurationInMicros;

                    count += helper.Write(buf, duration0.ToString(CultureInfo.InvariantCulture));
                    //以微秒为单位
                    count += helper.Write(buf, "us");
                    count += helper.Write(buf, TAB);
                }

                count += helper.WriteRaw(buf, data.ToString());
                count += helper.Write(buf, TAB);
            }

            count += helper.Write(buf, LF);

            return count;
        }

        public int EncodeMessage(IMessage message, ChannelBuffer buf)
        {
            if (message is ITransaction)
            {
                var transaction = message as ITransaction;
                IList<IMessage> children = transaction.Children;

                if (children.Count == 0)
                {
                    return EncodeLine(transaction, buf, 'A', Policy.WITH_DURATION);
                }
                int count = 0;
                int len = children.Count;

                count += EncodeLine(transaction, buf, 't', Policy.WITHOUT_STATUS);

                for (int i = 0; i < len; i++)
                {
                    IMessage child = children[i];

                    count += EncodeMessage(child, buf);
                }

                count += EncodeLine(transaction, buf, 'T', Policy.WITH_DURATION);

                return count;
            }
            if (message is IEvent)
            {
                return EncodeLine(message, buf, 'E', Policy.DEFAULT);
            }
            if (message is IHeartbeat)
            {
                return EncodeLine(message, buf, 'H', Policy.DEFAULT);
            }
            if (message is IMetric)
            {
                return EncodeLine(message, buf, 'M', Policy.DEFAULT);
            }
            throw new Exception(string.Format("Unsupported message type: {0}.", message.Type));
        }

        #region Nested type: BufferHelper

        protected internal class BufferHelper
        {
            private readonly UTF8Encoding _mEncoding = new UTF8Encoding();

            public String Read(ChannelBuffer buf, byte separator)
            {
                int count = buf.BytesBefore(separator);

                if (count < 0)
                {
                    return null;
                }
                byte[] data = new byte[count];

                buf.ReadBytes(data);
                buf.ReadByte(); // get rid of separator

                return Encoding.UTF8.GetString(data);
            }

            public String ReadRaw(ChannelBuffer buf, byte separator)
            {
                int count = buf.BytesBefore(separator);

                if (count < 0)
                {
                    return null;
                }
                byte[] data = new byte[count];

                buf.ReadBytes(data);
                buf.ReadByte(); // get rid of separator

                int length = data.Length;

                for (int i = 0; i < length; i++)
                {
                    if (data[i] == '\\')
                    {
                        if (i + 1 < length)
                        {
                            byte b = data[i + 1];

                            if (b == 't')
                            {
                                data[i] = (byte)'\t';
                            }
                            else if (b == 'r')
                            {
                                data[i] = (byte)'\r';
                            }
                            else if (b == 'n')
                            {
                                data[i] = (byte)'\n';
                            }
                            else
                            {
                                data[i] = b;
                            }

                            Array.Copy(data, i + 2, data, i + 1, length - i - 2);
                            length--;
                        }
                    }
                }

                return Encoding.UTF8.GetString(data, 0, length);
            }

            public int Write(ChannelBuffer buf, byte b)
            {
                buf.WriteByte(b);
                return 1;
            }

            public int Write(ChannelBuffer buf, String str)
            {
                if (str == null)
                {
                    str = "null";
                }

                byte[] data = _mEncoding.GetBytes(str);

                buf.WriteBytes(data);
                return data.Length;
            }

            public int WriteRaw(ChannelBuffer buf, String str)
            {
                if (str == null)
                {
                    str = "null";
                }

                byte[] data = _mEncoding.GetBytes(str);

                int len = data.Length;
                int count = len;
                int offset = 0;

                for (int i = 0; i < len; i++)
                {
                    byte b = data[i];

                    if (b == '\t' || b == '\r' || b == '\n' || b == '\\')
                    {
                        buf.WriteBytes(data, offset, i - offset);
                        buf.WriteByte('\\');

                        if (b == '\t')
                        {
                            buf.WriteByte('t');
                        }
                        else if (b == '\r')
                        {
                            buf.WriteByte('r');
                        }
                        else if (b == '\n')
                        {
                            buf.WriteByte('n');
                        }
                        else
                        {
                            buf.WriteByte(b);
                        }

                        count++;
                        offset = i + 1;
                    }
                }

                if (len > offset)
                {
                    buf.WriteBytes(data, offset, len - offset);
                }

                return count;
            }
        }

        #endregion

        #region Nested type: DateHelper

        ///<summary>
        ///  Thread safe date helper class. DateFormat is NOT thread safe.
        ///</summary>
        protected internal class DateHelper
        {
            public String Format(long timestamp)
            {
                return new DateTime(timestamp * 10000L).ToString("yyyy-MM-dd HH:mm:ss.fff");
            }

            public long Parse(String str)
            {
                DateTime dateTime = DateTime.ParseExact(str, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture);

                return dateTime.Ticks / 10000L;
            }
        }

        #endregion
    }
}