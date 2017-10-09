using Microsoft.VisualBasic;
using System;


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
        
        
        public string GetHashDifficulty2(long lSharesSolved, double dHistoricalHashPs, string sNetworkID)
        {
            // This section was originally created to ratchet up the diff when we had x11, in order to award higher HPS2 pool measurements based on the diff of the problem given to the client
            // But, after f7000 kicked in, we now award equal difficulty shares to every miner, so that we dont have to reverse engineer the diff back from the solution,
            // now we just increase HPS2 an equal amount per share solved.  (This prevents any funny attacks on the client side).

            // The line below sets everyone at step 30.  Step 30 was chosen as one that takes about 3 minutes to solve on an average machine on one thread.
            if (lSharesSolved > 0) lSharesSolved = 30;
            
            long lTargetSolveCountPerRound = 60;
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
            sPrefix = sPrefix.Substring(sPrefix.Length -dSubLen ,dSubLen);
            string sPrePrefix = (sNetworkID == "test" ? "000000" : "000000");
            if (lSharesSolved > 100) sPrePrefix += "000";
            if (lSharesSolved > 125) sPrePrefix += "000000";
            string sHashTarget = sPrePrefix + sPrefix + "111100000000000000000000000000000000000000000000000000000000";
            sHashTarget = Strings.Left(sHashTarget, 64);
            return sHashTarget;
        }

        private string GetHashTarget(string sMinerGuid, string sNetworkID)
        {
            string sHashTarget = "";
            double dCt = 0;
            if ((clsStaticHelper.RequestModulusByMinerGuid(this.Server,this.Application,this.Request,sMinerGuid) % 10) == 0)
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

        protected void Page_Load(object sender, System.EventArgs e)
        {
            try
            {
                string sMiner = (Request.Headers["Miner"] ?? "").ToString();
                string sAction = (Request.Headers["Action"] ?? "").ToString();
                string sSolution = (Request.Headers["Solution"] ?? "").ToString();
                string sNetworkID = (Request.Headers["NetworkID"] ?? "").ToString();
                string sAgent = (Request.Headers["Agent"] ?? "").ToString();
                string sIP = (Request.UserHostAddress ?? "").ToString();
                string sWorkID = (Request.Headers["WorkID"] ?? "").ToString();
                string sThreadID1 = (Request.Headers["ThreadID"] ?? "").ToString();
                string sForensic = sMiner + "," + sAction + "," + sSolution + "," + sNetworkID + "," + sAgent + "," + sIP + "," + sWorkID + "," + sThreadID1;
                string sReqAction = (Request.QueryString["action"] ?? "").ToString();
                string sOS = (Request.Headers["OS"] ?? "").ToString();
               
                sAgent = sAgent.Replace(".", "");
                double dAgent = Convert.ToDouble("0" + sAgent);

                if (dAgent > 1000 && dAgent < 1035)
                {
                    string sResponse1 = "<RESPONSE>PLEASE UPGRADE</RESPONSE><ERROR>PLEASE UPGRADE</ERROR><EOF></HTML>";
                    Response.Write(sResponse1);
                    return;
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
                else if (sReqAction=="housecleaning")
                {
                    clsStaticHelper.Housecleaning("main",Server , this.Application, true);
                    string sTester = clsStaticHelper.GetBibleHash("1", "2", "3", "4", "main");
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

                if (sMinerGuid.Length == 0)
                {
                    Response.Write("<ERROR>INVALID MINER GUID " + sMiner + "</ERROR><END></HTML><EOF>" + Constants.vbCrLf);
                    return;
                }
                string sPoolRecvAddress = clsStaticHelper.AppSetting("PoolReceiveAddress_" + sNetworkID, "");

                switch (sAction)
                {
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
                        string sql10 = "exec InsWork '" + sNetworkID + "','" + sMinerGuid + "','" + sThreadID1 + "','" + sMiner + "','" + sHashTarget + "','" + sWork1 + "','" + sIP + "'";
                        clsStaticHelper.mPD.Exec(sql10);
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

                        // Track users OS so we can have some nice metrics of speed per OS, and total pool speed, etc:
                        string sql2 = "Update Work Set OS='" + sOS + "',endtime=getdate(),ThreadStart='"
                                + Strings.Trim(sThreadStart) + "',solution='" + sSolution + "',HashCounter='" + Strings.Trim(sHashCounter)
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
                    clsStaticHelper.Log("ACTION.ASPX: " + ex.Message + "," + s);
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
