using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace BiblePayPool2018
{
    public partial class UploadFile : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            

        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            if ((File1.PostedFile != null) && (File1.PostedFile.ContentLength > 0))
            {
                string fn = System.IO.Path.GetFileName(File1.PostedFile.FileName);
                string SaveLocation = Server.MapPath("Data") + "\\" + fn;
                try
                {
                    File1.PostedFile.SaveAs(SaveLocation);
                    Response.Write("The file has been uploaded.");
                }
                catch (Exception ex)
                {
                    Response.Write("Error: " + ex.Message);
                   
                }
            }
            else
            {
                Response.Write("Please select a file to upload.");
            }

        }

       
    }
}