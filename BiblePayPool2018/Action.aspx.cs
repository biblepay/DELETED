using Microsoft.VisualBasic;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using static BiblePayPool2018.Ext;

namespace BiblePayPool2018
{
    public partial class Action : System.Web.UI.Page
    {
      
        public string MinerGuid(string sMiner, ref double dAutoWithdraws)
        {
            string sMinerGuid  = clsStaticHelper.AppCache(sMiner, this.Application);

            if (sMinerGuid.Length == 0)
            {
                // If the miner guid is not cached in memory we need to get it from the database:
                string sql = "Select miners.id,users.withdrawalAddressValidated from miners with(nolock) inner join Users(nolock) on users.id = miners.userid"
                    + " Where miners.username = '" + clsStaticHelper.PurifySQL(sMiner, 99) + "'";
                DataTable dt = clsStaticHelper.mPD.GetDataTable2(sql,false);
                if (dt.Rows.Count > 0)
                {
                    string sID = dt.Rows[0]["id"].ToString();
                    dAutoWithdraws = clsStaticHelper.GetDouble(dt.Rows[0]["withdrawalAddressValidated"]);
                    clsStaticHelper.AppCache(sMiner, sID, Server, this.Application);
                    clsStaticHelper.AppCache(sMiner + "_AutoWithdraws", dAutoWithdraws.ToString(), Server, this.Application);
                    return sID;
                }
                else
                {
                    return "";
                }
            }
            else
            {
                dAutoWithdraws = clsStaticHelper.GetDouble(clsStaticHelper.AppCache(sMiner + "_AutoWithdraws", this.Application));
                return sMinerGuid;
            }
        }

        public double GetCachedSolvedCount(string sMinerGuid, string sNetworkID)
        {
            string sKey = "solved" + "_" + sMinerGuid;
            double dCachedSolved = clsStaticHelper.GetDouble(clsStaticHelper.AppCache(sKey, this.Application));
            if (clsStaticHelper.LogLimiter() > 1000)
            {
                string sql = "Select count(*) as ct from Work with (nolock) where endtime is not null and minerid='" 
                    + clsStaticHelper.GuidOnly(sMinerGuid)
                    + "' and networkid='" + clsStaticHelper.VerifyNetworkID(sNetworkID) + "'"; // and endtime is not null";
                dCachedSolved = clsStaticHelper.GetDouble(clsStaticHelper.mPD.ReadFirstRow2(sql, 0, false));
                clsStaticHelper.AppCache(sKey, dCachedSolved.ToString(), Server, this.Application);
            }
            return dCachedSolved;
        }
        
        private SystemObject Sys = null;
        private bool IsLoggedOn()
        {
            string sTheUser = clsStaticHelper.GetCookie("username");
            string sThePass = clsStaticHelper.GetCookie("password");
            if (sTheUser != "" && sThePass != "")
            {
                Login l = new Login(Sys);
                bool bMyDepersist = false;

                try
                {
                    bMyDepersist = l.VerifyUser(sTheUser, sThePass, ref Sys, false);

                }
                catch(Exception ex)
                {
                    clsStaticHelper.Log(" Cant depersist user " + ex.Message);
                }
                return bMyDepersist;
            }
            return false;
        }
        

        public string GetHashDifficulty2(long lSharesSolved, double dHistoricalHashPs, string sNetworkID)
        {
            string sZeroes = "000";
            if (lSharesSolved < 4)
            {
                sZeroes = "000";
            }
            else if (lSharesSolved >= 5 && lSharesSolved <= 20)
            {
                sZeroes = "00000";
            }
            else if (lSharesSolved > 20 && lSharesSolved <= 30)
            {
                sZeroes = "00000000";
            }
            else if (lSharesSolved > 30 && lSharesSolved < 99)
            {
                sZeroes = "0000000000";
            }
            else if (lSharesSolved > 99)
            {
                sZeroes = "0000000000";
            }
            string sHashTarget = sZeroes + "111100000000000000000000000000000000000000000000000000000000";
            sHashTarget = Strings.Left(sHashTarget, 64);
            return sHashTarget;
        }

        private string GetHashTarget(string sMinerGuid, string sNetworkID)
        {
            string sHashTarget = "";
            double dCSS2 = GetCachedSolvedCount(sMinerGuid, sNetworkID);
            double dHPS = 0;
            sHashTarget = GetHashDifficulty2((long)dCSS2, dHPS, sNetworkID);
            return sHashTarget;
        }
       
        private bool AnalyzeTip(int iTipHeight, String sNetworkID)
        {
            if (iTipHeight > clsStaticHelper.nCurrentTipHeightMain)
            {
                clsStaticHelper.GetTipHeight(sNetworkID);
            }
            if (sNetworkID == "main")
            {
                if (iTipHeight < clsStaticHelper.nCurrentTipHeightMain - 1 || iTipHeight > clsStaticHelper.nCurrentTipHeightMain) return false;
            }
            return true;
        }
        
        string GetXML(object o, string sFieldName)
        {
            string sValue = o.ToString();
            string sXML = "<" + sFieldName + ">" + sValue + "</" + sFieldName + ">";
            return sXML;
        }
        
        long RawTransactionAge(string sNetworkId, string sMyAddress, string sTxId, double dAmount)
        {
            string sRawTx = clsStaticHelper.GetRawTransaction(sNetworkId, sTxId);
            if (sRawTx.Contains(sMyAddress) && sRawTx.Contains(dAmount.ToString()))
            {
                return 10;
            }
            if (sRawTx.Contains("?") && sRawTx.Contains(dAmount.ToString())) return 9;
            return 0;
        }

        public void SendWalletOrders(string sNetworkID, string sRecAddr)
        {
            try
            {
                string sql = "Select * from Orders where networkid='" + clsStaticHelper.VerifyNetworkID(sNetworkID) 
                    + "' and WalletOrder=1 and WalletOrderProcessed=0 and TxId is not null";
                DataTable dt10 = clsStaticHelper.mPD.GetDataTable2(sql);
                for (int i = 0; i < dt10.Rows.Count; i++)
                {
                    string id = dt10.Rows[i]["id"].ToString();
                    string sProductID = dt10.Rows[i]["ProductID"].ToString();
                    string sUserGuid = dt10.Rows[i]["UserId"].ToString();
                    string sTxId = dt10.Rows[i]["TxId"].ToString();
                    double dAmount = ToDouble(dt10.Rows[i]["Amount"]);
                    clsStaticHelper.Log(" Processing order " + id);

                    long lConfirms = RawTransactionAge(sNetworkID, sRecAddr, sTxId, dAmount);
                    // Pull the User Address info
                    if (lConfirms > 4)
                    {
                        clsStaticHelper.Log(" Got order confirm! processing " + id);

                        string sql20 = "Select * from users where id ='" + clsStaticHelper.GuidOnly(sUserGuid) + "'";

                        DataTable dtU = clsStaticHelper.mPD.GetDataTable2(sql20);
                        if (dtU.Rows.Count > 0)
                        {
                            string sql21 = "Select * from Products where productid = '" + clsStaticHelper.PurifySQL(sProductID,50)
                                + "' and networkid='" + clsStaticHelper.VerifyNetworkID(sNetworkID) + "'";
                            DataTable dt22 = clsStaticHelper.mPD.GetDataTable2(sql21);
                            if (dt22.Rows.Count == 0)
                            {
                                string sql23 = "Update Orders set Status1='UNABLE TO LOCATE PRODUCT',WalletOrderProcessed=1,Updated=getdate() where id='" + clsStaticHelper.GuidOnly(id) + "'";
                                clsStaticHelper.mPD.Exec2(sql23);
                                break;
                            }
                            double dPrice = Convert.ToDouble(dt22.Rows[0]["price"].ToString());
                            double dBBP = clsStaticHelper.PriceInBBP(dPrice / 100);
                            string sRetailer = dt22.Rows[0]["Retailer"].ToString();
                            string sTitle = dt22.Rows[0]["Title"].ToString();
                            // Verify the tx is confirmed
                            string sCountry = dtU.Rows[0]["Country"].ToString().ToUpper();
                            Zinc.BiblePayMouse bm_0 = new Zinc.BiblePayMouse("");
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
                            // The user has a good address, enough BBP, go ahead and buy it - Place the order, and track it
                            string sToken = USGDFramework.clsStaticHelper.GetConfig("MouseToken_E");
                            Zinc.BiblePayMouse bm = new Zinc.BiblePayMouse(sToken);
                            Zinc.BiblePayMouse.Product[] product = new Zinc.BiblePayMouse.Product[1];
                            Zinc.BiblePayMouse.Product pNew = new Zinc.BiblePayMouse.Product();
                            pNew.quantity = 1;
                            pNew.product_id = sProductID;
                            product[0] = pNew;
                            dPrice += 10*100; //allow for shipping
                            double max_price = dPrice;
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
                            string indep = id.ToString().ToUpper().Substring(0, 5);
                            string sMouseId = bm.CreateOrder(indep, sRetailer, product, max_price, is_gift, "", shipAddr, ship1, pm1, ba1, rc1, wh1, cn1);
                            sql = "Update Orders set Title='" + 
                                sTitle + "',WalletOrderProcessed=1,MouseId = '" + 
                                clsStaticHelper.PurifySQL(sMouseId,55)
                                + "',Status1='PLACED' where ID = '" + clsStaticHelper.GuidOnly(id) + "'";
                            clsStaticHelper.mPD.Exec2(sql);
                        }
                    }
                }
                clsStaticHelper.GetOrderStatusUpdates();
            }
            catch(Exception ex)
            {
                clsStaticHelper.Log("Error while placing wallet orders " + ex.Message);
            }
                    
        }
        
        double GetProductPrice(string sProductId, string sNetworkID)
        {
                string sql = "Select * from Products where networkid='" + clsStaticHelper.VerifyNetworkID(sNetworkID)
                + "' and Productid='" + clsStaticHelper.PurifySQL(sProductId,55) + "' and inwallet=1";
                DataTable dt = clsStaticHelper.mPD.GetDataTable2(sql);
                if (dt.Rows.Count > 0)
                {
                    double dPrice = ToDouble(dt.Rows[0]["Price"]);
                    double dBBP = clsStaticHelper.PriceInBBP(dPrice / 100);
                    return dBBP;
                }
                return 0;
        }

        double CalculateGospelClickBounty(string sID, string sClickerGuid)
        {
            string sql = "Select * From Links where id = '" + clsStaticHelper.GuidOnly(sID) + "'";
            DataTable dt = clsStaticHelper.mPD.GetDataTable2(sql);
            if (dt.Rows.Count > 0)
            {
                double dBounty = ToDouble(dt.Rows[0]["PaymentPerClick"]);
                double dBudget = ToDouble(dt.Rows[0]["Budget"]);
                double dClicks = ToDouble(dt.Rows[0]["Clicks"]);
                double dSpent = dClicks * dBounty;
                // Ensure this user was not awarded this link click before:
                sql = "Select count(*) ct from transactionLog where transactiontype='GOSPEL_BOUNTY' and userid='" 
                    + clsStaticHelper.GuidOnly(sClickerGuid)
                    + "' and destination='" + clsStaticHelper.GuidOnly(sID)
                    + "'";
                double AwardCount = clsStaticHelper.mPD.GetScalarDouble2(sql, "ct");
                if (AwardCount == 0)
                {
                    // increment clicks
                    sql = "Update Links Set Clicks=isnull(clicks,0)+1 where id = '" + clsStaticHelper.GuidOnly(sID) + "'";
                    clsStaticHelper.mPD.Exec2(sql);
                    if (dSpent < dBudget)
                    {
                        // We have rewards left to give:
                        return dBounty;
                    }
                }
                return 0;

            }
            return 0;
        }

        protected void Page_Load(object sender, System.EventArgs e)
        {
                try
                {

                    string sUserA = (Request.Headers["Miner"] ?? "").ToString();
                    string sUserB = (Request.Headers["Action"] ?? "").ToString();
                    string sUserC = (Request.Headers["Solution"] ?? "").ToString();
                    string sRow1 = sUserA + " " + sUserB + " " + sUserC;
                    string sMiner = (Request.Headers["Miner"] ?? "").ToString();
                    string sBBPAddress = (Request.Headers["Miner"] ?? "").ToString();
                    string sAction = (Request.Headers["Action"] ?? "").ToString();
                    string sSolution = (Request.Headers["Solution"] ?? "").ToString();
                    string sNetworkID = (Request.Headers["NetworkID"] ?? "").ToString();
                    string sAgent = (Request.Headers["Agent"] ?? "").ToString();
                    string sIP = (Request.UserHostAddress ?? "").ToString();
                    string sWorkID = (Request.Headers["WorkID"] ?? "").ToString();
                    string sThreadID1 = (Request.Headers["ThreadID"] ?? "").ToString();
                    string sReqAction = (Request.QueryString["action"] ?? "").ToString();
                    string sTip = (Request.QueryString["tip"] ?? "").ToString();
                if (sTip != "")
                {
                    bool bIsLogged = IsLoggedOn();
                    if (bIsLogged)
                    {
                        Sys.SetObjectValue("Tip", "Recipient1", sTip);

                        Response.Redirect("pool.ashx");
                    }else
                    {
                        Response.Redirect("pool.ashx");
                    }
                }
                    string sReqLink = (Request.QueryString["link"] ?? "").ToString();
                    if (sReqLink != "") sReqAction = "link";
                    string sOS = (Request.Headers["OS"] ?? "").ToString();
                    sAgent = sAgent.Replace(".", "");
                    double dAgent = Convert.ToDouble("0" + sAgent);
                    if (dAgent > 1000 && dAgent < 1035 || (dAgent > 1000 && dAgent < 1101))
                    {
                        string sResponse1 = "<RESPONSE>PLEASE UPGRADE</RESPONSE><ERROR>PLEASE UPGRADE</ERROR><EOF></HTML>";
                        Response.Write(sResponse1);
                        return;
                    }
                if (sReqAction == "verify_email")
                {
                    string sID = (Request.QueryString["id"] ?? "").ToString();
                    string sql = "Select username,id,email,password from Users where id = '" + clsStaticHelper.GuidOnly(sID) + "'";
                    string email = clsStaticHelper.mPD.ReadFirstRow2(sql, "email");
                    string sUserName = clsStaticHelper.mPD.ReadFirstRow2(sql, "username");

                    if (email.Length > 3)
                    {
                        // Verified
                        sql = "Update Users set EmailVerified = 1 where id = '" + clsStaticHelper.GuidOnly(sID) + "'";
                        clsStaticHelper.mPD.Exec2(sql);
                        string sBody = "<html>Dear " + sUserName.ToUpper() + ", <br><br>Your E-Mail has been verified.<br><br>  Have a great day! <br><br> May Jesus' Kingdom Extend for Infinity,<br>BiblePay Support<br></html>";
                        Response.Write(sBody);
                        return;
                    }
                }
                else if (sReqAction == "api_proposals")
                {
                    string sql = "Select * from Proposal where network = 'main'";
                    string data = clsStaticHelper.DataDump(sql, "resubmit");
                    Response.Write(data);
                    return;
                }
                else if (sReqAction == "api_superblock")
                {
                    string sql = "Select * from Superblocks";
                    string data = clsStaticHelper.DataDump(sql, "");
                    Response.Write(data);
                }
                else if (sReqAction == "api_leaderboard")
                {
                    string sql = "Select * from RosettaMaster";
                    string data = clsStaticHelper.DataDump(sql, "");
                    Response.Write(data);
                    return;
                }
                else if (sReqAction == "metrics")
                {
                    string sql = "Select count(*) ct from Orphans";
                    double dOrphanCount = clsStaticHelper.mPD.GetScalarDouble2(sql, "ct");
                    string sMetrics = "<METRICS><ORPHANCOUNT>" + dOrphanCount.ToString() + "</ORPHANCOUNT></METRICS>";
                    Response.Write(sMetrics);
                    return;
                }
                else if (sReqAction == "wcgrac")
                {
                    string cpid = (Request.QueryString["cpid"] ?? "").ToString();
                    string sql = "select isnull(wcgrac,0) as wcgrac from Superblocks where cpid='" + cpid + "' and height = (select max(height) from superblocks)";
                    double dRAC = clsStaticHelper.mPD.GetScalarDouble2(sql, "wcgrac", false);
                    string sMetrics = "<WCGRAC>" + dRAC.ToString() + "</WCGRAC><EOF>";
                    Response.Write(sMetrics);
                    return;
                }
                else if (sReqAction == "teamrac")
                {
                    string sql = "select max(totalrac) a from superblocks where height=(select max(height) from superblocks)";
                    double dRAC = clsStaticHelper.mPD.GetScalarDouble2(sql, "a", false);
                    string sMetrics = "<TEAMRAC>" + dRAC.ToString() + "</TEAMRAC><EOF>";
                    Response.Write(sMetrics);
                    return;
                }
                else if (sReqAction == "unbanked")
                {
                    string sql = "select tempteam.id,tempteam.rosettaid, tempteam.UserName,sum(isnull(temphostdetails1.RAC,0)) as RAC, sum(isnull(temphostdetails2.rac,0)) as ARMRac "
                        + "  from TempTeam"
                        + " inner join TempHosts on tempHosts.RosettaID = tempTeam.RosettaID "
                        + " left join TempHostDetails temphostdetails1 on TempHosts.Computerid = temphostdetails1.computerid and isnull(temphostdetails1.arm,0) = 0 "
                        + " left join temphostdetails temphostdetails2 on temphosts.computerid = temphostdetails2.computerid  and temphostdetails2.arm = 1 "
                        + " group by tempteam.id,tempteam.rosettaid, tempteam.username"
                        + " having sum(isnull(temphostdetails2.rac,0)) > 0 and sum(isnull(temphostdetails1.rac,0)) < 15 ";
                    DataTable dtUnbanked = clsStaticHelper.mPD.GetDataTable2(sql, false);
                    string sData = "<UNBANKED>";
                    for (int z = 0; z < dtUnbanked.Rows.Count; z++)
                    {
                        string sRosettaId = dtUnbanked.Rows[z]["rosettaid"].ToString();
                        double dArmRac = clsStaticHelper.GetDouble(dtUnbanked.Rows[z]["ARMRac"].ToString());
                        string sRow = sRosettaId + "," + dArmRac.ToString() + ",\r\n<ROW>";
                        sData += sRow;
                    }
                    sData += "</UNBANKED></HTML><EOF>";
                    Response.Write(sData);
                    return;
                }
                else if (sReqAction == "continue_action")
                {
                    string sID = (Request.QueryString["id"] ?? "").ToString();
                    if (sID.Length > 1)
                    {

                        string sql10 = "Select * From RequestLog where id = '" + clsStaticHelper.GuidOnly(sID)
                            + "' and processed is null";
                        DataTable dtReq = clsStaticHelper.mPD.GetDataTable2(sql10);
                        if (dtReq.Rows.Count == 0)
                        {
                            Response.Write("Sorry, No Transaction present.");
                            return;
                        }

                        string sql2 = "Update RequestLog set IP2='" + clsStaticHelper.PurifySQL(sIP, 80)
                        + "' Where id = '" + clsStaticHelper.GuidOnly(sID)
                        + "'";
                        clsStaticHelper.mPD.Exec2(sql2);

                        if (dtReq.Rows.Count > 0)
                        {
                            string sUserGuid = dtReq.Rows[0]["userguid"].ToString();
                            sql10 = "Select * from Users where id = '" + clsStaticHelper.GuidOnly(sUserGuid)
                            + "'";
                            DataTable dtU = clsStaticHelper.mPD.GetDataTable2(sql10);
                            if (dtU.Rows.Count > 0)
                            {
                                string sUG = dtU.Rows[0]["id"].ToString();
                                string sNetworkID2 = dtReq.Rows[0]["Network"].ToString();
                                double Amount = clsStaticHelper.GetDouble(dtReq.Rows[0]["Amount"]);
                                string sUN = dtU.Rows[0]["username"].ToString();
                                double dEV = clsStaticHelper.GetDouble(dtU.Rows[0]["EmailVerified"]);
                                double dUV = clsStaticHelper.GetDouble(dtU.Rows[0]["SendVerified"]);
                                double dWithdraws = clsStaticHelper.GetDouble(dtU.Rows[0]["Withdraws"]);
                                double oldBalance = 0;
                                double dImmature = 0;
                                string sDest = dtReq.Rows[0]["Address"].ToString();

                                clsStaticHelper.GetUserBalances(sNetworkID2, sUG, ref oldBalance, ref dImmature);
                                double dAvail = oldBalance - dImmature;
                                if (dAvail < Amount)
                                {
                                    Response.Write("Sorry, Balance too low.");
                                    return;
                                }
                                if (Amount > 10000)
                                {
                                    Response.Write("Please wait until January 1, 2019 before withdrawing more than 10000 BBP.  ");
                                    return;
                                }

                                if (Amount < 3 || Amount > 40000)
                                {
                                    Response.Write("Sorry, amount out of range (1).");
                                    return;
                                }
                                int iHeight = clsStaticHelper.GetTipHeight(sNetworkID2);
                                if (iHeight < 1000)
                                {
                                    Response.Write("Sorry, Chain error.");
                                    return;
                                }
                                if (dEV != 1)
                                {
                                    Response.Write("Sorry, Email must be reverified.");
                                    return;
                                }
                                // Ensure the user has an approved receiving address
                                // If SendVerified is set, let it go through, or if this address has been used before
                                string sql = "Select count(*) a from TransactionLog where transactiontype='withdrawal' and destination='"
                                    + sDest + "'";
                                double dAddrUseCt = clsStaticHelper.mPD.GetScalarDouble2(sql, "a");
                                bool bIdentityVerified = false;
                                if (dAddrUseCt > 1 || dUV == 1 || dWithdraws > 3) bIdentityVerified = true;
                                if (!bIdentityVerified)
                                {
                                    string sNarr1 = "Sorry, your identity must be verified.  Please send an e-mail to contact@biblepay.org with reference number "
                                    + sID + ".";
                                    Response.Write("<br><br>Verifying user activity...<br>Verifying transactions...<br><br>");
                                }

                                sql = "Select Value from System where systemkey='withdraw'";
                                string sStatus = clsStaticHelper.mPD.GetScalarString2(sql, "value");
                                if (sStatus == "DOWN")
                                {
                                    Response.Write("Sorry, withdrawals are down temporarily, please try later.");
                                    return;
                                }
                                // Ensure last withdrawal hasnt happened in a while...

                                sql = "select max(updated) upd from TransactionLog where added > getdate() - 2  and transactionType = 'withdrawal' and amount > 9000";
                                string sLast = clsStaticHelper.mPD.GetScalarString2(sql, "upd");
                                if (sLast == "") sLast = Convert.ToDateTime("1/1/1970").ToShortDateString();

                                double dDiff = Math.Abs(DateAndTime.DateDiff(DateInterval.Second, DateTime.Now, Convert.ToDateTime(sLast)));
                                if (dDiff < 120)
                                {
                                    Response.Write("Sorry, a user has recently withdrawn a large sum of BBP.  Please wait 2 minutes and try again.  This restriction will be lifted on Jan 31, 2018 pending positive Pool Health.");
                                    return;
                                }

                                sql = "select max(updated) upd from TransactionLog where added > getdate() - 2  and transactionType = 'withdrawal' and amount > 1000";
                                string sLast2 = clsStaticHelper.mPD.GetScalarString2(sql, "upd");
                                if (sLast2 == "") sLast2 = Convert.ToDateTime("1/1/1970").ToShortDateString();

                                double dDiff2 = Math.Abs(DateAndTime.DateDiff(DateInterval.Second, DateTime.Now, Convert.ToDateTime(sLast2)));
                                if (dDiff2 < 90)
                                {
                                    Response.Write("Sorry, a user has recently withdrawn BBP.  Please wait 90 seconds and try again.  This restriction will be lifted on Jan 31, 2018 pending positive Pool Health.");
                                    return;
                                }

                                // Reverify balance
                                double newBalance = oldBalance - Amount;
                                string sTXID = "";
                                try
                                {
                                    sTXID = clsStaticHelper.Z(sID, sDest, Amount, sNetworkID2);
                                    if (sTXID.Length > 5)
                                    {
                                        bool bTrue = clsStaticHelper.AdjustUserBalance(sNetworkID2, sUserGuid, -1 * Amount);
                                        clsStaticHelper.InsTxLog(sUN, sUG, sNetworkID2,
                                            iHeight, sTXID, Amount, oldBalance, newBalance, sDest, "withdrawal", "");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    sTXID = "ERR60514";
                                }
                                if (sTXID.Length > 5)
                                {
                                    sql = "Update RequestLog set TXID='" + clsStaticHelper.PurifySQL(sTXID, 125)
                                    + "',processed=1 where id = '" + clsStaticHelper.GuidOnly(sID)
                                    + "'";
                                    clsStaticHelper.mPD.Exec2(sql);
                                }
                                // Notify of the transaction
                                string sSite = USGDFramework.clsStaticHelper.GetConfig("WebSite");
                                string sNarr = "<img src='" + sSite + "images/logo.png'><p><p><br><br> Transaction verified.  <br><br><p><p>BBP Withdraw in the amount of "
                                    + Amount.ToString() + " has been transmistted to " + sDest
                                    + " in TransactionID " + sTXID + ". <br><p><p> Thank you for using BiblePay.";
                                Response.Write(sNarr);
                                return;
                            }
                        }
                    }
                }

                    if (sReqAction=="password_recovery")
                    {
                        string sID1 = (Request.QueryString["id"] ?? "").ToString();
                        string sql = "Select userid from passwordreset where id = '" + clsStaticHelper.GuidOnly(sID1) + "'";
                        DataTable dt100 = clsStaticHelper.mPD.GetDataTable2(sql);
                        if (dt100.Rows.Count==0)
                        {
                            Response.Write("Sorry, no password reset action available.");
                            return;
                        }
                        string sID = dt100.Rows[0]["userid"].ToString();
                        sql = "Select username,id,email,password from Users where id = '" + clsStaticHelper.GuidOnly(sID) + "'";
                        string email = clsStaticHelper.mPD.ReadFirstRow2(sql, "email");
                        if (email.Length > 3)
                        {
                            // Change the users password for them and notify them:
                            string sUserName = clsStaticHelper.mPD.ReadFirstRow2(sql, "username");
                            string sNewPass = Guid.NewGuid().ToString();
                            sNewPass = sNewPass.Replace("-", "");
                            sNewPass = sNewPass.Substring(0, 7);
                            sql = "Update Users Set Password = '" + USGDFramework.modCryptography.SHA256(sNewPass) + "' where id = '" + clsStaticHelper.GuidOnly(sID) + "'";
                            clsStaticHelper.mPD.Exec2(sql);
                            sql = "Delete from passwordReset where id = '" + clsStaticHelper.GuidOnly(sID1) + "'";
                            clsStaticHelper.mPD.Exec2(sql);
                            string sBody = "<html>Dear " + sUserName.ToUpper() + ", <br><br>Your password has been reset to:<br>" + sNewPass + "<br><br>  Please Log In with the new password, and optionally change it. <br><br> Warm Regards,<br>BiblePay Support<br></html>";
                            Response.Write(sBody);
                            return;
                        }
                        else
                        {
                            Response.Write("<html>Invalid Password Recovery Request</html>");
                            return;
                        }
                    }
                    else if (sReqAction=="link")
                    {
                        // Grab the ID from the link
                        string sID = Request.QueryString["link"];
                        string sql = "Select * from Links where id like '" + clsStaticHelper.PurifySQL(sID,100) + "%'";
                        DataTable dt = clsStaticHelper.mPD.GetDataTable2(sql);
                        if (dt.Rows.Count > 0)
                        {
                            string Payer = dt.Rows[0]["userid"].ToString();
                            string sURL = dt.Rows[0]["OriginalURL"].ToString();
                            string sGuid = dt.Rows[0]["id"].ToString();
                            double dBudget = ToDouble(dt.Rows[0]["Budget"]);
                            bool bIsLogged = IsLoggedOn();
                            double dBountyPaid = 0;
                            if (bIsLogged)
                            {
                                if (Payer != Sys.UserGuid.ToString())
                                {
                                    // Credit user and redirect - as long as user email is verified
                                    double dBounty = CalculateGospelClickBounty(sGuid, Sys.UserGuid);
                                    if (dBounty > 0)
                                    {
                                        string sql100 = "Select isnull(emailverified,0) EV from USERS where id='" + clsStaticHelper.GuidOnly(Sys.UserGuid)
                                        + "'";
                                        double d1 = clsStaticHelper.GetScalarDouble2(sql100, "EV");
                                        if (d1 == 1)
                                        {
                                            double dBalance = clsStaticHelper.AwardBounty(sGuid, Payer, -1 * dBounty, "GOSPEL_CAMPAIGN", 0, 
                                                Guid.NewGuid().ToString(), "main", sID.ToString(), false);
                                            if (dBalance > 0)
                                            {
                                                // Transfer the money to the Clicker
                                                dBountyPaid = dBounty;
                                                dBalance = clsStaticHelper.AwardBounty(sGuid, Sys.UserGuid, dBounty, "GOSPEL_BOUNTY", 0, Guid.NewGuid().ToString(), "main", sID.ToString(), false);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // Same User just redirect
                                }
                                string sJava = "<html><head><script>setTimeout(\"location.href = '" + sURL + "';\",2000);</script></head><body>";
                                string sSite = USGDFramework.clsStaticHelper.GetConfig("WebSite");
                                string sNarr10 = sJava + "<img src='" + sSite + "images/logo.png'><p><p><br><br>Thank you for using BiblePay.  Gospel Bounty Award: " + dBountyPaid.ToString() + " BBP.";
                                Response.Write(sNarr10);
                                return;
                            }
                            else
                            {
                                // User should be redirected to login page
                                string sJava = "<html><head><script>setTimeout(\"location.href = '" + sURL + "';\",2000);</script></head><body>";
                                string sSite = USGDFramework.clsStaticHelper.GetConfig("WebSite");
                                string sNarr10 = sJava + "<img src='" + sSite + "images/logo.png'><p><p><br><br> You must log in to the pool at http://pool.biblepay.org to be awarded for Gospel Link Clicks.  <b>Gospel Bounty Awarded: " + dBountyPaid.ToString() + " BBP.";
                                Response.Write(sNarr10);
                                return;
                            }
                        }
                        else
                        {
                            Response.Write("Invalid BiblePay Link");
                            return;
                        }
                    }
                    else if (sReqAction=="housecleaning")
                    {
                        sNetworkID = "main";
                        for (int i = 0; i < 50; i++ )
                        {
                            string sql = "Update system set value=value+1 where systemkey='main_housecleaning'";
                            clsStaticHelper.BatchExec3(sql, Application);
                        }
                    }


                long lMaxThreadCount = 40;
                if (Conversion.Val(sThreadID1) > lMaxThreadCount)
                {
                    Response.Write("<RESPONSE><ERROR>MAX THREAD COUNT OF " + Strings.Trim(lMaxThreadCount.ToString()) + " EXCEEDED-PLEASE LOWER THREADCOUNT</ERROR></RESPONSE><END></HTML>");
                    return;
                }
                string sResponse = "";
                if (!clsStaticHelper.ValidateNetworkID(sNetworkID))
                {
                    Response.Write("<ERROR>INVALID NETWORK ID</ERROR><END></HTML>");
                    return;
                }
                ////////////////////////////////////////////// READY TO MINE ///////////////////////////////////////////////////////////////////////////
                double dAutoWithdrawsEnabled = 0;
                string sMinerGuid = MinerGuid(sMiner, ref dAutoWithdrawsEnabled);

                if (sAction !="get_products" && sAction != "buy_product" 
                    && sAction != "get_product_escrow_address" && sAction != "order_status" && sAction != "order" && sAction != "trades" 
                    && sAction != "cancel" && sAction != "escrow" && sAction != "execution_history" && sAction != "finishtrades" && sAction != "process_escrow")
                {
                    // These two commands use the BBP address instead for the PK
                    if (sMinerGuid.Length == 0)
                    {
                        Response.Write("<ERROR>INVALID MINER GUID " + sMiner + "</ERROR><END></HTML><EOF>" + Constants.vbCrLf);
                        return;
                    }
                }
              
                string sPoolRecvAddress = clsStaticHelper.GetNextPoolAddress(sNetworkID, Server, this.Application);
                
                if (sAction == "cancel") sAction = "order";

                switch (sAction)
                {
                    case "process_escrow":
                         // update these matches with a guid here 
                         string sql11 = "Select * from Trade where networkid='" 
                            + clsStaticHelper.VerifyNetworkID(sNetworkID)
                            + "' and (match is not null OR MatchSell Is Not Null) and Address='" 
                            + clsStaticHelper.PurifySQL(sBBPAddress,100)
                            + "' and EscrowTxId is null";
                        DataTable dt = clsStaticHelper.mPD.GetDataTable2(sql11);
                        string sTrades = "<RESPONSE><ESCROWS>";
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            string id = dt.Rows[i]["id"].ToString();
                            string Action = dt.Rows[i]["Act"].ToString();
                            double amount = ToDouble(dt.Rows[i]["price"]);
                            double quantity = ToDouble(dt.Rows[i]["quantity"]);
                            // Note, on the Buy side: total = amount*quantity, on the sell side, amount = QUANTITY & coin=colored(401)
                            double total = 0;
                            if (Action=="BUY")
                            {
                                total = amount * quantity;
                            }
                            else if (Action=="SELL")
                            {
                                total = quantity;
                            }
                            string sHash = dt.Rows[i]["hash"].ToString();
                            string symbol = dt.Rows[i]["symbol"].ToString();
                            string sRow = "<ESCROW><HASH>" + sHash + "</HASH><ESCROW_ADDRESS>" + sPoolRecvAddress + "</ESCROW_ADDRESS><ESCROWID>" + id + "</ESCROWID><AMOUNT>" 
                                + total.ToString() + "</AMOUNT><SYMBOL>" + symbol + "</SYMBOL></ESCROW><ROW>";
                            sTrades += sRow;

                        }
                        sTrades += "</ESCROWS></RESPONSE></EOF></HTML>";
                        // SendMutatedTransactions(sNetworkID);
                        Response.Write(sTrades);
                        // MoveCompletedTrades(sNetworkID);
                        break;

                    case "order_status":
                        // Move orders to next status:
                        SendWalletOrders(sNetworkID, sPoolRecvAddress);
                        // Show the order statuses: 
                        string sql = "Select * from Orders where networkid='" + clsStaticHelper.VerifyNetworkID(sNetworkID)
                            + "' and Address='" + clsStaticHelper.PurifySQL(sBBPAddress,100)
                            + "' ";
                        DataTable dt10 = clsStaticHelper.mPD.GetDataTable2(sql);
                        string sO = "<RESPONSE><ORDERSTATUS>";
                        for (int i = 0; i < dt10.Rows.Count; i++)
                        {
                            string id = dt10.Rows[i]["id"].ToString();
                            string sProductID = dt10.Rows[i]["productid"].ToString();
                            string sStatus1 = (dt10.Rows[i]["status1"] ?? "").ToString();
                            string sStatus2 = (dt10.Rows[i]["status2"] ?? "").ToString();
                            string sStatus3 = (dt10.Rows[i]["status3"] ?? "").ToString();
                            string sTitle = dt10.Rows[i]["Title"].ToString();
                            string sTXID1 = dt10.Rows[i]["TXID"].ToString();
                            string sAdded = dt10.Rows[i]["Added"].ToString();
                            string sConcStatus = sStatus1 + ";" + sStatus3;
                            double dAmount = ToDouble(dt10.Rows[i]["Amount"]);
                            string sPayload = "<ID>" + sProductID + "</ID><ADDED>" + sAdded + "</ADDED><AMOUNT>" + dAmount.ToString()
                                + "</AMOUNT><TXID>" + sTXID1 + "</TXID>"
                                + "<TITLE>" + sTitle + "</TITLE><STATUS1>" + sStatus1 + "</STATUS1><STATUS2>" + sStatus2 + "</STATUS2><STATUS3>" + sStatus3 + "</STATUS3><CONCSTATUS>"
                                + sConcStatus + "</CONCSTATUS><PRODUCTID>" + sProductID + "</PRODUCTID>";
                            string sRow = "<STATUS>" + sPayload + "</STATUS><ROW>";
                            sO += sRow;

                        }
                        sO += "</ORDERSTATUS></RESPONSE></EOF></HTML>";
                        clsStaticHelper.Log(sO);
                        Response.Write(sO);
                        break;
                    case "execution_history":
                        string sql12 = "Select * from TradeComplete where networkid='" + clsStaticHelper.VerifyNetworkID(sNetworkID)
                            + "' and Address='"
                           + clsStaticHelper.PurifySQL(sBBPAddress,100)
                           + "' and executed > getdate()-7 order by executed";
                        DataTable dt0 = clsStaticHelper.mPD.GetDataTable2(sql12);
                        string sTradeHistory = "<RESPONSE><TRADEHISTORY>";
                        for (int i = 0; i < dt0.Rows.Count; i++)
                        {
                            string id = dt0.Rows[i]["id"].ToString();
                            double amount = ToDouble(dt0.Rows[i]["price"]);
                            double quantity = ToDouble(dt0.Rows[i]["quantity"]);
                            double total = amount * quantity;
                            string sHash = dt0.Rows[i]["hash"].ToString();
                            string symbol = dt0.Rows[i]["symbol"].ToString();
                            string sAct = dt0.Rows[i]["Act"].ToString();
                            string sDate = dt0.Rows[i]["Executed"].ToString();
                            string sRow = "<TRADEH><ACTION>" + sAct + "</ACTION><EXECUTED>" + sDate + "</EXECUTED><HASH>" + sHash + "</HASH><AMOUNT>"
                                + amount.ToString() + "</AMOUNT><QUANTITY>" + quantity.ToString() + "</QUANTITY><TOTAL>" + total.ToString() + "</TOTAL><SYMBOL>" 
                                + symbol + "</SYMBOL></TRADEH><ROW>";
                            sTradeHistory += sRow;
                        }
                        sTradeHistory += "</TRADEHISTORY></RESPONSE></EOF></HTML>";
                        Response.Write(sTradeHistory);
                        break;
                 
                    case "finishtrades":
                        break;
                        
                   
                    case "get_products":
                        try
                        {
                            sql = "Select * from Products where networkid='" + clsStaticHelper.VerifyNetworkID(sNetworkID)
                                + "' and inwallet = 1";
                            dt = clsStaticHelper.mPD.GetDataTable2(sql);
                            string sQ = "<RESPONSE><PRODUCTS>";
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                double dPrice2 = ToDouble(dt.Rows[i]["Price"]);
                                double dBBP = clsStaticHelper.PriceInBBP(dPrice2 / 100);
                                string sRow = "<PRODUCT>";
                                sRow += GetXML(1000.ToString(), "TIME")
                                        + GetXML(dt.Rows[i]["ProductID"], "ID")
                                        + GetXML(dBBP.ToString(), "AMOUNT")
                                        + GetXML(dt.Rows[i]["Title"], "TITLE")
                                        + GetXML(dt.Rows[i]["PRODUCT_DETAILS"], "DETAILS")
                                        + GetXML(dt.Rows[i]["Pics"], "URL");

                                sRow += "</PRODUCT>";
                                sQ += sRow;
                            }
                            sQ += "</PRODUCTS></RESPONSE></EOF></HTML>";
                            Response.Write(sQ);
                        }
                        catch(Exception ex)
                        {
                            clsStaticHelper.Log("ERR IN GET_PRODUCTS " + ex.Message);
                        }
                        break;
                    case "get_product_escrow_address":
                        string sP = "<RESPONSE><PRODUCT_ESCROW_ADDRESS>" + sPoolRecvAddress + "</PRODUCT_ESCROW_ADDRESS></RESPONSE></EOF></HTML>";
                        Response.Write(sP);
                        break;
                    case "buy_product":
                        try
                        {
                            bool bSuccess = clsStaticHelper.EnsureBBPUserExists(sBBPAddress);
                            if (!bSuccess)
                            {
                                Response.Write("<RESPONSE><ERR>BAD USERID [ERROR: 1002, PubBBPAddress: " + sBBPAddress + "]</ERR></RESPONSE></EOF>");
                                return;
                            }
                            string sBBPUserGuid = clsStaticHelper.GetBBPUserGuid(sBBPAddress);
                            if (sBBPAddress == "" || sBBPUserGuid == "")
                            {
                                Response.Write("<RESPONSE><ERR>BAD USERID-GUID [" + sBBPAddress + "]</ERR></RESPONSE></EOF>");
                                return;
                            }
                            string sTXID = clsStaticHelper.ExtractXML(sSolution, "<TXID>", "</TXID>").ToString();
                            double vOUT = ToDouble(clsStaticHelper.ExtractXML(sSolution, "<VOUT>", "</VOUT>"));
                            string sProdID = clsStaticHelper.ExtractXML(sSolution, "<PRODUCTID>", "</PRODUCTID>").ToString();
                            string sName = clsStaticHelper.ExtractXML(sSolution, "<NAME>", "</NAME>").ToString();
                            string sDryRun = clsStaticHelper.ExtractXML(sSolution, "<DRYRUN>", "</DRYRUN>").ToString();
                            string sAddress1 = clsStaticHelper.ExtractXML(sSolution, "<ADDRESS1>", "</ADDRESS1>").ToString();
                            string sAddress2 = clsStaticHelper.ExtractXML(sSolution, "<ADDRESS2>", "</ADDRESS2>").ToString();
                            string sCity = clsStaticHelper.ExtractXML(sSolution, "<CITY>", "</CITY>").ToString();
                            string sState = clsStaticHelper.ExtractXML(sSolution, "<STATE>", "</STATE>").ToString();
                            string sZip = clsStaticHelper.ExtractXML(sSolution, "<ZIP>", "</ZIP>").ToString();
                            string sPhone = clsStaticHelper.ExtractXML(sSolution, "<PHONE>", "</PHONE>").ToString();
                            double dAmt = ToDouble(clsStaticHelper.ExtractXML(sSolution, "<AMOUNT>", "</AMOUNT>"));
                            // Verify the address
                            string sql80 = "Update Users Set Updated=getdate(),DelName='" 
                                + clsStaticHelper.PurifySQL(sName,125)
                                + "',Address1='" + clsStaticHelper.PurifySQL(sAddress1,125)
                                + "',Address2='" + clsStaticHelper.PurifySQL(sAddress2,125)
                                + "',City='" + clsStaticHelper.PurifySQL(sCity,125)
                                + "',State='"
                                + clsStaticHelper.PurifySQL(sState,25)
                                + "',Zip='" + clsStaticHelper.PurifySQL(sZip,25)
                                + "',Country='US',Phone='" + clsStaticHelper.PurifySQL(sPhone,100)
                                + "' WHERE ID='" + clsStaticHelper.GuidOnly(sBBPUserGuid)
                                + "'";
                            clsStaticHelper.mPD.Exec2(sql80);
                            // Verify Product Exists
                            double dPrice = GetProductPrice(sProdID, sNetworkID);
                            if (dPrice == 0)
                            {
                                Response.Write("<RESPONSE><ERR>Product Not Found</ERR></RESPONSE><EOF></HTML>");
                                return;
                            }
                            // Call out to Smarty Streets

                            Zinc.BiblePayMouse bm_0 = new Zinc.BiblePayMouse("");
                            Zinc.BiblePayMouse.MouseOutput mo = bm_0.ValidateAddress(sAddress1, sAddress2, sCity, sState, sZip);
                            if (!mo.dpv_match_code)
                            {
                                Response.Write("<RESPONSE><ERR>Address Valiation Failed (US Addresses Only)</ERR></RESPONSE><EOF></HTML>");
                                return;
                            }

                            if (dPrice != dAmt && sDryRun != "DRY")
                            {
                                string sNarr = "<RESPONSE><ERR>Payment received for " + dAmt.ToString() + "BBP differs from cost of "   
                                    + dPrice.ToString() + "BBP</ERR></RESPONSE></EOF><END></HTML>";
                                Response.Write(sNarr);
                                return;
                            }

                            // Verify escrow amount and confirms before sending the package
                            string sOrderGuid = Guid.NewGuid().ToString();
                            bool banned = false;
                            if (sNetworkID == "test")
                            {
                                string sql199 = "Select count(*) ct from Orders where networkid='" + clsStaticHelper.VerifyNetworkID(sNetworkID)
                                    + "' and WalletOrder=1 and txid is not null and address='" + clsStaticHelper.PurifySQL(sBBPAddress,100)
                                    + "'";
                                double dBot = clsStaticHelper.mPD.GetScalarDouble2(sql199, "ct");
                                if (dBot > 0) banned =true;
                            }
                            if (sDryRun != "DRY" && !banned)
                            {
                                // Ensure this guy hasnt bought any other testnet orders
                                sql = "Insert into Orders (id,added) values ('" + clsStaticHelper.GuidOnly(sOrderGuid)
                                    + "',getdate())";
                                clsStaticHelper.mPD.Exec2(sql);
                                sql = "Update Orders Set NetworkID='" + clsStaticHelper.VerifyNetworkID(sNetworkID)
                                    + "',WalletOrderProcessed=0,Address='" 
                                    + clsStaticHelper.PurifySQL(sBBPAddress,100)
                                    + "',WalletOrder=1,Userid='" + clsStaticHelper.GuidOnly(sBBPUserGuid)
                                    + "',ProductId='"
                                    + clsStaticHelper.PurifySQL(sProdID,100)
                                    + "',Title='',Added=getdate(),Status1='Placed',TXID='" 
                                    + clsStaticHelper.PurifySQL(sTXID,125)
                                    + "',Amount='" 
                                    + dAmt.ToString() 
                                    + "' where id = '" + clsStaticHelper.GuidOnly(sOrderGuid)
                                    + "'";
                                clsStaticHelper.mPD.Exec2(sql);
                            }
                            else
                            {
                                sTXID = sOrderGuid;
                            }

                            Response.Write("<RESPONSE><STATUS>SUCCESS</STATUS><TXID>" + sTXID + "</TXID><ORDERID>" + sOrderGuid + "</ORDERID></RESPONSE></EOF></HTML>");
                        }
                        catch(Exception ex)
                        {
                            clsStaticHelper.Log(" WHILE BUYING PROD " + ex.Message);
                            Response.Write("<RESPONSE><ERR>UNABLE TO PASS ORDER THROUGH VIABLE SANCTUARY. PLEASE TRY AGAIN LATER. </ERR></RESPONSE></EOF>");
                        }
                        break;
                    case "trades":
                        string sql5 = "Select * from trade where networkid='" + clsStaticHelper.VerifyNetworkID(sNetworkID)
                            + "' AND EscrowTxId is null and executed is null and match is null";
                        DataTable dt2 = clsStaticHelper.mPD.GetDataTable2(sql5);
                        string sTrades2 = "<RESPONSE><TRADES>";
                        for (int i = 0; i < dt2.Rows.Count; i++)
                        {
                            string sRow = GetXML(dt2.Rows[i]["time"], "TIME") 
                                + GetXML(dt2.Rows[i]["act"], "ACTION") + GetXML(dt2.Rows[i]["symbol"], "SYMBOL") 
                                + GetXML(dt2.Rows[i]["Address"], "ADDRESS") + GetXML(dt2.Rows[i]["quantity"], "QUANTITY") + GetXML(dt2.Rows[i]["price"], "PRICE");
                            sRow += "<ROW><TRADE>";
                            sTrades2 += sRow;
                        }
                        sTrades2 += "</TRADES></RESPONSE></EOF></HTML>";
                        try
                        {
                            // SendMutatedTransactions(sNetworkID);
                        }
                        catch(Exception ex)
                        {
                            clsStaticHelper.Log(" sending mutated " + ex.Message);
                        }
                        Response.Write(sTrades2);
                        break;
                    case "readytomine2":
                        string sWork1 = Guid.NewGuid().ToString();
                        string sHealth = clsStaticHelper.GetHealth(sNetworkID);
                        if (sHealth == "HEALTH_DOWN")
                        {
                            sResponse = "<RESPONSE>HEALTH_DOWN</RESPONSE><ERROR>HEALTH_DOWN</ERROR><EOF>";
                            Response.Write(sResponse);
                            return;
                        }
                        if (dAutoWithdrawsEnabled == 0 && false)
                        {
                            sResponse = "<RESPONSE>AUTO WITHDRAWS MUST BE ENABLED, PLEASE UPDATE YOUR BIBLEPAY RECEIVING ADDRESS IN THE POOL.</RESPONSE><ERROR>AUTO_WITHDRAWS_MUST_BE_ENABLED</ERROR><EOF>";
                            Response.Write(sResponse);
                            return;
                        }
                        string sHashTarget = GetHashTarget(sMinerGuid, sNetworkID);
                        string sql100 = "exec InsWork '" 
                            + clsStaticHelper.VerifyNetworkID(sNetworkID)
                            + "','" + clsStaticHelper.GuidOnly(sMinerGuid)
                            + "','" + clsStaticHelper.GetDouble(sThreadID1).ToString()
                            + "','" + clsStaticHelper.PurifySQL(sMiner,100)
                            + "','" + clsStaticHelper.PurifySQL(sHashTarget,100)
                            + "','" + clsStaticHelper.PurifySQL(sWork1,100)
                            + "','" + clsStaticHelper.PurifySQL(sIP,100)
                            + "'";
                        clsStaticHelper.mPD.Exec2(sql100, false);
                        sResponse = "<RESPONSE> <ADDRESS>" + sPoolRecvAddress + "</ADDRESS><HASHTARGET>" + sHashTarget 
                            + "</HASHTARGET><MINERGUID>" + sMinerGuid + "</MINERGUID><WORKID>" + sWork1 + "</WORKID></RESPONSE>";
                        sResponse = sResponse + "<END></HTML><EOF></EOF>" + Constants.vbCrLf;
                        Response.Write(sResponse);
                        return;
                    case "solution":
                        string sHealth2 = clsStaticHelper.GetHealth(sNetworkID);
                        if (sHealth2 == "HEALTH_DOWN")
                        {
                            sResponse = "<RESPONSE>HEALTH_DOWN</RESPONSE><ERROR>HEALTH_DOWN</ERROR><EOF>";
                            Response.Write(sResponse);
                            return;
                        }
                        string[] vSolution = sSolution.Split(new string[] { "," }, StringSplitOptions.None);
                        if (vSolution.Length < 12)
                        {
                                Response.Write("<ERROR>MALFORMED SOLUTION</ERROR><END>");
                                return;
                        }
                        // Insert entire solution record first, record validated in service
                        double dThreadStart = clsStaticHelper.GetDouble(vSolution[9]);
                        double dHashCounter = clsStaticHelper.GetDouble(vSolution[10]);
                        double dTimerStart = clsStaticHelper.GetDouble(vSolution[11]);
                        double dTimerEnd = clsStaticHelper.GetDouble(vSolution[12]);

                        // Calculate Thread HPS and Box HPS (this is for HPS reading only, not for HPS2)
                        double nBoxHPS = 1000.0 * dHashCounter / ((dTimerEnd) - (dTimerStart) + 0.01);
                        double dThreadId = clsStaticHelper.GetDouble(vSolution[7]);
                        double dThreadWork = clsStaticHelper.GetDouble(vSolution[8]);
                        double nThreadHPS = 1000.0 * (dThreadWork) / ((dTimerEnd) - (dThreadStart) + 0.01);
                        string sWorkId2 = vSolution[6];
                        string sBlockHash = vSolution[0];
                        string sBlockTime = vSolution[1];
                        string sPrevBlockTime = vSolution[2];
                        string sPrevHeight = vSolution[3];
                        string sHashSolution = vSolution[4];
                        string sBlockHash1 = "";
                        string sTxHash1 = "";
                        double nNonce = 0;
                        if (vSolution.Length > 13) nNonce = clsStaticHelper.GetDouble(vSolution[13]);
                        if (vSolution.Length > 14) sBlockHash1 = vSolution[14];
                        if (vSolution.Length > 15) sTxHash1 = vSolution[15];
                        string sSol2 = sSolution;
                        if (sSol2.Length > 400) sSol2 = sSol2.Substring(0, 398);
                        
                        // Track users OS so we can have some nice metrics of speed per OS, and total pool speed, etc:
                        string sql2 = "Update Work Set OS='" + clsStaticHelper.PurifySQL(sOS,50)
                            + "',endtime=getdate(),ThreadStart='"
                                + dThreadStart.ToString()
                                + "',SolutionBlockHash='" + sBlockHash1 + "',SolutionTxHash='" + sTxHash1 + "',solution='" 
                                + sSol2
                                + "',Nonce='" + nNonce.ToString() 
                                + "',HashCounter='" + dHashCounter.ToString()
                                + "',TimerStart='" + dTimerStart.ToString()
                                 + "',TimerEnd='"  + dTimerEnd.ToString()
                                  + "',ThreadHPS='"
                                 + nThreadHPS.ToString()
                                 + "',ThreadID='" + dThreadId.ToString()
                                  + "',ThreadWork='" 
                                  + dThreadWork.ToString() 
                                  + "',BoxHPS='"      
                                  + nBoxHPS.ToString()
                                  + "' where id = '" + clsStaticHelper.GuidOnly(sWorkId2)
                                  + "' and ENDTIME IS NULL ";
                        // Execute this in a way that overcomes deadlocks:

                        clsStaticHelper.mPD.Exec2(sql2, false);

                        string sStatus = "OK";
                        sResponse = "<RESPONSE><STATUS>" + sStatus + "</STATUS><WORKID>"
                                + sWorkId2 + "</WORKID></RESPONSE><END></HTML>" + Constants.vbCrLf;
                        Response.Write(sResponse);
                        return;
                    default:
                        Response.StatusCode = 405;
                        Response.StatusDescription = "REQUEST " + sAction + " UNKNOWN.";
                        return;
                }
            }
            catch (Exception ex)
            {
                string s = ex.StackTrace.ToString();
                double dLogLimiter = clsStaticHelper.LogLimiter();
                if (dLogLimiter > 900)
                {
                    bool bMask = false;
                    if (ex.Message.Contains("Thread was being aborted")) bMask = true;
                    if (!bMask) clsStaticHelper.Log("ACTION.ASPX: " + ex.Message + "," + s);
                    try
                    {
                        Response.Write("UNKNOWN TCP/IP ERROR<END>");
                    }
                    catch (Exception ex2)
                    {
                        clsStaticHelper.Log("Exception(2): " + ex2.Message);
                    }
                }
            }
        }
    }
}
