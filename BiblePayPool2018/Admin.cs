using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.DataVisualization.Charting;

namespace BiblePayPool2018
{
    public class Admin : USGDGui
    {
        public Admin(SystemObject S)
            : base(S)
        { }

    
        public WebReply MainMenu()
        {
            Section s1 = new Section("Admin", 1, Sys, this);
            GodEdit geLink1 = new GodEdit("Admin", GodEdit.GEType.Anchor, "LinkSettings", "Settings", Sys);
            s1.AddControl(geLink1);
            return s1.Render(this, false);
        }

        public WebReply Context_Selected()
        {
            string sSelectedContext = Sys.GetObjectValue("SettingsEdit", "Context");
            return LinkSettings_Click();
        }

        public WebReply Name_Selected()
        {
            string sSelectedName = Sys.GetObjectValue("SettingsEdit", "Name");
            return LinkSettings_Click();
        }
        public WebReply LinkSettings_Click()
        {
            Section s1 = new Section("SettingsEdit", 2, Sys, this);
            GodEdit ddContext = new GodEdit("SettingsEdit", "Context", Sys);
            ddContext.Type = GodEdit.GEType.Lookup;
            ddContext.CaptionText = "Context:";
            ddContext.LookupValues = Sys.BindColumn("Setting", "Context", "");
            s1.AddControl(ddContext);
            GodEdit ddName = new GodEdit("SettingsEdit", "Name", Sys);
            ddName.Type = GodEdit.GEType.Lookup;
            ddName.CaptionText = "Name:";
            string sSelectedContext = Sys.GetObjectValue("SettingsEdit", "Context");
            string sWhere = "Context='" + sSelectedContext + "'";
            ddName.LookupValues = Sys.BindColumn("Setting", "Name",sWhere);
            s1.AddControl(ddName);
            string sSelectedName = Sys.GetObjectValue("SettingsEdit", "Name");
            string sSql = "Select Value from Setting where Context='" + sSelectedContext + "' and Name='" + sSelectedName + "'";
            System.Data.DataTable dt1 = Sys._data.GetDataTable(sSql);
            string sValue = "";

            if (dt1.Rows.Count > 0) 
            {
                 sValue = dt1.Rows[0][0].ToString();
            }

            GodEdit txtValue = new GodEdit("SettingsEdit", "Value", Sys);
            txtValue.CaptionText = "Value:";
            txtValue.TextBoxValue = sValue;
            s1.AddControl(txtValue);
            return s1.Render(this, true);
        }


        public WebReply FormLoad()
        {
            return Sys.Redirect("Admin",this);
        }
        
    }
    
}