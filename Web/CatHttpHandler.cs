using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.SessionState;
using Com.Dianping.Cat.Util;
using System.Threading;
using Com.Dianping.Cat.Message;

namespace Com.Dianping.Cat.Web
{
    public class CatHttpHandler : IHttpHandler, IRequiresSessionState
    {
        private ITransaction tran = null;
        private IHttpHandler handler;

        public bool IsReusable { get { return handler.IsReusable; } }

        public CatHttpHandler(IHttpHandler httpHandler)
        {
            this.handler = httpHandler;
        }

        public void ProcessRequest(HttpContext context)
        {
            tran = CatHelper.BeginServerTransaction("URL", response: context.Response);
            try
            {
                handler.ProcessRequest(context);
            }
            catch (Exception ex)
            {
                var baseEx = ex.GetBaseException();
                if (baseEx is ThreadAbortException)
                {
                    return;
                }
                tran.SetStatus(ex);
                throw;
            }
            finally
            {
                tran.Complete();
            }
        }
    }
}
