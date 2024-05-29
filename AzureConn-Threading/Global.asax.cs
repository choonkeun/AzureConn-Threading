using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

namespace AzureConn
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
        }
        protected void Application_Error(object sender, EventArgs e)
        {
            if (null != System.Web.HttpContext.Current.Session)
            {
                HttpContext.Current.Response.Write("<hr/>Session ERROR:" + e.ToString());
            }
        }
        protected void Application_End(object sender, EventArgs e)
        {
            HttpContext.Current.Response.Write("<hr/>Session End:" + e.ToString());
        }
    }
}