using System;
using System.Data;

namespace BiblePayPool2018
{
    
    public class Ticket : USGDGui
    {

        public Ticket(SystemObject S) : base(S)
        {
            this.ObjectName = "Ticket";
      
        }

        public WebReply TicketList_ContextMenu_Add()
        {
             return TicketSearch();
        }
       
        public WebReply TicketSearch()
        {

            Section TicketSearch = Sys.RenderSection("Ticket Search", "Ticket", 1, this, SystemObject.SectionMode.Search);
            Edit geBtnB = TicketSearch.AddButton("btnSearch", "Search");
            return TicketSearch.Render(this, true);
        }
       
        public WebReply btnSearch_Click()
        {
            string sWhereClause = Sys.GenerateWhereClauseFromSection("Ticket Search","Ticket");
            //Filter the ticket_list web list based on the Search fields.
            Sys.SetObjectValue("Ticket List","TicketSearchWhereClause",sWhereClause);
            return TicketList(); 
        }

        public WebReply TicketAdd()
        {
            Section TicketAdd = Sys.RenderSection("Ticket View", "Ticket", 1, this, SystemObject.SectionMode.Edit);
            Edit geBtnB = TicketAdd.AddButton("btnSave", "Save");
            WebReply ta = TicketAdd.Render(this, true);
            return ta;
        }

        public WebReply TicketList_AddNew()
        {
   
            Section TicketAdd = Sys.RenderSection("Ticket View", "Ticket", 1, this, SystemObject.SectionMode.Add);
            Edit geBtnB = TicketAdd.AddButton("btnSave", "Save");
            WebReply ta = TicketAdd.Render(this, true);
            return ta;
        }


        public WebReply btnSave_Click()
        {
            Sys.SaveObject("Ticket View", "Ticket", this);
            this.ViewGuid = Sys.LastWebObject.guid;
            return TicketView();
        }

        public WebReply TicketView()
        {
            Section sT = Sys.RenderSection("Ticket View", "Ticket", 1, this, SystemObject.SectionMode.View);
            //Add Ticket View + Ticket History to web reply
            WebReply wTV = sT.Render(this, true);
            WebReply wTH = TicketHistory();
            wTV.AddWebPackages(wTH.Packages);
            return wTV;
        }


        public WebReply TicketView_EditClick()
        {
            // Enter Edit Mode on the Page
            this.SectionMode = SystemObject.SectionMode.Edit;
            return TicketAdd();
        }
      
        // Ticket History area:
        public WebReply TicketHistory()
        {
            //Id,TicketId,Body,Added,updated,deleted
            SQLDetective s = Sys.GetSectionSQL("Ticket History", "TicketHistory", this.ViewGuid);
            Weblist w = new Weblist(Sys);
            WebReply wr = w.GetWebList(s.GenerateQuery(), "Ticket History", "tickethistory", "Body", "TicketHistory",this,false);
            return wr;
        }
         
        public WebReply TicketList()
        {
            //Produces a web list of Tickets assigned to the current user
            SQLDetective s = Sys.GetSectionSQL("Ticket List", "Ticket", string.Empty);
            // If they are coming in from the search page
            if (WhereClause.Length > 0)
            {
                s.WhereClause = WhereClause;
            }
            else
            {
                if (ParentID == "") ParentID = Sys.Organization.ToString();
                s.WhereClause = "AssignedTo='" + Sys.UserGuid.ToString() + "' and Ticket.deleted=0  and Ticket.ParentID = '" + this.ParentID.ToString() + "'";
         
            }
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
            WebReply wr = w.GetWebList(sql, "My Tickets", "Ticket List", "","Ticket",this,false);
            return wr;
        }


        public WebReply TicketHistory_OrderByClick()
        {
            return TicketView();
            
        }

        public WebReply TicketList_OrderByClick()
        {
            return TicketList();
        }

        public WebReply TicketList_RowClick()
        {
            ViewGuid = Sys.LastWebObject.guid;
            //Update the Ticket View section
            Sys.SetObjectValue("Ticket View","TicketHistoryGuid", String.Empty);
            return TicketView();
        }

        public WebReply TicketHistory_RowClick()
        {
            Sys.SetObjectValue("Ticket List","TicketHistoryGuid", Sys.LastWebObject.guid);
            string sql = "Select Tickethistory.ParentID from ticketHistory where tickethistory.id = '" + USGDFramework.Shared.GuidOnly(Sys.LastWebObject.guid)
                + "'";
            DataTable dt = Sys._data.GetDataTable2(sql);
            string sParentID = dt.Rows[0]["ParentID"].ToString();
            Sys.SetObjectValue("Ticket List","TicketViewGuid", sParentID);
            //Update the Ticket View section
            return TicketView();
        }
        
    }
}