using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Com.Dianping.Cat.Message;
using Com.Dianping.Cat.Util;
using System.IO;

namespace Com.Dianping.Cat.Web
{
    public class CatHttpModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.PostMapRequestHandler += context_PostMapRequestHandler;
        }

        void context_PostMapRequestHandler(object sender, EventArgs e)
        {
            if (!Cat.IsInitialized() || !Cat.GetManager().CatEnabled)
            {
                return;
            }

            var context = System.Web.HttpContext.Current;

            if (context == null || context.Handler == null)
                return;

            if (context.Handler is IHttpAsyncHandler)
                context.Handler = new CatHttpAsyncHandler((IHttpAsyncHandler)context.Handler);
            else
                context.Handler = new CatHttpHandler(context.Handler);
        }

        public void Dispose()
        {
        }
    }
}
