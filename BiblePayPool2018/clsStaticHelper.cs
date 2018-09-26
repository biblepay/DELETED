using Bitnet.Client;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Web;

namespace BiblePayPool2018
{
    public class clsStaticHelper
    {
        public static object myLock = "A";
        public static object myLock2 = "A";
        public static long lBackEndVer = 2022;
        public static bool bResult;
        public static USGDFramework.Data mPD = new USGDFramework.Data();
        public static HttpServerUtility mHttpServer;
        public static SqlConnection sqlConnection;
        public static BitnetClient mBitnetMain;
        public static BitnetClient mBitnetTest;
        public static double nCurrentTipHeightMain = 0;
        public static double mBitnetUseCount = 0;
        public static BiblePayPool2018.WebReply mAboutWebReply = null;
        public static string msReadOnly = "background-color: black;";

        public static object ExtractXML(string sData, string sStartKey, string sEndKey)
        {
            int iPos1 = Strings.InStr(1, sData, sStartKey);
            iPos1 = iPos1 + Strings.Len(sStartKey);
            int iPos2 = Strings.InStr(iPos1, sData, sEndKey);
            if (iPos2 == 0)  return "";
            string sOut = Strings.Mid(sData, iPos1, iPos2 - iPos1);
            return sOut;
        }
        public static double GetDouble(object o)
        {
            if (o == null) return 0;
            if (o.ToString() == "") return 0;
            double d = Convert.ToDouble(o.ToString());
            return d;
        }

        public static bool AdjustUserBalance(string networkid, string userguid, double amt)
        {
            string sql = "exec AdjustUserBalance '" + clsStaticHelper.VerifyNetworkID(networkid)
                + "','" + amt.ToString() + "','" + userguid + "'";
            mPD.Exec2(sql);
            return true;
        }

        public static double RetrieveCPIDRAC(string sCPID, string sFileName)
        {
            string sPath = "c:\\inetpub\\ftproot\\biblepay\\" + sFileName;
            if (!System.IO.File.Exists(sPath))
            {
                return -2;
            }

            using (System.IO.StreamReader sr = new System.IO.StreamReader(sPath))
            {
                while (sr.EndOfStream == false)
                {
                    string sTemp = sr.ReadLine();
                    string[] vData = sTemp.Split(new string[] { "," }, StringSplitOptions.None);
                    if (vData.Length > 3)
                    {
                        string sDataCPID = vData[0];
                        double dRac = GetDouble(vData[2]);
                        double dCreated = GetDouble(vData[4]);
                        string sName = vData[5];
                        if (sCPID.ToLower()==sDataCPID.ToLower())
                        {
                            return dRac;
                        }
                    }

                }
            }
            return -1;
        }

        public static bool InsTxLog(string susername, string suserguid, 
            string networkid, double iHeight, string sTXID, double amt, double oldBalance, double newBalance, string sDestination, string sTransactionType, string sNotes)
        {
            string sql = "exec InsTxLog '" + iHeight.ToString() + "','" + clsStaticHelper.PurifySQL(sTXID,125)
                + "','" + clsStaticHelper.PurifySQL(susername,100)
                + "','" + clsStaticHelper.PurifySQL(suserguid,100)
                + "','" + clsStaticHelper.PurifySQL(sTransactionType,100)
                + "','" + clsStaticHelper.PurifySQL(sDestination,125)
                + "','" + amt.ToString()
                + "','" + oldBalance.ToString() 
                + "','" + newBalance.ToString() + "','"
                + clsStaticHelper.VerifyNetworkID(networkid)
                + "','" + clsStaticHelper.PurifySQLLong(sNotes,4000) + "'";
            clsStaticHelper.mPD.Exec2(sql);
            return true;
        }

        public static Zinc.BiblePayMouse.MouseOutput GetCachedCryptoPrice(string sSymbol)
        {
            string q = "Select * from CryptoPrice where Symbol = '" + clsStaticHelper.PurifySQL(sSymbol,25)
                + "' and Added > getdate()-(1/24.01) ";
            DataTable dt = mPD.GetDataTable2(q,false);
            if (dt.Rows.Count > 0)
            {
                double dBTCPrice = Convert.ToDouble(dt.Rows[0]["BTCPrice"].ToString());
                Zinc.BiblePayMouse.MouseOutput m1 = new Zinc.BiblePayMouse.MouseOutput();
                m1.BTCPrice = dBTCPrice;
                m1.BBPPrice = Convert.ToDouble(dt.Rows[0]["BBPPrice"].ToString());
                return m1;
            }
            string sToken = USGDFramework.clsStaticHelper.GetConfig("MouseToken_E");
            Zinc.BiblePayMouse bpm = new Zinc.BiblePayMouse(sToken);
            Zinc.BiblePayMouse.MouseOutput mp = bpm.GetCryptoPrice("bbp");
            // Delete the old record, insert the new record
            if (mp.BTCPrice > 0 && mp.BBPPrice > 0)
            {
                string sql = "Delete from CryptoPrice where Symbol='" + clsStaticHelper.PurifySQL(sSymbol,25)
                    + "'";
                sql += "\r\nInsert Into CryptoPrice (id,Symbol,BTCPrice,BBPPrice,Added) values (newid(),'" + clsStaticHelper.PurifySQL(sSymbol,25)
                    + "','" 
                    + mp.BTCPrice.ToString() + "','" + mp.BBPPrice.ToString() + "',getdate())";
                mPD.Exec2(sql);
            }
            else
            {
                string q1 = "Select * from CryptoPrice where Symbol = '" + clsStaticHelper.PurifySQL(sSymbol,25)
                    + "' ";
                DataTable dt1 = mPD.GetDataTable2(q1);
                if (dt1.Rows.Count > 0)
                {
                    double dBTCPrice = Convert.ToDouble(dt1.Rows[0]["BTCPrice"].ToString());
                    Zinc.BiblePayMouse.MouseOutput m1 = new Zinc.BiblePayMouse.MouseOutput();
                    m1.BTCPrice = dBTCPrice;
                    m1.BBPPrice = Convert.ToDouble(dt1.Rows[0]["BBPPrice"].ToString());

                    return m1;
                }

            }
            return mp;
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
                _pool.Expires = DateTime.Now.AddDays(7);
                if (HttpContext.Current != null)
                {
                    HttpContext.Current.Response.Cookies.Add(_pool);
                }
                HttpContext.Current.Response.Cookies["credentials_" + sKey].Value = sValue;
            }
            catch(Exception ex)
            {
                string sError = ex.Message;

                
            }
        }
        


        public static string GetNextPoolAddress(string sNetworkID, HttpServerUtility server, HttpApplicationState ha)
        {
            try
            {
                double d = LogLimiter();
                List<string> pa = GetPoolAddress(sNetworkID, server, ha);
                double iFactor = pa.Count / 1000.001; //addresscount divided by loglimit length
                int iPtr = (int)(Math.Round(iFactor * d, 0) - 1);
                iPtr = 0;
                string sAddress = pa[iPtr];
                return sAddress;
            }
            catch(Exception ex)
            {
                List<string> pa1 = MemorizePoolAddresses(sNetworkID, server, ha);
                return pa1[0];
            }
        }

        public static List<string> GetPoolAddress(string sNetworkID, HttpServerUtility server, HttpApplicationState ha)
        {
            object a = ha["pooladdresses_" + sNetworkID];
            if (a==null)
            {
                MemorizePoolAddresses(sNetworkID, server, ha);
            }
            List<string> pa = (List<string>)a;
            return pa;
        }

        private static List<string> MemorizePoolAddresses(string sNetworkID, HttpServerUtility server, HttpApplicationState ha)
        {
            string sql = "Select * From PoolAddresses where network='" + sNetworkID + "'";
            DataTable dt = mPD.GetDataTable2(sql);
            List<string> a = new List<string>();

            for (int i=0; i < dt.Rows.Count; i++)
            {
                a.Add(dt.Rows[i]["Address"].ToString());
            }
            // Cache it
            ha["pooladdresses_" + sNetworkID] = a;
            return a;
        }

        public static string GetCookie(string sKey)
        {
            HttpCookie _pool = HttpContext.Current.Request.Cookies["credentials_" + sKey];
            if (_pool != null)
            {
                string sOut = (_pool.Value ?? string.Empty).ToString();
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

        public static void InitRPC(string sNetworkID)
        {
            mBitnetUseCount += 1;
            BitnetClient oBit;
            if (sNetworkID.ToLower() == "main")
            {
                oBit =  mBitnetMain;
            }
            else
            {
                oBit = mBitnetTest;
            }

            try
            {
                if (oBit == null || mBitnetUseCount < 3 || sNetworkID=="test")
                {
                    oBit = new BitnetClient(USGDFramework.clsStaticHelper.GetConfig("RPCURL" + sNetworkID));
                    string sPass = USGDFramework.clsStaticHelper.GetConfig("RPCPass" + sNetworkID + "_E");
                    NetworkCredential Cr = new NetworkCredential(USGDFramework.clsStaticHelper.GetConfig("RPCUser" + sNetworkID), sPass);
                    oBit.Credentials = Cr;
                    if (sNetworkID.ToLower()=="main")
                    {
                        mBitnetMain = oBit;
                    }
                    else
                    {
                        mBitnetTest = oBit;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public static BitnetClient GetBitNet(string sNetworkID)
        {

            InitRPC(sNetworkID);
            if (sNetworkID.ToLower()=="main")
            {
                return mBitnetMain;
            }
            else
            {
                return mBitnetTest;
            }
        }

        public static bool ValidateBiblepayAddress(string sAddress, string sNetworkID)
        {
            try
            {
                object oValid = null;
                BitnetClient oBit = clsStaticHelper.GetBitNet(sNetworkID);
                oValid = oBit.ValidateAddress(sAddress);
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
            string sql = "select value from system with (nolock) where systemKey='" + clsStaticHelper.VerifyNetworkID(sNetworkID)
                + "'";
            try
            {
                DataTable dt = default(DataTable);
                dt = mPD.GetDataTable2(sql);
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

        public static double GetSystemDouble(string sKey)
        {
            string sql = "Update System Set Value=Value+1 where systemKey='" + clsStaticHelper.PurifySQL(sKey,25) + "'";
            mPD.Exec2(sql);
            sql = "Select Value from System where systemKey='" + sKey + "'";
            double d1 = GetScalarDouble2(sql, "Value");
            return d1;
        }

     
        public static int GetTipHeight(string snetworkid)
        {
            try
            {
                BitnetClient oBit = clsStaticHelper.GetBitNet(snetworkid);
                int iTipHeight = oBit.GetBlockCount();
                if (snetworkid=="main" && iTipHeight > 0) nCurrentTipHeightMain = iTipHeight;
                return iTipHeight;
            }
            catch (Exception)
            {
                return -1;
            }
        }
        
        public static double GetNonceInfo(string sNetworkID)
        {
                try
                {
                    object[] oParams = new object[1];
                    oParams[0] = "pinfo";
                    BitnetClient oBit = GetBitNet(sNetworkID);
                    dynamic oOut = oBit.InvokeMethod("exec", oParams);
                    double nNonceInfo = GetDouble(oOut["result"]["pinfo"]);
                    return nNonceInfo;
                }
                catch (Exception ex)
                {
                    clsStaticHelper.Log("GetNonceInfo: " + ex.Message);
                    return 0;
                }
        }


        public static string GetBibleHash(string sBlockHash, string sBlockTime, string sPrevBlockTime, string sPrevHeight, string sNetworkID, string sNonce)
        {
            for (int x = 1; x <= 2; x++)
            {
                try
                {
                    object[] oParams = new object[6];
                    oParams[0] = "biblehash";
                    oParams[1] = sBlockHash;
                    oParams[2] = Conversion.Val(sBlockTime).ToString();
                    oParams[3] = Conversion.Val(sPrevBlockTime).ToString();
                    oParams[4] = Conversion.Val(sPrevHeight).ToString();
                    oParams[5] = Conversion.Val(sNonce).ToString();
                    BitnetClient oBit = GetBitNet(sNetworkID);
                    dynamic oOut = oBit.InvokeMethod("exec", oParams);
                    string sBibleHash = "";
                    sBibleHash = oOut["result"]["BibleHash"].ToString();
                    return sBibleHash;
                }
                catch (Exception ex)
                {
                    if (clsStaticHelper.LogLimiter() > 900)
                    {
                        clsStaticHelper.Log("getbiblehash forensics " + sNetworkID + ": " + " run biblehash " + Strings.Trim(sBlockHash) + " " + Strings.Trim(sBlockTime) + " " + Strings.Trim(sPrevBlockTime) + " " + Strings.Trim(sPrevHeight) + " " + Strings.Trim(sNonce) + Constants.vbCrLf + ex.Message.ToString());
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

        public static double AwardBounty(string sLetterID, string sUserId, double dAmount, string sBOUNTYNAME, double nHeight, string sTxId, string sNetworkID, 
            string sNotes, bool bLetter)
        {
            try
            {
                string sUserName = "";
                double cBalance = GetUserBalance(sUserId, sNetworkID, ref sUserName);
                double cNewBalance = cBalance + dAmount;
                string sql2 = "Insert into transactionlog (id,height,transactionid,username,userid,transactiontype,destination,amount,oldbalance,newbalance,added,updated,rake,networkid,notes)" + " values (newid(),'"
                    + nHeight.ToString() + "','"
                    + clsStaticHelper.PurifySQL(sTxId,100) 
                    + "','" + clsStaticHelper.PurifySQL(sUserName,100)
                    + "','"
                    + clsStaticHelper.PurifySQL(sUserId,100)
                    + "','" + clsStaticHelper.PurifySQL(sBOUNTYNAME,100)
                    + "','" + clsStaticHelper.PurifySQL(sLetterID,100)
                    + "','"
                    + dAmount.ToString() + "','"
                    + cBalance.ToString()
                    + "','"
                    + cNewBalance.ToString()
                    + "',getdate(),getdate(),0,'" + clsStaticHelper.VerifyNetworkID(sNetworkID)
                    + "','" + clsStaticHelper.PurifySQLLong(sNotes,4000)
                    + "')";
                mPD.Exec2(sql2);
                //update balance now if the tx was inserted:
                sql2 = "Update Users set balance" + clsStaticHelper.VerifyNetworkID(sNetworkID)
                    + " = '" + cNewBalance.ToString()
                    + "' where id = '" + clsStaticHelper.GuidOnly(sUserId)
                    + "'";
                mPD.Exec2(sql2);
                if (bLetter)
                {
                    //Mark the record Paid
                    sql2 = "Update Letters set PAID=1 where id = '" + clsStaticHelper.GuidOnly(sLetterID) + "'";
                    mPD.Exec2(sql2);
                    sql2 = "Insert into Letterwritingfees (id,height,added,amount,networkid,quantity) values (newid(),0,getdate(),'"
                        + (-1 * dAmount).ToString() + "','" + clsStaticHelper.VerifyNetworkID( sNetworkID)
                        + "',0)";
                    mPD.Exec2(sql2);
                    //subtract the bounty from the bounty table
                }
                return cNewBalance;

            }
            catch (Exception ex)
            {
                clsStaticHelper.Log("Unable to insert new AwardBounty in tx log : " + ex.Message);
                return 0;
            }
            return 0;
        }

        public static void GetOSInfo(string sOS, ref string sNarr, ref double dMinerCount, ref double dAvgHPS, ref double dTotal)
        {
            sOS = clsStaticHelper.PurifySQL(sOS,50);
            string sql = "Select count(distinct minername) ct from WORK with (nolock) where OS is not null";
            dTotal = mPD.GetScalarDouble2(sql, "ct");
            sql = "Select count(distinct minername) ct,os from work with (nolock) where os is not null  AND OS='" + sOS + "' group by os";
            dMinerCount = mPD.GetScalarDouble2(sql, "ct");
            sql = "Select avg(boxhps) avgboxhps,os from Work with (nolock) where os is not null AND OS='" + sOS + "' group by OS";
            dAvgHPS = Math.Round(mPD.GetScalarDouble2(sql, "avgboxhps"), 2);
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

        public static string GetShowBlock(string sNetworkid, string sCommand, int iBlockNumber, string sJSONFieldName)
        {
            try
            {
                BitnetClient oBit = GetBitNet(sNetworkid);
                object[] oParams = new object[1];
                oParams[0] = iBlockNumber.ToString();
                dynamic oOut = oBit.InvokeMethod(sCommand, oParams);
                // Default RPC result key is named "result"
                string sOut = oOut["result"][sJSONFieldName].ToString();
                return sOut;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static string GetBlockTx(string sNetworkid, string sCommand, int iBlockNumber, int iPositionId)
        {
            try
            {
                BitnetClient oBit = GetBitNet(sNetworkid);
                object[] oParams = new object[1];
                oParams[0] = iBlockNumber.ToString();
                dynamic oOut = oBit.InvokeMethod(sCommand,oParams);
                string sOut = oOut["result"]["tx"][iPositionId].ToString();
                return sOut;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public struct Payment
        {
            public string Address;
            public double Amount;
            public string UserId;
            public string UserName;
            public double OldBalance;
            public double NewBalance;
        }

        public static string SendMany(Payment[] p, string sNetworkID, string sFromAccount, string sComment)
        {
            string sPack = "";
            for (int i=0;i < p.Length; i++)
            {
                string sAmount = string.Format("{0:#.00}", p[i].Amount);
                string sRowOld = "\"" + p[i].Address + "\"" + ":" + sAmount;
                string sRow = "<RECIPIENT>" + p[i].Address + "</RECIPIENT><AMOUNT>" + sAmount + "</AMOUNT><ROW>";
                sPack += sRow;
            }

            string sXML = "<RECIPIENTS>" + sPack + "</RECIPIENTS>";
            clsStaticHelper.GetWLA(sNetworkID, 30);

            try
            {
                BitnetClient bc = GetBitNet(sNetworkID);
                object[] oParams = new object[4];
                oParams[0] = "sendmanyxml";
                oParams[1] = sFromAccount;
                oParams[2] = sXML;
                oParams[3] = sComment;
                dynamic oOut = bc.InvokeMethod("exec", oParams);
                string sTX = oOut["result"]["txid"].ToString();
                return sTX;
            }
            catch (Exception)
            {
                    return "";
            }
        }
        
        public static string GetRawTransaction(string sNetworkid, string sTxid)
        {
            try
            {
                BitnetClient b = GetBitNet(sNetworkid);
                object[] oParams = new object[2];
                oParams[0] = sTxid;
                oParams[1] = 1;
                dynamic oOut = b.InvokeMethod("getrawtransaction", oParams);
                // Loop Through the Vouts and get the recip ids and the amounts
                string sOut = "";
                for (int y = 0; y < 99; y++)
                {
                    string sPtr = "";
                    try
                    {
                         sPtr = (oOut["result"]["vout"][y] ?? "").ToString();
                    }
                    catch(Exception ey)
                    {

                    }
                    
                    if (sPtr != "")
                    {
                        string sAmount = oOut["result"]["vout"][y]["value"].ToString();
                        string sAddress = "";
                        if (oOut["result"]["vout"][y]["scriptPubKey"]["addresses"] != null)
                        {
                            sAddress = oOut["result"]["vout"][y]["scriptPubKey"]["addresses"][0].ToString();
                        }
                        else { sAddress = "?"; } //Happens when pool pays itself
                        sOut += sAmount + "," + sAddress + "|";
                    }
                    else
                    {
                        break;
                    }
                }
                return sOut;
            }
            catch (Exception ex)
            {
                clsStaticHelper.Log(ex.Message);
                return "";
            }
        }

        public static string GetMiningInfo(string sNetworkID, string sKey)
        {
            try
            {
                BitnetClient bc = GetBitNet(sNetworkID);
                object[] oParams = new object[1];
                oParams[0] = "";
                dynamic oOut = bc.InvokeMethod("getmininginfo");
                string sOut = oOut["result"][sKey].ToString();
                return sOut;
            }
            catch (Exception ex)
            {
                string test = ex.Message;
            }
            return "";
        }

        public static string GetGenericInfo0(string sNetworkid, string sCommand, string sKey, string sResultName)
        {
            try
            {
                BitnetClient bc = GetBitNet(sNetworkid);
               dynamic oOut = bc.InvokeMethod(sCommand);
                // Default RPC result key is named "result"
                string sOut = oOut[sResultName][sKey].ToString();
                return sOut;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static double GetMasternodeCount(string sNetworkid)
        {
            try
            {
                BitnetClient b = GetBitNet(sNetworkid);
                object[] oParams = new object[1];
                oParams[0] = "list";
                dynamic oOut = b.InvokeMethod("masternode", oParams);
                string sResult = oOut["result"].ToString();
                string[] vData = sResult.Split(new string[] { "ENABLED" }, StringSplitOptions.None);
                return (double)vData.Length;

            }
            catch (Exception ex)
            {
                return 1;
            }
        }

        public static string GetGenericInfo(string sNetworkid, string sCommand, string sKey, string sResultName)
        {
            try
            {
                BitnetClient bc = GetBitNet(sNetworkid);
                object[] oParams = new object[1];
                oParams[0] = "";
                dynamic oOut = bc.InvokeMethod(sCommand);
                string sOut = oOut[sResultName][sKey].ToString();
                return sOut;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static bool InList(string sIgnoreList, string sData)
        {
            string[] vData = sIgnoreList.Split(new string[] { "," }, StringSplitOptions.None);
            sData = sData.ToLower();
            for (int i = 0; i < vData.Length; i++)
            {
                string sIgnoreData = vData[i].ToLower();
                if (sData == sIgnoreData && sData != "")
                {
                    return true;
                }
            }
            return false;
        }
        public static string DataDump(string sSQL, string sIgnoreFields)
        {
            DataTable dt = clsStaticHelper.mPD.GetDataTable2(sSQL);
            string sOut = "";
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string sValue = "";
                string sHeading = "";
                
                for (int y = 0; y < dt.Columns.Count; y++)
                {

                    bool bAllowed = InList(sIgnoreFields, dt.Columns[y].ColumnName);
                    if (!bAllowed)
                    {
                        sHeading += dt.Columns[y].ColumnName + "|";
                        sValue += dt.Rows[i][y].ToString() + "|";
                    }
                }
                if (i==0)
                {
                    sOut += sHeading + "<ROW>\r\n";

                }
                else
                {
                    sOut += sValue + "<ROW>\r\n";
                }
            }
            return sOut;
        }

        public static string GetGenericInfo2(string sNetworkid, string sCommand1, object[] oParam, string sResultName)
        {
            try
            {
                BitnetClient bc = GetBitNet(sNetworkid);
                dynamic oOut = bc.InvokeMethod(sCommand1, oParam);
                string sOut = oOut["result"][sResultName].ToString();
                return sOut;
            }
            catch (Exception)
            {
                return String.Empty;
            }
        }
        public static dynamic GetGenericObject(string sNetworkId, string sCommand1, object[] oParam)
        {
            try
            {
                BitnetClient bc = GetBitNet(sNetworkId);
                dynamic oOut = bc.InvokeMethod(sCommand1, oParam);
                return oOut;
            }
            catch (Exception ex)
            {
                return String.Empty;
            }
        }
        public static string GetGenericInfo3(string sNetworkid, string sCommand1, object[] oParam)
        {
            try
            {
                BitnetClient bc = GetBitNet(sNetworkid);
                dynamic oOut = bc.InvokeMethod(sCommand1, oParam);
                string sOut = oOut["result"].ToString();
                return sOut;
            }
            catch (Exception ex)
            {
                return String.Empty;
            }
        }
        public struct VoteType
        {
            public double AbsoluteYesCount;
            public double YesCount;
            public double NoCount;
            public double AbstainCount;
            public bool Success;
        }
        public static VoteType GetGenericInfo4(string sNetworkid, string sCommand1, object[] oParam)
        {
            VoteType v = new VoteType();
            v.Success = false;
            v.AbstainCount = -1;
            try
            {
                BitnetClient bc = GetBitNet(sNetworkid);
                dynamic oOut = bc.InvokeMethod(sCommand1, oParam);
                v.AbsoluteYesCount = Convert.ToDouble(oOut["result"]["FundingResult"]["AbsoluteYesCount"].ToString());
                v.YesCount = Convert.ToDouble(oOut["result"]["FundingResult"]["YesCount"].ToString());
                v.NoCount = Convert.ToDouble(oOut["result"]["FundingResult"]["NoCount"].ToString());
                v.AbstainCount = Convert.ToDouble(oOut["result"]["FundingResult"]["AbstainCount"].ToString());
                v.Success = true;
                return v;
            }
            catch (Exception ex)
            {
                return v;
            }
        }

        public static double GetScalarDouble2(string sSql, string vCol)
        {
            DataTable dt1 = new DataTable();
            dt1 = mPD.GetDataTable2(sSql);
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
                BitnetClient bc = GetBitNet(sNetworkID);
                dynamic oOut = bc.InvokeMethod("exec", oParams);
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
            string sql = "Select isnull(boxhps" + sNetworkID + ",0) As hps from miners where id ='" + clsStaticHelper.GuidOnly(sMinerGuid) + "'";
            double dct = clsStaticHelper.GetDouble(clsStaticHelper.mPD.ReadFirstRow2(sql, 0));
            return dct;
        }

        public static double GetSolvedCount(string sMinerGuid, string sNetworkID)
        {
            string sql = "Select count(*) as ct from Work with (nolock) where minerid='" 
                + clsStaticHelper.GuidOnly(sMinerGuid) 
                + "' and networkid='" + sNetworkID + "' and endtime is not null"; 
            double dct = Conversion.Val("0" + clsStaticHelper.mPD.ReadFirstRow2(sql, 0));
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

        public static string Left(string data, int left)
        {
            if (data == null) return "";
            if (data.Length >= left)
            {
                return data.Substring(0, left);
            }
            return data;
        }
        
        public static void GetOrderStatusUpdates()
        {
            // for any order that is ours, that is not complete
            string sql = "Select * from Orders where Status3 is null";
            DataTable dt = clsStaticHelper.mPD.GetDataTable2(sql);
            string sToken = USGDFramework.clsStaticHelper.GetConfig("MouseToken_E");
            Zinc.BiblePayMouse b = new Zinc.BiblePayMouse(sToken);
            for (int i = 0; i <= dt.Rows.Count - 1; i++)
            {
                string sOrderGuid = dt.Rows[i]["id"].ToString();
                string sMouseId = dt.Rows[i]["mouseid"].ToString();
                Zinc.BiblePayMouse.MouseOutput mo = b.GetOrderStatus(sMouseId);
                // Ascertain the status - ordered, processed, tracking provided
                if (mo.tracking != null)
                {
                    if (mo.tracking.Length > 1)
                    {
                        string sql3 = "Update orders set Status3='" + clsStaticHelper.PurifySQL(mo.tracking,25)
                            + "' where id = '" + clsStaticHelper.GuidOnly(sOrderGuid)
                            + "'";
                        clsStaticHelper.mPD.Exec2(sql3);
                    }
                }
                if (mo.code.Length > 1)
                {
                    string sql2 = "Update orders set Status1 = '" + clsStaticHelper.PurifySQL(Left(mo.code, 20),35) 
                        + "' where id = '" + clsStaticHelper.GuidOnly(sOrderGuid) + "'";
                    clsStaticHelper.mPD.Exec2(sql2);
                }
                if (mo.message != null)
                {
                    if (mo.message.Length > 1)
                    {
                        string sStatus = MsgToStatus(mo.message);
                        string sql2 = "Update orders set Updated=getdate(),Status1='PLACED',Status2 = '" + clsStaticHelper.PurifySQL(sStatus,25)
                            + "' where id = '" + clsStaticHelper.GuidOnly(sOrderGuid) + "'";
                        clsStaticHelper.mPD.Exec2(sql2);
                    }
                }
            }
        }

        public static string MsgToStatus(string data)
        {
            string sOut = "";
            if (data.Contains("Request was placed onto the queue and will launch when the retailer account is no longer busy with other requests."))
            {
                sOut = "QUEUED";
            }
            if (data.Contains("Initiated the request on the retailer."))
            {
                sOut = "VERIFYING INVENTORY";
            }
            if (data.Contains("The request completed and returned a response."))
            {

                sOut = "FILLING ORDER";
            }
            return sOut;
        }

        public static double GetUpvotedLetterCount()
        {
            string sql = "select isnull(count(*),0) as Ct from Letters (nolock) where added > getdate()-60 and Upvote >= 7";
            double dct = Conversion.Val("0" + clsStaticHelper.mPD.ReadFirstRow2(sql, 0));
            return dct;
        }

        public static double GetRequiredLetterCount()
        {
            string sql = "select isnull(count(*),0) as Ct from Orphans (nolock) ";
            double dct = Conversion.Val("0" + clsStaticHelper.mPD.ReadFirstRow2(sql, 0));
            return dct;
        }

        public static double GetMyLetterCount(string sUserGuid)
        {
            string sql = "select isnull(count(*),0) as Ct from Letters (nolock) where added > getdate()-60 and Upvote >= 7 and Userid='" + clsStaticHelper.GuidOnly(sUserGuid) + "'";
            double dct = Conversion.Val("0" + clsStaticHelper.mPD.ReadFirstRow2(sql, 0));
            return dct;
        }
        
        public static double GetUserBalance(string sUserId, string sNetworkID, ref string sUserName)
        {
            try
            {
                if (sUserId == "") return 0;
                string sql = "Select isnull(Balance" + sNetworkID + ",0) as balance,USERNAME from Users where id = '" + clsStaticHelper.GuidOnly(sUserId)
                    + "'";
                DataTable dtBal = default(DataTable);
                dtBal = mPD.GetDataTable2(sql);
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

        public static bool GetUserBalances(string sNetworkID, string userguid, ref double dBalance, ref double dImmature)
        {
            if (userguid == "") return false;

            string sql = "Select isnull(Balance" + clsStaticHelper.VerifyNetworkID(sNetworkID)
                + ",0) as bal1 From Users with (nolock) where id='" + clsStaticHelper.GuidOnly(userguid)
                + "' and deleted=0";
            dBalance = mPD.GetScalarDouble2(sql, "bal1");
            sql = "Select isnull(sum(amount),0) As Immature from transactionlog where updated > getdate()-1 And userid='" + clsStaticHelper.GuidOnly(userguid)
                + "'"
                    + " And networkid = '" + clsStaticHelper.VerifyNetworkID(sNetworkID)
                    + "' and transactiontype='MINING_CREDIT'";
            dImmature = mPD.GetScalarDouble2(sql, "Immature");
            return true;
        }

        
        public static double GetTotalBounty()
        {
            string sql = "select isnull(sum(amount),0) as amount from LetterWritingFees";
            double d = Conversion.Val(clsStaticHelper.mPD.ReadFirstRow2(sql, 0));
            return d;
        }

        public static double GetTotalLetterBountiesPaid(int iSuffixType)
        {
            string sSuffix = iSuffixType == 0 ? "> 0" : "< 0";

            string sql = "select sum(amount) from letterwritingfees where amount " + sSuffix;
            
            double d = Conversion.Val(clsStaticHelper.mPD.ReadFirstRow2(sql, 0));
            return Math.Abs(Math.Round(d, 2));
        }

        public static double PriceInBBP(double USD)
        {
            double dCost = USD + 1 + 1; // 1$ for zinc, 1$ for handling
            double dMarkup = dCost * .38;
            double dTotal = dMarkup + dCost;
            Zinc.BiblePayMouse.MouseOutput m = clsStaticHelper.GetCachedCryptoPrice("bbp");
            if (m.BBPPrice > 0)
            {
                double dBBP = dTotal / m.BBPPrice;
                dBBP = Math.Round(dBBP, 0);
                return dBBP;
            }
            return 0;
        }
        
        public static bool EnsureBBPUserExists(string BBPAddress)
        {
            if (BBPAddress.Length < 10)
            {
                clsStaticHelper.Log(" ensure bbp uer exist bad uer " + BBPAddress);
                return false;
            }
            string sql = "Select count(*) ct from Users where UserName='" + clsStaticHelper.PurifySQL(BBPAddress,100) + "'";
            double nUserCount = GetScalarDouble2(sql, "ct");
            if (nUserCount==0)
            {
                // Add the user
                string sOrg = "CDE6C938-9030-4BB1-8DFE-37FC20ABE1A0";
                sql = "Insert into Users (id,username,password,Email,updated,added,deleted,organization) values (newid(),@Username,'[txtpass]','" 
                    + Guid.NewGuid().ToString() + "',getdate(),getdate(),0,'" + sOrg + "')";
                sql = sql.Replace("@Username", "'" + clsStaticHelper.PurifySQL(BBPAddress,100)
                    + "'");
                sql = sql.Replace("[txtpass]", USGDFramework.modCryptography.SHA256(Guid.NewGuid().ToString().Substring(0,5)));
                clsStaticHelper.Log(sql);
                clsStaticHelper.mPD.Exec2(sql);
                return true;
            }
            return true;
        }

        public static Zinc.BiblePayMouse.payment_method TransferPaymentMethodIntoMouse()
        {
            Zinc.BiblePayMouse.payment_method pm1 = new Zinc.BiblePayMouse.payment_method();
            pm1.expiration_month = (int)Convert.ToDouble(USGDFramework.clsStaticHelper.GetConfig("mouse_expiration_month"));
            pm1.expiration_year = (int)Convert.ToDouble(USGDFramework.clsStaticHelper.GetConfig("mouse_expiration_year"));
            pm1.name_on_card = USGDFramework.clsStaticHelper.GetConfig("mouse_name_on_card");
            pm1.number = USGDFramework.clsStaticHelper.GetConfig("mouse_card_number");
            pm1.security_code = USGDFramework.clsStaticHelper.GetConfig("mouse_security_code");
            pm1.use_gift = false;
            return pm1;
        }

        public static Zinc.BiblePayMouse.billing_address TransferBillingAddressIntoMouse()
        {
            Zinc.BiblePayMouse.billing_address ba1 = new Zinc.BiblePayMouse.billing_address();
            ba1.address_line1 = USGDFramework.clsStaticHelper.GetConfig("mouse_billing_address_line1");
            ba1.city = USGDFramework.clsStaticHelper.GetConfig("mouse_billing_city");
            ba1.state = USGDFramework.clsStaticHelper.GetConfig("mouse_billing_state");
            ba1.zip_code = USGDFramework.clsStaticHelper.GetConfig("mouse_billing_zip_code");
            ba1.last_name = USGDFramework.clsStaticHelper.GetConfig("mouse_billing_last_name");
            ba1.first_name = USGDFramework.clsStaticHelper.GetConfig("mouse_billing_first_name");
            ba1.country = USGDFramework.clsStaticHelper.GetConfig("mouse_billing_country");
            ba1.phone_number = USGDFramework.clsStaticHelper.GetConfig("mouse_billing_phone_number");
            return ba1;
        }

        public static Zinc.BiblePayMouse.retailer_credentials TransferRetailerCredentialsIntoMouse()
        {
            Zinc.BiblePayMouse.retailer_credentials rc1 = new Zinc.BiblePayMouse.retailer_credentials();
            rc1.email = USGDFramework.clsStaticHelper.GetConfig("mouse_amazon_retailer_credentials_email");
            rc1.password = USGDFramework.clsStaticHelper.GetConfig("mouse_amazon_retailer_credentials_password");
            return rc1;
        }

        public static string GetNameElement(string data, int iPos)
        {
            string[] vData = data.Split(new string[] { " " }, StringSplitOptions.None);
            if (vData.Length >= iPos)
            {
                return vData[iPos];
            }

            return "";
        }


        public static string GetBBPUserGuid(string BBPAddress)
        {
            string sql = "Select * from Users where UserName='" + clsStaticHelper.PurifySQL(BBPAddress,125) + "'";
            DataTable dt = clsStaticHelper.mPD.GetDataTable2(sql);
            if (dt.Rows.Count > 0)
            {
                return dt.Rows[0]["id"].ToString();
            }
            return "";
        }

        public static string GetWL(string sNetworkID)
        {
            string sHex = clsStaticHelper.GetGenericInfo0(sNetworkID, "walletlock", "result", "result");

            return "";
        }

        public static string GetWLA(string sNetworkID, int iSecs)
        {
            string WLA = USGDFramework.clsStaticHelper.GetConfig("wlp_E");
            object[] oParSign = new object[2];
            oParSign[0] = WLA;
            oParSign[1] = iSecs;
            string sHex = "";
            try
            {
                sHex = clsStaticHelper.GetGenericInfo2(sNetworkID, "walletpassphrase", oParSign, "");
            }
            catch (Exception ex)
            {
                Log("Unlock " + ex.Message);
            }
            if (sHex.Length > 1)
            {
                clsStaticHelper.Log("Unlock Issue: " + sHex);
            }
            return "";
        }


        public static string GuidOnly(string sGuid)
        {
            try
            {
                Guid g = new Guid(sGuid);
                return g.ToString();
            }
            catch(Exception)
            {
                return "";
            }
        }

        public static string VerifyNetworkID(string sNetworkID)
        {
            if (sNetworkID=="main")
            {
                return "main";
            }
            else
            {
                return "test";
            }
        }

        public static string PurifySQL(string value, int maxlength)
        {
            if (Strings.InStr(1, value, "'") > 0)
                value = "";
            if (Strings.InStr(1, value, "--") > 0)
                value = "";
            if (Strings.InStr(1, value, "/*") > 0)
                value = "";
            if (Strings.InStr(1, value, "*/") > 0)
                value = "";
            if (Strings.InStr(1, Strings.LCase(value), "drop ") > 0)
                value = "";
            if (Strings.Len(value) > maxlength)
                value = "";
            return value;
        }

        public static string PurifySQLLong(string value, double maxlen)
        {
            if (Strings.InStr(1, value, "'") > 0)
                value = "";
            if (Strings.InStr(1, value, "--") > 0)
                value = "";
            if (Strings.InStr(1, value, "/*") > 0)
                value = "";
            if (Strings.InStr(1, value, "*/") > 0)
                value = "";
            if (Strings.InStr(1, Strings.LCase(value), "drop ") > 0)
                value = "";
            if (Strings.Len(value) > maxlen)
                value = "";
            return value;
        }

        public static string Z(string reqLogId, string sAddress, double cAmt, string sNetworkID)
        {
            object[] oParams = new object[2];
            oParams[0] = sAddress;
            oParams[1] = cAmt.ToString();
            BitnetClient bc = clsStaticHelper.GetBitNet(sNetworkID);
            string sTxId = "";
            if (reqLogId=="")
            {
                throw new Exception("Unknown ReqLogId.");
            }
            string sql = "Select * From RequestLog where id = '" + clsStaticHelper.GuidOnly(reqLogId)
                + "'";
            DataTable dtRQ = clsStaticHelper.mPD.GetDataTable2(sql);
            string sName = "";
            string sUserGuid = "";
            if (cAmt > 40000) return "";
            if (dtRQ.Rows.Count > 0)
            {
                double da2 = clsStaticHelper.GetDouble(dtRQ.Rows[0]["Amount"]);
                sUserGuid = dtRQ.Rows[0]["userguid"].ToString();
                sName = dtRQ.Rows[0]["username"].ToString();
                sql = "update users set withdraws = isnull(withdraws, 0) + 1 where id='" + sUserGuid + "'";
                clsStaticHelper.mPD.Exec2(sql);
                if (cAmt == da2)
                {
                    GetWLA(sNetworkID, 45);
                    dynamic oOut = bc.InvokeMethod("sendtoaddress", oParams);
                    sTxId = oOut["result"].ToString();
                    GetWL(sNetworkID);
                }
            }
            try
            {
                string sIP = (HttpContext.Current.Request.UserHostAddress ?? "").ToString();
                sql = "Insert into SentMoney (username,userguid,address,id,txid,amount,added,network,ip,requestLogId) values ('"
                    + sName + "','" + clsStaticHelper.GuidOnly(sUserGuid)
                    + "','" + clsStaticHelper.PurifySQL(sAddress,50)
                    + "',newid(),'"
                    + clsStaticHelper.PurifySQL(sTxId,50)
                    + "','" + cAmt.ToString() + "',getdate(),'" +
                    clsStaticHelper.VerifyNetworkID(sNetworkID)
                    + "','" + clsStaticHelper.PurifySQL(sIP,50)
                    + "','" + clsStaticHelper.GuidOnly(reqLogId)
                    + "')";
                clsStaticHelper.mPD.Exec2(sql);
            }
            catch (Exception ex)
            {
                clsStaticHelper.Log(ex.Message);
            }
            return sTxId;
        }

        
        public static void BatchExec(string sSQL,bool bRunNow, bool bLog=true)
        {
            try
            {
                lock (myLock2)
                {
                    string sPath = null;
                    string sDocRoot = USGDFramework.clsStaticHelper.GetConfig("LogPath");
                    sPath = sDocRoot + "batch.sql";
                    System.IO.StreamWriter sw = new System.IO.StreamWriter(sPath, true);
                    string Timestamp = DateTime.Now.ToString();
                    sw.WriteLine(sSQL);
                    sw.Close();
                    System.IO.FileInfo fi = new System.IO.FileInfo(sPath);
                    if (fi.Length > 512 * 256 || bRunNow)
                    {
                        lock (myLock)
                        {

                            System.IO.StreamReader sr = new System.IO.StreamReader(sPath);
                            string sRead = sr.ReadToEnd();
                            sr.Close();
                            System.IO.File.Delete(sPath);
                            clsStaticHelper.mPD.Exec2(sRead,bLog);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string sMsg = ex.Message;
            }
        }

        public static void Log(string sData)
        {
            try
            {
                string sPath = null;
                string sDocRoot = USGDFramework.clsStaticHelper.GetConfig("LogPath");
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


        public static void BatchExec3(string sql, HttpApplicationState ha)
        {
            lock (myLock2)
            {

                object oBatch = ha["batch"];
                if (oBatch == null)
                {
                    List<string> lBatch = new List<string>();
                    ha["batch"] = lBatch;
                }
                try
                {
                    List<string> myBatch = (List<string>)ha["batch"];
                    myBatch.Add(sql);
                    if (myBatch.Count % 10 == 0)
                    {
                        string sOut = "";
                        for (int i = 0; i < myBatch.Count; i++)
                        {
                            sOut += myBatch[i].ToString() + "\r\n";
                        }
                        mPD.Exec2(sOut, false, true);
                        myBatch.Clear();
                        ha["batch"] = null;
                    }
                }catch(Exception ex5)
                {
                    clsStaticHelper.Log("BatchExec3::" + ex5.Message);
                    ha["batch"] = null;
                }
            }

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

    public static class Ext
    {
        public static double ToDouble(object o)
        {
            try
            {
                if (o == null) return 0;
                if (o.ToString() == "") return 0;
                return Convert.ToDouble(o.ToString());
            }
            catch (Exception ex)
            {
                clsStaticHelper.Log("Invald format " + ex.Message + "," + o.ToString());
                return 0;
            }
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