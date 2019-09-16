using Microsoft.VisualBasic;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using static USGDFramework.Shared;
using static USGDFramework.HexadecimalEncoding;

namespace BiblePayPool2018
{
    public partial class Action : System.Web.UI.Page
    {

        public static int MaxWithdrawalAmount = 11000;
        public static void AddLog(string sData)
        {
            string sPath = "AspxLog.txt";
            System.IO.StreamWriter sw = new System.IO.StreamWriter(sPath, true);
            string Timestamp = DateTime.Now.ToString();
            sw.WriteLine(Timestamp + ", " + sData);
            sw.Close();
        }
    
        public string MinerGuid(string sMiner, ref double dAutoWithdraws, ref double dFunded)
        {
            string sMinerGuid  = clsStaticHelper.AppCache(sMiner, this.Application);
            if (sMinerGuid.Length == 0)
            {
                // If the miner guid is not cached in memory we need to get it from the database:
                string sql = "Select miners.id,uz.withdrawalAddressValidated,miners.funded from miners with(nolock) inner join Uz(nolock) on Uz.id = miners.userid"
                    + " Where miners.username = '" + PurifySQL(sMiner, 99) + "'";
                DataTable dt =  mPD.GetDataTable2(sql,false);
                if (dt.Rows.Count > 0)
                {
                    string sID = dt.Rows[0]["id"].ToString();
                    
                    dAutoWithdraws =  GetDouble(dt.Rows[0]["withdrawalAddressValidated"]);
                    dFunded = GetDouble(dt.Rows[0]["funded"]);
                    clsStaticHelper.AppCache(sMiner, sID, Server, this.Application);
                    clsStaticHelper.AppCache(sMiner + "_AutoWithdraws", dAutoWithdraws.ToString(), Server, this.Application);
                    clsStaticHelper.AppCache(sMiner + "_Funded", dFunded.ToString(), Server, this.Application);
                    return sID;
                }
                else
                {
                    return String.Empty;
                }
            }
            else
            {
                dAutoWithdraws =  GetDouble(clsStaticHelper.AppCache(sMiner + "_AutoWithdraws", this.Application));
                dFunded = GetDouble(clsStaticHelper.AppCache(sMiner + "_Funded", this.Application));
                return sMinerGuid;
            }
        }

        public double GetCachedSolvedCount(string sMinerGuid, string sNetworkID)
        {
            string sKey = "solved" + "_" + sMinerGuid;
            double dCachedSolved =  GetDouble( clsStaticHelper.AppCache(sKey, this.Application));
            if (dCachedSolved == 0 || LogLimiter() > 500)
            {
                string sql = "Select count(*) as ct from Work with (nolock) where endtime is not null and userid = (select userid from miners where id='" 
                    +  GuidOnly(sMinerGuid) + "') and networkid='" +  VerifyNetworkID(sNetworkID) + "'";

                 dCachedSolved =  GetDouble( mPD.ReadFirstRow2(sql, 0, false));
                 clsStaticHelper.AppCache(sKey, dCachedSolved.ToString(), Server, this.Application);
            }
            return dCachedSolved;
        }
        
        private SystemObject Sys = null;
        private bool IsLoggedOn()
        {
            string sTheUser =  clsStaticHelper.GetCookie("username");
            string sThePass =  clsStaticHelper.GetCookie("password");
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
                     Log(" Cant depersist user " + ex.Message);
                }
                return bMyDepersist;
            }
            return false;
        }
        
        public string GetHashDifficulty2(int iThreadID, long lSharesSolved, double dHistoricalHashPs, string sNetworkID)
        {
            string sZeroes = "000000";
            double dLL = LogLimiter();
            string sHashTarget = sZeroes + USGDFramework.modCryptography.SHA256(dLL.ToString()) 
                + USGDFramework.modCryptography.SHA256(dLL.ToString()) + USGDFramework.modCryptography.SHA256(dLL.ToString());
            sHashTarget = Strings.Left(sHashTarget, 64);
            return sHashTarget;
        }

        private string GetHashTarget(int iThreadID, string sMinerGuid, string sNetworkID)
        {
            string sHashTarget = String.Empty;
            double dCSS2 = GetCachedSolvedCount(sMinerGuid, sNetworkID);
            double dHPS = 0;
            sHashTarget = GetHashDifficulty2(iThreadID, (long)dCSS2, dHPS, sNetworkID);
            return sHashTarget;
        }
       
        private bool AnalyzeTip(int iTipHeight, String sNetworkID)
        {
            if (iTipHeight >  nCurrentTipHeightMain)
            {
                 GetTipHeight(sNetworkID);
            }
            if (sNetworkID == "main")
            {
                if (iTipHeight <  nCurrentTipHeightMain - 1 || iTipHeight >  nCurrentTipHeightMain) return false;
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
            string sRawTx =  GetRawTransaction(sNetworkId, sTxId);
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
                string sql = "Select * from Orders where networkid='" +  VerifyNetworkID(sNetworkID) 
                    + "' and WalletOrder=1 and WalletOrderProcessed=0 and TxId is not null";
                DataTable dt10 =  mPD.GetDataTable2(sql);
                for (int i = 0; i < dt10.Rows.Count; i++)
                {
                    string id = dt10.Rows[i]["id"].ToString();
                    string sProductID = dt10.Rows[i]["ProductID"].ToString();
                    string sUserGuid = dt10.Rows[i]["UserId"].ToString();
                    string sTxId = dt10.Rows[i]["TxId"].ToString();
                    double dAmount = ToDouble(dt10.Rows[i]["Amount"]);
                     Log(" Processing order " + id);

                    long lConfirms = RawTransactionAge(sNetworkID, sRecAddr, sTxId, dAmount);
                    // Pull the User Address info
                    if (lConfirms > 4)
                    {
                         Log(" Got order confirm! processing " + id);

                        string sql20 = "Select * from Uz where id ='" +  GuidOnly(sUserGuid) + "'";

                        DataTable dtU =  mPD.GetDataTable2(sql20);
                        if (dtU.Rows.Count > 0)
                        {
                            string sql21 = "Select * from Products where productid = '" +  PurifySQL(sProductID,50)
                                + "' and networkid='" +  VerifyNetworkID(sNetworkID) + "'";
                            DataTable dt22 =  mPD.GetDataTable2(sql21);
                            if (dt22.Rows.Count == 0)
                            {
                                string sql23 = "Update Orders set Status1='UNABLE TO LOCATE PRODUCT',WalletOrderProcessed=1,Updated=getdate() where id='" +  GuidOnly(id) + "'";
                                 mPD.Exec2(sql23);
                                break;
                            }
                            double dPrice = Convert.ToDouble(dt22.Rows[0]["price"].ToString());
                            double dBBP =  PriceInBBP(dPrice / 100);
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
                            shipAddr.first_name =  GetNameElement(sDelName, 0).ToUpper();
                            shipAddr.last_name =  GetNameElement(sDelName, 1).ToUpper();
                            // The user has a good address, enough BBP, go ahead and buy it - Place the order, and track it
                            string sToken = USGDFramework.clsStaticHelper.GetConfig("MouseToken");

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
                            Zinc.BiblePayMouse.payment_method pm1 =  TransferPaymentMethodIntoMouse();
                            Zinc.BiblePayMouse.billing_address ba1 =  TransferBillingAddressIntoMouse();
                            Zinc.BiblePayMouse.retailer_credentials rc1 =  TransferRetailerCredentialsIntoMouse();
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
                                 PurifySQL(sMouseId,55)
                                + "',Status1='PLACED' where ID = '" +  GuidOnly(id) + "'";
                             mPD.Exec2(sql);
                        }
                    }
                }
                GetOrderStatusUpdates();
            }
            catch(Exception ex)
            {
                 Log("Error while placing wallet orders " + ex.Message);
            }
                    
        }
        
        double GetProductPrice(string sProductId, string sNetworkID)
        {
                string sql = "Select * from Products where networkid='" +  VerifyNetworkID(sNetworkID)
                + "' and Productid='" +  PurifySQL(sProductId,55) + "' and inwallet=1";
                DataTable dt =  mPD.GetDataTable2(sql);
                if (dt.Rows.Count > 0)
                {
                    double dPrice = ToDouble(dt.Rows[0]["Price"]);
                    double dBBP =  PriceInBBP(dPrice / 100);
                    return dBBP;
                }
                return 0;
        }

        double CalculateGospelClickBounty(string sID, string sClickerGuid)
        {
            string sql = "Select * From Links where id = '" +  GuidOnly(sID) + "'";
            DataTable dt =  mPD.GetDataTable2(sql);
            if (dt.Rows.Count > 0)
            {
                double dBounty = ToDouble(dt.Rows[0]["PaymentPerClick"]);
                double dBudget = ToDouble(dt.Rows[0]["Budget"]);
                double dClicks = ToDouble(dt.Rows[0]["Clicks"]);
                double dSpent = dClicks * dBounty;
                // Ensure this user was not awarded this link click before:
                sql = "Select count(*) ct from transactionLog where transactiontype='GOSPEL_BOUNTY' and userid='" 
                    +  GuidOnly(sClickerGuid)
                    + "' and destination='" +  GuidOnly(sID)
                    + "'";
                double AwardCount =  mPD.GetScalarDouble2(sql, "ct");
                if (AwardCount == 0)
                {
                    sql = "Update Links Set Clicks=isnull(clicks,0)+1 where id = '" +  GuidOnly(sID) + "'";
                     mPD.Exec2(sql);
                    if (dSpent < dBudget)
                    {
                        return dBounty;
                    }
                }
                return 0;

            }
            return 0;
        }

        public string PenTest(string sPostData, string sUserId, string sUserName)
        {
            string sTemp = sPostData.ToUpper();
            sTemp = sTemp.Replace(" ", "");
            bool bDirty = sTemp.Contains("SELECT") ||
                sTemp.Contains("INSERT") || sTemp.Contains("UPDATE") || sTemp.Contains("DELETE") ||
                sTemp.Contains("DROPTABLE") || sTemp.Contains("EXEC");
            if (bDirty)
            {
                LogSQLI(sUserId + "-" + sUserName + ": " + sPostData);
                sPostData = "";
            }
            return sPostData;
        }

        protected void Page_Load(object sender, System.EventArgs e)
        {
                try
                {
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
                string sSessionID = (Request.Headers["SessionID"] ?? "").ToString();
                string sWorkerName1 = (Request.Headers["WorkerID1"] ?? "").ToString();
                string sWorkerName2 = (Request.Headers["WorkerID2"] ?? "").ToString();

                string sNID = (Request.QueryString["networkid"] ?? "").ToString();
                if (sNID != "") sNetworkID = sNID;
                string MinerID1 = (Request.QueryString["minerguid"] ?? "").ToString();
                if (MinerID1 != "")
                    sMiner = MinerID1;
                if (sReqAction != "")
                    sAction = sReqAction;
                sMiner = PenTest(sMiner, sMiner, sMiner);
                sBBPAddress = PenTest(sBBPAddress, "", "");
                sAction = PenTest(sAction, "", "");
                sSolution = PenTest(sSolution, sMiner, sMiner);
                sNetworkID = PenTest(sNetworkID, sMiner, sMiner);
                sAgent = PenTest(sAgent, "", "");
                sWorkID = PenTest(sWorkID, "", "");
                sThreadID1 = PenTest(sThreadID1, "", "");
                sReqAction = PenTest(sReqAction, "", "");
                sTip = PenTest(sTip, "", "");

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
                    if (dAgent > 1000 && dAgent < 1444)
                    {
                        string sResponse1 = "<RESPONSE>PLEASE UPGRADE</RESPONSE><ERROR>PLEASE UPGRADE</ERROR><EOF></HTML>";
                        Response.Write(sResponse1);
                        return;
                    }
                if (sReqAction == "verify_email")
                {
                    string sID = (Request.QueryString["id"] ?? "").ToString();
                    string sql = "Select username,id,email,password from Uz where id = '" +  GuidOnly(sID) + "'";
                    string email =  mPD.ReadFirstRow2(sql, "email");
                    string sUserName =  mPD.ReadFirstRow2(sql, "username");

                    if (email.Length > 3)
                    {
                        // Verified
                        sql = "Update Uz set EmailVerified = 1 where id = '" +  GuidOnly(sID) + "'";
                        mPD.Exec2(sql);
                        string sBody = "<html>Dear " + sUserName.ToUpper() + ", <br><br>Your E-Mail has been verified.<br><br>  Have a great day! <br><br> May Jesus' Kingdom Extend for Infinity,<br>BiblePay Support<br></html>";
                        Response.Write(sBody);
                        return;
                    }
                }
                else if (sReqAction == "api_proposals")
                {
                    string sql = "Select * from Proposal where network = 'main'";
                    string data =  DataDump(sql, "resubmit");
                    Response.Write(data);
                    return;
                }
                else if (sReqAction == "api_superblock")
                {
                    string sql = "Select * from Superblocks where added > getdate()-7";
                    string data =  DataDump(sql, "");
                    Response.Write(data);
                }
                else if (sReqAction == "getcpk")
                {
                    string data = GetDatalist("cpk");
                    Response.Write(data);
                   return;
                }
                else if (sReqAction == "getbms")
                {
                    string data = GetDatalist("cpk-bms");
                    Response.Write(data);
                    return;
                }
                else if (sReqAction == "getbmsuser")
                {
                    string data = GetDatalist("cpk-bmsuser");
                    Response.Write(data);
                    return;
                }
                else if (sReqAction == "cameroonchildren")
                {
                    string sData = GetCameroon();
                    byte[] bytes = StrToByteArray(sData);
                    Response.Clear();
                    Response.ContentType = "text/csv";
                    Response.AddHeader("Content-Length", bytes.Length.ToString());
                    Response.AddHeader("Content-disposition", "attachment; filename=\"cameroonchildren.csv" + "\"");
                    Response.BinaryWrite(bytes);
                    Response.Flush();
                    Response.End();
                    return;
                }
                else if (sReqAction == "cpk")
                {
                    // 5-2-2019
                    string sCPK = GetCameroon();
                    string sTest = "";
                    Response.Write(sCPK + "<EOF></HTML>");
                    return;
                }
                else if (sReqAction == "price-quote-btc")
                {
                    double dPrice = GetPriceQuote("BTC/USD");
                    string sResult = "<MIDPOINT>" + dPrice.ToString() + "</MIDPOINT><EOF>";
                    Response.Write(sResult);
                    return;
                }
                else if (sReqAction == "health-post")
                {
                    string sBlocks = PurifySQL(ExtractXML(sSolution, "<BLOCKS>", "</BLOCKS>").ToString(), 50);
                    string sHash = PurifySQL(ExtractXML(sSolution, "<HASH>", "</HASH>").ToString(), 125);
                    string sPamHash = PurifySQL(ExtractXML(sSolution, "<PAMHASH>", "</PAMHASH>").ToString(), 125);
                    string sVotes = PurifySQL(ExtractXML(sSolution, "<GSCVOTES>", "</GSCVOTES>").ToString(), 50);
                    string sStatus = PurifySQL(ExtractXML(sSolution, "<STATUS>", "</STATUS>").ToString(), 255);
                    string sql = "Delete from SanctuaryHealth where ip='" + sIP
                        + "'\r\nInsert into SanctuaryHealth (id, added, ip, pamhash, blocks, hash, votes, status) values (newid(), getdate(), '" 
                        + sIP + "','" + sPamHash + "','" + sBlocks + "','" 
                        + sHash + "','" + sVotes + "','" + sStatus + "')";
                    mPD.Exec2(sql, false, false);
                    Response.Write("<RESPONSE>Acknowledged</RESPONSE><EOF></HTML>\r\n");
                }
                else if (sReqAction == "price-quote-bbp")
                {
                    double dPrice = GetPriceQuote("BBP/BTC");
                    string sPrice = dPrice.ToString("0." + new string('#', 339));
                    string sResult = "<MIDPOINT>" + sPrice + "</MIDPOINT><EOF>";
                    Response.Write(sResult);
                    return;
                }
                else if (sReqAction == "metrics")
                {
                    string sql = "Select count(*) ct from Orphans";
                    double dOrphanCount =  mPD.GetScalarDouble2(sql, "ct");
                    string sMetrics = "<METRICS><ORPHANCOUNT>" + dOrphanCount.ToString() + "</ORPHANCOUNT></METRICS>";
                    Response.Write(sMetrics);
                    return;
                }
                else if (sReqAction == "api_contact")
                {
                    string sContactType = (Request.QueryString["contact_type"] ?? "").ToString();
                    string sql = "Select * from objectcontact where contacttype='" + sContactType + "'";
                    string data =  DataDump(sql, "");
                    Response.Write(data);
                    Response.End();
                    return;
                }
                else if (sReqAction == "wcgrac")
                {
                    string cpid = (Request.QueryString["cpid"] ?? "").ToString();
                    string sql = "select avg(wcgrac) as wcgrac from Superblocks where cpid='" + cpid + "' and added > getdate()-3 and wcgrac > 0 ";
                    double dRAC =  mPD.GetScalarDouble2(sql, "wcgrac", false);
                    string sMetrics = "<WCGRAC>" + dRAC.ToString() + "</WCGRAC><EOF>";
                    Response.Write(sMetrics);
                    return;
                }
                else if (sReqAction == "teamrac")
                {
                    string sql = "select max(totalrac) a from superblocks where height=(select max(height) from superblocks)";
                    double dRAC =  mPD.GetScalarDouble2(sql, "a", false);
                    string sMetrics = "<TEAMRAC>" + dRAC.ToString() + "</TEAMRAC><EOF>";
                    Response.Write(sMetrics);
                    return;
                }
                else if (sReqAction == "unbanked")
                {
                    USGDFramework.MyWebClient myHttp = new USGDFramework.MyWebClient();
                    var sTeamURl = "https://forum.biblepay.org/tools/api.php?action=unbanked";
                    var sData = myHttp.DownloadString(sTeamURl);
                    Response.Write(sData);
                    return;
                }
                else if (sReqAction == "continue_action")
                {
                    string sID = (Request.QueryString["id"] ?? "").ToString();
                    if (sID.Length > 1)
                    {
                        string sql10 = "Select * From RequestLog where id = '" +  GuidOnly(sID)
                            + "' and processed is null";
                        DataTable dtReq =  mPD.GetDataTable2(sql10);
                        if (dtReq.Rows.Count == 0)
                        {
                            Response.Write("Sorry, No Transaction present.");
                            return;
                        }

                        string sql2 = "Update RequestLog set IP2='" +  PurifySQL(sIP, 80)
                        + "' Where id = '" +  GuidOnly(sID)
                        + "'";
                         mPD.Exec2(sql2);

                        if (dtReq.Rows.Count > 0)
                        {
                            string sUserGuid = dtReq.Rows[0]["userguid"].ToString();
                            sql10 = "Select * from Uz where id = '" +  GuidOnly(sUserGuid)
                            + "'";
                            DataTable dtU =  mPD.GetDataTable2(sql10);
                            if (dtU.Rows.Count > 0)
                            {
                                string sUG = dtU.Rows[0]["id"].ToString();
                                string sNetworkID2 = dtReq.Rows[0]["Network"].ToString();
                                double Amount =  GetDouble(dtReq.Rows[0]["Amount"]);
                                string sUN = dtU.Rows[0]["username"].ToString();
                                double dEV =  GetDouble(dtU.Rows[0]["EmailVerified"]);
                                double dUV =  GetDouble(dtU.Rows[0]["SendVerified"]);
                                double dWithdraws =  GetDouble(dtU.Rows[0]["Withdraws"]);
                                double dLimit =  GetDouble(dtU.Rows[0]["Limit"]);
                                if (dLimit < 1001) dLimit = 1001;
                                double oldBalance = 0;
                                double dImmature = 0;
                                string sDest = dtReq.Rows[0]["Address"].ToString();
                                GetUserBalances(sNetworkID2, sUG, ref oldBalance, ref dImmature);
                                double dAvail = oldBalance - dImmature;
                                if (dAvail < Amount)
                                {
                                    Response.Write("Sorry, Balance too low.");
                                    return;
                                }
                                if (Amount > dLimit)
                                {
                                    Response.Write("This withdrawal amount exceeds BiblePay's hot wallet limit; please withdraw less than " + dLimit.ToString() + " bbp.  ");

                                    return;
                                }

                                if (Amount < 3 || Amount > 40000)
                                {
                                    Response.Write("Sorry, amount out of range (1).");
                                    return;
                                }
                                int iHeight =  GetTipHeight(sNetworkID2);
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
                                double dAddrUseCt =  mPD.GetScalarDouble2(sql, "a");
                                bool bIdentityVerified = false;
                                if (dAddrUseCt > 1 || dUV == 1 || dWithdraws > 3) bIdentityVerified = true;
                                if (!bIdentityVerified)
                                {
                                    string sNarr1 = "Sorry, your identity must be verified.  Please send an e-mail to contact@biblepay.org with reference number "
                                    + sID + ".";
                                    Response.Write("<br><br>Verifying user activity...<br>Verifying transactions...<br><br>");
                                }

                                sql = "Select Value from System where systemkey='withdraw'";
                                string sStatus =  mPD.GetScalarString2(sql, "value");
                                if (sStatus == "DOWN")
                                {
                                    Response.Write("Sorry, withdrawals are down temporarily, please try later.");
                                    return;
                                }
                                // Ensure last withdrawal hasnt happened in a while...

                                sql = "select max(updated) upd from TransactionLog where added > getdate() - 2  and transactionType = 'withdrawal' and amount > 9000";
                                string sLast =  mPD.GetScalarString2(sql, "upd");
                                if (sLast == "") sLast = Convert.ToDateTime("1/1/1970").ToShortDateString();

                                double dDiff = Math.Abs(DateAndTime.DateDiff(DateInterval.Second, DateTime.Now, Convert.ToDateTime(sLast)));
                                if (dDiff < 120)
                                {
                                    Response.Write("Sorry, a user has recently withdrawn a large sum of BBP.  Please wait 2 minutes and try again.  <br><br>Sorry for the inconvenience.<br><br>Thank you for using BiblePay.");
                                    return;
                                }

                                sql = "select max(updated) upd from TransactionLog where added > getdate() - 2  and transactionType = 'withdrawal' and amount > '" + MaxWithdrawalAmount.ToString() + "'";
                                string sLast2 =  mPD.GetScalarString2(sql, "upd");
                                if (sLast2 == "")
                                    sLast2 = Convert.ToDateTime("1/1/1970").ToShortDateString();

                                double dDiff2 = Math.Abs(DateAndTime.DateDiff(DateInterval.Second, DateTime.Now, Convert.ToDateTime(sLast2)));
                                if (dDiff2 < 90)
                                {
                                    Response.Write("Sorry, a user has recently withdrawn BBP.  Please wait 90 seconds and try again.  This restriction will be lifted on Jan 31, 2018 pending positive Pool Health.");
                                    return;
                                }
                                
                                sql = "select max(added) add1 from Withdraws where added > getdate() - 2 and amount > 500";
                                sLast2 =  mPD.GetScalarString2(sql, "add1");
                                if (sLast2 == "") sLast2 = Convert.ToDateTime("1/1/1970").ToShortDateString();

                                dDiff2 = Math.Abs(DateAndTime.DateDiff(DateInterval.Second, DateTime.Now, Convert.ToDateTime(sLast2)));
                                if (dDiff2 < 121)
                                {
                                    Response.Write("Sorry, a user has recently withdrawn a large sum of BBP.  Please wait 120 seconds and try again. ");
                                    return;
                                }

                                // Reverify balance
                                double newBalance = oldBalance - Amount;
                                string sTXID = "";
                                try
                                {
                                    
                                    sTXID =  A1(sID, sDest, Amount, sNetworkID2, sIP);
                                    if (sTXID.Length > 5)
                                    {
                                        bool bTrue =  AdjustUserBalance(sNetworkID2, sUserGuid, -1 * Amount);
                                         InsTxLog(sUN, sUG, sNetworkID2,
                                            iHeight, sTXID, Amount, oldBalance, newBalance, sDest, "withdrawal", "");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    sTXID = "ERR60514";
                                }
                                if (sTXID.Length > 5)
                                {
                                    sql = "Update RequestLog set TXID='" +  PurifySQL(sTXID, 125)
                                    + "',processed=1 where id = '" +  GuidOnly(sID)
                                    + "'";
                                     mPD.Exec2(sql);
                                }

                                sql = "Insert into Withdraws (id, amount, added, address, userguid) values (newid(), '" + Amount.ToString() + "',getdate(),'" + sDest + "','" + sUserGuid + "')";
                                try
                                {
                                     mPD.Exec2(sql, true, true);
                                    string sBody = "Dear BiblePay, <br>We have sent " + Amount.ToString() + " out to " + sUN + " for tx id " + sTXID + ".<br><br>BiblePay Team<br>";

                                    bool sent = Sys.SendEmail("rob@biblepay.org", "BiblePay Sendmoney", sBody, true, true);

                                }
                                catch (Exception ex)
                                {
                                     Log("SendWithdraw::" + ex.Message);
                                }

                                // Notify of the transaction
                                string sSite = USGDFramework.clsStaticHelper.GetConfig("WebSite");

                                string sNarr = "<img src='" + sSite + "images/logo.png' width=200 height=200><p><p><br><br> God Bless You, and know that God Loves You. <br>"
                                    +" Jesus is the way, the truth, and the life.  Know that there is no other true God that can provide salvation other than Jesus!  "
                                    +"<br><br><p><p>BBP Withdraw in the amount of <font color=green>"
                                    + Amount.ToString() + "</font> has been transmitted to " + sDest
                                    + " in TransactionID " + sTXID + ". <br><p><br><p><h3><font color=maroon> Thank you for using BiblePay.";
                                Response.Write(sNarr);
                                return;
                            }
                        }
                    }
                }

                    if (sReqAction=="password_recovery")
                    {
                        string sID1 = (Request.QueryString["id"] ?? "").ToString();
                        string sql = "Select userid from passwordreset where id = '" +  GuidOnly(sID1) + "'";
                        DataTable dt100 =  mPD.GetDataTable2(sql);
                        if (dt100.Rows.Count==0)
                        {
                            Response.Write("Sorry, no password reset action available.");
                            return;
                        }
                        string sID = dt100.Rows[0]["userid"].ToString();
                        sql = "Select username,id,email,password from Uz where id = '" +  GuidOnly(sID) + "'";
                        string email =  mPD.ReadFirstRow2(sql, "email");
                        if (email.Length > 3)
                        {
                            // Change the uz password for them and notify them:
                            string sUserName =  mPD.ReadFirstRow2(sql, "username");
                            string sNewPass = Guid.NewGuid().ToString();
                            sNewPass = sNewPass.Replace("-", "");
                            sNewPass = sNewPass.Substring(0, 7);
                            sql = "Update Uz Set Password = '" + USGDFramework.modCryptography.SHA256(sNewPass) + "' where id = '" +  GuidOnly(sID) + "'";
                             mPD.Exec2(sql);
                            sql = "Delete from passwordReset where id = '" +  GuidOnly(sID1) + "'";
                             mPD.Exec2(sql);
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
                        string sql = "Select * from Links where id like '" +  PurifySQL(sID,100) + "%'";
                        DataTable dt =  mPD.GetDataTable2(sql);
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
                                        string sql100 = "Select isnull(emailverified,0) EV from Uz where id='" +  GuidOnly(Sys.UserGuid)
                                        + "'";
                                        double d1 =  GetScalarDouble2(sql100, "EV");
                                        if (d1 == 1)
                                        {
                                            double dBalance =  AwardBounty(sGuid, Payer, -1 * dBounty, "GOSPEL_CAMPAIGN", 0, 
                                                Guid.NewGuid().ToString(), "main", sID.ToString(), false);
                                            if (dBalance > 0)
                                            {
                                                // Transfer the money to the Clicker
                                                dBountyPaid = dBounty;
                                                dBalance =  AwardBounty(sGuid, Sys.UserGuid, dBounty, "GOSPEL_BOUNTY", 0, Guid.NewGuid().ToString(), "main", sID.ToString(), false);
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
                             
                        }
                    }

                long lMaxThreadCount = 60;
                if (Conversion.Val(sThreadID1) > lMaxThreadCount)
                {
                    Response.Write("<RESPONSE><ERROR>MAX THREAD COUNT OF " + Strings.Trim(lMaxThreadCount.ToString()) + " EXCEEDED-PLEASE LOWER THREADCOUNT</ERROR></RESPONSE><END></HTML>");
                    return;
                }
                string sResponse = "";
                if (! ValidateNetworkID(sNetworkID))
                {
                    Response.Write("<ERROR>INVALID NETWORK ID [" + sNetworkID + "]</ERROR><END></HTML>");
                    return;
                }
                ////////////////////////////////////////////// READY TO MINE ///////////////////////////////////////////////////////////////////////////
                double dAutoWithdrawsEnabled = 0;
                double dFunded = 0;
                string sMinerGuid = MinerGuid(sMiner, ref dAutoWithdrawsEnabled, ref dFunded);

                if (sAction !="get_products" && sAction != "buy_product" 
                    && sAction != "get_product_escrow_address" && sAction != "order_status" && sAction != "order" && sAction != "trades" 
                    && sAction != "cancel" && sAction != "escrow" && sAction != "process_escrow")
                {
                    // These two commands use the BBP address instead for the PK
                    if (sMinerGuid.Length == 0)
                    {
                        Response.Write("<ERROR>INVALID MINER GUID " + sMiner + "</ERROR><END></HTML><EOF>" + Constants.vbCrLf);
                        return;
                    }
                }
              
                string sPoolRecvAddress =  clsStaticHelper.GetNextPoolAddress(sNetworkID, Server, this.Application);
                
                if (sAction == "cancel") sAction = "order";

                switch (sAction)
                {
                    case "process_escrow":
                        // update these matches with a guid here 
                        string sql11 = "Select * from Trade where networkid='"
                           + VerifyNetworkID(sNetworkID)
                           + "' and (match is not null OR MatchSell Is Not Null) and Address='"
                           + PurifySQL(sBBPAddress, 100)
                           + "' and EscrowTxId is null";
                        DataTable dt = mPD.GetDataTable2(sql11);
                        string sTrades = "<RESPONSE><ESCROWS>";
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            string id = dt.Rows[i]["id"].ToString();
                            string Action = dt.Rows[i]["Act"].ToString();
                            double amount = ToDouble(dt.Rows[i]["price"]);
                            double quantity = ToDouble(dt.Rows[i]["quantity"]);
                            // Note, on the Buy side: total = amount*quantity, on the sell side, amount = QUANTITY & coin=colored(401)
                            double total = 0;
                            if (Action == "BUY")
                            {
                                total = amount * quantity;
                            }
                            else if (Action == "SELL")
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
                        Response.Write(sTrades);
                        break;

                    case "order_status":
                        SendWalletOrders(sNetworkID, sPoolRecvAddress);
                        string sql = "Select * from Orders where networkid='" + VerifyNetworkID(sNetworkID)
                            + "' and Address='" + PurifySQL(sBBPAddress, 100)
                            + "' ";
                        DataTable dt10 = mPD.GetDataTable2(sql);
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
                        Log(sO);
                        Response.Write(sO);
                        break;
                    case "execution_history":
                        break;

                    case "finishtrades":
                        break;

                    case "get_products":
                        try
                        {
                            sql = "Select * from Products where networkid='" + VerifyNetworkID(sNetworkID)
                                + "' and inwallet = 1";
                            dt = mPD.GetDataTable2(sql);
                            string sQ = "<RESPONSE><PRODUCTS>";
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                double dPrice2 = ToDouble(dt.Rows[i]["Price"]);
                                double dBBP = PriceInBBP(dPrice2 / 100);
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
                        catch (Exception ex)
                        {
                            Log("ERR IN GET_PRODUCTS " + ex.Message);
                        }
                        break;
                    case "get_product_escrow_address":
                        string sP = "<RESPONSE><PRODUCT_ESCROW_ADDRESS>" + sPoolRecvAddress + "</PRODUCT_ESCROW_ADDRESS></RESPONSE></EOF></HTML>";
                        Response.Write(sP);
                        break;
                    case "buy_product":
                        try
                        {
                            bool bSuccess = EnsureBBPUserExists(sBBPAddress);
                            if (!bSuccess)
                            {
                                Response.Write("<RESPONSE><ERR>BAD USERID [ERROR: 1002, PubBBPAddress: " + sBBPAddress + "]</ERR></RESPONSE></EOF>");
                                return;
                            }
                            string sBBPUserGuid = GetBBPUserGuid(sBBPAddress);
                            if (sBBPAddress == "" || sBBPUserGuid == "")
                            {
                                Response.Write("<RESPONSE><ERR>BAD USERID-GUID [" + sBBPAddress + "]</ERR></RESPONSE></EOF>");
                                return;
                            }
                            string sTXID = ExtractXML(sSolution, "<TXID>", "</TXID>").ToString();
                            double vOUT = ToDouble(ExtractXML(sSolution, "<VOUT>", "</VOUT>"));
                            string sProdID = ExtractXML(sSolution, "<PRODUCTID>", "</PRODUCTID>").ToString();
                            string sName = ExtractXML(sSolution, "<NAME>", "</NAME>").ToString();
                            string sDryRun = ExtractXML(sSolution, "<DRYRUN>", "</DRYRUN>").ToString();
                            string sAddress1 = ExtractXML(sSolution, "<ADDRESS1>", "</ADDRESS1>").ToString();
                            string sAddress2 = ExtractXML(sSolution, "<ADDRESS2>", "</ADDRESS2>").ToString();
                            string sCity = ExtractXML(sSolution, "<CITY>", "</CITY>").ToString();
                            string sState = ExtractXML(sSolution, "<STATE>", "</STATE>").ToString();
                            string sZip = ExtractXML(sSolution, "<ZIP>", "</ZIP>").ToString();
                            string sPhone = ExtractXML(sSolution, "<PHONE>", "</PHONE>").ToString();
                            double dAmt = ToDouble(ExtractXML(sSolution, "<AMOUNT>", "</AMOUNT>"));
                            // Verify the address
                            string sql80 = "Update Uz Set Updated=getdate(),DelName='"
                                + PurifySQL(sName, 125)
                                + "',Address1='" + PurifySQL(sAddress1, 125)
                                + "',Address2='" + PurifySQL(sAddress2, 125)
                                + "',City='" + PurifySQL(sCity, 125)
                                + "',State='"
                                + PurifySQL(sState, 25)
                                + "',Zip='" + PurifySQL(sZip, 25)
                                + "',Country='US',Phone='" + PurifySQL(sPhone, 100)
                                + "' WHERE ID='" + GuidOnly(sBBPUserGuid)
                                + "'";
                            mPD.Exec2(sql80);
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
                                string sql199 = "Select count(*) ct from Orders where networkid='" + VerifyNetworkID(sNetworkID)
                                    + "' and WalletOrder=1 and txid is not null and address='" + PurifySQL(sBBPAddress, 100)
                                    + "'";
                                double dBot = mPD.GetScalarDouble2(sql199, "ct");
                                if (dBot > 0) banned = true;
                            }
                            if (sDryRun != "DRY" && !banned)
                            {
                                // Ensure this guy hasnt bought any other testnet orders
                                sql = "Insert into Orders (id,added) values ('" + GuidOnly(sOrderGuid)
                                    + "',getdate())";
                                mPD.Exec2(sql);
                                sql = "Update Orders Set NetworkID='" + VerifyNetworkID(sNetworkID)
                                    + "',WalletOrderProcessed=0,Address='"
                                    + PurifySQL(sBBPAddress, 100)
                                    + "',WalletOrder=1,Userid='" + GuidOnly(sBBPUserGuid)
                                    + "',ProductId='"
                                    + PurifySQL(sProdID, 100)
                                    + "',Title='',Added=getdate(),Status1='Placed',TXID='"
                                    + PurifySQL(sTXID, 125)
                                    + "',Amount='"
                                    + dAmt.ToString()
                                    + "' where id = '" + GuidOnly(sOrderGuid)
                                    + "'";
                                mPD.Exec2(sql);
                            }
                            else
                            {
                                sTXID = sOrderGuid;
                            }

                            Response.Write("<RESPONSE><STATUS>SUCCESS</STATUS><TXID>" + sTXID + "</TXID><ORDERID>" + sOrderGuid + "</ORDERID></RESPONSE></EOF></HTML>");
                        }
                        catch (Exception ex)
                        {
                            Log(" WHILE BUYING PROD " + ex.Message);
                            Response.Write("<RESPONSE><ERR>UNABLE TO PASS ORDER THROUGH VIABLE SANCTUARY. PLEASE TRY AGAIN LATER. </ERR></RESPONSE></EOF>");
                        }
                        break;
                    case "readytomine2":
                        string sWork1 = Guid.NewGuid().ToString();
                        string sHealth = GetHealth(sNetworkID);
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
                        string sHashTarget = GetHashTarget((int)ToDouble(sThreadID1), sMinerGuid, sNetworkID);
                        string sql100 = "exec InsWork '"
                            + VerifyNetworkID(sNetworkID)
                            + "','" + GuidOnly(sMinerGuid)
                            + "','" + GetDouble(sThreadID1).ToString()
                            + "','" + PurifySQL(sMiner, 100)
                            + "','" + PurifySQL(sHashTarget, 100)
                            + "','" + PurifySQL(sWork1, 100)
                            + "','" + PurifySQL(sIP, 100)
                            + "','" + PurifySQL(sSessionID, 99) 
                            + "','" + PurifySQL(sWorkerName1, 99) 
                            + "','" + PurifySQL(sWorkerName2, 99) + "'";


                        mPD.SureExec(sql100, false, true);
                        string sABN = "";
                        if (dFunded == 1)
                        {
                            sql = "select top 1 solutionBlockHash from work where validated=1 and endtime is not null and updated > getdate()-.01 and error is null order by endtime desc";
                            DataTable dt1 = mPD.GetDataTable2(sql, false);
                            if (dt1.Rows.Count > 0)

                            {
                                try
                                {
                                    sABN = dt1.Rows[0]["solutionBlockHash"].ToString();
                                 
                                    string sABN2 = ConvertFromHexStringToAscii(sABN);
                                 
                                    string guid1 = ExtractXML(sABN2, "<MINERGUID>", "</MINERGUID>").ToString();
                                    string oldGuid = "<MINERGUID>" + guid1.ToLower() + "</MINERGUID>";
                                    string newGuid = "<MINERGUID>" + sMinerGuid.ToLower() + "</MINERGUID>";
                                    string oldHex = ToHexString(oldGuid).ToLower();
                                    string newHex = ToHexString(newGuid).ToLower();
                                    sABN = sABN.Replace(oldHex, newHex);
                                }
                                catch(Exception ex)
                                {
                                    Response.Write("<ERR1>" + ex.Message + "</ERR1>");
                                }
                             }

                        }
                        sResponse = "<RESPONSE> <ADDRESS>" + sPoolRecvAddress + "</ADDRESS><HASHTARGET>" + sHashTarget
                            + "</HASHTARGET><BLOCKDATA>" + sABN + "</BLOCKDATA><MINERGUID>" 
                            + sMinerGuid + "</MINERGUID><WORKID>" + sWork1 + "</WORKID>";
                        // Proto v2.0:  Deadline && PoolCommand
                        int nNow = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                        int nDeadline = nNow + (60 * 29);
                        if (nDeadline > 0)
                        {
                            sResponse += "<POOLCOMMAND></POOLCOMMAND><DEADLINE>" + nDeadline.ToString() + "</DEADLINE>";
                        }
                        sResponse = sResponse + "</RESPONSE><END></HTML><EOF></EOF>" + Constants.vbCrLf;

                        Response.Write(sResponse);
                        return;
                    case "solution":
                        string sHealth2 = GetHealth(sNetworkID);
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
                        double dThreadStart = GetDouble(vSolution[9]);
                        double dHashCounter = GetDouble(vSolution[10]);
                        double dTimerStart = GetDouble(vSolution[11]);
                        double dTimerEnd = GetDouble(vSolution[12]);

                        // Calculate Thread HPS and Box HPS (this is for HPS reading only, not for HPS2)
                        double nBoxHPS = 1000.0 * dHashCounter / ((dTimerEnd) - (dTimerStart) + 0.01);
                        double dThreadId = GetDouble(vSolution[7]);
                        double dThreadWork = GetDouble(vSolution[8]);
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
                        if (vSolution.Length > 13) nNonce = GetDouble(vSolution[13]);
                        if (vSolution.Length > 14) sBlockHash1 = vSolution[14];
                        if (vSolution.Length > 15) sTxHash1 = vSolution[15];
                        string sSol2 = sSolution;
                        if (sSol2.Length > 400) sSol2 = sSol2.Substring(0, 398);
                        if (vSolution.Length < 10 || sSol2.Length < 70)
                        {
                            Log("bad: " + sSol2);
                            return;
                        }
                        // Track uz OS so we can have some nice metrics of speed per OS, and total pool speed, etc:
                        string sql2 = "Update Work Set OS='" + PurifySQL(sOS, 50)
                            + "', endtime=getdate(), ThreadStart='"
                                + dThreadStart.ToString()
                                + "',SolutionBlockHash='" + PurifySQL2(sBlockHash1)
                                + "',SolutionTxHash='" + PurifySQL2(sTxHash1)
                                + "',solution='"
                                + PurifySQL2(sSol2)
                                + "',Nonce='" + nNonce.ToString()
                                + "',HashCounter='" + dHashCounter.ToString()
                                + "',TimerStart='" + dTimerStart.ToString()
                                + "',TimerEnd='" + dTimerEnd.ToString()
                                + "',ThreadHPS='"
                                + nThreadHPS.ToString()
                                + "',ThreadID='" + dThreadId.ToString()
                                + "',ThreadWork='"
                                + dThreadWork.ToString()
                                + "',BoxHPS='"
                                + nBoxHPS.ToString()
                                + "' where id = '" + GuidOnly(sWorkId2)
                                + "' and ENDTIME IS NULL ";
                        mPD.Exec2(sql2, false);
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
                double dLogLimiter =  LogLimiter();
                if (dLogLimiter > 900)
                {
                    bool bMask = false;
                    if (ex.Message.Contains("Thread was being aborted")) bMask = true;
                    if (!bMask)  Log("ACTION.ASPX: " + ex.Message + "," + s);
                    try
                    {
                        Response.Write("UNKNOWN TCP/IP ERROR<END>");
                    }
                    catch (Exception ex2)
                    {
                         Log("Exception(2): " + ex2.Message);
                    }
                }
            }
        }
    }
}
