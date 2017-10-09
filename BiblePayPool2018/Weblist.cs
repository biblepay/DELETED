using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace BiblePayPool2018
{
    public class Weblist
    {
        private SystemObject sys = null;
        public bool bShowRowSelect { get; set; }
        public bool bShowRowTrash { get; set; }
        public bool bShowRowHighlightedByUserName { get; set; }
        public bool bSupportCloaking { get; set; }
        public string URLDefaultValue { get; set; }
        public Weblist(SystemObject s)
        {
            sys = s;
        }

        public List<ContextMenuItem> listContextMenuItems = new List<ContextMenuItem>();
            
        public struct ContextMenuItem
        {
            public string Name;
            public string Caption;
            public string Icon;
        }

        public string CleanName(string sName)
        {
            string sOut = sName.ToLower();
            sOut = sOut.Replace(" ", "");
            return sOut;
        }

        public ContextMenuItem AddContextMenuitem(string ItemName, string ItemCaption, string ItemIcon)
        {
            ContextMenuItem cmi = new ContextMenuItem();
            cmi.Name = ItemName;
            cmi.Caption = ItemCaption;
            cmi.Icon = ItemIcon;
            listContextMenuItems.Add(cmi);
            return cmi;
        }

        public WebReply GetWebList(string sql, string sTitle, string sSectionName, string CommentsRow, string SourceTable, object caller, bool bRemoveDiv)
        {

            string myClass = caller.GetType().ToString();
            StackTrace stackTrace = new StackTrace();           
            StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)
            StackFrame callingFrame = stackFrames[1];
            MethodInfo method = (MethodInfo)callingFrame.GetMethod();
            string sMyMethod = method.Name;

            // CONTEXT MENU 
            AddContextMenuitem(".", "", ".");

            string htmlCMI = "";
            foreach (ContextMenuItem cm1 in listContextMenuItems)
            {
                string sRow = "";
                sRow = "   \"" + cm1.Name + "\": {name: \"" + cm1.Caption + "\", icon: \"" + cm1.Caption + "\"},";
                htmlCMI += sRow;
            }

            string sContextMenuCssClass = "context-menu-" + CleanName(sSectionName);
      
            if (htmlCMI.Length > 0) htmlCMI = htmlCMI.Substring(0, htmlCMI.Length - 1);
            string sContextEvent = " onclick=\"var sUSGDID = $(this)[0].getAttribute('usgdid');postdiv(this,'contextmenu','" + myClass + "','" + sMyMethod + "_ContextMenu_'+key,USGDID);\"";
             sContextEvent = " var sUSGDID = $(this)[0].getAttribute('usgdid');postdiv(this,'contextmenu','" + myClass + "','" + sMyMethod + "_ContextMenu_'+key,sUSGDID);";


            string sContextMenu = "  $(function() {   $.contextMenu({     selector: '." + sContextMenuCssClass + "',        callback: function(key, options) { " +
                "       " + sContextEvent + "            },"
               +"       items: {  " + htmlCMI + "                     }                    });"
               +"       $('." + sContextMenuCssClass + "').on('click', function(e){      console.log('clicked', this);        })        });";
            
            bool Expanded = !(sys.GetObjectValue(sSectionName, "ExpandableSection" + myClass + sMyMethod) == "EXPANDED" ? true : false);
            string sExpandedClass = Expanded ? "icon-minus" : "icon-plus";
            sExpandedClass = Expanded ? "icon-chevron-up" : "icon-chevron-down";

            string sAddNewButton = "<span onclick=postdiv(this,'addnew','" + myClass + "','" + sMyMethod + "_AddNew',''); style='float:right;' class='"
                + "icon-plus" + "'>&nbsp;&nbsp;&nbsp;&nbsp;</span>";
            
            string sExpandButton = "<span onclick=postdiv(this,'expand','" + myClass + "','" + sMyMethod + "',''); style='float:right;' class='"
                + sExpandedClass + "'>&nbsp;&nbsp;&nbsp;&nbsp;</span>";

            string sButtons = sExpandButton + sAddNewButton; //These buttons end up going in reverse order.
            string html = "";

            if (!bRemoveDiv) html += "<div id='" + sSectionName + "' name='" + sSectionName + "'>";
        
             html += "<table frame=box cellspacing=4 cellpadding=4 width=100% class=TFtable style='xmin-eight:1vh'>"
                + "<tr><th colspan=10 cellpadding=0 cellspacing=0 class='ui-dialog-titlebar ui-corner-all ui-widget-header'>"
                + "<span class='ui-dialog-title'>" + sTitle + "</span>" + sButtons + "</th></tr>";
            // Custom Context Sensitive menu and event, and Dispatch Function

            if (Expanded)
            {
                USGDTable dt = null;
                try
                {
                    dt = sys.GetUSGDTable(sql, SourceTable);
                }
                catch (Exception ex)
                {
                    string sErr = html + "<tr><td>" + ex.Message + "</td></tr></table></div>";

                    WebReply wr5 = new WebReply();
                    wr5.AddWebReply(sErr, "", "Error Dialog",false);
                    return wr5;
                }
                // Column Names
                string sHeader = "<TR>";
                for (int c = 0; c < dt.Cols; c++)
                {
                    string sCN = dt.ColumnNames[c];
                    string sKey = SourceTable.ToUpper() + "," + sCN.ToUpper();

                    string sCaption = sys.GetFieldCaption(SourceTable, sCN);

                    bool bCommentsColumn = false;
                    string sColspan = string.Empty;

                    if (sCaption.ToUpper() == CommentsRow.ToUpper())
                    {
                        bCommentsColumn = true;
                    }
                    if (bCommentsColumn)
                    {
                        sHeader += "</TR><TR>";
                        sColspan = "colspan='" + dt.Cols.ToString() + "'";

                    }
                    // Mask column if its a primary key guid
                    bool bMasked = false;
                    if (sCaption.ToUpper() == "ID" || sCaption.ToUpper() == "CLOAK" || sCaption.ToUpper() == "BATCHID")
                    {
                        bMasked = true;
                    }
                    if (!bMasked)
                    {
                        // Icon OrderBy
                        string sOrderByClass = sys.GetObjectValue(sSectionName,"OrderByClass" + sCN) == "down" ? "icon-chevron-up" : "icon-chevron-down";
                        string sButtons2 = "<span onclick=postdiv(this,'OrderByClick','" + myClass + "','" + sMyMethod + "_OrderByClick','" + sCN + "'); style='float:right;' class='"
                                + sOrderByClass + "'>&nbsp;&nbsp;&nbsp;&nbsp;</span>";
                        sHeader += "<th align=left " + sColspan + "class='ui-dialog-titlebar' style='border-bottom: grey thin solid'>" + sCaption + sButtons2 + "</th>";
                    }
                }
                sHeader += "</TR>";
                html += sHeader;
                // RENDER VALUES
                for (int y = 0; y < dt.Rows; y++)
                {

                    string sID = dt.GuidValue(y, "Id").ToString();
                    string sOnRowClick = "postdiv(this,'rowclick','" + myClass + "','" + sMyMethod + "_RowClick','" + sID + "');";
                    // This is where we render each weblist ROW of data
                    bool bRowHighlighted = false;
                    bool bRowCloaked = false;
                    //Prescan row to see if highlighted
                    for (int xCol = 0; xCol < dt.Cols; xCol++)
                    {
                        string sCaption = dt.ColumnNames[xCol];
                        string sValue = dt.Value(y, xCol).ToString();
                        if (sCaption.ToUpper() == "USERNAME" && sValue == sys.Username)
                        {
                            bRowHighlighted = true;
                        }
                    }
                    if (bSupportCloaking)
                    {
                        string sValue = dt.Value(y, "Cloak").ToString();
                        if (sValue == "1") bRowCloaked = true;

                    }
                    string sSpecialCSS = bRowHighlighted && bShowRowHighlightedByUserName ? "Activated" : "";
                    // Add the context sensitive right click menu here:
                    string sRow = "<TR usgdid='" + sID + "'  class='" + sContextMenuCssClass + " " + sSpecialCSS
                             + "'   onclick=\"$(this).addClass('Activated').siblings().removeClass('Activated');" + sOnRowClick + "\">";

                    for (int x = 0; x < dt.Cols; x++)
                    {
                        string sValue = dt.Value(y, x).ToString();
                        string sCaption = dt.ColumnNames[x];
                        string sColspan = string.Empty;
                        bool bCommentsColumn = false;
                        if (sCaption.ToUpper() == "USERNAME" || sCaption.ToUpper()=="MINERNAME")
                        {
                            if (bRowCloaked) sValue = "Anonymous";
                        }
                        if (sCaption.ToUpper()=="STATS")
                        {
                            if (bRowCloaked) sValue = "Anonymous";
                        }
                        if (sCaption.ToUpper() == CommentsRow.ToUpper())
                        {
                            bCommentsColumn = true;
                            sColspan = "colspan='" + dt.Cols.ToString() + "'";
                        }
                        if (bCommentsColumn)
                        {
                            sRow += "</TR><TR class='" + sContextMenuCssClass + "'>";
                        }
                        // Mask column if its a guid
                        bool bMasked = false;
                        if (sCaption.ToUpper() == "ID" || sCaption.ToUpper() == "TICKETID" || sCaption.ToUpper() == "TICKET GUID" || sCaption.ToUpper() == "BATCHID" || sCaption.ToUpper()=="CLOAK")
                        {
                            bMasked = true;
                        }
                        if (!bMasked)
                        {
                            if (sCaption.ToUpper()=="URL")
                            {
                                string sGuid = dt.Value((int)y,"id").ToString();
                                    string s1 = "<div id='div" + sGuid
                                    + "' name='div" + sGuid + "'><a id='img" 
                                    + sCaption
                                    + "' name='img" + sGuid
                                    + "' data-featherlight='" + sValue + "'>" + URLDefaultValue + "</a></div>";
                                string j1 = "$('#img" + sGuid + "').featherlightGallery();";
                                sContextMenu += j1;
                                sValue = s1;
                            }

                            if (sCaption.ToUpper() == "NEEDWRITTEN")
                            {
                                string sVal = sValue.ToString() == "1" ? "TRUE" : "FALSE";
                                sValue = sVal;
                            }
                                sRow += "<TD class='ui-dialog-title' style='float:none' " + sColspan + ">" + sValue;
                            // Add buttons to view the row
                            if (x == dt.Cols - 1)
                            {
                                //Button for Viewing the row
                                string sButtons2 = "<span align=right onclick=postdiv(this,'handview','" + myClass + "','" + sMyMethod + "','" + sID + "'); style='float:right;' class='"
                                     + "icon-hand-up" + "'>&nbsp;&nbsp;&nbsp;&nbsp;</span>";
                                if (bShowRowSelect)
                                {
                                    sRow += sButtons2;
                                }
                                // Button for Deleting the Row (Trash Icon)
                                string sButtons3 = "<span align=right onclick=postdiv(this,'handview','" + myClass + "','" + sMyMethod 
                                    + "_Delete_Click','" + sID + "'); style='float:right;' class='"
                                    + "icon-trash" + "'>&nbsp;&nbsp;&nbsp;&nbsp;</span>";
                                if (bShowRowTrash)
                                {
                                    sRow += sButtons3;
                                }

                            }
                            sRow += "</TD>";
                        }
                    }

                    sRow += "</TR>";
                    html += sRow;
                }
            }

            html += "</TABLE><p>";
            if (!bRemoveDiv) html += "</div>";
            string javascript = sContextMenu;

            WebReply wr = new WebReply(html, javascript, sSectionName, false);
            return wr;

        }

    }
}