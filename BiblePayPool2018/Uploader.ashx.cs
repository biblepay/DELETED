using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Web.SessionState;

namespace BiblePayPool2018
{
    /// <summary>
    /// Summary description for Uploader
    /// </summary>
    public class Uploader : IHttpHandler, IRequiresSessionState
    {


        public static void Log1(string sData)
        {
            try
            {
                string sPath = "uploadlog.dat";
                System.IO.StreamWriter sw = new System.IO.StreamWriter(sPath, true);
                string Timestamp = DateTime.Now.ToString();
                sw.WriteLine(Timestamp + ", " + sData);
                sw.Close();
            }
            catch (Exception ex)
            {
                string sMsg = ex.Message;
            }
        }
        
        public void ProcessRequest(HttpContext context)
        {
            
            if (context.Session["Sys"] == null)
            {

                throw new Exception("Sys is null.");
            }
            SystemObject sys = (SystemObject)context.Session["Sys"];
            // Ensure we have a parent for this posted file.
            string sParentID = context.Request.QueryString["parentId"].ToString();
            // If an existing ID already exists, Edit the document; if not Add the document
            string sID = context.Request.QueryString["id"].ToString();
            string sDocGuid = "";
            string sParentType = context.Request.QueryString["parenttype"].ToString().ToLower();
            string sTargetTable = (sParentType == "picture" || sParentType=="letter" || sParentType=="news") ? "picture" : "documents";
            Log1(" Saving doctype " + sParentType);

            if (sParentType == "letter") sID = "";
            if (sParentType == "news") sID = "";
            bool bIsNew = false;
            if (sID.Length > 28)
            {
                sDocGuid = sID;
            }
            else
            {
                sDocGuid = Guid.NewGuid().ToString();
                bIsNew = true;

                try
                {
                    ((BiblePayPool2018.USGDGui)context.Session["CallingClass"]).ViewGuid = sDocGuid;
                }
                catch(Exception ex)
                {

                }
            }
            if (sParentID.Length > 0)
            {
                for (int i = 0; i < context.Request.Files.Count; i++)
                {
                    if (i > 0)
                    {
                        sDocGuid = Guid.NewGuid().ToString();
                        bIsNew = true;
                    }
                    HttpPostedFile postedFile = context.Request.Files[i];
                    string fn = System.IO.Path.GetFileName(postedFile.FileName);
                    fn = USGDFramework.Shared.PurifySQL(fn, 255);

                    string SaveLocation = context.Server.MapPath("Data") + "\\" + fn;
                    string sDocName = postedFile.FileName;
                    string sSan = USGDFramework.clsStaticHelper.GetConfig("SAN");
                    if (bIsNew)
                    {
                        string sql = "Insert into "+ sTargetTable + " (id,deleted,added,addedby,organization,parentid) " 
                        + " values ('" + sDocGuid + "',0,getdate(),'" +
                        USGDFramework.Shared.GuidOnly(sys.UserGuid)
                        + "','" + USGDFramework.Shared.GuidOnly(sys.Organization.ToString())
                         + "','" + sParentID + "')";
                        sys._data.Exec2(sql);
                    }
                    Log1(" Added sql record " + sDocGuid);

                    try
                    {

                        postedFile.SaveAs(SaveLocation);
                        Log1(" saved file " + SaveLocation);

                        //Copy the file to the san, with the proper naming convention.
                        FileInfo fi = new FileInfo(SaveLocation);
                        string sTargetFileName = sDocGuid.Substring(0,8) + "" + fi.Extension;
                        string sTargetPath = sSan + sTargetFileName;
                        string sExtension = fi.Extension;
                        string sFullFileName = fi.Name;
                        string sFileNamePrefix = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);
                        System.IO.File.Copy(SaveLocation, sTargetPath, true);
                        string sURL = "";
                        if (sTargetTable == "picture")
                        {
                            //Copy SAN image to public web site
                            string sPublicSite = sSan + "\\Images\\";
                            string sTargetImagePath = sPublicSite + sTargetFileName;
                            string sExt2 = sExtension.ToLower();
                            bool bBlocked = (sExt2 == ".png" || sExt2 == ".jpg" || sExt2 == ".jpeg") ? false : true;
                            if (bBlocked)
                            {
                                throw new Exception("Extension not allowed.");
                            }
                            Log1(" Copying " + sTargetPath + " to " + sTargetImagePath + " sz " + fi.Length.ToString());



                            System.IO.File.Copy(sTargetPath, sTargetImagePath, true);
                            sURL = USGDFramework.clsStaticHelper.GetConfig("WebSite") + "SAN/" + sTargetFileName;
                        }
                        string sql = "Update "+ sTargetTable + " set Name='" + sFileNamePrefix + "',Extension = '" + sExtension 
                            + "',URL='" + sURL + "',SAN='" + sSan + "',FullFileName='" + sFullFileName + "',ParentType='" 
                            + USGDFramework.Shared.PurifySQL(sParentType,50)
                            + "',Updated=getdate(),Size='" + fi.Length.ToString() + "',UpdatedBy='" 
                            + USGDFramework.Shared.GuidOnly(sys.UserGuid)
                            + "' WHERE id  = '" + USGDFramework.Shared.GuidOnly(sDocGuid) + "'";
                        sys._data.Exec2(sql);
                        if (bIsNew) sys.LastWebObject.guid = sDocGuid;

                        context.Response.Write("The file has been uploaded.  ");
                    }
                    catch (Exception ex)
                    {
                        Log1("UPLOAD ERROR: " + ex.Message);

                        context.Response.Write("Error: " + ex.Message);
                    }
                }
                context.Response.StatusCode = 200;
            }
            else
            {
                USGDFramework.Shared.Log("UPLOAD ERROR: Empty");

                context.Response.Write("Unable to attach upload to parent object; Object Empty;");
            }
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}