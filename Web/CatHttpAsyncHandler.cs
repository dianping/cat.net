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
    public class CatHttpAsyncHandler : IHttpAsyncHandler, IRequiresSessionState
    {
        private ITransaction tran = null;
        private IHttpAsyncHandler asyncHandler;

        public bool IsReusable { get { return asyncHandler.IsReusable; } }

        public CatHttpAsyncHandler(IHttpAsyncHandler asyncHandler)
        {
            this.asyncHandler = asyncHandler;
        }

        public IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
        {
            if (asyncHandler == null)
                return null;
            tran = CatHelper.BeginServerTransaction("URL", response: context.Response);
            try
            {
                return asyncHandler.BeginProcessRequest(context, cb, extraData);
            }
            catch (Exception ex)
            {
                CatHelper.SetTrancationStatus(tran, ex);
                throw;
            }
        }

        public void EndProcessRequest(IAsyncResult result)
        {
            if (asyncHandler == null)
                return;
            try
            {
                asyncHandler.EndProcessRequest(result);
            }
            catch (Exception ex)
            {
                CatHelper.SetTrancationStatus(tran, ex);
                throw;
            }
            finally
            {
                CatHelper.CompleteTrancation(tran);
            }
        }

        public void ProcessRequest(HttpContext context)
        {
            tran = CatHelper.BeginServerTransaction("URL", response: context.Response);
            try
            {
                asyncHandler.ProcessRequest(context);
            }
            catch (Exception ex)
            {
                var baseEx = ex.GetBaseException();
                if (baseEx is ThreadAbortException)
                {
                    return;
                }
                CatHelper.SetTrancationStatus(tran, baseEx);
                throw;
            }
            finally
            {
                CatHelper.CompleteTrancation(tran);
            }
        }
    }
}
