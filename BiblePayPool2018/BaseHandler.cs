using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Web.SessionState;

namespace BiblePayPool2018
{
    public abstract class BaseHttpHandler : IHttpHandler, IRequiresSessionState
    {
        /// <summary>
        /// Creates a new <see cref="BaseHttpHandler"/> instance.
        /// </summary>
        public BaseHttpHandler() { }
        public string sPostData = string.Empty;

        public void ProcessRequest(HttpContext context)
        {
            SetResponseCachePolicy(context.Response.Cache);
            if ( !context.User.Identity.IsAuthenticated && false )
            {
                RespondForbidden(context);
                return;
            }

            if (HttpContext.Current.Session["Sys"]==null)
            {
                // No System object yet
                SystemObject s = new SystemObject("1");
                s.Username = "PAGE"; //Temporary values (until user logs in)
                s.CurrentHTTPContext = HttpContext.Current;
                s.IP = (HttpContext.Current.Request.UserHostAddress ?? "").ToString();

                HttpContext.Current.Session["Sys"] = s;
            }
            SystemObject Sys = (SystemObject)HttpContext.Current.Session["Sys"];
            context.Response.ContentType = "text/html"; // ContentMimeType;
            sPostData = (context.Request["post"] ?? String.Empty).ToString();
            Sys.iCount++;

            if (sPostData == String.Empty)
            {
                // This is the initial call during page load
                string style = "";
                USGDFramework.Data d = new USGDFramework.Data();
                string sURL1 = HttpContext.Current.Request.Url.ToString();
                string sMenu = d.GetTopLevelMenu(sURL1);
                if (Sys.Theme==null) Sys.Theme="Biblepay";
                string sTheme = "css/" + Sys.Theme + ".css";
                string sJQuery = "<script src='https://code.jquery.com/jquery-1.12.4.js'></script><script src='https://code.jquery.com/ui/1.12.1/jquery-ui.js'></script>";
                string sCss = " <link rel=stylesheet href='https://code.jquery.com/ui/1.12.1/themes/base/jquery-ui.css'> "
                    + "<link rel='stylesheet' href='https://jqueryui.com/resources/demos/style.css'>"
                    + "<link rel=stylesheet href='"+ sTheme + "'>"
                    + "";
                // Top banner (Note this is dynamic - if you come in from the pool, it says Biblepay Pool, but if you come in from accountability.biblepay.org, it says "Biblepay Accountability"
                
                string sSiteURL = HttpContext.Current.Request.Url.ToString();
                bool bDAHF = (HttpContext.Current.Request.Url.ToString().ToUpper().Contains("DAHF"));

                string sSiteBanner = sSiteURL.ToUpper().Contains("ACCOUNTABILITY") ? "Biblepay Accountability" : "Biblepay Pool";
                if (bDAHF) sSiteBanner = "BiblePay DAHF";

                string sBanner = "<div id='top_banner'><table width='100%' class='title2'> <tr>         <td  rowspan=2 width=15%>"
                 + "<img class='content companylogo' id='org_image' src=Images/logo.png width=90 height=90 /> </td> "
                 + "<td width=20% nowrap align=left>"
                 + "<h1><bold><span id='org_name'>" + sSiteBanner + "</span></h1>"
                 + "</td><td width=50%>&nbsp;</td>"
                 + "<td width=15% nowrap align=left></td>"
                 +"         <td width=8%>&nbsp;</td>"
                 +"         <td width=8%>&nbsp;</td></tr>"
                 +"         <tr><td width=37% nowrap align=left>&nbsp;</td><td align=left>&nbsp;</td><td>&nbsp;</td>"
                 +"         <td>&nbsp;</td>"
                 +"         </tr>"
                 +"         <tr><td></td><td width=10%></td>"
                 +"         <td><h7><span name=ApplicationMessage>Application Message</span></h7>"
                 +"             <span align=right><div id=12></div></span> "
                 +"         </td></tr>"
                 +"         </table></div>";

                sJQuery += "<script src='/scripts/jquery.uploadify.js'></script>";
                sJQuery += "<script src='scripts/Core.js'></script>";
                sJQuery += "<script src='/scripts/jquery.contextMenu.js'></script>";
                sJQuery += "<script src='/scripts/featherlight.js'></script>";
                sJQuery += "<script src='/scripts/featherlight.gallery.js'></script>";

                string sOut = "<html><head>" + style + sJQuery + " <link rel='stylesheet' type='text/css' href='/scripts/xuploadify.css' /> " 
                    + sCss
                    + " </head><body onload=formload();>" + sBanner
                    + "<table><tr valign=top><td width=10%>" + sMenu + "</td><td width=2%>&nbsp;</td><td width=86%>"
                    + "<div name=divbreadcrumb></div><div name=1><div name=2><div name=3></div></div></div></td><td width=2%>&nbsp;</td></tr></table></body></html>";
                context.Response.Write(sOut);
                return;
            }
       
            HandleRequest(context);
        }

        public bool IsReusable
        {
            get
            {
                return true;
            }
        }

        public abstract void HandleRequest(HttpContext context);

        public virtual void SetResponseCachePolicy
            (HttpCachePolicy cache)
        {
            cache.SetCacheability(HttpCacheability.NoCache);
            cache.SetNoStore();
            cache.SetExpires(DateTime.MinValue);
        }

        protected void RespondFileNotFound(HttpContext context)
        {
            context.Response.StatusCode = 404;
            context.Response.End();
        }

        protected void RespondInternalError(HttpContext context)
        {
            context.Response.StatusCode =  (int)HttpStatusCode.InternalServerError;
            context.Response.End();
        }

        protected void RespondForbidden(HttpContext context)
        {
            context.Response.StatusCode                 = (int)HttpStatusCode.Forbidden;
            context.Response.End();
        }
    }
}