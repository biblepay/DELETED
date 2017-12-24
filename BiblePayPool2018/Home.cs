using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.DataVisualization.Charting;
using System.Data;
using System.Web.SessionState;

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
            WebReply wr = w.GetWebList(sql, "Expense List", "Expense List", "", "Expense", this, false);
            return wr;
        }


        public WebReply TxHistory_Export()
        {
            string sql = "Select top 512 ID,TransactionType,Destination,Amount,NewBalance,Updated,Height,Notes From TransactionLog where "
                    + " NetworkID = '" + Sys.NetworkID + "' AND UserID='" + Sys.UserGuid.ToString() + "' ORDER BY Updated desc";
            string URL = Sys.SqlToCSV(sql);
            Dialog e = new Dialog(Sys);
            string sNarr = "<a href='" + URL + "'><font color=orange><b>Click here to download CSV File.</a>";
            WebReply wr2 = e.CreateDialog("Data_Export", "Data_Exported", "Data Exported.  " + sNarr +"",300, 150);
            return wr2;
         }

        public WebReply TxHistory()
        {
            string sql = "Select top 512 ID,TransactionType,Destination,Amount,NewBalance,Updated,Height,Notes From TransactionLog where "
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
            if (Sys.Username != Sys.AppSetting("AdminUser", ""))
            {
                Dialog d = new Dialog(Sys);
                WebReply wr = d.CreateYesNoDialog("Admin", "AdminError", "AdminYes", "AdminNo", "Admin_Error", "Sorry, you do not have admin privileges.  Would you like to apply for them?", this);
                return wr;
            }
            clsStaticHelper.ScanBlocksForPoolBlocksSolved("main", true);
            Dialog e = new Dialog(Sys);

            if (true)
            {
                clsStaticHelper.RewardUpvotedLetterWriters();
            }
            WebReply wr2 = e.CreateDialog("Admin1", "Successfully paid old participants", "Successfully paid old participants.", 100, 100);
            return wr2;
        }

        public WebReply LettersInbound()
        {
            string sql = "Select * from LettersInbound order by ";
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
            string sURL = Sys._data.GetScalarString(sql, "URL");
            string sName = Sys._data.GetScalarString(sql, "Name");
            string sPage = Sys._data.GetScalarString(sql, "Page");
            string sNarr = sName + " - " + sPage.ToString();
            Section s = new Section("l1", 1, Sys, this);
            Dialog d = new Dialog(Sys);
            string sBody = "<img width=900 height=1000 src='" + sURL + "'>";
            string sNarr2 = sNarr.Replace(" ", "_");
            WebReply wr = d.CreateDialog(sNarr2, "View1", sBody, 1000, 1100);
            return wr;
        }

        public WebReply BlockDistribution_RowClick()
        {
            
            string sId = Sys.LastWebObject.guid.ToString();
            if (sId == "") return BlockDistribution();
            string sql = "Select top 512 block_distribution.id,users.username, block_distribution.height, block_distribution.block_subsidy, "
                + " block_distribution.subsidy, block_distribution.paid, block_distribution.hps, block_distribution.PPH, "
                + " block_distribution.updated, block_distribution.stats,users.cloak  from block_distribution "
                + " inner join Users on users.id = block_distribution.userid WHERE Block_Distribution.ID='" + sId + "' ";
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
            string             sql = "Select * from blockdistribution" + Sys.NetworkID;
            SQLDetective s = Sys.GetSectionSQL("Block Distribution View", "Block_Distribution", string.Empty);
            Weblist w = new Weblist(Sys);
            w.bShowRowHighlightedByUserName = true;
            w.bSupportCloaking = true;
            WebReply wr = w.GetWebList(sql, "Block Distribution View", "Block Distribution View", "", "Block_Distribution", this, false);
            return wr;
        }

        public WebReply BlockHistory()
        {
            string sql = "Select top 512 ID,Height,Updated,Subsidy,MinerNameWhoFoundBlock as Miner_Who_Found_Block,MinerNameByHashPs as Top_Miner_By_HashPS,BlockVersion FROM BLOCKS where networkid='"
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
            string sSourceTable = "Leaderboard" + Sys.NetworkID;
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
                s.OrderBy = "hps2 desc";
            }
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
            w.bShowRowHighlightedByUserName = true;
            w.bSupportCloaking = true;
            WebReply wr = w.GetWebList(sql, "Leaderboard View", "Leaderboard View", "", "Leaderboard", this, false);
            return wr;
        }

        public WebReply Leaderboard_OrderByClick()
        {
            return Leaderboard();
        }

        public WebReply MyLeaderboard()
        {
            string sSourceTable = "Leaderboard" + Sys.NetworkID;
            SQLDetective s = Sys.GetSectionSQL("Leaderboard View", sSourceTable, string.Empty);
            if (ParentID == "") ParentID = Sys.Organization.ToString();
            s.WhereClause = " username='" + Sys.Username + "'";
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
            WebReply wr = w.GetWebList(sql, "My Leaderboard", "My Leaderboard", "", "Leaderboard", this, false);
            return wr;
        }

        public WebReply ProposalFundingChart()
        {
            string sql = "select Name,absoluteYesCount,NoCt,MasternodeCount, (AbsoluteYesCount)/(MasterNodeCount + .01) as popularity from proposal where Network='" 
                + Sys.NetworkID + "' and paidtime is null";
            // Chart the funding percent by proposal
            Chart c = new Chart();
            c.Width = 1500;
            Series s = new Series("ChartOrphanHistory");
            s.ChartType = System.Web.UI.DataVisualization.Charting.SeriesChartType.Bar;
            s.LabelForeColor = System.Drawing.Color.Green;
            s.Color = System.Drawing.Color.Green;
            c.ChartAreas.Add("ChartArea");
            c.ChartAreas[0].BorderWidth = 1;
            c.Series.Add(s);
            c.ChartAreas[0].AxisX.LabelStyle.Interval = 1;
            DataTable dt = Sys._data.GetDataTable(sql);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                double YesCt = GetDouble(dt.Rows[i]["absoluteYesCount"]);
                double NoCt = Math.Abs(GetDouble(dt.Rows[i]["NoCt"]));
                double MNC = GetDouble(dt.Rows[i]["MasternodeCount"]);
                string sNarr = YesCt.ToString() + " - " + MNC.ToString();
                double Pop = GetDouble(dt.Rows[i]["Popularity"]);
                bool bRed = (Pop * 100) < 10 && NoCt > 0;
                string Name = dt.Rows[i]["Name"].ToString();
                string sFullName = Name + " [" + sNarr + "]";
                double dPointValue = Pop * 100;
                s.Points.AddXY(sFullName, dPointValue);
                if (bRed)
                {
                    foreach (DataPoint p in s.Points)
                    {
                        if (p.AxisLabel == sFullName)
                        {
                            p.Color = System.Drawing.Color.Red;
                            p.YValues[0] = dPointValue * -1;
                            if (p.YValues[0] < 1 || true) p.YValues[0] = -1;
                        }
                    }
                }
            }
            c.ChartAreas[0].AxisX.LabelStyle.ForeColor = System.Drawing.Color.LightGreen;
            c.ChartAreas[0].AxisY.LabelStyle.ForeColor = System.Drawing.Color.LightGreen;
            c.ChartAreas[0].BackColor = System.Drawing.Color.Black;
            c.Titles.Add("Proposal Funding Level");
            c.Titles[0].ForeColor = System.Drawing.Color.Green;
            c.BackColor = System.Drawing.Color.Black;
            c.ForeColor = System.Drawing.Color.Green;
            string sFileName = Guid.NewGuid().ToString() + ".png";
            string sSan = clsStaticHelper.AppSetting("SAN", "SAN_NOT_SET");
            string sTargetPath = sSan + sFileName;
            c.SaveImage(sTargetPath);
            Section s1 = new Section("Proposal Funding Level", 1, Sys, this);
            Edit gePFL = new Edit("Proposal Funding Level", Edit.GEType.Image, "W1", "", Sys);
            gePFL.URL = clsStaticHelper.AppSetting("WebSite", "http://myurl.biblepay.org/") + "SAN/" + sFileName;
            s1.AddControl(gePFL);
            return s1.Render(this, false);
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

        public WebReply AccountEdit()
        {
            string sql = "Select * from Users with (nolock) where Username='" + Sys.Username + "' and deleted=0";
            System.Data.DataTable dt = Sys._data.GetDataTable(sql);
            if (dt.Rows.Count > 0)
            {
                Sys.SetObjectValue("Account Edit", "Email", "" + dt.Rows[0]["Email"].ToString());
                Sys.SetObjectValue("Account Edit", "DefaultWithdrawalAddress", "" + dt.Rows[0]["WithdrawalAddress"].ToString());
                string sSuffix = Sys.NetworkID == "main" ? "" : "testnet";
                string sCloak = dt.Rows[0]["cloak"].ToString() == "1" ? "true" : "false";
                Sys.SetObjectValue("Account Edit", "Cloak", sCloak);
                double dBalance = GetDouble(dt.Rows[0]["Balance" + Sys.NetworkID]);
                Sys.SetObjectValue("Account Edit", "Balance", dBalance.ToString());
                Sys.SetObjectValue("Account Edit", "Address1", dt.Rows[0]["Address1"].ToString());
                Sys.SetObjectValue("Account Edit", "Address2", dt.Rows[0]["Address2"].ToString());
                Sys.SetObjectValue("Account Edit", "City", dt.Rows[0]["City"].ToString());
                Sys.SetObjectValue("Account Edit", "State", dt.Rows[0]["State"].ToString());
                Sys.SetObjectValue("Account Edit", "Zip", dt.Rows[0]["Zip"].ToString());
                Sys.SetObjectValue("Account Edit", "DelName", dt.Rows[0]["DelName"].ToString());
                Sys.SetObjectValue("Account Edit", "Phone", dt.Rows[0]["Phone"].ToString());
                Sys.SetObjectValue("Account Edit", "Country", dt.Rows[0]["Country"].ToString());
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
            AccountEdit.AddBlank();
            Edit geCloak = new Edit("Account Edit", "Cloak", Sys);
            geCloak.CaptionText = "Cloak Users and Miners:";
            geCloak.Type = Edit.GEType.CheckBox;
            AccountEdit.AddControl(geCloak);
            AccountEdit.AddBlank();
            Edit geBalance = new Edit("Account Edit", "Balance", Sys);
            geBalance.CaptionText = "Balance:";
            geBalance.Type = Edit.GEType.Text;
            geBalance.TextBoxAttribute = "readonly";
            geBalance.TextBoxStyle = "background-color:grey;";
            AccountEdit.AddControl(geBalance);
            AccountEdit.AddBlank();
            // Bible Pay Store
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
            Edit geBtnSave = new Edit("Account Edit", Edit.GEType.Button, "btnAccountSave", "Save", Sys);
            AccountEdit.AddControl(geBtnSave);
            Edit geBtnEmail = new Edit("Account Edit", Edit.GEType.Button, "btnEmailVerify", "Verify E-Mail", Sys);
            AccountEdit.AddControl(geBtnEmail);
            return AccountEdit.Render(this, true);
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
            string sql = "Update Users set DelName=@DelName,Address1=@Address1,Phone=@Phone,Address2=@Address2,City=@City,State=@State,Zip=@Zip,Country=@Country,Updated=getdate(), Cloak='" + iCloak.ToString() + "', Email=@Email, WithdrawalAddress=@DefaultWithdrawalAddress where username='"
                 + Sys.Username + "' and deleted=0";
            sql = Sys.PrepareStatement("Account Edit", sql);
            Sys._data.Exec(sql);
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
                string sql2 = "Update Users set password='[txtAltPass]' where username='" + Sys.Username + "' and deleted=0";
                sql2 = Sys.PrepareStatement("Account Edit", sql2);
                sql2 = sql2.Replace("[txtAltPass]", modCryptography.Des3EncryptData(sNewPass));
                Sys._data.Exec(sql2);
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
            string sIP = (HttpContext.Current.Request.UserHostAddress ?? "").ToString();
            string sql = "Delete from Votes where letterid = '" + sId + "' and (userid = '" + Sys.UserGuid.ToString() + "' OR ip='" + sIP + "')";
            Sys._data.Exec(sql);
            double dUpvote = (sVote == "upvote") ? 1 : 0;
            double dDownvote = (sVote == "downvote") ? 1 : 0;
            sql = "insert into votes (id,added,letterid,userid,upvote,downvote,ip) values (newid(),getdate(),'" + sId
                + "','" + Sys.UserGuid.ToString() + "'," + dUpvote.ToString()
                + "," + dDownvote.ToString() + ",'" + sIP + "')";
            Sys._data.Exec(sql);
            sql = "update Letters set upvote=(Select sum(Upvote) from Votes where letterid='" + sId
                + "'), downvote = (Select sum(downvote) from votes where letterid='" + sId + "') where id='" + sId + "'";
            Sys._data.Exec(sql);
            sql = "update letters set Approved=0 where Upvote-Downvote < 10";
            Sys._data.Exec(sql);
            sql = "update letters set Approved=1 where Upvote-Downvote >= 10";
            Sys._data.Exec(sql);
            sql = "update letters set Sent=0 where sent is null";
            Sys._data.Exec(sql);
            sql = "Update Orphans set NeedWritten = 0 where 1=1";
            Sys._data.Exec(sql);
            sql = "Update Orphans set NeedWritten = 1 where OrphanID not in (Select OrphanID from letters where added > getdate()-60)";
            Sys._data.Exec(sql);
            return OrphanLetters();
        }

        public WebReply Announce()
        {
            Section Announce = new Section("Announce", 1, Sys, this);
            Announce.MaskSectionMode = true;
            string sql = "Select Value from System where SystemKey='Announce'";
            string a = Sys._data.GetScalarString(sql, "Value");
            AddLabelWithCaptionWidth("Announce", "lblA", "Announcements:", Announce, a, 290);
            AddLabel("Announce", "lblD", "<p>", Announce, "");
            // Outgoing letter enforcement fees
            AddLabel("Announce", "lblOC1", "<p>", Announce, "");
            double dLWF = Convert.ToDouble(Sys.AppSetting("fee_letterwriting", "0"));
            double dUpvotedCount = clsStaticHelper.GetUpvotedLetterCount();
            double dRequiredCount = clsStaticHelper.GetRequiredLetterCount();
            AddLabel("Announce", "lblOC", "Outbound Approved Letter Count:", Announce, dUpvotedCount.ToString());
            AddLabel("Announce", "lblRQC", "Outbound Required Letter Count:", Announce, dRequiredCount.ToString());
            double dFee = (dUpvotedCount < dRequiredCount ? dLWF : 0);
            AddLabel("Announce", "lblLetterFees", "Non-Participating Letter Fees:", Announce, (dFee * 100).ToString() + "%");
            double dMyLetterCount = clsStaticHelper.GetMyLetterCount(Sys.UserGuid.ToString());
            double dMyLetterFee = (dMyLetterCount == 0 ? dFee : 0);
            AddLabel("Announce", "lblLetterFees2", "My (Personal) Letter Fees:", Announce, (dMyLetterFee * 100).ToString() + "%");
            double dBounty = clsStaticHelper.GetTotalBounty();
            double dIndBounty = Math.Round(dBounty / (dRequiredCount + .01), 2);
            AddLabel("Announce", "lblLetterBounty", "Current Approved Letter Writing Bounty:", Announce, dIndBounty.ToString());
            // Balance
            double dBal = 0;
            double dImm = 0;
            clsStaticHelper.GetUserBalances(Sys.NetworkID, Sys.UserGuid.ToString(), ref dBal, ref dImm);
            AddLabel("Announce", "lblBal", "Total Balance:", Announce, dBal.ToString());
            AddLabel("Announce", "lblImm", "Immature Balance:", Announce, dImm.ToString());
            // User stats - coins mined in last 1, 24 hour and 7 days
            sql = "Select sum(Amount) a from TransactionLog where Added > getdate()-(1/24.01) and TransactionType = 'MINING_CREDIT'  and networkid = '" + Sys.NetworkID + "' and userid='" + Sys.UserGuid.ToString() + "'";
            double dUC1 = Math.Round(clsStaticHelper.mPD.GetScalarDouble(sql, "a"), 2);
            sql = "Select sum(Amount) a from TransactionLog where Added > getdate()-(1) and TransactionType = 'MINING_CREDIT'  and networkid = '" + Sys.NetworkID + "' and userid='" + Sys.UserGuid.ToString() + "'";
            double dUC24 = Math.Round(clsStaticHelper.mPD.GetScalarDouble(sql, "a"), 2);
            sql = "Select sum(Amount) a from TransactionLog where Added > getdate()-(7) and TransactionType = 'MINING_CREDIT'  and networkid = '" + Sys.NetworkID + "' and userid='" + Sys.UserGuid.ToString() + "'";
            double dUC7d = Math.Round(clsStaticHelper.mPD.GetScalarDouble(sql, "a"), 2);
            AddLabel("Announce", "uc1", "Coins mined (in the last hour):", Announce, dUC1.ToString());
            AddLabel("Announce", "uc7", "Coins mined (in the last 24 hours):", Announce, dUC24.ToString());
            AddLabel("Announce", "uc24", "Coins mined (in the last week):", Announce, dUC7d.ToString());
            // BTC Price, BBP Price
            Zinc.BiblePayMouse.MouseOutput m = clsStaticHelper.GetCachedCryptoPrice("bbp");
            AddLabel("Announce", "lblBTC", "BTC Price: ", Announce, "$" + m.BTCPrice.ToString());
            AddLabel("Announce", "lblBBP", "BBP Price:", Announce, "$" + m.BBPPrice.ToString());
            AddLabel("Announce", "lblHR", "", Announce, "<hr>");
            AddLabel("Announce", "lblP", "<p>", Announce, "");
            AddLabel("Announce", "lblOC5", "<p>", Announce, "");
            AddLabel("Announce", "lbl71", "Note:", Announce, "** To write a letter to one of our orphans, from the left menu click: Orphans | Sponsored Orphan List | then Right Click on one of the Orphan records (where the NeedWritten column=TRUE).  Our children LOVE to receive letters!  Please write to one that needs written and you will receive the Letter Writing Bounty above once the letter is upvoted to 7 votes! **");
            bool bAcc = (HttpContext.Current.Request.Url.ToString().ToUpper().Contains("ACCOUNTABILITY"));
            Sys.lOrphanNews++;
            bool bShowOrphanNews = (bAcc || Sys.lOrphanNews == 1);
            if (bShowOrphanNews)
            {
                string sVideo = clsStaticHelper.mPD.GetScalarString("Select Value as Video from System where SystemKey='Video'", "Video");
                string sOrphanNews = "<xdiv style='width:400;height:400'><iframe src='" + sVideo + "'></xdiv>";
                AddLabel("Announce", "vidCompassion", "Orphan News:", Announce, sOrphanNews);
            }
            return Announce.Render(this, true);
        }


        public WebReply About()
        {
            Section About = new Section("About", 1, Sys, this);
            Edit lblInfo = new Edit("About", "lblInfo", Sys);
            lblInfo.Type = Edit.GEType.Label;
            lblInfo.CaptionText = "Pool Information:";
            About.AddControl(lblInfo);
            AddLabel("About", "lblProvidedBy", "Provided By:", About, Sys.AppSetting("OperatedBy", "Unknown"));
            string sURL = HttpContext.Current.Request.Url.ToString();
            sURL = sURL.Substring(0, sURL.Length - 10);
            AddLabel("About", "lblDomain", "Domain:", About, sURL);
            AddLabel("About", "lblVersion", "Pool Version:", About, Version.ToString());
            string sQTVersion = clsStaticHelper.GetInfoVersion(Sys.NetworkID);
            AddLabel("About", "lblVersionQT", "Biblepay Version:", About, sQTVersion);
            AddLabel("About", "lblEmail", "E-Mail:", About, Sys.AppSetting("OwnerEmail", "Unknown"));
            AddLabel("About", "lblNetwork", "Network:", About, Sys.NetworkID);
            double dFee = Convert.ToDouble(Sys.AppSetting("fee", "0"));
            double dFeeAnonymous = Convert.ToDouble(Sys.AppSetting("fee_anonymous", "0"));
            AddLabel("About", "lblFee", "Pool Fees:", About, (dFee * 100).ToString() + "%");
            AddLabel("About", "lblFeeAnonymous", "Anonymous User Pool Fees:", About, (dFeeAnonymous * 100).ToString() + "%");
            //  Calculate load 
            string sql = "Select count(*) As hitct, round((count(*) + 0.01) / 400 * 100, 2) As load  From work with (nolock) Where added > getdate() - 0.00006";
            double load = Sys._data.GetScalarDouble(sql, "load");
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
            clsStaticHelper.GetOSInfo("WIN", ref sWin, ref dMinerCount, ref dAvgHPS, ref dTotal);
            clsStaticHelper.GetOSInfo("LIN", ref sLin, ref dMinerCount, ref dAvgHPS, ref dTotal);
            clsStaticHelper.GetOSInfo("MAC", ref sMac, ref dMinerCount, ref dAvgHPS, ref dTotal);
            AddLabel("About", "lblwin", "Windows:", About, sWin);
            AddLabel("About", "lblLinux", "Linux:", About, sLin);
            AddLabel("About", "lblLinux", "Mac:", About, sLin);
            // Sum of Leaderboard
            sql = "Select sum(hps) hps,count(*) users from (select avg(boxhps) hps ,minername from work with (nolock) where endtime is not null group by minername) a ";
            double dHPS = Math.Round(clsStaticHelper.mPD.GetScalarDouble(sql, "hps"), 2);
            double dUsers = clsStaticHelper.mPD.GetScalarDouble(sql, "users");
            AddLabel("About", "lblHP1", "Pool Total HPS:", About, dHPS.ToString());
            AddLabel("About", "lblUS1", "Pool Miners:", About, dUsers.ToString());
            //  Total Mined coins in 24 hour period
            sql = "Select sum(Amount) a from TransactionLog where Added > getdate() - 1 and TransactionType = 'MINING_CREDIT'  and networkid = '" + Sys.NetworkID + "'";
            double dCoins24 = Math.Round(clsStaticHelper.mPD.GetScalarDouble(sql, "a"), 2);
            sql = "Select count(distinct height) b from blocks where updated > getdate() - 1 and networkid = '" + Sys.NetworkID + "'";
            double dB24 = Math.Round(clsStaticHelper.mPD.GetScalarDouble(sql, "b"), 2);
            AddLabel("About", "lblCM", "Coins Mined (24 hour period):", About, dCoins24.ToString());
            double dBP = Math.Round( (dB24 )/ 204, 2) * 100;
            AddLabel("About", "lblBM", "Blocks Mined (24 hour period):", About, dB24.ToString() + " (" + dBP.ToString() + "%)");
            // Last block Found
            sql = "select max(height) c,max(updated) d from blocks where networkid='" + Sys.NetworkID + "'";
            double dLBF = clsStaticHelper.mPD.GetScalarDouble(sql, "c");
            string sUpd = clsStaticHelper.mPD.GetScalarString(sql, "d");
            string sNarr2 = dLBF.ToString() + " (" + sUpd + ")";
            AddLabel("About", "lblUpdLb", "Last Height Mined:", About, sNarr2);
            double diff = Math.Round(GetDouble(clsStaticHelper.GetMiningInfo(Sys.NetworkID, "difficulty")), 2);
            AddLabel("About", "lbldf", "Current Difficulty:", About, diff.ToString() );
            // Pool Efficiency
            sql = "select(86400 * 7) / count(distinct height) / 60 e from blocks where updated > getdate() - 7 and networkid = '" + Sys.NetworkID + "'";
            double d7 = Math.Round(clsStaticHelper.mPD.GetScalarDoubleWithNoLog(sql, "e"), 2);
            sql = "select(86400 * 1) / count(distinct height) / 60 f from blocks where updated > getdate() - 1 and networkid = '" + Sys.NetworkID + "'";
            double d1 = Math.Round(clsStaticHelper.mPD.GetScalarDoubleWithNoLog(sql, "f"), 2);
            AddLabel("About", "ats1", "Avg Time to Solve Block (Over 7 day period):", About, d7.ToString());
            AddLabel("About", "ats2", "Avg Time to Solve Block (Over 24 hour period):", About, d1.ToString());
            double pe = Math.Round(d7 / (d1) + .01, 2) * 100;
            AddLabel("About", "ats3", "Efficiency:", About, pe.ToString() + "%");
            sql = "Select max(MasternodeCount) a from Proposal where network='" + Sys.NetworkID + "' and paidtime is null";
            double mn1 = clsStaticHelper.mPD.GetScalarDouble(sql, "a");
            AddLabel("About", "mnc1", "Masternode Count:", About, mn1.ToString());
            return About.Render(this, true);
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
            string sql = "Select * from Picture where parentid='" + sLetterGuid + "'";
            System.Data.DataTable dt = Sys._data.GetDataTable(sql);
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
            return WritePics.Render(this, false);
        }


        public WebReply WriteToOrphan()
        {
            Section Write = new Section("Write", 2, Sys, this);
            // To Recipient Name & Writer Name
            Edit geRecipient = new Edit("Write", "lblName", Sys);
            geRecipient.Type = Edit.GEType.Label;
            geRecipient.CaptionText = "Orphan Name: " + Sys.GetObjectValue("Write", "Name");
            Write.AddControl(geRecipient);
            Edit geWriter = new Edit("Write", "lblUsername", Sys);
            geWriter.Type = Edit.GEType.Label;
            geWriter.CaptionText = "Writer Username: " + Sys.GetObjectValue("Write", "Username");
            Write.AddControl(geWriter);

            //Body & Bio Caption
            Edit geLink1 = new Edit("Write", Edit.GEType.Anchor, "LinkBody", "Body (Letter Writing Tips)", Sys);
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
            geBio.URL = "http://www.compassion.com/cart/needbiopopup.aspx?gid=" + sLastOrphanID;
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
                // Upload picture control
                Edit ctlUpload = new Edit("Write", Edit.GEType.UploadControl, "btnUpload", "Add Picture", Sys);
                ctlUpload.Id = Guid.NewGuid().ToString();
                string sLetterGuid = Sys.GetObjectValue("Write", "LetterGuid");
                ctlUpload.ParentGuid = sLetterGuid;
                ctlUpload.ParentType = "Letter";
                Write.AddControl(ctlUpload);
            }

            WebReply wrWrite = Write.Render(this, true);
            WebReply wrPics = WritePics();
            wrWrite.AddWebPackages(wrPics.Packages);
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
            if (sBody.Length < 10)
            {
                return d.CreateDialog("Error", "Record Not Updated", "Sorry, Body must be longer.  ", 100, 100);
            }
            sBody = sBody.Replace("\"", "`");
            sBody = sBody.Replace("'", "`");
            string sql = "Update Letters set body='" + sBody
                + "',name='" + sTo + "',userid='"
                + Sys.UserGuid.ToString() + "',username='"
                + Sys.Username + "',added=getdate(),orphanid='" + sOrphanID
                + "' where id = '" + sLetterGuid + "'";
            Sys._data.Exec(sql);
            sql = Sys.PrepareStatement("Write", sql);
            Sys._data.Exec(sql);
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
            string sToken = clsStaticHelper.AppSetting("MouseToken", "");
            DataTable dt = clsStaticHelper.mPD.GetDataTable(sql);
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
                        + "',title='" + CleanStr(mo.title) + "',price='" + CleanStr(mo.price.ToString()) + "',Pics='" + CleanStr(mo.URL) + "' WHERE ID = '" + sItemGuid + "'";
                    clsStaticHelper.mPD.Exec(sql);
                }
            }
        }


        public WebReply btnBuy_Click()
        {
            // Verify US, Address, and Balance
            double dBal1 = 0;
            double dImmature = 0;
            clsStaticHelper.GetUserBalances(Sys.NetworkID,Sys.UserGuid,ref dBal1, ref dImmature);
            double dBalAvailable = dBal1 - dImmature;
            string sBuyGuid = Sys.LastWebObject.guid;
            string sql = "Select * from Products where id = '" + sBuyGuid + "'";
            DataTable dt = clsStaticHelper.mPD.GetDataTable(sql);
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
            double dBBP = clsStaticHelper.PriceInBBP(dPrice / 100);
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
            sql = "Select * from Users where id='" + Sys.UserGuid.ToString() + "'";
            DataTable dtU = clsStaticHelper.mPD.GetDataTable(sql);
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
                    sql = "Update users set AddressVerified='1' where id = '" + Sys.UserGuid.ToString() + "'";
                    clsStaticHelper.mPD.Exec(sql);
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
            shipAddr.first_name = clsStaticHelper.GetNameElement(sDelName, 0).ToUpper();
            shipAddr.last_name = clsStaticHelper.GetNameElement(sDelName, 1).ToUpper();
            if (shipAddr.phone_number.Length != 10 && shipAddr.phone_number.Length != 12)
            {
                wr = d.CreateDialog("Purchase_Error", "Purchase Error", "Sorry, our carrier must have a phone number that is either 10 or 12 digits long.  Please modify your phone number in account settings.", 250, 250);
                return wr;
            }

            // Create the actual order guid **********************************
            string sOrderGuid = Guid.NewGuid().ToString();
            sql = "Insert into Orders (id,added) values ('" + sOrderGuid + "',getdate())";
            clsStaticHelper.mPD.Exec(sql);

            // The user has a good address, enough BBP, go ahead and buy it
            int iHeight = Sys.GetTipHeight(Sys.NetworkID);

            clsStaticHelper.InsTxLog(Sys.Username,Sys.UserGuid,Sys.NetworkID,
                iHeight, sOrderGuid, -1 * dBBP, dBal1, dBal1-dBBP, sOrderGuid, "PURCHASE", sProductID);
            bool bTrue = clsStaticHelper.AdjustUserBalance(Sys.NetworkID,Sys.UserGuid,-1 * dBBP);

            sql = "Update Orders Set Userid='" + Sys.UserGuid.ToString() + "',ProductId='"
                + sProductID + "',Title='" + sTitle + "',Added=getdate(),Amount='" + dBBP.ToString() + "' where id = '" + sOrderGuid + "'";
            clsStaticHelper.mPD.Exec(sql);
            // Place the order, and track it
            string sToken = clsStaticHelper.AppSetting("MouseToken", "");
            Zinc.BiblePayMouse bm = new Zinc.BiblePayMouse(sToken);
            Zinc.BiblePayMouse.Product[] product = new Zinc.BiblePayMouse.Product[1];
            Zinc.BiblePayMouse.Product pNew = new Zinc.BiblePayMouse.Product();
            pNew.quantity = 1;
            pNew.product_id = sProductID;
            product[0] = pNew;
            double max_price = dPrice / 100;
            Zinc.BiblePayMouse.shipping ship1 = new Zinc.BiblePayMouse.shipping();
            ship1.max_days = 10;
            ship1.max_price = max_price;
            ship1.order_by = "price";

            Zinc.BiblePayMouse.payment_method pm1 = clsStaticHelper.TransferPaymentMethodIntoMouse();
            Zinc.BiblePayMouse.billing_address ba1 = clsStaticHelper.TransferBillingAddressIntoMouse();
            Zinc.BiblePayMouse.retailer_credentials rc1 = clsStaticHelper.TransferRetailerCredentialsIntoMouse();
            Zinc.BiblePayMouse.webhooks wh1 = new Zinc.BiblePayMouse.webhooks();
            wh1.order_failed = "";
            Zinc.BiblePayMouse.client_notes1 cn1 = new Zinc.BiblePayMouse.client_notes1();
            cn1.our_internal_order_id = "";
            wh1.order_failed = "";
            wh1.order_placed = "";
            string indep = sOrderGuid.ToString().ToUpper().Substring(0, 5);
            string sMouseId = bm.CreateOrder(indep, sRetailer, product, max_price, is_gift, "", shipAddr, ship1, pm1, ba1, rc1, wh1, cn1);
            sql = "Update Orders set MouseId = '" + sMouseId + "' where ID = '" + sOrderGuid + "'";
            clsStaticHelper.mPD.Exec(sql);
            return StoreList();
        }

        public WebReply OrdersList()
        {
            clsStaticHelper.GetOrderStatusUpdates();
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
            string sql = "Select * from Products where Pics is not null and networkid='" + Sys.NetworkID + "' order by Added";
            System.Data.DataTable dt = Sys._data.GetDataTable(sql);
            if (dt.Rows.Count > 0)
            {
                for (int iRow = 0; iRow <= dt.Rows.Count - 1; iRow++)
                {
                    string sItemGuid = dt.Rows[iRow]["id"].ToString();
                    string sURL = FirstURL(dt.Rows[iRow]["Pics"].ToString());
                    string sDesc = "<TABLE><TR><TD>Title:</td><td>" + dt.Rows[iRow]["title"].ToString() + "</td></tr>";
                    sDesc += "<TR><TD>Description:</td><td>" + dt.Rows[iRow]["product_details"].ToString() + "</td></tr>";
                    double dPrice = Convert.ToDouble(dt.Rows[iRow]["price"].ToString());
                    double dBBP = clsStaticHelper.PriceInBBP(dPrice / 100);
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
            string sql = "Select * from Picture where addedby = '" + Sys.UserGuid.ToString() + "' and parenttype='News'";
            System.Data.DataTable dt = Sys._data.GetDataTable(sql);
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
            Edit ctlUpload = new Edit("News Pictures", Edit.GEType.UploadControl, "btnNewsUpload", "Add Picture", Sys);
            ctlUpload.Id = Guid.NewGuid().ToString();
            ctlUpload.ParentGuid = ctlUpload.Id;
            ctlUpload.ParentType = "News";
            anp.AddControl(ctlUpload);
            WebReply wrAnp = anp.Render(this, true);
            WebReply wrPics = NewsPics();
            wrAnp.AddWebPackages(wrPics.Packages);
            return wrAnp;
        }

        
        public WebReply Faucet()
        {
            Section Faucet = new Section("Faucet", 1, Sys, this);
            Edit geDestination = new Edit("Faucet", "Destination", Sys);
            geDestination.CaptionText = "Destination Address:";
            geDestination.TextBoxStyle = "width:300px";
            Faucet.AddControl(geDestination);
            Random d = new Random();
            int i2 = d.Next(1000);
            int i1 = d.Next(500);
            int i3 = i1 + i2;
            string sMath = "What is " + i1.ToString() + " + " + i2.ToString() + "?";
            Edit geMath = new Edit("Faucet", "Math", Sys);
            geMath.CaptionText = sMath;
            geMath.TextBoxStyle = "width:300px";
            Sys.SetObjectValue("Faucet", "MathAnswer", i3.ToString());
            Faucet.AddControl(geMath);
            Edit geBtnSend = new Edit("Faucet", Edit.GEType.Button, "btnFaucetSend", "Send", Sys);
            Faucet.AddControl(geBtnSend);
            return Faucet.Render(this, true);
        }
        

        public WebReply Withdraw()
        {
            string sql = "Select * from Users with (nolock) where Username='" + Sys.Username + "' and deleted=0";
            System.Data.DataTable dt = Sys._data.GetDataTable(sql);
            if (dt.Rows.Count > 0)
            {
                Sys.SetObjectValue("Withdraw", "Destination", "" + dt.Rows[0]["WithdrawalAddress"].ToString());
            }
            updBalance();
            
            Section Withdraw = new Section("Withdraw", 1, Sys, this);
            Edit geAvailableBalance = new Edit("Withdraw", "Available", Sys);
            geAvailableBalance.CaptionText = "Available Balance:";
            geAvailableBalance.TextBoxAttribute = "readonly";
            geAvailableBalance.TextBoxStyle = "background-color:grey;";
            Withdraw.AddControl(geAvailableBalance);

            Edit geImmature = new Edit("Withdraw", "Immature", Sys);
            geImmature.CaptionText = "Immature Balance:";
            geImmature.TextBoxAttribute = "readonly";
            geImmature.TextBoxStyle = "background-color:grey;";
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
            clsStaticHelper.GetUserBalances(Sys.NetworkID,Sys.UserGuid,ref dBal1, ref dImmature);
            double dBalAvailable  = dBal1 - dImmature;
            Sys.SetObjectValue("Withdraw", "Available", dBalAvailable.ToString());
            Sys.SetObjectValue("Withdraw", "Immature", dImmature.ToString());
            Sys.SetObjectValue("Withdraw", "Balance", dBal1.ToString());
        }

        public WebReply btnFaucetSend_Click()
        {
            Bitnet.Client.BitnetClient bc = Sys.InitRPC(Sys.NetworkID);
            double csAmt = GetDouble(clsStaticHelper.AppSetting("faucet_amount", "12.75"));
            Dialog d = new Dialog(Sys);
            WebReply wr;
            // Get count of use by IP
            string sIP = (HttpContext.Current.Request.UserHostAddress ?? "").ToString();
            string sAddress = Sys.GetObjectValue("Faucet", "Destination");
            string sSql = "Select count(*) as ct From Faucet where network='" + Sys.NetworkID + "' and (address = '" + sAddress + "' or IP='" + sIP + "')";
            double lIpHitCount = clsStaticHelper.GetScalarDouble(sSql, "ct");
            double iAnswer = Convert.ToDouble("0" + Sys.GetObjectValue("Faucet", "MathAnswer").ToString());
            double dTheirAnswer = Convert.ToDouble("0" + Sys.GetObjectValue("Faucet", "Math").ToString());
            if (iAnswer != dTheirAnswer)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, Wrong Math Answer", 150, 150);
                return wr;
            }
            if (lIpHitCount > 0 || sIP == "")
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
                clsStaticHelper.InsTxLog(Sys.Username,Sys.UserGuid,Sys.NetworkID,iHeight, Guid.NewGuid().ToString(), csAmt, 0, 0, sDestination, "Faucet","");
                sTXID = clsStaticHelper.Z(sReqLogId,sDestination, csAmt, Sys.NetworkID);
            }
            catch (Exception ex)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, an error occurred during the withdrawal (Code: 65003 [" + ex.Message + "]) - Biblepay Server rejected destination address.", 150, 200);
                return wr;
            }

            string sql = "Insert into Faucet (id,height,transactionid,IP,amount,added,network,address) values (newid(),'" + iHeight.ToString() + "','" + sTXID.Trim() + "','" + sIP + "','" + csAmt.ToString() + "',getdate(),'" + Sys.NetworkID + "','" + sAddress + "')";
            Sys._data.Exec(sql);

            wr = d.CreateDialog("Success", "Success", "Successfully transmitted " + csAmt.ToString()
                    + " BiblePay to " + sAddress                     + ".  <p>Transaction ID: " + sTXID, 600, 200);
            return wr;
        }

        
        public WebReply btnWithdrawSend_Click()
        {
            // Send funds
            string sql = "Select * From Users where id = '" + Sys.UserGuid.ToString() + "'";
            DataTable dtUser = Sys._data.GetDataTable(sql);
            Bitnet.Client.BitnetClient bc = Sys.InitRPC(Sys.NetworkID);
            double oldBalance = 0;
            double dImmature = 0;
            clsStaticHelper.GetUserBalances(this.Sys.NetworkID,this.Sys.UserGuid.ToString(), ref oldBalance, ref dImmature);
            double csAmt = clsStaticHelper.GetDouble(Sys.GetObjectValue("Withdraw", "Amount"));
            csAmt = Math.Round(csAmt, 4);
            Dialog d = new Dialog(Sys);
            WebReply wr;
            if (csAmt > (oldBalance - dImmature))
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, Withdrawal amount of " + csAmt.ToString()
                    + " exceeds your balance of " + (oldBalance - dImmature).ToString() + ".", 150, 150);
                return wr;
            }
            if (csAmt < .01)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, Withdrawal amount is negative or almost zero.", 150, 150);
                return wr;
            }
            if (csAmt < 100)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, Withdrawal amount must be above 99 bbp.", 150, 150);
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
                    + sName + "','" + sGuid + "','" 
                    + sAddress + "','" + sRLGuid + "','"
                    + "','" + (csAmt).ToString() + "',getdate(),'" + this.Sys.NetworkID + "','" + sIP + "')";
                Sys._data.Exec(sql2);

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
                if (ParentID == "") ParentID = Sys.Organization.ToString();
            s.WhereClause = " userid='" + "" + Sys.UserGuid.ToString() + "'";
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
            w.bShowRowSelect = false;
            w.bShowRowTrash = true;
            WebReply wr = w.GetWebList(sql, "Worker List", "Worker List", "", "Miners", this, false);
            return wr;
        }

        public WebReply WorkerList_OrderByClick()
        {
            return WorkerList();
        }

        public WebReply WorkerList_Delete_Click()
        {
            string sID = Sys.LastWebObject.guid.ToString();
            string sql = "Delete from Miners where ID='" + sID + "'";
            try
            {
                if (sID.Length < 34) throw new Exception("Unable to delete worker: Record pointer error.");

                Sys._data.Exec(sql);
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
            WebReply wr = w.GetWebList(sql, "Sponsored Orphan List", "Orphans", "", "Orphans", this, false);
            WebReplyPackage wrp1 = wr.Packages[0];
            wrp1.ClearScreen = true;
            wr.Packages[0] = wrp1;
            return wr;
        }


        public WebReply OrphanList_RowClick()
        {
            string sId = Sys.LastWebObject.guid.ToString();
            string sql = "Select URL,Name from Orphans where id = '" + sId + "'";
            string sURL = Sys._data.GetScalarString(sql, "URL");
            Section s = new Section("l1", 1, Sys, this);
            Dialog d = new Dialog(Sys);
            string sCSS = "background-color:grey";
            string sBody = "<iframe style='" + sCSS + "' width=100% height=700px  src='" + sURL + "'>";
            string sNarr = Sys._data.GetScalarString(sql, "Name");
            string sNarr2 = sNarr.Replace(" ", "_");
            WebReply wr = d.CreateDialog(sNarr2, "View1", sBody, 1000, 1100);
            return wr;
        }

        public WebReply OrphanList_ContextMenu_Write()
        {
            string sOrphanID = Sys.LastWebObject.guid.ToString();
            string sql = "Select OrphanID,Name from Orphans where id = '" + sOrphanID + "'";
            Sys.SetObjectValue("Write", "Body", "");
            string sLastOrphanID = Sys._data.GetScalarString(sql, "OrphanID");
            string sOrphanName = Sys._data.GetScalarString(sql, "Name");
            // New Letter Guid
            string sLetterGuid = Guid.NewGuid().ToString();
            Sys.SetObjectValue("Write", "LetterGuid", sLetterGuid);
            string sql1 = "Insert into Letters (id) values ('" + sLetterGuid + "')";
            Sys._data.Exec(sql1);
            Sys.SetObjectValue("Write", "Name", sOrphanName);
            Sys.SetObjectValue("Write", "Username", Sys.Username);
            Sys.SetObjectValue("Write", "OrphanID", sLastOrphanID);
            return WriteToOrphan();
        }

        public WebReply OrphanLetters()
        {
            SQLDetective s = Sys.GetSectionSQL("Orphan Letters", "Letters", string.Empty);
            if (ParentID == "") ParentID = Sys.Organization.ToString();
            s.WhereClause = " len(body) > 1 and Added > getdate()-59 ";
            s.OrderBy = " added";
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
            w.AddContextMenuitem("Upvote", "Upvote", "Upvote");
            w.AddContextMenuitem("Downvote", "Downvote", "Downvote");
            WebReply wr = w.GetWebList(sql, "Outgoing Letters", "Outgoing Letters", "", "Letters", this,  false);
            WebReplyPackage wrp1 = wr.Packages[0];
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

        public WebReply OrphanLetters_RowClick()
        {
            string sOrphanID = Sys.LastWebObject.guid.ToString();
            string sql = "Select ID,Name,Username,OrphanID,Body from Letters where id = '" + sOrphanID + "'";
            string sLastOrphanID = Sys._data.GetScalarString(sql, "OrphanID");
            string sLetterGuid = Sys._data.GetScalarString(sql, "id").ToString();
            string sOrphanName = Sys._data.GetScalarString(sql, "name").ToString();
            string sUsername = Sys._data.GetScalarString(sql, "username").ToString();
            string sBody = Sys._data.GetScalarString(sql, "Body");
            sBody.Replace("`", "'");
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

        public string GetFieldFromObjectBasedOnLastClick(string sFieldName, string sObjName)
        {
            string sId = "";
            try
            {
                sId = Sys.LastWebObject.guid.ToString();
            }
            catch (Exception) { }
            string sql = "Select " + sFieldName + " From " + sObjName + " where id = '" + sId + "'";
            DataTable dt = Sys._data.GetDataTable(sql);
            if (dt.Rows.Count > 0)
            {
                string sValue = (dt.Rows[0][sFieldName] ?? String.Empty).ToString();
                return sValue;
            }
            return "NA";
        }

        public WebReply ClipDialog(string sClipText, string js,  string sDialogBody)
        {
            Dialog d = new Dialog(Sys);
            WebReply wr = d.CreateDialogWithMoreJavascript("dialog1", sDialogBody, sClipText, 700, 210, js);
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

        public void ProcessProposals()
        {
            string sql = "Select * from Proposal where paidtime is null and network='" + Sys.NetworkID + "'";
            DataTable dt = Sys._data.GetDataTable(sql);
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

                // if the item has no sPrepareTxId
                if (sPrepareTxId == "" && sHex != "")
                {
                    clsStaticHelper.GetWLA(Sys.NetworkID, 30);
                    // Submit the gobject to the network 
                    string sArgs = "0 1 " + unixStartTimeStamp.ToString() + " " + sHex;
                    string sCmd1 = "gobject prepare " + sArgs;
                    object[] oParams = new object[5];
                    oParams[0] = "prepare";
                    oParams[1] = "0";
                    oParams[2] = "1";
                    oParams[3] = unixStartTimeStamp.ToString();
                    oParams[4] = sHex;
                    string sPrepareTxid = clsStaticHelper.GetGenericInfo3(Sys.NetworkID, "gobject", oParams);
                    string sql4 = "Update Proposal Set PrepareTxId='" + sPrepareTxid + "',SubmitTime=null,Submitted=0,gobjectid=null,SubmitTxId=null,PrepareTime=getdate() where id = '" + sId + "'";
                    Sys._data.Exec(sql4);
                    clsStaticHelper.GetWL(Sys.NetworkID);
                }

                // if the object is unpaid and has a triggertxid, find out if its been paid
                if (sTriggerTxId.Length > 10 && dAbsoluteYesCount > 0 && sPaidTime == "" && dHeight > 0)
                {
                    string sSuperBlockTxId = clsStaticHelper.GetBlockTx(Sys.NetworkID, "showblock", (int)dHeight, 0);
                    // Now see if this rec address and amount is in the block
                    string sRawTx = clsStaticHelper.GetRawTransaction(Sys.NetworkID, sSuperBlockTxId);
                    if (sRawTx.Contains(sRecAddr) && sRawTx.Contains(dAmt.ToString()))
                    {
                        // This tx was paid in that superblock, update it
                        sql = "update proposal set PaidTime=getdate(),SuperBlockTxId='" + sSuperBlockTxId + "' where id = '" + sId + "'";
                        Sys._data.Exec(sql);
                    }
                    else
                    {
                        // if height > dHeight, and it has not been paid, call the retrigger by removing the TriggerTxId 
                        int nHeight = Convert.ToInt32(Sys.GetTipHeight(Sys.NetworkID).ToString());
                        if (nHeight > (dHeight + 4))
                        {
                            sql = "update proposal set triggertxid=null,height=null,triggertime=null where id = '" + sId + "'";
                            Sys._data.Exec(sql);
                        }
                    }
                }
                // If this object has a supermajority vote, and its TriggerTxId is null, try to place it in the budget
                bool bSupermajority = dAbsoluteYesCount > (dMasterNodeCount * .24) && dAbsoluteYesCount > 0 && dMasterNodeCount > 0;
                if (sTriggerTxId == "" && bSupermajority && dBudgetable==1)
                {
                    // First find the next superblock for this network
                    double dNextSuperblock = Convert.ToDouble(clsStaticHelper.GetGenericInfo(Sys.NetworkID, "getgovernanceinfo", "nextsuperblock", "result").ToString());
                    if (dNextSuperblock > 0) dNextSuperblock += 25;
                    sql = "Select * from Proposal where Network='" + Sys.NetworkID + "' and AbsoluteYesCount > 0 and TriggerTxID is null and gobjectid is not null and len(receiveaddress) > 1 and amount is not null";
                    DataTable dtSuperblock = Sys._data.GetDataTable(sql);
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
                        string sTriggerHex = clsStaticHelper.GetGenericInfo2(Sys.NetworkID, "gobject", oParams, "Hex");
                        // Now we need to submit the trigger as a gobject, and if we receive the Sanctuary txid, (must be from a masternode) we can update all these records again
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

                                string sTriggerTxid = clsStaticHelper.GetGenericInfo3(Sys.NetworkID, "gobject", oParamTrigger);
                                for (int i = 0; i < dtSuperblock.Rows.Count; i++)
                                {
                                    string sProposalGuid = dtSuperblock.Rows[i]["id"].ToString();
                                    string sql3 = "Update proposal set Height='" + dNextSuperblock.ToString() + "',TriggerTxId='" + sTriggerTxid + "',triggertime=getdate() where id = '"
                                        + sProposalGuid + "'";
                                    Sys._data.Exec(sql3);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            clsStaticHelper.Log(" err creating superblock " + ex.Message);
                        }
                    }
                }

                // if this object has been submitted, try to get its Sanctified votes:
                if (gObjectID.Length > 10)
                {
                    // Grab votes for objectid 
                    object[] oParams = new object[2];
                    oParams[0] = "get";
                    oParams[1] = gObjectID;
                    clsStaticHelper.VoteType v = clsStaticHelper.GetGenericInfo4(Sys.NetworkID, "gobject", oParams);
                    if (v.Success && v.AbstainCount >= 0)
                    {
                        // Update the database with the Sanctuary vote results:
                        sql = "update proposal set absoluteYesCount='" + v.AbsoluteYesCount.ToString() + "',YesCount='" + v.YesCount.ToString() + "',NoCt='" 
                            + v.NoCount.ToString() + "',AbstainCount='" + v.AbstainCount.ToString() + "' where id = '" + dt.Rows[ii]["id"].ToString() + "'";
                        Sys._data.Exec(sql);
                    }
                }

                // if this proposal is not submitted, lets submit it and grab the SubmitTXID
                if (sSubmitTxId == ""  && sPrepareTxId.Length > 10)
                {
                    // Submit the gobject to the network - gobject submit parenthash revision time datahex collateraltxid
                    clsStaticHelper.GetWLA(Sys.NetworkID, 20);
                    string sArgs = "0 1 " + dStartStamp.ToString() + " " + sHex + " " + sPrepareTxId;
                    string sCmd1 = "gobject submit " + sArgs;
                    object[] oParams = new object[6];
                    oParams[0] = "submit";
                    oParams[1] = "0";
                    oParams[2] = "1";
                    oParams[3] = dStartStamp.ToString();
                    oParams[4] = sHex;
                    oParams[5] = sPrepareTxId;
                    sSubmitTxId = clsStaticHelper.GetGenericInfo3(Sys.NetworkID, "gobject", oParams);
                    if (sSubmitTxId.Length > 20)
                    {
                        // Update the record allowing us to know this has been submitted
                        sql = "Update Proposal set Submitted=1,SubmitTime=GetDate(),Gobjectid='" + sSubmitTxId + "',SubmitTxId='" + sSubmitTxId + "' where id = '" + dt.Rows[ii]["id"].ToString()  + "'";
                        Sys._data.Exec(sql);
                    }
                    clsStaticHelper.GetWL(Sys.NetworkID);
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
            ProcessProposals();
            SQLDetective s = Sys.GetSectionSQL("Proposal List", "Proposal", string.Empty);
            s.WhereClause = " network='" + Sys.NetworkID + "' and PaidTime is null and TriggerTxId is null";
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);

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
            WebReply wrChart = ProposalFundingChart();
            wr.AddWebPackages(wrChart.Packages);
            return wr;
        }

        public WebReply FundedList()
        {
            SQLDetective s = Sys.GetSectionSQL("Funded List", "Proposal", string.Empty);
            s.WhereClause = " network='" + Sys.NetworkID + "' and PaidTime is not null";
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
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
            Section ProposalAdd = new Section("Add Proposal", 1, Sys, this);
            Edit geName = new Edit("Add Proposal", "Proposal Name", Sys);
            geName.CaptionText = "Proposal Name";
            // Gobject format in Wallet: [["proposal",{"end_epoch":"1","name":"2","payment_address":"3","payment_amount":"444","start_epoch":"5","type":1,"url":"7"}]]
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
            bool bValid = Sys.ValidateBiblepayAddress(sFRA, Sys.NetworkID);
            if (!bValid) sErr = "Invalid receiving address.";
            if (Convert.ToDouble("0" + sAmount) < 100)
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

            // gobject prepare 0 1 EPOCH_TIME HEX
            int unixEndTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            int unixStartTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            string sType = "1"; //Proposal
            string sArgs = unixEndTimestamp.ToString() + " " + sName + " " + sFRA + " " + sAmount + " " + unixStartTimestamp.ToString() + " " + sType + " " + sURL;
            string sCmd1 = "gobject serialize " + sArgs;
            object[] oParams = new object[8];
            oParams[0] = "serialize";
            oParams[1] = unixEndTimestamp.ToString();
            oParams[2] = sName;
            oParams[3] = sFRA;
            oParams[4] = sAmount;
            oParams[5] = unixStartTimestamp.ToString();
            oParams[6] = sType;
            oParams[7] = sURL;
            string sHex = clsStaticHelper.GetGenericInfo2(Sys.NetworkID, "gobject", oParams, "Hex");
            
            if (sHex.Length < 10)
            {
                // Throw an error
                sErr = "Gobject Create proposal failed in step [HEX].";
            }
            
            // Insert the row into proposals
            string sql = "Insert Into Proposal (id,UserId,UserName,URL,name,receiveaddress,amount,unixstarttime,unixendtime,preparetxid,added,updated,hex,network,prepared,preparetime) "
                + " values (newid(),'" + Sys.UserGuid.ToString() + "','" + Sys.Username + "','" + sURL + "','" 
                + sName + "','" + sFRA + "','" + sAmount + "','" + unixStartTimestamp.ToString() 
                + "','" + unixEndTimestamp.ToString() 
                + "',null,getdate(),getdate(),'" + sHex + "','" + Sys.NetworkID + "',1,getdate())";

            Sys._data.Exec(sql);
            // update the Sanctuary Count
            double dMNCount = clsStaticHelper.GetMasternodeCount(Sys.NetworkID);
            sql = "Update proposal set MasternodeCount='" + dMNCount.ToString() + "' where network='" + Sys.NetworkID + "' and paidtime is null";
            if (dMNCount > 1) clsStaticHelper.mPD.Exec(sql);
            // redirect user to the list
            return ProposalList();
        }


        public WebReply LinkAdd()
        {
            //url,ppc,budget
            Section LinkAdd = new Section("Add Link", 1, Sys, this);
            Edit eURL = new Edit("Add Link", "URL", Sys);
            eURL.CaptionText = "URL:";
            eURL.TextBoxStyle = "width:650px";

            LinkAdd.AddControl(eURL);
            Edit eNotes = new Edit("Add Link", "Notes", Sys);
            eNotes.CaptionText = "Notes:";
            eNotes.TextBoxStyle = "width:800px";
            LinkAdd.AddControl(eNotes);

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

        public WebReply btnLinkSave_Click()
        {
            string sNotes = Sys.Substring2(Sys.GetObjectValue("Add Link", "Notes"), 999);
            double dPPC = Sys.GetObjectDouble("Add Link", "PPC");
            double dBudget = Sys.GetObjectDouble("Add Link", "Budget");
            string sURL = Sys.GetObjectValue("Add Link", "URL");
            sURL = sURL.Replace("[amp]", "&");
            // Construct the Tiny URL:

            Dialog d = new Dialog(Sys);
            string sUserName = "";
            double Balance = clsStaticHelper.GetUserBalance(Sys.UserGuid.ToString(), Sys.NetworkID, ref sUserName);
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
            string sSite = clsStaticHelper.AppSetting("WebSite", "http://pool.biblepay.org/");
            string sShortURL = sSite + "" + "Action.aspx?link=" + sPrefix;
            string sql = "Insert Into Links (id,Notes,Clicks,OriginalURL,URL,Added,UserId,PaymentPerClick,Budget) values ('" + sID + "','" + sNotes + "',0,'" + sURL 
                + "','" + sShortURL + "',getdate(),'" + Sys.UserGuid.ToString() 
                + "','" + dPPC.ToString() + "','" + dBudget.ToString() + "')";
            try
            {
                Sys._data.Exec(sql);
            }
            catch (Exception ex)
            {
                WebReply wr = d.CreateDialog("Error", "Error while Adding Link",
                    "Error while adding Link: " + ex.Message.ToString(), 240, 200);
                return wr;
            }
            return LinkList();
        }


        public WebReply LinkList()
        {
            SQLDetective s = Sys.GetSectionSQL("Link List", "Links", string.Empty);
            if (ParentID == "") ParentID = Sys.Organization.ToString();
            s.WhereClause = " 1=1";
            s.OrderBy = " Added ";
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
            w.URLDefaultValue = "Display";
            w.AddContextMenuitem("CopyLink", "Copy Link", "Copy Link");
            WebReply wr = w.GetWebList(sql, "Link List", "Link List", "", "Links", this, false);
            return wr;
        }

        public WebReply LinkList_ContextMenu_CopyLink()
        {
            string sValue = GetFieldFromObjectBasedOnLastClick("URL", "Links");
            string sCmd = sValue;
            string js = GetJSForClipCopy(sCmd);
            string sPre = (sValue.Length > 5) ? sCmd : "Unable to locate URL.";
            return ClipDialog(sPre, js, "Copy URL");
        }

        public WebReply WorkerAdd()
        {
            Section WorkerAdd = new Section("Add Worker", 1, Sys, this);
            Edit geWorkerUsername = new Edit("Add Worker", "Username", Sys);
            geWorkerUsername.CaptionText = "Worker Username:";
            WorkerAdd.AddControl(geWorkerUsername);
            Edit geNotes = new Edit("Add Worker", "Notes", Sys);
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
            string sql = "Insert Into Miners (id,UserId,Username,Notes,updated,added) values (newid(),'" + Sys.UserGuid.ToString() + "',@Username,"
                         + "'" + Sys.PurifySQLStatement(sNotes) + "',getdate(),getdate())";
            sql = Sys.PrepareStatement("Add Worker", sql);
            try
            {
                Sys._data.ExecWithThrow(sql, false);
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
            return Sys.Redirect("Home",this);
        }
    }
}