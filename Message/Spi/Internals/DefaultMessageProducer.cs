using Com.Dianping.Cat.Message.Internals;
using System;
using System.IO;

namespace Com.Dianping.Cat.Message.Spi.Internals
{
    public class DefaultMessageProducer : IMessageProducer
    {
        private readonly IMessageManager _mManager;

        public DefaultMessageProducer(IMessageManager manager)
        {
            _mManager = manager;
        }

        public String CreateMessageId()
        {
            return _mManager.GetMessageIdFactory().GetNextId();
        }

        #region IMessageProducer Members

        public virtual void LogError(Exception cause)
        {
            var ignore = false;
            if (cause.Data.Contains("CatIgnore") && cause.Data["CatIgnore"] is bool)
                ignore = (bool)(cause.Data["CatIgnore"]);

            if (ignore) return;

            StringWriter writer = new StringWriter();

            try
            {
                writer.WriteLine(cause.Message);
                writer.WriteLine(cause.StackTrace);
                var innerException = cause.InnerException;
                while (innerException != null)
                {
                    writer.WriteLine("-------------------------------------------------------------------");
                    writer.WriteLine(innerException.Message);
                    writer.WriteLine(innerException.StackTrace);

                    innerException = innerException.InnerException;
                }
            }
            catch
            {
            }

            LogEvent("Error", cause.GetType().FullName, "ERROR",
                     writer.ToString());
        }

        public virtual void LogEvent(String type, String name, String status, String nameValuePairs)
        {
            IEvent evt0 = NewEvent(type, name);

            if (!string.IsNullOrEmpty(nameValuePairs))
            {
                evt0.AddData(nameValuePairs);
            }

            evt0.Status = status;
            evt0.Complete();
        }

        public virtual void LogHeartbeat(String type, String name, String status, String nameValuePairs)
        {
            IHeartbeat heartbeat = NewHeartbeat(type, name);

            if (!string.IsNullOrEmpty(nameValuePairs))
            {
                heartbeat.AddData(nameValuePairs);
            }
            heartbeat.Status = status;
            heartbeat.Complete();
        }

        public virtual void LogMetric(String name, String status, String nameValuePairs)
        {
            String type = string.Empty;
            IMetric metric = NewMetric(type, name);

            if (!string.IsNullOrWhiteSpace(nameValuePairs))
            {
                metric.AddData(nameValuePairs);
            }

            metric.Status = status;
            metric.Complete();
        }

        public virtual IEvent NewEvent(String type, String name)
        {
            if (!_mManager.HasContext())
            {
                _mManager.Setup();
            }

            if (_mManager.CatEnabled)
            {
                IEvent evt0 = new DefaultEvent(type, name);

                _mManager.Add(evt0);
                return evt0;
            }
            return new NullEvent();
        }

        public virtual IHeartbeat NewHeartbeat(String type, String name)
        {
            if (!_mManager.HasContext())
            {
                _mManager.Setup();
            }

            if (_mManager.CatEnabled)
            {
                IHeartbeat heartbeat = new DefaultHeartbeat(type, name);

                _mManager.Add(heartbeat);
                return heartbeat;
            }
            return new NullHeartbeat();
        }

        public virtual ITransaction NewTransaction(String type, String name)
        {
            // this enable CAT client logging cat message without explicit setup
            if (!_mManager.HasContext())
            {
                _mManager.Setup();
            }

            if (_mManager.CatEnabled)
            {
                ITransaction transaction = new DefaultTransaction(type, name, _mManager.End);

                _mManager.Start(transaction);
                return transaction;
            }
            return new NullTransaction();
        }

        /// <summary>
        /// new metric
        /// </summary>
        /// <param name="type">group</param>
        /// <param name="name">key</param>
        public virtual IMetric NewMetric(String type, String name)
        {
            // this enable CAT client logging cat message without explicit setup
            if (!_mManager.HasContext())
            {
                _mManager.Setup();
            }

            if (_mManager.CatEnabled)
            {
                IMetric metric = new DefaultMetric(string.IsNullOrWhiteSpace(type) ? string.Empty : type, name);

                _mManager.Add(metric);
                return metric;
            }
            return new NullMetric();
        }

        #endregion

    }
}