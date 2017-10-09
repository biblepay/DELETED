using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.DataVisualization.Charting;

namespace BiblePayPool2018
{
    public class Home : USGDGui
    {
        public Home(SystemObject S)
            : base(S)
        { }

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

        public WebReply TxHistory()
        {
            string sql = "Select top 512 ID,TransactionType,Destination,Amount,NewBalance,Updated,Height,Notes From TransactionLog where "
                + " NetworkID = '" + Sys.NetworkID + "' AND UserID='" + Sys.UserGuid.ToString() + "' ORDER BY ";
            
            SQLDetective s = Sys.GetSectionSQL("Transaction History", "TransactionLog", string.Empty);
            if (s.OrderBy.Contains("Updated"))
            {
                s.OrderBy = " height desc";
            }
            sql += s.OrderBy;
            Weblist w = new Weblist(Sys);
            w.bShowRowHighlightedByUserName = false;
            w.bSupportCloaking = false;
            WebReply wr = w.GetWebList(sql, "Transaction History","Transaction History", "", "TransactionLog", this, false);
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
            if (Sys.Username != Sys.AppSetting("AdminUser",""))
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
            string sNarr = sName + " - " +sPage.ToString();
            Section s = new Section("l1", 1, Sys, this);
            Dialog d = new Dialog(Sys);
            string sBody = "<img width=900 height=1000 src='" + sURL + "'>";
            string sNarr2 = sNarr.Replace(" ", "_");
            WebReply wr =  d.CreateDialog(sNarr2,"View1",sBody, 1000, 1100);
            return wr;
        }

        public WebReply BlockDistribution_RowClick()
        {
            string sId = Sys.LastWebObject.guid.ToString();
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
            string sql = "Select top 512 block_distribution.id,users.username, block_distribution.height, block_distribution.block_subsidy, "
                + " block_distribution.subsidy, block_distribution.paid, block_distribution.hps, block_distribution.PPH, "
                + " block_distribution.updated,  users.cloak  from block_distribution "
                + " inner join Users on users.id = block_distribution.userid WHERE Block_Distribution.NetworkID='" + Sys.NetworkID + "' order by ";

            SQLDetective s = Sys.GetSectionSQL("Block Distribution View", "Block_Distribution", string.Empty);
            if (s.OrderBy.Contains("Updated"))
            {
                s.OrderBy = " height desc,subsidy desc";
            }
            sql += s.OrderBy;
            Weblist w = new Weblist(Sys);
            w.bShowRowHighlightedByUserName = true;
            w.bSupportCloaking = true;
            WebReply wr = w.GetWebList(sql, "Block Distribution View", "Block Distribution View", "", "Block_Distribution", this, false);
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
            s.WhereClause = " username='" + Sys.Username  + "'";
            string sql = s.GenerateQuery();
            Weblist w = new Weblist(Sys);
            WebReply wr = w.GetWebList(sql, "My Leaderboard", "My Leaderboard", "", "Leaderboard", this, false);
            return wr;
        }


        public WebReply OrphanChartArea()
        {
            Chart c = new Chart();
            Series s = new Series("ChartOrphanHistory");
            s.ChartType = System.Web.UI.DataVisualization.Charting.SeriesChartType.Column;
            c.ChartAreas.Add("OrphanChartArea");
            s.Points.AddXY("FundsRaised", 6125);
            s.Points.AddXY("ExpensePaid", 6745);
            s.LabelForeColor = System.Drawing.Color.Green;
            s.Color = System.Drawing.Color.Green;
            c.ChartAreas[0].AxisX.LabelStyle.ForeColor = System.Drawing.Color.LightGreen;
            c.ChartAreas[0].AxisY.LabelStyle.ForeColor = System.Drawing.Color.LightGreen;
            c.ChartAreas[0].BackColor = System.Drawing.Color.Black;
            c.Series.Add(s);
            c.Titles.Add("Historical Active Orphan Sponsorship Count");
            c.Titles[0].ForeColor = System.Drawing.Color.Green;
            c.BackColor = System.Drawing.Color.Black;
            c.ForeColor = System.Drawing.Color.Green;
            s.LabelForeColor = System.Drawing.Color.Green;
            string sFileName = Guid.NewGuid().ToString() + ".png";
            Section s1 = new Section("Chart", 1, Sys, this);
            GodEdit geWeather = new GodEdit("Chart", GodEdit.GEType.Image, "W1", "", Sys);
            geWeather.URL = "images/" + sFileName;
            s1.AddControl(geWeather);
            GodEdit geLink1 = new GodEdit("Chart", GodEdit.GEType.Anchor, "Link1", "Drill in to Orphan History", Sys);
            s1.AddControl(geLink1);
            return s1.Render(this, false);
        }
        
        public WebReply Link1_Click()
        {
            return OrphanChartArea();
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
            WebReply wr = d.CreateDialog("Network","Switch Network", "Network changed to Main", 0, 0);
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
                double dBalance = Convert.ToDouble(dt.Rows[0]["Balance" + Sys.NetworkID]);
                Sys.SetObjectValue("Account Edit", "Balance", dBalance.ToString());
            }

            Section AccountEdit = new Section("Account Edit", 1, Sys, this);
            GodEdit geDefaultWithdrawalAddress = new GodEdit("Account Edit", "DefaultWithdrawalAddress", Sys);
            geDefaultWithdrawalAddress.CaptionText = "Default Withdrawal Address:";
            geDefaultWithdrawalAddress.TextBoxStyle = "width:420px";
            AccountEdit.AddControl(geDefaultWithdrawalAddress);

            GodEdit gePassword = new GodEdit("Account Edit", GodEdit.GEType.Password, "Password", "Password:", Sys);
            AccountEdit.AddControl(gePassword);
            if (gePassword.TextBoxValue.Length > 0  && gePassword.TextBoxValue.Length < 3 && Sys.GetObjectValue("AccountEdit", "Caption1") == String.Empty)
            {
                gePassword.ErrorText = "Invalid Username or Password";
            }
            
            GodEdit geEmail = new GodEdit("Account Edit", "Email", Sys);
            geEmail.CaptionText = "Email:";
            AccountEdit.AddControl(geEmail);

            GodEdit geCloak = new GodEdit("Account Edit", "Cloak", Sys);
            geCloak.CaptionText = "Cloak Users and Miners:";
            geCloak.Type = GodEdit.GEType.CheckBox;
            AccountEdit.AddControl(geCloak);

            GodEdit geBalance = new GodEdit("Account Edit", "Balance", Sys);
            geBalance.CaptionText = "Balance:";
            geBalance.Type = GodEdit.GEType.Text;
            geBalance.TextBoxAttribute = "readonly";
            geBalance.TextBoxStyle = "background-color:grey;";
            AccountEdit.AddControl(geBalance);
            GodEdit geBtnSave = new GodEdit("Account Edit", GodEdit.GEType.Button, "btnAccountSave", "Save", Sys);
            AccountEdit.AddControl(geBtnSave);
            return AccountEdit.Render(this, true);
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
            string sql = "Update Users set Updated=getdate(), Cloak='" + iCloak.ToString() + "', Email=@Email, WithdrawalAddress=@DefaultWithdrawalAddress where username='"
                 + Sys.Username + "' and deleted=0";
            sql = Sys.PrepareStatement("Account Edit",sql);
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
                WebReply wr3= d.CreateDialog("Error", "Password does not meet Validation Requirements", "Sorry, Password too short.", 150, 150);
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
            WebReply wr = d.CreateDialog("Account", "Account Edit",sNarr, 0, 0);
            return wr;
        }


        private void AddLabel(string sSectionName, string sControlName,string sCaption, Section s, string sValue)
        {
            GodEdit lbl1 = new GodEdit(sSectionName, sControlName, Sys);
            lbl1.Type = GodEdit.GEType.Label;
            lbl1.CaptionText = sCaption;
            lbl1.TextBoxValue = sValue;
            s.AddControl(lbl1);
        }

        public WebReply Vote_Click(string sVote)
        {
            string sId = Sys.LastWebObject.guid;
            if (sId.Length==0)
            {
                Dialog d = new Dialog(Sys);
                WebReply wr = d.CreateDialog("Error", "Vote Failure", "Failed to access vote record.", 100, 100);
                return wr;
            }
            string sql = "Delete from Votes where letterid = '" + sId + "' and userid = '" + Sys.UserGuid.ToString() + "'";
            Sys._data.Exec(sql);
            double dUpvote = (sVote == "upvote") ? 1 : 0;
            double dDownvote = (sVote == "downvote") ? 1 : 0;
            sql = "insert into votes (id,letterid,userid,upvote,downvote) values (newid(),'" + sId
                + "','" + Sys.UserGuid.ToString() + "'," + dUpvote.ToString()
                + "," + dDownvote.ToString() + ")";
            Sys._data.Exec(sql);
            sql = "update Letters set upvote=(Select sum(Upvote) from Votes where letterid='" + sId
                + "'), downvote = (Select sum(downvote) from votes where letterid='" + sId + "') where id='" + sId + "'";
            Sys._data.Exec(sql);
            sql = "update letters set Approved=0 where Upvote < 7";
            Sys._data.Exec(sql);
            sql = "update letters set Approved=1 where Upvote >= 7";
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
            AddLabel("Announce", "lblA", "Announcements:", Announce, a);
            AddLabel("Announce", "lblD", "<p>", Announce, "");
            // Outgoing letter enforcement fees
            AddLabel("Announce", "lblOC1", "<p>", Announce, "");

            double dLWF = Convert.ToDouble(Sys.AppSetting("fee_letterwriting", "0"));

            double dUpvotedCount = clsStaticHelper.GetUpvotedLetterCount();
            double dRequiredCount = clsStaticHelper.GetRequiredLetterCount();

            AddLabel("Announce", "lblOC", "Outbound Approved Letter Count:", Announce, dUpvotedCount.ToString());

            AddLabel("Announce", "lblRQC", "Outbound Required Letter Count:", Announce, dRequiredCount.ToString());
            double dFee = (dUpvotedCount < dRequiredCount ? dLWF : 0);
            AddLabel("Announce", "lblLetterFees", "Non-Participating Letter Fees:", Announce, (dFee*100).ToString() + "%");
            double dMyLetterCount = clsStaticHelper.GetMyLetterCount(Sys.UserGuid.ToString());
            double dMyLetterFee = (dMyLetterCount == 0 ? dFee : 0);
            AddLabel("Announce", "lblLetterFees2", "My (Personal) Letter Fees:", Announce, (dMyLetterFee * 100).ToString() + "%");
            double dBounty = clsStaticHelper.GetTotalBounty();
            double dIndBounty = Math.Round(dBounty / (dRequiredCount+.01), 2);
            AddLabel("Announce", "lblLetterBounty", "Current Approved Letter Writing Bounty:", Announce, dIndBounty.ToString());

            // Balance
            double dBal = 0;
            double dImm = 0;
            Sys.GetUserBalances(ref dBal, ref dImm);
            AddLabel("Announce", "lblBal", "Total Balance:", Announce, dBal.ToString());
            AddLabel("Announce", "lblImm", "Immature Balance:", Announce, dImm.ToString());
            AddLabel("Announce", "lblP", "<p>", Announce, "");
            AddLabel("Announce", "lblOC5", "<p>", Announce, "");
            AddLabel("Announce", "lbl71", "Note:", Announce, "** SOME FUNCTIONS (such as Letter Writing) Require a Right Click from the Web List **");
            bool bAcc = (HttpContext.Current.Request.Url.ToString().ToUpper().Contains("ACCOUNTABILITY"));
            Sys.lOrphanNews++;
            bool bShowOrphanNews = (bAcc || Sys.lOrphanNews == 1);
            if (bShowOrphanNews)
            {
               string sOrphanNews= "<xdiv style='width:400;height:400'><iframe src='https://player.vimeo.com/video/228287944'></xdiv>";
               AddLabel("Announce", "vidCompassion", "Orphan News:", Announce, sOrphanNews);
            }
            return Announce.Render(this, true);
        }


        public WebReply About()
        {
            Section About = new Section("About", 1, Sys, this);
            GodEdit lblInfo = new GodEdit("About", "lblInfo", Sys);
            lblInfo.Type = GodEdit.GEType.Label;
            lblInfo.CaptionText = "Pool Information:";
            About.AddControl(lblInfo);
            AddLabel("About", "lblProvidedBy", "Provided By:", About, Sys.AppSetting("OperatedBy","Unknown"));
            string sURL = HttpContext.Current.Request.Url.ToString();

            sURL = sURL.Substring(0, sURL.Length - 10);
            AddLabel("About", "lblDomain", "Domain:", About, sURL);
            AddLabel("About", "lblVersion", "Pool Version:", About, Version.ToString());
            string sQTVersion = clsStaticHelper.GetInfoVersion(Sys.NetworkID);
            AddLabel("About", "lblVersionQT", "Biblepay Version:", About, sQTVersion);

            AddLabel("About", "lblEmail", "E-Mail:", About,Sys.AppSetting("OwnerEmail","Unknown"));
            AddLabel("About", "lblNetwork", "Network:", About, Sys.NetworkID);

            double dFee = Convert.ToDouble(Sys.AppSetting("fee","0"));
            double dFeeAnonymous = Convert.ToDouble(Sys.AppSetting("fee_anonymous","0"));
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
            AddLabel("About", "lblUS1", "Pool Miners:",About, dUsers.ToString());
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
                    GodEdit gePics = new GodEdit("Write", "lbl" + iRow.ToString(), Sys);
                    gePics.Type = GodEdit.GEType.Label;
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
            GodEdit geRecipient = new GodEdit("Write", "lblName", Sys);
            geRecipient.Type = GodEdit.GEType.Label;
            geRecipient.CaptionText = "Orphan Name: " + Sys.GetObjectValue("Write", "Name");
            Write.AddControl(geRecipient);
            GodEdit geWriter = new GodEdit("Write", "lblUsername", Sys);
            geWriter.Type = GodEdit.GEType.Label;
            geWriter.CaptionText = "Writer Username: " + Sys.GetObjectValue("Write", "Username");
            Write.AddControl(geWriter);

            //Body & Bio Caption
            GodEdit geLink1 = new GodEdit("Write", GodEdit.GEType.Anchor, "LinkBody", "Body (Letter Writing Tips)", Sys);
            geLink1.MaskColumn1 = true;
            Write.AddControl(geLink1);
            
            GodEdit geBioLabel = new GodEdit("Write", "lblBiography", Sys);
            geBioLabel.Type = GodEdit.GEType.Label;
            geBioLabel.CaptionText = "Biography:";
            Write.AddControl(geBioLabel);

            //Body & Bio
            GodEdit geBody = new GodEdit("Write", "Body", Sys);
            geBody.CaptionText = "";
            geBody.Type = GodEdit.GEType.TextArea;
            geBody.Height = "500px";
            geBody.cols = 90;
            geBody.TdWidth = "width='50%' ";
            geBody.rows = 24;
            geBody.MaskColumn1 = true;
            Write.AddControl(geBody);
            GodEdit geBio = new GodEdit("Write", "Biography", Sys);
            geBio.CaptionText = "Biography";
            geBio.Type = GodEdit.GEType.IFrame;
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
                GodEdit geSave = new GodEdit("Write", GodEdit.GEType.Button, "btnWriteSave", "Save", Sys);
                Write.AddControl(geSave);
                // Upload picture control
                GodEdit ctlUpload = new GodEdit("Write", GodEdit.GEType.UploadControl, "btnUpload", "Add Picture", Sys);
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
                    GodEdit gePics = new GodEdit("News - Pictures", "lbl" + iRow.ToString(), Sys);
                    gePics.Type = GodEdit.GEType.Label;
                    gePics.CaptionText = sRow;
                    NewsPics.AddControl(gePics);
                }
            }

            return NewsPics.Render(this, false);
        }



        public WebReply AddNewsPicture()
        {
            Section anp = new Section("News Pictures", 2, Sys, this);
            // Upload picture control
            GodEdit ctlUpload = new GodEdit("News Pictures", GodEdit.GEType.UploadControl, "btnNewsUpload", "Add Picture", Sys);
            ctlUpload.Id = Guid.NewGuid().ToString();
            ctlUpload.ParentGuid = ctlUpload.Id;
            ctlUpload.ParentType = "News";
            anp.AddControl(ctlUpload);
            WebReply wrAnp = anp.Render(this, true);
            WebReply wrPics = NewsPics();
            wrAnp.AddWebPackages(wrPics.Packages);
            return wrAnp;
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
            GodEdit geAvailableBalance = new GodEdit("Withdraw", "Available", Sys);
            geAvailableBalance.CaptionText = "Available Balance:";
            geAvailableBalance.TextBoxAttribute = "readonly";
            geAvailableBalance.TextBoxStyle = "background-color:grey;";
            Withdraw.AddControl(geAvailableBalance);

            GodEdit geImmature = new GodEdit("Withdraw", "Immature", Sys);
            geImmature.CaptionText = "Immature Balance:";
            geImmature.TextBoxAttribute = "readonly";
            geImmature.TextBoxStyle = "background-color:grey;";
            Withdraw.AddControl(geImmature);

            GodEdit geAmount = new GodEdit("Withdraw", "Amount", Sys);
            geAmount.CaptionText = "Amount to Withdraw:";
            Withdraw.AddControl(geAmount);

            GodEdit geDestination = new GodEdit("Withdraw", "Destination", Sys);
            geDestination.CaptionText = "Destination Address:";
            geDestination.TextBoxStyle = "width:300px";

            Withdraw.AddControl(geDestination);
            GodEdit geBtnSend = new GodEdit("Withdraw", GodEdit.GEType.Button, "btnWithdrawSend", "Send", Sys);
            Withdraw.AddControl(geBtnSend);
            return Withdraw.Render(this, true);
        }

        private void updBalance()
        {
            //Compute immature balance
            double dBal1 = 0;
            double dImmature = 0;
            Sys.GetUserBalances(ref dBal1, ref dImmature);
            double dBalAvailable  = dBal1 - dImmature;
            Sys.SetObjectValue("Withdraw", "Available", dBalAvailable.ToString());
            Sys.SetObjectValue("Withdraw", "Immature", dImmature.ToString());
            Sys.SetObjectValue("Withdraw", "Balance", dBal1.ToString());
        }


        public WebReply btnWithdrawSend_Click()
        {
            // Send funds, log to transaction log
            Sys.InitializeNewBitnet(Sys.NetworkID);
            double oldBalance = 0;
            double dImmature = 0;
            Sys.GetUserBalances(ref oldBalance, ref dImmature);
            double csAmt = Convert.ToDouble("" + Sys.GetObjectValue("Withdraw", "Amount"));
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
            if (csAmt < 1)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, Withdrawal amount must be above 1 bbp.", 150, 150);
                return wr;
            }

            string sTXID = "";

            if (Sys.GetObjectValue("Withdraw", "Destination").Length < 10)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, Invalid Destination Address", 150, 150);
                return wr;
            }
            // Validate Address
            bool bValid = Sys.ValidateBiblepayAddress(Sys.GetObjectValue("Withdraw", "Destination"), Sys.NetworkID);
            if (!bValid)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, Invalid Destination Address (Code: 65002)", 150, 150);
                return wr;
            }

            string sDestination = Sys.GetObjectValue("Withdraw", "Destination");
            int iHeight = Sys.GetTipHeight(Sys.NetworkID);
            try
            {
                sTXID = Sys.SendMoney(sDestination, csAmt, Sys.NetworkID);
            }
            catch (Exception ex)
            {
                wr = d.CreateDialog("Withdrawal_Error", "Withdrawal Error", "Sorry, an error occurred during the withdrawal (Code: 65003 [" + ex.Message + "]) - Biblepay Server rejected destination address.", 150, 200);
                return wr;
            }

            double newBalance = oldBalance - csAmt;

            string sql = "Update Users set Balance" + Sys.NetworkID + " = '" + (Math.Round(newBalance, 2)).ToString()
                + "' where username='" + Sys.Username + "' and deleted=0";
            Sys._data.Exec(sql);
            sql = "Insert into TransactionLog (id,height,transactionid,username,userid,transactiontype,destination,amount,oldbalance,newbalance,added,updated,networkid,notes) "
                 + " values (newid(),'" + iHeight.ToString() + "','" + sTXID.Trim()
                 + "','" + Sys.Username + "','" + Sys.UserGuid.ToString() + "','withdrawal','"
                 + Sys.GetObjectValue("Withdraw", "Destination")
                 + "','" + csAmt.ToString()
                 + "','" + (Math.Round(oldBalance, 2)).ToString()
                 + "','" + (Math.Round(newBalance, 2)).ToString()
                 + "',getdate(),getdate(),'" + Sys.NetworkID + "','')";

            Sys._data.Exec(sql);
            updBalance();

            wr = d.CreateDialog("Success", "Success", "Successfully transmitted " + csAmt.ToString()
                    + " BiblePay to " + Sys.GetObjectValue("Withdraw", "Destination")
                    + ".  <p>Transaction ID: " + sTXID
                    + "<p>Your new balance is: " + newBalance.ToString(), 600, 200);

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
            string sBody = "<iframe  style='" + sCSS + "'   width=100% height=700px  src='" + sURL + "'>";
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
            s.WhereClause = " len(body) > 1 ";
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
            // Persist Letter GUID
            Sys.SetObjectValue("Write", "LetterGuid", sLetterGuid);
            Sys.SetObjectValue("Write", "Name", sOrphanName);
            Sys.SetObjectValue("Write", "Username", sUsername);
            Sys.SetObjectValue("Write", "OrphanID", sLastOrphanID);
            return WriteToOrphan();
        }

        public WebReply ProposalAdd()
        {
            Section ProposalAdd = new Section("Add Proposal", 1, Sys, this);
            GodEdit geName = new GodEdit("Add Proposal", "Proposal Name", Sys);
            geName.CaptionText = "Proposal Name";
            // Gobject format in Wallet: [["proposal",{"end_epoch":"1","name":"2","payment_address":"3","payment_amount":"444","start_epoch":"5","type":1,"url":"7"}]]
            GodEdit geFRA = new GodEdit("Add Proposal", "Funding Receiving Address", Sys);
            geFRA.CaptionText = "Funding Receiving Address";
            GodEdit geAmount = new GodEdit("Add Proposal", "Proposal Amount", Sys);
            geAmount.CaptionText = "Proposal Amount";
            GodEdit geURL = new GodEdit("Add Proposal", "Discussion URL", Sys);
            geURL.CaptionText = "Discussion URL";
            ProposalAdd.AddControl(geName);
            ProposalAdd.AddControl(geFRA);
            ProposalAdd.AddControl(geAmount);
            ProposalAdd.AddControl(geURL);

            GodEdit geBtnSave = new GodEdit("Save", GodEdit.GEType.Button, "btnProposalSave", "Save", Sys);
            ProposalAdd.AddControl(geBtnSave);
            return ProposalAdd.Render(this, true);
        }


        public WebReply btnProposalSave_Click()
        {
            string sName = Sys.GetObjectValue("Add Proposal", "Proposal Name");
            sName.Replace(" ", "_");
            string sFRA = Sys.GetObjectValue("Add Proposal", "Funding Receiving Address");
            string sAmount = Sys.GetObjectValue("Add Proposal", "Proposal Amount");
            string sURL = Sys.GetObjectValue("Add Proposal", "Discussion URL");
            // gobject prepare 0 1 EPOCH_TIME HEX
            // Prepare the Hex string
            // Gobject format in Wallet: [["proposal",{"end_epoch":"1","name":"2","payment_address":"3","payment_amount":"444","start_epoch":"5","type":1,"url":"7"}]]
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
            string sHex = clsStaticHelper.GetGenericInfo2("test", "gobject", oParams, "Hex");
            string sResult = "";
            return ProposalAdd();
        }

        public WebReply WorkerAdd()
        {
            Section WorkerAdd = new Section("Add Worker", 1, Sys, this);
            GodEdit geWorkerUsername = new GodEdit("Add Worker", "Username", Sys);
            geWorkerUsername.CaptionText = "Worker Username:";
            WorkerAdd.AddControl(geWorkerUsername);
            GodEdit geNotes = new GodEdit("Add Worker", "Notes", Sys);
            geNotes.CaptionText = "Notes:";
            WorkerAdd.AddControl(geNotes);
            GodEdit geBtnSave = new GodEdit("Save", GodEdit.GEType.Button, "btnWorkerSave", "Save", Sys);
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
                Sys._data.Exec(sql);
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