using System.Globalization;

namespace Com.Dianping.Cat.Message.Internals
{
    using Dianping.Cat;
    using Configuration;
    using Message;
    using Spi;
    using Spi.Internals;
    using System;
    using System.Collections.Generic;
    using System.Threading;


    public class DefaultMessageManager : IMessageManager
    {
        // we don't use static modifier since MessageManager is a singleton in
        // production actually
        private readonly CatThreadLocal<Context> _mContext = new CatThreadLocal<Context>();

        private ClientConfig _mClientConfig;

        private MessageIdFactory _mFactory;

        private bool _mFirstMessage = true;
        private String _mHostName;

        #region 未用到的方法

        //public virtual TransportManager TransportManager
        //{
        //    get { return _mManager; }
        //}

        public virtual ClientConfig ClientConfig
        {
            get { return _mClientConfig; }
        }

        public virtual ITransaction PeekTransaction
        {
            get
            {
                Context ctx = GetContext();

                return ctx != null ? ctx.PeekTransaction() : null;
            }
        }

        public virtual IMessageTree ThreadLocalMessageTree
        {
            get
            {
                Context ctx = _mContext.Value;

                return ctx != null ? ctx.Tree : null;
            }
        }

        public virtual void Reset()
        {
            // destroy current thread local data
            _mContext.Dispose();
        }

        public MessageIdFactory GetMessageIdFactory()
        {
            return _mFactory;
        }

        #endregion

        //private TransportManager _mManager;
        //private IMessageStatistics _mStatistics;

        //private StatusUpdateTask _mStatusUpdateTask;

        #region IMessageManager Members

        public virtual void InitializeClient(ClientConfig clientConfig)
        {
            _mClientConfig = clientConfig ?? new ClientConfig();

            _mHostName = NetworkInterfaceManager.GetLocalHostName();

            //_mStatistics = new DefaultMessageStatistics();
            //_mManager = new TransportManager(_mClientConfig, _mStatistics);
            _mFactory = new MessageIdFactory();
            //_mStatusUpdateTask = new StatusUpdateTask(_mStatistics);

            // initialize domain and ip address
            _mFactory.Initialize(_mClientConfig.Domain.Id);

            //// start status update task
            //ThreadPool.QueueUserWorkItem(_mStatusUpdateTask.Run);

            Logger.Info("Thread(StatusUpdateTask) started.");
        }

        public virtual bool HasContext()
        {
            return _mContext.Value != null;
        }

        public virtual bool CatEnabled
        {
            get { return _mClientConfig.Domain.Enabled && _mContext.Value != null; }
        }

        public virtual void Add(IMessage message)
        {
            Context ctx = GetContext();

            if (ctx != null)
            {
                ctx.Add(this, message);
            }
            else
                Logger.Warn("Context没取到");
        }

        public virtual void Setup()
        {
            Context ctx = new Context(_mClientConfig.Domain.Id, _mHostName,
                                      NetworkInterfaceManager.GetLocalHostAddress());

            _mContext.Value = ctx;
        }

        public virtual void Start(ITransaction transaction)
        {
            Context ctx = GetContext();

            if (ctx != null)
            {
                ctx.Start(this, transaction);
            }
            else if (_mFirstMessage)
            {
                _mFirstMessage = false;
                Logger.Info("CAT client is not enabled because it's not initialized yet");
            }
            else
                Logger.Warn("Context没取到");
        }

        public virtual void End(ITransaction transaction)
        {
            Context ctx = GetContext();

            if (ctx != null)
            {
                //if (!transaction.Standalone) return;
                if (ctx.End(this, transaction))
                {
                    _mContext.Dispose();
                }
            }
            else
                Logger.Warn("Context没取到");
        }

        #endregion

        internal void Flush(IMessageTree tree)
        {
            //if (_mManager != null)
            //{
            //    IMessageSender sender = _mManager.Sender;

            //    if (sender != null && !ShouldThrottle(tree))
            //    {
            //        sender.Send(tree);

            //        if (_mStatistics != null)
            //        {
            //            _mStatistics.OnSending(tree);
            //        }
            //    }
            //}
        }

        internal Context GetContext()
        {
            if (Cat.IsInitialized())
            {
                Context ctx = _mContext.Value;

                if (ctx != null)
                {
                    return ctx;
                }
                if (_mClientConfig.DevMode)
                {
                    throw new Exception(
                        "Cat has not been initialized successfully, please call Cal.setup(...) first for each thread.");
                }
            }

            return null;
        }

        internal String NextMessageId()
        {
            return _mFactory.GetNextId();
        }

        //internal bool ShouldThrottle(IMessageTree tree)
        //{
        //    return false;
        //}

        #region Nested type: Context

        internal class Context
        {
            private readonly Stack<ITransaction> _mStack;
            private readonly IMessageTree _mTree;

            public Context(String domain, String hostName, String ipAddress)
            {
                _mTree = new DefaultMessageTree();
                _mStack = new Stack<ITransaction>();

                Thread thread = Thread.CurrentThread;
                String groupName = Thread.GetDomain().FriendlyName;

                _mTree.ThreadGroupName = groupName;
                _mTree.ThreadId = thread.ManagedThreadId.ToString(CultureInfo.InvariantCulture);
                _mTree.ThreadName = thread.Name;

                _mTree.Domain = domain;
                _mTree.HostName = hostName;
                _mTree.IpAddress = ipAddress;
            }

            public IMessageTree Tree
            {
                get { return _mTree; }
            }

            /// <summary>
            ///   添加Event和Heartbeat
            /// </summary>
            /// <param name="manager"> </param>
            /// <param name="message"> </param>
            public void Add(DefaultMessageManager manager, IMessage message)
            {
                if ((_mStack.Count == 0))
                {
                    IMessageTree tree = _mTree.Copy();
                    tree.MessageId = manager.NextMessageId();
                    tree.Message = message;
                    manager.Flush(tree);
                }
                else
                {
                    ITransaction entry = _mStack.Peek();
                    entry.AddChild(message);
                }
            }

            ///<summary>
            ///  return true means the transaction has been flushed.
            ///</summary>
            ///<param name="manager"> </param>
            ///<param name="transaction"> </param>
            ///<returns> true if message is flushed, false otherwise </returns>
            public bool End(DefaultMessageManager manager, ITransaction transaction)
            {
                if (_mStack.Count != 0)
                {
                    ITransaction current = _mStack.Pop();
                    while (transaction != current && _mStack.Count != 0)
                    {
                        current = _mStack.Pop();
                    }
                    if (transaction != current)
                        throw new Exception("没找到对应的Transaction.");

                    if (_mStack.Count == 0)
                    {
                        ValidateTransaction(current);

                        IMessageTree tree = _mTree.Copy();
                        _mTree.MessageId = null;
                        _mTree.Message = null;
                        manager.Flush(tree);
                        return true;
                    }
                    return false;
                }

                throw new Exception("Stack为空, 没找到对应的Transaction.");
            }

            /// <summary>
            ///   返回stack的顶部对象
            /// </summary>
            /// <returns> </returns>
            public ITransaction PeekTransaction()
            {
                return (_mStack.Count == 0) ? null : _mStack.Peek();
            }

            /// <summary>
            ///   添加transaction
            /// </summary>
            /// <param name="manager"> </param>
            /// <param name="transaction"> </param>
            public void Start(DefaultMessageManager manager, ITransaction transaction)
            {
                if (_mStack.Count != 0)
                {
                    transaction.Standalone = false;
                    ITransaction entry = _mStack.Peek();
                    entry.AddChild(transaction);
                }
                else
                {
                    _mTree.MessageId = manager.NextMessageId();
                    _mTree.Message = transaction;
                }

                _mStack.Push(transaction);
            }

            //验证Transaction
            internal void ValidateTransaction(ITransaction transaction)
            {
                IList<IMessage> children = transaction.Children;
                int len = children.Count;
                for (int i = 0; i < len; i++)
                {
                    IMessage message = children[i];
                    var transaction1 = message as ITransaction;
                    if (transaction1 != null)
                    {
                        ValidateTransaction(transaction1);
                    }
                }

                if (!transaction.IsCompleted())
                {
                    // missing transaction end, log a BadInstrument event so that
                    // developer can fix the code
                    IMessage notCompleteEvent = new DefaultEvent("CAT", "BadInstrument")
                                                    {Status = "TransactionNotCompleted"};
                    notCompleteEvent.Complete();
                    transaction.AddChild(notCompleteEvent);
                    transaction.Complete();
                }
            }
        }

        #endregion
    }
}