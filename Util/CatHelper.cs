using Com.Dianping.Cat.Message;
using Com.Dianping.Cat.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace Com.Dianping.Cat.Util
{
    public static class CatHelper
    {
        public class CatRequestMessage
        {
            public string CatRootId { get; private set; }
            public string CatParentId { get; private set; }
            public string CatId { get; private set; }
            public string RequestMothed { get; private set; }

            public CatRequestMessage(string catRootId, string catParentId, string catId, string requestMothed)
            {
                CatRootId = catRootId;
                CatParentId = catParentId;
                CatId = catId;
                RequestMothed = requestMothed;
            }
        }

        private const string CatRootId = "X-Cat-RootId";
        private const string CatParentId = "X-Cat-ParentId";
        private const string CatId = "X-Cat-Id";

        public static ITransaction BeginClientTransaction(string type, string name, WebRequest request)
        {
            var tran = Cat.NewTransaction(type, name);
            tran.Status = "0";
            Cat.LogEvent(type, request.RequestUri.AbsolutePath, "0", request.RequestUri.ToString());
            var tree = Cat.GetManager().ThreadLocalMessageTree;
            if (tree == null)
            {
                Cat.GetManager().Setup();
                tree = Cat.GetManager().ThreadLocalMessageTree;
            }
            string serverMessageId = Cat.GetProducer().CreateMessageId();
            string rootMessageId = (tree.RootMessageId ?? tree.MessageId);
            string currentMessageId = tree.MessageId;
            Cat.LogEvent("RemoteCall", "HttpRequest", "0", serverMessageId);

            request.Headers.Add(CatHelper.CatRootId, rootMessageId);
            request.Headers.Add(CatHelper.CatParentId, currentMessageId);
            request.Headers.Add(CatHelper.CatId, serverMessageId);

            return tran;
        }

        public static ITransaction BeginServerTransaction(string type, string name = null, HttpResponse response = null)
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

                if (string.IsNullOrWhiteSpace(name))
                    name = request.Path;

                var tran = Cat.GetProducer().NewTransaction(type, name);
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

                if (response != null)
                {
                    if (!string.IsNullOrWhiteSpace(rootMessageId))
                        response.AddHeader(CatHelper.CatRootId, rootMessageId);
                    if (!string.IsNullOrWhiteSpace(currentMessageId))
                        response.AddHeader(CatHelper.CatParentId, currentMessageId);
                    response.AddHeader(CatHelper.CatId, tree.MessageId);
                }

                Cat.GetProducer().LogEvent(type, type + ".Server", "0", getURLServerValue(request));
                Cat.GetProducer().LogEvent(type, type + ".Method", "0", getURLMethodValue(request));
                Cat.GetProducer().LogEvent(type, type + ".Client", "0", AppEnv.GetClientIp(request));
                return tran;
            }
            catch
            {
                return null;
            }
        }

        public static ITransaction BeginRequestTransaction(out CatRequestMessage catRequestMessage, string type, string name, string requestMothed)
        {
            var tran = Cat.NewTransaction(type, name);
            Cat.LogEvent(type, name, "0", string.Format("Mothed.Request : {0}", requestMothed));
            var tree = Cat.GetManager().ThreadLocalMessageTree;
            if (tree == null)
            {
                Cat.GetManager().Setup();
                tree = Cat.GetManager().ThreadLocalMessageTree;
            }
            string serverMessageId = Cat.GetProducer().CreateMessageId();
            string rootMessageId = (tree.RootMessageId ?? tree.MessageId);
            string currentMessageId = tree.MessageId;
            Cat.LogEvent("RemoteCall", "Request", "0", serverMessageId);

            catRequestMessage = new CatRequestMessage(rootMessageId, currentMessageId, serverMessageId, name);

            return tran;
        }

        public static ITransaction BeginResponseTransaction(CatRequestMessage catRequestMessage, string type, string responseMothed)
        {
            if (catRequestMessage == null)
                throw new ArgumentNullException("catRequestMessage");
            string rootMessageId = catRequestMessage.CatRootId;
            string serverMessageId = catRequestMessage.CatParentId;
            string currentMessageId = catRequestMessage.CatId;

            var tran = Cat.NewTransaction(type, responseMothed);
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

            Cat.LogEvent(type, responseMothed, "0", string.Format("Mothed.Response : {0}", catRequestMessage.RequestMothed));

            return tran;
        }


        public static string GetRootMessageId()
        {
            if (!Cat.IsInitialized() || !Cat.GetManager().CatEnabled)
            {
                return string.Empty;
            }
            var tree = Cat.GetManager().ThreadLocalMessageTree;
            if (tree == null)
            {
                Cat.GetManager().Setup();
                tree = Cat.GetManager().ThreadLocalMessageTree;
            }

            return string.IsNullOrEmpty(tree.RootMessageId) ? tree.MessageId : tree.RootMessageId;
        }

        public static string GetMessageId()
        {
            if (!Cat.IsInitialized()) { return string.Empty; }
            if (!Cat.GetManager().CatEnabled) { return string.Empty; }
            var tree = Cat.GetManager().ThreadLocalMessageTree;
            if (tree == null)
            {
                Cat.GetManager().Setup();
                tree = Cat.GetManager().ThreadLocalMessageTree;
            }
            return tree.MessageId;
        }

        #region url info
        private static string getURLServerValue(HttpRequest request)
        {
            if (request == null)
                return string.Empty;
            StringBuilder sb = new StringBuilder();
            sb.Append("IPS=").Append(AppEnv.GetClientIp(request));
            sb.Append("&VirtualIP=").Append(AppEnv.GetRemoteIp(request));
            sb.Append("&Server=").Append(AppEnv.IP);
            sb.Append("&Referer=").Append(request.getHeader("Referer"));
            sb.Append("&Agent=").Append(request.getHeader("User-Agent"));
            return sb.ToString();
        }

        private static string getURLMethodValue(HttpRequest request)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(request.getServerVariables("Request_Method")).Append(" ");
            sb.Append(request.Url.ToString());
            return sb.ToString();
        }

        private static string getHeader(this HttpRequest request, string key)
        {
            if (request == null)
                request = HttpContext.Current.Request;
            var headerKey = request.Headers.AllKeys.Where(p => p.Equals(key, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            return request.Headers[headerKey] ?? "null";
        }

        private static string getServerVariables(this HttpRequest request, string key)
        {
            if (request == null)
                request = HttpContext.Current.Request;
            var headerKey = request.ServerVariables.AllKeys.Where(p => p.Equals(key, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            return request.ServerVariables[headerKey] ?? "null";
        }
        #endregion

    }
}
