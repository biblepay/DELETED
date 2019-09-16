using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BiblePayPool2018
{
    public class Dialog
    {
        private SystemObject Sys = null;
        public Dialog(SystemObject sys)
        {
            Sys = sys;
        }
        private string zindex = "style='z-index:-1;'";
        public WebReply CreateDialog(string sID, string Title, string Body, int Width, int Height)
        {
            string sSuffix = Guid.NewGuid().ToString().Substring(0,4);


            string sHTML = "<div id='" + sID + sSuffix + "' title='" + Title + "' " + zindex + ">"
                + "<p><span class='xui-icon xui-icon-circle-check' style='float:left; margin:0 7px 50px 0;'></span>"
                + Body + " </p></div>";       
            string sWH = String.Empty;
            if (Width > 0 || Height > 0)
            {
                sWH = "width: " + Width.ToString() + ", height: " + Height.ToString() + ",";
            }
            string sOpt = "var opt = {        autoOpen: false,   position: { my: 'top', at: 'top+150' },     modal: true, " + sWH + "title: '" + Title + "' };";
            string sJavascript = sOpt + " var theDialog = $('#" + sID + sSuffix + "').dialog(opt); theDialog.dialog('open');";
            WebReply wr = new WebReply(sHTML, sJavascript,sID, true);
            return wr;
        }

        public WebReply CreateDialogWithMoreJavascript(string sID, string Title, string Body, int Width, int Height, string sMoreJavascript)
        {

            string sHTML = "<div id='" + sID + "' title='" + Title + "' " + zindex + ">"
                + "<p><span class='ui-icon ui-icon-circle-check' style='float:left; margin:0 7px 50px 0;'></span>"
                + Body + " </p></div>";
            string sWH = String.Empty;
            if (Width > 0 || Height > 0)
            {
                sWH = "width: " + Width.ToString() + ", height: " + Height.ToString() + ",";
            }
            string sOpt = "var opt = {        autoOpen: false,   position: { my: 'top', at: 'top+150' },     modal: true, " + sWH + "title: '" + Title + "' };";
            string sJavascript = sOpt + " var theDialog = $('#" + sID + "').dialog(opt); theDialog.dialog('open'); "+ sMoreJavascript;

            WebReply wr = new WebReply(sHTML, sJavascript, sID, true);
            return wr;
        }


        public WebReply CreateDialogWebList(string sID, string Title, string Body, int Width, int Height)
        {
            string suffix = Guid.NewGuid().ToString().Substring(0,4);


            string sHTML = "<div id='" + sID +suffix + "' title='" + Title + "' " + zindex + ">"
                + "<p><span class='ui-icon ui-icon-circle-check' style='float:none !IMPORTANT; margin:0 7px 50px 0;'></span>"
                + Body + " </p></div>";
            string sWH = String.Empty;
            if (Width > 0 || Height > 0)
            {
                sWH = "width: " + Width.ToString() + ", height: " + Height.ToString() + ",";
            }
            //$("<div></div>").css({height:"350px", overflow:"auto"});
            string sOpt = "var opt = {        autoOpen: false,   position: { my: 'top', at: 'top+150' },     modal: true, " + sWH + "title: '" + sID + "' };";
            string sJavascript = sOpt + " var theDialog = $('#" + sID + suffix + "').dialog(opt).css({overflow:\"auto\"}); theDialog.dialog('open');";
            WebReply wr = new WebReply(sHTML, sJavascript, sID, true);
            return wr;
        }
        public WebReply CreateYesNoDialog(string sSectionName, string sDialogID, string sYesID, string sNoID, string Title, string Body, object Caller)
        {

            Edit btnYes = new Edit(sSectionName,Edit.GEType.Button, sYesID, "Yes", Sys);
            btnYes.IsInDialog = true;
            btnYes.DialogName = sDialogID;
            string sBtnYes = btnYes.Render(Caller).Packages[0].HTML;
            Edit btnNo = new Edit(sSectionName,Edit.GEType.Button, sNoID, "No", Sys);
            btnNo.IsInDialog = true;
            btnNo.DialogName = sDialogID;
            string sBtnNo = btnNo.Render(Caller).Packages[0].HTML;
            string sButtons = "<table><tr><td>" + sBtnYes + "</td><td>" + sBtnNo + "</td></tr></table>";
            string sHidden = "<input type='hidden' id='hdialog' name='hdialog' value='hdialog'>";
            string sHTML = "<div id='" + sDialogID + "' title='" + Title + "' " + zindex + ">"
                + "<p><span class='ui-icon ui-icon-circle-check' style='float:left; margin:0 7px 50px 0;'></span>"
                 + Body + " </p><p>" + sButtons + "</p>" + sHidden + "<p></div>";
            string sOpt = "var opt = {        autoOpen: false,        modal: true,        width: 550,        height:350,        title: '" + Title + "' };";
            string sJavascript = sOpt + " var theDialog = $('#" + sDialogID + "').dialog(opt); theDialog.dialog('open');";
            WebReply wr = new WebReply(sHTML, sJavascript,sSectionName, true);
            return wr;
        }

    }

    public class WebRequest
    {
        public string eventName = string.Empty;
        public string action = string.Empty;
    }


    public struct WebReplyPackage
    {
        public string HTML;
        public string Javascript;
        public string doappend;
        public string breadcrumb;
        public string breadcrumbdiv;
        public string ApplicationMessage;
        public string SectionName;
        public bool ClearScreen;
        public bool SingleUITable;
    }

    public class WebReply
    {
        public List<WebReplyPackage> Packages = new List<WebReplyPackage>();
        public WebReply()
        {

        }
        public void AddWebReply(string sHTML, string sJavascript, string SectionName, bool DoAppend)
        {
            WebReplyPackage wrp = new WebReplyPackage();
            wrp.HTML = sHTML;
            wrp.Javascript = sJavascript;
            wrp.doappend = DoAppend ? "true" : "false";
            wrp.SectionName = SectionName;
            Packages.Add(wrp);
        }
        public WebReply(string sHTML, string sJavascript, string SectionName, bool DoAppend)
        {
            WebReplyPackage wrp = new WebReplyPackage();
            wrp.HTML = sHTML;
            wrp.Javascript = sJavascript;
            wrp.doappend = DoAppend ? "true" : "false";
            wrp.SectionName = SectionName;
            Packages.Add(wrp);
        }

        public void AddWebPackage(WebReplyPackage p)
        {
            Packages.Add(p);
        }
        public void AddWebPackages(List<WebReplyPackage> lP)
        {
            foreach (WebReplyPackage p in lP)
            {
                AddWebPackage(p);
            }
        }
    }
}