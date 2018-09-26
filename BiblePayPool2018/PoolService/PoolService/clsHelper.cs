using Bitnet.Client;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PoolService
{
    static class clsStaticHelper
    {
        public static USGDFramework.Data mPD = new USGDFramework.Data();
        public static object myLock = "A";
        public static BitnetClient mBitnetMain;
        public static BitnetClient mBitnetTest;
        public static double nCurrentTipHeightMain = 0;
        public static double mBitnetUseCount = 0;
        public static DateTime dtLastMetricRefresh = DateTime.Now;

        public static DateTime dtMyAnchorTime = DateTime.Now;


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
            t=t.AddHours(6);
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
                    clsStaticHelper.Log("SendEmail: We encountered a problem while sending the outbound e-mail to " + strTo.ToString() + ", port " + smtp.Port.ToString() + ", host " + smtp.Host + ", Email error: " + ex.Message);
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
                        clsStaticHelper.mPD.ExecResilient2(sRead,false);
                    }
                }
            }
            catch (Exception ex)
            {
                string sMsg = ex.Message;
            }
        }

        public static double ElapsedSinceLastRefresh()
        {

            double dElapsedSecs = (DateTime.Now - dtLastMetricRefresh).TotalSeconds;
            
            return dElapsedSecs;

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
            double dct = GetDouble(clsStaticHelper.mPD.ReadFirstRow2(sql, 0));
            return dct;
        }


        public static double GetTotalBounty()
        {
            string sql = "select isnull(sum(amount),0) as amount from LetterWritingFees";
            double d = GetDouble(clsStaticHelper.mPD.ReadFirstRow2(sql, 0));
            return d;
        }


        public static void RewardUpvotedLetterWriters()
        {
            //Pay out any funds marked as unpaid and add a transaction to transactionLog
            string sql = "select * from letters where paid <> 1 and upvote >= 10";
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
                string sql = "Select isnull(Balance" + sNetworkID + ",0) as balance,USERNAME from Users where id = '" + sUserId + "'";
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
                clsStaticHelper.Log("unable to get user balance for user " + sUserId);
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
                sql2 = "Update Users set balance" + sNetworkID + " = '" + cNewBalance.ToString()
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
                clsStaticHelper.Log("Unable to insert new AwardBounty in tx log : " + ex.Message);
                return 0;
            }
            return 0;
        }

        public static void Housecleaning(string sNetworkID)
        {
               
        }

        public static List<string> MemorizeNonces(string sNetworkID)
        {
            string sql = "Update Work set Work.Userid = (Select Miners.Userid from Miners where Miners.id = Work.Minerid) where Work.UserId is null";
            mPD.Exec2(sql);
            sql = "Select distinct userid,nonce from Work where endtime is not null and validated=1 and error is null and endtime > getdate()-1/24.01/60";
            DataTable dt = mPD.GetDataTable2(sql);
            List<string> nonces = new List<string>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                double dNonce = GetDouble(dt.Rows[i]["nonce"]);
                string sUserId = dt.Rows[i]["UserId"].ToString();
                string sTotal = sUserId + dNonce.ToString();
                nonces.Add(sTotal);
            }
            return nonces;
        }

        public static void WarnAdopters(string sNetworkID, int iThreshhold)
        {
            // This sends out an email warning anyone who has not written for 30 days to write to child.  If after 30 days we must force the child to be abandoned.
            string sql = "select * from users where letterdeadline < getdate()-" + iThreshhold.ToString() + " and adoptedorphanid is not null and (select count(*) from Letters inner join Orphans on Orphans.orphanid = Letters.orphanid where letters.userid = users.id and letters.added > getdate()-" 
                + iThreshhold.ToString() + " and len(body) > 100 and orphans.id = users.AdoptedOrphanId)= 0";

            DataTable dt = clsStaticHelper.mPD.GetDataTable2(sql);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string sUserId = dt.Rows[i]["id"].ToString();
                string sOrphanId = dt.Rows[i]["AdoptedOrphanId"].ToString();
                string s1 = "Select Name from Orphans where id = '" + sOrphanId + "'";
                string sName = clsStaticHelper.mPD.GetScalarString2(s1, "Name", false);
                string sUserName = dt.Rows[i]["username"].ToString();
                string sEmail = dt.Rows[i]["Email"].ToString();
                string sBody = "Dear " + sUserName 
                    + ",<br><br>The BiblePay Adoption System is notifying you that you have not written to " 
                    + sName + " in over "  + iThreshhold.ToString() + " day(s).  <br>Please consider writing to your child today.<br>"
                    + "<br>After 60 days, BiblePay will be forced to abandon the childs relationship from your account which could be dissapointing for a child.<br><br><br>Thank you for using BiblePay.<br><br>Best Regards,<br>BiblePay Support";

                bool sent = clsStaticHelper.SendEmail(sEmail, "BiblePay Adoption System", sBody, true, true);
            }
        }

        public static void TitheExtraBalances(string sNetworkID)
        {
            string sql = "Select id,balancemain from users where id not in (select distinct userid from transactionLog (nolock) where added > getdate()-90 and transactiontype <> 'ORPHAN_TITHE') and balancemain > 1000 ";
            DataTable dt = clsStaticHelper.mPD.GetDataTable2(sql);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string sLetterId = Guid.NewGuid().ToString();
                string sId = dt.Rows[i]["id"].ToString();
                double dDockAmount = -1 * (GetDouble(dt.Rows[i]["balancemain"]) * .02);
                AwardBounty(sLetterId,sId, dDockAmount, "ORPHAN_TITHE", 0, sLetterId, "main", "ORPHAN_TITHE " + sLetterId, false);
                
            }
        }


        public static void VerifySolutions(string sNetworkID)
        {
            string sql = "SELECT * from WORK (nolock) where Validated is null and endtime is not null and networkid='" 
                + sNetworkID + "'";
            DataTable dt = clsStaticHelper.mPD.GetDataTable2(sql);
            int nLowTip = 1;
            int nLowTipThreshhold = 4;
            nLowTip = clsStaticHelper.GetTipHeight(sNetworkID);
            int nHighTip = nLowTip + nLowTipThreshhold;
            double nMaxNonce = clsStaticHelper.GetNonceInfo(sNetworkID);
            List<string> sNonces = MemorizeNonces(sNetworkID);
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
                    string sTxHex = dt.Rows[i]["SolutionTxHash"].ToString();

                    string sNonce = "0";
                    if (vSolution.Length > 13) sNonce = vSolution[13];
                    double dNonce = clsStaticHelper.GetDouble(sNonce);
                    string sID = dt.Rows[i]["id"].ToString();
                    string sTotal = sUserId + dNonce.ToString();
                    string sMinername = dt.Rows[i]["minername"].ToString();

                    int nTheirTip = Convert.ToInt32(sPrevHeight);
                    string sError = "";
                    if ((nTheirTip < (nLowTip - nLowTipThreshhold)) || (nTheirTip > nHighTip))
                    {
                        sError = "BLOCK_IS_STALE"; // <RESPONSE>BLOCK_IS_STALE</RESPONSE><ERROR>BLOCK_IS_STALE</ERROR><EOF></HTML>
                    }
                    //Verify solution matches
                    string sBibleHash2 = "";
                    string sBlockMessage = "";
                    bool bSigValid = false;
                    string sError5 = "";
                    string sRecipient = AnalyzeSolutionBlock(sNetworkID, sBlockHex, sTxHex, ref sBibleHash2, ref sBlockMessage, 
                        ref bSigValid, ref sError5);
                    if (sBlockHex.Length < 5)
                    {
                        sError = "INVALID_BLOCK_HEX_PLEASE_UPGRADE ";
                    }
                    

                    string sCPIDSig = ExtractXML(sBlockMessage, "<cpidsig>", "</cpidsig>").ToString();
                    if (sCPIDSig.Length < 5)
                    {
                        sError = "INVALID_CPID_SIGNATURE";
                    }

                    if (!bSigValid)
                    {
                        if (sError5 != "")
                        {
                            sError = sError5;
                        }
                        else
                        {
                            sError = "ILLEGAL_SOLUTION_NO_MAGNITUDE";
                        }
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
                    if (dNonce < 1 && nLowTip > 23000)
                    {
                        sError = "BLOCK_SOLUTION_INCOMPLETE";
                    }

                    if (sNonces.Contains(sTotal))        
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
                        double nHeight = clsStaticHelper.GetDouble(sPrevHeight) + 1;
                        if (sError == "")
                        {
                            string sql2 = "Update Work Set Solution2='', Validated=1 WHERE id = '" + sID + "'";
                            clsStaticHelper.mPD.Exec2(sql2, false, false);
                        }
                        else
                        {
                            string sql2 = "Update Work Set Error='" + sError + "',Validated=1,EndTime=null where id = '" + sID + "'";
                            clsStaticHelper.mPD.ExecResilient2(sql2,false);
                        }
                    }
                    catch (Exception ex2)
                    {
                        clsStaticHelper.Log("Encountered Error while Verifying " + ex2.Message);
                    }
                    System.Threading.Thread.Sleep(7);

                }
            }
        }

        public static double GetScalarDouble(string sSql, string vCol, bool bLog=true)
        {
            DataTable dt1 = new DataTable();
            dt1 = mPD.GetDataTable2(sSql,bLog);
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
                dt = mPD.GetDataTable2(sql);
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

        private static List<string> MemorizePoolAddresses(string sNetworkID)
        {
            string sql = "Select * From PoolAddresses where network='" + sNetworkID + "'";
            DataTable dt = mPD.GetDataTable2(sql,false);
            List<string> a = new List<string>();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                a.Add(dt.Rows[i]["Address"].ToString());
            }
            return a;
        }

        public static bool ScanBlocksForPoolBlocksSolved(string sNetworkID, int iLookback)
        {
            //Find highest block solved
            List<string> poolAddresses = MemorizePoolAddresses(sNetworkID);
            int nTipHeight = GetTipHeight(sNetworkID);
            if (nTipHeight < 1)
            {
                return false;
            }
            if (sNetworkID == "main" & nTipHeight < 1000)
            {
                return false;
            }
            for (int nHeight = (nTipHeight - iLookback); nHeight <= nTipHeight; nHeight++)
            {
                string sql2 = "Select count(*) as Ct from Blocks where networkid='" + sNetworkID + "' and height='"  + nHeight.ToString() + "'";
                double oCt = GetScalarDouble(sql2, "ct", false);
                if (oCt == 0)
                {
                    double cSub = GetDouble(GetBlockInfo(nHeight, "subsidy", sNetworkID));
                    //if recipient is pool...
                    string sRecipient = GetBlockInfo(nHeight, "recipient", sNetworkID);
                    for (int z = 0; z < poolAddresses.Count; z++)
                    {
                        string sPoolRecvAddress = poolAddresses[z];
                        if (sRecipient == sPoolRecvAddress)
                        {
                            string sMinerNameByHashPs = "?";

                            string sql1 = "Select isnull(Username,'') as UserName,isnull(MinerName,'') as MinerName from leaderboard" + sNetworkID + " (nolock) order by HPS2 desc";
                            DataTable dt10 = mPD.GetDataTable2(sql1);
                            if (dt10.Rows.Count > 0)
                            {
                                sMinerNameByHashPs = dt10.Rows[0]["Username"].ToString() + "." + dt10.Rows[0]["MinerName"].ToString();
                            }
                            // Populate the Miner_who_found_block field:
                            string sVersion = GetBlockInfo(nHeight, "blockversion", sNetworkID);
                            string sMinerGuid = GetBlockInfo(nHeight, "minerguid", sNetworkID);
                            sql1 = "Select isnull(username,'') as UserName from Miners where id='" + sMinerGuid + "'";
                            dt10 = mPD.GetDataTable2(sql1);
                            string sMinerNameWhoFoundBlock = "?";

                            if (dt10.Rows.Count > 0)
                            {
                                sMinerNameWhoFoundBlock = (dt10.Rows[0]["UserName"] ?? "").ToString();
                            }

                            string sql = "insert into blocks (id,height,updated,subsidy,networkid,minernamebyhashps,minerid,blockversion,MinerNameWhoFoundBlock) values (newid(),'"
                                + nHeight.ToString()
                                + "',getdate(),'" + cSub.ToString() + "','" + sNetworkID + "','" + sMinerNameByHashPs
                                + "','" + sMinerGuid + "','"
                                + sVersion + "','" + sMinerNameWhoFoundBlock + "')";
                            try
                            {
                                mPD.Exec2(sql);
                            }
                            catch (Exception ex)
                            {
                                Log("Already in blocks table" + nHeight.ToString());
                            }
                            AddBlockDistribution(nHeight, cSub, sNetworkID);
                        }
                    }
                    
                }
            }

            PayBlockParticipants(sNetworkID);
            return true;
        }


        public static void GetDifficultyList(string sNetworkID, int iLookback)
        {
            int nTipHeight = GetTipHeight(sNetworkID);
            for (int nHeight = (nTipHeight - iLookback); nHeight <= nTipHeight; nHeight++)
            {
                double dDiff = GetDouble(GetShowBlock(sNetworkID, "showblock", nHeight, "difficulty"));
                double dPOWDiff = GetDouble(GetShowBlock(sNetworkID, "showblock", nHeight, "pow_difficulty"));
                string sql = "Delete from difficulty where height = '" + nHeight.ToString() + "'";
                mPD.Exec2(sql, false, false);

                sql = "Insert into Difficulty (id,height,difficulty,added,network,powdifficulty,podcdifficulty) values (newid(),'" 
                    + nHeight.ToString() + "','" + dDiff.ToString()
                    + "',getdate(),'" + sNetworkID + "','" + dPOWDiff.ToString() + "','" + dDiff.ToString() + "')";
                mPD.Exec2(sql, false, false);
            }
        }

        public static string GetShowBlock(string sNetworkid, string sCommand, int iBlockNumber, string sJSONFieldName)
        {
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
                string a = clsStaticHelper.mPD.GetScalarString2(sql10, "Added");
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
                            DataTable rl = clsStaticHelper.mPD.GetDataTable2(sql3);
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
                                clsStaticHelper.mPD.Exec2(sql3);
                            }
                            else
                            {
                                // insert questionable record
                                string sql2 = "Insert into RequestLog (username,userguid,address,id,txid,amount,added,network,ip,audited,questionable) values ('"
                                        + "',null,'" + clsStaticHelper.PurifySQL(sAddress, 80)
                                        + "',null,'" + clsStaticHelper.PurifySQL(sTxid, 100)
                                        + "','" + dAmount.ToString()
                                        + "',getdate(),'" + sNetworkID + "',null,1,1)";
                                clsStaticHelper.mPD.Exec2(sql2);
                            }
                            // Check the users balance
                            string sUserName = "";
                            double dBal = clsStaticHelper.GetUserBalance(sUserGuid, sNetworkID, ref sUserName);
                            if (dBal <= .05)
                            {
                                sql3 = "Update RequestLog set Questionable=1 where TXID='" + clsStaticHelper.PurifySQL(sTxid, 80)
                                    + "'";
                                clsStaticHelper.mPD.Exec2(sql3);
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
                string sql = "Select sum(Amount) a from TransactionLog where transactionType='mining_credit' and added > getdate()-1 and networkid = '" + clsStaticHelper.VerifyNetworkID(sNetworkID) + "'";
                double dCredit = clsStaticHelper.mPD.GetScalarDouble2(sql, "a");
                sql = "Select sum(Amount) a from TransactionLog where transactionType = 'withdrawal' and added > getdate()-1 and networkid='" + clsStaticHelper.VerifyNetworkID(sNetworkID)
                    + "'";
                double dDebit = clsStaticHelper.mPD.GetScalarDouble2(sql, "a");
                sql = "delete from Metrics where 1=1";
                clsStaticHelper.mPD.Exec2(sql);
                sql = "Insert into Metrics (id,network,credits,debits,walletdebits,added) values (newid(),'" + clsStaticHelper.VerifyNetworkID(sNetworkID)
                    + "','" + dCredit.ToString() + "','"
                    + dDebit.ToString() + "','" + dWalletDebit.ToString() + "',getdate())";
                clsStaticHelper.mPD.Exec2(sql);
            }
            catch (Exception ex)
            {
                clsStaticHelper.Log("Audit::" + ex.Message);
            }
        }

        public static void PayBlockParticipants(string sNetworkID)
        {
            // Command Timeout Expired:
            string sql3 = "exec payBlockParticipants '" + sNetworkID + "'";
            mPD.ExecWithTimeout2(sql3, 11000);
            return;
        }

        public static string GetBlockInfo(long nHeight, string sFieldName, string sNetworkID)
        {
            try
            {
                object[] oParams = new object[2];
                oParams[0] = "subsidy";
                oParams[1] = nHeight.ToString();
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


        public static double GetUpvotedLetterCount()
        {
            string sql = "select isnull(count(*),0) as Ct from Letters where added > getdate()-60 and Upvote >= 10";
            double dct = GetDouble(clsStaticHelper.mPD.ReadFirstRow2(sql, 0));
            return dct;
        }


        public static double GetMyLetterCount(string sUserGuid)
        {
            string sql = "select isnull(count(*),0) as Ct from Letters where added > getdate()-60 and Upvote >= 10 and Userid='" + sUserGuid + "'";
            double dct = GetDouble(clsStaticHelper.mPD.ReadFirstRow2(sql, 0));
            return dct;
        }

        public static void AddBlockDistribution(long nHeight, double cBlockSubsidy, string sNetworkID)
        {
            //Ensure this block distribution does not yet exist
            string sql = "select count(*) as ct from block_distribution where height='" + nHeight.ToString() + "' and networkid='" + sNetworkID + "'";
            DataTable dt = new DataTable();
            dt = mPD.GetDataTable2(sql);
            if (dt.Rows.Count > 0)
            {
                if (Convert.ToInt32(dt.Rows[0]["ct"]) > 0)
                {
                    clsStaticHelper.Log("Block already exists in block distribution block # " + nHeight.ToString());
                    //Throw New Exception("Block already exists in block distribution")
                    return;
                }
            }

            //First pre ascertain the total participants and total hashing power
            sql = "select count(*) as participants,isnull(sum(isnull(HPS" + sNetworkID + ",0)),0) as hashpower from Users where HPS" + sNetworkID + " > 0";
            DataTable dtHashPower = new DataTable();
            dtHashPower = mPD.GetDataTable2(sql);
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
                Log("No participants in block #" + nHeight.ToString());
                //   Throw New Exception("No Participants in block # " + Trim(nHeight))
                return;
            }

            // Normal Pool Fees here:
            double dFee = GetDouble(USGDFramework.clsStaticHelper.GetConfig("fee"));
            double dFeeAnonymous = GetDouble(USGDFramework.clsStaticHelper.GetConfig("fee_anonymous"));
            // Letter writing Pool Fees here:
            double dUpvotedCount = clsStaticHelper.GetUpvotedLetterCount();
            double dRequiredCount = clsStaticHelper.GetRequiredLetterCount();
            double dLWF = GetDouble(USGDFramework.clsStaticHelper.GetConfig("fee_letterwriting"));
            double dLetterFees = (dUpvotedCount < dRequiredCount) ? dLWF : 0;
            double dTotalLetterFeeCount = 0;
            //Ascertain payment per hash
            dynamic dPPH = cBlockSubsidy / (dTotalHPS + 0.01);
            //Loop through current users with HPS > 0 (those that are participating in this block) and log the hps, and the block info
            sql = "Select * from Users where HPS" + sNetworkID + " > 0 order by username";
            dt = mPD.GetDataTable2(sql);
            double dTotalLetterWritingFees = 0;
            for (int x = 0; x <= dt.Rows.Count - 1; x++)
            {
                string sUserId = dt.Rows[x]["id"].ToString();
                string sUserName = dt.Rows[x]["username"].ToString();
                double hps = Convert.ToDouble(dt.Rows[x]["HPS" + sNetworkID]);
                double cloak = GetDouble(dt.Rows[x]["Cloak"].ToString());
                // Find if user has not written any letters
                // double dLetterCount = clsStaticHelper.GetMyLetterCount(sUserId);
                double dMyLetterFees = 0;

                //Get Sub Stats for the user (MinerName and HPS)
                sql = "select avg(work.BoxHPS) HPS, MinerName from Work with (nolock)  inner join Miners on Miners.id = work.minerid " + " inner join Users on Miners.Userid = Users.Id And Users.Id = '" + sUserId + "' " + " where Work.BoxHps > 0 AND Work.Networkid='" + sNetworkID + "' " + " Group by minername order by MinerName";
                DataTable dtStats = new DataTable();
                try
                {
                    dtStats = mPD.GetDataTable2(sql);
                    string sStats = "";
                    for (int y = 0; y <= dtStats.Rows.Count - 1; y++)
                    {
                        string sRow = dtStats.Rows[y]["MinerName"].ToString() + ": "
                            + GetDouble(Math.Round(Convert.ToDouble(dtStats.Rows[y]["HPS"]), 0).ToString())
                            + " (" + GetDouble(Math.Round(hps, 0).ToString()) + ")";
                        sStats += sRow + "<br>";
                        if (sStats.Length > 3500) sStats = sStats.Substring(0, 3500);
                    }
                    double cMinerSubsidy = dPPH * hps;
                    double fee1 = dFee * cMinerSubsidy;
                    double fee2 = dFeeAnonymous * cMinerSubsidy;
                    double fee3 = dMyLetterFees * cMinerSubsidy;
                    double dTotalFees = (cloak == 1 ? (fee2 + fee1 + fee3) : (fee1 + fee3));
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
                    mPD.Exec2(sql,false,true);
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
                mPD.Exec2(sql);
            }
        }

        public static string AnalyzeSolutionBlock(string sNetworkID, string sBlockHex, string sTxHex, ref string sBibleHash, ref string sBlockMessage,
            ref bool bSigValid, ref string sError1)
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
                    string sDebug = "exec hexblocktocoinbase " + sBlockHex + " " + sTxHex;
                    bSigValid = oOut["result"]["cpid_sig_valid"];
                    bSigValid = oOut["result"]["cpid_legal"];
                    string sCPID = ExtractXML(sBlockMessage, "<cpidsig>", "</cpidsig>").ToString();
                    sError1 = oOut["result"]["cpid_legality_narr"].ToString();
                    return sRecipient;
                }
                catch (Exception ex)
                {
                    clsStaticHelper.Log("AnalyzeSolutionBlock " + ex.Message.ToString());
                    
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
                        clsStaticHelper.Log("getbiblehash forensics " + sNetworkID + ": " 
                            + " run biblehash " + sBlockHash 
                            + " " + sBlockTime
                            + " " + sPrevBlockTime
                            + " " + sPrevHeight
                            + " " + sNonce
                             + "\r\n" + ex.Message.ToString());
                        clsStaticHelper.mBitnetUseCount += 1;
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


        public static bool MassPayment(string sNetworkID)
        {
            // For every Verified recipient owed > 100 , pay the payment percent to them once per day
            string sql = "Select balancemain as oldbalance,WithdrawalAddress,balancemain-isnull((select sum(amount) from TransactionLog where transactiontype = 'mining_credit' "
                + " and transactionlog.userid = users.id and transactionLog.Added > getdate() - 1),0) owed,id,username "
                + " From Users  where balancemain > 100 and withdrawaladdress <> '' and withdrawaladdress is not null and withdrawaladdressvalidated=1 "
                + " order by balancemain desc ";
            DataTable dtMass = mPD.GetDataTable2(sql);
            double total = 0;
            clsStaticHelper.Payment[] Payments = new Payment[dtMass.Rows.Count];
            int iPosition = 0;
            for (int i = 0; i < dtMass.Rows.Count; i++)
            {
                double Owed = clsStaticHelper.GetDouble(dtMass.Rows[i]["owed"]) * .10;
                string sAddress = dtMass.Rows[i]["WithdrawalAddress"].ToString();
                bool bValid = clsStaticHelper.ValidateBiblepayAddress(sAddress, sNetworkID);
                if (bValid)
                {
                    // ensure address is not duplicated either
                    for (int z = 0; z < Payments.Length; z++)
                    {
                        if (Payments[z].Address == sAddress && sAddress.Length > 0)
                        {
                            bValid = false;
                            break;
                        }
                    }
                    if (Owed > 100 && bValid)
                    {
                        total += Owed;
                        clsStaticHelper.Payment p = new Payment();
                        p.Address = sAddress;
                        p.Amount = Owed;
                        p.UserId = dtMass.Rows[i]["id"].ToString();
                        p.UserName = dtMass.Rows[i]["username"].ToString();
                        p.OldBalance = clsStaticHelper.GetDouble(dtMass.Rows[i]["oldbalance"]);
                        p.NewBalance = p.OldBalance - p.Amount;
                        Array.Resize<Payment>(ref Payments, iPosition + 1);

                        Payments[iPosition] = p;
                        iPosition++;
                    }
                }
            }
            string sTXID = "";

            try
            {
                sTXID = clsStaticHelper.SendMany(Payments, sNetworkID, "pool", "MassPoolPayments");
            }
            catch (Exception ex)
            {
                clsStaticHelper.Log("Unable to send tx id " + ex.Message);
            }

            int iHeight = 0;
            try
            {
                iHeight = clsStaticHelper.GetTipHeight(sNetworkID);
            }
            catch (Exception ex2)
            {

            }
            if (iPosition < 1) return true;

            try
            {
                sql = "insert into MassPayments (id,added,Recipients,Total,TXID) values (newid(),getdate(),'"
                    + (iPosition + 1).ToString() + "','" + total.ToString() + "','" + sTXID + "')";
                mPD.Exec2(sql);
            }
            catch(Exception ex3)
            {

            }
            if (sTXID.Length > 5)
            {
                for (int i = 0; i <= iPosition; i++)
                {
                    try
                    {
                        if (Payments[i].Amount > 0)
                        {
                            bool bAdj = clsStaticHelper.AdjustUserBalance(sNetworkID, Payments[i].UserId, -1 * Payments[i].Amount);
                            clsStaticHelper.InsTxLog(Payments[i].UserName, Payments[i].UserId, sNetworkID,
                                iHeight, sTXID, Payments[i].Amount, Payments[i].OldBalance, Payments[i].NewBalance,
                                Payments[i].Address, "withdrawal", "auto_withdrawal");
                        }
                    }
                    catch(Exception ex4)
                    {
                        clsStaticHelper.Log("mass payments: " + ex4.Message);
                    }
                }

            }
            return true;

        }

        public static bool AdjustUserBalance(string networkid, string userguid, double amt)
        {
            string sql = "exec AdjustUserBalance '" + clsStaticHelper.VerifyNetworkID(networkid)
                + "','" + amt.ToString() + "','" + userguid + "'";
            mPD.Exec2(sql);
            return true;
        }

        public static bool InsTxLog(string susername, string suserguid,
            string networkid, double iHeight, string sTXID, double amt, double oldBalance, double newBalance, string sDestination, string sTransactionType, string sNotes)
        {
            string sql = "exec InsTxLog '" + iHeight.ToString() + "','" + clsStaticHelper.PurifySQL(sTXID, 125)
                + "','" + clsStaticHelper.PurifySQL(susername, 100)
                + "','" + clsStaticHelper.PurifySQL(suserguid, 100)
                + "','" + clsStaticHelper.PurifySQL(sTransactionType, 100)
                + "','" + clsStaticHelper.PurifySQL(sDestination, 125)
                + "','" + amt.ToString()
                + "','" + oldBalance.ToString()
                + "','" + newBalance.ToString() + "','"
                + clsStaticHelper.VerifyNetworkID(networkid)
                + "','" + sNotes + "'";
            clsStaticHelper.mPD.Exec2(sql);
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

        public static int GetTipHeight(string snetworkid)
        {
            try
            {
                BitnetClient oBit = clsStaticHelper.GetBitNet(snetworkid);
                int iTipHeight = oBit.GetBlockCount();
                if (snetworkid == "main" && iTipHeight > 0) nCurrentTipHeightMain = iTipHeight;
                return iTipHeight;
            }
            catch (Exception)
            {
                return -1;
            }
        }
        public static double GetDouble(object o)
        {
            if (o == null) return 0;
            if (o.ToString() == "") return 0;
            double d = Convert.ToDouble(o.ToString());
            return d;
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

    }
}
