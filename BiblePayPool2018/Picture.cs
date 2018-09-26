using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Web.SessionState;

namespace BiblePayPool2018
{

    public class Picture : USGDGui, IRequiresSessionState
    {

        public Picture(SystemObject S) : base(S)
        {
            this.ObjectName = "Picture";
            S.CurrentHttpSessionState["CallingClass"] = this;
        }

        public WebReply DocumentSearch()
        {
            Section Search = Sys.RenderSection("Picture Search", "Picture", 1, this, SystemObject.SectionMode.Search);
            Edit geBtnB = Search.AddButton("btnSearch", "Search");
            return Search.Render(this, true);
        }

        public WebReply btnSearch_Click()
        {
            WhereClause = Sys.GenerateWhereClauseFromSection("Picture Search", "Picture");
            return PictureList();
        }

        public WebReply PictureAdd()
        {
            Section Add = Sys.RenderSection("Picture Add", "Picture", 1, this, SystemObject.SectionMode.Edit);
            Edit ctlUpload = new Edit("Picture Add", Edit.GEType.UploadControl, "btnSave", "Save", Sys);
            ctlUpload.Id = this.ViewGuid;
            ctlUpload.ParentGuid = this.ParentID;
            ctlUpload.ParentType = "Picture";
            Add.AddControl(ctlUpload);
            return Add.Render(this, true);
        }


        public WebReply btnSave_Click()
        {
            Sys.SaveObject("Picture Add", "Picture", this);
            return PictureView();
        }

        public WebReply PictureView()
        {
            Section sT = Sys.RenderSection("Picture View", "Picture", 1, this, SystemObject.SectionMode.View);
            string sURL = Sys.GetPictureURL(this.ViewGuid);
            sT.DisplayPictureHTML = "<a id=PV name=PV data-featherlight='" + sURL + "'><img src='" + sURL + "' /></a>";
            WebReply wTV = sT.Render(this, true);
            return wTV;
        }

        public WebReply PictureList_AddNew()
        {
            this.SectionMode = SystemObject.SectionMode.Add;
            return PictureAdd();
        }


        public WebReply PictureView_EditClick()
        {
            this.SectionMode = SystemObject.SectionMode.Add;
            return PictureAdd();
        }

        public WebReply PictureList()
        {
            SQLDetective s = Sys.GetSectionSQL("Picture List", "Picture", string.Empty);
            if (WhereClause.Length > 0)
            {
                s.WhereClause = WhereClause;
            }
            else
            {
                s.WhereClause = "Picture.Organization='" + Sys.Organization.ToString() + "' and Picture.Deleted=0  and Picture.ParentID = '" + this.ParentID.ToString() + "'";
            }
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
            WebReply wr = w.GetWebList(sql, "Picture List", "Picture List", "", "Picture", this, false);
            return wr;
        }

        public WebReply PictureGallery()
        {
            string sql = "Select ID from Picture where Picture.Organization='" + clsStaticHelper.GuidOnly(Sys.Organization.ToString())
                + "' and Picture.Deleted=0";
            DataTable dt = Sys._data.GetDataTable2(sql);
            string html = "<DIV>";
            for (int iRows = 0; iRows < dt.Rows.Count; iRows++)
            {
                string sURL = Sys.GetPictureURL(dt.Rows[iRows]["id"].ToString());
                string cell = "<a id=PV name=PV data-featherlight='" + sURL + "'><img width=150 height=150 src='" + sURL + "' /></a>&nbsp;";
                html += cell;

            }
            html += "</DIV>";
       
            Section s = new Section("Picture Gallery", 1, Sys, this);
            Edit g = new Edit("PictureGallery", Edit.GEType.HTML, Sys);
            g.Name = "pg1";
            g.HTML = html;
            s.AddControl(g);
            return s.Render(this, false);
            
        }

        public WebReply PictureList_OrderByClick()
        {
            return PictureList();
        }

        public WebReply PictureList_RowClick()
        {
            ViewGuid = Sys.LastWebObject.guid;
            return PictureView();
        }
        

        public WebReply btnLightbox_Click()
        {
            Section s = new Section("Lightbox", 1, Sys, this);
            Edit g = new Edit("Lightbox", Edit.GEType.Lightbox, Sys);
            g.Name = "Lightbox";
            g.CaptionText = "Open";
            g.URL = "images/BiblePay.png";
            s.AddControl(g);
            WebReply wr1 = s.Render(this, true);
            return wr1;
        }

    }
}