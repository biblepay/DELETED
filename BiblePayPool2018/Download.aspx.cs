using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;

namespace BiblePayPool2018
{
    public partial class Download : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            SystemObject sys = (SystemObject)Context.Session["Sys"];
            string sTemp = sys.AppSetting("TEMP", "c:\\code\\temp\\");
            string sPath = sTemp + Request.QueryString["file"];
            FileInfo tgtFile =  new FileInfo(sPath);
            Response.Clear();
            Response.AddHeader("Content-Disposition", "attachment; filename=" + tgtFile.Name);
            Response.AddHeader("Content-Length", tgtFile.Length.ToString());
            Response.ContentType = "application/octet-stream";
            Response.WriteFile(tgtFile.FullName);
            Response.End();
        }
    }
}