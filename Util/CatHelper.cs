using Com.Dianping.Cat.Message;
using Com.Dianping.Cat.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace Com.Dianping.Cat.Util
{
    public class CatHelper
    {
        public const string CatRootId = "X-Cat-RootId";
        public const string CatParentId = "X-Cat-ParentId";
        public const string CatId = "X-Cat-Id";

        public static ITransaction BeginClientTransaction(string name, string requestingUrl, WebRequest request)
        {
            if (!Cat.IsInitialized() || !Cat.GetManager().CatEnabled)
                return null;
            try
            {
                var tran = Cat.GetProducer().NewTransaction("HttpCall", name);
                tran.Status = "0";
                Cat.GetProducer().LogEvent("HttpCall.Server", "HttpRequest", "0", requestingUrl);
                var tree = Cat.GetManager().ThreadLocalMessageTree;
                if (tree == null)
                {
                    Cat.GetManager().Setup();
                    tree = Cat.GetManager().ThreadLocalMessageTree;
                }
                string serverMessageId = Cat.GetProducer().CreateMessageId();
                string rootMessageId = (tree.RootMessageId ?? tree.MessageId);
                string currentMessageId = tree.MessageId;
                Cat.GetProducer().LogEvent("RemoteCall", "HttpRequest", "0", serverMessageId);

                if (request != null)
                {
                    request.Headers.Add(CatHelper.CatRootId, rootMessageId);
                    request.Headers.Add(CatHelper.CatParentId, currentMessageId);
                    request.Headers.Add(CatHelper.CatId, serverMessageId);
                }

                return tran;
            }
            catch
            {
                return null;
            }
        }

        public static ITransaction BeginServerTransaction(string url, string name)
        {
            if (!Cat.IsInitialized() || !Cat.GetManager().CatEnabled)
                return null;
            try
            {
                var httpContext = System.Web.HttpContext.Current;
                if (httpContext == null) { return null; }
                var request = httpContext.Request;
                string rootMessageId = request.Headers[CatHelper.CatRootId];
                string serverMessageId = request.Headers[CatHelper.CatParentId];
                string currentMessageId = request.Headers[CatHelper.CatId];

                var tran = Cat.GetProducer().NewTransaction("HttpService", url);
                var tree = Cat.GetManager().ThreadLocalMessageTree;
                if (tree == null)
                {
                    Cat.GetManager().Setup();
                    tree = Cat.GetManager().ThreadLocalMessageTree;
                }
                tree.RootMessageId = rootMessageId;
                tree.ParentMessageId = serverMessageId;
                if (!string.IsNullOrEmpty(currentMessageId))
                {
                    tree.MessageId = currentMessageId;
                }
                tran.Status = "0";
                Cat.GetProducer().LogEvent("HttpService.Server", name, "0", AppEnv.GetClientIp(request));
                return tran;
            }
            catch
            {
                return null;
            }
        }

        public static ITransaction BeginTransaction(string type, string name)
        {
            if (!Cat.IsInitialized() || !Cat.GetManager().CatEnabled)
                return null;
            try
            {
                var tran = Cat.GetProducer().NewTransaction(type, name);
                tran.Status = "0";
                return tran;
            }
            catch
            {
                return null;
            }
        }

        public static void AddServerEvent(string type, string name)
        {
            if (!Cat.IsInitialized() || !Cat.GetManager().CatEnabled)
                return;
            Cat.GetProducer().LogEvent(type, name, "0", null);
        }

        public static string GetRootMessageId()
        {
            if (!Cat.IsInitialized() || !Cat.GetManager().CatEnabled)
                return string.Empty;
            var tree = Cat.GetManager().ThreadLocalMessageTree;
            if (tree == null)
            {
                Cat.GetManager().Setup();
                tree = Cat.GetManager().ThreadLocalMessageTree;
            }

            string rootId = tree.RootMessageId;
            if (string.IsNullOrEmpty(rootId))
            {
                rootId = tree.MessageId;
            }
            return rootId;
        }

        public static string GetMessageId()
        {
            if (!Cat.IsInitialized() || !Cat.GetManager().CatEnabled)
                return string.Empty;
            var tree = Cat.GetManager().ThreadLocalMessageTree;
            if (tree == null)
            {
                Cat.GetManager().Setup();
                tree = Cat.GetManager().ThreadLocalMessageTree;
            }
            return tree.MessageId;
        }

        public static void SetTrancationStatus(ITransaction tran, string status)
        {
            if (tran != null)
            {
                tran.Status = status;
            }
        }

        public static void SetTrancationStatus(ITransaction tran, Exception exception)
        {
            if (tran != null)
            {
                tran.SetStatus(exception);
            }
        }

        public static void CompleteTrancation(ITransaction tran)
        {
            if (tran != null)
            {
                tran.Complete();
            }
        }
    }
}
