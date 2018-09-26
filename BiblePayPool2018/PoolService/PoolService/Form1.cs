using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace PoolService
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            if (true || !System.Diagnostics.Debugger.IsAttached)
            {
                 Run();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string sSource = txtEncrypt.Text;
            string sEnc = USGDFramework.modCryptography.Des3EncryptData2(sSource);
            txtEncrypted.Text = sEnc;
        }

        public DateTime dtLastLeader = DateTime.Now.Subtract(TimeSpan.FromDays(1));
       
        public void Run()
        {

            // Store the system startup time here
            clsStaticHelper.dtMyAnchorTime = clsStaticHelper.GetCurrentUtcTime();
            // SERVICE_MAIN ENTRY POINT
            while (true)
            {
                try
                {
                    string sNetworkID = "main";
                    string sHealth = clsStaticHelper.GetHealth(sNetworkID);
                    
                    if (sHealth != "HEALTH_DOWN")
                    {
                        clsStaticHelper.ScanBlocksForPoolBlocksSolved(sNetworkID, 222);
                        clsMyScraper.ProcessStaticGraphics();

                    }
                    double dLeaderDiff = Math.Abs(DateAndTime.DateDiff(DateInterval.Minute, DateTime.Now, dtLastLeader));
                    if (dLeaderDiff > 180)
                    {
                        dtLastLeader = DateTime.Now;
                        // Induct the daily Rosetta data
                        try
                        {
                            clsMyScraper.InductSuperblockData();
                            clsMyScraper.InductRosettaLeaderboardData();
                        }
                        catch (Exception ex)
                        {
                            string sMyErr = ex.Message;

                        }

                    }

                    if (clsStaticHelper.ElapsedSinceLastRefresh() > 220)
                    {
                        clsStaticHelper.dtLastMetricRefresh = DateTime.Now;
                        string sql = "exec UpdatePool '" + sNetworkID + "'";
                        clsStaticHelper.mPD.Exec2(sql);
                        if (clsStaticHelper.LogLimiter() > 994) clsStaticHelper.RewardUpvotedLetterWriters();
                        clsStaticHelper.DeleteOldLogs();
                        // See if its time for a mass payment
                        sql = "select max(added) added from MassPayments where  added > getdate()-2 ";
                        string sLast = clsStaticHelper.mPD.GetScalarString2(sql, "added");
                        if (sLast == "") sLast = "1/1/1970";

                        double dDiff = Math.Abs(DateAndTime.DateDiff(DateInterval.Minute, DateTime.Now, Convert.ToDateTime(sLast)));
                        if (dDiff > 1400 && DateTime.Now.Hour==9)
                        {
                            // Time for a mass payment
                            clsStaticHelper.MassPayment("main");
                            clsStaticHelper.Log("Sent mass payments");
                            clsStaticHelper.AuditWithdrawals(sNetworkID);
                            clsStaticHelper.GetDifficultyList("main", 777);
                            // Orphan Tithe
                            clsStaticHelper.TitheExtraBalances("main");
                            clsStaticHelper.WarnAdopters("main", 30);
                        }
                    }

                    clsStaticHelper.VerifySolutions(sNetworkID);
                    System.Threading.Thread.Sleep(1000);
                }
                catch(Exception ex)
                {
                    clsStaticHelper.Log("PoolService:Run:" + ex.Message);
                }
            }
        }
    }
}
