using Microsoft.VisualBasic;
using System;
using System.Data;
using System.Data.SqlClient;
using static BiblePayPool2018.Ext;

namespace BiblePayPool2018
{
    public partial class Action : System.Web.UI.Page
    {
      
        public string MinerGuid(string sMiner)
        {
            string sMinerGuid  = clsStaticHelper.AppCache(sMiner, this.Application);
            if (sMinerGuid.Length == 0)
            {
                // If the miner guid is not cached in memory we need to get it from the database:
                string sql = "Select * from miners with (nolock) where username='" + sMiner + "'";
                string sID = clsStaticHelper.mPD.ReadFirstRow(sql, "id");
                clsStaticHelper.AppCache(sMiner, sID, Server, this.Application);
                return sID;
            }
            else
            {
                return sMinerGuid;
            }
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
            // This section was originally created to ratchet up the diff when we had x11, in order to award higher HPS2 pool measurements based on the diff of the problem given to the client
            // But, after f7000 kicked in, we now award equal difficulty shares to every miner, so that we dont have to reverse engineer the diff back from the solution,
            // now we just increase HPS2 an equal amount per share solved.  (This prevents any funny attacks on the client side).
            // The line below sets everyone at step 30.  Step 30 was chosen as one that takes about 3 minutes to solve on an average machine on one thread.
            if (lSharesSolved > 0) lSharesSolved = 30;
            long lTargetSolveCountPerRound = 35;
            double dMasterPrefix = 4444444;
            int dSubLen = dMasterPrefix.ToString().Length;
            double dPrefix = dMasterPrefix;
            for (int y = 0; y <= lSharesSolved; y++)
            {
                double dStep = ((dMasterPrefix / 1) / lTargetSolveCountPerRound) * 1.5;
                if (y > (lTargetSolveCountPerRound * 0.7))
                    dStep = dStep / 4;
                dPrefix = dPrefix - dStep;
            }
            if (dPrefix < 90) dPrefix = 90;
            string sPrefix = "00000000000" + Strings.Trim(Math.Round(dPrefix, 0).ToString());
            sPrefix = sPrefix.Substring(sPrefix.Length - dSubLen, dSubLen);
            // 7 zeroes = F7000 - hard
            // 6 zeroes = F9000 - easier
            bool F9000 = true;
            string sZeroes = F9000 ? "00000" : "0000000";
            string sPrePrefix = (sNetworkID == "test" ? sZeroes : sZeroes);
            string sHashTarget = sPrePrefix + sPrefix + "111100000000000000000000000000000000000000000000000000000000";
            sHashTarget = Strings.Left(sHashTarget, 64);
            return sHashTarget;
        }

        private string GetHashTarget(string sMinerGuid, string sNetworkID)
        {
            string sHashTarget = "";
            double dCt = 0;
            if ((clsStaticHelper.RequestModulusByMinerGuid(this.Server,this.Application,this.Request,sMinerGuid) % 50) == 0)
            {
                dCt = clsStaticHelper.GetSolvedCount(sMinerGuid, sNetworkID);
            }
            double dHPS = 0;
            sHashTarget = GetHashDifficulty2((long)dCt, dHPS, sNetworkID);
            return sHashTarget;
        }

        public string GetReqdHashTarget(string workid)
        {
            string sql = "Select hashtarget from work with (nolock) where id = '" + Strings.Trim(workid) + "'";
            string sTarget = clsStaticHelper.mPD.ReadFirstRow(sql, 0);
            return sTarget;
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


        public struct Trade
        {
            public string Hash;
            public             int Time;
            public             string Action;
            public             string Symbol;
            public double Quantity;
            public double Price;
            public double Total;
            public string Address;
            public string EscrowTXID;
            public double VOUT;
        }
        
        
        string GetXML(object o, string sFieldName)
        {
            string sValue = o.ToString();
            string sXML = "<" + sFieldName + ">" + sValue + "</" + sFieldName + ">";
            return sXML;
        }

        Trade DepersistTrade(string sSolution)
        {
            Trade t = new Trade();
            t.Hash = clsStaticHelper.ExtractXML(sSolution, "hash=", ",").ToString();
            t.Time = (int)ToDouble(clsStaticHelper.ExtractXML(sSolution, "time=", ","));
            t.Action = clsStaticHelper.ExtractXML(sSolution, "action=", ",").ToString();
            t.Symbol = clsStaticHelper.ExtractXML(sSolution, "symbol=", ",").ToString();
            t.Quantity = ToDouble(clsStaticHelper.ExtractXML(sSolution, "quantity=", ","));
            t.Address = clsStaticHelper.ExtractXML(sSolution, "address=", ",").ToString();
            t.Price = ToDouble(clsStaticHelper.ExtractXML(sSolution, "price=", ","));
            t.EscrowTXID = clsStaticHelper.ExtractXML(sSolution, "escrowtxid=", ",").ToString();
            t.VOUT = (int)ToDouble(clsStaticHelper.ExtractXML(sSolution, "vout=", ","));
            t.Total = t.Price * t.Quantity;
            return t;
        }
        
        
        long RawTransactionAge(string sNetworkId, string sMyAddress, string sTxId, double dAmount)
        {
            string sRawTx = clsStaticHelper.GetRawTransaction(sNetworkId, sTxId);
            if (sRawTx.Contains(sMyAddress) && sRawTx.Contains(dAmount.ToString()))
            {
                //TODO: Confirm height is > 5 and TxId was never paid to us before
                return 10;
            }
            if (sRawTx.Contains("?") && sRawTx.Contains(dAmount.ToString())) return 9;
            return 0;
        }

        public void SendWalletOrders(string sNetworkID, string sRecAddr)
        {
            try
            {
                string sql = "Select * from Orders where networkid='" + sNetworkID + "' and WalletOrder=1 and WalletOrderProcessed=0 and TxId is not null";
                DataTable dt10 = clsStaticHelper.mPD.GetDataTable(sql);
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

                        string sql20 = "Select * from users where id ='" + sUserGuid + "'";

                        DataTable dtU = clsStaticHelper.mPD.GetDataTable(sql20);
                        if (dtU.Rows.Count > 0)
                        {
                            string sql21 = "Select * from Products where productid = '" + sProductID + "' and networkid='" + sNetworkID + "'";
                            DataTable dt22 = clsStaticHelper.mPD.GetDataTable(sql21);
                            if (dt22.Rows.Count == 0)
                            {
                                string sql23 = "Update Orders set Status1='UNABLE TO LOCATE PRODUCT',WalletOrderProcessed=1,Updated=getdate() where id='" + id + "'";
                                clsStaticHelper.mPD.Exec(sql23);
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
                            string sToken = clsStaticHelper.AppSetting("MouseToken", "");
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
                            sql = "Update Orders set Title='" + sTitle + "',WalletOrderProcessed=1,MouseId = '" + sMouseId + "',Status1='PLACED' where ID = '" + id + "'";
                            clsStaticHelper.mPD.Exec(sql);
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
                string sql = "Select * from Products where networkid='" + sNetworkID + "' and Productid='" + sProductId + "' and inwallet=1";
                DataTable dt = clsStaticHelper.mPD.GetDataTable(sql);
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
            string sql = "Select * From Links where id = '" + sID + "'";
            DataTable dt = clsStaticHelper.mPD.GetDataTable(sql);
            if (dt.Rows.Count > 0)
            {
                double dBounty = ToDouble(dt.Rows[0]["PaymentPerClick"]);
                double dBudget = ToDouble(dt.Rows[0]["Budget"]);
                double dClicks = ToDouble(dt.Rows[0]["Clicks"]);
                double dSpent = dClicks * dBounty;
                // Ensure this user was not awarded this link click before:
                sql = "Select count(*) ct from transactionLog where transactiontype='GOSPEL_BOUNTY' and userid='" + sClickerGuid + "' and destination='" + sID + "'";
                double AwardCount = clsStaticHelper.mPD.GetScalarDouble(sql, "ct");
                if (AwardCount == 0)
                {
                    // increment clicks
                    sql = "Update Links Set Clicks=isnull(clicks,0)+1 where id = '" + sID + "'";
                    clsStaticHelper.mPD.Exec(sql);
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
                    string sMiner = (Request.Headers["Miner"] ?? "").ToString();
                    string sBBPAddress = (Request.Headers["Miner"] ?? "").ToString();
                    string sAction = (Request.Headers["Action"] ?? "").ToString();
                    string sSolution = (Request.Headers["Solution"] ?? "").ToString();
                    string sNetworkID = (Request.Headers["NetworkID"] ?? "").ToString();
                    string sAgent = (Request.Headers["Agent"] ?? "").ToString();
                    string sIP = (Request.UserHostAddress ?? "").ToString();
                    string sWorkID = (Request.Headers["WorkID"] ?? "").ToString();
                    string sThreadID1 = (Request.Headers["ThreadID"] ?? "").ToString();
                    string sForensic = sMiner + "," + sAction + "," + sSolution + "," + sNetworkID + "," + sAgent + "," + sIP + "," + sWorkID + "," + sThreadID1;
                    string sReqAction = (Request.QueryString["action"] ?? "").ToString();
                    string sReqLink = (Request.QueryString["link"] ?? "").ToString();
                    if (sReqLink != "") sReqAction = "link";
                    string sOS = (Request.Headers["OS"] ?? "").ToString();
                    sAgent = sAgent.Replace(".", "");
                    double dAgent = Convert.ToDouble("0" + sAgent);
                    if (dAgent > 1000 && dAgent < 1035 || (dAgent > 1000 && dAgent < 1069))
                    {
                        string sResponse1 = "<RESPONSE>PLEASE UPGRADE</RESPONSE><ERROR>PLEASE UPGRADE</ERROR><EOF></HTML>";
                        Response.Write(sResponse1);
                        return;
                    }
                    if (sReqAction == "verify_email")
                    {
                        string sID = (Request.QueryString["id"] ?? "").ToString();
                        string sql = "Select username,id,email,password from Users where id = '" + sID.ToString() + "'";
                        string email = clsStaticHelper.mPD.ReadFirstRow(sql, "email");
                        string sUserName = clsStaticHelper.mPD.ReadFirstRow(sql, "username");

                        if (email.Length > 3)
                        {
                            // Verified
                            sql = "Update Users set EmailVerified = 1 where id = '" + sID.ToString() + "'";
                            clsStaticHelper.mPD.Exec(sql);
                            string sBody = "<html>Dear " + sUserName.ToUpper() + ", <br><br>Your E-Mail has been verified.<br><br>  Have a great day! <br><br> May Jesus' Kingdom Extend for Infinity,<br>BiblePay Support<br></html>";
                            Response.Write(sBody);
                            return;
                        }
                    }

                    if (sReqAction == "continue_action")
                    {
                        string sID = (Request.QueryString["id"] ?? "").ToString();
                        if (sID.Length > 1)
                        { 
                            string sql2 = "Update RequestLog set IP2='" + sIP + "',UserName2='" + "' where id = '" + sID + "'";
                            clsStaticHelper.mPD.Exec(sql2);
                            string sql10 = "Select * From RequestLog where id = '" + sID + "' and processed is null";
                            DataTable dtReq = clsStaticHelper.mPD.GetDataTable(sql10);
                            if (dtReq.Rows.Count==0)
                            {
                                Response.Write("Sorry, No Transaction present.");
                                return;
                            }
                            if (dtReq.Rows.Count > 0)
                            {
                                string sUserGuid = dtReq.Rows[0]["userguid"].ToString();
                                sql10 = "Select * from Users where id = '" + sUserGuid + "'";
                                DataTable dtU = clsStaticHelper.mPD.GetDataTable(sql10);
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

                                    if (Amount < 100 || Amount > 40000)
                                    {

                                        Response.Write("Sorry, amount out of range.");
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
                                    double dAddrUseCt = clsStaticHelper.mPD.GetScalarDouble(sql, "a");
                                    bool bIdentityVerified = false;
                                    if (dAddrUseCt > 1 || dUV == 1 || dWithdraws > 3) bIdentityVerified = true;
                                    if (!bIdentityVerified)
                                    {
                                        string sNarr1 = "Sorry, your identity must be verified.  Please send an e-mail to contact@biblepay.org with reference number " + sID + ".";
                                        if (Amount > 5000)
                                        {
                                            Response.Write(sNarr1);
                                            Response.Write("<br><br>Thank you for using BiblePay.<br>");
                                            return;
                                        }
                                        else
                                        {
                                            Response.Write("<br><br>Verifying user activity...<br>Verifying transactions...<br><br>");
                                        }
                                    }
                                    // Reverify balance
                                    bool bTrue = clsStaticHelper.AdjustUserBalance(sNetworkID2,sUserGuid,-1*Amount);
                                    double newBalance = oldBalance - Amount;
                                    string sTXID = "";
                                    try
                                    {
                                        // Audit the sent Tx before sending this withdrawal
                                        clsStaticHelper.AuditWithdrawals(sNetworkID2);
                                        sTXID = clsStaticHelper.Z(sID, sDest, Amount, sNetworkID2);
                                        clsStaticHelper.InsTxLog(sUN, sUG, sNetworkID2,
                                            iHeight, sTXID, Amount, oldBalance, newBalance, sDest, "withdrawal", "");
                                    }
                                    catch(Exception ex)
                                    {
                                        sTXID = "ERR60514";
                                    }
                                    if (sTXID.Length > 5)
                                    {
                                        sql = "Update RequestLog set TXID='" + sTXID + "',processed=1 where id = '" + sID + "'";
                                        clsStaticHelper.mPD.Exec(sql);
                                    }
                                    // Notify of the transaction
                                    string sSite = clsStaticHelper.AppSetting("WebSite", "http://pool.biblepay.org/");
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
                        string sID = (Request.QueryString["id"] ?? "").ToString();
                        string sql = "Select username,id,email,password from Users where id = '" + sID.ToString() + "'";
                        string email = clsStaticHelper.mPD.ReadFirstRow(sql, "email");
                        if (email.Length > 3)
                        {
                            // Change the users password for them and notify them:
                            string sUserName = clsStaticHelper.mPD.ReadFirstRow(sql, "username");
                            string sNewPass = Guid.NewGuid().ToString();
                            sNewPass = sNewPass.Replace("-", "");
                            sNewPass = sNewPass.Substring(0, 7);
                            sql = "Update Users Set Password = '" + modCryptography.Des3EncryptData(sNewPass) + "' where id = '" + sID + "'";
                            clsStaticHelper.mPD.Exec(sql);
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
                        string sql = "Select * from Links where id like '" + sID + "%'";
                        DataTable dt = clsStaticHelper.mPD.GetDataTable(sql);
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
                                        string sql100 = "Select isnull(emailverified,0) EV from USERS where id='" + Sys.UserGuid.ToString() + "'";
                                        double d1 = clsStaticHelper.GetScalarDouble(sql100, "EV");
                                        if (d1 == 1)
                                        {
                                            double dBalance = clsStaticHelper.AwardBounty(sGuid, Payer, -1 * dBounty, "GOSPEL_CAMPAIGN", 0, Guid.NewGuid().ToString(), "main", sID.ToString(), false);
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
                                string sSite = clsStaticHelper.AppSetting("WebSite", "http://pool.biblepay.org/");
                                string sNarr10 = sJava + "<img src='" + sSite + "images/logo.png'><p><p><br><br>Thank you for using BiblePay.  Gospel Bounty Award: " + dBountyPaid.ToString() + " BBP.";
                                Response.Write(sNarr10);
                                return;
                            }
                            else
                            {
                                // User should be redirected to login page
                                string sJava = "<html><head><script>setTimeout(\"location.href = '" + sURL + "';\",2000);</script></head><body>";
                                string sSite = clsStaticHelper.AppSetting("WebSite", "http://pool.biblepay.org/");
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
                        string s1 = clsStaticHelper.AppSetting("PoolReceiveAddress_test", "");
                        clsStaticHelper.AuditWithdrawals("main");
                        string sMinerGuid2 = MinerGuid("desktop");
                        sNetworkID = "main";
                    }
                    
                if (clsStaticHelper.LogLimiter() > 998)   clsStaticHelper.Housecleaning("main", this.Server, this.Application, true);

                long lMaxThreadCount = 40;
                if (Conversion.Val(sThreadID1) > lMaxThreadCount)
                {
                    Response.Write("<RESPONSE><ERROR>MAX THREAD COUNT OF " + Strings.Trim(lMaxThreadCount.ToString()) + " EXCEEDED-PLEASE LOWER THREADCOUNT</ERROR></RESPONSE><END></HTML>");
                    return;
                }
                string sResponse = "";
                if (!clsStaticHelper.ValidateNetworkID(sNetworkID))
                {
                    clsStaticHelper.Log("Invalid network id ");
                    Response.Write("<ERROR>INVALID NETWORK ID</ERROR><END></HTML>");
                    return;
                }

                string sMinerGuid = MinerGuid(sMiner);

                if (sAction !="get_products" && sAction != "buy_product" && sAction != "get_product_escrow_address" && sAction != "order_status" && sAction != "order" && sAction != "trades" && sAction != "cancel" && sAction != "escrow" && sAction != "execution_history" && sAction != "finishtrades" && sAction != "process_escrow")
                {
                    // These two commands use the BBP address instead for the PK
                    if (sMinerGuid.Length == 0)
                    {
                        Response.Write("<ERROR>INVALID MINER GUID " + sMiner + "</ERROR><END></HTML><EOF>" + Constants.vbCrLf);
                        return;
                    }
                }
                string sPoolRecvAddress = clsStaticHelper.AppSetting("PoolReceiveAddress_" + sNetworkID, "");
                if (sAction == "cancel") sAction = "order";

                switch (sAction)
                {
                    case "process_escrow":
                         // update these matches with a guid here 
                         string sql11 = "Select * from Trade where networkid='" + sNetworkID + "' and (match is not null OR MatchSell Is Not Null) and Address='" 
                            + sBBPAddress + "' and EscrowTxId is null";
                        DataTable dt = clsStaticHelper.mPD.GetDataTable(sql11);
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
                        string sql = "Select * from Orders where networkid='" + sNetworkID + "' and Address='" + sBBPAddress + "' ";
                        DataTable dt10 = clsStaticHelper.mPD.GetDataTable(sql);
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
                        // MoveCompletedTrades(sNetworkID);
                        string sql12 = "Select * from TradeComplete where networkid='" + sNetworkID + "' and Address='"
                           + sBBPAddress + "' and executed > getdate()-7 order by executed";
                        DataTable dt0 = clsStaticHelper.mPD.GetDataTable(sql12);
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
                        // SendMutatedTransactions(sNetworkID);
                        Response.Write(sTradeHistory);
                        break;

                    case "escrow":
                        // in this case we update the escrowtxid on the transaction
                        Trade t1 = DepersistTrade(sSolution);
                        string sql99 = "Update Trade set VOUT='" + t1.VOUT.ToString() + "',EscrowTXID='" + t1.EscrowTXID + "' where hash='" + t1.Hash + "'";
                        clsStaticHelper.mPD.Exec(sql99);
                        // SendMutatedTransactions(sNetworkID);
                        break;
                    case "finishtrades":
                        break;
                        
                    case "order":
                        Trade t = DepersistTrade(sSolution);
                        // if action == CANCEL, cancel the orders
                        t.Action = t.Action.ToUpper();
                        if (t.Action == "CANCEL")
                        {
                            string sql4 = "Delete from Trade where quantity='" + t.Quantity.ToString() + "' address='" + t.Address + "' and symbol='" + t.Symbol 
                                + "' and networkid='" + sNetworkID + "' and (match is null and matchell is null) and escrowtxid is null and executed is null \r\n ";
                            clsStaticHelper.mPD.Exec(sql4);
                        }
                        else
                        {
                            // delete any records for this address with same symbol and action
                            string sql4 = "Delete from Trade where address='" + t.Address + "' and networkid = '" + sNetworkID + "' and symbol='" + t.Symbol + "' and act='" 
                                + t.Action + "' and quantity = '" + t.Quantity.ToString() + "' and (match is null and matchsell is null) \r\n";
                            clsStaticHelper.mPD.ExecResilient(sql4);
                            sql4 = "Insert into Trade (id,address,hash,time,added,ip,act,symbol,quantity,price,total,networkid) values (newid(),'"
                                + t.Address + "','" + t.Hash + "','" + t.Time.ToString() + "',getdate(),'','"
                                + t.Action + "',"
                                + "'" + t.Symbol + "','" + t.Quantity.ToString() + "','" + t.Price.ToString() + "','" + t.Total.ToString() + "','" + sNetworkID + "')";
                            clsStaticHelper.mPD.ExecResilient(sql4);
                            sql4 = "exec ordermatch '" + sNetworkID + "'";
                            clsStaticHelper.mPD.ExecResilient(sql4);
                        }
                  
                        string sTradeResponse = "<RESPONSE>OK</RESPONSE></EOF></HTML>";
                        Response.Write(sTradeResponse);
                        break;
                    case "get_products":
                        try
                        {
                            sql = "Select * from Products where networkid='" + sNetworkID + "' and inwallet=1";
                            dt = clsStaticHelper.mPD.GetDataTable(sql);
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
                            string sql80 = "Update Users Set Updated=getdate(),DelName='" + sName + "',Address1='" + sAddress1 + "',Address2='" + sAddress2 + "',City='" + sCity + "',State='"
                                + sState + "',Zip='" + sZip + "',Country='US',Phone='" + sPhone + "' WHERE ID='" + sBBPUserGuid + "'";
                            clsStaticHelper.mPD.Exec(sql80);
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
                                string sNarr = "<RESPONSE><ERR>Payment received for " + dAmt.ToString() + "BBP differs from cost of "    + dPrice.ToString() + "BBP</ERR></RESPONSE></EOF><END></HTML>";
                                Response.Write(sNarr);
                                return;
                            }

                            // Verify escrow amount and confirms before sending the package
                            string sOrderGuid = Guid.NewGuid().ToString();
                            bool banned = false;
                            if (sNetworkID == "test")
                            {
                                string sql199 = "Select count(*) ct from Orders where networkid='" + sNetworkID + "' and WalletOrder=1 and txid is not null and address='" + sBBPAddress + "'";
                                double dBot = clsStaticHelper.mPD.GetScalarDouble(sql199, "ct");
                                if (dBot > 0) banned =true;
                            }
                            if (sDryRun != "DRY" && !banned)
                            {
                                // Ensure this guy hasnt bought any other testnet orders
                                sql = "Insert into Orders (id,added) values ('" + sOrderGuid + "',getdate())";
                                clsStaticHelper.mPD.Exec(sql);
                                sql = "Update Orders Set NetworkID='" + sNetworkID + "',WalletOrderProcessed=0,Address='" + sBBPAddress + "',WalletOrder=1,Userid='" + sBBPUserGuid + "',ProductId='"
                                    + sProdID + "',Title='',Added=getdate(),Status1='Placed',TXID='" + sTXID + "',Amount='" + dAmt.ToString() + "' where id = '" + sOrderGuid + "'";
                                clsStaticHelper.mPD.Exec(sql);
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
                        string sql5 = "Select * from trade where networkid='" + sNetworkID + "' AND EscrowTxId is null and executed is null and match is null";
                        DataTable dt2 = clsStaticHelper.mPD.GetDataTable(sql5);
                        string sTrades2 = "<RESPONSE><TRADES>";
                        for (int i = 0; i < dt2.Rows.Count; i++)
                        {
                            string sRow = GetXML(dt2.Rows[i]["time"], "TIME") + GetXML(dt2.Rows[i]["act"], "ACTION") + GetXML(dt2.Rows[i]["symbol"], "SYMBOL") 
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
                        string sHashTarget = GetHashTarget(sMinerGuid, sNetworkID);
                        string sql100 = "exec InsWork '" + sNetworkID + "','" + sMinerGuid + "','" + sThreadID1 + "','" + sMiner + "','" + sHashTarget + "','" + sWork1 + "','" + sIP + "'";
                        clsStaticHelper.mPD.Exec(sql100);
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
                        string sThreadStart = vSolution[9];
                        string sHashCounter = vSolution[10];
                        string sTimerStart = vSolution[11];
                        string sTimerEnd = vSolution[12];
                        // Calculate Thread HPS and Box HPS (this is for HPS reading only, not for HPS2)
                        double nBoxHPS = 1000.0 * Conversion.Val(sHashCounter) / (Conversion.Val(sTimerEnd) - Conversion.Val(sTimerStart) + 0.01);
                        string sThreadId = vSolution[7];
                        string sThreadWork = vSolution[8];
                        double nThreadHPS = 1000.0 * Conversion.Val(sThreadWork) / (Conversion.Val(sTimerEnd) - Conversion.Val(sThreadStart) + 0.01);
                        string sWorkId2 = vSolution[6];
                        string sBlockHash = vSolution[0];
                        string sBlockTime = vSolution[1];
                        string sPrevBlockTime = vSolution[2];
                        string sPrevHeight = vSolution[3];
                        string sHashSolution = vSolution[4];
                        double nNonce = 0;
                        if (vSolution.Length > 13) nNonce = clsStaticHelper.GetDouble(vSolution[13]);

                        // Track users OS so we can have some nice metrics of speed per OS, and total pool speed, etc:
                        string sql2 = "Update Work Set OS='" + sOS + "',endtime=getdate(),ThreadStart='"
                                + Strings.Trim(sThreadStart) + "',solution='" + sSolution + "',Nonce='" + nNonce.ToString() + "',HashCounter='" + Strings.Trim(sHashCounter)
                                + "',TimerStart='" + Strings.Trim(sTimerStart) + "',TimerEnd='"  + Strings.Trim(sTimerEnd) + "',ThreadHPS='"
                               + Strings.Trim(nThreadHPS.ToString())        + "',ThreadID='" + Strings.Trim(sThreadId) + "',ThreadWork='" + Strings.Trim(sThreadWork)
                                  + "',BoxHPS='"            + Strings.Trim(nBoxHPS.ToString()) + "' where id = '" + sWorkId2 + "' and ENDTIME IS NULL ";
                        // Execute this in a way that overcomes deadlocks:
                        clsStaticHelper.mPD.ExecResilient(sql2);
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
                        if (ex.Message.ToLower().Contains("connection"))
                        {
                            // Could be used to add code to recover from SQL connection failures, but I recommend fixing the root of the problem instead.
                        }
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
