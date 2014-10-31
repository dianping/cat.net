using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace Com.Dianping.Cat.Util
{
    public static class AppEnv
    {

        static AppEnv()
        {
            IP = getLocalIP();
        }

        public static string IP { get; private set; }

        private static string getLocalIP()
        {
            try
            {
                return NetworkInterfaceManager.GetLocalHostAddress();
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string GetClientIp(HttpRequest request)
        {
            if (request == null)
            {
                return string.Empty;
            }
            var serverVariables = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (!string.IsNullOrEmpty(serverVariables))
            {
                var items = serverVariables.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (items.Length > 0)
                {
                    return items[0];
                }
            }
            return request.ServerVariables["REMOTE_ADDR"];
        }
    }
}
