using System;
using Microsoft.VisualBasic;
using System.Collections.Generic;
using System.Web;
using System.Data;
using System.Web.SessionState;
using static USGDFramework.Shared;
using System.Threading;

namespace BiblePayPool2018
{
    public class Home : USGDGui, IRequiresSessionState
    {
        public Home(SystemObject S) : base(S)
        {
            S.CurrentHttpSessionState["CallingClass"] = this;
        }

        public WebReply ExpenseList()
        {
            string sql = "Select top 2048 * from Expense ORDER BY ";
            SQLDetective s = Sys.GetSectionSQL("Expense List", "Expense", string.Empty);
            if (s.OrderBy.Contains("Updated"))
            {
                s.OrderBy = " Added";
            }
            sql += s.OrderBy;
            Weblist w = new Weblist(Sys);
            w.bShowRowHighlightedByUserName = false;
            w.bSupportCloaking = false;
            w.sGrandTotalColumn = "Amount";
            WebReply wr = w.GetWebList(sql, "Expense List", "Expense List", "", "Expense", this, false);
            return wr;
        }

        public WebReply NewExchangeFundList()
        {
            string sql = "Select * from NewExchangeFund ";
            SQLDetective s = Sys.GetSectionSQL("New Exchange Fund", "NewExchangeFund", string.Empty);
            s.OrderBy = " Added";
            sql += s.OrderBy;
            Weblist w = new Weblist(Sys);
            w.bShowRowHighlightedByUserName = false;
            w.bSupportCloaking = false;
            w.sGrandTotalColumn = "BTCAmount";
            WebReply wr = w.GetWebList(sql, "New Exchange Fund List", "New Exchange Fund List", "", "NewExchangeFund", this, false);
            return wr;
        }

        public WebReply TxHistory_Export()
        {
            string sql = "Select ID,TransactionType,Destination,Amount,NewBalance,Updated,Height,Notes From TransactionLog where "
                    + " NetworkID = '" + Sys.NetworkID + "' AND UserID='" + Sys.UserGuid.ToString() + "' ORDER BY Updated desc";
            string URL = Sys.SqlToCSV(sql);
            Dialog e = new Dialog(Sys);
            string sNarr = "<a href='" + URL + "'><font color=orange><b>Click here to download CSV File.</a>";
            WebReply wr2 = e.CreateDialog("Data_Export", "Data_Exported", "Data Exported.  " + sNarr +"",300, 150);
            return wr2;
        }

        public WebReply TxHistory()
        {
            string sql = "Select top 1024 ID,TransactionType,Destination,Amount,NewBalance,Updated,Height,Notes From TransactionLog where "
                + " NetworkID = '" + Sys.NetworkID + "' AND UserID='" + Sys.UserGuid.ToString() + "' ORDER BY ";
            SQLDetective s = Sys.GetSectionSQL("Transaction History", "TransactionLog", string.Empty);
            if (s.OrderBy.Contains("Updated"))
            {
                s.OrderBy = " Updated desc";
            }
            sql += s.OrderBy;
            Weblist w = new Weblist(Sys);
            w.bShowRowHighlightedByUserName = false;
            w.bSupportCloaking = false;
            w.bShowRowExport = true;
            WebReply wr = w.GetWebList(sql, "Transaction History", "Transaction History", "", "TransactionLog", this, false);
            return wr;
        }

        public WebReply AdminYes_Click()
        {
            Dialog e = new Dialog(Sys);
            WebReply wr2 = e.CreateDialog("Admin1", "Not_Supported", "This feature is not yet supported (Error code 60008).", 100, 100);
            return wr2;
        }

        public WebReply Admin()
        {
            if (Sys.Username != USGDFramework.clsStaticHelper.GetConfig("AdminUser"))

            {
                Dialog d = new Dialog(Sys);
                WebReply wr = d.CreateYesNoDialog("Admin", "AdminError", "AdminYes", "AdminNo", "Admin_Error", "Sorry, you do not have admin privileges.  Would you like to apply for them?", this);
                return wr;
            }
            Dialog e = new Dialog(Sys);
            WebReply wr2 = e.CreateDialog("Admin1", "Successfully paid old participants", "Successfully paid old participants.", 100, 100);
            return wr2;
        }

        public WebReply LettersInbound()
        {
            string sql = "Select top 500 * from LettersInbound order by ";
            SQLDetective s = Sys.GetSectionSQL("Orphan Inbound Letters", "LettersInbound", string.Empty);
            if (s.OrderBy.Contains("Updated"))
            {
                s.OrderBy = " Added desc,OrphanID, Page";
            }
            sql += s.OrderBy;
            Weblist w = new Weblist(Sys);
            w.bShowRowHighlightedByUserName = false;
            w.URLDefaultValue = "View";
            w.bSupportCloaking = false;
            WebReply wr = w.GetWebList(sql, "Orphan Inbound Letters", "Inbound Letters", "", "LettersInbound", this, false);
            return wr;
        }

        public WebReply LettersInbound_RowClick()
        {
            string sId = Sys.LastWebObject.guid.ToString();
            string sql = "Select URL,Name,Page from LettersInbound where id = '" + sId + "'";
            string sURL = Sys._data.GetScalarString2(sql, "URL");
            string sName = Sys._data.GetScalarString2(sql, "Name");
            string sPage = Sys._data.GetScalarString2(sql, "Page");
            string sNarr = sName + " - " + sPage.ToString();
            Section s = new Section("l1", 1, Sys, this);
            Dialog d = new Dialog(Sys);
            string sBody = "<img width=900 height=1000 src='" + sURL + "'>";
            string sNarr2 = sNarr.Replace(" ", "_");
            WebReply wr = d.CreateDialog(sNarr2, "View1", sBody, 1000, 1100);
            return wr;
        }

        public WebReply Leaderboard_RowClick()
        {
            string sId = Sys.LastWebObject.data2;
            if (sId == "")
                return FormLoad();

            string sql = "select Max(hps.id) id, minername, avg(funded) funded,count(threadid) as Threads, avg(totalhps) as hps,  avg(threadshares) Shares,"
                + " round(avg(totalhps) / avg(maxhps) * 100, 2) as health"
                +"  from hps where username = '" + sId + "' group by minername order by minername ";
            Section s1 = new Section("Stats View", 1, Sys, this);
            Dialog d = new Dialog(Sys);
            SQLDetective s = Sys.GetSectionSQL("Stats View", "Stats View", string.Empty);
            Weblist w = new Weblist(Sys);
            w.bShowRowHighlightedByUserName = true;
            w.bSupportCloaking = true;
            WebReply wr = w.GetWebList(sql, "Leaderboard Details View", "Leaderboard Details View", "", "HPS", this, false);
            string html = wr.Packages[0].HTML;
            WebReply wr2 = d.CreateDialogWebList("Leaderboard_Details_View", "Leaderboard_Details_View", html, 1200, 640);
            return wr2;
        }

        public WebReply BlockDistribution_RowClick()
        {
            
            string sId = Sys.LastWebObject.guid.ToString();
            if (sId == "") return BlockDistribution();
            string sql = "Select top 512 block_distribution.id,uz.username, block_distribution.height, block_distribution.block_subsidy, "
                + " block_distribution.subsidy, block_distribution.paid, block_distribution.hps, block_distribution.PPH, "
                + " block_distribution.updated, block_distribution.stats,uz.cloak  from block_distribution "
                + " inner join Uz on uz.id = block_distribution.userid WHERE Block_Distribution.ID='" + sId + "' ";
            Section s1 = new Section("Stats View", 1, Sys, this);
            Dialog d = new Dialog(Sys);
            SQLDetective s = Sys.GetSectionSQL("Stats View", "Stats View", string.Empty);
            Weblist w = new Weblist(Sys);
            w.bShowRowHighlightedByUserName = true;
            w.bSupportCloaking = true;
            WebReply wr = w.GetWebList(sql, "Stats View", "Stats View", "", "Block_Distribution", this, false);
            string html = wr.Packages[0].HTML;
            WebReply wr2 = d.CreateDialogWebList("Stats_View", "Stats_View", html, 1200, 640);
            return wr2;
        }

        public WebReply BlockDistribution()
        {
            string sql = "Select * from blockdistribution" + Sys.NetworkID;
            SQLDetective s = Sys.GetSectionSQL("Block Distribution View", "Block_Distribution", string.Empty);
            Weblist w = new Weblist(Sys);
            w.bShowRowHighlightedByUserName = true;
            w.bSupportCloaking = true;
            WebReply wr = w.GetWebList(sql, "Block Distribution View", "Block Distribution View", "", "Block_Distribution", this, false);
            return wr;
        }

        string sqlUnbanked = "select tempteam.id,tempteam.rosettaid, tempteam.UserName,sum(isnull(temphostdetails1.RAC,0)) as RAC, sum(isnull(temphostdetails2.rac,0)) as ARMRac "
    + "  from TempTeam"
    + " inner join TempHosts on tempHosts.RosettaID = tempTeam.RosettaID "
    + " left join TempHostDetails temphostdetails1 on TempHosts.Computerid = temphostdetails1.computerid and isnull(temphostdetails1.arm,0) = 0 "
    + " left join temphostdetails temphostdetails2 on temphosts.computerid = temphostdetails2.computerid  and temphostdetails2.arm = 1 "
    + " left join superblocks on temphosts.rosettaid = superblocks.rosettaid and superblocks.added > getdate() - 1 "
    + " group by tempteam.id,tempteam.rosettaid, tempteam.username"
    + " having sum(isnull(temphostdetails2.rac,0)) > 0 and sum(isnull(temphostdetails1.rac,0)) < 15 "
    + " and sum(isnull(superblocks.wcgrac,0)) < 100 and avg(superblocks.unbanked)=1";

        public WebReply RosettaARMReport()
        {
            SQLDetective s = Sys.GetSectionSQL("Rosetta Leaderboard View", "tempHosts", string.Empty);
            s.OrderBy = "";
            Weblist w = new Weblist(Sys);
            w.bShowRowHighlightedByUserName = true;
            w.bSupportCloaking = true;
            WebReply wr = w.GetWebList(sqlUnbanked, "Rosetta Unbanked Report", "Rosetta Unbanked Report", "", "tempHosts", this, false);
            return wr;
        }

        public WebReply RosettaARMReport_RowClick()
        {
            string sSourceTable = "Temp";
            WebReply wr = GetDrillDialog(sSourceTable, "Rosetta Unbanked View", sqlUnbanked);
            return (wr == null) ? Leaderboard() : wr;
        }

        public WebReply SuperblockView()
        {
            SQLDetective s = Sys.GetSectionSQL("Superblock View", "superblocks", string.Empty);
            s.OrderBy = " modifiedrac desc";
            s.WhereClause = " height = (Select max(height) from Superblocks) ";
            Weblist w = new Weblist(Sys);
            w.bShowRowHighlightedByUserName = true;
            w.bSupportCloaking = true;
            string sql2 = s.GenerateQuery();
            WebReply wr = w.GetWebList(sql2, "Superblock View", "Superblock View", "", "superblocks", this, false);
            return wr;
        }

        public WebReply SuperblockView_RowClick()
        {
            string sSourceTable = "Superblocks";
            WebReply wr = GetDrillDialog(sSourceTable, "Superblock View" , "");
            return (wr == null) ? Leaderboard() : wr;
        }

        public WebReply SuperblockViewNonBiblePay()
        {
            SQLDetective s = Sys.GetSectionSQL("Superblock View Non BiblePay", "superblocks", string.Empty);
            s.OrderBy = " modifiedrac desc";
            s.WhereClause = " height = (Select max(height) from Superblocks) and team <> 15044 ";
            Weblist w = new Weblist(Sys);
            w.bShowRowHighlightedByUserName = true;
            w.bSupportCloaking = true;
            string sql2 = s.GenerateQuery();
            WebReply wr = w.GetWebList(sql2, "Superblock View Non BiblePay", "Superblock View Non BiblePay", "", "superblocks", this, false);
            return wr;
        }

        string sqlMachine = "select top 50 temphosts.id,tempteam.username as Username,temphostdetails.rac as MachineRAC, temphostdetails.added as Added,"
                + "temphostdetails.computerid as ComputerID, tempteam.rac as RAC, temphostdetails.cpu,"
                + "temphosts.rosettaID, temphostdetails.procs, temphostdetails.memory, temphostdetails.fps as CalcsPerSecond,"
                + " temphostdetails.turnaround as Turnaround"
            + ", temphostdetails.rac / 1000 as MachineRank, "
            + "temphostdetails.fps* temphostdetails.procs / 5000 as MachinePower"
            + " from tempHosts inner join TempHostDetails on TempHosts.Computerid = temphostdetails.computerid"
            + " inner join TempTeam on TempHosts.rosettaID = tempteam.rosettaid order by tempHosts.RAC desc";

        public WebReply BlockHistory()
        {
            string sql = "Select top 512 ID,Height,Updated,Subsidy,MinerNameWhoFoundBlock as Miner_Who_Found_Block,MinerNameByHashPs as Top_Miner_By_HashPS,Funded,BlockVersion FROM BLOCKS where networkid='"
                + Sys.NetworkID + "' order by  Updated Desc ";
            SQLDetective s = Sys.GetSectionSQL("Block Distribution View", "Block_Distribution", string.Empty);
            if (s.OrderBy.Contains("Updated"))
            {
                s.OrderBy = " height desc";
            }
            s.OrderBy = "";
            Weblist w = new Weblist(Sys);
            w.bShowRowHighlightedByUserName = true;
            w.bSupportCloaking = true;
            WebReply wr = w.GetWebList(sql, "Block History View", "Block History View", "", "Blocks", this, false);
            return wr;
        }

        public WebReply Leaderboard()
        {
            string sql1 = "Select Value from System where systemkey='main'";
            string sHealth = mPD.GetScalarString2(sql1, "Value");
            if (sHealth == "DOWN")
            {
                Dialog d = new Dialog(this.Sys);
                WebReply wr1 = d.CreateDialog("DialogHealth", "Leaderboard View", "This network is currently down for maintenance.  We are sorry for the inconvenience.  Please try back soon.  ",
                    700, 400);
                return wr1;
            }

            string sSourceTable = "Leaderboard" + Sys.NetworkID + "summary";
            SQLDetective s = Sys.GetSectionSQL("Leaderboard View", sSourceTable, string.Empty);
            if (WhereClause.Length > 0)
            {
                s.WhereClause = WhereClause;
            }
            else
            {
                if (ParentID == "") ParentID = Sys.Organization.ToString();
                s.WhereClause = " 1=1";
            }
            if (s.OrderBy.Contains("Updated"))
            {
                s.OrderBy = "hps desc";
            }
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
            w.ExtraColumnPost = "username";
            w.bShowRowHighlightedByUserName = true;
            w.bSupportCloaking = true;

            w.AddContextMenuitem("Detail", "Detail", "Detail");

            WebReply wr = w.GetWebList(sql, "Leaderboard View", "Leaderboard View", "", "Leaderboard", this, false);
            return wr;
        }

        public WebReply Leaderboard_ContextMenu_Detail()
        {
            return Leaderboard_RowClick();
        }

        void AddLookups(Edit gedit, List<string> s)
        {
            for (int i = 0; i < s.Count; i++)
            {
                string sValue = s[i];
                SystemObject.LookupValue i1 = new SystemObject.LookupValue();
                i1.ID = sValue;
                i1.Value = sValue;
                i1.Caption = sValue;
                gedit.LookupValues.Add(i1);
            }
        }

        public struct PhoneCarrier
        {
            public string CarrierName;
            public string Domain;
        }

        public List<string> GetCarrierNames()
        {
            List<PhoneCarrier> l1 = GetCarriers();
            List<string> s1 = new List<string>();
            for (int i = 0; i < l1.Count; i++)
            {
                string sCarrier = l1[i].CarrierName;
                s1.Add(sCarrier);
            }
            return s1;
        }

        public List<PhoneCarrier> GetCarriers()
        {
            List<PhoneCarrier> lp = new List<PhoneCarrier>();
            string sCarrier = "ATT|@txt.att.net,T-Mobile|@tmomail.net,Verizon|@vtext.com,Sprint|@messaging.sprintpcs.com,XFinity Mobile|@vtext.com,Virgin Mobile|@vmobl.com,Tracfone|@mmst5.tracfone.com,Metro PCS|@mymetropcs.com,Boost Mobile|@sms.myboostmobile.com,Cricket|@sms.cricketwireless.net,Republic Wireless|@text.republicwireless.com,Google Fi|@msg.fi.google.com,U.S.Cellular|@email.uscc.net,Ting|@message.ting.com,Consumer Cellular|@mailmymobile.net,C-Spire|@cspire1.com,Page Plus|@vtext.com";
            string[] vCarriers = sCarrier.Split(new string[] { "," }, StringSplitOptions.None);
            for (int i = 0; i < vCarriers.Length; i++)
            {
                string sRow = vCarriers[i];
                string[] vRow = sRow.Split(new string[] { "|" }, StringSplitOptions.None);
                PhoneCarrier p = new PhoneCarrier();
                p.CarrierName = vRow[0];
                p.Domain = vRow[1];
                lp.Add(p);
            }
            return lp;
        }

        public WebReply GetDrillDialog(string sqltablename, string sSectionName,string sOptSQL)
        {
            Dialog d = new Dialog(Sys);
            string sId = Sys.LastWebObject.guid.ToString();
            string sql = "";
            if (sOptSQL == "")
            {
                SQLDetective s = Sys.GetSectionSQL(sSectionName, sqltablename, string.Empty);
                s.WhereClause = " id = '" + sId + "'";
                s.OrderBy = "";
                sql = s.GenerateQuery();
            }
            else
            {
                sql = sOptSQL;
            }
            USGDTable dt = Sys.GetUSGDTable(sql, sqltablename);
            string sHTML = "<TABLE>";
            if (dt.Rows < 1) return null;
            double dCloak = 0;
            if (dt.Value(0, "cloak") != null) dCloak = GetDouble(dt.Value(0, "cloak").ToString());
            if (dt.Rows > 0)
            {
                for (int c = 0; c < dt.Cols; c++)
                {
                    string sCN = dt.ColumnNames[c];
                    string sCaption = Sys.GetFieldCaption(sqltablename, sCN);
                    string sValue = dt.Value(0, c).ToString();
                    if (dCloak == 1 && sCaption == "username") sValue = "Anonymous";
                    if (sCaption != "id" && sCaption.ToLower() != "cloak")
                    {
                        string sRow = "<TR><TD>" + sCaption + ": </td><td>" + sValue + "</tr>";
                        sHTML += sRow;
                    }
                }
            }
            sHTML += "</TABLE>";
            WebReply wr = d.CreateDialog("Dialog1", sSectionName + " Details", sHTML, 700, 400);
            return wr;

        }

        public WebReply Leaderboard_OrderByClick()
        {
            return Leaderboard();
        }

        public WebReply MyLeaderboard()
        {
            string sSourceTable = "Leaderboard" + Sys.NetworkID;
            SQLDetective s = Sys.GetSectionSQL("My Leaderboard", sSourceTable, string.Empty);
            if (ParentID == "") ParentID = Sys.Organization.ToString();
            s.WhereClause = " username='" + Sys.Username + "'";
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
            w.ExtraColumnPost = "username";
            WebReply wr = w.GetWebList(sql, "My Leaderboard", "My Leaderboard", "", "Leaderboard", this, false);
            return wr;
        }

        public WebReply MyMissingMiners()
        {
            string sql = "select [uz].id,[uz].username, miners.username as minername,miners.highhpsmain as hps,"
                + "miners.updated from Miners inner join uz on uz.id = miners.userid  where miners.Updated > getdate() - 7 and "
                +" miners.Updated < getdate() - 1 / 48.01 and highhps" + Sys.NetworkID + " > 0"
                + " and uz.id='" + Sys.UserGuid.ToString() + "'";
            
            Weblist w = new Weblist(Sys);
            WebReply wr = w.GetWebList(sql, "My Missing Miners", "My Missing Miners", "", "My Missing Miners", this, false);
            return wr;
        }

        public WebReply NetworkTest()
        {
            Dialog d = new Dialog(Sys);
            WebReply wr = d.CreateDialog("Network", "Switch Network", "Network changed to Test", 0, 0);
            Sys.NetworkID = "test";
            return wr;
        }

        public WebReply NetworkMain()
        {
            Dialog d = new Dialog(Sys);
            WebReply wr = d.CreateDialog("Network", "Switch Network", "Network changed to Main", 0, 0);
            Sys.NetworkID = "main";
            return wr;
        }

        public void Bind(string SectionName, string fieldNames, DataTable dt, int iRowNumber)
        {
            string[] vCR = fieldNames.Split(new string[] { "," }, StringSplitOptions.None);
            for (int i = 0; i < vCR.Length; i++)
            {
                string field = vCR[i];
                string myValue = dt.Rows[iRowNumber][field].ToString();

                Sys.SetObjectValue(SectionName, field, myValue);
            }
        }

        public WebReply AccountEdit()
        {
            string sql = "Select * from Uz with (nolock) where id='" +  GuidOnly(Sys.UserGuid) + "' and deleted=0";
            System.Data.DataTable dt = Sys._data.GetDataTable2(sql);
            if (dt.Rows.Count > 0)
            {
                Bind("Account Edit", "Theme,CellPhone,Carrier,VerificationCode,Email,WithdrawalAddress", dt, 0);
                string sSuffix = Sys.NetworkID == "main" ? "" : "testnet";
                string sCloak = dt.Rows[0]["cloak"].ToString() == "1" ? "true" : "false";
                Sys.SetObjectValue("Account Edit", "Cloak", sCloak);
                double dBalance = GetDouble(dt.Rows[0]["Balance" + Sys.NetworkID]);
                Sys.SetObjectValue("Account Edit", "Balance", dBalance.ToString());
                Bind("Account Edit", "Address1,Address2,City,State,Zip,DelName,Phone,Country,CPID,AdoptedOrphanID,AdoptionDate,LetterDeadline", dt, 0);
                double dWAV =  GetDouble(dt.Rows[0]["WithdrawalAddressValidated"]);
                string sWAV = (dWAV == 1) ? "On" : "Off";
                Sys.SetObjectValue("Account Edit", "WithdrawalAddressValidated", sWAV);
            }

            Section AccountEdit = new Section("Account Edit", 2, Sys, this);
            Edit geDefaultWithdrawalAddress = new Edit("Account Edit", "DefaultWithdrawalAddress", Sys);
            geDefaultWithdrawalAddress.CaptionText = "Default Withdrawal Address:";
            geDefaultWithdrawalAddress.TextBoxStyle = "width:420px";
            AccountEdit.AddControl(geDefaultWithdrawalAddress);
            AccountEdit.AddBlank();

            Edit gePassword = new Edit("Account Edit", Edit.GEType.Password, "Password", "Password:", Sys);
            AccountEdit.AddControl(gePassword);
            AccountEdit.AddBlank();
            if (gePassword.TextBoxValue.Length > 0 && gePassword.TextBoxValue.Length < 3 && Sys.GetObjectValue("AccountEdit", "Caption1") == String.Empty)
            {
                gePassword.ErrorText = "Invalid Username or Password";
            }

            Edit geEmail = new Edit("Account Edit", "Email", Sys);
            geEmail.CaptionText = "Email:";
            AccountEdit.AddControl(geEmail);
            Edit geBtnEmail = new Edit("Account Edit", Edit.GEType.Button, "btnEmailVerify", "Verify E-Mail", Sys);
            AccountEdit.AddControl(geBtnEmail);
            // Theme
            AccountEdit.AddBlank();
            Edit ddTheme = new Edit("Account Edit", "Theme", Sys);
            ddTheme.Type = Edit.GEType.Lookup;
            ddTheme.CaptionText = "Theme:";
            ddTheme.LookupValues = new List<SystemObject.LookupValue>();
            AddDropDown(ref ddTheme, "Biblepay", "Biblepay");
            //ddTheme.LookupValues.Add(i1);
            AddDropDown(ref ddTheme, "Dark", "Dark");
            AccountEdit.AddBlank();
            AccountEdit.AddControl(ddTheme);
            AccountEdit.AddBlank();
            // Bible Pay Store

            AddLabel("Account Edit", "lblA", "<p><ul><b>The following optional fields are used for the BiblePay Store:",AccountEdit, "");
            AccountEdit.AddBlank();
            Edit geDelName = new Edit("Account Edit", "DelName", Sys);
            geDelName.CaptionText = "Delivery Name:";
            geDelName.TextBoxStyle = "width:450px";
            AccountEdit.AddControl(geDelName);
            AccountEdit.AddBlank();
            Edit geAddr1 = new Edit("Account Edit", "Address1", Sys);
            geAddr1.CaptionText = "Address Line 1:";
            geAddr1.TextBoxStyle = "width:420px";
            AccountEdit.AddControl(geAddr1);
            AccountEdit.AddBlank();
            Edit geAddr2 = new Edit("Account Edit", "Address2", Sys);
            geAddr2.CaptionText = "Address Line 2:";
            geAddr2.TextBoxStyle = "width:420px";
            AccountEdit.AddControl(geAddr2);
            AccountEdit.AddBlank();
            Edit geCity = new Edit("Account Edit", "City", Sys);
            geCity.CaptionText = "City:";
            AccountEdit.AddControl(geCity);
            AccountEdit.AddBlank();
            Edit geState = new Edit("Account Edit", "State", Sys);
            geState.CaptionText = "State:";
            geState.TextBoxStyle = "width:110px";
            AccountEdit.AddControl(geState);
            AccountEdit.AddBlank();
            Edit geZip = new Edit("Account Edit", "Zip", Sys);
            geZip.CaptionText = "Postal Code:";
            geZip.TextBoxStyle = "width:250px";
            AccountEdit.AddControl(geZip);
            AccountEdit.AddBlank();
            Edit geCountry = new Edit("Account Edit", "Country", Sys);
            geCountry.CaptionText = "Country:";
            AccountEdit.AddControl(geCountry);
            AccountEdit.AddBlank();
            Edit gePhone = new Edit("Account Edit", "Phone", Sys);
            gePhone.CaptionText = "Delivery Phone Number:";
            AccountEdit.AddControl(gePhone);

            AccountEdit.AddBlank();

            // Airdrop info
            AddLabel("Account Edit", "lblB", "<p><ul><b>The following optional fields are used for airdrops and faucet rewards:", AccountEdit, "");
            AccountEdit.AddBlank();

            Edit geCell = new Edit("Account Edit", "CellPhone", Sys);
            geCell.CaptionText = "Cell Phone Number:";
            AccountEdit.AddControl(geCell);

            Edit geCarrier = new Edit("Account Edit", "Carrier", Sys);
            geCarrier.Type = Edit.GEType.Lookup;
            geCarrier.CaptionText = "Carrier:";
            geCarrier.LookupValues = new List<SystemObject.LookupValue>();
            List<string> s1 = GetCarrierNames();
            AddLookups(geCarrier, s1);
            AccountEdit.AddBlank();
            AccountEdit.AddControl(geCarrier);
            AccountEdit.AddBlank();
            // Cell Phone Verification Code

            Edit geVerCode = new Edit("Account Edit", "VerificationCode", Sys);
            geVerCode.CaptionText = "Cell Phone Verification Code:";
            AccountEdit.AddControl(geVerCode);

            Edit geBtnCellVerify = new Edit("Account Edit", Edit.GEType.DoubleButton, "btnSendSMSCode", "Send SMS Code", Sys);
            geBtnCellVerify.Name2 = "btnVerifySMSCode";
            geBtnCellVerify.CaptionText2 = "Verify SMS Code";
            AccountEdit.AddControl(geBtnCellVerify);

            // Automatic Withdrawals
            Edit geAW = new Edit("Account Edit", "WithdrawalAddressValidated", Sys, "Automatic Withdrawals:");
            AccountEdit.AddControl(geAW);

            // Implement a "Double button" here:
            Edit geBtnAuto = new Edit("Account Edit", Edit.GEType.DoubleButton, "btnRecAddressVerify", "Enable Automatic Withdrawals", Sys);
            geBtnAuto.Name2 = "btnRecOff";
            geBtnAuto.CaptionText2 = "Disable Automatic Withdrawals";
            AccountEdit.AddControl(geBtnAuto);

            Edit geBtnSave = new Edit("Account Edit", Edit.GEType.Button, "btnAccountSave", "Save", Sys);
            AccountEdit.AddControl(geBtnSave);
            AccountEdit.AddBlank();
            return AccountEdit.Render(this, true);
        }
        string GetSMSDomain(string Carrier)
        {
            List<PhoneCarrier> lp = GetCarriers();
            for (int i = 0; i < lp.Count;i++)
            {
                if (lp[i].CarrierName == Carrier)
                    return lp[i].Domain;
            }
            return "";
        }

        string GetSMSEmail()
        {
            string sql = "Select * From Uz where id = '" + GuidOnly(Sys.UserGuid) + "'";
            string sPhone = mPD.GetScalarString2(sql, "cellphone");
            string sCarrier = GetSMSDomain(mPD.GetScalarString2(sql, "carrier"));
            if (sPhone == "" || sCarrier == "") return "";
            if (GetDouble(sPhone) == 0) return "";
            string sPhoneNo = sPhone + sCarrier;
            return sPhoneNo;
        }

        public WebReply btnSendSMSCode_Click()
        {
            Dialog d = new Dialog(Sys);
            Random e = new Random();
            int iPin = e.Next(50000);
            string sEmail = GetSMSEmail();
            string sql = "Update Uz set SMSCode = '" + iPin.ToString() + "' where id = '" + GuidOnly(Sys.UserGuid) + "'";
            mPD.Exec2(sql);
            string sBody = "BiblePay PIN Code: " + iPin.ToString();
            bool sent = false;
            if (sEmail != "")
            {
                sent =  Sys.SendEmail(sEmail, "BiblePay SMS", sBody, false);
            }
            string sNarr = "";
            if (sent == false || sEmail == "")
            {
                sNarr = "Unable to send SMS Pin code.  Please check your cell phone number and your Carrier Name and try again.";
            }
            else
            {
                sNarr = "A PIN code has been sent to your cell phone.  If you did not receive it, please check your phone number and your Carrier Name and try again.  ";
            }
            WebReply wr2 = d.CreateDialog("Notify", "BBP SMS Send", sNarr, 650, 350);
            WebReply wrAE = AccountEdit();
            wr2.AddWebPackages(wrAE.Packages);
            return wr2;
        }

        public WebReply btnVerifySMSCode_Click()
        {
            string sql = "Select * from Uz where id = '" + GuidOnly(Sys.UserGuid) + "'";
            string sCode = mPD.GetScalarString2(sql, "SMSCode");
            string sMyCode = Sys.GetObjectValue("Account Edit", "VerificationCode");
            string sNarr = "";
            if (sCode == "")
            {
                sNarr = "Your SMS Code has not been sent yet.  Please click Send SMS Code first.";
            }
            else if (sMyCode == "")
            {
                sNarr = "You must type your SMS verification code into Account Edit first, please populate the field first.";
            }
            else if (sMyCode == sCode)
            {
                sNarr = "SMS Code Verified!  Welcome to BiblePay!";
                sql = "Update UZ set CellPhoneVerified=1 where id = '" + Sys.UserGuid.ToString() + "'";
                mPD.Exec2(sql, false, false);
            }
            else if (sMyCode != sCode)
            {
                sNarr = "The verification codes did not match.  Please re-send your SMS verification code, re-type it, click Save Record, then click Verify SMS Code.";
            }
            Dialog d = new Dialog(Sys);
            string sData = new Random().Next(11000).ToString();
            WebReply wr2 = d.CreateDialog("VerifySMSCode" + sData, "BBP SMS Send", sNarr, 650, 350);
            WebReply wrAE = AccountEdit();
            wr2.AddWebPackages(wrAE.Packages);
            return wr2;
        }

        public WebReply btnRecAddressVerify_Click()
        {
            Dialog d = new Dialog(Sys);
            string sNarr = "You have agreed to receive <font color=red>automatic payments</font> once per day when the balance exceeds the minimum threshhold.  <font color=yellow> Please ensure that you keep your BiblePay receiving address Up To Date, otherwise you risk losing pool withdrawals.  Your address has been verified. ";
            string sAddress = Sys.GetObjectValue("Account Edit","DefaultWithdrawalAddress");
            bool bValid = Sys.ValidateBiblepayAddress(sAddress, Sys.NetworkID);
            if (!bValid) sNarr = "Invalid BiblePay Address.  Please update it before continuing.";
            if (bValid)
            {
                //update the user record
                string sql = "Update Uz set WithdrawalAddressValidated=1 where id = '" +  GuidOnly(Sys.UserGuid) + "'";
                 mPD.Exec2(sql);
            }
            WebReply wr2 = d.CreateDialog("Notify000", "BBP Address Verification", sNarr, 650, 350);
            WebReply wrAE = AccountEdit();
            wr2.AddWebPackages(wrAE.Packages);
            return wr2;
        }

        public WebReply btnRecOff_Click()
        {
            Dialog d = new Dialog(Sys);
            string sNarr = "Automatic Withdrawals have been <font color=yellow> disabled. </font> ";
            string sAddress = Sys.GetObjectValue("Account Edit", "DefaultWithdrawalAddress");
            string sql = "Update Uz set WithdrawalAddressValidated=0 where id = '" + GuidOnly(Sys.UserGuid) + "'";
             mPD.Exec2(sql);
            WebReply wr2 = d.CreateDialog("Notify2", "Automatic Withdrawals Disabled", sNarr, 650, 350);
            WebReply wrAE = AccountEdit();
            wr2.AddWebPackages(wrAE.Packages);
            return wr2;
        }

        public WebReply btnEmailVerify_Click()
        {
            Dialog d = new Dialog(Sys);
            string sEmail = Sys.GetObjectValue("Account Edit", "Email");
            string sNarr = "";
            if (!Sys.IsValidEmailFormat(sEmail))
            {
                sNarr = "Email address on file for this user is not valid.  Please update the e-mail address.";
            }
            else
            {
                sNarr = "E-Mail verification instructions have been sent, please check your inbox.";
                string sLink = HttpContext.Current.Request.Url.ToString();
                sLink = sLink.Substring(0, sLink.Length - 10);
                sLink += "/Action.aspx?action=verify_email&id=" + Sys.UserGuid.ToString();
                string sBody = "Dear " + Sys.Username.ToUpper() + ",<br><br>Please follow these instructions to verify your E-Mail address:<br><br>Please Click the Link Below to Verify your E-Mail address.  <br><br><br><br><a href='" + sLink + "'>Verify E-Mail Address</a><br><br>Thank you for using BiblePay.<br><br>Best Regards,<br>BiblePay Support";
                bool sent = Sys.SendEmail(sEmail, "BiblePay E-Mail Verification System", sBody, true);
            }
            WebReply wr2 = d.CreateDialog("Notify",
                "Verify", sNarr, 150, 150);
            return wr2;
        }

        public WebReply btnAccountSave_Click()
        {
            Dialog d = new Dialog(Sys);
            if (Sys.Username.ToUpper() == "GUEST")
            {
                WebReply wr3 = d.CreateDialog("Error", "Validation Requirements", "Sorry, Illegal Activity.", 150, 150);
                return wr3;
            }

            int iCloak = Sys.GetObjectValue("Account Edit", "Cloak") == "true" ? 1 : 0;
            string sql = "Update Uz set VerificationCode=@VerificationCode,CellPhone=@CellPhone,Carrier=@Carrier,DelName=@DelName,CPID=@CPID,Address1=@Address1,Phone=@Phone,Address2=@Address2,City=@City,State=@State,Zip=@Zip,Country=@Country,Updated=getdate(), Cloak='"
                + iCloak.ToString() + "', Email=@Email, Theme=@Theme, WithdrawalAddress=@DefaultWithdrawalAddress where id='"
                 +  GuidOnly(Sys.UserGuid)  + "' and deleted=0";
            sql = Sys.PrepareStatement("Account Edit", sql);
            Sys._data.Exec2(sql);
            string sNarr = "Record Updated.";
            string sNewPass = Sys.GetObjectValue("Account Edit", "Password");
            if (sNewPass.Contains("'"))
            {
                WebReply wr2 = d.CreateDialog("Error", "Password does not meet Validation Requirements", "Sorry, Password contains illegal characters: ', Please remove illegal characters.", 150, 150);
                return wr2;
            }
            if (sNewPass.Length > 0 && sNewPass.Length < 5)
            {
                WebReply wr3 = d.CreateDialog("Error", "Password does not meet Validation Requirements", "Sorry, Password too short.", 150, 150);
                return wr3;
            }
            if (sNewPass.Length > 3)
            {
                string sql2 = "Update Uz set password='[txtAltPass]' where id= '" +  GuidOnly(Sys.UserGuid)
                    + "' and deleted=0";
                sql2 = Sys.PrepareStatement("Account Edit", sql2);
                sql2 = sql2.Replace("[txtAltPass]", USGDFramework.modCryptography.SHA256(sNewPass));
                Sys._data.Exec2(sql2);
                sNarr += " (PASSWORD UPDATED.) ";
            }
            WebReply wr = d.CreateDialog("Account", "Account Edit", sNarr, 0, 0);
            return wr;
        }

        private void AddLabel(string sSectionName, string sControlName, string sCaption, Section s, string sValue)
        {
            Edit lbl1 = new Edit(sSectionName, sControlName, Sys);
            lbl1.Type = Edit.GEType.Label;
            lbl1.CaptionText = sCaption;
            lbl1.TextBoxValue = sValue;
            s.AddControl(lbl1);
        }

        private void AddLabelCSS(string sSectionName, string sControlName, string sCaption, Section s, string sValue, string sCSS, string sValueCSS)
        {
            Edit lbl1 = new Edit(sSectionName, sControlName, Sys);
            lbl1.Type = Edit.GEType.Label;
            lbl1.CaptionText = sCaption;
            lbl1.TextBoxValue = sValue;
            lbl1.CaptionWidthCSS = " style='" + sCSS + ";'";
            lbl1.TextBoxStyle = sValueCSS;
            s.AddControl(lbl1);
        }

        private void AddLabelWithCaptionWidth(string sSectionName, string sControlName, string sCaption, Section s, string sValue, int CaptionWidth)
        {
            Edit lbl1 = new Edit(sSectionName, sControlName, Sys);
            lbl1.Type = Edit.GEType.Label;
            lbl1.CaptionText = sCaption;
            lbl1.TextBoxValue = sValue;
            lbl1.CaptionWidthCSS = " style='width:" + CaptionWidth.ToString() + "px;'";
            s.AddControl(lbl1);
        }

        public WebReply Vote_Click(string sVote)
        {
            string sId = Sys.LastWebObject.guid;
            if (sId.Length == 0)
            {
                Dialog d = new Dialog(Sys);
                WebReply wr = d.CreateDialog("Error", "Vote Failure", "Failed to access vote record.", 100, 100);
                return wr;
            }

            string sql22 = "Select EmailVerified from Uz where id='" + GuidOnly(Sys.UserGuid.ToString()) + "'";
            double dV = mPD.GetScalarDouble2(sql22, "EmailVerified");
            if (dV == 0 && false)
            {
                Dialog d = new Dialog(Sys);
                WebReply wr = d.CreateDialog("Error (1)", " Vote Failure!", "Your email must be verified first before you can vote.", 100, 150);
                return wr;
            }

                string sIP = (HttpContext.Current.Request.UserHostAddress ?? "").ToString();
            string sWhere =" where letterid = '" +  GuidOnly(sId) 
                + "' and (userid = '" +  GuidOnly(Sys.UserGuid) + "')";
            string sql = "Select count(*) ct from Votes " + sWhere;
            double dExistCount = Sys._data.GetScalarDouble2(sql, "ct");
            double dUpvote = (sVote == "upvote") ? 1 : 0;
            double dDownvote = (sVote == "downvote") ? 1 : 0;
    
            if (true)
            {
                sql = "Delete from Votes where (Userid='" + Sys.UserGuid.ToString() + "' OR ip='" + sIP + "') and letterid = '" + GuidOnly(sId) + "'";
                Sys._data.Exec2(sql);
                sql = "insert into votes (id,added,letterid,userid,upvote,downvote,ip) values (newid(),getdate(),'" + sId
                + "','" + Sys.UserGuid.ToString() + "'," + dUpvote.ToString()
                + "," + dDownvote.ToString() + ",'" + sIP + "')";
                Sys._data.Exec2(sql);
            }
           
            sql = "update Letters set upvote=(Select sum(Upvote) from Votes where letterid='" + GuidOnly(sId)
                + "'), downvote = (Select sum(downvote) from votes where letterid='" +  GuidOnly(sId)
                + "') where id='" +  GuidOnly(sId) + "'";
            Sys._data.Exec2(sql);
            sql = "update letters set Approved=0 where Upvote-Downvote < 10";
            Sys._data.Exec2(sql);
            sql = "update letters set Approved=1 where Upvote-Downvote >= 10";
            Sys._data.Exec2(sql);
            sql = "update letters set Sent=0 where sent is null";
            Sys._data.Exec2(sql);
            sql = "Update Orphans set NeedWritten = 0 where 1=1";
            Sys._data.Exec2(sql);
            sql = "Update Orphans set NeedWritten = 1 where OrphanID not in (Select OrphanID from letters where added > getdate()-60 and len(body) > 100)";
            Sys._data.Exec2(sql);
            return OrphanLetters();
        }

        public WebReply Announce()
        {
            object o = HttpContext.Current.Session["Announce"];
            Random d = new Random();
            int i1 = d.Next(100);
            if (o != null && i1 > 40)
                return (WebReply)o;

            Section Announce = new Section("Announce", 1, Sys, this);
            Announce.MaskSectionMode = true;
            string sql = "Select Value from System where SystemKey='Announce'";
            string a = Sys._data.GetScalarString2(sql, "Value", false);
            AddLabelWithCaptionWidth("Announce", "lblA", "Announcements:", Announce, a, 290);
            AddLabel("Announce", "lblD", "<p>", Announce, "");
            // Outgoing letter enforcement fees
            AddLabel("Announce", "lblOC1", "<p>", Announce, "");
            double dLWF = Convert.ToDouble(USGDFramework.clsStaticHelper.GetConfig("fee_letterwriting"));
            double dUpvotedCount =  GetUpvotedLetterCount();
            double dRequiredCount =  GetRequiredLetterCount();
            AddLabel("Announce", "lblOC", "Outbound Approved Letter Count:", Announce, dUpvotedCount.ToString());
            AddLabel("Announce", "lblRQC", "Outbound Required Letter Count:", Announce, dRequiredCount.ToString());
            double dMyLetterCount =  GetMyLetterCount(Sys.UserGuid.ToString());
            AddLabel("Announce", "lblLetterCount", "My Outgoing Letter Count:", Announce, dMyLetterCount.ToString());
            double dBounty =  GetTotalBounty();
            double dFactor = Convert.ToDouble(USGDFramework.clsStaticHelper.GetConfig("letterwritingfactor"));
            double dIndBounty = Math.Round(dBounty / (dRequiredCount + .01), 2) * dFactor;
            AddLabel("Announce", "lblLetterBounty", "Current Approved Letter Writing Bounty:", Announce, dIndBounty.ToString());
            // Total Paid bounties out of pool
            double dPaidBounties =  GetTotalLetterBountiesPaid(1);
            double dTotalBounties =  GetTotalLetterBountiesPaid(0);
            string sNarr = " (" + dPaidBounties.ToString() + "/" + dTotalBounties.ToString() + ")";
            AddLabel("Announce", "lbllbp1", "Letter Bounties Paid (Paid / Funds donated to Pool):", Announce, sNarr);
            // Balance
            double dBal = 0;
            double dImm = 0;
             GetUserBalances(Sys.NetworkID, Sys.UserGuid.ToString(), ref dBal, ref dImm);
            AddLabel("Announce", "lblBal", "Total Balance:", Announce, dBal.ToString());
            AddLabel("Announce", "lblImm", "Immature Balance:", Announce, dImm.ToString());
            // User stats - coins mined in last 1, 24 hour and 7 days
            sql = "Select sum(Amount) a from TransactionLog (nolock) where Added > getdate()-(2/24.01) and TransactionType = 'MINING_CREDIT'  and networkid = '" +  VerifyNetworkID(Sys.NetworkID)
                + "' and userid='" + Sys.UserGuid.ToString() + "'";
            double dUC1 = Math.Round( mPD.GetScalarDouble2(sql, "a",false), 2) / 2;
            sql = "Select sum(Amount) a from TransactionLog (nolock) where Added > getdate()-(1) and TransactionType = 'MINING_CREDIT'  and networkid = '" +  VerifyNetworkID(Sys.NetworkID)
                + "' and userid='" +  GuidOnly(Sys.UserGuid)
                + "'";
            double dUC24 = Math.Round( mPD.GetScalarDouble2(sql, "a",false), 2);
            sql = "Select sum(Amount) a from TransactionLog (nolock) where Added > getdate()-(7) and TransactionType = 'MINING_CREDIT'  and networkid = '" + 
                 VerifyNetworkID(Sys.NetworkID)
                + "' and userid='" +  GuidOnly(Sys.UserGuid)
                + "'";
            double dUC7d = Math.Round( mPD.GetScalarDouble2(sql, "a",false), 2);
            AddLabel("Announce", "uc1", "Coins mined (in the last hour):", Announce, dUC1.ToString());
            AddLabel("Announce", "uc7", "Coins mined (in the last 24 hours):", Announce, dUC24.ToString());
            AddLabel("Announce", "uc24", "Coins mined (in the last week):", Announce, dUC7d.ToString());
            // BTC Price, BBP Price
            Zinc.BiblePayMouse.MouseOutput m =  GetCachedCryptoPrice("bbp");
            AddLabel("Announce", "lblBTC", "BTC Price: ", Announce, "$" + m.BTCPrice.ToString());
            AddLabel("Announce", "lblBBP", "BBP Price:", Announce, "$" + m.BBPPrice.ToString("0." + new string('#', 339)));
            AddLabel("Announce", "lblHR", "", Announce, "<hr>");
            AddLabel("Announce", "lblP", "<p>", Announce, "");
            AddLabel("Announce", "lblOC5", "<p>", Announce, "");
            bool bAcc = (HttpContext.Current.Request.Url.ToString().ToUpper().Contains("ACCOUNTABILITY"));
            Sys.lOrphanNews++;
            bool bShowOrphanNews = (bAcc || Sys.lOrphanNews == 1);
            if (true)
            {
                string sVideo =  mPD.GetScalarString2("Select Value as Video from System (nolock) where SystemKey='Video'", "Video",false);
                string sOrphanNews = "<xdiv style='width:400;height:400'><iframe src='" + sVideo
                    + "' /></xdiv>";
                AddLabel("Announce", "vidCompassion", "Orphan News:", Announce, sOrphanNews);
            }
            sql = "Update Uz set LastLogin=getdate() where id = '" + Sys.UserGuid.ToString() +"'";
            Sys._data.Exec2(sql);
            HttpContext.Current.Session["Announce"] = Announce.Render(this, true);
            return (WebReply)HttpContext.Current.Session["Announce"];
        }

        public WebReply MyTweets()
        {
            Section secTweets = new Section("Tweets", 1, Sys, this);
            string sTweets = "<div style='width:100%; height:450; overflow:auto;'><a class='twitter-timeline' "
                +"href='https://twitter.com/BiblePay?ref_src=twsrc%5Etfw'>Tweets by BiblePay</a> "
                + "<script src='https://platform.twitter.com/widgets.js' charset='utf-8'></script><script>window.onerror = function() { return false; };</script></div>";

            Edit tweet = new Edit("Tweets", Edit.GEType.HTML, Sys);
            tweet.HTML = sTweets;
            tweet.cols = 2;
            tweet.Name = "tweet";
            secTweets.AddControl(tweet);
            return secTweets.Render(this, false);
        }

        public WebReply MyCPID()
        {
            Section secCpid = new Section("CPID", 1, Sys, this);
            string sql = "Select CPID from Uz where uz.id='" + Sys.UserGuid.ToString() + "'";
            string sCpid =  mPD.GetScalarString2(sql, "CPID", false);
            sql = "Select * from Superblocks where CPID='" + sCpid + "' and height = (Select max(height) from superblocks)";
            DataTable dtCPID =  mPD.GetDataTable2(sql, false);
            if (dtCPID.Rows.Count < 1)
            {
                AddLabel("CPID", "lbl2", "CPID:", secCpid, "No information is available.  Please setup your CPID in account info. ");
            }
            else
            {
                string sStyle = "padding-left: 100px";
                AddLabelCSS("CPID", "lbl44", "CPID:", secCpid, sCpid, sStyle, "");

                AddLabelCSS("CPID", "lbl4", "Magnitude:", secCpid, dtCPID.Rows[0]["Magnitude"].ToString(), sStyle,"");
                AddLabelCSS("CPID", "lbl5", "UTXO Weight:", secCpid, dtCPID.Rows[0]["UTXOWeight"].ToString(), sStyle,"");
                AddLabelCSS("CPID", "lbl7", "RAC:", secCpid, dtCPID.Rows[0]["AvgRac"].ToString(),sStyle,"");
                AddLabelCSS("CPID", "lbl8", "Unbanked:", secCpid, dtCPID.Rows[0]["Unbanked"].ToString(), sStyle,"");
                double dMagnitude =  GetDouble(dtCPID.Rows[0]["Magnitude"]);
                double nextpayment = 1197260 * (dMagnitude / 1000);
                AddLabelCSS("CPID", "lbl11", "Estimated Next Payment:", secCpid, nextpayment.ToString(), sStyle,"");
            }
            return secCpid.Render(this, false);
        }

        public WebReply About()
        {
            Random d = new Random();
            int i1 = d.Next(100);
            if (i1 > 35) clsStaticHelper.mAboutWebReply = null;
            if ( clsStaticHelper.mAboutWebReply != null) return clsStaticHelper.mAboutWebReply;
           
            Section About = new Section("About", 1, Sys, this);
            Edit lblInfo = new Edit("About", "lblInfo", Sys);
            lblInfo.Type = Edit.GEType.Label;
            lblInfo.CaptionText = "Pool Information:";
            About.AddControl(lblInfo);
            AddLabel("About", "lblProvidedBy", "Provided By:", About, USGDFramework.clsStaticHelper.GetConfig("OperatedBy"));
            string sURL = HttpContext.Current.Request.Url.ToString();
            sURL = sURL.Substring(0, sURL.Length - 10);
            AddLabel("About", "lblDomain", "Domain:", About, sURL);
            AddLabel("About", "lblVersion", "Pool Version:", About, Version.ToString());
            string sQTVersion =  GetInfoVersion(Sys.NetworkID);
            AddLabel("About", "lblVersionQT", "Biblepay Version:", About, sQTVersion);
            AddLabel("About", "lblEmail", "E-Mail:", About, USGDFramework.clsStaticHelper.GetConfig("OwnerEmail"));
            AddLabel("About", "lblNetwork", "Network:", About, Sys.NetworkID);
            double dFee = Convert.ToDouble(USGDFramework.clsStaticHelper.GetConfig("fee"));
            double dFeeAnonymous = Convert.ToDouble(USGDFramework.clsStaticHelper.GetConfig("fee_anonymous"));
            AddLabel("About", "lblFee", "Base Pool Fees:", About, (dFee * 100).ToString() + "%");
            AddLabel("About", "lblFeeAnonymous", "Additional Anonymous User Pool Fees:", About, ((dFeeAnonymous + dFee) * 100).ToString() + "%");
            //  Calculate load 
            string sql = "Select count(*) As hitct, round((count(*) + 0.01) / 4000 * 100, 2) As load  From work with(nolock) Where added > getdate() - 0.000777";
            double load = Sys._data.GetScalarDouble2(sql, "load",false);
            AddLabel("About", "lblLoad", "Load:", About, (load).ToString() + "%");
            string sHeight = Sys.GetTipHeight(Sys.NetworkID).ToString();
            AddLabel("About", "lblHeight", "Height:", About, sHeight);
            // Calculate the Metrics
            double dMinerCount = 0;
            double dAvgHPS = 0;
            double dTotal = 0;
            string sWin = "";
            string sLin = "";
            string sMac = "";
             GetOSInfo("WIN", ref sWin, ref dMinerCount, ref dAvgHPS, ref dTotal);
             GetOSInfo("LIN", ref sLin, ref dMinerCount, ref dAvgHPS, ref dTotal);
             GetOSInfo("MAC", ref sMac, ref dMinerCount, ref dAvgHPS, ref dTotal);
            AddLabel("About", "lblwin", "Windows:", About, sWin);
            AddLabel("About", "lblLinux", "Linux:", About, sLin);
            AddLabel("About", "lblLinux", "Mac:", About, sLin);
            // Sum of Leaderboard
            sql = "Select sum(hps) hps,count(*) uz from (select avg(boxhps) hps ,minername from work with (nolock) where endtime is not null group by minername) a ";
            double dHPS = Math.Round( mPD.GetScalarDouble2(sql, "hps"), 2);
            double dUzers =  mPD.GetScalarDouble2(sql, "uz",false);
            AddLabel("About", "lblHP1", "Pool Total HPS:", About, dHPS.ToString());
            AddLabel("About", "lblUS1", "Pool Miners:", About, dUzers.ToString());
            //  Total Mined coins in 24 hour period
            sql = "Select sum(Amount) a from TransactionLog where Added > getdate() - 1 and TransactionType = 'MINING_CREDIT'  and networkid = '" 
                +  VerifyNetworkID(Sys.NetworkID)
                + "'";
            double dCoins24 = Math.Round( mPD.GetScalarDouble2(sql, "a",false), 2);
            sql = "Select count(distinct height) b from blocks where updated > getdate() - 1 and networkid = '" +  VerifyNetworkID(Sys.NetworkID)
                + "'";
            double dB24 = Math.Round( mPD.GetScalarDouble2(sql, "b",false), 2);
            AddLabel("About", "lblCM", "Coins Mined (24 hour period):", About, dCoins24.ToString());
            double dBP = Math.Round( (dB24 )/ 204, 2) * 100;
            AddLabel("About", "lblBM", "Blocks Mined (24 hour period):", About, dB24.ToString() + " (" + dBP.ToString() + "%)");
            // Funded blocks mined (24)
            sql = "Select value from System where systemkey='fbs'";
            double fbs24 = Math.Round(mPD.GetScalarDouble2(sql, "value", false),0);
            AddLabel("About", "lblUpdfbs24", "Funded Blocks Mined (24 hour period):", About, fbs24.ToString());
            sql = "Select value from System where systemkey='bsr'";
            double bsr24 = Math.Round(mPD.GetScalarDouble2(sql, "value", false), 2) * 100;
            AddLabel("About", "lblfbsr24", "Funded miner ABN fee (Funded Block Solve Ratio):", About, bsr24.ToString() + "%");
            // Last block Found
            sql = "select max(height) c,max(updated) d from blocks where networkid='" +  VerifyNetworkID(Sys.NetworkID)
                + "'";
            double dLBF =  mPD.GetScalarDouble2(sql, "c",false);
            string sUpd =  mPD.GetScalarString2(sql, "d",false);
            string sNarr2 = dLBF.ToString() + " (" + sUpd + ")";
            AddLabel("About", "lblUpdLb", "Last Height Mined:", About, sNarr2);
            double diff = Math.Round(GetDouble( GetMiningInfo(Sys.NetworkID, "difficulty")), 2);
            AddLabel("About", "lbldf", "Current Difficulty:", About, diff.ToString() );
            // Pool Efficiency
            sql = "select(86400 * 7) / count(distinct height) / 60 e from blocks where updated > getdate() - 7 and networkid = '" +  VerifyNetworkID(Sys.NetworkID) + "'";
            double d7 = Math.Round( mPD.GetScalarDoubleWithNoLog2(sql, "e"), 2);
            sql = "select(86400 * 1) / count(distinct height) / 60 f from blocks where updated > getdate() - 1 and networkid = '" +  VerifyNetworkID(Sys.NetworkID) + "'";
            double d1 = Math.Round( mPD.GetScalarDoubleWithNoLog2(sql, "f"), 2);
            AddLabel("About", "ats1", "Avg Time to Solve Block (Over 7 day period):", About, d7.ToString());
            AddLabel("About", "ats2", "Avg Time to Solve Block (Over 24 hour period):", About, d1.ToString());
            double pe = Math.Round(d7 / (d1) + .01, 2) * 100;
            AddLabel("About", "ats3", "Efficiency:", About, pe.ToString() + "%");
            sql = "Select max(MasternodeCount) a from Proposal where network='" + Sys.NetworkID + "' and paidtime is null";
            double mn1 =  mPD.GetScalarDouble2(sql, "a",false);
            AddLabel("About", "mnc1", "Masternode Count:", About, mn1.ToString());
            clsStaticHelper.mAboutWebReply = About.Render(this, true);
            return  clsStaticHelper.mAboutWebReply;
        }

        public WebReply LinkBody_Click()
        {
            Section s = new Section("l1", 1, Sys, this);
            Dialog d = new Dialog(Sys);
            string sCSS = "background-color:transparent";
            string sURL = "LetterWritingTips.htm";
            string sBody = "<iframe  style='" + sCSS + "'   width=100% height=900px  src='" + sURL + "'>";
            WebReply wr = d.CreateDialog("Letter_Writing_Tips", "View1", sBody, 1000, 1100);
            return wr;
        }

        public WebReply WritePics()
        {
            Section WritePics = new Section("Write - Pictures", 4, Sys, this);
            // Show thumbnails of every picture attached here:
            string sLetterGuid = Sys.GetObjectValue("Write", "LetterGuid");
            if (sLetterGuid.Length > 0)
            {
                string sql = "Select * from Picture where parentid='" +  GuidOnly(sLetterGuid) + "'";
                System.Data.DataTable dt = Sys._data.GetDataTable2(sql);
                string sRow = "";
                if (dt.Rows.Count > 0)
                {
                    for (int iRow = 0; iRow <= dt.Rows.Count - 1; iRow++)
                    {
                        string sPicId = dt.Rows[iRow]["id"].ToString();
                        string sURL = dt.Rows[iRow]["URL"].ToString();
                        sRow = "<img width='250' height='250' src='" + sURL + "' />";
                        Edit gePics = new Edit("Write", "lbl" + iRow.ToString(), Sys);
                        gePics.Type = Edit.GEType.Label;
                        gePics.CaptionText = sRow;
                        WritePics.AddControl(gePics);
                    }
                }
            }
            return WritePics.Render(this, false);
        }

        public WebReply LettersOutboundSpecificOrphan_RowClick()
        {
            string sId = Sys.LastWebObject.guid.ToString();
            string sql = "Select * from Letters where id = '" +  GuidOnly(sId) + "'";
            string sBody = Sys._data.GetScalarString2(sql, "Body");
            string sWrittenBy = Sys._data.GetScalarString2(sql, "Username");
            Section s = new Section("l1", 1, Sys, this);
            Dialog d = new Dialog(Sys);
            string sHTML = "<html><br>" + sBody + "<br><br>       [Press [ESC] to Close]</html>";
            WebReply wr = d.CreateDialog("nar1", "Outbound Letter (Written By: " + sWrittenBy + ") ", sHTML, 777, 577);
            return wr;
        }

        public WebReply LettersOutboundSpecificOrphan(string sOrphanID)
        {
            string sql = "Select id,OrphanID,Name,Added,username as WrittenBy from Letters WHERE orphanid = '" + sOrphanID + "' and body is not null and len(body) > 10 order by ";
            SQLDetective s = Sys.GetSectionSQL("Orphan Outbound Letters", "LettersOutbound", string.Empty);
            if (s.OrderBy.Contains("Updated"))
            {
                s.OrderBy = " Added desc,OrphanID";
            }
            sql += s.OrderBy;
            Weblist w = new Weblist(Sys);
            w.bShowRowHighlightedByUserName = false;
            w.URLDefaultValue = "View";
            w.bSupportCloaking = false;
            WebReply wr = w.GetWebList(sql, "Orphan Outbound Letters", "Outbound Letters", "", "LettersOutbound", this, false);
            return wr;
        }

        public WebReply LettersInboundSpecificOrphan(string sOrphanID)
        {
            string sql = "Select * from LettersInbound WHERE orphanid = '" + sOrphanID + "' order by ";
            SQLDetective s = Sys.GetSectionSQL("Orphan Inbound Letters", "LettersInbound", string.Empty);
            if (s.OrderBy.Contains("Updated"))
            {
                s.OrderBy = " Added desc,OrphanID, Page";
            }
            sql += s.OrderBy;
            Weblist w = new Weblist(Sys);
            w.bShowRowHighlightedByUserName = false;
            w.URLDefaultValue = "View";
            w.bSupportCloaking = false;
            WebReply wr = w.GetWebList(sql, "Orphan Inbound Letters", "Inbound Letters", "", "LettersInbound", this, false);
            return wr;
        }

        public WebReply WriteToOrphan()
        {
            Section Write = new Section("Write", 2, Sys, this);
            // To Recipient Name & Writer Name
            Edit geRecipient = new Edit("Write", "lblName", Sys);
            geRecipient.Type = Edit.GEType.Label;
            geRecipient.CaptionText = "Orphan Name: " + Sys.GetObjectValue("Write", "Name") 
                + "<br>To see the pictures attached to this letter please collapse and expand the Pictures section.  (<small>If you have problems in chrome adding a picture, Click the Menu Ellipsis | Settings | Scroll Down to Advanced and open | Scroll to the System Menu Div | Disable Use hardware acceleration.</small>)";

            Write.AddControl(geRecipient);
            Edit geWriter = new Edit("Write", "lblUsername", Sys);
            geWriter.Type = Edit.GEType.Label;
            geWriter.CaptionText = "Writer Username: " + Sys.GetObjectValue("Write", "Username");
            Write.AddControl(geWriter);

            //Body & Bio Caption
            Edit geLink1 = new Edit("Write", Edit.GEType.Anchor, "LinkBody", "<font color=yellow>Body (Letter Writing Tips)", Sys);
            geLink1.MaskColumn1 = true;
            Write.AddControl(geLink1);
            Edit geBioLabel = new Edit("Write", "lblBiography", Sys);
            geBioLabel.Type = Edit.GEType.Label;
            geBioLabel.CaptionText = "Biography:";
            Write.AddControl(geBioLabel);

            //Body & Bio
            Edit geBody = new Edit("Write", "Body", Sys);
            geBody.CaptionText = "";
            geBody.Type = Edit.GEType.TextArea;
            geBody.Height = "500px";
            geBody.cols = 90;
            geBody.TdWidth = "width='50%' ";
            geBody.rows = 24;
            geBody.MaskColumn1 = true;
            Write.AddControl(geBody);

            Edit geBio = new Edit("Write", "Biography", Sys);
            geBio.CaptionText = "Biography";
            geBio.Type = Edit.GEType.IFrame;
            string sLastOrphanID = Sys.GetObjectValue("Write", "OrphanID");
            string sql = "Select URL from Orphans where orphanid = '" +  PurifySQL(sLastOrphanID, 100) + "'";
            string sURL =  mPD.GetScalarString2(sql, "url", false);
            geBio.URL = sURL;  //10-4-2018

            geBio.Width = "100%";
            geBio.Height = "500px";
            
            Write.AddControl(geBio);
            // Who is owner
            string sWriterName = Sys.GetObjectValue("Write", "Username");
            string sTo = Sys.GetObjectValue("Write", "Name");
            bool bWriterIsUser = false;
            if (sWriterName == Sys.Username) bWriterIsUser = true;

            if (bWriterIsUser)
            {
                // Save Button
                Edit geSave = new Edit("Write", Edit.GEType.Button, "btnWriteSave", "Save", Sys);
                Write.AddControl(geSave);
                //Upload control 2
                Edit ctlUpload2 = new Edit("Write", Edit.GEType.UploadControl2, "btnUpload", "Add Picture", Sys);
                ctlUpload2.Id = Guid.NewGuid().ToString();
                string sLetterGuid2 = Sys.GetObjectValue("Write", "LetterGuid");
                ctlUpload2.ParentGuid = sLetterGuid2;
                ctlUpload2.ParentType = "Letter";
                Write.AddControl(ctlUpload2);
            }

            WebReply wrWrite = Write.Render(this, true);
            // Tack on the Recent Communication from the Orphan  4-7-2018
            WebReply orphanReply = LettersInboundSpecificOrphan(sLastOrphanID);
            wrWrite.AddWebPackages(orphanReply.Packages);
            // Tack on the Outbound recent communication from the Orphan 7-11-2018
            WebReply orphanReplyOutbound = LettersOutboundSpecificOrphan(sLastOrphanID);
            wrWrite.AddWebPackages(orphanReplyOutbound.Packages);
            // Tack on the pics added by the user
         
            WebReply wrPics = WritePics();
            wrWrite.AddWebPackages(wrPics.Packages);

            //WebReplyPackage wrp1 = wrPics.Packages[0];
            //wrp1.Javascript = "location.reload();";
            
            return wrWrite;
        }

        public WebReply btnUpload_Click()
        {
            Sys.SaveObject("Write", "Picture", this);
            return WriteToOrphan(); // This allows the user to see what they uploaded below the TextArea
        }

        public WebReply btnWriteSave_Click()
        {
            // If user owns this letter, allows EDIT-SAVE
            string sLetterGuid = Sys.GetObjectValue("Write", "LetterGuid");
            string sOrphanID = Sys.GetObjectValue("Write", "OrphanID");
            // if this is an edit, dont allow edit if they dont own it
            string sWriterName = Sys.GetObjectValue("Write", "Username");
            string sTo = Sys.GetObjectValue("Write", "Name");
            Dialog d = new Dialog(Sys);
            if (sWriterName != Sys.Username)
            {
                return d.CreateDialog("Error", "Record Not Updated", "Sorry, you are not authorized to Edit this message.  ", 100, 100);
            }

            if (sOrphanID == "")
            {
                return d.CreateDialog("Error", "Record Not Updated", "Sorry, Orphan ID empty.", 100, 100);
            }
            string sBody = Sys.GetObjectValue("Write", "Body");
            if (sBody.Length < 750)
            {
                return d.CreateDialog("Error", "Record Not Updated", "Sorry, Body must be longer.  ", 450, 200);
            }

            string[] vCR = sBody.Split(new string[] { "\n\n" }, StringSplitOptions.None);
            if (vCR.Length < 6)
            {
                return d.CreateDialog("Error", "Record Not Updated", "Sorry, Body must contain more than 4 paragraphs of content.  ", 450, 200);
            }

            sBody = sBody.Replace("\"", "`");
            sBody = sBody.Replace("'", "`");
            sBody = sBody.Replace("“", "`");
            sBody = sBody.Replace("”", "`");
            sBody = sBody.Replace("--", " - ");

            string sql = "Update Letters set body='" +  PurifySQLLong(sBody,3999)
                                + "',name='" +  PurifySQL(sTo,100)
                                + "',userid='"                +  GuidOnly(Sys.UserGuid)
                                + "',username='"              +  PurifySQL(Sys.Username,100)
                                + "',added=getdate(),orphanid='" +  PurifySQL(sOrphanID,100)
                                                + "' where id = '" +  GuidOnly(sLetterGuid)
                                                + "'";
            Sys._data.Exec2(sql);

            string sql2 = "Select AdoptedOrphanId from Uz where id='" + Sys.UserGuid.ToString() + "'";
            string sOrphanId = Sys._data.GetScalarString2(sql2, "AdoptedOrphanID", false);
            if (sOrphanId.Length > 1)
            {
                sql2 = "Select orphanid from orphans where id = '" +  PurifySQL(sOrphanId, 100) + "'";
                string sStringID = Sys._data.GetScalarString2(sql2, "orphanid", false);

                // If the user has an adopted child clear the flag
                sql = "Select count(*) a from Letters where Added > getdate()-32 and orphanid='" + sStringID + "' and userid='" + Sys.UserGuid.ToString() + "'";
                double dCt = Sys._data.GetScalarDouble2(sql, "a", false);
                if (dCt > 0)
                {
                    sql = "Update Uz set LetterDeadline=getdate()+31 where id = '" + Sys.UserGuid.ToString() + "'";
                    Sys._data.Exec2(sql);
                }
            }
            return OrphanLetters();
        }

        public string FirstURL(string sData)
        {
            string[] vURL = sData.Split(new string[] { "," }, StringSplitOptions.None);
            if (vURL.Length > 0)
            {
                return vURL[0];
            }
            return "";
        }

        private static string CleanStr(string data)
        {
            string d1 = data.Replace("'", "''");
            d1 = d1.Replace("[", "");
            d1 = d1.Replace("]", "");
            d1 = d1.Replace("\"", "");
            d1 = d1.Replace("\r\n", "<br>");
            return d1;
        }

        private static void UpdateProducts()
        {
            // Scan all products for items with a null url:  Pull the details down from BiblePayMouse, populate the details and URLs
            string sql = "Select * from Products where Pics IS NULL";
            string sToken = USGDFramework.clsStaticHelper.GetConfig("MouseToken");
            DataTable dt =  mPD.GetDataTable2(sql);
            Zinc.BiblePayMouse bm = new Zinc.BiblePayMouse(sToken);
            for (int i = 0; i <= dt.Rows.Count - 1; i++)
            {
                string sProdId = dt.Rows[i]["ProductID"].ToString();
                string sRetailer = dt.Rows[i]["Retailer"].ToString();
                string sItemGuid = dt.Rows[i]["id"].ToString();
                if (sProdId.Length > 1 && sRetailer.Length > 1)
                {
                    Zinc.BiblePayMouse.MouseOutput mo = bm.GetProductDetails(sProdId, sRetailer);
                    sql = "UPDATE Products set product_details = '" + CleanStr(mo.product_details)
                        + "',title='" + CleanStr(mo.title) + "',price='" 
                        + CleanStr(mo.price.ToString()) + "',Pics='" + CleanStr(mo.URL) + "' WHERE ID = '" + sItemGuid + "'";
                     mPD.Exec2(sql);
                }
            }
        }


        public WebReply btnBuy_Click()
        {

            // Verify US, Address, and Balance
            double dBal1 = 0;
            double dImmature = 0;
            GetUserBalances(Sys.NetworkID, Sys.UserGuid, ref dBal1, ref dImmature);
            double dBalAvailable = dBal1 - dImmature;
            string sBuyGuid = Sys.LastWebObject.guid;
            bool bFirstPass = true;

            string temp = Sys.GetObjectValue("Purchase", "OK");
            if (temp.Length > 16)
            {
                sBuyGuid = temp;
                bFirstPass = false;
                Sys.SetObjectValue("Purchase", "OK", "");
            }
            else
            {
                Sys.SetObjectValue("Purchase", "OK", sBuyGuid);
                bFirstPass = true;
            }
            string sql = "Select * from Products where id = '" + GuidOnly(sBuyGuid) + "'";
            DataTable dt = mPD.GetDataTable2(sql);
            Dialog d = new Dialog(Sys);
            WebReply wr;
            if (Sys.NetworkID != "main")
            {
                wr = d.CreateDialog("Purchase_Error", "Purchase Error", "Sorry, products are not available in TestNet.", 150, 150);
                return wr;
            }

            if (dt.Rows.Count == 0)
            {
                wr = d.CreateDialog("Purchase_Error", "Purchase Error", "Sorry, unable to locate product.", 150, 150);
                return wr;
            }
            double dPrice = Convert.ToDouble(dt.Rows[0]["price"].ToString());
            double dBBP = PriceInBBP(dPrice / 100);
            string sProductID = dt.Rows[0]["productID"].ToString();
            string sRetailer = dt.Rows[0]["Retailer"].ToString();
            string sTitle = dt.Rows[0]["Title"].ToString();

            if (Sys.NetworkID != "main")
            {
                wr = d.CreateDialog("Purchase_Error", "Purchase Error", "Sorry, storefront is unavailable in testnet.", 250, 250);
                return wr;
            }
            if (dBalAvailable < dBBP)
            {
                wr = d.CreateDialog("Purchase_Error", "Purchase Error", "Sorry, your available balance is less than the product price of " + dBBP.ToString() + " BBP.", 250, 250);
                return wr;
            }
            // Add a new Order if the account is in good standing
            sql = "Select * from Uz where id='" + GuidOnly(Sys.UserGuid) + "'";
            DataTable dtU = mPD.GetDataTable2(sql);
            string sAddressVerified = "";
            string sCountry = "";
            if (dtU.Rows.Count > 0)
            {
                sAddressVerified = dtU.Rows[0]["AddressVerified"].ToString();
                sCountry = dtU.Rows[0]["Country"].ToString().ToUpper();
            }

            bool bUSA = (sCountry == "US" || sCountry == "USA" || sCountry == "UNITED STATES");
            if (!bUSA)
            {
                wr = d.CreateDialog("Purchase_Error", "Purchase Error", "Sorry, we are only shipping to US Addresses at this time.  If you are in the US, please set your Country Code in Account Settings.", 250, 250);
                return wr;
            }

            if (sAddressVerified != "1")
            {
                //Call out to Smarty Streets
                string sAddr1 = dtU.Rows[0]["Address1"].ToString();
                string sAddr2 = dtU.Rows[0]["Address2"].ToString();
                string sCity = dtU.Rows[0]["City"].ToString();
                string sState = dtU.Rows[0]["State"].ToString();
                string sZip = dtU.Rows[0]["Zip"].ToString();
                Zinc.BiblePayMouse bm_0 = new Zinc.BiblePayMouse("");
                Zinc.BiblePayMouse.MouseOutput mo = bm_0.ValidateAddress(sAddr1, sAddr2, sCity, sState, sZip);
                if (!mo.dpv_match_code)
                {
                    wr = d.CreateDialog("Purchase_Error", "Purchase Error", "Sorry, Address Validation Failed.  Please update your address in Account Settings.", 250, 250);
                    return wr;
                }
                else
                {
                    sql = "Update uz set AddressVerified='1' where id = '" + GuidOnly(Sys.UserGuid) + "'";
                    mPD.Exec2(sql);
                }
            }

            bool is_gift = false;
            Zinc.BiblePayMouse.shipping_address shipAddr = new Zinc.BiblePayMouse.shipping_address();
            shipAddr.address_line1 = dtU.Rows[0]["Address1"].ToString();
            shipAddr.address_line2 = dtU.Rows[0]["Address2"].ToString();
            shipAddr.city = dtU.Rows[0]["City"].ToString();
            shipAddr.state = dtU.Rows[0]["State"].ToString();
            shipAddr.zip_code = dtU.Rows[0]["Zip"].ToString();
            string sDelName = dtU.Rows[0]["DelName"].ToString();
            shipAddr.country = dtU.Rows[0]["Country"].ToString();
            shipAddr.phone_number = dtU.Rows[0]["Phone"].ToString();
            shipAddr.first_name = GetNameElement(sDelName, 0).ToUpper();
            shipAddr.last_name = GetNameElement(sDelName, 1).ToUpper();
            if (shipAddr.phone_number.Length != 10 && shipAddr.phone_number.Length != 12)
            {
                wr = d.CreateDialog("Purchase_Error", "Purchase Error", "Sorry, our carrier must have a phone number that is either 10 or 12 digits long.  Please modify your phone number in account settings.", 250, 250);
                return wr;
            }

            // Create the actual order guid **********************************
            string sOrderGuid = Guid.NewGuid().ToString();
            sql = "Insert into Orders (id,added) values ('" + sOrderGuid + "',getdate())";
            mPD.Exec2(sql);

            // Double Check the user really wants it?
            string sNarr = "Are you sure you would like to purchase '" + sProductID + "' for " + dBBP.ToString() + "?";
            WebReply wr1 = d.CreateYesNoDialog("Purchase", "Purchase", "PurchaseYes", "PurchaseNo",
                "BiblePay Storefront", sNarr, this);

            if (bFirstPass) return wr1;

            // The user has a good address, enough BBP, go ahead and buy it
            int iHeight = Sys.GetTipHeight(Sys.NetworkID);

             InsTxLog(Sys.Username,Sys.UserGuid,Sys.NetworkID,
                iHeight, sOrderGuid, -1 * dBBP, dBal1, dBal1-dBBP, sOrderGuid, "PURCHASE", sProductID);
            bool bTrue =  AdjustUserBalance(Sys.NetworkID,Sys.UserGuid,-1 * dBBP);

            sql = "Update Orders Set Userid='" +  GuidOnly(Sys.UserGuid)
                 + "',ProductId='"     +  PurifySQL(sProductID,60)
                 + "',Title='" + PurifySQL( sTitle,200)
                 + "',Added=getdate(),Amount='" + dBBP.ToString() 
                 + "',FirstName='" + PurifySQL(shipAddr.first_name,200)
                 + "',LastName='" + PurifySQL(shipAddr.last_name,200)
                 + "',Address1='" + PurifySQL(shipAddr.address_line1,200)
                 + "',Address2='" + PurifySQL(shipAddr.address_line2, 200)
                 + "',city='" + PurifySQL(shipAddr.city,200)
                 + "',Phone='" + PurifySQL(shipAddr.phone_number,20) 
                 + "',State = '" + PurifySQL(shipAddr.state,20)
                 + "',zip='" + PurifySQL(shipAddr.zip_code,20)
                 + "' where id = '" +  GuidOnly(sOrderGuid) + "'";

             mPD.Exec2(sql);

            string sErr = "";
            string sBody = "Thank you for your order.  Your tracking number will be updated shortly. "
                +" To see updates on your order, please see our Store | Orders List.";

            sql = "Update Orders set Status3='PROCESSING',updated=getdate() where ID = '" + GuidOnly(sOrderGuid) + "'";
            mPD.Exec2(sql);

            if (sErr == "")
            {
                // Send a notification to Rob
                // notify rob when the hackers start voting
                string sBody1 = "Dear Rob - New store order has been received \n" + sOrderGuid + ".";
                string sEmail1 = "rob@biblepay.org";
                bool sent = Sys.SendEmail(sEmail1, "New Order in Store", sBody1, true, true);


                wr = d.CreateDialog("Success", "Processing", sBody, 500, 200);
            }
            else
            {
                wr = d.CreateDialog("Success", "Unable to Process", sErr, 800, 400);
            }

            return wr;
        }

        public WebReply PurchaseYes_Click()
        {
            
            return btnBuy_Click();
        }

        public WebReply PurchaseNo_Click()
        {
            string sBuyGuid = Sys.LastWebObject.guid;
            Sys.SetObjectValue("Purchase", "OK", "");
            return StoreList();
        }

        public WebReply OrdersList()
        {
             GetOrderStatusUpdates();
            SQLDetective s = Sys.GetSectionSQL("Orders List", "Orders", string.Empty);
            s.WhereClause = "UserId='" + Sys.UserGuid.ToString() + "' ";
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
            WebReply wr = w.GetWebList(sql, "Orders List", "Orders List", "", "Orders", this, false);
            return wr;
        }

        public WebReply StoreList()
        {
            UpdateProducts();
            Section StoreList = new Section("Biblepay Store", 4, Sys, this);
            string sql = "Select * from Products where Pics is not null and inwallet=0 and networkid='" +  VerifyNetworkID(Sys.NetworkID)
                + "' order by Added";
            System.Data.DataTable dt = Sys._data.GetDataTable2(sql);
            if (dt.Rows.Count > 0)
            {
                for (int iRow = 0; iRow <= dt.Rows.Count - 1; iRow++)
                {
                    string sItemGuid = dt.Rows[iRow]["id"].ToString();
                    string sURL = FirstURL(dt.Rows[iRow]["Pics"].ToString());
                    string sDesc = "<TABLE><TR><TD>Title:</td><td>" + dt.Rows[iRow]["title"].ToString() + "</td></tr>";
                    sDesc += "<TR><TD>Description:</td><td>" + dt.Rows[iRow]["product_details"].ToString() + "</td></tr>";
                    double dPrice = Convert.ToDouble(dt.Rows[iRow]["price"].ToString());
                    double dBBP =  PriceInBBP(dPrice / 100);
                    sDesc += "<TR><TD>Price:</td><td>" + dBBP.ToString() + " BBP</td></tr>";
                    Edit geBtnBuy = new Edit("Biblepay Store", Edit.GEType.Button, "btnBuy", "Buy", Sys);
                    geBtnBuy.Id = sItemGuid.ToString();
                    geBtnBuy.Name = sItemGuid;
                    geBtnBuy.ParentGuid = sItemGuid;
                    geBtnBuy.sAltGuid = sItemGuid;
                    WebReply wrBtn = geBtnBuy.Render(this);
                    string sBtn = wrBtn.Packages[0].HTML;
                    sDesc += "<TR><td>" + sBtn + "</td></tr>";
                    sDesc += "<tr><td>Image:</td><td><img align=center width='250' height='250' src='" + sURL + "' /></table>";
                    Edit gePics = new Edit("Biblepay Store", "lbl" + iRow.ToString(), Sys);
                    gePics.Type = Edit.GEType.Label;
                    gePics.CaptionText = sDesc;
                    StoreList.AddControl(gePics);
                }
            }

            // Piggyback the orders list here (after the products)
            WebReply wr = StoreList.Render(this, true);
            WebReply wrOrders = OrdersList();
            wr.AddWebPackages(wrOrders.Packages);
            return wr;
        }

        public WebReply btnNewsUpload_Click()
        {
            return AddNewsPicture(); // This allows the user to see what they uploaded below the TextArea
        }

        public WebReply NewsPics()
        {
            Section NewsPics = new Section("News - Pictures", 4, Sys, this);
            // Show thumbnails of every picture attached by myself
            string sql = "Select * from Picture where addedby = '" +  GuidOnly(Sys.UserGuid)
                + "' and parenttype='News'";
            System.Data.DataTable dt = Sys._data.GetDataTable2(sql);
            string sRow = "";
            if (dt.Rows.Count > 0)
            {
                for (int iRow = 0; iRow <= dt.Rows.Count - 1; iRow++)
                {
                    string sPicId = dt.Rows[iRow]["id"].ToString();
                    string sURL = dt.Rows[iRow]["URL"].ToString();
                    sRow = "<span style='white-space:nowrap;' >" + sURL + "</span> " + "<img width='250' height='250' src='" + sURL + "' />";
                    Edit gePics = new Edit("News - Pictures", "lbl" + iRow.ToString(), Sys);
                    gePics.Type = Edit.GEType.Label;
                    gePics.CaptionText = sRow;
                    NewsPics.AddControl(gePics);
                }
            }

            return NewsPics.Render(this, false);
        }

        public WebReply AddNewsPicture()
        {
            Section anp = new Section("News Pictures", 2, Sys, this);
            Edit ctlUpload = new Edit("News Pictures", Edit.GEType.UploadControl2, "btnNewsUpload", "Add Picture", Sys);
            ctlUpload.Id = Guid.NewGuid().ToString();
            ctlUpload.ParentGuid = ctlUpload.Id;
            ctlUpload.ParentType = "News";
            anp.AddControl(ctlUpload);
            WebReply wrAnp = anp.Render(this, true);
            WebReply wrPics = NewsPics();
            wrAnp.AddWebPackages(wrPics.Packages);
            return wrAnp;
        }

        public WebReply btnAssociate_Click()
        {
            Bitnet.Client.BitnetClient bc = Sys.InitRPC(Sys.NetworkID);
            Dialog d = new Dialog(Sys);
            WebReply wr;
            string sPassword = Sys.GetObjectValue("Unbanked", "Password");
            string sEmail = Sys.GetObjectValue("Unbanked", "Email");
            string sAddress = Sys.GetObjectValue("Unbanked", "Address");
            string sResponse = "";
            if (sAddress == "" || sEmail == "" || sPassword == "")
            {
                sResponse = "Receiving address, E-Mail and Password must be populated.";
            }
            if (sResponse == "")
            {
                //specify boincemail, boincpassword.  Optionally specify force=true/false.
                object[] oParams = new object[5];
                oParams[0] = "associate";
                oParams[1] = sEmail;
                oParams[2] = sPassword;
                oParams[3] = "true";
                oParams[4] = sAddress;
                Bitnet.Client.BitnetClient bc2 =  GetBitNet(Sys.NetworkID);
                 GetWLA(Sys.NetworkID, 30);
                dynamic oOut = bc2.InvokeMethod("exec", oParams);
                 GetWL(Sys.NetworkID);
                sResponse = oOut["result"]["Results"].ToString();
            }
            if (sResponse == "") sResponse = "Success!";
            wr = d.CreateDialog("dialog1","Associate Researcher CPID for the Unbanked v1.3", sResponse, 450, 450);
            return wr;
        }


        public WebReply Deposit()
        {
           
            Section d = new Section("Deposit", 1, Sys, this);
            AddLabel("Deposit", "l1", "<p>", d, "Welcome to the Biblepay Deposit Page! <br>   ");

            // Look for any inbound deposits

            string sql = "Select * from UZ where id='" + Sys.UserGuid.ToString() + "'";
            string sDepAddress =  mPD.GetScalarString2(sql, "ReceiveAddress");
            string sUserName = mPD.GetScalarString2(sql, "Username");
            if (sUserName=="")
            {
                return Leaderboard();
            }

            if (sDepAddress == "")
            {
                sDepAddress = Sys.GetNewDepositAddress();
                sql = "Update UZ set receiveaddress='" + sDepAddress + "' where id = '" + Sys.UserGuid.ToString() + "'";
                mPD.Exec2(sql, true, true);
                Log("New receiving address for " + sUserName + ": " + sDepAddress);
            }


            sql = "select * from deposit where added > getdate() - .5  and credited is null and amount is not null and address='" + sDepAddress + "'";
            string sAdded =  mPD.GetScalarString2(sql, "Added");

            string sDepNarr = "";
            if (sAdded != "")
            {
                string sTxId =  mPD.GetScalarString2(sql, "txid");
                string nAmt =  mPD.GetScalarString2(sql, "amount");

                sDepNarr = "<br><h3><font color=green>PENDING DEPOSIT : TXID " + sTxId + " Received " + sAdded.ToString()  + " Amount=" + nAmt.ToString()
                    + ".  <br>Please wait 4 blocks for confirmation - the deposit will be automatically credited to your account.<br></font>";
            }

            string sReq = "<br><font color=orange>Instructions:<br>Copy the deposit address found below to your BiblePay home wallet.  Send funds to this address.  "
                +" Wait a minimum of 4 confirms (or 1 with instantsend).  <br><p>Come back here and check to see for pending deposits - or check your BiblePay pool balance.  "
                +" <br> Once confirmed, it will be spendable on store items, "
                +" or available for withdrawal.<br></font><br>" + sDepNarr;

            AddLabel("Deposit", "l3", "<p>", d, sReq);
            Edit geDestination = new Edit("Deposit", "ReceiveAddress", Sys);
            geDestination.CaptionText = "Deposit Address:";
            geDestination.TextBoxStyle = "width:400px";
            geDestination.Width = "300px";
            if (sDepAddress == "") sDepAddress = "NA";

            Sys.SetObjectValue("Deposit", "ReceiveAddress", sDepAddress);
            geDestination.TextBoxAttribute = "readonly";
            geDestination.TextBoxStyle =  msReadOnly;
            geDestination.TextBoxValue = sDepAddress;
            geDestination.TextBoxStyle = "width:620px";
            d.AddControl(geDestination);
            
            return d.Render(this, true);
        }

        // Loop through our customer addresses for the last 10 blocks:
        public WebReply Faucet()
        {
            Section Faucet = new Section("Faucet", 1, Sys, this);
            AddLabel("Faucet", "l1", "<p>",Faucet, "Welcome to the Biblepay Faucet! <br>   ");
            double csAmt = GetDouble(USGDFramework.clsStaticHelper.GetConfig("faucet_amount"));
            AddLabel("Faucet", "l2", "<p>", Faucet, "Reward Amount:  "+ csAmt.ToString() + " BBP");
            string sReq = "<br><ul><font color=orange>Requirements:<li>Your CPID must be a member of team Biblepay in Rosetta@Home or World Community Grid<li>"
                +"You must have a CPID that has not been paid by the faucet previously<li>You must have more than 100 RAC in either Rosetta@Home or World Community Grid<li>You must not be in any biblepay superblock as a researcher"
                +"<li>Your IP must not have received a faucet reward in the past<li>(It may take up to 24 hours for your CPID and RAC to be inducted into the pool from our sancs)</ul></font>";
            sReq = "<br><ul><font color=orange>Requirements:<li>Please populate your Cell Phone and Carrier in Account Settings, and Verify your Cell Number<li>Your IP must not have received rewards in the past<li>Your cell phone number must not have been used in the past</ul></font>";

            AddLabel("Faucet", "l3", "<p>", Faucet, sReq);
            Edit geDestination = new Edit("Faucet", "Destination", Sys);
            geDestination.CaptionText = "Destination Address:";
            geDestination.TextBoxStyle = "width:300px";
            Faucet.AddControl(geDestination);
            Edit geBtnSend = new Edit("Faucet", Edit.GEType.Button, "btnFaucetSend", "Send", Sys);
            Faucet.AddControl(geBtnSend);
            return Faucet.Render(this, true);
        }

        public WebReply btnFaucetSend_Click()
        {
            Bitnet.Client.BitnetClient bc = Sys.InitRPC(Sys.NetworkID);
            double csAmt = GetDouble(USGDFramework.clsStaticHelper.GetConfig("faucet_amount"));
            Dialog d = new Dialog(Sys);
            WebReply wr;
            // Get count of use by IP
            string sIP = (HttpContext.Current.Request.UserHostAddress ?? "").ToString();
            string sAddress = Sys.GetObjectValue("Faucet", "Destination");
            string sCPID = Sys.GetObjectValue("Faucet", "CPID");
            string sql = "Select cellphone,CellPhoneVerified from Uz where id = '" + Sys.UserGuid.ToString() + "'";

            string sPhone = mPD.GetScalarString2(sql, "cellphone", false);
            double dVerified = mPD.GetScalarDouble2(sql, "CellPhoneVerified", false);
            if (dVerified == 0 || sPhone=="")
            {
                wr = d.CreateDialog("Withdrawal_Error1", "Withdrawal Error", "Sorry, your cell phone number has not been verified.  Please go to account settings and populate cell phone number and carrier, and click Send SMS Code.", 350, 250);
                return wr;
            }

            string sSql = "Select count(*) as ct From Faucet where network='" +  VerifyNetworkID(Sys.NetworkID)
                + "' and (address = '" +  PurifySQL(sAddress, 50)  + "' or IP='" + sIP + "')";
            double lIpHitCount =  GetScalarDouble2(sSql, "ct");
            double dRAC =  RetrieveCPIDRAC(sCPID, "team1") +  RetrieveCPIDRAC(sCPID, "team2");

            string sql2 = "Select count(*) as ct from Faucet where network='" + Sys.NetworkID + "' and cellphone = '" + sPhone + "'";
            double dPC =  GetScalarDouble2(sql2, "ct");
            if (dPC > 0)
            {
                wr = d.CreateDialog("Withdrawal_Error3", "Withdrawal Error", "Sorry, this cell phone has already received faucet rewards.", 250, 250);
                return wr;
            }
            sql = "select count(*) ct from uz where added > '2-11-2019' and id = '" + Sys.UserGuid.ToString() + "'";
            double dUC = GetScalarDouble2(sql, "ct");
            if (dUC == 0)
            {
                wr = d.CreateDialog("Withdrawal_Error4", "Withdrawal Error", "Sorry, this reward is for new users only.", 250, 250);
                return wr;
            }
            string sql22 = "select max(added) upd from RequestLog where added > getdate() - 2";
            string sLast2 =  mPD.GetScalarString2(sql22, "upd");
            double dDiff2 = Math.Abs(DateAndTime.DateDiff(DateInterval.Second, DateTime.Now, Convert.ToDateTime(sLast2)));
            if (dDiff2 < 120)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, Someone has withdrawn from the faucet in the last 119 seconds, please try again in two minutes.", 250, 250);
                return wr;
            }

            sql22 = "Select sum(amount) amt from RequestLog where userguid='" +  GuidOnly(Sys.UserGuid.ToString()) + "' and Notes='FAUCET'";
            double dAmt =  mPD.GetScalarDouble2(sql22, "amt");
            if (dAmt > 5 && false)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, Faucet has been abused.  Please locate a new faucet.", 250, 250);
                return wr;
            }

            sql22 = "Select EmailVerified from Uz where id='" +  GuidOnly(Sys.UserGuid.ToString()) + "'";
            double dV =  mPD.GetScalarDouble2(sql22, "EmailVerified");
            if (dV == 0 && false)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, E-Mail must be verified first before using the faucet.", 250, 250);
                return wr;
            }

            if (false && (lIpHitCount > 0 || sIP == ""))
            {
                wr = d.CreateDialog("Faucet_Error", "Faucet Error", "Sorry, IP Address has already used faucet within minimum withdrawal period.  Please wait 60 days to pull more from faucet.", 250, 250);
                return wr;
            }

            string sTXID = "";

            if (Sys.GetObjectValue("Faucet", "Destination").Length < 10)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, Invalid Destination Address", 250, 150);
                return wr;
            }
            int iHeight = Sys.GetTipHeight(Sys.NetworkID);
            // Validate Address
            bool bValid = Sys.ValidateBiblepayAddress(sAddress, Sys.NetworkID);
            if (!bValid || iHeight < 100)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, Invalid Destination Address (Code: 65002)", 150, 150);
                return wr;
            }

            string sDestination = Sys.GetObjectValue("Faucet", "Destination");
            try
            {
                string sReqLogId = Guid.NewGuid().ToString();
                string sql4 = "Insert into RequestLog (username,userguid,address,id,txid,amount,added,network,ip,notes) values ('"
                    +  PurifySQL(Sys.Username, 100)
                    + "','" +  GuidOnly(Sys.UserGuid)
                    + "','"
                    +  PurifySQL(sAddress, 120)
                    + "','" + sReqLogId + "','"
                    + "','" + (csAmt).ToString() + "',getdate(),'" +  VerifyNetworkID(this.Sys.NetworkID)
                    + "','" +  PurifySQL(sIP, 34)
                    + "','FAUCET')";
                Sys._data.Exec2(sql4);


                 InsTxLog(Sys.Username, Sys.UserGuid, Sys.NetworkID, iHeight, Guid.NewGuid().ToString(), csAmt, 0, 0, sDestination, "Faucet", "");
                sTXID =  A1(sReqLogId, sDestination, csAmt, Sys.NetworkID, sIP);
            }
            catch (Exception ex)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, an error occurred during the withdrawal (Code: 65003 [" + ex.Message + "]) - Biblepay Server rejected destination address.", 150, 200);
                return wr;
            }
           
            sql = "Insert into Faucet (id,height,transactionid,IP,amount,added,network,address,cellphone) values (newid(),'"
                + iHeight.ToString() + "','"
                + sTXID.Trim() + "','"
                + sIP + "','" + csAmt.ToString() + "',getdate(),'" +  VerifyNetworkID(Sys.NetworkID) + "','"
                +  PurifySQL(sAddress, 100)
                + "','" +  PurifySQL(sPhone, 100) + "')";
            Sys._data.Exec2(sql);
            //Send email to Jaap:
            string sBody = "Dear Airdrop, <br>BiblePay has transmitted " + csAmt.ToString() + " BBP to Cell " + sPhone
                + ".  <br><br>Thank you for using BiblePay!";
            string sEmail = "rob@biblepay.org";
            bool sent = Sys.SendEmail(sEmail, "New Faucet Cell", sBody, true, true);
           
            wr = d.CreateDialog("Success", "Success", "Successfully transmitted " + csAmt.ToString()
                    + " BiblePay to " + sAddress + ".  <p>Transaction ID: " + sTXID, 600, 200);
            return wr;
        }


        public WebReply NonBiblePayFaucet()
        {
            Section Faucet = new Section("Faucet", 1, Sys, this);
            AddLabel("Faucet", "l1", "<p>", Faucet, "Welcome to the Non-Biblepay Faucet! <br>   ");
            double csAmt = GetDouble(USGDFramework.clsStaticHelper.GetConfig("faucet_amount_non_bbp"));
            AddLabel("Faucet", "l2", "<p>", Faucet, "Reward Amount:  " + csAmt.ToString() + " BBP");
            string sReq = "<br><ul><font color=orange>Requirements:<li>"
                +"You must have a CPID that has not been paid by the faucet previously<li>"
                +"You must have more than 100 RAC in either Rosetta@Home or World Community Grid<li>You must not be in any biblepay superblock as a researcher"
                + "<li>Your IP must not have received a faucet reward in the past</ul></font>";
            AddLabel("Faucet", "l3", "<p>", Faucet, sReq);
            Edit geDestination = new Edit("Faucet", "Destination", Sys);
            geDestination.CaptionText = "Destination Address:";
            geDestination.TextBoxStyle = "width:300px";
            Faucet.AddControl(geDestination);
            Edit geCPID = new Edit("Faucet", "CPID", Sys);
            geCPID.CaptionText = "Enter your CPID:";
            geCPID.TextBoxStyle = "width:300px";
            Faucet.AddControl(geCPID);
            Edit geBtnSend = new Edit("Faucet", Edit.GEType.Button, "btnNonBiblePayFaucetSend", "Send", Sys);
            Faucet.AddControl(geBtnSend);
            return Faucet.Render(this, true);
        }

        public WebReply btnNonBiblePayFaucetSend_Click()
        {
            Bitnet.Client.BitnetClient bc = Sys.InitRPC(Sys.NetworkID);
            double csAmt = GetDouble(USGDFramework.clsStaticHelper.GetConfig("faucet_amount_non_bbp"));
            Dialog d = new Dialog(Sys);
            WebReply wr;
            // Get count of use by IP
            string sIP = (HttpContext.Current.Request.UserHostAddress ?? "").ToString();
            string sAddress = Sys.GetObjectValue("Faucet", "Destination");
            string sCPID = Sys.GetObjectValue("Faucet", "CPID");
            string sSql = "Select count(*) as ct From Faucet where network='" +  VerifyNetworkID(Sys.NetworkID)
                + "' and (address = '" +  PurifySQL(sAddress, 50)
                + "' or IP='" + sIP + "')";
            double lIpHitCount =  GetScalarDouble2(sSql, "ct");
            double dRAC =  RetrieveCPIDRAC(sCPID, "team1") +  RetrieveCPIDRAC(sCPID, "team2");

            string sql2 = "Select count(*) as ct from Faucet where network='" + Sys.NetworkID + "' and Cpid = '" + sCPID + "'";
            double dCPIDcount =  GetScalarDouble2(sql2, "ct");
            if (dCPIDcount > 0)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, CPID already received faucet rewards", 150, 150);
                return wr;
            }

            string sql3 = "select count(*) ct from superblocks where cpid='" + sCPID + "' and magnitude > 0 and added > getdate()-60";
            double dSupercount =  GetScalarDouble2(sql3, "ct");
            if (dSupercount > 0)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, CPID has received " + dSupercount.ToString() + " superblock rewards already.", 177, 177);
                return wr;
            }

            if (sCPID.Length != 32)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, CPID is invalid.", 150, 150);
                return wr;
            }

            dRAC =  GetWebRAC(sCPID);

            if (dRAC < 100)
            {
                wr = d.CreateDialog("Withdrawal_Error", 
                    "Withdrawal Error", "Sorry, CPID does not have enough RAC (Rac is currently reading at " 
                    + dRAC.ToString() + "). Please ensure your RAC > 100 in either Rosetta@Home or World Community Grid.", 250, 350);
                return wr;
            }

            string sql22 = "select max(added) upd from RequestLog where added > getdate() - 2";
            string sLast2 =  mPD.GetScalarString2(sql22, "upd");
            double dDiff2 = Math.Abs(DateAndTime.DateDiff(DateInterval.Second, DateTime.Now, Convert.ToDateTime(sLast2)));
            if (dDiff2 < 120)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, Someone has withdrawn from the faucet in the last 119 seconds, please try again in two minutes.", 250, 250);
                return wr;
            }

            sql22 = "Select sum(amount) amt from RequestLog where userguid='" +  GuidOnly(Sys.UserGuid.ToString()) + "' and Notes='FAUCET'";
            double dAmt =  mPD.GetScalarDouble2(sql22, "amt");
           
            sql22 = "Select EmailVerified from Uz where id='" +  GuidOnly(Sys.UserGuid.ToString()) + "'";
            double dV =  mPD.GetScalarDouble2(sql22, "EmailVerified");
            if (dV == 0 && false)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, E-Mail must be verified first before using the faucet.", 250, 250);
                return wr;
            }

            if (false && (lIpHitCount > 0 || sIP == ""))
            {
                wr = d.CreateDialog("Faucet_Error", "Faucet Error", "Sorry, IP Address has already used faucet within minimum withdrawal period.  Please wait 60 days to pull more from faucet.", 250, 250);
                return wr;
            }

            string sTXID = "";

            if (Sys.GetObjectValue("Faucet", "Destination").Length < 10)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, Invalid Destination Address", 150, 150);
                return wr;
            }
            int iHeight = Sys.GetTipHeight(Sys.NetworkID);
            // Validate Address
            bool bValid = Sys.ValidateBiblepayAddress(sAddress, Sys.NetworkID);
            if (!bValid || iHeight < 100)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, Invalid Destination Address (Code: 65002)", 150, 150);
                return wr;
            }

            string sDestination = Sys.GetObjectValue("Faucet", "Destination");
            try
            {
                string sReqLogId = Guid.NewGuid().ToString();
                string sql4 = "Insert into RequestLog (username,userguid,address,id,txid,amount,added,network,ip,notes) values ('"
                    +  PurifySQL(Sys.Username, 100)
                    + "','" +  GuidOnly(Sys.UserGuid)
                    + "','"
                    +  PurifySQL(sAddress, 120)
                    + "','" + sReqLogId + "','"
                    + "','" + (csAmt).ToString() + "',getdate(),'" +  VerifyNetworkID(this.Sys.NetworkID)
                    + "','" +  PurifySQL(sIP, 34)
                    + "','FAUCET')";
                Sys._data.Exec2(sql4);


                 InsTxLog(Sys.Username, Sys.UserGuid, Sys.NetworkID, iHeight, Guid.NewGuid().ToString(), csAmt, 0, 0, sDestination, "Faucet", "");
                sTXID =  USGDFramework.Shared.A1(sReqLogId, sDestination, csAmt, Sys.NetworkID, sIP);
            }
            catch (Exception ex)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, an error occurred during the withdrawal (Code: 65003 [" + ex.Message + "]) - Biblepay Server rejected destination address.", 150, 200);
                return wr;
            }

            string sql = "Insert into Faucet (id,height,transactionid,IP,amount,added,network,address,cpid) values (newid(),'"
                + iHeight.ToString() + "','"
                + sTXID.Trim() + "','"
                + sIP + "','" + csAmt.ToString() + "',getdate(),'" +  VerifyNetworkID(Sys.NetworkID) + "','"
                +  PurifySQL(sAddress, 100)
                + "','" +  PurifySQL(sCPID, 100) + "')";
            Sys._data.Exec2(sql);
            string sBody = "Dear Non-BiblePay Faucet Stakeholders, <br>BiblePay has transmitted " + csAmt.ToString() + " BBP to CPID " + sCPID + ".  <br><br>Thank you for using BiblePay!";
            string sEmail = "jaap@biblepay.org";
            bool sent = Sys.SendEmail(sEmail, "New Faucet CPID", sBody, true, true);
            wr = d.CreateDialog("Success", "Success", "Successfully transmitted " + csAmt.ToString()
                    + " BiblePay to " + sAddress + ".  <p>Transaction ID: " + sTXID, 600, 200);
            return wr;
        }

        public WebReply Tip()
        {
            Section Tip = new Section("Tip", 1, Sys, this);
            Edit geAmount = new Edit("Tip", "Amount", Sys);
            geAmount.CaptionText = "Please enter the desired tip amount:";
            Tip.AddControl(geAmount);
            string sUserId = Sys.GetObjectValue("Tip", "ToRecipient");

            string sql = "Select ID from Uz where username='" +  GuidOnly(sUserId) + "'";

            string sToGuid =  mPD.GetScalarString2(sql, "ID", false);
            
            Dialog d = new Dialog(Sys);

            if (sToGuid=="")
            {
                WebReply w = d.CreateDialog("Tip", "Tip Bot Error", "Sorry, the Tip Recipient does not exist.", 150, 150);
                return w;
            }
            Sys.SetObjectValue("Tip", "ToGuid", sToGuid);
            Sys.SetObjectValue("Tip","ToRecipient",sUserId);

            string sCap = "Welcome to Biblepay's Tip Bot.  <br>You are tipping: " + sUserId + ".<br>";
            AddLabel("Tip", "lbl2", "<p>", Tip, sCap);

            Edit geBtnSend = new Edit("Tip", Edit.GEType.Button, "btnTipSend", "Send", Sys);
            Tip.AddControl(geBtnSend);
            return Tip.Render(this, true);
        }


        public WebReply btnTipSend_Click()
        {
            double oldBalance = 0;
            double dImmature = 0;
             GetUserBalances(this.Sys.NetworkID, this.Sys.UserGuid.ToString(), ref oldBalance, ref dImmature);
            double dAmount =  GetDouble(Sys.GetObjectValue("Tip", "Amount"));
            dAmount = Math.Round(dAmount, 2);
            Dialog d = new Dialog(Sys);
            WebReply wr;
            if (dAmount > (oldBalance - dImmature))
            {
                wr = d.CreateDialog("Tip1", "Tip Bot Error", "Sorry, Tip amount of " + dAmount.ToString()
                    + " exceeds your balance of " + (oldBalance - dImmature).ToString() + ".", 150, 150);
                return wr;
            }
            if (dAmount < .01)
            {
                wr = d.CreateDialog("Tip1", "Tip Error", "Sorry, Tip amount is negative or almost zero.", 150, 150);
                return wr;
            }
            string sToGuid = Sys.GetObjectValue("Tip", "ToGuid");
            if (sToGuid == "")
            {
                wr = d.CreateDialog("Withdrawal_Error", "Tip Error", "Sorry, Security Error (60003).", 150, 150);
                return wr;
            }

            // Transfer the money by Removing the money:
            string sGuid = Guid.NewGuid().ToString();
            string sPayer = Sys.UserGuid.ToString();
            double dBalance =  AwardBounty(sGuid, sPayer, -1 * dAmount, "Tip", 0,
                                          sGuid, "main", sGuid, false);
            // Transfer the money to the Clicker
              AwardBounty(sGuid, sToGuid, dAmount, "Tip", 0, sGuid, "main", sGuid, false);
            // Done transferring the money

            string sRecipient = Sys.GetObjectValue("Tip", "ToRecipient");
            string sNarr = "Thank you for using Biblepay Tip Bot.  <br>Successfully delivered Tip for " + dAmount.ToString() + "BBP to " + sRecipient + ". <BR>";

            wr = d.CreateDialog("Success", "Biblepay Tip Bot", sNarr, 600, 200);
            return wr;
        }

        double DaysOld(string sDate)
        {
            TimeSpan tsDelta = DateTime.Now.Subtract(Convert.ToDateTime(sDate));
            return tsDelta.Days;
        }

        public WebReply Withdraw()
        {
            string sql = "Select * from Uz with (nolock) where id='" +  GuidOnly(Sys.UserGuid)
                + "' and deleted=0";
            System.Data.DataTable dt = Sys._data.GetDataTable2(sql);
            if (dt.Rows.Count > 0)
            {
                Sys.SetObjectValue("Withdraw", "Destination", "" + dt.Rows[0]["WithdrawalAddress"].ToString());
            }
            updBalance();
            Section Withdraw = new Section("Withdraw", 1, Sys, this);
            Edit geAvailableBalance = new Edit("Withdraw", "Available", Sys, "Available Balance:");
            Withdraw.AddControl(geAvailableBalance);

            Edit geImmature = new Edit("Withdraw", "Immature", Sys, "Immature Balance:");
            Withdraw.AddControl(geImmature);

            Edit geAmount = new Edit("Withdraw", "Amount", Sys);
            geAmount.CaptionText = "Amount to Withdraw:";
            Withdraw.AddControl(geAmount);

            Edit geDestination = new Edit("Withdraw", "Destination", Sys);
            geDestination.CaptionText = "Destination Address:";
            geDestination.TextBoxStyle = "width:300px";

            Withdraw.AddControl(geDestination);
            Edit geBtnSend = new Edit("Withdraw", Edit.GEType.Button, "btnWithdrawSend", "Send", Sys);
            Withdraw.AddControl(geBtnSend);
            return Withdraw.Render(this, true);
        }

        private void updBalance()
        {
            //Compute immature balance
            double dBal1 = 0;
            double dImmature = 0;
             GetUserBalances(Sys.NetworkID,Sys.UserGuid,ref dBal1, ref dImmature);
            double dBalAvailable  = dBal1 - dImmature;
            Sys.SetObjectValue("Withdraw", "Available", dBalAvailable.ToString());
            Sys.SetObjectValue("Withdraw", "Immature", dImmature.ToString());
            Sys.SetObjectValue("Withdraw", "Balance", dBal1.ToString());
        }

        
        public WebReply btnWithdrawSend_Click()
        {
            // Send funds
            string sql = "Select * From Uz where id = '" +  GuidOnly(Sys.UserGuid) + "'";
            DataTable dtUser = Sys._data.GetDataTable2(sql);
            Bitnet.Client.BitnetClient bc = Sys.InitRPC(Sys.NetworkID);
            if (dtUser.Rows.Count < 1) return Leaderboard();

            double daysOld = DaysOld(dtUser.Rows[0]["Added"].ToString());

            double oldBalance = 0;
            double dImmature = 0;
             GetUserBalances(this.Sys.NetworkID,this.Sys.UserGuid.ToString(), ref oldBalance, ref dImmature);
            double csAmt =  GetDouble(Sys.GetObjectValue("Withdraw", "Amount"));
            csAmt = Math.Round(csAmt, 4);
            Dialog d = new Dialog(Sys);
            WebReply wr;
            if (csAmt > (oldBalance - dImmature))
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, Withdrawal amount of " + csAmt.ToString()
                    + " exceeds your balance of " + (oldBalance - dImmature).ToString() + ".", 150, 150);
                return wr;
            }
            if (daysOld < 5)
            {
                wr = d.CreateDialog("WithdrawalError2", "Withdrawal Error", "Sorry, account is too new to withdraw funds.", 150, 150);
                return wr;
            }
            if (csAmt < .01)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, Withdrawal amount is negative or almost zero.", 150, 150);
                return wr;
            }
            if (csAmt < 4)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, Withdrawal amount must be above 4 bbp.", 150, 150);
                return wr;
            }
            if (csAmt > 40000)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, Withdrawal amount must be below 40000 bbp.", 150, 150);
                return wr;
            }
            if (dtUser.Rows.Count < 1)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, Security Error (60003).", 150, 150);
                return wr;
            }

            if (Sys.GetObjectValue("Withdraw", "Destination").Length < 10)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, Invalid Destination Address", 150, 150);
                return wr;
            }
            // Validate Address
            string sAddress = Sys.GetObjectValue("Withdraw", "Destination");
            bool bValid = Sys.ValidateBiblepayAddress(sAddress, Sys.NetworkID);
            if (!bValid)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, Invalid Destination Address (Code: 65002)", 150, 150);
                return wr;
            }
            int iHeight = Sys.GetTipHeight(Sys.NetworkID);
            if (iHeight < 10)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, Invalid Chain Height (Code: 65003)", 150, 150);
                return wr;
            }
            string sEmail = dtUser.Rows[0]["Email"].ToString();
            double dVerified = GetDouble(dtUser.Rows[0]["EmailVerified"]);
            if (dVerified != 1)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, E-mail must be verified before making a withdrawal.  <font color=red>Please click Account | Account Settings | Verify E-Mail.  (Code: 65005)", 450, 350);
                return wr;
            }

            string sDestination = Sys.GetObjectValue("Withdraw", "Destination");
            double newBalance = oldBalance - csAmt;
            string sTXGuid = Guid.NewGuid().ToString();
            string sRLGuid = "";
            try
            {
                string sIP = (HttpContext.Current.Request.UserHostAddress ?? "").ToString();
                string sName = (this.Sys.Username ?? "").ToString();
                string sGuid = (this.Sys.UserGuid ?? "").ToString();
                sRLGuid = Guid.NewGuid().ToString();
                string sql2 = "Insert into RequestLog (username,userguid,address,id,txid,amount,added,network,ip) values ('"
                    +  PurifySQL(sName, 100)
                    + "','" +  GuidOnly(sGuid)
                    + "','"
                    +  PurifySQL( sAddress,120)
                    + "','" + sRLGuid + "','"
                    + "','" + (csAmt).ToString() + "',getdate(),'" +  VerifyNetworkID(this.Sys.NetworkID)
                    + "','" +  PurifySQL(sIP,34)
                    + "')";
                Sys._data.Exec2(sql2);

            }
            catch (Exception ex)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, an error occurred during the withdrawal (Code: 65003 [" + ex.Message + "]) - Biblepay Server rejected destination address.", 150, 200);
                return wr;
            }

            string sLink = HttpContext.Current.Request.Url.ToString();
            sLink = sLink.Substring(0, sLink.Length - 10);
            sLink += "/Action.aspx?action=continue_action&id=" + sRLGuid.ToString();
            string sBody = "Dear " + Sys.Username.ToUpper() 
                + ",<br><br>BiblePay Pool Withdrawal Request:<br><br>Please click the link below to complete the withdrawal request. "
                + "<br><br><br><br><a href='" 
                + sLink + "'>Withdraw Funds</a><br><br>Thank you for using BiblePay.<br><br>Best Regards,<br>BiblePay Support";
            bool sent = Sys.SendEmail(sEmail, "BiblePay Funds Withdrawal (BBP)", sBody, true);
            string sSuffix = "";
            if (!sent)
            {
                sSuffix = "There was a problem sending the withdrawal email.  Please contact support at contact@biblepay.org";
            }
            else
            {
                sSuffix = "Please check your e-mail to complete the transaction of " + csAmt.ToString()
                    + " BiblePay to " + Sys.GetObjectValue("Withdraw", "Destination");
            }
            wr = d.CreateDialog("Success", "Success", "Withdrawal request processed.  "+ sSuffix  + ".  <p>", 600, 200);
            return wr;
        }
        
        public WebReply WorkerList()
        {
            SQLDetective s = Sys.GetSectionSQL("Worker List", "Miners", string.Empty);
            s.WhereClause = " userid='" + "" + Sys.UserGuid.ToString() + "'";
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
            w.bShowRowSelect = false;
            w.bShowRowTrash = true;
            WebReply wr = w.GetWebList(sql, "Worker List", "Worker List", "", "Miners", this, false);
            WebReplyPackage wrp1 = wr.Packages[0];
            wrp1.ClearScreen = true;
            wr.Packages[0] = wrp1;
            return wr;
        }

        public WebReply WorkerList_OrderByClick()
        {
            return WorkerList();
        }

        public WebReply WorkerList_Remove_Click()
        {
            string sID = Sys.LastWebObject.guid.ToString();
            string sql = "Delete from Miners where ID='" +  GuidOnly(sID)
                + "'";
            try
            {
                if (sID.Length < 34) throw new Exception("Unable to delete worker: Record pointer error.");

                Sys._data.Exec2(sql);
            }
            catch(Exception ex)
            {
                Dialog d = new Dialog(Sys);
                WebReply wr = d.CreateDialog("dlgerr", "Error while Deleting Miner", "(Error 60003) "+ ex.Message , 240, 200);
                return wr;
            }
            return WorkerList();
        }

        public WebReply OrphanAuction()
        {
            SQLDetective s = Sys.GetSectionSQL("Orphan Fundraisers", "OrphanAuction", string.Empty);
            if (ParentID == "") ParentID = Sys.Organization.ToString();
            s.WhereClause = "";
            s.OrderBy = " updated desc";
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
            w.sGrandTotalColumn = "BBPAmount;BTCRaised;Amount;BTCAmount";
            WebReply wr = w.GetWebList(sql, "Orphan Fundraisers", "Orphan Fundraisers", "", "OrphanAuction", this, false);
            return wr;
        }


        public WebReply OrphanList()
        {
            SQLDetective s = Sys.GetSectionSQL("Orphan List", "Orphans", string.Empty);
            if (ParentID == "") ParentID = Sys.Organization.ToString();
            s.WhereClause = "";
            s.OrderBy = " updated";
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
            w.URLDefaultValue = "View Biography";
            w.AddContextMenuitem("Write", "Write", "Write");
            w.AddContextMenuitem("View", "View", "View");
            WebReply wr = w.GetWebList(sql, "Sponsored Orphan List", "Orphans", "", "Orphans", this, false);
            WebReplyPackage wrp1 = wr.Packages[0];
            wrp1.ClearScreen = true;
            wr.Packages[0] = wrp1;
            return wr;
        }

        public WebReply OrphanList_RowClick()
        {
            string sId = Sys.LastWebObject.guid.ToString();
            string sql = "Select URL,Name from Orphans where id = '" +  GuidOnly( sId)
                + "'";
            string sURL = Sys._data.GetScalarString2(sql, "URL");
            Section s = new Section("l1", 1, Sys, this);
            Dialog d = new Dialog(Sys);
            string sCSS =  msReadOnly;
            string sBody = "<iframe style='background-color:gold;' width=100% height=700px  src='" + sURL + "'>";
            string sNarr = Sys._data.GetScalarString2(sql, "Name");
            string sNarr2 = sNarr.Replace(" ", "_");
            WebReply wr = d.CreateDialog(sNarr2, "View Orphan Biography", sBody, 1000, 1100);
            return wr;
        }

        public WebReply AdoptionList()
        {
            SQLDetective s = Sys.GetSectionSQL("Adoption List", "Orphans", string.Empty);
            if (ParentID == "") ParentID = Sys.Organization.ToString();
            s.WhereClause = " Id Not In (Select AdoptedOrphanId from Uz where AdoptedOrphanId is not null) ";
            s.OrderBy = " updated";
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
            w.URLDefaultValue = "View Biography";
            w.AddContextMenuitem("Adopt", "Adopt", "Adopt");
            w.AddContextMenuitem("View", "View", "View");
            WebReply wr = w.GetWebList(sql, "Adoption List", "Orphans", "", "Orphans", this, false);
            WebReplyPackage wrp1 = wr.Packages[0];
            wrp1.ClearScreen = true;
            wr.Packages[0] = wrp1;
            return wr;
        }

        public WebReply AdoptionList_RowClick()
        {
            string sId = Sys.LastWebObject.guid.ToString();
            string sql = "Select URL,Name from Orphans where id = '" +  GuidOnly(sId)
                + "'";
            string sURL = Sys._data.GetScalarString2(sql, "URL");
            Section s = new Section("l1", 1, Sys, this);
            Dialog d = new Dialog(Sys);
            string sCSS =  msReadOnly;
            string sBody = "<iframe style='background-color:gold;' width=100% height=700px  src='" + sURL + "'>";
            string sNarr = Sys._data.GetScalarString2(sql, "Name");
            string sNarr2 = sNarr.Replace(" ", "_");
            WebReply wr = d.CreateDialog(sNarr2, "View Orphan Biography", sBody, 1000, 1100);
            return wr;
        }

        public WebReply WriteLetter_ContextMenu_Write()
        {
            return OrphanList_ContextMenu_Write();
        }

        public WebReply WriteLetter_RowClick()
        {
            return OrphanList_ContextMenu_Write();
        }

        public WebReply WriteLetter()
        {
            //First check to see if user has adopted a child
            string sql2 = "Select AdoptedOrphanId from Uz where id='" + Sys.UserGuid.ToString() + "'";
            string sOrphanId = Sys._data.GetScalarString2(sql2, "AdoptedOrphanID", false);
            if (sOrphanId.Length > 1)
            {
                Sys.LastWebObject.guid = sOrphanId;
                return WriteLetter_ContextMenu_Write();
            }
            SQLDetective s = Sys.GetSectionSQL("Write List", "Orphans", string.Empty);
            if (ParentID == "") ParentID = Sys.Organization.ToString();
            s.WhereClause = " needwritten=1 and orphanid not  in (select orphanid from letters where letters.orphanid=orphanid and added > getdate()-60) ";
            s.WhereClause += " and Id Not In (Select AdoptedOrphanId from Uz where AdoptedOrphanId is not null)";
            s.OrderBy = " updated";
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
            w.URLDefaultValue = "Write Letter";
            w.AddContextMenuitem("View", "View", "View");
            w.AddContextMenuitem("Write", "Write", "Write");
            WebReply wr = w.GetWebList(sql, "Orphans Who Need Written To", "Orphans", "", "Orphans", this, false);
            WebReplyPackage wrp1 = wr.Packages[0];
            wrp1.ClearScreen = true;
            wr.Packages[0] = wrp1;
            return wr;
        }
       
        public WebReply WriteLetter_ContextMenu_View()
        {
            string sId = Sys.LastWebObject.guid.ToString();
            string sql = "Select URL,Name from Orphans where id = '" +  GuidOnly(sId)      + "'";
            string sURL = Sys._data.GetScalarString2(sql, "URL");
            Section s = new Section("l1", 1, Sys, this);
            Dialog d = new Dialog(Sys);
            string sCSS =  msReadOnly;
            string sBody = "<iframe style='background-color:gold;' width=100% height=700px  src='" + sURL + "'>";
            string sNarr = Sys._data.GetScalarString2(sql, "Name");
            string sNarr2 = sNarr.Replace(" ", "_");
            WebReply wr = d.CreateDialog(sNarr2, "View Orphan Biography", sBody, 1000, 1100);
            return wr;
        }

        public WebReply OrphanList_ContextMenu_View()
        {
            return WriteLetter_ContextMenu_View();
        }

        public WebReply AdoptionList_ContextMenu_View()
        {
            return WriteLetter_ContextMenu_View();
        }

        public WebReply AdoptionList_ContextMenu_Adopt()
        {
            string sId = Sys.LastWebObject.guid.ToString();
            string sql = "Select URL,Name from Orphans where id = '" +  GuidOnly(sId)  + "'";
            string sURL = Sys._data.GetScalarString2(sql, "URL", false);
            Section s = new Section("l1", 1, Sys, this);
            Dialog d = new Dialog(Sys);
            string sCSS =  msReadOnly;
            string sName = Sys._data.GetScalarString2(sql, "Name");
            sql = "Select AdoptedOrphanID from Uz where id='" + Sys.UserGuid.ToString() + "'";
            string sCurrentID = Sys._data.GetScalarString2(sql, "AdoptedOrphanID");
            string sNarr = "Are you sure you would like to adopt " + sName + "?  This means you will be responsible for writing letters to the child at least once per month.  The child will start a personal relationship with you and will write letters back to you. ";
            if (sCurrentID.Length > 1)
            {
                sNarr = "Sorry, you already have an adopted child.  The only way to adopt a new child is to go to account settings and abandon your relationship with the current child. ";
                WebReply wr2 = d.CreateDialog("AdoptionListClick", "Unable to Adopt a New Child", sNarr, 300, 300);
                return wr2;
            }
            string sID = Sys.LastWebObject.guid.ToString();
            Sys.SetObjectValue("Adoption", "ChildID", sID);
            WebReply wr = d.CreateYesNoDialog("AdoptionList","adopt1", "AdoptYes", "AdoptNo", "BiblePay Adoption System", sNarr, this);
            return wr;
        }

        public WebReply AdoptYes_Click()
        {
            Dialog e = new Dialog(Sys);
            string sID = Sys.GetObjectValue("Adoption", "ChildID");
            // at this point we must assignt the orphan guid to the user account
            string sql = "Update uz set adoptedorphanid = '" + sID + "',letterdeadline=getdate()+31,adoptiondate=getdate() where id='" + Sys.UserGuid.ToString() + "'";
            Sys._data.Exec2(sql);
            Sys.SetObjectValue("Adoption", "ChildID", "");
            WebReply wr2 = e.CreateDialog("Admin1", "Thank You!", "You have adopted a child! Please see your account settings for the Child's Name and adoption status.  ",
                200, 300);
            return wr2;
        }

        public WebReply AdoptNo_Click()
        {
            return AdoptionList();
        }

        public WebReply AbandonYes_Click()
        {
            Dialog e = new Dialog(Sys);
            string sql = "Update Uz Set AdoptedOrphanId = null,adoptiondate=null,letterdeadline=null where id = '" + Sys.UserGuid.ToString() + "'";
            Sys._data.Exec2(sql);

            WebReply wr2 = e.CreateDialog("Admin1", "Processed", "You have abandoned the letter writing responsibility for this child.",
                200, 300);
            // Clear the flag
            WebReply wrAccount = AccountEdit();
            wr2.AddWebPackages(wrAccount.Packages);

            return wr2;
        }

        public WebReply AbandonNo_Click()
        {
            return AccountEdit();
        }


        public WebReply OrphanList_ContextMenu_Write()
        {
            // if they wrote too many...
            string sql = "select count(*) ct from letters where added > getdate()-30 and body is not null and approved=0 and userid='" +  GuidOnly(Sys.UserGuid) + "'";
            double ct =  GetScalarDouble2(sql, "ct");
            if (ct >= 7)
            {
                Dialog d = new Dialog(Sys);
                WebReply wr = d.CreateDialog("dia1", "Outbound Letter Limit Exceeded","Sorry, please wait until next month to write more letters.", 1000, 1100);
                return wr;
            }

            string sOrphanID = Sys.LastWebObject.guid.ToString();
            sql = "Select OrphanID,Name from Orphans where id = '" +  GuidOnly(sOrphanID) + "'";
            Sys.SetObjectValue("Write", "Body", "");
            string sLastOrphanID = Sys._data.GetScalarString2(sql, "OrphanID", false);
            string sOrphanName = Sys._data.GetScalarString2(sql, "Name", false);
            // New Letter Guid
            string sLetterGuid = Guid.NewGuid().ToString();
            Sys.SetObjectValue("Write", "LetterGuid", sLetterGuid);
            string sql1 = "Insert into Letters (id) values ('" +  GuidOnly(sLetterGuid) + "')";
            Sys._data.Exec2(sql1);
            Sys.SetObjectValue("Write", "Name", sOrphanName);
            Sys.SetObjectValue("Write", "Username", Sys.Username);
            Sys.SetObjectValue("Write", "OrphanID", sLastOrphanID);
            return WriteToOrphan();
        }

        public WebReply MyOutboundLetters()
        {
            SQLDetective s = Sys.GetSectionSQL("My Outgoing Letters", "Letters", string.Empty);
            if (ParentID == "") ParentID = Sys.Organization.ToString();
            s.WhereClause = " len(body) > 1 and Added > getdate()-90 and (userid = '" + Sys.UserGuid.ToString() + "')";
            s.OrderBy = " added";
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
            w.AddContextMenuitem("Upvote", "Upvote", "Upvote");
            w.AddContextMenuitem("Downvote", "Downvote", "Downvote");
            w.OptionalHeaderComments = "<small><font color=lime>(Right Click to Vote on a Letter)</font></small>";
            WebReply wr = w.GetWebList(sql, "Outgoing Letters", "Outgoing Letters", "", "Letters", this, false);
            WebReplyPackage wrp1 = wr.Packages[0];
            // Add to breadcrumb trail
            Sys.AddBreadcrumb("My Outbound Letters", "BiblePayPool2018.Home", "MyOutboundLetters", true);
            wrp1.ClearScreen = true;
            wr.Packages[0] = wrp1;
            return wr;
        }

        public WebReply OrphanLetters()
        {
            SQLDetective s = Sys.GetSectionSQL("Orphan Letters", "Letters", string.Empty);
            if (ParentID == "") ParentID = Sys.Organization.ToString();
            s.WhereClause = " len(body) > 1 and Added > getdate()-90 and isnull(approved,0)=0 and isnull(sent,0) = 0";
            s.OrderBy = " added";
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
            w.AddContextMenuitem("Upvote", "Upvote", "Upvote");
            w.AddContextMenuitem("Downvote", "Downvote", "Downvote");
            w.OptionalHeaderComments = "<small><font color=lime>(Right Click to Vote on a Letter)</font></small>";
            WebReply wr = w.GetWebList(sql, "Outgoing Letters", "Outgoing Letters", "", "Letters", this,  false);
            WebReplyPackage wrp1 = wr.Packages[0];
            // Add to breadcrumb trail
            Sys.AddBreadcrumb("Letters", "BiblePayPool2018.Home", "OrphanLetters", true);
            wrp1.ClearScreen = true;
            wr.Packages[0] = wrp1;
            return wr;
        }

        public WebReply OrphanLetters_ContextMenu_Downvote()
        {
            return Vote_Click("downvote");
        }

        public WebReply OrphanLetters_ContextMenu_Upvote()
        {
            return Vote_Click("upvote");
        }

        public WebReply MyOutboundLetters_RowClick()
        {
            return OrphanLetters_RowClick();
        }

        public WebReply OrphanLetters_RowClick()
        {
            string sOrphanID = Sys.LastWebObject.guid.ToString();
            string sql = "Select ID,Name,Username,OrphanID,Body from Letters where id = '" +  GuidOnly(sOrphanID) + "'";
            string sLastOrphanID = Sys._data.GetScalarString2(sql, "OrphanID");
            string sLetterGuid = Sys._data.GetScalarString2(sql, "id").ToString();
            string sOrphanName = Sys._data.GetScalarString2(sql, "name").ToString();
            string sUsername = Sys._data.GetScalarString2(sql, "username").ToString();
            string sBody = Sys._data.GetScalarString2(sql, "Body");
            sBody = sBody.Replace("`", "\"");
            Sys.SetObjectValue("Write", "Body", sBody);
            Sys.SetObjectValue("Write", "LetterGuid", sLetterGuid);
            Sys.SetObjectValue("Write", "Name", sOrphanName);
            Sys.SetObjectValue("Write", "Username", sUsername);
            Sys.SetObjectValue("Write", "OrphanID", sLastOrphanID);
            return WriteToOrphan();
        }

        public double GetDouble(object o)
        {
            if (o == null) return 0;
            if (o.ToString() == "") return 0;
            double d = Convert.ToDouble(o.ToString());
            return d;
        }
        
        public WebReply DifficultyHistory()
        {
          
            Section s1 = new Section("Difficulty History", 1, Sys, this);
            Edit gePFL = new Edit("Difficulty History", Edit.GEType.Image, "W1", "", Sys);
            Charts c = new Charts(Sys);

            string sFileName = c.GetChartOfDifficulty(Sys, System.Drawing.Color.Beige, "POW Difficulty", "powdifficulty");
            gePFL.URL = USGDFramework.clsStaticHelper.GetConfig("WebSite") + "SAN/" + sFileName;
            s1.AddControl(gePFL);

            return s1.Render(this, false);
        }


        public string GetFieldValueFromTableBasedOnPK(string sFieldName, string sWhereFieldName, string sTable, string sWhereValue)
        {
            string sql = "Select " + sFieldName + " from " + sTable + " where " + sWhereFieldName + "='" + sWhereValue + "'";
            DataTable dt = Sys._data.GetDataTable2(sql);
            if (dt.Rows.Count > 0)
            {
                string sValue = (dt.Rows[0][sFieldName] ?? String.Empty).ToString();
                return sValue;
            }
            return "";
        }

        public string GetFieldFromObjectBasedOnLastClick(string sFieldName, string sObjName)
        {
            string sId = "";
            try
            {
                sId = Sys.LastWebObject.guid.ToString();
            }
            catch (Exception) { }
            string sql = "Select " +  PurifySQL(sFieldName,1000)
                + " From " +  PurifySQL(sObjName,50)
                + " where id = '" +  GuidOnly(sId)
                + "'";
            DataTable dt = Sys._data.GetDataTable2(sql);
            if (dt.Rows.Count > 0)
            {
                string sValue = (dt.Rows[0][sFieldName] ?? String.Empty).ToString();
                return sValue;
            }
            return "NA";
        }

        public WebReply ClipDialog(string sClipText, string js, string sDialogBody)
        {
            Dialog d = new Dialog(Sys);
            string g = Guid.NewGuid().ToString();

            WebReply wr = d.CreateDialogWithMoreJavascript("dialog1" +g , sDialogBody, sClipText, 700, 210, js);
            WebReplyPackage wrp1 = wr.Packages[0];
            wrp1.ClearScreen = false;
            wr.Packages[0] = wrp1;
            return wr;
        }

        public WebReply ProposalList_ContextMenu_CopyProposalVoteAgainst()
        {
            string sValue = GetFieldFromObjectBasedOnLastClick("GObjectID", "Proposal");
            string sCmd = "gobject vote-many " + sValue + " funding no";
            string js = GetJSForClipCopy(sCmd);
            string sPre = (sValue.Length > 5) ? sCmd : "Unable to locate Proposal.";
            return ClipDialog(sPre, js, "Vote Against Proposal");
        }

        public WebReply ProposalList_ContextMenu_CopyProposalVote()
        {
            string sValue = GetFieldFromObjectBasedOnLastClick("GObjectID", "Proposal");
            string sCmd = "gobject vote-many " + sValue + " funding yes";
            string js = GetJSForClipCopy(sCmd);
            string sPre = (sValue.Length > 5) ?  sCmd : "Unable to locate Proposal.";
            return ClipDialog(sPre, js, "Vote for proposal");
        }
        
        public WebReply ProposalList_ContextMenu_CopyProposalID()
        {
            string sValue = GetFieldFromObjectBasedOnLastClick("GObjectID", "Proposal");
            string js = GetJSForClipCopy(sValue);
            string sPre = (sValue.Length > 5) ? "Copied Proposal " + sValue + " to clipboard." : "Unable to locate Proposal.";
            return ClipDialog(sPre, js, "Copy Proposal ID");
        }


        public void InductMissingProposals(string sNetworkID)
        {
            Random d = new Random();
            int i1 = d.Next(100);
            if (i1 > 11) return;

            //This code pulls in proposals entered by the core wallet that are not already in the pool (by gobject id)
            object[] oParams = new object[3];
            oParams[0] = "list";
            oParams[1] = "all";
            oParams[2] = "proposals";
            dynamic o =  GetGenericObject(Sys.NetworkID, "gobject", oParams);

            var oResult = o["result"];
            foreach (var oIT in oResult)
            {
                string sMyResult = oIT.ToString();
                
                var ds = oIT.First["DataString"];
                dynamic oProposal = Newtonsoft.Json.JsonConvert.DeserializeObject(ds.Value);
                string end_epoch = oProposal[0][1]["end_epoch"];
                string sProposalHash = oIT.First["Hash"];
                string sUnixStartTime = oIT.First["CreationTime"];
                string sCreated = UnixTimeStampToDateTime(GetDouble(sUnixStartTime)).ToShortDateString();
                TimeSpan tsDelta = DateTime.Now.Subtract(Convert.ToDateTime(sCreated));

                if (tsDelta.Days < 60)
                {
                    string sName = oProposal[0][1]["name"];
                    string sCollateralHash = oIT.First["CollateralHash"];
                    string sURL = oProposal[0][1]["url"];
                    string sAddress = oProposal[0][1]["payment_address"];
                    string sAmount = oProposal[0][1]["payment_amount"];
                    string sExpenseType = oProposal[0][1]["expensetype"];
                    sURL = sURL.Replace("'", "`");
                    sName = sName.Replace("'", "`");
                    if (sExpenseType == null) sExpenseType = "Charity";
                    if (true)
                    {

                        string sMasternodeCount = "400"; //Hack <- Fix
                        string sql = "insert into proposal (id,masternodecount,name,receiveaddress,amount,url,unixstarttime,unixendtime,preparetxid,"
                            + "added,updated,hex,submittxid,network,prepared,submitted,userid,username,preparetime,submittime,gobjectid,expensetype) values "
                            + "(newid(), '" + sMasternodeCount + "','" + sName + "','" + sAddress + "','" + sAmount + "','" + sURL + "','" + sUnixStartTime + "','" + end_epoch + "','" + sCollateralHash + "'"
                            + ",getdate(),getdate(),null,'" + sCollateralHash + "','" + sNetworkID + "','1','1',null,'biblepay_core','" + sCreated + "','" + sCreated + "','"
                            + sProposalHash + "','" + sExpenseType + "')";
                        string sql2 = "Select count(*) ct from proposal where preparetxid = '" + sCollateralHash + "'";
                        double dCount =  GetScalarDouble2(sql2, "ct");
                        if (dCount == 0)
                        {
                             mPD.Exec2(sql);
                        }
                    }
                }
           }
        }

        public void ProcessProposals(string sNetworkID)
        {

            InductMissingProposals(sNetworkID);

            string sql = "Select * from Proposal where paidtime is null and network='" +  VerifyNetworkID(Sys.NetworkID) + "'";
            DataTable dt = Sys._data.GetDataTable2(sql);
            for (int ii = 0; ii < dt.Rows.Count; ii++)
            {
                string sURL = (dt.Rows[ii]["URL"] ?? String.Empty).ToString();
                string sTxId = (dt.Rows[ii]["SubmitTxId"]).ToString();
                string sPrepareTxId = (dt.Rows[ii]["PrepareTxId"]).ToString();
                double dStartStamp = Convert.ToDouble((dt.Rows[ii]["UnixStartTime"]).ToString());
                string sHex = (dt.Rows[ii]["Hex"]).ToString();
                string gObjectID = (dt.Rows[ii]["gobjectid"]).ToString();
                double dAbsoluteYesCount = GetDouble(dt.Rows[ii]["AbsoluteYesCount"]);
                string sTriggerTxId = (dt.Rows[ii]["TriggerTxId"]).ToString();
                string sPaidTime = (dt.Rows[ii]["PaidTime"]).ToString();
                double dHeight = GetDouble((dt.Rows[ii]["height"].ToString()));
                string sId = dt.Rows[ii]["id"].ToString();
                double dAmt = GetDouble(dt.Rows[ii]["amount"]);
                double dMasterNodeCount = GetDouble((dt.Rows[ii]["MasternodeCount"].ToString()));
                double unixStartTimeStamp = GetDouble((dt.Rows[ii]["UnixStartTime"]));
                string sRecAddr = (dt.Rows[ii]["ReceiveAddress"]).ToString();
                string sName = (dt.Rows[ii]["Name"]).ToString();
                string sSubmitTxId = (dt.Rows[ii]["SubmitTxId"]).ToString();
                double dBudgetable = GetDouble(dt.Rows[ii]["budgetable"]);

                // if the item has no sPrepareTxId but it has been prepared into Hex, submit the Proposal Collateral to network
                if (sPrepareTxId == "" && sHex != "")
                {
                     GetWLA(Sys.NetworkID, 30);
                    // Submit the gobject to the network 
                    string sArgs = "0 1 " + unixStartTimeStamp.ToString() + " " + sHex;
                    string sCmd1 = "gobject prepare " + sArgs;
                    object[] oParams = new object[5];
                    oParams[0] = "prepare";
                    oParams[1] = "0";
                    oParams[2] = "1";
                    oParams[3] = unixStartTimeStamp.ToString();
                    oParams[4] = sHex;
                    string sPrepareTxid =  GetGenericInfo3(Sys.NetworkID, "gobject", oParams);
                    string sql4 = "Update Proposal Set PrepareTxId='" + 
                        sPrepareTxid + "',SubmitTime=null,Submitted=0,gobjectid=null,SubmitTxId=null,PrepareTime=getdate() where id = '" +  GuidOnly(sId)
                        + "'";
                    Sys._data.Exec2(sql4);
                     GetWL(Sys.NetworkID);
                }

                // if the object is unpaid and has a triggertxid, find out if its been paid
                if (sTriggerTxId.Length > 10 && dAbsoluteYesCount > 0 && sPaidTime == "" && dHeight > 0)
                {
                    string sSuperBlockTxId =  GetBlockTx(Sys.NetworkID, "showblock", (int)dHeight, 0);
                    // Now see if this rec address and amount is in the block
                    string sRawTx =  GetRawTransaction(Sys.NetworkID, sSuperBlockTxId);
                    if (sRawTx.Contains(sRecAddr) && sRawTx.Contains(dAmt.ToString()))
                    {
                        // This tx was paid in that superblock, update it
                        sql = "update proposal set PaidTime=getdate(),SuperBlockTxId='" + sSuperBlockTxId + "' where id = '" +  GuidOnly(sId)
                            + "'";
                        Sys._data.Exec2(sql);
                    }
                    else
                    {
                        // if height > dHeight, and it has not been paid, call the retrigger by removing the TriggerTxId 
                        int nHeight = Convert.ToInt32(Sys.GetTipHeight(Sys.NetworkID).ToString());
                        if (nHeight > (dHeight + 4))
                        {
                            sql = "update proposal set triggertxid=null,height=null,triggertime=null where id = '" +  GuidOnly(sId) + "'";
                            Sys._data.Exec2(sql);
                        }
                    }
                }
                // If this object has a supermajority vote, and its TriggerTxId is null, try to place it in the budget
                bool bSupermajority = dAbsoluteYesCount > (dMasterNodeCount * .12) && dAbsoluteYesCount > 0 && dMasterNodeCount > 0;
                if (sTriggerTxId == "" && bSupermajority && dBudgetable==1)
                {
                    // First find the next superblock for this network
                    double dNextSuperblock = Convert.ToDouble( GetGenericInfo(Sys.NetworkID, "getgovernanceinfo", "nextsuperblock", "result").ToString());
                    //if (dNextSuperblock > 0) dNextSuperblock += 25;
                    sql = "Select * from Proposal where Network='" +  VerifyNetworkID(Sys.NetworkID)
                        + "' and AbsoluteYesCount > 0 and TriggerTxID is null and gobjectid is not null and len(receiveaddress) > 1 and amount is not null and budgetable=1 ";
                    DataTable dtSuperblock = Sys._data.GetDataTable2(sql);
                    string sMyAddresses = "";
                    string sMyAmounts = "";
                    string sMyGobjects = "";
                    if (dtSuperblock.Rows.Count > 0)
                    {
                        for (int i = 0; i < dtSuperblock.Rows.Count; i++)
                        {
                            // Pull the receiving addresses, the amount, and the gobjectid
                            string sRecAddress = (dtSuperblock.Rows[i]["ReceiveAddress"]).ToString();
                            string sAmount = (dtSuperblock.Rows[i]["Amount"]).ToString();
                            string sGobjects = (dtSuperblock.Rows[i]["gobjectid"]).ToString();
                            sMyAddresses += sRecAddress + "|";
                            sMyAmounts += sAmount + "|";
                            sMyGobjects += sGobjects + "|";
                        }

                        if (sMyAddresses.Length > 1) sMyAddresses = sMyAddresses.Substring(0, sMyAddresses.Length - 1);
                        if (sMyAmounts.Length > 1) sMyAmounts = sMyAmounts.Substring(0, sMyAmounts.Length - 1);
                        if (sMyGobjects.Length > 1) sMyGobjects = sMyGobjects.Substring(0, sMyGobjects.Length - 1);

                        // Create the Trigger
                        object[] oParams = new object[6];
                        oParams[0] = "serialize-trigger";
                        oParams[1] = dNextSuperblock.ToString();
                        oParams[2] = sMyAddresses;
                        oParams[3] = sMyAmounts;
                        oParams[4] = sMyGobjects;
                        oParams[5] = "2";  // Trigger is Type 2 for a superblock Trigger
                        string sTest = oParams[0] + " " + oParams[1] + " " + oParams[2] + " " + oParams[3] + " " + " " + oParams[4] + " " + "2";
                        string sTriggerHex =  GetGenericInfo2(Sys.NetworkID, "gobject", oParams, "Hex");
                        // Now we need to submit the trigger as a gobject, and if we receive the Sanctuary txid, (must be from a masternode) we can update all these records again
                        Log("proposal trigger " + sTest);
                        try
                        {
                            if (sTriggerHex.Length > 10)
                            {
                                int unixTriggerTime = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                                object[] oParamTrigger = new object[5];
                                oParamTrigger[0] = "submit";
                                oParamTrigger[1] = "0";
                                oParamTrigger[2] = "1";
                                oParamTrigger[3] = unixTriggerTime.ToString();
                                oParamTrigger[4] = sTriggerHex;
                                string sTriggerArgs = "gobject submit 0 1 " + unixTriggerTime.ToString() + " " + sTriggerHex;
                                GetWLA(Sys.NetworkID, 60);
                                
                                string sTriggerTxid =  GetGenericInfo3(Sys.NetworkID, "gobject", oParamTrigger);
                                 GetWL(Sys.NetworkID);
                                for (int i = 0; i < dtSuperblock.Rows.Count; i++)
                                {
                                    string sProposalGuid = dtSuperblock.Rows[i]["id"].ToString();
                                    string sql3 = "Update proposal set Height='" 
                                        + dNextSuperblock.ToString() + "',TriggerTxId='" + sTriggerTxid + "',triggertime=getdate() where id = '"
                                        +  GuidOnly(sProposalGuid) + "'";
                                    Sys._data.Exec2(sql3);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                             Log(" err creating superblock " + ex.Message);
                        }
                    }
                }

                // if this object has been submitted, try to get its Sanctified votes:
                if (gObjectID.Length > 10)
                {
                    object[] oParams = new object[2];
                    oParams[0] = "get";
                    oParams[1] = gObjectID;
                     VoteType v =  GetGenericInfo4(Sys.NetworkID, "gobject", oParams);
                    if (v.Success && v.AbstainCount >= 0)
                    {
                        // Update the database with the Sanctuary vote results:
                        sql = "update proposal set absoluteYesCount='" + v.AbsoluteYesCount.ToString() + "',YesCount='" + v.YesCount.ToString() + "',NoCt='" 
                            + v.NoCount.ToString() + "',AbstainCount='" + v.AbstainCount.ToString() 
                            + "' where id = '" +  GuidOnly(dt.Rows[ii]["id"].ToString()) + "'";
                        Sys._data.Exec2(sql);
                    }
                }

                // if this proposal is not submitted, lets submit it and grab the SubmitTXID
                if (sSubmitTxId == ""  && sPrepareTxId.Length > 10)
                {
                    // Submit the gobject to the network - gobject submit parenthash revision time datahex collateraltxid
                     GetWLA(Sys.NetworkID, 20);
                    string sArgs = "0 1 " + dStartStamp.ToString() + " " + sHex + " " + sPrepareTxId;
                    string sCmd1 = "gobject submit " + sArgs;
                    object[] oParams = new object[6];
                    oParams[0] = "submit";
                    oParams[1] = "0";
                    oParams[2] = "1";
                    oParams[3] = dStartStamp.ToString();
                    oParams[4] = sHex;
                    oParams[5] = sPrepareTxId;
                    sSubmitTxId =  GetGenericInfo3(Sys.NetworkID, "gobject", oParams);
                    if (sSubmitTxId.Length > 20)
                    {
                        // Update the record allowing us to know this has been submitted
                        sql = "Update Proposal set Submitted=1,SubmitTime=GetDate(),Gobjectid='" 
                            + sSubmitTxId + "',SubmitTxId='" + sSubmitTxId + "' where id = '" 
                            + dt.Rows[ii]["id"].ToString()  + "'";
                        Sys._data.Exec2(sql);
                    }
                     GetWL(Sys.NetworkID);
                }
            }
        }

        public string GetJSForClipCopy(string data)
        {
            string js = "var textArea=document.createElement('input');"
                + "   textArea.style.float='top'; textArea.style.background = 'transparent';"
                + "   textArea.value = '" + data + "'; document.body.appendChild(textArea); textArea.select();"
                + "   try { var successful = document.execCommand('copy'); document.body.removeChild(textArea); }  catch(e){ }";
            return js;
        }

        public WebReply ProposalList()
        {
            ProcessProposals(Sys.NetworkID);
            SQLDetective s = Sys.GetSectionSQL("Proposal List", "Proposal", string.Empty);
            s.WhereClause = " network='" + Sys.NetworkID + "' and PaidTime is null and TriggerTxId is null and added > getdate()-45";
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
            w.AlternatingRows = 1;
            w.AlternationColor = "NonAlternated";

            w.URLDefaultValue = "View Proposal";
            w.AddContextMenuitem("CopyProposalID", "Copy Proposal ID", "Copy Proposal ID");
            w.AddContextMenuitem("CopyProposalVote", "Copy Vote For Proposal", "");
            w.AddContextMenuitem("CopyProposalVoteAgainst", "Copy Vote Against Proposal", "");

            w.bShowRowSelect = false;
            w.bShowRowTrash = false;
            WebReply wr = w.GetWebList(sql, "Proposal List", "Proposal List", "Name", "Proposal", this, false);
            // Add the Budget
            WebReply wBudget = BudgetList();
            wr.AddWebPackages(wBudget.Packages);
            // Add the Funded Proposals
            WebReply wProposalsFunded= FundedList();
            wr.AddWebPackages(wProposalsFunded.Packages);
            // Add the chart
            Charts c = new Charts(Sys);

            WebReply wrChart = c.ProposalFundingChart(Sys);
            wr.AddWebPackages(wrChart.Packages);
            // Add the Funded By ExpenseType Chart
            WebReply wrChart2 = c.ProposalFundingByExpenseType(Sys);
            wr.AddWebPackages(wrChart2.Packages);
            return wr;
        }

        public WebReply FundedList()
        {
            SQLDetective s = Sys.GetSectionSQL("Funded List", "Proposal", string.Empty);
            s.WhereClause = " network='" + Sys.NetworkID + "' and PaidTime is not null";
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
            Sys.SetObjectValue("Funded List", "ExpandableSection" + "BiblePayPool2018.Home" + "FundedList", "EXPANDED");
            
            w.bShowRowSelect = false;
            w.URLDefaultValue = "Discussion";
            w.bShowRowTrash = true;
            WebReply wr = w.GetWebList(sql, "Funded List", "Funded List", "Name", "Proposal", this, false);
            return wr;
        }

        public WebReply BudgetList()
        {
            SQLDetective s = Sys.GetSectionSQL("Budget List", "Proposal", string.Empty);
            s.WhereClause = " network='" + Sys.NetworkID + "' and PaidTime is null and TriggerTxId is not null";
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
            w.URLDefaultValue = "Show Budget Discussion";
            w.AddContextMenuitem("CopyBudgetID", "Copy Budget ID", "Copy Budget ID");
            w.AddContextMenuitem("CopyBudgetVote", "Copy Vote for Budget", "");
            w.AddContextMenuitem("CopyBudgetVoteAgainst", "Copy Vote Against Budget", "");
            w.bShowRowSelect = false;
            w.URLDefaultValue = "Discussion";
            w.bShowRowTrash = true;
            WebReply wr = w.GetWebList(sql, "Budget List", "Budget List", "Name", "Proposal", this, false);
            return wr;
        }


        public WebReply BudgetList_ContextMenu_CopyBudgetID()
        {
            string sValue = GetFieldFromObjectBasedOnLastClick("TriggerTxID", "Proposal");
            string js = GetJSForClipCopy(sValue);
            string sPre = (sValue.Length > 5) ? "Copied Budget Vote " + sValue + " to clipboard." : "Unable to locate Budget.";
            return ClipDialog(sPre, js,"Copy Budget ID");
        }

        public WebReply BudgetList_ContextMenu_CopyBudgetVote()
        {
            string sValue = GetFieldFromObjectBasedOnLastClick("TriggerTxID", "Proposal");
            string sCmd = "gobject vote-many " + sValue + " funding yes";
            string js = GetJSForClipCopy(sCmd);
            string sPre = (sValue.Length > 5) ?  sCmd : "Unable to locate Budget.";
            return ClipDialog(sPre, js,"Vote For Budget");
        }


        public WebReply BudgetList_ContextMenu_CopyBudgetVoteAgainst()
        {
            string sValue = GetFieldFromObjectBasedOnLastClick("TriggerTxID", "Proposal");
            string sCmd = "gobject vote-many " + sValue + " funding no";
            string js = GetJSForClipCopy(sCmd);
            string sPre = (sValue.Length > 5) ? sCmd : "Unable to locate Budget.";
            return ClipDialog(sPre, js, "Vote Against Budget");
        }

        public WebReply ProposalAdd()
        {
            Dialog d = new Dialog(Sys);
            WebReply wr = d.CreateDialog("dlgErr1", "Error while navigataing to Add Proposal", "Sorry, this page is under construction.  Please add the proposal using the BiblePay Evolution Core wallet (QT).", 350, 300);
            return wr;
            Section ProposalAdd = new Section("Add Proposal", 1, Sys, this);
            AddLabel("Add Proposal", "lbl1", "Note:",ProposalAdd, "** [" + Sys.NetworkID + "] Note: Each new Proposal costs 2500 BBP.  Please do not Save the proposal unless you agree to pay 2500 BBP **");
            Edit geName = new Edit("Add Proposal", "Proposal Name", Sys);
            geName.CaptionText = "Proposal Name";
            Edit geFRA = new Edit("Add Proposal", "Funding Receiving Address", Sys);
            geFRA.CaptionText = "Funding Receiving Address";
            Edit geAmount = new Edit("Add Proposal", "Proposal Amount", Sys);
            geAmount.CaptionText = "Proposal Amount";
            Edit geURL = new Edit("Add Proposal", "Discussion URL", Sys);
            geURL.CaptionText = "Discussion URL";
            geURL.TextBoxStyle = "width:620px";
            geName.TextBoxStyle = "width:600px";
            geAmount.TextBoxStyle = "width:300px";
            geFRA.TextBoxStyle = "width:390px";
            ProposalAdd.AddControl(geName);
            ProposalAdd.AddControl(geFRA);
            ProposalAdd.AddControl(geAmount);
            ProposalAdd.AddControl(geURL);
            // Budget Center

            Edit ddExpenseType = new Edit("Add Proposal", "ExpenseType", Sys);

            ddExpenseType.Type = Edit.GEType.Lookup;
            ddExpenseType.CaptionText = "Expense Type:";
            ddExpenseType.LookupValues = new List<SystemObject.LookupValue>();
            SystemObject.LookupValue i1 = new SystemObject.LookupValue();
            i1.ID = "Charity";
            i1.Value = "Charity";
            i1.Caption = "Charity";
            ddExpenseType.LookupValues.Add(i1);

            SystemObject.LookupValue i2 = new SystemObject.LookupValue();
            i2.ID = "PR";
            i2.Value = "PR";
            i2.Caption = "PR";
            ddExpenseType.LookupValues.Add(i2);

            SystemObject.LookupValue i3 = new SystemObject.LookupValue();
            i3.ID = "P2P";
            i3.Value = "P2P";
            i3.Caption = "P2P";
            ddExpenseType.LookupValues.Add(i3);

            SystemObject.LookupValue i4 = new SystemObject.LookupValue();
            i4.ID = "IT";
            i4.Value = "IT";
            i4.Caption = "IT";
            ddExpenseType.LookupValues.Add(i4);

            SystemObject.LookupValue i5 = new SystemObject.LookupValue();
            i5.ID = "Poll";
            i5.Value = "Poll";
            i5.Caption = "Poll";
            ddExpenseType.LookupValues.Add(i5);
            
            ProposalAdd.AddControl(ddExpenseType);
            
            Edit geBtnSave = new Edit("Save", Edit.GEType.Button, "btnProposalSave", "Save", Sys);
            ProposalAdd.AddControl(geBtnSave);
            return ProposalAdd.Render(this, true);
        }

        bool IsUrlValid(string sURL)
        {
            Uri uriResult;
            bool result = Uri.TryCreate(sURL, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            return result;

        }

        public WebReply btnProposalSave_Click()
        {
            string sName = Sys.GetObjectValue("Add Proposal", "Proposal Name");
            sName.Replace(" ", "_");
            string sFRA = Sys.GetObjectValue("Add Proposal", "Funding Receiving Address");
            string sAmount = Sys.GetObjectValue("Add Proposal", "Proposal Amount");
            string sURL = Sys.GetObjectValue("Add Proposal", "Discussion URL");
            Dialog d = new Dialog(Sys);
            string sErr = "";
            if (sName.Length < 5) sErr = "Name too short.";
            sName.Replace("'", "");

            bool bValid = Sys.ValidateBiblepayAddress(sFRA, Sys.NetworkID);
            if (!bValid) sErr = "Invalid receiving address. [" + Sys.NetworkID + "]";
            string sExpenseType = Sys.GetObjectValue("Add Proposal", "ExpenseType");
            if (sExpenseType == "") sErr = "Expense Type Missing.";

            if (Convert.ToDouble("0" + sAmount) < 10)
            {
                sErr = "Amount too low";
            }

            if (sURL.Length < 5) sErr = "URL invalid";
            if (!IsUrlValid(sURL)) sErr = "URL is not formatted.";
            if (sErr != "")
            {
                WebReply wr = d.CreateDialog("dlgerr", "Error while Adding Proposal",sErr, 340, 300);
                return wr;
            }

            double dOldBalance = 0;
            double dImmature = 0;

             GetUserBalances(Sys.NetworkID,Sys.UserGuid, ref dOldBalance, ref dImmature);
            double dAvail = dOldBalance - dImmature;
            if (dAvail < 2500)
            {
                WebReply wr = d.CreateDialog("dlgErr", "Error while adding proposal", "Sorry, balance too low to add a proposal.  Requirement : 2500 BBP.", 350, 300);
                return wr;
            }
            

            // gobject prepare 0 1 EPOCH_TIME HEX
            int unixEndTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            int unixStartTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            string sType = "1"; //Proposal
            string sArgs = unixEndTimestamp.ToString() + " " + sName + " " + sFRA + " " + sAmount + " " + unixStartTimestamp.ToString() + " " + sType + " " + sURL;
            string sCmd1 = "gobject serialize " + sArgs;
            object[] oParams = new object[9];
            oParams[0] = "serialize";
            oParams[1] = unixEndTimestamp.ToString();
            oParams[2] = sName;
            oParams[3] = sFRA;
            oParams[4] = sAmount;
            oParams[5] = unixStartTimestamp.ToString();
            oParams[6] = sType;
            oParams[7] = sURL;
            oParams[8] = sExpenseType;
            string sHex =  GetGenericInfo2(Sys.NetworkID, "gobject", oParams, "Hex");
            
            if (sHex.Length < 10)
            {
                // Throw an error
                sErr = "Gobject Create proposal failed in step [HEX].";
            }

            string sID = Guid.NewGuid().ToString();

            // Insert the row into proposals
            string sql = "Insert Into Proposal (id,ExpenseType,UserId,UserName,URL,name,receiveaddress,amount,unixstarttime,unixendtime,preparetxid,added,updated,hex,network,prepared,preparetime) "
                + " values ('" +  GuidOnly(sID) + "','" +  PurifySQL(sExpenseType,50)
                + "','" + Sys.UserGuid.ToString() + "','" + Sys.Username + "','" +  PurifySQL(sURL,150)
                + "','"              +  PurifySQL(sName,100)
                + "','" +  PurifySQL(sFRA,80)
                + "','" +  PurifySQL(sAmount,50)
                + "','" + unixStartTimestamp.ToString() 
                + "','" + unixEndTimestamp.ToString() 
                + "',null,getdate(),getdate(),'" + sHex + "','" + Sys.NetworkID + "',1,getdate())";

            Sys._data.Exec2(sql);
            // update the Sanctuary Count
            double dMNCount =  GetMasternodeCount(Sys.NetworkID);
            sql = "Update proposal set MasternodeCount='" + dMNCount.ToString() + "' where network='" +  VerifyNetworkID(Sys.NetworkID)
                + "' and paidtime is null";
            if (dMNCount > 1)  mPD.Exec2(sql);
            //1-3-2018
            bool bTrue =  AdjustUserBalance(Sys.NetworkID, Sys.UserGuid, -1 * 2500);
            int iHeight =  GetTipHeight(Sys.NetworkID);
             InsTxLog(Sys.Username, Sys.UserGuid, Sys.NetworkID,
                iHeight, "Proposal", -1 * 2500, dOldBalance, dOldBalance - 2500,
                sID, "PROPOSAL", sID);

            // redirect user to the list
            return ProposalList();
        }


        void AddDropDown(ref Edit dd, string sID, string sCaption)
        {
            SystemObject.LookupValue i1 = new SystemObject.LookupValue();
            i1.ID = sID;
            i1.Value = sID;
            i1.Caption = sCaption;
            dd.LookupValues.Add(i1);
        }

        public WebReply LinkAdd()
        {
            Section LinkAdd = new Section("Add Link", 1, Sys, this);
            Edit eURL = new Edit("Add Link", "URL", Sys);
            eURL.CaptionText = "URL:";
            eURL.TextBoxStyle = "width:650px";

            LinkAdd.AddControl(eURL);
            Edit eNotes = new Edit("Add Link", "Notes", Sys);
            eNotes.CaptionText = "Notes:";
            eNotes.TextBoxStyle = "width:800px";
            LinkAdd.AddControl(eNotes);

            // Faith levels:
            // Level 0 = (Atheist (non belief in God), Agnostic (No allegience to a Religion), Non-Christian (Allegience to a non Christian Religion),
            //    At this level we want to convince the non-believer that God is Real, God created the universe, Jesus is Lord,
            // and that Christianity is the correct religion to choose.  We want to back up our statements with Christian evidence.

            // Level 1 = Lukewarm or Backslidden Christian (One who has fallen away from the faith, needs support), reading the bible is hard, words have no meaning
            //  People in level 1 have a Conventional and Scientific world view; they dont think of the miracles of Christ could be real: the virgin birth, 
            //  the resurrection, haven, things that dont conform to the laws of physics
            //  They go to a Christian Church because others go, they believe Christ might be God but dont have a personal relationship with God

            // Level 2 = Conversion to Christianity, Repentant Heart - This person has decided to give his/her life to the Lord,
            // and has a repentant heart. They are sorry for squandering their lives aimlessly sinning, and are committed to repenting
            // for all of the mortal sins and transforming their lives into newborn believers, and accepting the Holy Spirit, dedicating
            // their temple to being ambassadors of Christ

            // Level 3 = Baby Christian (Saved and zealous for learning more)
            // Level 2 are considered Postconventional, you accept the miracles of Christianity and that heaven and hell exist in another dimension.

            // Level 4 = Dedicated to Jesus:  Believe in the Holy Spirit, and corresponding Gifts: Speaking in Tongues, Healing, Prophesying, 
            // Have read the entire bible and memorized The Lords Prayer, Psalm 23, rely on the Holy Spirit for Guidance
            // Your personal decisions are made by the Holy Spirit, You care about what he thinks of you,
            // you are dedicated to spreading the gospel, Prayer is important to solve problems and or important for healing, you will die for Jesus

            // Level 5 = Watchman/Prophet/Pastor/Teacher/Saint, this level is reserved for advanced Christians who dedicate their lives to his ministry 
            //  and analyze advanced topics, such as Armageddon, Hell, and spend the rest of their days serving the Lord


            Edit ddCategory = new Edit("Add Link", "FaithCategory", Sys);
            ddCategory.LookupValues = new List<SystemObject.LookupValue>();
            ddCategory.Type = Edit.GEType.Lookup;
            ddCategory.CaptionText = "Faith Category:";

            AddDropDown(ref ddCategory, "0", "Non-Believer/Atheist/Agnostict");
            AddDropDown(ref ddCategory, "1", "Lukwarm/Backslidden");
            AddDropDown(ref ddCategory, "2", "Converting");
            AddDropDown(ref ddCategory, "3", "BabyChristian");
            AddDropDown(ref ddCategory, "4", "Dedicated");
            AddDropDown(ref ddCategory, "5", "Prophet");

            LinkAdd.AddControl(ddCategory);
            Edit ePPC = new Edit("Add Link", "PPC", Sys);
            ePPC.CaptionText = "Payment Per Click:";
            LinkAdd.AddControl(ePPC);

            Edit eBudget = new Edit("Add Link", "Budget", Sys);
            eBudget.CaptionText = "Total Budget:";
            LinkAdd.AddControl(eBudget);

            Edit eBtnSave = new Edit("Add Link", Edit.GEType.Button, "btnLinkSave", "Save", Sys);
            LinkAdd.AddControl(eBtnSave);
            return LinkAdd.Render(this, true);
        }

        public WebReply btnWriteToChild_Click()
        {
            return WriteLetter();
        }

        public WebReply btnAbandon_Click()
        {

            string sChildID = Sys.GetObjectValue("Account Edit", "AdoptedOrphanID");
            if (sChildID == "")
            {
                return AccountEdit();
            }

            string sql = "Select ID,Name from Orphans where id = '" +  PurifySQL(sChildID, 100) + "'";
            Section s = new Section("l1", 1, Sys, this);
            Dialog d = new Dialog(Sys);
            string sCSS =  msReadOnly;
            string sName = Sys._data.GetScalarString2(sql, "Name", false);
            string sNarr = "Are you sure you would like to abandon " + sName + "?  This means you will no longer have a letter writing relationship. ";
            WebReply wr = d.CreateYesNoDialog("AdoptionAbandon", "adopt1", "AbandonYes", "AbandonNo", "BiblePay Adoption System", sNarr, this);
            return wr;
        }

        public WebReply btnLinkSave_Click()
        {
            string sNotes = Sys.Substring2(Sys.GetObjectValue("Add Link", "Notes"), 999);
            double dPPC = Sys.GetObjectDouble("Add Link", "PPC");
            double dBudget = Sys.GetObjectDouble("Add Link", "Budget");
            string sURL = Sys.GetObjectValue("Add Link", "URL");
            string sFaithCategory = Sys.GetObjectValue("Add Link", "FaithCategory");
            sURL = sURL.Replace("[amp]", "&");
            // Construct the Tiny URL:
            Dialog d = new Dialog(Sys);
            string sUserName = "";
            double Balance =  GetUserBalance(Sys.UserGuid.ToString(), Sys.NetworkID, ref sUserName);
            if (dBudget > Balance)
            {
                WebReply wr = d.CreateDialog("dlgerr", "Error while creating budget", "Sorry, Link budget exceeds your balance.", 240, 200);
                return wr;
            }
            if (dBudget < 1 || dPPC < 1)
            {
                WebReply wr = d.CreateDialog("dlgerr", "Error while creating budget", "Sorry, Link budget and Link Payment Per Click must be greater than zero.", 240, 200);
                return wr;
            }
            if (dBudget < dPPC)
            {
                WebReply wr = d.CreateDialog("dlgerr", "Error while creating budget", "Sorry, Link budget is less than one click payment.  Please increase total Link Budget.", 240, 200);
                return wr;
            }
            if (sNotes=="" || sURL=="")
            {
                WebReply wr = d.CreateDialog("dlgerr", "Error while creating budget", "Sorry, Link URL must be populated and Notes must be populated with a description of the URL.", 240, 200);
                return wr;
            }
            string sID = Guid.NewGuid().ToString();
            string sPrefix = sID.Substring(0, 7);
            string sSite = USGDFramework.clsStaticHelper.GetConfig("WebSite");
            string sShortURL = sSite + "" + "Action.aspx?link=" + sPrefix;
            string sql = "Insert Into Links (id,FaithCategory,username,Notes,Clicks,OriginalURL,URL,Added,UserId,PaymentPerClick,Budget) values ('" +
                sID + "','" +  PurifySQL(sFaithCategory.ToString(),100)
                + "','" + Sys.Username.ToString() + "','" +  PurifySQL(sNotes,335)
                + "',0,'" +  PurifySQL(sURL,455)
                    + "','" +   PurifySQL(sShortURL,400)
                                + "',getdate(),'" +  GuidOnly(Sys.UserGuid)
                                                + "','" + dPPC.ToString() 
                                                + "','" + dBudget.ToString() + "')";
            try
            {
                Sys._data.Exec2(sql);
            }
            catch (Exception ex)
            {
                WebReply wr = d.CreateDialog("Error", "Error while Adding Link",
                    "Error while adding Link: " + ex.Message.ToString(), 240, 200);
                return wr;
            }
            return LinkList();
        }

        
        public WebReply LinkNarrative(string sNarrative)
        {
            Section Faith = new Section("Faith", 1, Sys, this);
            AddLabelWithCaptionWidth("Faith", "lblA", "<p>", Faith, "Biblepay - Walk in Faith", 11);
            AddLabel("Faith", "lblB", "<p>", Faith, "<p><p><br><b><h3>" + sNarrative + "</h3> ");
            return Faith.Render(this, true);
        }
                
        public WebReply LinkList()
        {

            string sType = GetFieldFromObjectBasedOnLastClick("ObjectType", "Menu");
            string sLevel = GetFieldValueFromTableBasedOnPK("Level", "ObjectType", "FaithLevel", sType);
            string sNarr = GetFieldValueFromTableBasedOnPK("Notes", "ObjectType", "FaithLevel", sType);

            // Make a Section for the Narrative first
            string sWhere = " 1=1";
            if (sLevel != "") sWhere = " FaithCategory='" + sLevel + "'";

            
            SQLDetective s = Sys.GetSectionSQL("Link List", "Links", string.Empty);
            if (ParentID == "") ParentID = Sys.Organization.ToString();
            s.WhereClause = sWhere;

            s.OrderBy = " Added ";
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
            w.URLDefaultValue = "Display";
            w.AddContextMenuitem("CopyLink", "Copy Link", "Copy Link");
            WebReply wrLinkNarr = LinkNarrative(sNarr);
            WebReply wrLinkList = w.GetWebList(sql, "Link List", "Link List", "", "Links", this, false);
            wrLinkNarr.AddWebPackages(wrLinkList.Packages);
            return wrLinkNarr;
        }

        public WebReply LinkList_ContextMenu_CopyLink()
        {
            string sValue = GetFieldFromObjectBasedOnLastClick("URL", "Links");
            string sCmd = sValue;
            string js = GetJSForClipCopy(sCmd);
            string sPre = (sValue.Length > 5) ? sCmd : "Unable to locate URL.";
            return ClipDialog(sPre, js, "Copy URL");
        }

        public WebReply DirectorList()
        {
            SQLDetective s = Sys.GetSectionSQL("Director List", "DonationDirector", string.Empty);
            s.WhereClause = " 1=1";
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
            w.bShowRowSelect = false;
            w.bShowRowTrash = false;
            WebReply wr = w.GetWebList(sql, "Director List", "Director List", "", "DonationDirector", this, false);
            WebReplyPackage wrp1 = wr.Packages[0];
            wrp1.ClearScreen = true;
            wr.Packages[0] = wrp1;
            return wr;
        }

        public WebReply DirectorAdd()
        {
            Section sDD = new Section("Direct a Donation", 1, Sys, this);
            Edit geTXID = new Edit("Direct a Donation", "TXID", Sys);
            geTXID.CaptionText = "Donation Transaction ID:";
            geTXID.TextBoxStyle = "width:420px";
            sDD.AddControl(geTXID);
            Edit ddCharity = new Edit("Direct a Donation", "Charity", Sys);
            ddCharity.Type = Edit.GEType.Lookup;
            ddCharity.CaptionText = "Charity:";
            ddCharity.LookupValues = new List<SystemObject.LookupValue>();
            AddDropDown(ref ddCharity, "Compassion", "Compassion");
            AddDropDown(ref ddCharity, "BLOOM", "BLOOM");
            AddDropDown(ref ddCharity, "SXS Women of Uganda", "SXS Women of Uganda");
            AddDropDown(ref ddCharity, "HFOC-Kenya", "HFOC-Kenya");
            AddDropDown(ref ddCharity, "Kairos", "Kairos");
            AddDropDown(ref ddCharity, "Cameroon One", "Cameroon One");
            sDD.AddControl(ddCharity);
            Edit geBtnSave = new Edit("Save", Edit.GEType.Button, "btnDDSave", "Save", Sys);
            sDD.AddControl(geBtnSave);
            return sDD.Render(this, true);
        }

        public WebReply btnDDSave_Click()
        {
            // Chosen Charity
            string sCharity = Sys.Substring2(Sys.GetObjectValue("Direct a Donation", "Charity"), 254);
            string sTXID = Sys.GetObjectValue("Direct a Donation", "TXID");
            string sError = "";
            string sUnique = Guid.NewGuid().ToString();
            if (sTXID.Length != 64)
            {
                sError = "You must enter a valid TXID.";
            }

            if (sCharity == "")
            {
                sError = "You must choose a charity.";
            }

            if (sError == "")
            {
                string sql = "Insert Into DonationDirector (id,UserId,TXID, Charity, Added,updated,username) values (newid(),'" + GuidOnly(Sys.UserGuid)
                     + "','" + sTXID + "','" + sCharity + "',getdate(),getdate(),'" + Sys.Username + "')";
                try
                {
                    Sys._data.ExecWithThrow2(sql, false);
                }
                catch (Exception ex)
                {
                    sError = "Error! " + ex.Message;
                }
            }

            Dialog d = new Dialog(Sys);
            WebReply wr = null;

            if (sError == "")
            {
                 wr = d.CreateDialog("Saved"  + sUnique, "Saved", "Donation Director Record Saved.", 240, 200);
            }
            else
            {
                wr = d.CreateDialog("DD Error" + sUnique,"Record Rejected", sError, 440, 400);


            }
            return wr;
            
        }
        public WebReply WorkerAdd()
        {
            Section WorkerAdd = new Section("Add Worker", 1, Sys, this);
            Edit geWorkerUsername = new Edit("Add Worker", "Username", Sys);
            geWorkerUsername.CaptionText = "Worker Username:";
            WorkerAdd.AddControl(geWorkerUsername);
            Edit geNotes = new Edit("Add Worker", "Notes", Sys);
            // Funded ABNs?
            Edit ddF = new Edit("Add Worker", "FundedABN", Sys);
            ddF.Type = Edit.GEType.Lookup;
            ddF.CaptionText = "Funded ABN?";
            ddF.LookupValues = new List<SystemObject.LookupValue>();

            AddDropDown(ref ddF, "0", "No");
            AddDropDown(ref ddF, "1", "Yes");
            WorkerAdd.AddControl(ddF);


            geNotes.CaptionText = "Notes:";
            WorkerAdd.AddControl(geNotes);
            Edit geBtnSave = new Edit("Save", Edit.GEType.Button, "btnWorkerSave", "Save", Sys);
            WorkerAdd.AddControl(geBtnSave);
            return WorkerAdd.Render(this, true);
        }

        public WebReply btnWorkerSave_Click()
        {
            string sNotes = Sys.Substring2(Sys.GetObjectValue("Add Worker", "Notes"), 254);
            Sys.SetObjectValue("Add Worker", "Notes", sNotes);

            if (Sys.GetObjectValue("Add Worker", "Username").Length > 19)
            {
                Sys.SetObjectValue("Add Worker", "Username", Sys.Substring2(Sys.GetObjectValue("Add Worker", "Username"), 19));
                Dialog d = new Dialog(Sys);
                WebReply wr = d.CreateDialog("dlgerr", "Error while Adding Miner", "Miner WorkerName must not be greater than 19 characters long.  Record Not Saved.  Miner Name updated to 19 length on page. (Error 60002)", 240, 200);
                return wr;
            }
            double nFunded = ToDouble(Sys.GetObjectValue("Add Worker", "FundedABN"));

            string sql = "Insert Into Miners (id,UserId,Username,Notes,Funded, updated,added) values (newid(),'" +  GuidOnly(Sys.UserGuid)
                 + "',@Username,"  + "'" +  PurifySQL(sNotes,500) + "','" + nFunded.ToString() + "',getdate(),getdate())";
            sql = Sys.PrepareStatement("Add Worker", sql);
            try
            {
                Sys._data.ExecWithThrow2(sql, false);
            }
            catch (Exception ex)
            {
                Dialog d = new Dialog(Sys);
                WebReply wr = d.CreateDialog("Error", "Error while Adding Miner",
                    "Error while adding record, most likely this miner username was already used previously, or has been used by another user. (Error 60001 " + ex.Message.Substring(0,10) + ")", 240, 200);
                return wr;
            }
            // Redirect user to miners list
            return WorkerList();
        }

        public WebReply FormLoad()
        {
            // If Tipping, redirect to tip bot, otherwise redirect to Home page
            string sToRecipient = Sys.GetObjectValue("Tip", "Recipient1");
            if (sToRecipient != "")
            {
                Sys.SetObjectValue("Tip", "ToRecipient", sToRecipient);
                Sys.SetObjectValue("Tip", "Recipient1", "");
                return Sys.Redirect("Tip", this);
            }

            return Sys.Redirect("Home", this);
        }
    }
}