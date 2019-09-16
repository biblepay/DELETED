using Bitnet.Client;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices;

namespace USGDFramework
{

    public static class HexadecimalEncoding
    {

        public static string ConvertFromHexStringToAscii(String hexString)
        {
            try
            {
                string ascii = string.Empty;

                for (int i = 0; i < hexString.Length; i += 2)
                {
                    String hs = string.Empty;

                    hs = hexString.Substring(i, 2);
                    uint decval = System.Convert.ToUInt32(hs, 16);
                    char character = System.Convert.ToChar(decval);
                    ascii += character;

                }

                return ascii;
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            return string.Empty;
        }


        public static string ToHexString(string str)
        {
            var sb = new System.Text.StringBuilder();

            var bytes = System.Text.Encoding.ASCII.GetBytes(str);
            foreach (var t in bytes)
            {
                sb.Append(t.ToString("X2"));
            }
            return sb.ToString();
        }

        public static string FromHexString(string hexString)
        {
            var bytes = new byte[hexString.Length / 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }
            return System.Text.Encoding.Unicode.GetString(bytes); 
        }
    }

    public static class Shared
    {

        public static DateTime dtMyAnchorTime = DateTime.Now;
        public static USGDFramework.Data mPD = new USGDFramework.Data();
        public static object myLock = "A";
        public static BitnetClient mBitnetMain;
        public static BitnetClient mBitnetTest;
        public static double nCurrentTipHeightMain = 0;
        public static double mBitnetUseCount = 0;
        public static long lBackEndVer = 2025;
        public static bool bResult;
        public static SqlConnection sqlConnection;
        public static string msReadOnly = "background-color: black;";
        public static string sHealthMain = "";


        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEMTIME
        {
            public short wYear;
            public short wMonth;
            public short wDayOfWeek;
            public short wDay;
            public short wHour;
            public short wMinute;
            public short wSecond;
            public short wMilliseconds;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetSystemTime(ref SYSTEMTIME st);
        public static DateTime GetCurrentUtcTime()
        {
            DateTime t = DateTime.Now;
            t = t.AddHours(6);
            return t;
        }
        public static void SetMyTime(short year, short month, short day1, short hour1, short minute1, short second1)
        {
            SYSTEMTIME st = new SYSTEMTIME();
            st.wYear = year;
            st.wMonth = month;
            st.wDay = day1;
            st.wHour = hour1;
            st.wMinute = minute1;
            st.wSecond = second1;
            SetSystemTime(ref st);
        }

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
                Log("Invald format " + ex.Message + "," + o.ToString());
                return 0;
            }
        }

        public static bool SendEmail(string strTo, string strSubject, string strBody, bool blnHTML = true, bool bCCBiblepay = false)
        {
            bool isSend = false;
            System.Net.Mail.MailMessage mailmsg = new System.Net.Mail.MailMessage();
            System.Net.Mail.MailAddress mailfrom =
                new System.Net.Mail.MailAddress(USGDFramework.clsStaticHelper.GetConfig("smtpreplytoemail"),
                USGDFramework.clsStaticHelper.GetConfig("smtpreplytoname"));
            System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient();
            strTo.Replace(" ", "");
            string[] strRecipients = Strings.Split(strTo, ";");
            System.Net.NetworkCredential smtpuser = new System.Net.NetworkCredential(USGDFramework.clsStaticHelper.GetConfig("smtpuser").ToString(),
                USGDFramework.clsStaticHelper.GetConfig("smtppassword_E").ToString());

            mailmsg.To.Add(strRecipients[0]);
            mailmsg.IsBodyHtml = blnHTML;
            mailmsg.From = mailfrom;
            mailmsg.Subject = strSubject;
            if (bCCBiblepay) mailmsg.Bcc.Add(USGDFramework.clsStaticHelper.GetConfig("smtpreplyto"));
            smtp.UseDefaultCredentials = false;
            smtp.EnableSsl = true;
            smtp.Port = (int)Convert.ToDouble(USGDFramework.clsStaticHelper.GetConfig("smtpport").ToString());
            mailmsg.Body = strBody;
            mailmsg.Priority = MailPriority.High;
            smtp.Host = USGDFramework.clsStaticHelper.GetConfig("smtpserver").ToString();
            smtp.Credentials = smtpuser;

            try
            {
                smtp.Send(mailmsg);
                isSend = true;
            }
            catch (Exception ex)
            {
                if (false)
                {
                    Log("SendEmail: We encountered a problem while sending the outbound e-mail to " + strTo.ToString() + ", port " + smtp.Port.ToString() + ", host " + smtp.Host + ", Email error: " + ex.Message);
                }
                isSend = false;
            }
            return isSend;
        }

        public static void VerifySystemTime()
        {
            DateTime currentTime = GetCurrentUtcTime();
            TimeSpan tsDelta = currentTime.Subtract(dtMyAnchorTime);
            if (tsDelta.TotalMinutes > 60)
            {
                int HoursOff = (int)(tsDelta.TotalSeconds + (60 * 10)) / (60 * 60);
                // subtract this many hours off current time and change time
                DateTime newTime = currentTime.Subtract(new TimeSpan(HoursOff, 0, 0));
                SetMyTime((short)newTime.Year, (short)newTime.Month, (short)newTime.Day, (short)newTime.Hour, (short)newTime.Minute, (short)newTime.Second);
                dtMyAnchorTime = newTime;
            }
            else
            {
                dtMyAnchorTime = currentTime;
            }
        }

        public static void Log(string sData)
        {
            try
            {
                string sPath = null;
                string sDocRoot = USGDFramework.clsStaticHelper.GetConfig("LogPath");
                sPath = sDocRoot + "service2018.dat";
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

        
        public static void LogSQLI(string sData)
        {
            try
            {
                string sPath = null;
                string sDocRoot = USGDFramework.clsStaticHelper.GetConfig("LogPath");
                sPath = sDocRoot + "sqlinjection.dat";
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
        
        public static void VerifySolutions(string sNetworkID)
        {
            string sql = "SELECT * from WORK (nolock) where Validated is null and endtime is not null and networkid='" + sNetworkID + "'";
            DataTable dt = mPD.GetDataTable2(sql, false);
            int nLowTip = 1;
            int nLowTipThreshhold = 4;
            nLowTip = GetTipHeight(sNetworkID);
            int nHighTip = nLowTip + nLowTipThreshhold;
            double nMaxNonce = GetNonceInfo(sNetworkID);
            List<string> sNonces = clsServiceOnly.MemorizeNonces(sNetworkID);
            List<string> poolAddresses = MemorizePoolAddresses(sNetworkID);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string sSolution = dt.Rows[i]["Solution"].ToString();
                string sUserId = (dt.Rows[i]["UserId"] ?? "").ToString();
                string[] vSolution = sSolution.Split(new string[] { "," }, StringSplitOptions.None);
                if (vSolution.Length > 11)
                {
                    string sBlockHash = vSolution[0];
                    string sBlockTime = vSolution[1];
                    string sPrevBlockTime = vSolution[2];
                    string sPrevHeight = vSolution[3];
                    string sHashSolution = vSolution[4];
                    string sBlockHex = dt.Rows[i]["SolutionBlockHash"].ToString();
                    string sMinerSolving = vSolution[5];
                    sql = "Select count(*) ct from Work (nolock) where SolutionBlockHash = '" + sBlockHex + "' and ID <> '" + dt.Rows[i]["id"].ToString() + "'";
                    double dDuplicate = mPD.GetScalarDoubleWithNoLog2(sql, "ct");
                    try
                    {
                        string sMySubmit = Submit1(sBlockHex, sNetworkID);
                    }
                    catch(Exception ex2)
                    {
                    }

                    string sAscii = HexadecimalEncoding.ConvertFromHexStringToAscii(sBlockHex);
                    string sTxHex = dt.Rows[i]["SolutionTxHash"].ToString();
                    string sNonce = "0";
                    if (vSolution.Length > 13) sNonce = vSolution[13];
                    double dNonce = GetDouble(sNonce);
                    string sID = dt.Rows[i]["id"].ToString();
                    string sTotal = sUserId + dNonce.ToString();
                    string sMinername = dt.Rows[i]["minername"].ToString();
                    string sMinerGuid = ExtractXML(sAscii, "<MINERGUID>", "</MINERGUID>").ToString();

                    int nTheirTip = Convert.ToInt32(sPrevHeight);
                    string sError = "";
                    if ((nTheirTip < (nLowTip - nLowTipThreshhold)) || (nTheirTip > nHighTip))
                    {
                        sError = "BLOCK_IS_STALE"; // <RESPONSE>BLOCK_IS_STALE</RESPONSE><ERROR>BLOCK_IS_STALE</ERROR><EOF></HTML>
                    }
                    //Verify solution matches
                    string sBibleHash2 = "";
                    string sBlockMessage = "";
                    string sError5 = "";
                    bool bABNValid = false;
                    double nABNWeight = 0;
                    string sRecipient = AnalyzeSolutionBlock(sNetworkID, sBlockHex, sTxHex, ref sBibleHash2, ref sBlockMessage,
                        ref bABNValid, ref nABNWeight, ref sError5);

                    if (dDuplicate > 0)
                    {
                        sError = "DUPL_SOL";
                    }
                    if (sMinerSolving != sMinerGuid)
                    {
                        sError = "INV_MINER_GUID";
                    }
                    if (sBibleHash2 != sHashSolution)
                    {
                        sError = "INVALID_SOLUTION_2";
                    }

                    if (!bABNValid)
                    {
                        sError = "ABN_WEIGHT_TOO_LOW";
                    }

                    if (sBlockHex.Length < 5)
                    {
                        sError = "INVALID_BLOCK_HEX_PLEASE_UPGRADE ";
                    }

                    // check pool recipient
                    if (!poolAddresses.Contains(sRecipient))
                    {
                        sError = "INVALID_SCRIPTSIG";
                    }
                    if (sHashSolution.Length < 10 || sBibleHash2.Length < 10)
                    {
                        sError += "MALFORMED_SOLUTION ";
                    }

                    if (dNonce > nMaxNonce && nLowTip > 23000 && nMaxNonce > 1)
                    {
                        sError = "BLOCK_SOLUTION_INVALID";
                    }

                    if (sNonces.Count > 1 && sNonces.Contains(sTotal))
                    {
                        sError = "BLOCK_SOLUTION_STALE";
                    }

                    try
                    {
                        string sTargetHash = dt.Rows[i]["hashtarget"].ToString();
                        if (sBibleHash2 == "") sBibleHash2 = "9999999999999999999";
                        string sOurPrefix = sBibleHash2.Substring(0, 10);
                        string sTargetPrefix = sTargetHash.Substring(0, 10);
                        decimal dOurPrefix = long.Parse(sOurPrefix, System.Globalization.NumberStyles.HexNumber);
                        decimal dTargPrefix = long.Parse(sTargetPrefix, System.Globalization.NumberStyles.HexNumber);

                        if (dOurPrefix > dTargPrefix) sError += "HIGH_HASH ";
                        // They made it
                        double nHeight = GetDouble(sPrevHeight) + 1;
                        if (sError == "")
                        {
                            string sql2 = "Update Work Set Solution2='', Validated=1 WHERE id = '" + sID + "'";
                            mPD.Exec2(sql2, false, false);
                        }
                        else
                        {
                            string sql2 = "Update Work Set Error='" + sError + "',Validated=1,EndTime=null where id = '" + sID + "'";
                            mPD.ExecResilient2(sql2, false);
                        }
                    }
                    catch (Exception ex2)
                    {
                        Log("Encountered Error while Verifying " + ex2.Message);
                    }
                    System.Threading.Thread.Sleep(7);

                }
            }
        }

        public static object ExtractXML(string sData, string sStartKey, string sEndKey)
        {
            int iPos1 = Strings.InStr(1, sData, sStartKey);
            if (iPos1 == 0) return "";
            iPos1 = iPos1 + Strings.Len(sStartKey);
            int iPos2 = Strings.InStr(iPos1, sData, sEndKey);
            if (iPos2 == 0) return "";
            string sOut = Strings.Mid(sData, iPos1, iPos2 - iPos1);
            return sOut;
        }

        public static void DeleteOldLogs()
        {
            string sDocRoot = USGDFramework.clsStaticHelper.GetConfig("LogPath");
            foreach (string xmlFile in System.IO.Directory.GetFiles(sDocRoot, "*.dat"))
            {
                System.IO.FileInfo fi = new System.IO.FileInfo(xmlFile);
                double Age = (DateTime.Now - fi.LastWriteTime).TotalDays;
                if (Age > 5)
                {
                    System.IO.File.Delete(xmlFile);
                }
            }
        }

        public static void BatchExec23(string sSQL, bool bRunNow, bool bLog = true)
        {
            try
            {
                string sPath = null;
                string sDocRoot = USGDFramework.clsStaticHelper.GetConfig("LogPath");
                sPath = sDocRoot + "batch2.sql";
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
                        mPD.ExecResilient2(sRead, false);
                    }
                }
            }
            catch (Exception ex)
            {
                string sMsg = ex.Message;
            }
        }


        public static double LogLimiter()
        {
            Random d = new Random();
            int iHow = d.Next(1000);
            return iHow;
        }

        public static double GetRequiredLetterCount()
        {
            string sql = "select isnull(count(*),0) as Ct from Orphans";
            double dct = GetDouble(mPD.ReadFirstRow2(sql, 0));
            return dct;
        }

        public static double GetTotalBounty()
        {
            string sql = "select isnull(sum(amount),0) as amount from LetterWritingFees";
            double d = GetDouble(mPD.ReadFirstRow2(sql, 0));
            return d;
        }

        public static void RewardUpvotedLetterWriters()
        {
            //Pay out any funds marked as unpaid and add a transaction to transactionLog
            string sql = "select * from letters where paid <> 1 and upvote >= 10 and added > getdate()-15";
            DataTable dt = new DataTable();
            dt = mPD.GetDataTable2(sql);
            double dApprovals = dt.Rows.Count;
            double dChildren = GetRequiredLetterCount();
            double dBounty = GetTotalBounty();
            double dFactor = Convert.ToDouble(USGDFramework.clsStaticHelper.GetConfig("letterwritingfactor"));

            double dIndBounty = (dBounty / (dChildren + .01)) * dFactor;
            for (int y = 0; y <= dt.Rows.Count - 1; y++)
            {
                string sId = dt.Rows[y]["id"].ToString();
                string sUserid = dt.Rows[y]["userid"].ToString();
                if (sId.Length > 1)
                {
                    if (dIndBounty > 1)
                    {
                        AwardBounty(sId, sUserid, dIndBounty, "LETTER_WRITING", 0, sId.ToString(), "main", sId.ToString(), true);
                    }
                }
            }

            // Pay voters for voting
            sql = "select userid,count(*) vct from votes where rewarded is null group by userid ";
            dt = new DataTable();
            dt = mPD.GetDataTable2(sql);
            for (int y = 0; y <= dt.Rows.Count - 1; y++)
            {
                double vct = GetDouble(dt.Rows[y]["vct"]);
                string sUserid = dt.Rows[y]["userid"].ToString();
                string sId = dt.Rows[y]["id"].ToString();

                if (sUserid.Length > 1)
                {
                    string sql2 = "Update Votes set rewarded=1 where id = '" + sId + "'";
                    mPD.Exec2(sql2);
                    dIndBounty = vct * 5;
                    if (dIndBounty > 1000) dIndBounty = 1000;
                    if (dIndBounty > 1)
                    {
                        AwardBounty(sId, sUserid, dIndBounty, "LETTER_VOTING", 0, sId.ToString(), "main", sId.ToString(), true);
                    }
                }
            }
        }

        public static double GetUserBalance(string sUserId, string sNetworkID, ref string sUserName)
        {
            try
            {
                if (sUserId == "") return 0;
                string sql = "Select isnull(Balance" + sNetworkID + ",0) as balance,USERNAME from Uz where id = '" + sUserId + "'";
                DataTable dtBal = default(DataTable);
                dtBal = mPD.GetDataTable2(sql);
                double cBalance = 0;
                if (dtBal.Rows.Count > 0)
                {
                    cBalance = GetDouble(dtBal.Rows[0]["Balance"]);
                    sUserName = dtBal.Rows[0]["UserName"].ToString();
                }
                return cBalance;
            }
            catch (Exception ex)
            {
                Log("unable to get user balance for user " + sUserId);
                return 0;
            }
        }


        public static double AwardBounty(string sLetterID, string sUserId, double dAmount, string sBOUNTYNAME, double nHeight, string sTxId, string sNetworkID, string sNotes, bool bLetter)
        {
            try
            {
                string sUserName = "";
                double cBalance = GetUserBalance(sUserId, sNetworkID, ref sUserName);
                double cNewBalance = cBalance + dAmount;
                string sql2 = "Insert into transactionlog (id,height,transactionid,username,userid,transactiontype,destination,amount,oldbalance,newbalance,added,updated,rake,networkid,notes)" + " values (newid(),'"
                    + nHeight.ToString() + "','"
                    + sTxId + "','" + sUserName + "','"
                    + sUserId + "','" + sBOUNTYNAME + "','" + sLetterID + "','"
                    + dAmount.ToString() + "','"
                    + cBalance.ToString()
                    + "','"
                    + cNewBalance.ToString()
                    + "',getdate(),getdate(),0,'" + sNetworkID + "','" + sNotes + "')";
                mPD.Exec2(sql2);
                //update balance now if the tx was inserted:
                sql2 = "Update Uz set balance" + sNetworkID + " = '" + cNewBalance.ToString()
                    + "' where id = '" + sUserId + "'";
                mPD.Exec2(sql2);
                if (bLetter)
                {
                    //Mark the record Paid
                    sql2 = "Update Letters set PAID=1 where id = '" + sLetterID + "'";
                    mPD.Exec2(sql2);
                    sql2 = "Insert into Letterwritingfees (id,height,added,amount,networkid,quantity) values (newid(),0,getdate(),'" + (-1 * dAmount).ToString() + "','" + sNetworkID + "',0)";
                    mPD.Exec2(sql2);
                }
                return cNewBalance;
            }
            catch (Exception ex)
            {
                Log("Unable to insert new AwardBounty in tx log : " + ex.Message);
                return 0;
            }
            return 0;
        }


        public static double GetScalarDouble(string sSql, string vCol, bool bLog = true)
        {
            DataTable dt1 = new DataTable();
            dt1 = mPD.GetDataTable2(sSql, bLog);
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

        public static string GetHealth(string sNetworkID)
        {
            string sql = "select value from system with (nolock) where systemKey='" + sNetworkID + "'";
            try
            {
                DataTable dt = default(DataTable);
                dt = mPD.GetDataTable2(sql, false);
                if (dt.Rows.Count > 0)
                {
                    string sOut = dt.Rows[0]["value"].ToString();
                    return sOut;
                }
            }
            catch (Exception)
            {
                return String.Empty;
            }
            return String.Empty;
        }

        public static List<string> MemorizePoolAddresses(string sNetworkID)
        {
            string sql = "Select * From PoolAddresses where network='" + sNetworkID + "'";
            DataTable dt = mPD.GetDataTable2(sql, false);
            List<string> a = new List<string>();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                a.Add(dt.Rows[i]["Address"].ToString());
            }
            return a;
        }

        public static void GetDifficultyList(string sNetworkID, int iLookback)
        {
            int nTipHeight = GetTipHeight(sNetworkID);
            

            for (int nHeight = (nTipHeight - iLookback); nHeight <= nTipHeight; nHeight++)
            {
                double dPOWDiff = GetDouble(GetShowBlock(sNetworkID, "showblock", nHeight, "difficulty"));
                double dABNWeight = GetDouble(GetShowBlock(sNetworkID, "showblock", nHeight, "anti-botnet-weight"));
                string sql = "Delete from difficulty where height = '" + nHeight.ToString() + "'";
                mPD.Exec2(sql, false, false);

                sql = "Insert into Difficulty (id,height,difficulty,added,network,powdifficulty,podcdifficulty,pogdifficulty,abnweight) values (newid(),'"
                    + nHeight.ToString() + "','" + dPOWDiff.ToString()
                    + "',getdate(),'" + sNetworkID + "','" + dPOWDiff.ToString() + "','" 
                    + dPOWDiff.ToString() + "','" + dPOWDiff.ToString() + "','" + dABNWeight.ToString() + "')";
                mPD.Exec2(sql, false, false);
            }
        }

        public static string GetShowBlock(string sNetworkid, string sCommand, int iBlockNumber, string sJSONFieldName)
        {
            if (true)
            {
                try
                {
                    BitnetClient oBit = GetBitNet(sNetworkid);
                    object[] oParams = new object[1];
                    oParams[0] = iBlockNumber.ToString();
                    dynamic oOut = oBit.InvokeMethod("getblock", oParams);
                    string sOut = oOut["result"][sJSONFieldName].ToString();
                    return sOut;
                }
                catch (Exception)
                {
                    return "";
                }
            }

            try
            {
                BitnetClient oBit = GetBitNet(sNetworkid);
                object[] oParams = new object[1];
                oParams[0] = iBlockNumber.ToString();
                dynamic oOut = oBit.InvokeMethod(sCommand, oParams);
                string sOut = oOut["result"][sJSONFieldName].ToString();
                return sOut;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static void AuditWithdrawals(string sNetworkID)
        {
            try
            {
                string sql10 = "Select Added from metrics";
                string a = mPD.GetScalarString2(sql10, "Added");
                var diffInSeconds = (System.DateTime.Now - Convert.ToDateTime(a)).TotalSeconds;
                if (diffInSeconds < (4 * 60 * 60)) return;
                double dAdjTime = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                BitnetClient b = GetBitNet(sNetworkID);
                object[] oParams = new object[2];
                oParams[0] = "*";
                oParams[1] = 988;
                dynamic oOut = b.InvokeMethod("listtransactions", oParams);
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
                        double dAmount = GetDouble(oOut["result"][y]["amount"]);
                        string sAddress = "";
                        string sCategory = oOut["result"][y]["category"].ToString();
                        double dTime = GetDouble(oOut["result"][y]["time"]);
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
                            DataTable rl = mPD.GetDataTable2(sql3);
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
                                mPD.Exec2(sql3);
                            }
                            else
                            {
                                // insert questionable record
                                string sql2 = "Insert into RequestLog (username,userguid,address,id,txid,amount,added,network,ip,audited,questionable) values ('"
                                        + "',null,'" + PurifySQL(sAddress, 80)
                                        + "',null,'" + PurifySQL(sTxid, 100)
                                        + "','" + dAmount.ToString()
                                        + "',getdate(),'" + sNetworkID + "',null,1,1)";
                                mPD.Exec2(sql2);
                            }
                            // Check the uzer balance
                            string sUserName = "";
                            double dBal = GetUserBalance(sUserGuid, sNetworkID, ref sUserName);
                            if (dBal <= .05)
                            {
                                sql3 = "Update RequestLog set Questionable=1 where TXID='" + PurifySQL(sTxid, 80)
                                    + "'";
                                mPD.Exec2(sql3);
                            }
                            // Adjust for UTC time
                            double dCutoff = 86400 + (60 * 60 * 4);
                            if (dWindow < dCutoff) dWalletDebit += dAmount;
                            if (dWindow > dCutoff) break;
                        }

                    }
                    else
                    {
                        break;
                    }
                }
                // Now grab the total mined in 24 hour,the total withdraws in 24 hour, and - the total withdraws from the wallet in 24 hour time period:
                string sql = "Select sum(Amount) a from TransactionLog where transactionType='mining_credit' and added > getdate()-1 and networkid = '" + VerifyNetworkID(sNetworkID) + "'";
                double dCredit = mPD.GetScalarDouble2(sql, "a");
                sql = "Select sum(Amount) a from TransactionLog where transactionType = 'withdrawal' and added > getdate()-1 and networkid='" + VerifyNetworkID(sNetworkID)
                    + "'";
                double dDebit = mPD.GetScalarDouble2(sql, "a");
                sql = "delete from Metrics where 1=1";
                mPD.Exec2(sql);
                sql = "Insert into Metrics (id,network,credits,debits,walletdebits,added) values (newid(),'" + VerifyNetworkID(sNetworkID)
                    + "','" + dCredit.ToString() + "','"
                    + dDebit.ToString() + "','" + dWalletDebit.ToString() + "',getdate())";
                mPD.Exec2(sql);
            }
            catch (Exception ex)
            {
                Log("Audit::" + ex.Message);
            }
        }

        public static void PayBlockParticipants(string sNetworkID)
        {
            // Command Timeout Expired:
            string sql3 = "exec payBlockParticipants '" + sNetworkID + "'";
            mPD.ExecWithTimeout2(sql3, 11000);
            return;
        }

        public static double GetUpvotedLetterCount()
        {
            string sql = "select isnull(count(*),0) as Ct from Letters where added > getdate()-60 and Upvote >= 10";
            double dct = GetDouble(mPD.ReadFirstRow2(sql, 0));
            return dct;
        }


        public static double GetMyLetterCount(string sUserGuid)
        {
            string sql = "select isnull(count(*),0) as Ct from Letters where added > getdate()-60 and Upvote >= 10 and Userid='" + sUserGuid + "'";
            double dct = GetDouble(mPD.ReadFirstRow2(sql, 0));
            return dct;
        }

        public static string Submit1(string sHex, string sNetworkID)
        {
            try
            {
                if (sHex == "") return "";

                object[] oParams = new object[1];
                oParams[0] = sHex;
                BitnetClient oBit = GetBitNet(sNetworkID);
                dynamic oOut = oBit.InvokeMethod("submitblock", oParams);
                string sResult = oOut["result"].ToString();
                return sResult;
            }
            catch (Exception ex)
            {
                Log("submitblock " + ex.Message.ToString());

            }
            return "";
        }

        public static string AnalyzeSolutionBlock(string sNetworkID, string sBlockHex, string sTxHex, ref string sBibleHash, ref string sBlockMessage,
            ref bool bABNPassed, ref double nABNWeight, ref string sError1)
        {
            try
            {
                if (sTxHex == "" || sBlockHex == "") return "";
                object[] oParams = new object[3];
                oParams[0] = "hexblocktocoinbase";
                oParams[1] = sBlockHex;
                oParams[2] = sTxHex;
                BitnetClient oBit = GetBitNet(sNetworkID);
                dynamic oOut = oBit.InvokeMethod("exec", oParams);
                sBibleHash = oOut["result"]["biblehash"].ToString();
                string sRecipient = oOut["result"]["recipient"].ToString();
                sBlockMessage = oOut["result"]["blockmessage"].ToString();
                nABNWeight = ToDouble(oOut["result"]["block_abn_weight"]);
                bABNPassed = oOut["result"]["abn_passed"] == "true";
                string sDebug = "exec hexblocktocoinbase " + sBlockHex + " " + sTxHex;

                string sCPID = ExtractXML(sBlockMessage, "<abnsig>", "</abnsig>").ToString();

                return sRecipient;
            }
            catch (Exception ex)
            {
                Log("AnalyzeSolutionBlock " + ex.Message.ToString());

            }
            return "";
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
                    oParams[2] = sBlockTime;
                    oParams[3] = sPrevBlockTime;
                    oParams[4] = sPrevHeight;
                    oParams[5] = sNonce;
                    BitnetClient oBit = GetBitNet(sNetworkID);
                    dynamic oOut = oBit.InvokeMethod("exec", oParams);
                    string sBibleHash = "";
                    sBibleHash = oOut["result"]["BibleHash"].ToString();
                    return sBibleHash;
                }
                catch (Exception ex)
                {
                    Log("getbiblehash forensics " + sNetworkID + ": "
                        + " run biblehash " + sBlockHash
                        + " " + sBlockTime
                        + " " + sPrevBlockTime
                        + " " + sPrevHeight
                        + " " + sNonce
                         + "\r\n" + ex.Message.ToString());
                    mBitnetUseCount += 1;
                }
            }
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
                sHex = GetGenericInfo2(sNetworkID, "walletpassphrase", oParSign, "");
            }
            catch (Exception ex)
            {
                Log("Unlock " + ex.Message);
            }
            if (sHex.Length > 1)
            {
                Log("Unlock Issue: " + sHex);
            }
            return "";
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
            for (int i = 0; i < p.Length; i++)
            {
                string sAmount = string.Format("{0:#.00}", p[i].Amount);
                string sRowOld = "\"" + p[i].Address + "\"" + ":" + sAmount;
                string sRow = "<RECIPIENT>" + p[i].Address + "</RECIPIENT><AMOUNT>" + sAmount + "</AMOUNT><ROW>";

                sPack += sRow;
            }

            string sXML = "<RECIPIENTS>" + sPack + "</RECIPIENTS>";
            GetWLA(sNetworkID, 30);

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

        public static bool ValidateBiblepayAddress(string sAddress, string sNetworkID)
        {
            try
            {
                object oValid = null;
                BitnetClient oBit = GetBitNet(sNetworkID);
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

        public static bool AdjustUserBalance(string networkid, string userguid, double amt)
        {
            string sql = "exec AdjustUserBalance '" + VerifyNetworkID(networkid)
                + "','" + amt.ToString() + "','" + userguid + "'";
            mPD.Exec2(sql);
            return true;
        }

        public static bool InsTxLog(string susername, string suserguid,
            string networkid, double iHeight, string sTXID, double amt, double oldBalance, double newBalance, string sDestination, string sTransactionType, string sNotes)
        {
            string sql = "exec InsTxLog '" + iHeight.ToString() + "','" + PurifySQL(sTXID, 125)
                + "','" + PurifySQL(susername, 100)
                + "','" + PurifySQL(suserguid, 100)
                + "','" + PurifySQL(sTransactionType, 100)
                + "','" + PurifySQL(sDestination, 125)
                + "','" + amt.ToString()
                + "','" + oldBalance.ToString()
                + "','" + newBalance.ToString() + "','"
                + VerifyNetworkID(networkid)
                + "','" + sNotes + "'";
            mPD.Exec2(sql);
            return true;
        }

        public static string VerifyNetworkID(string sNetworkID)
        {
            if (sNetworkID == "main")
            {
                return "main";
            }
            else
            {
                return "test";
            }
        }

        public static string PurifySQL9(string value, int maxlength)
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

        public static void InitRPC(string sNetworkID)
        {
            mBitnetUseCount += 1;
            BitnetClient oBit;
            if (sNetworkID.ToLower() == "main")
            {
                oBit = mBitnetMain;
            }
            else
            {
                oBit = mBitnetTest;
            }

            try
            {
                if (oBit == null || mBitnetUseCount < 3)
                {
                    oBit = new BitnetClient(USGDFramework.clsStaticHelper.GetConfig("RPCURL" + sNetworkID));
                    string sPass = USGDFramework.clsStaticHelper.GetConfig("RPCPass" + sNetworkID + "_E");
                    NetworkCredential Cr = new NetworkCredential(USGDFramework.clsStaticHelper.GetConfig("RPCUser" + sNetworkID), sPass);
                    oBit.Credentials = Cr;
                    if (sNetworkID.ToLower() == "main")
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
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
        private static string Clean(string sKey, string sData)
        {
            sData = sData.Replace(sKey, "");
            sData = sData.Replace("\r\n", "");
            sData = sData.Replace(":", "");
            sData = sData.Replace(",", "");
            sData = sData.Replace("\"", "");
            return sData.Trim();

        }
        public static string GetCameroon()
        {
            object[] oParams = new object[1];
            oParams[0] = "all";
            BitnetClient bc = GetBitNet("main");
            dynamic oOut = bc.InvokeMethod("listchildren", oParams);
            dynamic oResult = oOut["result"];
            string d = ",";
            string sOut = "Child ID,CPK,Bio URL,Balance\r\n";
            string sData = oOut.ToString();
            string[] vData = sData.Split(new string[] {"Child ID"}, StringSplitOptions.None);
            for (int i = 0; i < vData.Length; i++)
            {
                string[] vChild = vData[i].Split(new string[] { "\r\n" }, StringSplitOptions.None);
                if (vChild.Length > 5)
                {
                    string sID = Clean("Child", "Child" + vChild[0]);

                    string sCPK = Clean("CPK", vChild[1]);
                    string sBio = Clean("Biography", vChild[2]);
                    string sBalance = Clean("Balance", vChild[3]);

                    if (sID.Length > 3 && sBalance.Length > 1)
                    {

                        sOut += sID + d + sCPK + d + sBio + d + sBalance + d + "\r\n";
                    }
                }

            }
            return sOut;

        }

        public static string GetDatalist(string sType)
        {
            object[] oParams = new object[3];

            oParams[0] = "datalist";
            oParams[1] = sType;
            oParams[2] = "99999";

            BitnetClient bc = GetBitNet("main");
            JObject jOut = bc.InvokeMethod("exec", oParams);
            dynamic oResult = jOut["result"];
            string sOut = "";
            string myData = oResult.ToString();
            string[] firstData = myData.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            for (int i = 0; i < firstData.Length; i++)
            {
                string sData = firstData[i].ToString();
                string[] vData = sData.Split(new string[] { "|" }, StringSplitOptions.None);
                if (vData.Length > 5)
                {
                    string sCPK = ExtractXML(vData[0]+"|", ": \"", "|").ToString();
                    string sNickName = vData[1];
                    string sTimeStamp = vData[2];
                    string sOptData = vData[5];
                    string sRow = "CPK," + sCPK + ",NickName," + sNickName + ",OptData," + sOptData + "<ROW>";
                    sOut += sRow;
                }

            }
            return sOut;
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
                Log("GetNonceInfo: " + ex.Message);
                return 0;
            }
        }
        
        public static double GetDouble(object o)
        {
            if (o == null) return 0;
            if (o.ToString() == "") return 0;
            double d = Convert.ToDouble(o.ToString());
            return d;
        }
        
        public static double RetrieveCPIDRAC(string sCPID, string sFileName)
        {
            string sPath = "ftproot\\biblepay\\" + sFileName;
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
                        if (sCPID.ToLower() == sDataCPID.ToLower())
                        {
                            return dRac;
                        }
                    }

                }
            }
            return -1;
        }
        
        public static Zinc.BiblePayMouse.MouseOutput GetCachedCryptoPrice(string sSymbol)
        {
            string sql = "Select value from System where systemkey = 'PRICE_BBP/BTC'";
            Zinc.BiblePayMouse.MouseOutput m1 = new Zinc.BiblePayMouse.MouseOutput();
            m1.BBPSatoshi = Math.Round(Convert.ToDouble(GetScalarDouble2(sql, "value")), 12);

            sql = "Select value from System where systemkey = 'PRICE_BTC/USD'";
            m1.BTCPrice = Convert.ToDouble(GetScalarDouble2(sql, "value"));

            m1.BBPPrice = m1.BTCPrice * m1.BBPSatoshi;
            return m1;
        }

        public static byte[] StrToByteArray(string str)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            return encoding.GetBytes(str);
        }

        public static BitnetClient GetBitNet(string sNetworkID)
        {

            InitRPC(sNetworkID);
            if (sNetworkID.ToLower() == "main")
            {
                return mBitnetMain;
            }
            else
            {
                return mBitnetTest;
            }
        }

        public static double GetSystemDouble(string sKey)
        {
            string sql = "Update System Set Value=Value+1 where systemKey='" + PurifySQL(sKey, 25) + "'";
            mPD.Exec2(sql);
            sql = "Select Value from System where systemKey='" + sKey + "'";
            double d1 = GetScalarDouble2(sql, "Value");
            return d1;
        }

        public static int GetTipHeight(string snetworkid)
        {
            try
            {
                BitnetClient oBit = GetBitNet(snetworkid);
                int iTipHeight = oBit.GetBlockCount();
                if (snetworkid == "main" && iTipHeight > 0) nCurrentTipHeightMain = iTipHeight;
                return iTipHeight;
            }
            catch (Exception)
            {
                return -1;
            }
        }
        

        
        
        
        public static void GetOSInfo(string sOS, ref string sNarr, ref double dMinerCount, ref double dAvgHPS, ref double dTotal)
        {
            sOS = PurifySQL(sOS, 50);
            string sql = "Select count(distinct minername) ct from WORK with (nolock) where OS is not null";
            dTotal = mPD.GetScalarDouble2(sql, "ct", false);
            sql = "Select count(distinct minername) ct,os from work with (nolock) where os is not null  AND OS='" + sOS + "' group by os";
            dMinerCount = mPD.GetScalarDouble2(sql, "ct", false);
            sql = "Select avg(boxhps) avgboxhps,os from Work with (nolock) where os is not null AND OS='" + sOS + "' group by OS";
            dAvgHPS = Math.Round(mPD.GetScalarDouble2(sql, "avgboxhps", false), 2);
            double dPercent = Math.Round(dMinerCount / (dTotal + .01), 2) * 100;
            sNarr = "Miner Count: " + dMinerCount.ToString() + ", Avg HPS: " + dAvgHPS.ToString() + ", Usage: " + dPercent.ToString() + "%";
        }

        public static double GetDoubleVersion(string sNetworkID)
        {
            try
            {
                string sVer = GetInfoVersion(sNetworkID);
                sVer = sVer.Replace(".", "");
                double dVer = ToDouble(sVer);
                return dVer;
            }
            catch(Exception x)
            {
                return 0;
            }
        }

        public static string GetInfoVersion(string sNetworkID)
        {
            return VersionToString(GetGenericInfo(sNetworkID, "getinfo", "version", "result"));
        }

        public static string VersionToString(string sVersion)
        {
            // Version is Formatted with two alpha characters per biblepay version octet
            if (sVersion.Length < 7) return "";
            string sOut = sVersion.Substring(0, 1) + sVersion.Substring(2, 1) + sVersion.Substring(4, 1) + sVersion.Substring(6, 1);
            return sOut;
        }
        
        public static string GetBlockTx(string sNetworkid, string sCommand, int iBlockNumber, int iPositionId)
        {
            try
            {
                BitnetClient oBit = GetBitNet(sNetworkid);
                object[] oParams = new object[1];
                oParams[0] = iBlockNumber.ToString();
                dynamic oOut = oBit.InvokeMethod(sCommand, oParams);
                string sOut = oOut["result"]["tx"][iPositionId].ToString();
                return sOut;
            }
            catch (Exception)
            {
                return "";
            }
        }
                
        public static void ScanForAmounts(string sNetworkID)
        {
            string sql = "Select * from Deposit where Amount is null";
            DataTable d1 = mPD.GetDataTable2(sql, false);
            BitnetClient b = GetBitNet(sNetworkID);
            string sOut = "";
            for (int i = 0; i < d1.Rows.Count; i++)
            {
                string id = d1.Rows[i]["id"].ToString();
                string address = d1.Rows[i]["Address"].ToString();
                string sTxId = d1.Rows[i]["txid"].ToString();

                string sRawTx = GetRawTransaction(sNetworkID, sTxId);
                int nHeight = 0;

                double amt = GetAmtFromRawTx(sRawTx, address, out nHeight);
                if (amt > 0)
                {
                    sql = "Update Deposit set Amount = '" + amt.ToString() + "',height='" + nHeight.ToString() + "' where id = '" + id + "'";
                    mPD.Exec2(sql, false, true);
                }
            }
            CreditUsersForDeposits(sNetworkID);
        }

        public static void CreditUsersForDeposits(string sNetworkID)
        {
            string sql = "Select value from System where systemkey='emptyhotwallet' ";
            string saddress = mPD.GetScalarString2(sql, "value", false);
            int nTipHeight = GetTipHeight(sNetworkID);
            sql = "Select * from Deposit where Amount is not null and credited is null and DATEADD(minute, -10, getdate()) > added and " + nTipHeight.ToString() + " > Height+3";
            DataTable d1 = mPD.GetDataTable2(sql, false);
            BitnetClient b = GetBitNet(sNetworkID);
            for (int i = 0; i < d1.Rows.Count; i++)
            {
                string id = d1.Rows[i]["id"].ToString();
                string address = d1.Rows[i]["Address"].ToString();
                string sTxId = d1.Rows[i]["txid"].ToString();
                double oldamt = GetDouble(d1.Rows[i]["amount"]);
                string sUserId = d1.Rows[i]["userid"].ToString();
                int nHeight = (int)GetDouble(d1.Rows[i]["height"]);

                string sRawTx = GetRawTransaction(sNetworkID, sTxId);
                int nHeight2 = 0;

                double amt = GetAmtFromRawTx(sRawTx, address, out nHeight2);
                if (Math.Round(amt,0)  == Math.Round(oldamt,0) && amt > 0)
                {
                    sql = "Update Deposit set Credited = getdate() where id = '" + id + "'";
                    mPD.Exec2(sql, false, true);
                    // Credit the user
                    AwardBounty(sTxId, sUserId, amt, "DEPOSIT", nHeight2, sTxId, sNetworkID, "DEPOSIT " + sTxId, false);

                    if (!saddress.StartsWith("x") && saddress != "")
                    {
                        // empty the hot wallet by sending the money
                        EmptyHot(saddress, amt, sNetworkID);

                    }
                }
            }
        }

        public static double GetAmtFromRawTx(string sRaw, string sAddress, out int nHeight)
        {
            string[] vData = sRaw.Split(new string[] { "|" }, StringSplitOptions.None);
            for (int i = 0; i < vData.Length; i++)
            {
                string d = vData[i];
                if (d.Length > 1)
                {
                    string[] vRow = d.Split(new string[] { "," }, StringSplitOptions.None);
                    if (vRow.Length > 1)
                    {
                        string sAddr = vRow[1];
                        string sAmt = vRow[0];
                        string sHeight = vRow[2];
                        nHeight = (int)GetDouble(sHeight);

                        if (sAddr == sAddress && nHeight > 0)
                        {
                            return Convert.ToDouble(sAmt);
                        }

                    }
                }
            }
            nHeight = 0;
            return 0;
        }


        public static string GetDepositTXIDList(string sNetworkID)
        {
            string sql = "Select distinct id,receiveaddress from UZ where receiveaddress is not null";
            DataTable d1 = mPD.GetDataTable2(sql, false);
            BitnetClient b = GetBitNet(sNetworkID);
            string sOut = "";
            for (int i = 0; i < d1.Rows.Count; i++)
            {
                string address = d1.Rows[i]["receiveaddress"].ToString();
                string sUserId = d1.Rows[i]["id"].ToString();
                JObject jOut = b.InvokeMethod("getaddresstxids", address);
                dynamic o = jOut["result"];
                for (int j = 0; j < o.Count; j++)
                {
                    string sTxId = o[j].ToString();
                    sql = " IF NOT EXISTS (SELECT TXID FROM Deposit WHERE deposit.txid='"
                        + sTxId + "') BEGIN \r\n INSERT INTO Deposit (id,address,txid,userid,added) values (newid(),'"
                        + address + "','" + sTxId + "','" + sUserId + "',getdate()) END";

                    mPD.Exec2(sql, false, true);
                }
            }
            ScanForAmounts(sNetworkID);

            return sOut;
        }

        //Scan for the credit amount
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
                    catch (Exception ey)
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
                        string height = oOut["result"]["height"].ToString();

                        sOut += sAmount + "," + sAddress + "," + height + "|";
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
                Log(ex.Message);
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

        public static string GetSuperblockBudget(string sNetworkid,int nHeight)
        {
            try
            {
                BitnetClient bc = GetBitNet(sNetworkid);
                object[] oParams = new object[1];
                oParams[0] = nHeight;
                dynamic oOut = bc.InvokeMethod("getsuperblockbudget",oParams);
                string sOut = oOut["result"].ToString();
                return sOut;
            }
            catch (Exception ex)
            {
                string sTest = ex.Message;

                return "";
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
            catch (Exception ex)
            {
                string sTest = ex.Message;

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

        public static double GetCachedQuote(string ticker, out int age)
        {
            string sql = "Select updated,Value from System where systemkey='PRICE_" + ticker + "'";
            DataTable dt = mPD.GetDataTable2(sql, false);
            if (dt.Rows.Count < 1)
            {
                age = 0;
                return 0;
            }
            double d1 = GetDouble(dt.Rows[0]["Value"]);
            string s1 = dt.Rows[0]["Updated"].ToString();
            age = (int)Math.Abs(DateAndTime.DateDiff(DateInterval.Second, DateTime.Now, Convert.ToDateTime(s1)));
            return d1;

        }

        public static void CacheQuote(string ticker, string sPrice)
        {
            string sql = "Delete from System where SystemKey = 'PRICE_" + ticker + "'";
            mPD.Exec2(sql);
            sql = "Insert into System (id,systemkey,value,updated) values (newid(),'PRICE_" + ticker + "','" + sPrice + "',getdate())";
            mPD.Exec2(sql);
        }

        public static double GetPriceQuote(string ticker)
        {
            int age = 0;
            double dCachedQuote = GetCachedQuote(ticker, out age);
            if (dCachedQuote > 0 && age < (60 * 60 * 16))
                return dCachedQuote;

            string sURL = "https://www.southxchange.com/api/price/" + ticker;
            string sData = "";

            try
            {
                MyWebClient w = new MyWebClient();
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                sData = w.DownloadString(sURL);
            }
            catch (Exception ex)
            {
                Log("BAD PRICE ERROR" + ex.Message);

            }
            string bid = ExtractXML(sData, "Bid\":", ",").ToString();
            string ask = ExtractXML(sData, "Ask\":", ",").ToString();
            double dbid = ToDouble(bid);
            double dask = ToDouble(ask);
            double dmid = (dbid + dask ) / 2;
            if (dmid > 0)
            {
                CacheQuote(ticker, dmid.ToString("0." + new string('#', 339)));
            }
            else
            {
                return dCachedQuote;
            }
            return dmid;
        }

        public static string DataDump(string sSQL, string sIgnoreFields)
        {
            DataTable dt = mPD.GetDataTable2(sSQL);
            string sOut = "";
            string sHeading = "";
            for (int y = 0; y < dt.Columns.Count; y++)
            {

                bool bAllowed = InList(sIgnoreFields, dt.Columns[y].ColumnName);
                if (!bAllowed)
                {
                    sHeading += dt.Columns[y].ColumnName + "|";
                }

            }
            sOut += sHeading + "<ROW>\r\n";

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string sValue = "";

                for (int y = 0; y < dt.Columns.Count; y++)
                {

                    bool bAllowed = InList(sIgnoreFields, dt.Columns[y].ColumnName);
                    if (!bAllowed)
                    {
                        sValue += dt.Rows[i][y].ToString() + "|";
                    }
                }
                sOut += sValue + "<ROW>\r\n";
            }
            return sOut;
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
                Log("GET_BLOCK_INFO: " + ex.Message.ToString());
            }
            return "";
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

            // NOTE, since ZINC API is retired, lets bail out of this for now;
            return;

            // for any order that is ours, that is not complete
            string sql = "Select * from Orders where Status3 is null";
            DataTable dt = mPD.GetDataTable2(sql);
            string sToken = USGDFramework.clsStaticHelper.GetConfig("MouseToken");
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
                        string sql3 = "Update orders set Status3='" + PurifySQL(mo.tracking, 25)
                            + "' where id = '" + GuidOnly(sOrderGuid)
                            + "'";
                        mPD.Exec2(sql3);
                    }
                }
                if (mo.code.Length > 1)
                {
                    string sql2 = "Update orders set Status1 = '" + PurifySQL(Left(mo.code, 20), 35)  + "' where id = '" + GuidOnly(sOrderGuid) + "'";
                    mPD.Exec2(sql2);
                }
                if (mo.message != null)
                {
                    if (mo.message.Length > 1)
                    {
                        string sStatus = MsgToStatus(mo.message);
                        string sql2 = "Update orders set Updated=getdate(),Status1='PLACED',Status2 = '" + PurifySQL(sStatus, 255)
                            + "' where id = '" + GuidOnly(sOrderGuid) + "'";
                        mPD.Exec2(sql2);
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
            if (data.Contains("Validation failed"))
            {
                sOut = "VALIDATION FAILED";
            }
            return sOut;
        }

        public static bool GetUserBalances(string sNetworkID, string userguid, ref double dBalance, ref double dImmature)
        {
            if (userguid == "") return false;

            string sql = "Select isnull(Balance" + VerifyNetworkID(sNetworkID)
                + ",0) as bal1 From Uz with (nolock) where id='" + GuidOnly(userguid)
                + "' and deleted=0";
            dBalance = mPD.GetScalarDouble2(sql, "bal1");
            sql = "Select isnull(sum(amount),0) As Immature from transactionlog where updated > getdate()-1 And userid='" + GuidOnly(userguid)
                + "' And networkid = '" + VerifyNetworkID(sNetworkID)
                    + "' and transactiontype in ('LETTER_WRITING','MINING_CREDIT') ";
            dImmature = mPD.GetScalarDouble2(sql, "Immature", false);
            return true;
        }

        public static double GetTotalLetterBountiesPaid(int iSuffixType)
        {
            string sSuffix = iSuffixType == 0 ? "> 0" : "< 0";

            string sql = "select sum(amount) from letterwritingfees where amount " + sSuffix;

            double d = Conversion.Val(mPD.ReadFirstRow2(sql, 0));
            return Math.Abs(Math.Round(d, 2));
        }

        public static double PriceInBBP(double USD)
        {
            double dCost = USD + 1 + 1; // 1$ for zinc, 1$ for handling
            double dMarkup = dCost * .100;
            double dTotal = dMarkup + dCost;
            Zinc.BiblePayMouse.MouseOutput m = GetCachedCryptoPrice("bbp");
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
                Log(" ensure bbp uer exist bad uer " + BBPAddress);
                return false;
            }
            string sql = "Select count(*) ct from Uz where UserName='" + PurifySQL(BBPAddress, 100) + "'";
            double nUserCount = GetScalarDouble2(sql, "ct");
            if (nUserCount == 0)
            {
                // Add the user
                string sOrg = "CDE6C938-9030-4BB1-8DFE-37FC20ABE1A0";
                sql = "Insert into Uz (id,username,password,Email,updated,added,deleted,organization) values (newid(),@Username,'[txtpass]','"
                    + Guid.NewGuid().ToString() + "',getdate(),getdate(),0,'" + sOrg + "')";
                sql = sql.Replace("@Username", "'" + PurifySQL(BBPAddress, 100)
                    + "'");
                sql = sql.Replace("[txtpass]", USGDFramework.modCryptography.SHA256(Guid.NewGuid().ToString().Substring(0, 5)));
                Log(sql);
                mPD.Exec2(sql);
                return true;
            }
            return true;
        }

        public static Zinc.BiblePayMouse.payment_method TransferPaymentMethodIntoMouse()
        {
            Zinc.BiblePayMouse.payment_method pm1 = new Zinc.BiblePayMouse.payment_method();
            pm1.expiration_month = (int)Convert.ToDouble(USGDFramework.clsStaticHelper.GetConfig("mouseexpirationmonth"));
            pm1.expiration_year = (int)Convert.ToDouble(USGDFramework.clsStaticHelper.GetConfig("mouseexpirationyear"));
            pm1.name_on_card = USGDFramework.clsStaticHelper.GetConfig("mousenameoncard");
            pm1.number = USGDFramework.clsStaticHelper.GetConfig("mousecardnumber_E");
            pm1.security_code = USGDFramework.clsStaticHelper.GetConfig("mousesecuritycode");
            pm1.use_gift = false;
            return pm1;
        }

        public static Zinc.BiblePayMouse.billing_address TransferBillingAddressIntoMouse()
        {
            Zinc.BiblePayMouse.billing_address ba1 = new Zinc.BiblePayMouse.billing_address();
            ba1.address_line1 = USGDFramework.clsStaticHelper.GetConfig("mousebillingaddressline1");
            ba1.city = USGDFramework.clsStaticHelper.GetConfig("mousebillingcity");
            ba1.state = USGDFramework.clsStaticHelper.GetConfig("mousebillingstate");
            ba1.zip_code = USGDFramework.clsStaticHelper.GetConfig("mousebillingzipcode");
            ba1.last_name = USGDFramework.clsStaticHelper.GetConfig("mousebillinglastname");
            ba1.first_name = USGDFramework.clsStaticHelper.GetConfig("mousebillingfirstname");
            ba1.country = USGDFramework.clsStaticHelper.GetConfig("mousebillingcountry");
            ba1.phone_number = USGDFramework.clsStaticHelper.GetConfig("mousebillingphonenumber");
            return ba1;
        }

        public static Zinc.BiblePayMouse.retailer_credentials TransferRetailerCredentialsIntoMouse()
        {
            Zinc.BiblePayMouse.retailer_credentials rc1 = new Zinc.BiblePayMouse.retailer_credentials();
            rc1.email = USGDFramework.clsStaticHelper.GetConfig("mouseazretailemail");
            rc1.password = USGDFramework.clsStaticHelper.GetConfig("mouseazretailpass_E");
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
            string sql = "Select * from Uz where UserName='" + PurifySQL(BBPAddress, 125) + "'";
            DataTable dt = mPD.GetDataTable2(sql);
            if (dt.Rows.Count > 0)
            {
                return dt.Rows[0]["id"].ToString();
            }
            return "";
        }

        public static string GetWL(string sNetworkID)
        {
            string sHex = GetGenericInfo0(sNetworkID, "walletlock", "result", "result");

            return "";
        }

        public static string GuidOnly(string sGuid)
        {
            try
            {
                Guid g = new Guid(sGuid);
                return g.ToString();
            }
            catch (Exception)
            {
                return "";
            }
        }
        

        public static string PurifySQL2(string value)
        {
            string sTemp = value.ToUpper();
            sTemp = sTemp.Replace(" ", "");
            bool bDirty = sTemp.Contains("INSERT") || sTemp.Contains("UPDATE") || sTemp.Contains("DELETE") || sTemp.Contains("DROPTABLE");
            if (bDirty) return "";
            return value;
        }

        public static string PurifySQL(string value, double maxlength)
        {
            if (Strings.InStr(1, value, "'") > 0)
                value = "";
            if (Strings.InStr(1, value, "--") > 0)
                value = "";
            if (Strings.InStr(1, value, "/*") > 0)
                value = "";
            if (Strings.InStr(1, value, "*/") > 0)
                value = "";
            if (Strings.InStr(1, Strings.LCase(value), "xp_") > 0)
                value = "";
            if (Strings.InStr(1, value, ";") > 0)
                value = "";
            if (Strings.InStr(1, Strings.LCase(value), "drop ") > 0)
                value = "";
            if (Strings.Len(value) > maxlength)
                value = "";
            return value;
        }

        public static string A1(string reqLogId, string sAddress, double cAmt, string sNetworkID, string sIP)
        {
            object[] oParams = new object[2];
            oParams[0] = sAddress;
            oParams[1] = cAmt.ToString();
            BitnetClient bc = GetBitNet(sNetworkID);
            string sTxId = "";
            if (reqLogId == "")
            {
                throw new Exception("Unknown ReqLogId.");
            }
            string sql = "Select * From RequestLog where id = '" + GuidOnly(reqLogId) + "'";
            DataTable dtRQ = mPD.GetDataTable2(sql);
            string sName = "";
            string sUserGuid = "";
            if (cAmt > 40000) return "";
            if (dtRQ.Rows.Count > 0)
            {
                double da2 = GetDouble(dtRQ.Rows[0]["Amount"]);
                sUserGuid = dtRQ.Rows[0]["userguid"].ToString();
                sName = dtRQ.Rows[0]["username"].ToString();
                sql = "update uz set withdraws = isnull(withdraws, 0) + 1 where id='" + sUserGuid + "'";
                mPD.Exec2(sql);
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
               // string sIP = (HttpContext.Current.Request.UserHostAddress ?? "").ToString();
                sql = "Insert into SentMoney (username,userguid,address,id,txid,amount,added,network,ip,requestLogId) values ('"
                    + sName + "','" + GuidOnly(sUserGuid)
                    + "','" + PurifySQL(sAddress, 50)
                    + "',newid(),'"  + PurifySQL(sTxId, 50)
                    + "','" + cAmt.ToString() + "',getdate(),'" + VerifyNetworkID(sNetworkID)
                    + "','" + PurifySQL(sIP, 50)
                    + "','" + GuidOnly(reqLogId)
                    + "')";
                mPD.Exec2(sql);
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
            return sTxId;
        }
        
        public static string EmptyHot(string sAddress, double cAmt, string sNetworkID)
        {
            try
            {
                object[] oParams = new object[2];
                oParams[0] = sAddress;
                oParams[1] = cAmt.ToString();
                BitnetClient bc = GetBitNet(sNetworkID);
                string sTxId = "";
                GetWLA(sNetworkID, 45);
                dynamic oOut = bc.InvokeMethod("sendtoaddress", oParams);
                sTxId = oOut["result"].ToString();
                GetWL(sNetworkID);
                return sTxId;
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
            return "";
        }

        public static void BatchExec(string sSQL, bool bRunNow, bool bLog = true)
        {
            try
            {
                lock (myLock)
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
                            mPD.Exec2(sRead, bLog);
                        }
                    }
                }
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

        public static double GetWebRAC(string sCPID)
        {
            string sURL = "https://boinc.netsoft-online.com/e107_plugins/boinc/get_user.php?cpid=" + sCPID + "&format=xml";
            MyWebClient w = new MyWebClient();
            string sResult = w.DownloadString(sURL);
            string sRAC = ExtractXML(sResult, "Average Credit:</td>", "</tr>").ToString();
            string sRAC2 = ExtractXML(sRAC, ">", "</td>").ToString();
            sRAC2 = sRAC2.Replace(",", "");
            double dRAC = Convert.ToDouble(sRAC2);
            return dRAC;
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
                Shared.Log("Invald format " + ex.Message + "," + o.ToString());
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
