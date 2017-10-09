using Bitnet.Client;
using Microsoft.VisualBasic;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Web;
using ConfigurationSettings = System.Configuration.ConfigurationManager;

namespace BiblePayPool2018
{
    public class clsStaticHelper
    {
        public static object myLock = "A";
        public static long lVersion = 1017;
        public static bool bResult;
        public static USGDFramework.Data mPD = new USGDFramework.Data();
        public static HttpServerUtility mHttpServer;
        public static SqlConnection sqlConnection;
        public static BitnetClient mBitnetNewClient;
        public static double nCurrentTipHeightMain = 0;
        public static double mBitnetUseCount = 0;
        public static object ExtractXML(string sData, string sStartKey, string sEndKey)
        {
            int iPos1 = Strings.InStr(1, sData, sStartKey);
            iPos1 = iPos1 + Strings.Len(sStartKey);
            int iPos2 = Strings.InStr(iPos1, sData, sEndKey);
            if (iPos2 == 0)  return "";
            string sOut = Strings.Mid(sData, iPos1, iPos2 - iPos1);
            return sOut;
        }
        public static string ReadKey(string sKey, HttpApplicationState ha)
        {
            string sOut = null;
            if (ha[sKey] == null)
            {
                return "";
            }
            if (ha is null)
            {
                clsStaticHelper.Log(" ONS .. ");
                return "";
            }
            sOut = (ha[sKey] ?? "").ToString();
            return sOut;
        }


        public static void StoreCookie(string sKey, string sValue)
        {

            
            try
            {
                HttpCookie _pool = new HttpCookie("credentials_" + sKey);
                _pool[sKey] = sValue;
                _pool.Expires = DateTime.Now.AddDays(1);
                if (HttpContext.Current != null)
                {
                    HttpContext.Current.Response.Cookies.Add(_pool);
                }
            }
            catch(Exception)
            {

            }
        }

        public static string GetCookie(string sKey)
        {
            HttpCookie _pool = HttpContext.Current.Request.Cookies["credentials_" + sKey];
            if (_pool != null)
            {
                string sOut = (_pool[sKey] ?? string.Empty).ToString();
                return sOut;
            }
            return "";
        }
        

        public static void UpdateKey(string sKey, string sValue, HttpApplicationState ha)
        {
            ha[sKey] = sValue;
        }

        public static long KeyAge(string sKey, HttpApplicationState ha)
        {
            string sAge = ReadKey(sKey, ha);
            if (string.IsNullOrEmpty(sAge))
            {
                UpdateKey(sKey, Strings.Trim(DateTime.Now.ToString()), ha);
                sAge = ReadKey(sKey, ha);
            }
            long dDiff = 0;
            dDiff = Math.Abs(DateAndTime.DateDiff(DateInterval.Minute, DateTime.Now, Convert.ToDateTime(sAge)));
            return dDiff;
        }

        public clsStaticHelper()
        {
        }

        public static byte[] StrToByteArray(string str)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            return encoding.GetBytes(str);
        }

        public static bool ValidateBiblepayAddress(string sAddress, string sNetworkID)
        {
            try
            {
                InitializeNewBitnet(sNetworkID);
                object oValid = null;
                oValid = mBitnetNewClient.ValidateAddress(sAddress);
                string sValid = null;
                sValid = oValid.ToString();
                if (!sValid.Contains("true"))
                    return false;
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static void InitializeNewBitnet(string sNetworkID)
        {
       
            mBitnetUseCount += 1;
            try
            {
                if (mBitnetNewClient == null || mBitnetUseCount < 5)
                {
                    mBitnetNewClient = new BitnetClient(AppSetting("RPCURL" + sNetworkID, ""));
                    string sPass = AppSetting("RPCPass" + sNetworkID, "");
                    NetworkCredential Cr = new NetworkCredential(AppSetting("RPCUser" + sNetworkID, ""), sPass);
                    mBitnetNewClient.Credentials = Cr;
                }
            }
            catch(Exception)
            {
                mBitnetNewClient = new BitnetClient(AppSetting("RPCURL" + sNetworkID, ""));
                string sPass = AppSetting("RPCPass" + sNetworkID, "");
                NetworkCredential Cr = new NetworkCredential(AppSetting("RPCUser" + sNetworkID, ""), sPass);
                mBitnetNewClient.Credentials = Cr;
            }
        }

        public static double RequestModulusByMinerGuid(HttpServerUtility server, HttpApplicationState ha, HttpRequest hr, string sMinerGuid)
        {
            try
            {
                string sReqMod = AppCache(sMinerGuid + "_requestmodulus", ha);
                double dCount = Conversion.Val("0" + sReqMod) + 1;
                AppCache(sMinerGuid + "_requestmodulus", dCount.ToString(), server, ha);
                return dCount;
            }
            catch (Exception)
            {
                return 0;
            }

        }

        public static double RequestCountByIP(HttpServerUtility server, HttpApplicationState ha, HttpRequest hr, long iIncrementBy)
        {
            try
            {
                string sIP = (hr.UserHostAddress ?? "").ToString();
                string sKey = "rcbip" + sIP;
                string sClean = AppCache(sKey, ha);
                DateTime dtClean = DateTime.Now;
                if (sClean.Length > 0)
                    dtClean = Convert.ToDateTime(sClean);
                string sValue = AppCache(sKey + "_count", ha);
                double dCount = 0;
                if (sValue.Length > 0)
                    dCount = Conversion.Val(sValue);
                double dtSecs = DateAndTime.DateDiff(DateInterval.Second, dtClean, DateTime.Now);
                if (dtSecs > 60)
                {
                    //Clear the value
                    AppCache(sKey, DateTime.Now.ToString(), server, ha);
                    AppCache(sKey + "_count", "0", server, ha);
                    return 0;
                }
                else
                {
                    AppCache(sKey + "_count", Strings.Trim((dCount + iIncrementBy).ToString()), server, ha);

                }
                return dCount;
                //How many hits from ip over last minute
            }
            catch(Exception ex)
            {
                clsStaticHelper.Log("Request count by ip " + ex.Message);
                return 0;
            }
        }

        
        private static bool bSecondarySuffix;
        public static double LastRequestByIP(HttpServerUtility server, HttpApplicationState ha, HttpRequest hr, bool bClear)
        {
            string sIP = hr.UserHostAddress.ToString();
            string ssuffix = (bClear ? "c" : "p");
            bSecondarySuffix = !bSecondarySuffix;
            ssuffix += (bSecondarySuffix ? "1" : "2");
            string sClean = AppCache("lastrequestbyip" + sIP + ssuffix, ha);
            DateTime dtClean = default(DateTime);
            if (Strings.Len(sClean) > 0)
                dtClean = Convert.ToDateTime(sClean);
            double dtSecs = DateAndTime.DateDiff(DateInterval.Second, dtClean, DateTime.Now);
            AppCache("lastrequestbyip" + sIP + ssuffix, DateTime.Now.ToString(), server, ha);
            return dtSecs;
        }

        public static string sHealthMain = "";

        public static string GetHealth(string sNetworkID)
        {
            if (Strings.LCase(sNetworkID) == "main" & !string.IsNullOrEmpty(sHealthMain))
                return sHealthMain;
            string sql = "select value from system with (nolock) where systemKey='" + sNetworkID + "'";
            try
            {
                DataTable dt = default(DataTable);
                dt = mPD.GetDataTable(sql);
                if (dt.Rows.Count > 0)
                {
                    string sOut = dt.Rows[0]["value"].ToString();
                    if (Strings.LCase(sNetworkID) == "main")
                        sHealthMain = sOut;
                    return sOut;
                }
            }
            catch (Exception)
            {
                return String.Empty;
            }
            return String.Empty;
        }


        public static object Housecleaning(string sNetworkID, HttpServerUtility server, HttpApplicationState ha, bool bForce)
        {
            try
            {
                if (string.IsNullOrEmpty(sNetworkID)) sNetworkID = "test";
                //Once every minute or so, perform housecleaning
                string sClean = AppCache("lastcleaning" + sNetworkID, ha);
                DateTime dtClean = default(DateTime);
                if (Strings.Len(sClean) > 0)
                    dtClean = Convert.ToDateTime(sClean);
                double dtSecs = DateAndTime.DateDiff(DateInterval.Second, dtClean, DateTime.Now);
                string sDt8 = AppCache("lastscan" + sNetworkID, ha);
                DateTime dt8 = default(DateTime);
                if (Strings.Len(sDt8) > 0)
                    dt8 = Convert.ToDateTime(sDt8);
                double dt8Secs = DateAndTime.DateDiff(DateInterval.Second, dt8, DateTime.Now);
                string sHealth = GetHealth(sNetworkID);
                if (dt8Secs > 180)
                {
                    AppCache("lastscan" + sNetworkID, DateTime.Now.ToString(), server, ha);
                    lock (myLock)
                    {
                        if (sHealth != "HEALTH_DOWN")
                        {
                            ScanBlocksForPoolBlocksSolved(sNetworkID, bForce);
                        }
                    }
                }

                if (dtSecs > 60 || bForce)
                {
                    lock (myLock)
                    {
                        AppCache("lastcleaning" + sNetworkID, DateTime.Now.ToString(), server, ha);
                        string sql = "exec UpdatePool '' ";
                        mPD.Exec(sql);
                        // Verify all solutions
                        VerifySolutions(sNetworkID);
                        //Clear out old Work records (networkid does not matter as old is old) check to see if any blocks are solved
                        if (clsStaticHelper.LogLimiter() > 970)
                        {
                            RewardUpvotedLetterWriters();
                        }
                        return true;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                clsStaticHelper.Log("HOUSECLEANING ERR: " + ex.Message);
                return false;
            }
        }

        public static int GetTipHeight(string snetworkid)
        {
            try
            {
                InitializeNewBitnet(snetworkid);
                int iTipHeight = mBitnetNewClient.GetBlockCount();
                if (snetworkid=="main" && iTipHeight > 0) nCurrentTipHeightMain = iTipHeight;
                return iTipHeight;
            }
            catch (Exception)
            {
                return -1;
            }
        }
        
        public static string GetBibleHash(string sBlockHash, string sBlockTime, string sPrevBlockTime, string sPrevHeight, string sNetworkID)
        {
            for (int x = 1; x <= 2; x++)
            {
                try
                {
                    object[] oParams = new object[5];
                    oParams[0] = "biblehash";
                    oParams[1] = sBlockHash;
                    oParams[2] = Conversion.Val(sBlockTime).ToString();
                    oParams[3] = Conversion.Val(sPrevBlockTime).ToString();
                    oParams[4] = Conversion.Val(sPrevHeight).ToString();
                    clsStaticHelper.InitializeNewBitnet(sNetworkID);
                    dynamic oOut = clsStaticHelper.mBitnetNewClient.InvokeMethod("run", oParams);
                    string sBibleHash = "";
                    sBibleHash = oOut["result"]["BibleHash"].ToString();

                    return sBibleHash;
                }
                catch (Exception ex)
                {
                    if (clsStaticHelper.LogLimiter() > 900)
                    {
                        clsStaticHelper.Log("getbiblehash forensics " + sNetworkID + ": " + " run biblehash " + Strings.Trim(sBlockHash) + " " + Strings.Trim(sBlockTime) + " " + Strings.Trim(sPrevBlockTime) + " " + Strings.Trim(sPrevHeight) + Constants.vbCrLf + ex.Message.ToString());
                        clsStaticHelper.mBitnetUseCount += 1;
                    }
                }
            }
            return "";
        }

        public static double LogLimiter()
        {
            Random d = new Random();
            int iHow = d.Next(1000);
            return iHow;
        }

       

        public static void GetOSInfo(string sOS, ref string sNarr, ref double dMinerCount, ref double dAvgHPS, ref double dTotal)
        {
            string sql = "Select count(distinct minername) ct from WORK with (nolock) where OS is not null";
            dTotal = mPD.GetScalarDouble(sql, "ct");
            sql = "Select count(distinct minername) ct,os from work with (nolock) where os is not null  AND OS='" + sOS + "' group by os";
            dMinerCount = mPD.GetScalarDouble(sql, "ct");
            sql = "Select avg(boxhps) avgboxhps,os from Work with (nolock) where os is not null AND OS='" + sOS + "' group by OS";
            dAvgHPS = Math.Round(mPD.GetScalarDouble(sql, "avgboxhps"), 2);
            double dPercent = Math.Round(dMinerCount / (dTotal + .01), 2) * 100;
            sNarr = "Miner Count: " + dMinerCount.ToString() + ", Avg HPS: " + dAvgHPS.ToString() + ", Usage: " + dPercent.ToString() + "%";
        }


        public static string GetInfoVersion(string sNetworkID)
        {
            return VersionToString(GetGenericInfo(sNetworkID, "getinfo", "version","result"));
        }

        public static string VersionToString(string sVersion)
        {
            // Version is Formatted with two alpha characters per biblepay version octet
            if (sVersion.Length < 7) return "";
            string sOut = sVersion.Substring(0, 1) + sVersion.Substring(2,1) + sVersion.Substring(4, 1) + sVersion.Substring(6, 1);
            return sOut;
        }

        public static string GetGenericInfo(string sNetworkid, string sCommand, string sKey, string sResultName)
        {
            try
            {
                InitializeNewBitnet(sNetworkid);
                object[] oParams = new object[1];
                oParams[0] = "";
                dynamic oOut = clsStaticHelper.mBitnetNewClient.InvokeMethod(sCommand);
                // Default RPC result key is named "result"
                string sOut = oOut[sResultName][sKey].ToString();
                return sOut;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static string GetGenericInfo2(string sNetworkid, string sCommand1, object[] oParam, string sResultName)
        {
            try
            {
                InitializeNewBitnet(sNetworkid);
                dynamic oOut = clsStaticHelper.mBitnetNewClient.InvokeMethod(sCommand1, oParam);
                string sOut = oOut["result"][sResultName].ToString();
                return sOut;
            }
            catch (Exception)
            {
                return String.Empty;
            }
        }

        public static double GetScalarDouble(string sSql, string vCol)
        {
            DataTable dt1 = new DataTable();
            dt1 = mPD.GetDataTable(sSql);
            try
            {
                if (dt1.Rows.Count > 0)
                {
                    double oDbl = Convert.ToDouble("0" + dt1.Rows[0][vCol].ToString());
                    return oDbl;
                }
            }
            catch (Exception)
            {
            }
            return 0;
        }

        public static string GetBlockInfo(long nHeight, string sFieldName, string sNetworkID)
        {
            try
            {
                object[] oParams = new object[2];
                oParams[0] = "subsidy";
                oParams[1] = Strings.Trim(nHeight.ToString());
                InitializeNewBitnet(sNetworkID);
                dynamic oOut = mBitnetNewClient.InvokeMethod("run", oParams);
                string sOut = "";
                sOut = oOut["result"][sFieldName].ToString();
                return sOut;
            }
            catch (Exception ex)
            {
                clsStaticHelper.Log("GET_BLOCK_INFO: " + ex.Message.ToString());
            }
            return "";
        }


        private double GetHPS(string sMinerGuid, string sNetworkID)
        {
            string sql = "Select isnull(boxhps" + sNetworkID + ",0) As hps from miners where id ='" + Strings.Trim(sMinerGuid) + "'";
            double dct = Conversion.Val("0" + clsStaticHelper.mPD.ReadFirstRow(sql, 0));
            return dct;
        }


        public static double GetSolvedCount(string sMinerGuid, string sNetworkID)
        {
            string sql = "Select count(*) as ct from Work with (nolock) where minerid='" + Strings.Trim(sMinerGuid) + "' and networkid='" + sNetworkID + "'"; // and endtime is not null";
            double dct = Conversion.Val("0" + clsStaticHelper.mPD.ReadFirstRow(sql, 0));
            return dct;
        }
        

        public static bool ValidateNetworkID(string sNetId)
        {
            if (sNetId == "main")
                return true;
            if (sNetId == "test")
                return true;
            if (sNetId == "regtest")
                return true;
            return false;
        }


        private double GetElapsed(string sMinerGuid, string sNetworkID, string sThreadId)
        {
            string sql8 = "select datediff(second,starttime,getdate()) from work with (nolock) where minerid='" + sMinerGuid + "' and networkid='" + sNetworkID + "' and threadid='" + sThreadId + "'";
            string sElapsed = clsStaticHelper.mPD.ReadFirstRow(sql8, 0);
            if (string.IsNullOrEmpty(sElapsed))
                return 9999;
            return Conversion.Val(sElapsed);

        }

        
        public static bool ScanBlocksForPoolBlocksSolved(string sNetworkID, bool bHalfDay)
        {
            //Find highest block solved
            int nTipHeight = GetTipHeight(sNetworkID);
            if (nTipHeight < 1)
            {
                return false;
            }
            if (sNetworkID == "main" & nTipHeight < 1000)
            {
                return false;
            }

            string sPoolRecvAddress = clsStaticHelper.AppSetting("PoolReceiveAddress_" + sNetworkID,"");
            int lLookback = (bHalfDay ? 220 : 11);
            for (int nHeight = (nTipHeight - lLookback); nHeight <= nTipHeight; nHeight++)
            {
                string sql2 = "Select count(*) as Ct from Blocks where networkid='" + sNetworkID + "' and height='" + Strings.Trim(nHeight.ToString()) + "'";
                double oCt = GetScalarDouble(sql2, "ct");
                if (oCt == 0)
                {
                    double cSub = Conversion.Val("0" + GetBlockInfo(nHeight, "subsidy", sNetworkID));
                    //if recipient is pool...
                    string sRecipient = GetBlockInfo(nHeight, "recipient", sNetworkID);
                    if ((nHeight % 10 == 0))
                        cSub = 0;
                    if (sRecipient == sPoolRecvAddress)
                    {
                        string sql = "insert into blocks (id,height,updated,subsidy,networkid) values (newid(),'" + Strings.Trim(nHeight.ToString()) + "',getdate(),'" + cSub.ToString() + "','" + sNetworkID + "')";
                        try
                        {
                            mPD.Exec(sql);
                        }
                        catch (Exception ex)
                        {
                            Log("Already in blocks table" + Strings.Trim(nHeight.ToString()));
                        }
                        AddBlockDistribution(nHeight, cSub, sNetworkID);
                    }
                }
                else
                {
                    // Check for Clawback
                    double cSub = Conversion.Val("0" + GetBlockInfo(nHeight, "subsidy", sNetworkID));
                    //if recipient is pool...
                    string sRecipient = GetBlockInfo(nHeight, "recipient", sNetworkID);
                    if (sRecipient != sPoolRecvAddress && cSub > 0)
                    {
                        // Claw this amount
                        clsStaticHelper.Log(" CLAWBACK FOR HEIGHT " + nHeight.ToString());
                    }
                    
                }
            }

            PayBlockParticipants(sNetworkID);
            return true;
        }


        public static double GetUpvotedLetterCount()
        {
            string sql = "select isnull(count(*),0) as Ct from Letters where added > getdate()-60 and Upvote >= 7";
            double dct = Conversion.Val("0" + clsStaticHelper.mPD.ReadFirstRow(sql, 0));
            return dct;
        }

        public static double GetRequiredLetterCount()
        {
            string sql = "select isnull(count(*),0) as Ct from Orphans";
            double dct = Conversion.Val("0" + clsStaticHelper.mPD.ReadFirstRow(sql, 0));
            return dct;
        }

        public static double GetMyLetterCount(string sUserGuid)
        {
            string sql = "select isnull(count(*),0) as Ct from Letters where added > getdate()-60 and Upvote >= 7 and Userid='" + sUserGuid + "'";
            double dct = Conversion.Val("0" + clsStaticHelper.mPD.ReadFirstRow(sql, 0));
            return dct;
        }
        
        public static double GetUserBalance(string sUserId, string sNetworkID, ref string sUserName)
        {
            try
            {
                string sql = "Select isnull(Balance" + sNetworkID + ",0) as balance,USERNAME from Users where id = '" + sUserId + "'";
                DataTable dtBal = default(DataTable);
                dtBal = mPD.GetDataTable(sql);
                double cBalance = 0;
                
                if (dtBal.Rows.Count > 0)
                {
                    cBalance = Conversion.Val(dtBal.Rows[0]["Balance"]);
                    sUserName = dtBal.Rows[0]["UserName"].ToString();
                }
                return cBalance;
            }
            catch (Exception ex)
            {
                clsStaticHelper.Log("unable to get user balance for user " + sUserId);
                return 0;
            }
        }

        
        public static void AwardBounty(string sLetterID, string sUserId, double dAmount, string sBOUNTYNAME, double nHeight, string sTxId, string sNetworkID, string sNotes)
        {
            try
            {

                string sUserName = "";
                double cBalance = GetUserBalance(sUserId, sNetworkID, ref sUserName);
                double cNewBalance = cBalance + dAmount;
                
                string sql2 = "Insert into transactionlog (id,height,transactionid,username,userid,transactiontype,destination,amount,oldbalance,newbalance,added,updated,rake,networkid,notes)" + " values (newid(),'" 
                    + nHeight.ToString()  + "','" 
                    + Strings.Trim(sTxId) + "','" + sUserName + "','"
                    + sUserId + "','" +sBOUNTYNAME + "','" + sUserId + "','"
                    + Strings.Trim(dAmount.ToString()) + "','"
                    + Strings.Trim(cBalance.ToString()) + "','"
                    + Strings.Trim(cNewBalance.ToString())
                    + "',getdate(),getdate(),0,'" + sNetworkID + "','" + sNotes + "')";
                mPD.Exec(sql2);
                //update balance now if the tx was inserted:
                sql2 = "Update Users set balance" + sNetworkID + " = '" + Strings.Trim(cNewBalance.ToString()) + "' where id = '" + Strings.Trim(sUserId) + "'";
                mPD.Exec(sql2);
                //Mark the record Paid
                sql2 = "Update Letters set PAID=1 where id = '" + sLetterID + "'";
                mPD.Exec(sql2);
                sql2 = "Insert into Letterwritingfees (id,height,added,amount,networkid,quantity) values (newid(),0,getdate(),'" + (-1*dAmount).ToString() + "','" + sNetworkID + "',0)";
                mPD.Exec(sql2);
                //subtract the bounty from the bounty table

            }
            catch (Exception ex)
            {
                clsStaticHelper.Log("Unable to insert new AwardBounty in tx log : " + ex.Message);
            }
            
        }


        public static double GetTotalBounty()
        {
            string sql = "select isnull(sum(amount),0) as amount from LetterWritingFees";
            double d = Conversion.Val(clsStaticHelper.mPD.ReadFirstRow(sql, 0));
            return d;
        }

        
        public static void RewardUpvotedLetterWriters()
        {
            
            //Pay out any funds marked as unpaid and add a transaction to transactionLog
            string sql = "select * from letters where paid <> 1 and upvote >= 7";
            DataTable dt = new DataTable();
            dt = mPD.GetDataTable(sql);
            double dApprovals = dt.Rows.Count;
            double dChildren = GetRequiredLetterCount();
            double dBounty = GetTotalBounty();
            double dIndBounty = dBounty / (dChildren + .01);
            
            for (int y = 0; y <= dt.Rows.Count - 1; y++)
            {
                string sId = dt.Rows[y]["id"].ToString();
                string sUserid = dt.Rows[y]["userid"].ToString();
                if (sId.Length > 1)
                {
                    string sql2 = "Update Letters set PAID=1 where id = '" + sId + "'";
                    mPD.Exec(sql2);
                    if (dIndBounty > 1)
                    {
                        AwardBounty(sId,sUserid, dIndBounty, "LETTER_WRITING", 0, sId.ToString(), "main", sId.ToString());
                    }
                }
            }
        }


        public static void VerifySolutions(string sNetworkID)
        {
            string sql = "SELECT * from WORK where Validated is null and endtime is not null and networkid='" + sNetworkID + "'";
            // If tip is null get it
            DataTable dt = mPD.GetDataTable(sql);
            int nLowTip = 1;
            int nLowTipThreshhold = 4;
            nLowTip = GetTipHeight(sNetworkID);
            int nHighTip = nLowTip + nLowTipThreshhold;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string sSolution = dt.Rows[i]["Solution"].ToString();
                string[] vSolution = sSolution.Split(new string[] { "," }, StringSplitOptions.None);
                if (vSolution.Length > 11)
                {
                    string sBlockHash = vSolution[0];
                    string sBlockTime = vSolution[1];
                    string sPrevBlockTime = vSolution[2];
                    string sPrevHeight = vSolution[3];
                    string sHashSolution = vSolution[4];
                    string sID = dt.Rows[i]["id"].ToString();

                    int nTheirTip = Convert.ToInt32(sPrevHeight);
                    string sError = "";
                    if ((nTheirTip < (nLowTip-nLowTipThreshhold)) || (nTheirTip > nHighTip))
                    {
                        sError = "BLOCK_IS_STALE"; // <RESPONSE>BLOCK_IS_STALE</RESPONSE><ERROR>BLOCK_IS_STALE</ERROR><EOF></HTML>
                    }
                    //Verify solution matches
                    string sOurSolution = clsStaticHelper.GetBibleHash(sBlockHash, sBlockTime, sPrevBlockTime, sPrevHeight, sNetworkID);
                    if (sHashSolution.Length < 10 || sOurSolution.Length < 10)
                    {
                        sError = "MALFORMED_SOLUTION";
                    }

                    string sTargetHash = dt.Rows[i]["hashtarget"].ToString();
                    string sOurPrefix = sOurSolution.Substring(0, 10);
                    string sTargetPrefix = sTargetHash.Substring(0, 10);
                    decimal dOurPrefix = long.Parse(sOurPrefix, System.Globalization.NumberStyles.HexNumber);
                    decimal dTargPrefix = long.Parse(sTargetPrefix, System.Globalization.NumberStyles.HexNumber);
                    if (dOurPrefix > dTargPrefix) sError = "HIGH_HASH";
                    
                    // They made it
                    double nHeight = Conversion.Val(sPrevHeight) + 1;
                    string sForensically = "run biblehash " + Strings.Trim(sBlockHash) + " " + Strings.Trim(sBlockTime) + " " + Strings.Trim(sPrevBlockTime) + " " + Strings.Trim(sPrevHeight);
                    
                    if (sError == "")
                    {
                        string sql2 = "Update Work Set Solution2='" + sForensically + "', Validated=1 WHERE id = '" + sID + "'";
                        clsStaticHelper.mPD.ExecResilient(sql2);

                    }
                    else
                    {
                        string sql2 = "Update Work Set Error='" + sError + "',Validated=1,EndTime=null where id = '" + sID + "'";
                        clsStaticHelper.mPD.ExecResilient(sql2);
                    }
                }
            }
        }



        public static void PayBlockParticipants(string sNetworkID)
        {
            // TODO: Make this a cursor
            //Pay out any funds marked as unpaid and add a transaction to transactionLog
            string sql = "Select id,height,userid,username,subsidy,stats From Block_Distribution where paid is null and networkid = '" + sNetworkID + "'";
            DataTable dt = new DataTable();
            dt = mPD.GetDataTable(sql);
            for (int y = 0; y <= dt.Rows.Count - 1; y++)
            {
                string sBdId = dt.Rows[y]["id"].ToString();
                string sUserName = dt.Rows[y]["username"].ToString();
                double cSubsidy = Convert.ToDouble(dt.Rows[y]["subsidy"].ToString());
                string sUserId = dt.Rows[y]["userid"].ToString();
                string sStats = dt.Rows[y]["stats"].ToString();
                if (sStats.Length > 1000) sStats = sStats.Substring(0, 1000);
                string sLUN = "";
                double cBalance = GetUserBalance(sUserId, sNetworkID, ref sLUN);
                double cNewBalance = cBalance + cSubsidy;
                dynamic sHeight = dt.Rows[y]["height"].ToString();
                //Do this as a transaction, since TransactionLog has a unique constraint on the transactionid:
                string sql2 = "";

                try
                {
                    sql2 = "Insert into transactionlog (id,height,transactionid,username,userid,transactiontype,destination,amount,oldbalance,newbalance,added,updated,rake,networkid,notes)" + " values (newid(),'" + Strings.Trim(sHeight) + "','" + Strings.Trim(sBdId) + "','" + sUserName + "','" 
                        + sUserId + "','MINING_CREDIT','" + sUserId + "','" 
                        + Strings.Trim(cSubsidy.ToString()) + "','" 
                        + Strings.Trim(cBalance.ToString()) + "','" 
                        + Strings.Trim(cNewBalance.ToString()) 
                        + "',getdate(),getdate(),0,'" + sNetworkID + "','" + Strings.Trim(sStats) + "')";
                    mPD.Exec(sql2);
                    // Update balance now if the tx was inserted: (TODO: Add stored proc with transaction to update the user balance)
                    sql2 = "Update Users set balance" + sNetworkID + " = '" + Strings.Trim(cNewBalance.ToString()) + "' where id = '" + Strings.Trim(sUserId) + "'";
                    mPD.Exec(sql2);
                    // Mark the record Paid
                    sql2 = "Update Block_Distribution set Paid = getdate() where id = '" + sBdId + "'";
                    mPD.Exec(sql2);
                }
                catch (Exception ex)
                {
                    clsStaticHelper.Log("Unable to insert new record in tx log : " + ex.Message + ", " + sql2);
                }
            }
        }

        public static void AddBlockDistribution(long nHeight, double cBlockSubsidy, string sNetworkID)
        {
            //Ensure this block distribution does not yet exist
            string sql = "select count(*) as ct from block_distribution where height='" + nHeight.ToString() + "'";
            DataTable dt = new DataTable();
            dt = mPD.GetDataTable(sql);
            if (dt.Rows.Count > 0)
            {
                if (Convert.ToInt32(dt.Rows[0]["ct"]) > 0)
                {
                    clsStaticHelper.Log("Block already exists in block distribution block # " + Strings.Trim(nHeight.ToString()));
                    //Throw New Exception("Block already exists in block distribution")
                    return;
                }
            }

            //First pre ascertain the total participants and total hashing power
            sql = "select count(*) as participants,isnull(sum(isnull(HPS" + sNetworkID + ",0)),0) as hashpower from Users where HPS" + sNetworkID + " > 0";
            DataTable dtHashPower = new DataTable();
            dtHashPower = mPD.GetDataTable(sql);
            double dParticipants = 0;
            double dTotalHPS = 0;
            if (dtHashPower.Rows.Count > 0)
            {
                try
                {
                    dParticipants = Convert.ToDouble(dtHashPower.Rows[0]["participants"]);
                    dTotalHPS = Convert.ToDouble(dtHashPower.Rows[0]["hashpower"]);
                }
                catch (Exception ex)
                {
                }
            }
            if (dParticipants == 0)
            {
                Log("No participants in block #" + Strings.Trim(nHeight.ToString()));
                //   Throw New Exception("No Participants in block # " + Trim(nHeight))
                return;
            }

            // Normal Pool Fees here:
            double dFee = Conversion.Val(AppSetting("fee","0"));
            double dFeeAnonymous = Conversion.Val(AppSetting("fee_anonymous","0"));
            // Letter writing Pool Fees here:

            double dUpvotedCount = clsStaticHelper.GetUpvotedLetterCount();
            double dRequiredCount = clsStaticHelper.GetRequiredLetterCount();
            double dLWF = Conversion.Val(AppSetting("fee_letterwriting", "0"));
            double dLetterFees  = (dUpvotedCount < dRequiredCount) ? dLWF : 0;
            double dTotalLetterFeeCount = 0;
            
            //Ascertain payment per hash
            dynamic dPPH = cBlockSubsidy / (dTotalHPS + 0.01);
            //Loop through current users with HPS > 0 (those that are participating in this block) and log the hps, and the block info
            sql = "Select * from Users where HPS" + sNetworkID + " > 0 order by username";
            dt = mPD.GetDataTable(sql);
            double dTotalLetterWritingFees = 0;
            for (int x = 0; x <= dt.Rows.Count - 1; x++)
            {
                string sUserId = dt.Rows[x]["id"].ToString();
                string sUserName = dt.Rows[x]["username"].ToString();
                double hps = Convert.ToDouble(dt.Rows[x]["HPS" + sNetworkID]);
                double cloak = Conversion.Val("0" + ("" + dt.Rows[x]["Cloak"].ToString()));
                // Find if user has not written any letters
                double dLetterCount = clsStaticHelper.GetMyLetterCount(sUserId);
                double dMyLetterFees = (dLetterCount == 0 ? dLetterFees : 0);
                
                //Get Sub Stats for the user (MinerName and HPS)
                sql = "select avg(work.BoxHPS) HPS, MinerName from Work with (nolock)  inner join Miners on Miners.id = work.minerid " + " inner join Users on Miners.Userid = Users.Id And Users.Id = '" + sUserId + "' " + " where Work.BoxHps > 0 AND Work.Networkid='" + sNetworkID + "' " + " Group by minername order by MinerName";
                DataTable dtStats = new DataTable();
                try
                {
                    dtStats = mPD.GetDataTable(sql);
                    string sStats = "";
                    for (int y = 0; y <= dtStats.Rows.Count - 1; y++)
                    {
                        string sRow = dtStats.Rows[y]["MinerName"].ToString() + ": " 
                            + Strings.Trim(Math.Round(Convert.ToDouble(dtStats.Rows[y]["HPS"]), 0).ToString()) 
                            + " (" + Strings.Trim(Math.Round(hps, 0).ToString()) + ")";
                        sStats += sRow + "<br>";
                        if (sStats.Length > 3500) sStats = sStats.Substring(0, 3500);
                    }
                    double cMinerSubsidy = dPPH * hps;
                    double fee1 = dFee * cMinerSubsidy;
                    double fee2 = dFeeAnonymous * cMinerSubsidy;
                    double fee3 = dMyLetterFees * cMinerSubsidy;
                    double dTotalFees = (cloak == 1 ? (fee2+fee1+fee3) : (fee1+fee3));
                    dTotalLetterFeeCount += 1;
                    dTotalLetterWritingFees += fee3;
                    
                    cMinerSubsidy = cMinerSubsidy - dTotalFees;
                    if (cMinerSubsidy < 0)
                        cMinerSubsidy = 0;

                    sql = "Insert into Block_distribution (id,height,updated,block_subsidy,userid,username,hps,subsidy,paid,networkid,stats,pph) values (newid(),'" 
                        + nHeight.ToString() + "',getdate(),'" 
                        + cBlockSubsidy.ToString() + "','" + sUserId + "','" 
                        + sUserName + "','" 
                        + Math.Round(hps, 2).ToString() 
                        + "','" + cMinerSubsidy.ToString() 
                        + "',null,'" + sNetworkID + "','" + sStats + "','" + dPPH.ToString() + "')";
                    mPD.Exec(sql);
                }
                catch (Exception ex)
                {
                    Log("AddBlockDistribution " + ex.Message);
                }
            }
            // Track total fees charged for letters so we can award accurate letter writing bounties:
            if (dTotalLetterWritingFees > 0)
            {
                sql = "Insert into LetterWritingFees (id,height,added,amount,networkid,quantity) values (newid(),'" + nHeight.ToString()
                    + "',getdate(),'" + dTotalLetterWritingFees.ToString() + "','" + sNetworkID + "','" + dTotalLetterFeeCount.ToString() + "')";
                mPD.Exec(sql);
            }

        }

        
        public static string AppSetting(string sName, string sDefault)
        {
            string sSetting = (ConfigurationSettings.AppSettings[sName] ?? String.Empty).ToString();
            if (sSetting == String.Empty) return sDefault;
            return sSetting;
        }


        public static void Log(string sData)
        {
            try
            {
                string sPath = null;
                string sDocRoot = AppSetting("LogPath", "c:\\inetpub\\wwwroot\\");
                sPath = sDocRoot + "pool2018.dat";
                System.IO.StreamWriter sw = new System.IO.StreamWriter(sPath, true);
                string Timestamp = DateTime.Now.ToString();
                sw.WriteLine(Timestamp + ", " + sData);
                sw.Close();
            }
            catch (Exception ex)
            {
                string sMsg = ex.Message;
            }
        }

        
        public static string NotNull(object o)
        {
            string sOut = null;
            sOut = "" + o.ToString();
            return sOut;
        }


        public static string AppCache(string sKey, HttpApplicationState ha)
        {
            try
            {
                string sOut = null;
                sOut = ReadKey(sKey, ha);
                return sOut;
            }
            catch(Exception ex)
            {
                clsStaticHelper.Log("Err while AppCache accessing " + sKey + " " + ex.Message);
                return "";
            }
        }

        public static void AppCache(string sKey, string sValue, HttpServerUtility server, HttpApplicationState ha)
        {
            UpdateKey(sKey, sValue, ha);
        }
    }

    public class MyWebClient : System.Net.WebClient
    {
        protected override System.Net.WebRequest GetWebRequest(Uri uri)
        {
            System.Net.WebRequest w = base.GetWebRequest(uri);
            w.Timeout = 7000;
            return w;
        }
    }
    
}