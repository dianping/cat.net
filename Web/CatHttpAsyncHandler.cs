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
        private IHttpAsyncHandler asyncHandler;

        public bool IsReusable { get { return asyncHandler.IsReusable; } }

        public CatHttpAsyncHandler(IHttpAsyncHandler asyncHandler)
        {
            this.asyncHandler = asyncHandler;
        }

        public IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
        {
            var tran = CatHelper.BeginServerTransaction("URL", response: context.Response);
            try
            {
                if (extraData == null)
                    extraData = tran;

                return asyncHandler.BeginProcessRequest(context, cb, extraData);
            }
            catch (Exception ex)
            {
                Cat.GetProducer().LogError(ex);
                tran.SetStatus(ex);
                throw;
            }
        }

        public void EndProcessRequest(IAsyncResult result)
        {
            ITransaction tran = null;
            try
            {
                var extraData = result.AsyncState as ITransaction;
                if (extraData != null)
                    tran = extraData;

                asyncHandler.EndProcessRequest(result);
            }
            catch (Exception ex)
            {
                Cat.GetProducer().LogError(ex);
                tran.SetStatus(ex);
                throw;
            }
            finally
            {
                tran.Complete();
            }
        }

        public void ProcessRequest(HttpContext context)
        {
            var tran = CatHelper.BeginServerTransaction("URL", response: context.Response);
            try
            {
                asyncHandler.ProcessRequest(context);
            }
            catch (Exception ex)
            {
                if (ex.GetBaseException() is ThreadAbortException)
                {
                    return;
                }
                Cat.GetProducer().LogError(ex);
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
