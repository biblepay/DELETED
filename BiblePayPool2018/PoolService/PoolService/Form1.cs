using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
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
            bool bShowForm = false;

            if (!bShowForm)
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
        public DateTime dtLastRosetta = DateTime.Now;
        public static DateTime dtLastMetricRefresh = DateTime.Now;

        public static double ElapsedSinceLastRefresh()
        {
            double dElapsedSecs = (DateTime.Now - dtLastMetricRefresh).TotalSeconds;
            return dElapsedSecs;
        }


        public void Run()
        {

            // Store the system startup time here
            USGDFramework.Shared.dtMyAnchorTime = USGDFramework.Shared.GetCurrentUtcTime();


            // SERVICE_MAIN ENTRY POINT
            while (true)
            {
                try
                {
                    string sNetworkID = "main";
                    string sHealth = USGDFramework.Shared.GetHealth(sNetworkID);
                    
                    if (sHealth != "HEALTH_DOWN")
                    {
                        USGDFramework.clsServiceOnly.ScanBlocksForPoolBlocksSolved(sNetworkID, 222);

                        clsMyScraper.ProcessStaticGraphics();

                    }
                    double dLeaderDiff = Math.Abs(DateAndTime.DateDiff(DateInterval.Minute, DateTime.Now, dtLastLeader));
                    double dRosettaDiff = Math.Abs(DateAndTime.DateDiff(DateInterval.Minute, DateTime.Now, dtLastRosetta));
                                       

                    if (ElapsedSinceLastRefresh() > 220)
                    {
                        dtLastMetricRefresh = DateTime.Now;
                        string sql = "exec UpdatePool '" + sNetworkID + "'";
                        USGDFramework.Shared.mPD.Exec2(sql);

                        if (USGDFramework.Shared.LogLimiter() > 990) USGDFramework.Shared.RewardUpvotedLetterWriters();
                        USGDFramework.Shared.DeleteOldLogs();

                        // See if its time for a mass payment
                        sql = "select max(added) added from MassPayments where  added > getdate()-2 ";
                        string sLast = USGDFramework.Shared.mPD.GetScalarString2(sql, "added");

                        if (sLast == "") sLast = "1/1/1970";

                        double dDiff = Math.Abs(DateAndTime.DateDiff(DateInterval.Minute, DateTime.Now, Convert.ToDateTime(sLast)));
                        if (dDiff > 1400 && DateTime.Now.Hour==9)
                        {
                            // Time for a mass payment
                            USGDFramework.clsServiceOnly.MassPayment("main");
                            USGDFramework.Shared.Log("Sent mass payments");
                            USGDFramework.Shared.AuditWithdrawals(sNetworkID);
                            USGDFramework.Shared.GetDifficultyList("main", 900);
                            // Orphan Tithe
                            // Disable biblepay adoption system; disable warnadopters:                            clsStaticHelper.WarnAdopters("main", 30);
                        }
                    }

                    USGDFramework.Shared.VerifySolutions(sNetworkID);
                    USGDFramework.Shared.GetDepositTXIDList(sNetworkID);
                    USGDFramework.clsServiceOnly.ProcessOrders();

                    System.Threading.Thread.Sleep(20000);
                }
                catch(Exception ex)
                {
                    USGDFramework.Shared.Log("PoolService:Run:" + ex.Message);
                }
            }
        }
        public static string DoGetHostEntry(string hostname)
        {
            string[] vE= hostname.Split(new string[] { "@" }, StringSplitOptions.None);

            if (vE.Length < 1) return "";

            string domain = vE[1];
            try
            {
                IPHostEntry host = Dns.GetHostEntry(domain);

           
                foreach (IPAddress address in host.AddressList)
                {
                    string ip = address.ToString();
                    return ip;
                }
            }
            catch(Exception ex)
            {
                return "";
            }
            return "";
        }
        private void btnVerify_Click(object sender, EventArgs e)
        {
            // loop through all email addresses, verify bad ones and set flag in uz table
            string sql = "Select * from UZ order by added";
            DataTable dt1 = USGDFramework.Shared.mPD.GetDataTable2(sql, false);
            for (int i = 0; i < dt1.Rows.Count; i++)
            {
                string sEmail = dt1.Rows[i]["Email"].ToString();
                string ip = DoGetHostEntry(sEmail);
                if (ip=="")
                {
                    sql = "Update UZ set invalidemail=1 where id ='" + dt1.Rows[i]["id"].ToString() + "'";
                    USGDFramework.Shared.mPD.Exec2(sql);
                }
        
            }
       
        }

        private void GetDTValues(DataTable dt, out string sCols, out string sData)
        {
            sData = "";
            sCols = "";
            for (int z = 0; z < dt.Columns.Count; z++)
            {
                string colName = dt.Columns[z].ColumnName;
                if (colName == "OriginalURL") colName = "URL";
                sCols += colName + "<COL>";
            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string sVals = "";

                for (int z = 0; z < dt.Columns.Count; z++)
                {
                    string colValue = dt.Rows[i][z].ToString();
                    sVals += colValue + "<COL>";
                }
                string sRow = "<COLUMNS>" + sCols + "</COLUMNS><VALUES>" + sVals + "</VALUES><ROW>\r\n";
                sData += sRow;
            }

        }

        private void DumpTable(string sql, string sTableName)
        {
            DataTable dt = USGDFramework.Shared.mPD.GetDataTable2(sql, false);
            string sCols = "";
            string sData = "";
            GetDTValues(dt, out sCols, out sData);
            string sPath = "c:\\" + sTableName + ".dat";
            System.IO.StreamWriter sw = new System.IO.StreamWriter(sPath, false);
            sw.WriteLine(sData);
            sw.Close();

        }
        private void btnDump_Click(object sender, EventArgs e)
        {
            // Dump a table into DSQL
            // Lets start by dumping the Gospel Links and move them to DSQL
            // DumpTable("Select Added,UserName,OriginalURL, Notes from Links order by added", "GospelLinks");

            //Expense
            //string sql = "Select id, Added, Amount, URL, Charity, HandledBy from Expense order by Added";
            //DumpTable(sql, "Expense");

            string sql = "Select id, Added, BBPAmount, BTCRaised, BTCPrice, Amount, Notes, HandledBy, Charity,'' as URL from OrphanAuction order by added";
            DumpTable(sql, "Revenue");

            Environment.Exit(0);
        }

    }
}
