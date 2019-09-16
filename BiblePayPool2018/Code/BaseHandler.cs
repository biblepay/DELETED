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
               
                if (Sys.Theme==null || Sys.Theme=="") Sys.Theme="Dark";
                string sTheme = "css/" + Sys.Theme + ".css";
                string sJQuery = "<script src='https://code.jquery.com/jquery-1.12.4.js'>"
                    +"</script><script src='scripts/jquery-ui.js'></script>";
                string sCss = "<link rel='stylesheet' href='"+ sTheme + "'>";
                // Top banner (Note this is dynamic - if you come in from the pool, it says Biblepay Pool, but if you come in from accountability.biblepay.org, it says "Biblepay Accountability"
                
                string sSiteURL = HttpContext.Current.Request.Url.ToString();
                
                string sSiteBanner = sSiteURL.ToUpper().Contains("ACCOUNTABILITY") ? "Biblepay Accountability" : "Biblepay Pool";
                //Table width100%: 
                string sBanner = "<div id='top_banner'><table class='title2'> <tr><td rowspan=2>"
                 + "<img class='content companylogo' alt='BiblePay Logo' id='org_image' src='Images/logo.png' width='90' height='90' /> </td> "
                 + "<td style='width:15%'>"
                 + "<h1><span style='font-size:25px' id='org_name'>" + sSiteBanner + "</span></h1>"
                 + "</td><td style='width:50%'>&nbsp;</td>"
                 + "<td style='width:15%;white-space:nowrap;text-align:left'></td>"
                   + "         <td style='width:8%'>&nbsp;</td>"
                 +"         <td style='width:8%'>&nbsp;</td></tr>"
                 +"         <tr><td style='width:37%;white-space:nowrap;text-align:left'>&nbsp;</td>"
                 +"<td style='text-align:left'>&nbsp;</td><td>&nbsp;</td>"
                 +"         <td>&nbsp;</td><td></td>"
                 +"         </tr>"
                 +"         <tr><td></td><td style='width:10%'></td>"
                 + "         <td>"
                 +"<span style='text-align:left;visibility:hidden' id='ApplicationMessage'></span>"
                  + "             <span style='text-align:right'></span> "
                 + "         </td><td></td><td></td><td></td></tr><tr><td colspan=3><div id='divbreadcrumb'></div></td><td><td><td></tr>"
                 + "         </table></div>";

                sJQuery += "<script src='/scripts/jquery.uploadify.js'></script>";
                sJQuery += "<script src='scripts/Core.js?ver=3'></script>";
                sJQuery += "<script src='/scripts/jquery.contextMenu.js'></script>";
                sJQuery += "<script src='/scripts/featherlight.js'></script>";
                sJQuery += "<script src='/scripts/featherlight.gallery.js'></script>";


                string sOut = "<!DOCTYPE html><html lang='en'><head><title>BiblePay Pool</title>" 
                    + style + sJQuery + ""
                    + sCss + " </head><body onload='formload();' onbeforeunload='formunload();'>" + sBanner;

                if (false)
                {
                    sOut += "<table><tr valign=top><td width=10%>"
                        + sMenu + "</td><td width=2%>&nbsp;</td><td width=86% style='overflow:scroll' >"
                        + "<div id='divbreadcrumb'></div><div id='1'>"
                        + "<div id='2'><div id='3'></div></div></div></td><td width=2%>&nbsp;</td></tr></table></div></body></html>";
                }
                else
                {
                    string sNoScript = "<noscript><h1>Sorry, this site requires JavaScript to function.</h1></noscript>";
                    string sSideBar = "style='xz-index:1; top:145px;'";
                    string sRight = "style='height:75%; width:80%; top:145px; position:fixed; padding-bottom:100px;"
                        + "margin-left:220px;display: inline-block; overflow-y:scroll;overflow-x:hidden;'";
                    sOut += "<div " + sSideBar + ">" + sMenu + "</div>" 
                         + "<div id='1' " + sRight + "></div>"  + sNoScript
                        + "</body></html>";

                }

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