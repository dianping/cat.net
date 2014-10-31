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
            try
            {
                var context = System.Web.HttpContext.Current;
                var request = System.Web.HttpContext.Current.Request;

                if (filter_Handler(context))//如果被过滤了，就直接跳过
                    return;

                var handler = new CatHttpHandler { Handler = context.Handler };
                context.Handler = handler;
            }
            catch (Exception)
            {

            }
        }



        bool filter_Handler(HttpContext context)
        {
            var request = context.Request;
            var handlerType = context.Handler.GetType();
            var attibutes = handlerType.GetCustomAttributes(typeof(Attribute), false);

            if (context == null)
                return true;

            if (context.Handler.GetType().FullName.Equals("System.Web.Handlers.TransferRequestHandler", StringComparison.OrdinalIgnoreCase))
                return true;

            if (context.Handler == null && File.Exists(request.PhysicalPath))
                return true;

            if (attibutes.Any(p => p.GetType().FullName == "TongCheng.SOA.Interface.Attributes.SOABusiness"))
                return true;

            return false;
        }

        public void Dispose()
        {
        }
    }
}
