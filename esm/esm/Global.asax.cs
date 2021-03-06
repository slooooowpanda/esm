﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace esm
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            System.IO.File.WriteAllText(Server.MapPath("~") + "/App_Data/user_task.txt", "");
            System.IO.File.WriteAllText(Server.MapPath("~") + "/App_Data/OnlineUsers.txt", "");
            System.IO.File.WriteAllText(Server.MapPath("~") + "/App_Data/counter.txt", "0");
        }
    }
}
