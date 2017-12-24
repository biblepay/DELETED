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
        public static long lBackEndVer = 2022;
        public static bool bResult;
        public static USGDFramework.Data mPD = new USGDFramework.Data();
        public static HttpServerUtility mHttpServer;
        public static SqlConnection sqlConnection;
        public static BitnetClient mBitnetMain;
        public static BitnetClient mBitnetTest;
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
        public static double GetDouble(object o)
        {
            if (o == null) return 0;
            if (o.ToString() == "") return 0;
            double d = Convert.ToDouble(o.ToString());
            return d;
        }

        public static bool AdjustUserBalance(string networkid, string userguid, double amt)
        {
            string sql = "exec AdjustUserBalance '" + networkid  + "','" + amt.ToString() + "','" 
                + userguid + "'";
            mPD.Exec(sql);
            return true;
        }

        public static bool InsTxLog(string susername, string suserguid, 
            string networkid, double iHeight, string sTXID, double amt, double oldBalance, double newBalance, string sDestination, string sTransactionType, string sNotes)
        {
            string sql = "exec InsTxLog '" + iHeight.ToString() + "','" + sTXID + "','" + susername + "','" + suserguid
                + "','" + sTransactionType + "','" + sDestination + "','" + amt.ToString()
                + "','" + oldBalance.ToString() + "','" + newBalance.ToString() + "','"
                + networkid + "','" + sNotes + "'";
            clsStaticHelper.mPD.Exec(sql);
            return true;
        }

        public static Zinc.BiblePayMouse.MouseOutput GetCachedCryptoPrice(string sSymbol)
        {
            string q = "Select * from CryptoPrice where Symbol = '" + sSymbol + "' and Added > getdate()-(1/24.01) ";
            DataTable dt = mPD.GetDataTable(q);
            if (dt.Rows.Count > 0)
            {
                double dBTCPrice = Convert.ToDouble(dt.Rows[0]["BTCPrice"].ToString());
                Zinc.BiblePayMouse.MouseOutput m1 = new Zinc.BiblePayMouse.MouseOutput();
                m1.BTCPrice = dBTCPrice;
                m1.BBPPrice = Convert.ToDouble(dt.Rows[0]["BBPPrice"].ToString());
                return m1;
            }
            string sToken = clsStaticHelper.AppSetting("MouseToken", "");
            Zinc.BiblePayMouse bpm = new Zinc.BiblePayMouse(sToken);
            Zinc.BiblePayMouse.MouseOutput mp = bpm.GetCryptoPrice("bbp");
            // Delete the old record, insert the new record
            if (mp.BTCPrice > 0 && mp.BBPPrice > 0)
            {
                string sql = "Delete from CryptoPrice where Symbol='" + sSymbol + "'";
                sql += "\r\nInsert Into CryptoPrice (id,Symbol,BTCPrice,BBPPrice,Added) values (newid(),'" + sSymbol + "','" 
                    + mp.BTCPrice.ToString() + "','" + mp.BBPPrice.ToString() + "',getdate())";
                mPD.Exec(sql);
            }
            else
            {
                string q1 = "Select * from CryptoPrice where Symbol = '" + sSymbol + "' ";
                DataTable dt1 = mPD.GetDataTable(q1);
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
                if (oBit == null || mBitnetUseCount < 3)
                {
                    oBit = new BitnetClient(AppSetting("RPCURL" + sNetworkID, ""));
                    string sPass = AppSetting("RPCPass" + sNetworkID, "");
                    NetworkCredential Cr = new NetworkCredential(AppSetting("RPCUser" + sNetworkID, ""), sPass);
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

        public static double GetSystemDouble(string sKey)
        {
            string sql = "Update System Set Value=Value+1 where systemKey='" + sKey + "'";
            mPD.Exec(sql);
            sql = "Select Value from System where systemKey='" + sKey + "'";
            double d1 = GetScalarDouble(sql, "Value");
            return d1;
        }

        public static object Housecleaning(string sNetworkID, HttpServerUtility server, HttpApplicationState ha, bool bForce)
        {
            try
            {
                if (string.IsNullOrEmpty(sNetworkID)) sNetworkID = "test";
                //Once every interval perform housecleaning
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
                    // Allow up to 10 threads to hit this thing at once, then fall through and lock this thread
                    double d1 = GetSystemDouble("main_housecleaning");
                    if (d1 % 10 == 0)
                    {
                        lock (myLock)
                        {
                            AppCache("lastscan" + sNetworkID, DateTime.Now.ToString(), server, ha);
                            AppCache("lastcleaning" + sNetworkID, DateTime.Now.ToString(), server, ha);
                            if (sHealth != "HEALTH_DOWN")
                            {
                                ScanBlocksForPoolBlocksSolved(sNetworkID, bForce);
                            }
                            string sql = "exec UpdatePool '' ";
                            mPD.Exec(sql);
                            // Verify all solutions
                            VerifySolutions(sNetworkID);
                            //Clear out old Work records (networkid does not matter as old is old) check to see if any blocks are solved
                            if (clsStaticHelper.LogLimiter() > 990)
                            {
                                RewardUpvotedLetterWriters();
                            }
                            return true;
                        }
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
                    oParams[0] = "pmninfo";
                    BitnetClient oBit = GetBitNet(sNetworkID);
                    dynamic oOut = oBit.InvokeMethod("exec", oParams);
                    double nNonceInfo = GetDouble(oOut["result"]["pmninfo"]);
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

        public static string GetBlockTx(string sNetworkid, string sCommand, int iBlockNumber, int iPositionId)
        {
            try
            {
                BitnetClient oBit = GetBitNet(sNetworkid);
                object[] oParams = new object[1];
                oParams[0] = iBlockNumber.ToString();
                dynamic oOut = oBit.InvokeMethod(sCommand,oParams);
                // Default RPC result key is named "result"
                string sOut = oOut["result"]["tx"][iPositionId].ToString();
                return sOut;
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
            DataTable dt = clsStaticHelper.mPD.GetDataTable(sql);
            string sToken = clsStaticHelper.AppSetting("MouseToken", "");
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
                        string sql3 = "Update orders set Status3='" + mo.tracking + "' where id = '" + sOrderGuid + "'";
                        clsStaticHelper.mPD.Exec(sql3);
                    }
                }
                if (mo.code.Length > 1)
                {
                    string sql2 = "Update orders set Status1 = '" + Left(mo.code, 20) + "' where id = '" + sOrderGuid + "'";
                    clsStaticHelper.mPD.Exec(sql2);
                }
                if (mo.message != null)
                {
                    if (mo.message.Length > 1)
                    {
                        string sStatus = MsgToStatus(mo.message);
                        string sql2 = "Update orders set Updated=getdate(),Status1='PLACED',Status2 = '" + sStatus + "' where id = '" + sOrderGuid + "'";
                        clsStaticHelper.mPD.Exec(sql2);
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

                    if (sRecipient == sPoolRecvAddress)
                    {
                        string sMinerNameByHashPs = "?";

                        string sql1 = "Select isnull(Username,'') as UserName,isnull(MinerName,'') as MinerName from leaderboard" + sNetworkID + " order by HPS2 desc";
                            DataTable dt10 = mPD.GetDataTable(sql1);
                        if (dt10.Rows.Count > 0)
                        {
                            sMinerNameByHashPs = dt10.Rows[0]["Username"].ToString() + "." + dt10.Rows[0]["MinerName"].ToString();
                        }
                        // Populate the Miner_who_found_block field:
                        string sVersion = GetBlockInfo(nHeight, "blockversion", sNetworkID);
                        string sMinerGuid = GetBlockInfo(nHeight, "minerguid", sNetworkID);
                        sql1 = "Select isnull(username,'') as UserName from Miners where id='" + sMinerGuid + "'";
                        dt10 = mPD.GetDataTable(sql1);
                        string sMinerNameWhoFoundBlock = "?";

                        if (dt10.Rows.Count > 0)
                        {
                            sMinerNameWhoFoundBlock = (dt10.Rows[0]["UserName"] ?? "").ToString();
                        }

                        string sql = "insert into blocks (id,height,updated,subsidy,networkid,minernamebyhashps,minerid,blockversion,MinerNameWhoFoundBlock) values (newid(),'" 
                            + Strings.Trim(nHeight.ToString()) + "',getdate(),'" + cSub.ToString() + "','" + sNetworkID + "','" + sMinerNameByHashPs + "','" + sMinerGuid + "','" 
                            + sVersion + "','" + sMinerNameWhoFoundBlock + "')";
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
                        //clsStaticHelper.Log(" CLAWBACK FOR HEIGHT " + nHeight.ToString());
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
                if (sUserId == "") return 0;
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

        public static bool GetUserBalances(string sNetworkID, string userguid, ref double dBalance, ref double dImmature)
        {
            if (userguid == "") return false;

            string sql = "Select isnull(Balance" + sNetworkID + ",0) as bal1 From Users with (nolock) where id='" + userguid
                + "' and deleted=0";
            dBalance = mPD.GetScalarDouble(sql, "bal1");
            sql = "Select isnull(sum(amount),0) As Immature from transactionlog where updated > getdate()-1 And userid='" + userguid
                + "'"
                    + " And networkid = '" + sNetworkID + "' and transactiontype='MINING_CREDIT'";
            dImmature = mPD.GetScalarDouble(sql, "Immature");
            return true;
        }


        public static double AwardBounty(string sLetterID, string sUserId, double dAmount, string sBOUNTYNAME, double nHeight, string sTxId, string sNetworkID, string sNotes, bool bLetter)
        {
            try
            {
                string sUserName = "";
                double cBalance = GetUserBalance(sUserId, sNetworkID, ref sUserName);
                double cNewBalance = cBalance + dAmount;
                string sql2 = "Insert into transactionlog (id,height,transactionid,username,userid,transactiontype,destination,amount,oldbalance,newbalance,added,updated,rake,networkid,notes)" + " values (newid(),'" 
                    + nHeight.ToString()  + "','" 
                    + Strings.Trim(sTxId) + "','" + sUserName + "','"
                    + sUserId + "','" +sBOUNTYNAME + "','" + sLetterID + "','"
                    + Strings.Trim(dAmount.ToString()) + "','"
                    + Strings.Trim(cBalance.ToString()) + "','"
                    + Strings.Trim(cNewBalance.ToString())
                    + "',getdate(),getdate(),0,'" + sNetworkID + "','" + sNotes + "')";
                mPD.Exec(sql2);
                //update balance now if the tx was inserted:
                sql2 = "Update Users set balance" + sNetworkID + " = '" + Strings.Trim(cNewBalance.ToString()) + "' where id = '" + Strings.Trim(sUserId) + "'";
                mPD.Exec(sql2);
                if (bLetter)
                {
                    //Mark the record Paid
                    sql2 = "Update Letters set PAID=1 where id = '" + sLetterID + "'";
                    mPD.Exec(sql2);
                    sql2 = "Insert into Letterwritingfees (id,height,added,amount,networkid,quantity) values (newid(),0,getdate(),'" + (-1 * dAmount).ToString() + "','" + sNetworkID + "',0)";
                    mPD.Exec(sql2);
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


        public static double GetTotalBounty()
        {
            string sql = "select isnull(sum(amount),0) as amount from LetterWritingFees";
            double d = Conversion.Val(clsStaticHelper.mPD.ReadFirstRow(sql, 0));
            return d;
        }

        
        public static void RewardUpvotedLetterWriters()
        {
            //Pay out any funds marked as unpaid and add a transaction to transactionLog
            string sql = "select * from letters where paid <> 1 and upvote >= 10";
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
                        AwardBounty(sId,sUserid, dIndBounty, "LETTER_WRITING", 0, sId.ToString(), "main", sId.ToString(),true);
                    }
                }
            }
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
            string sql = "Select count(*) ct from Users where UserName='" + BBPAddress + "'";
            double nUserCount = GetScalarDouble(sql, "ct");
            if (nUserCount==0)
            {
                // Add the user
                string sOrg = "CDE6C938-9030-4BB1-8DFE-37FC20ABE1A0";
                sql = "Insert into Users (id,username,password,Email,updated,added,deleted,organization) values (newid(),@Username,'[txtpass]','" + Guid.NewGuid().ToString() + "',getdate(),getdate(),0,'" + sOrg + "')";
                sql = sql.Replace("@Username", "'" + BBPAddress + "'");
                sql = sql.Replace("[txtpass]", modCryptography.Des3EncryptData(Guid.NewGuid().ToString().Substring(0,5)));
                clsStaticHelper.Log(sql);
                clsStaticHelper.mPD.Exec(sql);
                return true;
            }
            return true;
        }

        public static Zinc.BiblePayMouse.payment_method TransferPaymentMethodIntoMouse()
        {
            Zinc.BiblePayMouse.payment_method pm1 = new Zinc.BiblePayMouse.payment_method();
            pm1.expiration_month = (int)Convert.ToDouble(clsStaticHelper.AppSetting("mouse_expiration_month", "0"));
            pm1.expiration_year = (int)Convert.ToDouble(clsStaticHelper.AppSetting("mouse_expiration_year", "0"));
            pm1.name_on_card = clsStaticHelper.AppSetting("mouse_name_on_card", "");
            pm1.number = clsStaticHelper.AppSetting("mouse_card_number", "");
            pm1.security_code = clsStaticHelper.AppSetting("mouse_security_code", "");
            pm1.use_gift = false;
            return pm1;
        }

        public static Zinc.BiblePayMouse.billing_address TransferBillingAddressIntoMouse()
        {
            Zinc.BiblePayMouse.billing_address ba1 = new Zinc.BiblePayMouse.billing_address();
            ba1.address_line1 = clsStaticHelper.AppSetting("mouse_billing_address_line1", "");
            ba1.city = clsStaticHelper.AppSetting("mouse_billing_city", "");
            ba1.state = clsStaticHelper.AppSetting("mouse_billing_state", "");
            ba1.zip_code = clsStaticHelper.AppSetting("mouse_billing_zip_code", "");
            ba1.last_name = clsStaticHelper.AppSetting("mouse_billing_last_name", "");
            ba1.first_name = clsStaticHelper.AppSetting("mouse_billing_first_name", "");
            ba1.country = clsStaticHelper.AppSetting("mouse_billing_country", "");
            ba1.phone_number = clsStaticHelper.AppSetting("mouse_billing_phone_number", "");
            return ba1;
        }

        public static Zinc.BiblePayMouse.retailer_credentials TransferRetailerCredentialsIntoMouse()
        {
            Zinc.BiblePayMouse.retailer_credentials rc1 = new Zinc.BiblePayMouse.retailer_credentials();
            rc1.email = clsStaticHelper.AppSetting("mouse_amazon_retailer_credentials_email", "");
            rc1.password = clsStaticHelper.AppSetting("mouse_amazon_retailer_credentials_password", "");
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
            string sql = "Select * from Users where UserName='" + BBPAddress + "'";
            DataTable dt = clsStaticHelper.mPD.GetDataTable(sql);
            if (dt.Rows.Count > 0)
            {
                return dt.Rows[0]["id"].ToString();
            }
            return "";
        }

        public static void VerifySolutions(string sNetworkID)
        {
            string sql = "SELECT * from WORK where Validated is null and endtime is not null and networkid='" + sNetworkID + "'";
            DataTable dt = mPD.GetDataTable(sql);
            int nLowTip = 1;
            int nLowTipThreshhold = 4;
            nLowTip = GetTipHeight(sNetworkID);
            int nHighTip = nLowTip + nLowTipThreshhold;
            double nMaxNonce = GetNonceInfo(sNetworkID);
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
                    string sNonce = "0";
                    if (vSolution.Length > 13) sNonce = vSolution[13];
                    double dNonce = GetDouble(sNonce);
                    string sID = dt.Rows[i]["id"].ToString();

                    int nTheirTip = Convert.ToInt32(sPrevHeight);
                    string sError = "";
                    if ((nTheirTip < (nLowTip-nLowTipThreshhold)) || (nTheirTip > nHighTip))
                    {
                        sError = "BLOCK_IS_STALE"; // <RESPONSE>BLOCK_IS_STALE</RESPONSE><ERROR>BLOCK_IS_STALE</ERROR><EOF></HTML>
                    }
                    //Verify solution matches
                    string sOurSolution = clsStaticHelper.GetBibleHash(sBlockHash, sBlockTime, sPrevBlockTime, sPrevHeight, sNetworkID, sNonce);
                    if (sHashSolution.Length < 10 || sOurSolution.Length < 10)
                    {
                        sError = "MALFORMED_SOLUTION";
                    }

                    if (dNonce > nMaxNonce && nLowTip > 23000)
                    {
                        sError = "BLOCK_SOLUTION_INVALID";
                    }
                    if (dNonce < 1 && nLowTip > 23000)
                    {
                        sError = "BLOCK_SOLUTION_INCOMPLETE";
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
        public static string GetWL(string sNetworkID)
        {
            string sHex = clsStaticHelper.GetGenericInfo0(sNetworkID, "walletlock", "result", "result");

            return "";
        }

        public static string GetWLA(string sNetworkID, int iSecs)
        {
            string WLA = modCryptography.Des3DecryptData(clsStaticHelper.AppSetting("wlp", ""));
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

        public static void AuditWithdrawals(string sNetworkID)
        {
            // Verify everything coming out of the wallet matches a requestlog guid
            // if not, insert a record.  If so audit the record.
            try
            {
                // Audit every 4 hours or so
                string sql10 = "Select Added from metrics";
                string a = clsStaticHelper.mPD.GetScalarString(sql10, "Added");
                var diffInSeconds = (System.DateTime.Now - Convert.ToDateTime(a)).TotalSeconds;
                if (diffInSeconds < (4 * 60 * 60)) return;
                double dAdjTime = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                BitnetClient b = GetBitNet(sNetworkID);
                object[] oParams = new object[2];
                oParams[0] = "*";
                oParams[1] = 988;
                dynamic oOut = b.InvokeMethod("listtransactions", oParams);
                // Loop Through the Vouts and get the recip ids and the amounts
                string sOut = "";
                double dMinTime = 991513008835;
                double dMaxTime = 0;
                double dWindow = 0;
                double dAge = 0;
                double dWalletDebit = 0;
                for (int y = 0; y < 599; y++)
                {
                    string sPtr = "";
                    try
                    {
                        sPtr = (oOut["result"][y] ?? "").ToString();
                    }
                    catch (Exception ey)
                    {

                    }

                    if (sPtr != "")
                    {
                        double dAmount = clsStaticHelper.GetDouble(oOut["result"][y]["amount"]);
                        string sAddress = "";
                        string sCategory = oOut["result"][y]["category"].ToString();
                        double dTime = clsStaticHelper.GetDouble(oOut["result"][y]["time"]);
                        string sTxid = oOut["result"][y]["txid"].ToString();
                        if (dTime < dMinTime) dMinTime = dTime;
                        if (dTime > dMaxTime) dMaxTime = dTime;
                        dWindow = dMaxTime - dMinTime; // When window surpasses one day, we no longer log the withdraw amount
                        dAge = dAdjTime - dTime;
                        if (oOut["result"][y]["address"] != null)
                        {
                            sAddress = oOut["result"][y]["address"].ToString();
                        }
                        else { sAddress = "?"; } //Happens when pool pays itself
                        if (sCategory == "send")
                        {
                            string sql3 = "Select * From requestLog where txid = '" + sTxid + "'";
                            DataTable rl = clsStaticHelper.mPD.GetDataTable(sql3);
                            string sID = "";
                            string sUserGuid = "";
                            if (rl.Rows.Count > 0)
                            {
                                sUserGuid = rl.Rows[0]["userguid"].ToString();
                                sID = rl.Rows[0]["id"].ToString();
                            }
                            if (sID.Length > 0)
                            {
                                sql3 = "Update RequestLog set Audited=1 where id = '" + sID + "'";
                                clsStaticHelper.mPD.Exec(sql3);
                            }
                            else
                            {
                                // insert questionable record
                                string sql2 = "Insert into RequestLog (username,userguid,address,id,txid,amount,added,network,ip,audited,questionable) values ('"
                                        + "',null,'" + sAddress + "',null,'" + sTxid + "','" + dAmount.ToString()
                                        + "',getdate(),'" + sNetworkID + "',null,1,1)";
                                clsStaticHelper.mPD.Exec(sql2);
                            }
                            // Check the users balance
                            string sUserName = "";
                            double dBal = clsStaticHelper.GetUserBalance(sUserGuid, sNetworkID, ref sUserName);
                            if (dBal <= .05)
                            {
                                sql3 = "Update RequestLog set Questionable=1 where TXID='" + sTxid + "'";
                                clsStaticHelper.mPD.Exec(sql3);
                            }
                            // Adjust for UTC time
                            double dCutoff = 86400 + (60 * 60 *  4);
                            if (dWindow < dCutoff)  dWalletDebit += dAmount;
                            if (dWindow > dCutoff) break;
                        }

                    }
                    else
                    {
                        break;
                    }
                }
                // Now grab the total mined in 24 hour,the total withdraws in 24 hour, and - the total withdraws from the wallet in 24 hour time period:
                string sql = "Select sum(Amount) a from TransactionLog where transactionType='mining_credit' and added > getdate()-1 and networkid = '" + sNetworkID + "'";
                double dCredit = clsStaticHelper.mPD.GetScalarDouble(sql, "a");
                sql = "Select sum(Amount) a from TransactionLog where transactionType = 'withdrawal' and added > getdate()-1 and networkid='" + sNetworkID + "'";
                double dDebit = clsStaticHelper.mPD.GetScalarDouble(sql, "a");
                sql = "delete from Metrics where 1=1";
                clsStaticHelper.mPD.Exec(sql);
                sql = "Insert into Metrics (id,network,credits,debits,walletdebits,added) values (newid(),'" + sNetworkID + "','" + dCredit.ToString() + "','" 
                    + dDebit.ToString() + "','" + dWalletDebit.ToString() + "',getdate())";
                clsStaticHelper.mPD.Exec(sql);
            }
            catch (Exception ex)
            {
                clsStaticHelper.Log("Audit::" + ex.Message);
            }
        }


        public static string Z(string reqLogId, string sAddress, double cAmt, string sNetworkID)
        {
            object[] oParams = new object[2];
            oParams[0] = sAddress;
            oParams[1] = cAmt.ToString();
            BitnetClient bc = clsStaticHelper.GetBitNet(sNetworkID);
            string sTxId = "";
            string sql = "Select * From RequestLog where id = '" + reqLogId + "'";
            DataTable dtRQ = clsStaticHelper.mPD.GetDataTable(sql);
            string sName = "";
            string sUserGuid = "";
            if (cAmt > 40000) return "";
            if (dtRQ.Rows.Count > 0)
            {
                double da2 = clsStaticHelper.GetDouble(dtRQ.Rows[0]["Amount"]);
                sUserGuid = dtRQ.Rows[0]["userguid"].ToString();
                sName = dtRQ.Rows[0]["username"].ToString();
                sql = "update users set withdraws = isnull(withdraws, 0) + 1 where id='" + sUserGuid + "'";
                clsStaticHelper.mPD.Exec(sql);
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
                    + sName + "','" + sUserGuid + "','" + sAddress + "',newid(),'"
                    + sTxId + "','" + cAmt.ToString() + "',getdate(),'" + sNetworkID + "','" + sIP + "','" + reqLogId + "')";
                clsStaticHelper.mPD.Exec(sql);
            }
            catch (Exception ex)
            {
                clsStaticHelper.Log(ex.Message);
            }
            return sTxId;
        }


        public static void PayBlockParticipants(string sNetworkID)
        {
            // Command Timeout Expired:
            string sql3 = "exec payBlockParticipants '" + sNetworkID + "'";
            mPD.ExecWithTimeout(sql3, 11000);
            return;
        }

        public static void AddBlockDistribution(long nHeight, double cBlockSubsidy, string sNetworkID)
        {
            //Ensure this block distribution does not yet exist
            string sql = "select count(*) as ct from block_distribution where height='" + nHeight.ToString() + "' and networkid='" + sNetworkID + "'";
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