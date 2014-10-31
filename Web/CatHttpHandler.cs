using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.SessionState;
using Com.Dianping.Cat.Util;

namespace Com.Dianping.Cat.Web
{
    public class CatHttpHandler : IHttpHandler, IRequiresSessionState
    {
        public IHttpHandler Handler { get; set; }
        public bool IsReusable { get { return Handler.IsReusable; } }

        public void ProcessRequest(HttpContext context)
        {
            var tran = CatHelper.BeginServerTransaction(context.Request.Url.ToString(), Handler.GetType().FullName);
            try
            {
                Handler.ProcessRequest(context);
            }
            catch (Exception ex)
            {
                CatHelper.SetTrancationStatus(tran, ex);
                CatHelper.CompleteTrancation(tran);
                throw ex;
            }
            CatHelper.CompleteTrancation(tran);
        }
    }
}
