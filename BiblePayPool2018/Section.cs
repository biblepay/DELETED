using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Xml;


namespace BiblePayPool2018
{
   
    public class Section
    {
        public string Name { get; set; }
        private Dictionary<string, object> _controls = null;
        public int ColumnCount { get; set; }
        public bool Expanded { get; set;  }
        public bool MaskSectionMode { get; set; }
        public string DisplayPictureHTML {get;set;}
        private SystemObject Sys = null;
        public SystemObject.SectionMode SecMode { get; set; }
        public Section(string name,int iColCt,SystemObject sys,object caller)
        {
            Name = name;
            ColumnCount = iColCt;
            _controls = new Dictionary<string, object>();
            Sys = sys;
            string myClass = caller.GetType().ToString();

            StackTrace stackTrace = new StackTrace();           
            StackFrame[] stackFrames = stackTrace.GetFrames();  
            StackFrame callingFrame = stackFrames[1];
            MethodInfo method = (MethodInfo)callingFrame.GetMethod();
            if (method.Name=="RenderSection")
            {
               callingFrame = stackFrames[2];
               method = (MethodInfo)callingFrame.GetMethod();
           
            }
            string sMyMethod = method.Name;
            sys.ActiveSection = name;
            Expanded = Sys.GetObjectValue(Name,"ExpandableSection" + myClass + sMyMethod) == "UNEXPANDED" ? false : true;
         
        }

        public Edit AddButton(string sButtonName, string sButtonCaption)
        {
            Edit b = new Edit(Name, Edit.GEType.Button, sButtonName, sButtonCaption, Sys);
            this.AddControl(b);
            return b;
        }

        public Edit AddTextbox(string sTextBoxName, string sCaption)
        {
            Edit geText = new Edit(Name, sTextBoxName, Sys);
            geText.CaptionText = sCaption;
            this.AddControl(geText);
            return geText;
        }

        public void AddBlank()
        {
            Edit b1 = new Edit(this.Name,Edit.GEType.HTML,Guid.NewGuid().ToString(), "",Sys);
            this.AddControl(b1);
            
        }

        public void AddControl(Object o)
        {
            Edit ge=(Edit)o;

            if (_controls.ContainsKey(ge.Name))
            {
                return;
            }

            _controls.Add(ge.Name, ge);

        }

        public string GetHeaderButton(string sName, string sCodeClass, string sCodeMethod, string sCSSClass)
        {
            string sButton = "<span>&nbsp;</span><span onclick=postdiv(this,'"+sName+"','" + sCodeClass + "','" + sCodeMethod
               + "',''); style='float:right;' class='"
               + sCSSClass + "'>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</span>&nbsp;";
            return sButton;
           
        }

        public Weblist.ContextMenuItem AddContextMenuitem(string ItemName, string ItemCaption, string ItemIcon)
        {
            Weblist.ContextMenuItem cmi = new Weblist.ContextMenuItem();
            cmi.Name = ItemName;
            cmi.Caption = ItemCaption;
            cmi.Icon = ItemIcon;
            listContextMenuItems.Add(cmi);
            return cmi;

        }
    

        public string CleanName(string sName)
        {
            string sOut = sName.ToLower();
            sOut = sOut.Replace(" ", "");
            return sOut;
        }

        public List<Weblist.ContextMenuItem> listContextMenuItems = new List<Weblist.ContextMenuItem>();
           
        public WebReply Render(object caller, bool ClearScreen)
        {
            string myClass = caller.GetType().ToString();
            StackTrace stackTrace = new StackTrace();           
            StackFrame[] stackFrames = stackTrace.GetFrames(); 
            StackFrame callingFrame = stackFrames[1];
            MethodInfo method = (MethodInfo)callingFrame.GetMethod();
            string sMyMethod = method.Name;
            // Button for Expand-Collapse
            string sExpandedClass = Expanded ? "icon-minus" : "icon-plus";
            sExpandedClass = Expanded ? "icon-chevron-up" : "icon-chevron-down";
            string sExpandButton = GetHeaderButton("expand", myClass, sMyMethod, sExpandedClass);
            string sEditButton = "";
            if (SecMode == SystemObject.SectionMode.View)
            {
                sEditButton = GetHeaderButton("edit", myClass, sMyMethod + "_EditClick", "icon-pencil");
            }

            string sButtons = sExpandButton + sEditButton;
            // Button for Edit
            string divname = this.Name;
            string sNarrative = "";
            string sBorderStyle = "";
            string sFrameStyle = "";
            
            if (SecMode==SystemObject.SectionMode.View)
            {
                sNarrative = "View";
            }
            else if (SecMode==SystemObject.SectionMode.Add)
            {
                sNarrative = "Add";
            }
            else if (SecMode==SystemObject.SectionMode.Edit)
            {
                sNarrative = "Edit";
            }
            else if (SecMode == SystemObject.SectionMode.Customize)
            {

               sNarrative = "Customize";
            }
            string FriendlyName = this.Name;
            if (!this.MaskSectionMode && false) FriendlyName += " <font size=1>[" + sNarrative + "]</font>"; 

            
            string sDivClass = "class='dragContent'";
            if (SecMode==SystemObject.SectionMode.Customize)
            {
                sBorderStyle = "style='border-top: solid 1px; border-bottom:solid 1px; border-left: solid 1px; border-right:solid 1px;'";
            }
            else
            {
                sFrameStyle = "frame=box";
            }
            

            // CONTEXT MENU 
            AddContextMenuitem("Customize", "Customize", "Customize");


            string htmlCMI = "";
            foreach (Weblist.ContextMenuItem cm1 in listContextMenuItems)
            {
                string sRow = "";
                sRow = "   \"" + cm1.Name + "\": {name: \"" + cm1.Caption + "\", icon: \"" + cm1.Caption + "\"},";
                htmlCMI += sRow;
            }

            string sContextMenuCssClass = "context-menu-" + CleanName(divname);



            string sOut = "<div id='" + divname + "' " + sDivClass + " name='" + divname + "'><table " + sBorderStyle + " class='" + sContextMenuCssClass + "' " + sFrameStyle + " cellspacing=5 cellpadding=5 width=100%>" 
                + "<tr><th colspan=10 cellpadding=0 cellspacing=0 style='border-bottom: grey thin solid' class='ui-dialog-titlebar ui-corner-all ui-widget-header'>"
                + "<span class='ui-dialog-title'>" + FriendlyName + "</span>" + sButtons + "</th></tr>";

            string javascript = "";
            
            if (htmlCMI.Length > 0) htmlCMI = htmlCMI.Substring(0, htmlCMI.Length - 1);
            string sContextEvent = " onclick=postdiv(this,'contextmenu','" + myClass + "','" + sMyMethod + "_ContextMenu_'+key,'');";
            string sContextMenu = "  $(function() {   $.contextMenu({     selector: '." + sContextMenuCssClass + "',        callback: function(key, options) { " +
                "       " + sContextEvent + "            },"
               + "       items: {  " + htmlCMI + "                     }                    });"
               + "       $('." + sContextMenuCssClass + "').on('click', function(e){      console.log('clicked', this);        })        });";



            javascript += sContextMenu;

            int iCurCol = 0;
            int iCurCellCount = 0;
            int iCurRowCount = 0;
            if (Expanded)
            {
                foreach (KeyValuePair<string, object> entry in _controls)
                {
                    Edit ge = (Edit)entry.Value;
                    if (iCurCol == 0)
                    {
                        sOut += "<TR>";
                    }

                    WebReply wr = ge.Render(caller);
                    string sDivWrapper = "divWrapper" + Guid.NewGuid().ToString();

                    if (SecMode == SystemObject.SectionMode.Customize)
                    {
                        string sObject = ge.CaptionText + ": " + ge.TextBoxValue;
                        string susgdid = Guid.NewGuid().ToString();
                        string sCoords = "coords" + iCurCol.ToString() + "-" + iCurRowCount.ToString();
                        sOut += "<td " + sBorderStyle + "><span usgdid='z" + susgdid + "' usgdname='" + sCoords + "' usgdcaption='" + sCoords + "'></span><div class='drag1'>"
                            + "<span usgdcaption='" + ge.CaptionText + "' usgdname='" + ge.Name + "' usgdid='" + susgdid 
                            + "' usgdvalue='" + ge.TextBoxValue + "' >" + sObject + "&nbsp;</span></div></td>";
                    }
                    else
                    {
                        sOut += "<td>" + wr.Packages[0].HTML + "</td>";
                    }
                    javascript += wr.Packages[0].Javascript;
                    iCurCol++;
                    iCurCellCount++;
                    if (iCurCol == ColumnCount)
                    {
                        if (iCurRowCount==0) 
                        {
                            if (DisplayPictureHTML != null)
                            {
                                sOut += "<td rowspan=5>" + DisplayPictureHTML + "</TD>";
                            }
                        }

                        iCurRowCount++;
                        sOut += "</TR>";
                        iCurCol = 0;
                    }
                }
            }

            sOut += "</TR>";
            sOut += "</table></div><p>";
            WebReply wr1 = new WebReply();

            string sJavaEventAfterDrop = "  var sOut=''; $(this).closest('table').find('span').each(function (index) "
                          + "        {        var sUSGDID = $(this)[0].getAttribute('usgdid'); "
                          + "                 var sUSGDValue = $(this)[0].getAttribute('usgdvalue'); "
                          + "                 var sUSGDCaption = $(this)[0].getAttribute('usgdcaption'); "
                          + "                 var sUSGDName = $(this)[0].getAttribute('usgdname'); "
                          + "        var s = $(this)[0].name + '[COL]' + $(this)[0].value + '[COL]' + sUSGDID + '[COL]' + sUSGDValue + '[COL]' + sUSGDCaption + '[COL]' + sUSGDName + '[ROW]';"
                          + "      if (sUSGDID != null) {  if (sUSGDID.length > 0)   sOut += s;  }  }); ";

            string sDropEvent = sJavaEventAfterDrop + "postdiv(this,'dropevent','" + myClass + "','" + sMyMethod + "','[DROPPABLE]" 
                + divname + "[/DROPPABLE][DATA]' + sOut + '[/DATA]');";
                

            string DraggableJavascript = "     $('.drag1').draggable({  "
                + "    helper: 'clone',        start: function(event, ui)"
                + "    {            c.tr = this;                c.helper = ui.helper;            }    });"
                + "   $('.drag1').droppable({     drop: function(event, ui) {     "
                + "  "
                + "   $(c.helper).remove();        }     });";
            DraggableJavascript = " var myDraggedObject = null; $(function() { $( '.drag1' ).draggable({containment:parent});  }); ";
            DraggableJavascript = "var myDraggedObject = null; var myOriginalHTML = null; $('.drag1').draggable(  "
                +"{ revert: function(droppableContainer) {         if(droppableContainer) { var thisisvalid='valid';    }       else {        myDraggedObject.innerHTML=myOriginalHTML;     } } ,"
            +"      helper: 'clone', "
            +"      containment: '.dragContent', cursor:'move', snap: true, snapMode: 'inner',"
            + "     start: function(event, ui)   {  myDraggedObject = this; this.setAttribute('usgdname','test'); "
            +"       myOriginalHTML = myDraggedObject.innerHTML; myDraggedObject.setAttribute('usgdname','bye3');  "
            +"       myDraggedObject.innerHTML ='&nbsp;';      }   });  "
                + "                $('.drag1').droppable(  {drop: function(event, ui) { ui.helper[0].innerHTML='bye'; myDraggedObject.innerHTML = myOriginalHTML; ui.draggable.detach().appendTo($(this));   " + sDropEvent + "  }     }); ";

            if (SecMode == SystemObject.SectionMode.Customize)                 javascript += DraggableJavascript;
            wr1.AddWebReply(sOut, javascript, Name, false);
            WebReplyPackage wrp1 = wr1.Packages[0];
            wrp1.ClearScreen = ClearScreen;
            wr1.Packages[0] = wrp1;
            return wr1;
        }


    }


    

    public class Edit
    {

        public enum GEType
        {
            Anchor,
            Text,
            Button,
            Lookup,
            Date,
            TableRow,
            Caption,
            Password,
            TextArea,
            UploadControl,
            UploadControlClassic,
            Image,
            SortableList,
            DIV,
            Radio,
            Label,
            HTML,
            Lightbox,
            TreeView,
            TreeViewDummy,
            CheckBox,
            IFrame,
            DoubleButton
        };
        
        public string Name { get; set; }
        public string CaptionText { get; set; }
        public string CaptionText2 { get; set; }
        public string CaptionWidthCSS { get; set; }
        public XmlDocument Nodes { get; set; }

        public string TextBoxValue { get; set; }
        public string ErrorText { get; set; }
        public GEType Type { get; set; }
        public List<SystemObject.LookupValue> LookupValues { get; set; }
        public List<SystemObject.LookupValue> LookupValuesSelected { get; set; }
        public string Method { get; set;  }
        private SystemObject Sys { get; set; }
        public bool IsInDialog { get; set; }
        public string DialogName { get; set; }
        public int rows { get; set; }
        public int cols { get; set; }
        public int size { get; set; }
        public string ParentGuid { get; set; }
        public string ParentType { get; set; }
        public string Id { get; set; }
        public string URL { get; set; }
        public bool ReadOnly { get; set; }
        public string Section { get; set; }
        public String Name2 { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
        public string GroupName { get; set; }
        public string HTML { get; set; }
        public string Javascript { get; set; }
        public string ColSpan { get; set; }
        public bool MaskColumn1 { get; set; }
        public bool MaskBeginTD { get; set; }
        public bool MaskEndTD { get; set; }
        public string ColSpan2 { get; set; }
        public string TextBoxStyle { get; set; }
        public string TextBoxAttribute { get; set; }
        public string RadioName { get; set; }
        public string sAltGuid { get; set; }
        public bool ExtraTD { get; set; }
        private int iCounter = 0;
        private string sCumulativeHTML = "";
        public string TdWidth { get; set; }
        public List<SystemObject.LookupValue> ConvertStringToLookupList(string SemicolonDelimited)
        {
            string[] vRows = SemicolonDelimited.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            List<SystemObject.LookupValue> listLV = new List<SystemObject.LookupValue>();
            for (int i = 0; i < vRows.Length;i++)
            {
                 // Add the string entry to the List
                 SystemObject.LookupValue lv = new SystemObject.LookupValue();
                 lv.ID = Guid.NewGuid().ToString();
                 lv.Caption = vRows[i];
                 lv.Value = vRows[i];
                 listLV.Add(lv);
            }
            return listLV;
        }

        public Edit(string sSection, GEType ControlType, SystemObject sys)
        {
            Type = ControlType;
            Sys = sys;
            string fn = Sys.Username;
            Section = sSection;
        }
        
        public Edit(string sSection, string ControlName, SystemObject sys)
        {
            Name = ControlName;
            Type = GEType.Text;
            Sys = sys;
            Section = sSection;
            TextBoxValue = Sys.GetObjectValue(sSection,Name);
        }

        public Edit(string sSection, GEType ControlType, string ControlName, string ControlCaptionText, SystemObject sys)
        {
            Type = ControlType;
            Section = sSection;
            Name = ControlName;
            CaptionText = ControlCaptionText;
            if (ControlType==GEType.Button|| ControlType==GEType.UploadControl)
            {
                Method = Name  + "_Click";

            }

             if (ControlType==GEType.UploadControl)
             {
                 Method = Name + "_Click";
             }
            Sys = sys;
            TextBoxValue = Sys.GetObjectValue(Section, Name);
        }

        private string GetXMLValue(XmlNode oNode,string sAttribute)
        {
            if (oNode != null)
            {
                if (oNode[sAttribute].InnerText != null)
                { 
                    object oAttribute = oNode[sAttribute].InnerText;
                    string sCaption = (oAttribute ?? String.Empty).ToString();
                    return sCaption;
                }
                else
                {
                    return "";
                }
            }
            else
            {
                return "";
            }
            
        }

        private string sLastParentObject = "";


        private void appendtreeviewrow(XmlNode oNode)
        {

            string sCaption = oNode.Attributes["Caption"].InnerText;
            string sName = oNode.Name;
            string sParentObject = oNode.Attributes["ParentObject"].InnerText;
            string sCheckboxCaption1 = oNode.Attributes["CheckboxCaption1"].InnerText;
            string sCheckboxCaption2 = oNode.Attributes["CheckboxCaption2"].InnerText;
            string sCheckboxCaption3 = oNode.Attributes["CheckboxCaption3"].InnerText;
            string sCheckboxCaption4 = oNode.Attributes["CheckboxCaption4"].InnerText;
            iCounter++;
            string sRow = "<li>";

            if (sLastParentObject != sParentObject)
            {
                sCumulativeHTML += "<ul>";
            }

            sRow += "";
            sRow += "<input type='checkbox' style='display:none;' id='node-" + iCounter.ToString() + "' title='flower' />";

            string sCheckbox = "<div class='cb1'>";
            if (sCheckboxCaption1 != "")
            {
                sCheckbox += "<input type='checkbox' class=''  id='d-" + iCounter.ToString() + "' checked='' />";
            }

            if (sCheckboxCaption2 != "")
            {
                sCheckbox += "<input type='checkbox' class=''  id='d-" + iCounter.ToString() + "' checked='' />";
            }

            if (sCheckboxCaption3 != "")
            {
                sCheckbox += "<input type='checkbox' class=''  id='d-" + iCounter.ToString() + "' checked='' />";
            }

            if (sCheckboxCaption4 != "")
            {
                sCheckbox += "<input type='checkbox' class=''  id='d-" + iCounter.ToString() + "' checked='' />";
            }

            sCheckbox += "</div>";
            sRow += "<label><input type='checkbox' style='display:none;' title='flower' />" + "<span></span></label>" + "<label for='node-" + iCounter.ToString() + "'>" + sCheckbox + sCaption + "</label>\r\n";
            sCumulativeHTML += sRow;
            //when parent changes, break to a new row
            if (sLastParentObject != sParentObject)
            {
            }
            sLastParentObject = sParentObject;
        }

        public void traversenodes(XmlNode oCurrentNode)
        {
            appendtreeviewrow(oCurrentNode);
            for (int y = 0; y < oCurrentNode.ChildNodes.Count; y++)
            {
                XmlNode oNode = oCurrentNode.ChildNodes[y];
                appendtreeviewrow(oNode);
                // handle subnodes
                if (oNode.HasChildNodes)
                {
                    for (int z = 0; z < oNode.ChildNodes.Count; z++)
                    {
                        traversenodes(oNode.ChildNodes[z]);
                    }
                 }

                sCumulativeHTML += "</ul></ul>";
            }
        }

        public WebReply Render(object caller)
        {
            string sFlyout = (ErrorText ?? String.Empty).ToString().Length > 0 ? "example-right" : String.Empty;
            if (Type==GEType.Text || Type==GEType.Date || Type==GEType.Password)
            {
                string sSize = size > 0 ? "size='" + size.ToString() + "'" : String.Empty;
                string sReadOnly = ReadOnly ? "READONLY" : string.Empty;
                string sClass = ReadOnly ? "reado" : string.Empty;

                string sOut = "<td " + ColSpan + " " + Width +"><span>" + CaptionText + "</span></td><td " + ColSpan2 + "><input class='" + sClass + "' type='" + Type + "' " + sReadOnly + " name='" 
                    + Name + "' " + sSize + " id='" + Name + "' " + TextBoxAttribute + " style='" + TextBoxStyle + "' value='" + TextBoxValue + "' />&nbsp;<label class='" 
                    + sFlyout + "' for='" + Name + "'>" + ErrorText + "</label></td>";
                WebReply wr1 = new WebReply();
                wr1.AddWebReply(sOut, "",Section,false);
                return wr1;
            }
            //<input type="radio" name="gender" value="male" checked> Male <br>
            if (Type == GEType.Radio)
            {
                string sSize = size > 0 ? "size='" + size.ToString() + "'" : String.Empty;
                string sOut = "<td " + ColSpan + " " + Width + "><span>" 
                    +  "</span></td><td " + ColSpan2 + "><input class='" 
                    + "' type='" + Type + "' " + " name='"
                    + RadioName + "' " + sSize + " id='" + Name + "' " 
                    + TextBoxAttribute + " style='" + TextBoxStyle + "' value='" + TextBoxValue + "'>"  + CaptionText + "</input><label class='"
                    + sFlyout + "' for='" + Name + "'>" + ErrorText + "</label></td>";
                WebReply wr1 = new WebReply();
                wr1.AddWebReply(sOut, "", Section, false);
                return wr1;
            }
            else if (Type==GEType.Image)
            {
                string sOut = "<td colspan=2 align=center><img name='"
                  + Name + "' id='" + Name + "' src='" + URL + "' />&nbsp;</td>";
                return new WebReply(sOut, "",Section, false);
     
            }
            else if (Type==GEType.DIV)
            {
                string sOut = "<div name='"
                + Name + "' id='" + Name + "'>";
                WebReply wr1 = new WebReply();
                wr1.AddWebReply(sOut, "", Section, false);
                return wr1;
            }
            else if(Type==GEType.Lightbox)
            {
                string sData = "<div id='div"+ Name + "' name='div"+ Name + "'><a id='img"+ Name + "' name='img" + Name + "' data-featherlight='" + URL + "'>"+ CaptionText + "</a></div>";
                Javascript = "$('#img" + Name + "').featherlightGallery();";
                WebReply wr1 = new WebReply();
                wr1.AddWebReply(sData, Javascript, Section, false);
                return wr1;
            }
            else if (Type==GEType.HTML)
            {
                string sOut = "<div style='width:100%;' name='"
                      + Name + "' id='" + Name + "'>" + HTML + "</div>";
                WebReply wr1 = new WebReply();
                wr1.AddWebReply(sOut,Javascript, Section, false);
                return wr1;
            }
            else if (Type==GEType.Label)
            {
                string sOut = "<td " + CaptionWidthCSS + "><span id='" + Name + "'>" + CaptionText + "</span></td>";
                if (("" + TextBoxValue).Length > 0 )
                {
                    sOut += "<td>" + TextBoxValue + "</td>";
                }
                return new WebReply(sOut, "", Section, false);
            }

            else if (Type == GEType.TreeView)
            {

                string sJS = "      $('.acidjs-css3-treeview').delegate('label input:checkbox [title=flower]', 'change', function()"
                              + "\r\n {    "
                              + " var checkbox = $(this),     nestedList = checkbox.parent().next().next(),"
                              + "\r\n       selectNestedListCheckbox = nestedList.find('label:not([for]) input:checkbox [title=flower]'); "
                              + "  \r\n     if(checkbox.is(':checked')) {"
                              + "\r\n return selectNestedListCheckbox.prop('checked', true);    }"
                              + "  \r\n     selectNestedListCheckbox.prop('checked', false);  \r\n });";

                sJS = "";

                string sHTML = "<div class=\"acidjs-css3-treeview\">";

                // iterate through the nodes
                XmlNode eleOuter = Nodes.DocumentElement;
                XmlNode oNode = eleOuter.SelectNodes("NODES")[0].SelectNodes("Organization")[0];
                if (oNode != null)
                {
                    sHTML += "<ul>";
                }
                traversenodes(oNode);
                sHTML += sCumulativeHTML + "</div>";
                return new WebReply(sHTML, sJS, Section, false);
            }
            else if (Type==GEType.TreeViewDummy)
            {

                string sJS =  "      $('.acidjs-css3-treeview').delegate('label input:checkbox', 'change', function()"
                              +"\r\n {    "
                              +" var checkbox = $(this),     nestedList = checkbox.parent().next().next(),"
                              +"\r\n       selectNestedListCheckbox = nestedList.find('label:not([for]) input:checkbox'); "
                              +"  \r\n     if(checkbox.is(':checked')) {"
                              +"\r\n return selectNestedListCheckbox.prop('checked', true);    }"
                              +"  \r\n     selectNestedListCheckbox.prop('checked', false);  \r\n });";


                string sHTML = "<div class=\"acidjs-css3-treeview\"><ul>";
                int iCounter = 0;
                // iterate through the nodes
                XmlNode eleOuter = Nodes.DocumentElement;
                XmlNode oNodes = eleOuter.SelectNodes("xolddummy")[0];

                for (int y = 0; y < oNodes.ChildNodes.Count; y++)
                {
                    XmlNode oNode = oNodes.ChildNodes[y];
                    string sCaption = oNode["CAPTION"].InnerText;
                    string sName = oNode["NAME"].InnerText;
                    iCounter++;
                    string sRow = "<li><input type='checkbox' id='node-" + iCounter.ToString()
                        + "' checked='' /><label><input type='checkbox' /><span></span></label><label for='node-" + iCounter.ToString() + "'>" + sCaption + "</label>";
                    sHTML += sRow;

                    for (int iSubNodeCounter = 0; iSubNodeCounter < 10; iSubNodeCounter++)
                    {
                        // If Any child nodes, process here
                        XmlNode oSubNodes = oNode.SelectNodes("SUBNODES")[0];
                        sHTML += "<ul>";
                        for (int j = 0; j < oSubNodes.ChildNodes.Count; j++)
                        {
                            XmlNode oNode2 = oSubNodes.ChildNodes[j];
                            string sCaption2 = oNode2["CAPTION"].InnerText;
                            string sName2 = oNode2["NAME"].InnerText;
                            iCounter++;
                            string sRow2 = "<li><input type='checkbox' id='node-" + iCounter.ToString()
                                + "' checked='' /><label><input type='checkbox' /><span></span></label><label for='node-" + iCounter.ToString() + "'>" + sCaption2 + "</label>";
                            sHTML += sRow2;

                        }
                        sHTML += "</li>";
                        sHTML += "</ul>";
                    }
                    sHTML += "</li>";
                }
             
                sHTML += "</ul>";
                sHTML += "</div>";

                return new WebReply(sHTML, sJS, Section, false);

            }
            else if (Type==GEType.SortableList)
            {
                     string Name2 = Name + "2";
                     string ul1 = "<ul id='" + Name + "'>";
                     foreach (SystemObject.LookupValue lv in LookupValues)
                     {
                         //Select the Selected TextBoxValue
                         string key = lv.ID;
                         key = lv.Value;
                         string sRow= "<li class='sortable' usgdname='" + lv.Name + "' usgdcaption='" + lv.Caption + "' usgdid='" + lv.ID + "' usgdvalue='" + lv.Value + "' id='" + key + "' value='" + lv.Value + "'>" + lv.Caption + "</li>";
                         ul1 += sRow;
                     }

                     ul1 += "</ul>";

                     string ul2 = "<ul id='" + Name2 + "'>";
                     foreach (SystemObject.LookupValue lv in LookupValuesSelected)
                     {
                         //Select the Selected TextBoxValue
                         string key = lv.ID;
                         key = lv.Value;
                         string sRow = "<li class='sortable' usgdname='" + lv.Name + "' usgdcaption='" + lv.Caption + "' usgdid='" + lv.ID + "' usgdvalue='" + lv.Value + "' id='" + key + "' value='" + lv.Value + "'>" + lv.Caption + "</li>";
                         ul2 += sRow;
                     }

                     ul2 += "</ul>";

                     string html = "<td><div><table><tr><td>"+ CaptionText + "</td><td>" + CaptionText2 + "</td></tr><tr><td width='" 
                         + Width.ToString() + "'>" + ul1 + "</td><td width='" + Width.ToString() + "'>" 
                         + ul2 + "</td></tr></table></div></td>";


                     string myClass = caller.GetType().ToString();
                     StackTrace stackTrace = new StackTrace();           // get call stack
                     StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)
                     StackFrame callingFrame = stackFrames[2];
                     MethodInfo method = (MethodInfo)callingFrame.GetMethod();
                     string sMyMethod = Name + "_Sort";

                    string sJava2 = "  var sOut=''; $('#"+Name+"').closest('ul').find('li').each(function (index) "
              +"        {         var sUSGDID = $(this)[0].getAttribute('usgdid'); "
              +"                  var sUSGDValue = $(this)[0].getAttribute('usgdvalue'); "
              + "                 var sUSGDCaption = $(this)[0].getAttribute('usgdcaption'); "
              + "                 var sUSGDName = $(this)[0].getAttribute('usgdname'); "
              +"        var s = $(this)[0].name + '[COL]' + $(this)[0].value + '[COL]' + sUSGDID + '[COL]' + sUSGDValue + '[COL]' + sUSGDCaption + '[COL]' + sUSGDName + '[ROW]';"
              +"        sOut += s;    }); ";


                    string sJava3 = "  var sOut2=''; $('#" + Name2 + "').closest('ul').find('li').each(function (index) "
              + "        {         var sUSGDID = $(this)[0].getAttribute('usgdid'); "
              + "                  var sUSGDValue = $(this)[0].getAttribute('usgdvalue'); "
              + "                  var sUSGDCaption = $(this)[0].getAttribute('usgdcaption'); "
              + "                  var sUSGDName = $(this)[0].getAttribute('usgdname'); "
              + "        var s = $(this)[0].name + '[COL]' + $(this)[0].value + '[COL]' + sUSGDID + '[COL]' + sUSGDValue + '[COL]' + sUSGDCaption + '[COL]' + sUSGDName + '[ROW]';"
              + "        sOut2 += s;    }); ";


                     string serialize = " " + sJava2 + sJava3 + " var order = $('#"+ Name + "').sortable('toArray');  var order2=$('#"+ Name2+ "').sortable('toArray'); var data = order.join(';'); var data2=order2.join(';'); ";
                     string sUniqueId = Section + "[ROWSET]" + Name + "[ROWSET]";
                     string sEvent = serialize + "postdiv(this,'sortevent','" + myClass + "','" + sMyMethod + "','[SORTABLE]" + sUniqueId + "' + sOut + '[ROWSET]'+sOut2);";
                     string sIdentifier = "#" + Name + ",#" + Name2;

                     string javascript = "   $('"+ sIdentifier + "').sortable({     "
                          +" stop: function(event, ui){      " + sEvent + "        },"
                          + "connectWith: '" + sIdentifier + "'         });  $('" + sIdentifier+ "').disableSelection();";


                    return new WebReply(html, javascript,Section, false);
            }
            else if (Type == GEType.UploadControl)
            {
                string myClass = caller.GetType().ToString();
                StackTrace stackTrace = new StackTrace();           // get call stack
                StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)
                StackFrame callingFrame = stackFrames[1];
                MethodInfo method = (MethodInfo)callingFrame.GetMethod();
                string sMyMethod = method.Name;
                sMyMethod = Method;

                string sEvent = "postdiv(this,'buttonevent','" + myClass + "','" + sMyMethod + "','');";
                //sEvent = "";

                string html = "<td colspan=2><form enctype='multipart/form-data' method='POST' action='Uploader.ashx'>"
                    + "<p id='DivUploadControl'></p>"
                    + "<input type='hidden' name='MAX_FILE_SIZE' value='3000000' />"
                    + "<label class='fileContainer roundbutton'>"+ CaptionText 
                    + "<input class='inputfile' type='file' " 
                    + "id='myFile' name='myFile' multiple size=50 onchange=\"USGDFileUploader(this.form,'Uploader.ashx?parentid="+ ParentGuid + "&id="+ Id.ToString() 
                      + "&parenttype=" + ParentType + "','divupload','" + myClass + "','" + sMyMethod + "','" + Section + "');"+ "\" >"
                    + "</label>"
                    + "&nbsp;&nbsp;"
                    + "<div id=divupload></div>  </form></td>";
                return new WebReply(html, "", Section, false);

            }
            else if (Type==GEType.TextArea)
            {
                string sReadOnly = ReadOnly ? "READONLY" : string.Empty;
                string sClass = ReadOnly ? "reado" : string.Empty;
                string sColspan = "";
                if ((""+ColSpan).Length > 0)
                {
                    sColspan = "colspan='" + ColSpan + "'";
                }
                string td1 = "";
                if (!MaskColumn1)
                {
                    td1 = "<td><span>" + CaptionText + "</span></td>";
                }
                string sOut = td1 + "<td " + TdWidth + " " + sColspan + "><textarea " + sReadOnly + " class='" + sClass + "' type='" + Type + "' name='"
                  + Name + "' rows='" + rows.ToString() + "' cols='" + cols.ToString() + "' id='" 
                  + Name + "' style='width:" + Width + ";height:" + Height + ";' value='" + TextBoxValue + "'>" + TextBoxValue + "</textarea>&nbsp;<label class='"
                  + sFlyout + "' for='" + Name + "'>" + ErrorText + "</label></td>";
                return new WebReply(sOut, "", Section,false);
            }
            else if (Type==GEType.Caption)
            {
                string sOut = "<td colspan=2><span id='" + Name + "'>" +TextBoxValue + "</span></td>";
                return new WebReply(sOut, "", Section,false);
            }
            else if (Type==GEType.Button)
            {
                string myClass = caller.GetType().ToString();
                StackTrace stackTrace = new StackTrace();           
                StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)
                StackFrame callingFrame = stackFrames[1];
                MethodInfo method = (MethodInfo)callingFrame.GetMethod();
                string sMyMethod = method.Name;
                sMyMethod = Method;
                string sJavascriptSuffix = IsInDialog ? "$(this).closest('.ui-dialog-content').dialog('close'); " : String.Empty;
                string sOut = "";
                if (ExtraTD) sOut += "<td>&nbsp;</td>";
                if (!MaskBeginTD) sOut += "<td colspan='" + ColSpan + "'>";
                    sOut += "<input class=roundbutton type=button name='" + Name + "' id='" + Name + "' value='"
                    + CaptionText + "' onclick=\"" + sJavascriptSuffix + "postdiv(this,'buttonevent','" + myClass + "','" + sMyMethod + "','" + sAltGuid + "');\"  />";
                if (!MaskEndTD)    sOut += "</td>";

                return new WebReply(sOut, "", Section,false);

            }
            else if (Type == GEType.DoubleButton)
            {
                string myClass = caller.GetType().ToString();
                StackTrace stackTrace = new StackTrace();           
                StackFrame[] stackFrames = stackTrace.GetFrames();  
                StackFrame callingFrame = stackFrames[1];
                MethodInfo method = (MethodInfo)callingFrame.GetMethod();
                string sMyMethod = method.Name;
                sMyMethod = Method;
                string sJavascriptSuffix = IsInDialog ? "$(this).closest('.ui-dialog-content').dialog('close'); " : String.Empty;
                string sOut = "";
                if (!MaskBeginTD) sOut += "<td colspan='" + ColSpan + "'>";
                Method = "" + Name + "_Click";

                sOut += "<input class=roundbutton type=button name='" + Name + "' id='" + Name + "' value='"
                + CaptionText + "' onclick=\"" + sJavascriptSuffix + "postdiv(this,'buttonevent','" + myClass + "','" + Method + "','');\"  />";
                // Button2:
                Method = "" + Name2 + "_Click";

                sOut += "&nbsp;&nbsp;&nbsp;&nbsp;<input class=roundbutton type=button name='" + Name2 + "' id='" + Name2 + "' value='"
                + CaptionText2 + "' onclick=\"" + sJavascriptSuffix + "postdiv(this,'buttonevent','" + myClass + "','" + Method + "','');\"  />";
                if (!MaskEndTD) sOut += "</td>";
                return new WebReply(sOut, "", Section, false);
            }

            else if (Type==GEType.IFrame)
            {
                string myClass = caller.GetType().ToString();
                StackTrace stackTrace = new StackTrace();           // get call stack
                StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)
                StackFrame callingFrame = stackFrames[1];
                MethodInfo method = (MethodInfo)callingFrame.GetMethod();
                string sMyMethod = method.Name;
                sMyMethod = Method;
                string sJavascriptSuffix = IsInDialog ? "$(this).closest('.ui-dialog-content').dialog('close'); " : String.Empty;
                string sCSS = "background-color:grey";
                string sOut = "<td><iframe style='" + sCSS + "' src='" + URL + "' name='" + Name + "' id='" + Name + "' width='" + Width + "' height='" + Height + "'> </iframe></td>";
                return new WebReply(sOut, "", Section, false);
            }
            else if (Type == GEType.CheckBox)
            {
                string myClass = caller.GetType().ToString();
                StackTrace stackTrace = new StackTrace();           // get call stack
                StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)
                StackFrame callingFrame = stackFrames[1];
                MethodInfo method = (MethodInfo)callingFrame.GetMethod();
                string sMyMethod = method.Name;
                sMyMethod = Method;
                string sJavascriptSuffix = IsInDialog ? "$(this).closest('.ui-dialog-content').dialog('close'); " : String.Empty;
                string sCHECKED = TextBoxValue == "true" ? "CHECKED" : "";
                string sOut = "<td>" + CaptionText + "</td><td><input class=roundbutton type=checkbox " + sCHECKED + " value='" + TextBoxValue + "' name='" + Name + "' id='" + Name + "' value='"
                    + CaptionText + "' /></td>";
                return new WebReply(sOut, "", Section, false);

            }
            else if (Type==GEType.Anchor)
            {
                string myClass = caller.GetType().ToString();
                StackTrace stackTrace = new StackTrace();           // get call stack
                StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)
                StackFrame callingFrame = stackFrames[1];
                MethodInfo method = (MethodInfo)callingFrame.GetMethod();
                string sMyMethod = method.Name;
                sMyMethod = Method;
                string sJavascriptSuffix = IsInDialog ? "$(this).closest('.ui-dialog-content').dialog('close'); " : String.Empty;
                string sOut = "<td colspan=1><a name='" + Name + "' id='" + Name + "' value='"
                     + "' href=# onclick=\"" + sJavascriptSuffix + "postdiv(this,'buttonevent','" + myClass + "','" + this.Name + "_Click','');return true;\" >" + CaptionText + "</a></td>";
                return new WebReply(sOut, "", Section, false);

            }
            else if (Type==GEType.TableRow)
            {
                string sOut = "<tr>";
                return new WebReply(sOut, "", Section,false);
            }
            else if (Type==GEType.Lookup)
            {
                 string sReadOnly = ReadOnly ? "DISABLED" : string.Empty;
                 string sClass = ReadOnly ? "reado" : string.Empty;
                // Event
                string myClass = caller.GetType().ToString();
                         StackTrace stackTrace = new StackTrace();           // get call stack
                         StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)
                         StackFrame callingFrame = stackFrames[1];
                         MethodInfo method = (MethodInfo)callingFrame.GetMethod();
                         string sMyMethod = method.Name;
                         string sEvent = " onchange=postdiv(this,'lookupclick','" + myClass + "','" + Name + "_Selected',this.options[this.selectedIndex].value);";        


                 string sRows = "<td><span>" + CaptionText + "</span></td><td>" + "<select " + sReadOnly + " class='" + sClass + "' " + sEvent + " name='" + Name + "' id='" + Name + "'>";
                 if (LookupValues != null)
                 {
                     // Ensure we have a blank entry for empty or null values
                     string sBlankSelected = "";
                     if (TextBoxValue.Trim() == "") sBlankSelected = "SELECTED";
                     sRows += "<option " + sBlankSelected + " classid='" + "000" + "' value='" + "" + "'>" + "" + "</option>";
                     foreach (SystemObject.LookupValue lv in LookupValues)
                     {
                         string sSelected = String.Empty;
                         if (lv.Value == TextBoxValue)
                         {
                             sSelected = "SELECTED";
                         }
                         sRows += "<option " + sSelected + " USGDID='" + lv.ID + "' USGDVALUE='" + lv.Value + "' value='" + lv.Value + "'>" + lv.Caption + "</option>";
                     }
                 }
                 sRows += "</select></td>";
                 return new WebReply(sRows, "", Section,false);
            }
            else
            {
                string sErr = "";
            }
            return new WebReply("", "", Section,false);
        }
    }
}