using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.DataVisualization.Charting;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.DataVisualization.Charting;
using System.Data;
using System.Web.SessionState;
using static USGDFramework.Shared;

namespace BiblePayPool2018
{
    public class Charts: USGDGui, IRequiresSessionState
    {
        public Charts(SystemObject S) : base(S)
        {
            S.CurrentHttpSessionState["CallingClass"] = this;
        }

        System.Drawing.Color GetBGColor(SystemObject Sys)
        {
            if (Sys.Theme == "Dark")
            {
                return System.Drawing.Color.Black;
            }
            else if (Sys.Theme.ToLower() == "biblepay")
            {
                return System.Drawing.Color.Maroon;
            }
            else return System.Drawing.Color.Gray;
        }

        public double GetDouble(object o)
        {
            if (o == null) return 0;
            if (o.ToString() == "") return 0;
            double d = Convert.ToDouble(o.ToString());
            return d;
        }


        public WebReply ProposalFundingByExpenseType(SystemObject Sys)
        {

            string sql = "Select ExpenseType, sum(amount) amount From proposal where Network = '" +  VerifyNetworkID(Sys.NetworkID)
                + "' and paidtime is null and "
                + " absoluteYesCount > masternodecount * .1 Group by expensetype     union all select 'TOTAL' as t,5789219 as amt ";
            // Chart the funding per Expense Type
            Chart c = new Chart();
            c.Width = 1500;
            Series s = new Series("ChartOrphanHistory");
            s.ChartType = System.Web.UI.DataVisualization.Charting.SeriesChartType.Bar;
            s.LabelForeColor = System.Drawing.Color.Green;
            s.Color = System.Drawing.Color.Green;
            c.ChartAreas.Add("ChartArea");
            c.ChartAreas[0].BorderWidth = 1;
            c.Series.Add(s);
            c.ChartAreas[0].AxisX.LabelStyle.Interval = 1;
            DataTable dt = Sys._data.GetDataTable2(sql);
            double dTotal = 0;
            double dNextSuperblock = Convert.ToDouble(GetGenericInfo(Sys.NetworkID, "getgovernanceinfo", "nextsuperblock", "result").ToString());

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                double dAmountWinning = GetDouble(dt.Rows[i]["Amount"]);
                string sExpenseType = dt.Rows[i]["ExpenseType"].ToString();
                if (sExpenseType == "TOTAL") dAmountWinning = dTotal;
                
                double dBudget = ExpenseTypeToBudgetAmount(Sys,(int)dNextSuperblock, Sys.NetworkID, sExpenseType);


                double dPercentFunded = (dAmountWinning / (dBudget + .01));
                bool bRed = (dAmountWinning > dBudget + 1);
                dTotal += dAmountWinning;
                string Name = dt.Rows[i]["ExpenseType"].ToString();
                string sFullName = Name + " [" + dAmountWinning.ToString() + "/" + dBudget.ToString() + "]";

                double dPointValue = dPercentFunded * 100;

                s.Points.AddXY(sFullName, dPointValue);
                if (bRed)
                {
                    foreach (DataPoint p in s.Points)
                    {
                        if (p.AxisLabel == sFullName)
                        {
                            p.Color = System.Drawing.Color.Red;
                        }
                    }
                }
            }
            c.ChartAreas[0].AxisX.LabelStyle.ForeColor = System.Drawing.Color.LightGreen;
            c.ChartAreas[0].AxisY.LabelStyle.ForeColor = System.Drawing.Color.LightGreen;
            c.ChartAreas[0].BackColor = GetBGColor(Sys);
            c.Titles.Add("Proposal Funding Level by Expense Type");
            c.Titles[0].ForeColor = System.Drawing.Color.Green;
            c.BackColor = GetBGColor(Sys);
            c.ForeColor = System.Drawing.Color.Green;

            string sFileName = Guid.NewGuid().ToString() + ".png";
            string sSan = USGDFramework.clsStaticHelper.GetConfig("SAN");
            string sTargetPath = sSan + sFileName;
            if ( LogLimiter() > 990) DeleteOrphanedPngFiles(sSan);


            c.SaveImage(sTargetPath);
            Section s1 = new Section("Proposal Funding Level by Expense Type", 1, Sys, this);
            Edit gePFL = new Edit("Proposal Funding Level by Expense Type", Edit.GEType.Image, "W1", "", Sys);
            gePFL.URL = USGDFramework.clsStaticHelper.GetConfig("WebSite") + "SAN/" + sFileName;
            s1.AddControl(gePFL);
            return s1.Render(this, false);
        }

        static void DeleteOrphanedPngFiles(string folderName)
        {
            foreach (string xmlFile in System.IO.Directory.GetFiles(folderName, "*.png"))
            {
                System.IO.FileInfo fi = new System.IO.FileInfo(xmlFile);
                double Age = (DateTime.Now - fi.CreationTime).TotalDays;

                if (Age > 30)
                {
                    if (fi.Name.Length == 40)
                    {
                        System.IO.File.Delete(xmlFile);
                    }

                }
            }
        }



        public double ExpenseTypeToBudgetAmount(SystemObject Sys, int nHeight, string sNetworkID, string sExpenseType)
        {
            double dBudget = Convert.ToDouble(GetSuperblockBudget(Sys.NetworkID, nHeight).ToString());
            //            double dBudget = Convert.ToDouble(GetGenericInfo(Sys.NetworkID, "getgovernanceinfo", "nextbudget", "result").ToString());
            sExpenseType = sExpenseType.ToUpper();
            if (dBudget == 0) dBudget = 5610000;
            double dAmt = 0;
            switch (sExpenseType)
            {
                case "IT":
                    dAmt = .25 * dBudget;
                    break;
                case "PR":
                    dAmt = .125 * dBudget;
                    break;
                case "CHARITY":
                    dAmt = .50 * dBudget;
                    break;
                case "P2P":
                    dAmt = .125 * dBudget;
                    break;
                case "TOTAL":
                    dAmt = 1.00 * dBudget;
                    break;
                default:
                    dAmt = 0;
                    break;

            }
            return dAmt;
        }



        public string GetChartOfDifficulty(SystemObject Sys, System.Drawing.Color color, string sChartName, string sFieldName)
        {
            string sql = "select Height,Difficulty,POWDifficulty,PODCDifficulty,POGDifficulty From Difficulty Where network = '"
                    + Sys.NetworkID + "' and height > 50000 and added > getdate()-30 order by Height";
            Chart c = new Chart();
            c.Width = 1500;
            Series s = new Series(sChartName);
            s.ChartType = System.Web.UI.DataVisualization.Charting.SeriesChartType.FastLine;
            s.LabelForeColor = color;
            s.Color = System.Drawing.Color.Beige;
            s.BackSecondaryColor = GetBGColor(Sys);

            c.ChartAreas.Add("ChartArea");
            c.ChartAreas[0].BorderWidth = 1;
            c.Series.Add(s);
            c.ChartAreas[0].AxisX.LabelStyle.Interval = 250;
            DataTable dt = Sys._data.GetDataTable2(sql);

            c.Legends.Add(new System.Web.UI.DataVisualization.Charting.Legend(sChartName));
            c.Legends[sChartName].DockedToChartArea = "ChartArea";
            c.Legends[sChartName].BackColor = GetBGColor(Sys);
            c.Legends[sChartName].ForeColor = color;

            Series sAvg = new Series("ChartAvg");
            c.Series.Add(sAvg);

            sAvg.ChartType = SeriesChartType.FastLine;
            double dAvg = 0;
            double dAvgI = 0;
            double dWeight = 1;
            double dAvgT = 0;
            for (int i = 0; i < dt.Rows.Count; i += 7)
            {
                double Height = GetDouble(dt.Rows[i]["height"]);
                double dPOWDiff = GetDouble(dt.Rows[i][sFieldName]);

                string sNarr = Height.ToString();
                s.Points.AddXY(Height, dPOWDiff);
                dAvgI++;
                dWeight += .00017;
                dWeight = 1;
                dAvgT += (dPOWDiff * dWeight);
                dAvg = dAvgT / dAvgI;
                sAvg.Points.AddXY(Height, dAvg);

                foreach (DataPoint p in s.Points)
                {
                    p.Color = color;
                }
            }
            c.ChartAreas[0].AxisX.LabelStyle.ForeColor = System.Drawing.Color.LightGreen;
            c.ChartAreas[0].AxisY.LabelStyle.ForeColor = System.Drawing.Color.LightGreen;
            c.ChartAreas[0].BackColor = GetBGColor(Sys);
            c.Titles.Add(sChartName); //title
            c.Titles[0].ForeColor = System.Drawing.Color.Green;
            c.BackColor = GetBGColor(Sys);
            c.ForeColor = System.Drawing.Color.Green;
            string sFileName = Guid.NewGuid().ToString() + ".png";
            string sSan = USGDFramework.clsStaticHelper.GetConfig("SAN");
            string sTargetPath = sSan + sFileName;
            c.SaveImage(sTargetPath);
            return sFileName;
        }


        public WebReply ProposalFundingChart(SystemObject Sys)
        {
            string sql = "select Name,absoluteYesCount,NoCt,MasternodeCount, (AbsoluteYesCount)/(MasterNodeCount + .01) as popularity from proposal where Network='"
                +  VerifyNetworkID(Sys.NetworkID)
                + "' and paidtime is null and added > getdate()-37";
            // Chart the funding percent by proposal
            Chart c = new Chart();
            c.Width = 1500;
            Series s = new Series("ChartOrphanHistory");
            s.ChartType = System.Web.UI.DataVisualization.Charting.SeriesChartType.Bar;
            s.LabelForeColor = System.Drawing.Color.Green;
            s.Color = System.Drawing.Color.Green;
            c.ChartAreas.Add("ChartArea");
            c.ChartAreas[0].BorderWidth = 1;
            c.Series.Add(s);
            c.ChartAreas[0].AxisX.LabelStyle.Interval = 1;
            DataTable dt = Sys._data.GetDataTable2(sql);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                double YesCt = GetDouble(dt.Rows[i]["absoluteYesCount"]);
                double NoCt = Math.Abs(GetDouble(dt.Rows[i]["NoCt"]));
                double MNC = GetDouble(dt.Rows[i]["MasternodeCount"]);
                string sNarr = YesCt.ToString() + " - " + MNC.ToString();
                double Pop = GetDouble(dt.Rows[i]["Popularity"]);
                bool bRed = (Pop * 100) < 10 && NoCt > 0;
                string Name = dt.Rows[i]["Name"].ToString();
                string sFullName = Name + " [" + sNarr + "]";
                double dPointValue = Pop * 100;
                s.Points.AddXY(sFullName, dPointValue);
                if (bRed)
                {
                    foreach (DataPoint p in s.Points)
                    {
                        if (p.AxisLabel == sFullName)
                        {
                            p.Color = System.Drawing.Color.Red;
                            p.YValues[0] = dPointValue * -1;
                            if (p.YValues[0] < 1 || true) p.YValues[0] = -1;
                        }
                    }
                }
            }
            c.ChartAreas[0].AxisX.LabelStyle.ForeColor = System.Drawing.Color.LightGreen;
            c.ChartAreas[0].AxisY.LabelStyle.ForeColor = System.Drawing.Color.LightGreen;
            c.ChartAreas[0].BackColor = GetBGColor(Sys);
            c.Titles.Add("Proposal Funding Level");
            c.Titles[0].ForeColor = System.Drawing.Color.Green;
            c.BackColor = GetBGColor(Sys);
            c.ForeColor = System.Drawing.Color.Green;
            string sFileName = Guid.NewGuid().ToString() + ".png";
            string sSan = USGDFramework.clsStaticHelper.GetConfig("SAN");
            string sTargetPath = sSan + sFileName;
            c.SaveImage(sTargetPath);
            Section s1 = new Section("Proposal Funding Level", 1, Sys, this);
            Edit gePFL = new Edit("Proposal Funding Level", Edit.GEType.Image, "W1", "", Sys);
            gePFL.URL = USGDFramework.clsStaticHelper.GetConfig("WebSite") + "SAN/" + sFileName;
            s1.AddControl(gePFL);
            return s1.Render(this, false);
        }

        public WebReply CPIDHistory()
        {
            string sql = "Select CPID from Uz where uz.id='" + Sys.UserGuid.ToString() + "'";
            string sCpid = mPD.GetScalarString2(sql, "CPID", false);
            sql = "select Height,Magnitude,UTXOWeight,TaskWeight From Superblocks where cpid='" + sCpid
                + "' and height > 90000 order by Height";
            Chart c = new Chart();
            c.Width = 1500;
            Series s = new Series("Magnitude");
            s.ChartType = System.Web.UI.DataVisualization.Charting.SeriesChartType.Line;
            s.LabelForeColor = System.Drawing.Color.Green;
            s.Color = System.Drawing.Color.Green;
            c.ChartAreas.Add("ChartArea");
            c.ChartAreas[0].BorderWidth = 1;
            c.Series.Add(s);
            c.ChartAreas[0].AxisX.LabelStyle.Interval = 250;
            DataTable dt = Sys._data.GetDataTable2(sql);
            c.Legends.Add(new System.Web.UI.DataVisualization.Charting.Legend("Magnitude"));
            c.Legends.Add(new System.Web.UI.DataVisualization.Charting.Legend("UTXO Weight"));
            //c.Legends.Add(new System.Web.UI.DataVisualization.Charting.Legend("Task Weight"));

            // Set Docking chart of the legend to the Default chart area.
            c.Legends["Magnitude"].DockedToChartArea = "ChartArea";
            c.Legends["Magnitude"].BackColor = GetBGColor(Sys);
            c.Legends["Magnitude"].ForeColor = System.Drawing.Color.White;

            c.Legends["UTXO Weight"].DockedToChartArea = "ChartArea";
            c.Legends["UTXO Weight"].ForeColor = System.Drawing.Color.Beige;
            

            Series sUTXO = new Series("UTXOWeight");
            c.Series.Add(sUTXO);
            sUTXO.Color = System.Drawing.Color.Beige;
            sUTXO.ChartType = SeriesChartType.FastLine;
            
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                double Height = GetDouble(dt.Rows[i]["height"]);
                double dMagnitude = GetDouble(dt.Rows[i]["magnitude"]);
                double dUTXOWeight = GetDouble(dt.Rows[i]["utxoweight"]) / 1000;
                //  double dTaskWeight = (GetDouble(dt.Rows[i]["taskweight"]) + 5)/ 10;
                s.Points.AddXY(Height, dMagnitude);
                sUTXO.Points.AddXY(Height, dUTXOWeight);
            }
            c.ChartAreas[0].AxisX.LabelStyle.ForeColor = System.Drawing.Color.LightGreen;
            c.ChartAreas[0].AxisY.LabelStyle.ForeColor = System.Drawing.Color.LightGreen;
            c.ChartAreas[0].BackColor = GetBGColor(Sys);
            c.Titles.Add("CPID History");
            c.Titles[0].ForeColor = System.Drawing.Color.Green;
            c.BackColor = GetBGColor(Sys);
            c.ForeColor = System.Drawing.Color.Green;
            string sFileName = Guid.NewGuid().ToString() + ".png";
            string sSan = USGDFramework.clsStaticHelper.GetConfig("SAN");
            string sTargetPath = sSan + sFileName;
            c.SaveImage(sTargetPath);
            Section s1 = new Section("CPID History", 1, Sys, this);
            Edit gePFL = new Edit("CPID History", Edit.GEType.Image, "W1", "", Sys);
            gePFL.URL = USGDFramework.clsStaticHelper.GetConfig("WebSite") + "SAN/" + sFileName;
            s1.AddControl(gePFL);
            return s1.Render(this, false);
        }






    }
}