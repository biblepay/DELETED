using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;

namespace BiblePayPool2018
{

    public class Document : USGDGui
    {

        public Document(SystemObject S)  : base(S)
        {
            this.ObjectName = "Document";
            S.CurrentHttpSessionState["CallingClass"] = this;
        }

        public WebReply DocumentSearch()
        {
            Section Search = Sys.RenderSection("Document Search", "Documents", 1, this, SystemObject.SectionMode.Search);
            Edit geBtnB = Search.AddButton("btnSearch", "Search");
            return Search.Render(this, true);
        }

        public WebReply btnSearch_Click()
        {
            WhereClause = Sys.GenerateWhereClauseFromSection("Document Search", "Documents");
            return DocumentList();
        }

        public WebReply DocumentAdd()
        {

            Section Add = Sys.RenderSection("Document Add", "Documents", 1, this, SystemObject.SectionMode.Edit);
            Edit ctlUpload = new Edit("Document Add", Edit.GEType.UploadControl2, "btnSave", "Save", Sys);
            ctlUpload.Id = this.ViewGuid;
            ctlUpload.ParentGuid = this.ParentID;
            Add.AddControl(ctlUpload);
            return Add.Render(this, true);
        }

        public WebReply btnPhase3_Click()
        {
            return Phase3();
        }

        public WebReply DocumentList_AddNew()
        {
            this.SectionMode = SystemObject.SectionMode.Add;
            return DocumentAdd();
        }
        

        public WebReply btnSave_Click()
        {
            Sys.SaveObject("Document Add", "Documents", this);
            return DocumentView();
        }

        public WebReply DocumentView()
        {
            Section sT = Sys.RenderSection("Document View", "Documents", 1, this, SystemObject.SectionMode.View);
            WebReply wTV = sT.Render(this, true);
            return wTV;
        }

        public WebReply DocumentView_EditClick()
        {
            //Enter Edit Mode on the Page.
            return DocumentAdd();
        }

        public WebReply DocumentList()
        {
            SQLDetective s = Sys.GetSectionSQL("Document List", "Documents", string.Empty);
            // If they are coming in from the search page
            if (WhereClause.Length > 0)
            {
                s.WhereClause = WhereClause;
            }
            else
            {
                s.WhereClause = "Documents.Organization='" + Sys.Organization.ToString() + "' and Documents.deleted=0  and Documents.ParentID = '" + this.ParentID.ToString() + "'";
            }
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
            WebReply wr = w.GetWebList(sql, "Document List", "Document List", "", "Documents", this, false);
            return wr;
        }


        public WebReply DocumentList_OrderByClick()
        {
            return DocumentList();
        }

        public WebReply DocumentList_RowClick()
        {
            ViewGuid = Sys.LastWebObject.guid;
            return DocumentView();
        }

        public WebReply Phase3()
        {
            Section s = new Section("Phase3", 3, Sys, this);
            if (Sys.GetObjectValue("Phase3", "ConditionCount") == "") Sys.SetObjectValue("Phase3", "ConditionCount", "1");

            int iConditionCount = Convert.ToInt32(Sys.GetObjectValue("Phase3", "ConditionCount"));

            for (int x = 0; x < iConditionCount; x++)
            {
                Edit ddFields = new Edit("Phase3", "Fields"+x.ToString(), Sys);
                ddFields.Type = Edit.GEType.Lookup;
                ddFields.CaptionText = "Field:";
                ddFields.LookupValues = ddFields.ConvertStringToLookupList("Address;City;State;Zip;Phone");
                s.AddControl(ddFields);
                string sValueOfFieldsControl = Sys.GetObjectValue("Phase3", "Fields" + x.ToString());
                
                Edit ddConditions = new Edit("Phase3", "Conditions"+x.ToString(), Sys);
                ddConditions.Type = Edit.GEType.Lookup;
                ddConditions.CaptionText = "Conditions:";
                ddConditions.LookupValues = new List<SystemObject.LookupValue>();
                SystemObject.LookupValue i1 = new SystemObject.LookupValue();
                i1.ID = "Underlying ID";
                i1.Value = "DisplayValue";
                i1.Caption = "Caption";
                string sValueOfLookup = Sys.GetObjectValue("Phase3", "Conditions" + x.ToString());
                ddConditions.LookupValues.Add(i1);

                SystemObject.LookupValue i2 = new SystemObject.LookupValue();
                i2.ID = "Underlying ID2";
                i2.Value = "DisplayValueTwo";
                i2.Caption = "CaptionTwo";
                ddConditions.LookupValues.Add(i2);
                s.AddControl(ddConditions);
                Edit txtValue = new Edit("Phase3", "SelectedValue"+x.ToString(), Sys);
                txtValue.Type = Edit.GEType.Text;
                txtValue.CaptionText = "Selected Value:";
                string sValueOfSelected = Sys.GetObjectValue("Phase3", "SelectedValue" + x.ToString());
                s.AddControl(txtValue);
            }
            s.AddButton("btnAddCondition", "Add Condition");
            WebReply wr1 = s.Render(this, true);
            return wr1;

        }

        public WebReply btnAddCondition_Click()
        {
            int iConditionCount = Convert.ToInt32(Sys.GetObjectValue("Phase3", "ConditionCount")) + 1;
            Sys.SetObjectValue("Phase3", "ConditionCount", iConditionCount.ToString());
            return Phase3();
        }


        public WebReply btnLightbox_Click()
        {
                Section s = new Section("Lightbox",1, Sys, this);
                Edit g = new Edit("Lightbox",Edit.GEType.Lightbox,Sys);
                g.Name = "Lightbox";
                g.CaptionText = "Open";
                g.URL = "images/Data.png";
                s.AddControl(g);
                WebReply wr1 = s.Render(this, true);
                return wr1;
        }
       
    }
}