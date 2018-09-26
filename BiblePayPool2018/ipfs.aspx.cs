using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Diagnostics;

namespace BiblePayPool2018
{
    public partial class ipfs : System.Web.UI.Page
    {

        private string GetDocumentContents(System.Web.HttpRequestBase Request)
        {
            string documentContents;
            using (Stream receiveStream = Request.InputStream)
            {
                using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
                {
                    documentContents = readStream.ReadToEnd();
                }
            }
            return documentContents;
        }
        public static string RequestBody()
        {
            var bodyStream = new StreamReader(HttpContext.Current.Request.InputStream);
            bodyStream.BaseStream.Seek(0, SeekOrigin.Begin);
            var bodyText = bodyStream.ReadToEnd();
            return bodyText;
        }
        protected void Page_Load(object sender, EventArgs e)
        {

            try
            {
                string sUserB = (Request.Headers["Action"] ?? "").ToString();
                string sUserC = (Request.Headers["Solution"] ?? "").ToString();
                string sSourceFileName = (Request.Headers["Filename"] ?? "").ToString();


                double len1 = Request.ContentLength;
                string body = RequestBody();
                if (sSourceFileName == "")
                {
                    Response.Write("<ERROR>Filename not provided.</ERROR><EOF><END>");
                    return;
                }

                byte[] b = Convert.FromBase64String(body);
                if (b.Length > 48000000)
                {
                    Response.Write("<ERROR>File too large.</ERROR><END>");
                    return;
                }
                if (b.Length > 1)
                {
                    string guid = Guid.NewGuid().ToString();
                    string sOutPath = "c:\\ipfs\\store\\" + guid;
                    File.WriteAllBytes(sOutPath, b);
                    string sData = "";
                    var proc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "c:\\ipfs\\ipfs.exe",
                            Arguments = "add " + sOutPath,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };
                    proc.StartInfo.WorkingDirectory = "c:\\ipfs";
                    proc.Start();
                    while (!proc.StandardOutput.EndOfStream)
                    {
                        sData = proc.StandardOutput.ReadLine();
                    }

                    string[] vData = sData.Split(new string[] { " " }, StringSplitOptions.None);
                    if (vData.Length < 2)
                    {
                        Response.Write("<ERROR>Unable to add file to IPFS.</ERROR><END>");
                        return;
                    }
                    string sHash = vData[1];
                    string sLink = "https://ipfs.io/ipfs/" + sHash;
                    string sReply = "<DATA>" + sData + "</DATA><HASH>" + sHash + "</HASH><FILENAME>" + sSourceFileName + "</FILENAME><LINK>" + sLink + "</LINK><FILELEN>"
                        + b.Length.ToString() + "</FILELEN><END>";
                    Response.Write(sReply);
                    return;
                }
                else
                {
                    Response.Write("<ERROR>Invalid Network.</ERROR><END>");
                    return;
                }

                Response.Write("<ERROR>Unknown TCP Error</ERROR><END>");
            }
            catch(Exception ex)
            {
                Response.Write("<ERROR>Unexpected " + ex.Message + "</ERROR><END>");

            }
        }
    }
}