using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using System.Data;
using System.Drawing;

namespace PoolService
{
    public static class clsMyScraper
    {
        public static string[] Split(string data, string delimiter)
        {
            string[] vRows =data.Split(new string[] { delimiter }, StringSplitOptions.None);
            return vRows;

        }

        public static int UBound(string[] data)
        {
            return data.Length - 1;
        }

        public static string Replace(string data, string find, string withwhat)
        {
            if (find=="")
            {
                return data;
            }
            string sOut = data.Replace(find, withwhat);
            return sOut;
        }

        public static string Trim(string data)
        {
            return data.Trim();
        }

        public static void GetHostsByUser()
        {
            string sURL = "https://boinc.bakerlab.org/rosetta/hosts_user.php?userid=1983490";
            MyWebClient myHttp = new MyWebClient();
            string data1 = myHttp.DownloadString(sURL);
            string[] vRows = data1.Split(new string[] { "<tr" }, StringSplitOptions.None);
            for (int j = 1; j < vRows.Length; j++)
            {
                vRows[j] = vRows[j].Replace("</td>", "");
                string[] vCols = vRows[j].Split(new string[] { "<td>" }, StringSplitOptions.None);
                // Glean the individual data
                if (vCols.Length > 6)
                {
                    string hostid = clsStaticHelper.ExtractXML(vCols[1], "hostid=", ">").ToString();
                }
            }
        }

        public static void InductRosettaTasksByUser()
        {
            MyWebClient myHttp = new MyWebClient();
            for (int i = 0; i < 100; i+=20)
            {
                string sURL = "https://boinc.bakerlab.org/rosetta/results.php?userid=475629&offset=" + i.ToString() + "&show_names=0&state=1&appid=";
                sURL = "https://boinc.bakerlab.org/rosetta/results.php?hostid=3351460&offset=" + i.ToString() + "&show_names=0&state=1&appid=";
                string data1 = myHttp.DownloadString(sURL);
                string[] vRows;
                vRows = data1.Split(new string[] { "<tr" }, StringSplitOptions.None);
                for (int j = 1; j < vRows.Length; j++)
                {
                    vRows[j] = vRows[j].Replace("</td>", "");
                    vRows[j] = vRows[j].Replace("\n", "");
                    vRows[j] = vRows[j].Replace("<td align=right>", "<td>");

                    string[] vCols = vRows[j].Split(new string[] { "<td>" }, StringSplitOptions.None);
                    // Glean the individual data
                    if (vCols.Length >6)
                    {
                        string sStartTime = vCols[3].Trim();
                        string wuId = clsStaticHelper.ExtractXML(vCols[1], "resultid=", "\"").ToString();
                        string sStatus = vCols[5].Trim();
                    }

                }
            }
        }


        private static Image TextToFile(String text, Font font, Color textColor, Color backColor, string sFileName)
        {
            Image img = new Bitmap(1, 1);
            Graphics drawing = Graphics.FromImage(img);
            SizeF textSize = drawing.MeasureString(text, font);
            img.Dispose();
            drawing.Dispose();
            img = new Bitmap((int)textSize.Width, (int)textSize.Height);
            drawing = Graphics.FromImage(img);
            Color bgColor = System.Drawing.Color.Transparent;
            drawing.Clear(bgColor);
            Brush textBrush = new SolidBrush(textColor);
            drawing.DrawString(text, font, textBrush, 0, 0);
            drawing.Save();
            textBrush.Dispose();
            drawing.Dispose();
            img.Save(sFileName);
            return img;
        }

        public static void ProcessStaticGraphics()
        {
            Font font = new Font("Arial", 17);
            string sSource = "c:\\inetpub\\ftproot\\biblepay\\announce.txt";
            if (System.IO.File.Exists(sSource) == false) return;
            string data = System.IO.File.ReadAllText(sSource);
            string sTarget = "c:\\inetpub\\wwwroot\\biblepaypool2018\\images\\announce.png";
            TextToFile(data, font, System.Drawing.Color.Maroon, System.Drawing.Color.Transparent, sTarget);
        }

        public static void InductSuperblockData()
        {
            string sPath = "c:\\inetpub\\ftproot\\biblepay\\magnitude";
            if (System.IO.File.Exists(sPath) == false) return;
            string data = System.IO.File.ReadAllText(sPath);
            string[] vRows = data.Split(new string[] { "<ROW>" }, StringSplitOptions.None);
            string sql = "";
            if (vRows.Length > 3)
            {
                string row = vRows[2];
                row = row.Replace("\n", "");

                string[] vRowData = row.Split(new string[] { "," }, StringSplitOptions.None);
                double dHeight = clsStaticHelper.GetDouble(vRowData[12]);
                if (dHeight == 0) return;
                sql = "Delete from Superblocks where  height='" + dHeight.ToString() + "'";
                clsStaticHelper.mPD.Exec2(sql);
            }
            for (int i = 0; i < vRows.Length; i++)
            {
                string row = vRows[i];
                row = row.Replace("\n", "");
                
                string[] vRowData = row.Split(new string[] { "," }, StringSplitOptions.None);
                if (vRowData.Length > 5)
                {
                    //Address, cpid, magnitude, rosettaid, team, utxoweight, taskweight, total rac, unbanked indicator, UTXO Amount, AvgRAC, ModifiedRAC
                    string sAddress = vRowData[0];
                    string sCPID = vRowData[1];
                    string sMag = vRowData[2];
                    string sRosettaid = vRowData[3];
                    string sTeam = vRowData[4];
                    string taskweight = vRowData[6];
                    string totalrac = vRowData[7];
                    string unbanked = vRowData[8];
                    string utxoamount = vRowData[5];
                    string sUTXOWeight = vRowData[9];
                    string avgrac = vRowData[10];
                    string modifiedrac = vRowData[11];
                    string height = vRowData[12];
                    string RACWCG = vRowData[13];
                    sql = "Insert Into Superblocks (id, added, address, cpid, magnitude, rosettaid, team, utxoweight, taskweight, totalrac, unbanked, utxoamount, avgrac, modifiedrac, height, wcgrac) values ("
                        + "newid(), getdate(), '" + sAddress + "','" + sCPID + "','" + sMag + "','" + sRosettaid + "','" + sTeam + "','" + sUTXOWeight + "','"
                        + taskweight + "','" + totalrac + "','" + unbanked + "','"
                        + utxoamount + "','" + avgrac + "','" + modifiedrac + "','" + height + "','" + RACWCG + "')";

                    clsStaticHelper.mPD.Exec2(sql, false);
                    
                }
            }
            //Finalize the data:
            string sql3 = " Update Superblocks set MachineCount = (Select MachineCount from RosettaMaster where rosettamaster.rosettaid = superblocks.rosettaid), TotalProcs = (Select TotalProcs from RosettaMaster where rosettamaster.rosettaid = superblocks.rosettaid) ,Name = (Select Username from RosettaMaster where rosettamaster.rosettaid = superblocks.rosettaid)   where Superblocks.Added > getdate() - 1";
            clsStaticHelper.mPD.Exec2(sql3);
        }

        public static void InductRosettaLeaderboardData()
        {
            MyWebClient myHttp = new MyWebClient();
            string sql = "Delete from tempTeam where 1=1";
            clsStaticHelper.mPD.Exec2(sql, false);
            for (int x = 0; x <= 1240; x += 20)
            {
                var sTeamURl = "https://boinc.bakerlab.org/rosetta/team_members.php?teamid=15044&offset=" + x.ToString()
                    + "&sort_by=expavg_credit";
                var sData = myHttp.DownloadString(sTeamURl);
                ConvertPageToTable(sData, "tempTeam", "htmlrow,rosettaID,username,credit,rac,country", "rosettaID", "", "");
            }


            DataTable dt;
            sql = "Select * from TempTeam where 1=1 order by RosettaID";
            dt =clsStaticHelper.mPD.GetDataTable2(sql);
            for (int x = 0; x <= dt.Rows.Count - 1; x++)
            {
                long rosettaID = (long)Convert.ToDouble(dt.Rows[x]["rosettaID"].ToString());
                string sDrill = "https://boinc.bakerlab.org/rosetta/hosts_user.php?userid=" + rosettaID.ToString();
                string data1 = myHttp.DownloadString(sDrill);
                ConvertPageToTable(data1, "tempHosts", "computerID,Rank1,RAC,Credit,BoincVersion,CPU,GPU,OS,LastContact", "computerID", "rosettaID", rosettaID.ToString());
            }
            sql = "Delete from TempHosts where added < getdate()-7";
            clsStaticHelper.mPD.Exec2(sql);

            sql = "Select * from tempHosts where 1=1 order by ComputerID";
            dt =clsStaticHelper.mPD.GetDataTable2(sql);
            for (int x = 0; x <= dt.Rows.Count - 1; x++)
            {
                long computerID = (long)Convert.ToDouble(dt.Rows[x]["computerID"].ToString());
                sql = "select count(*) ct from TempHostDetails where computerid='" + computerID.ToString() + "' and added > getdate()-3";

                double dExists = clsStaticHelper.mPD.GetScalarDouble2(sql, "ct", false);
                if (dExists == 0)
                {

                    string sURL = "https://boinc.bakerlab.org/rosetta/show_host_detail.php?hostid=" + computerID.ToString();
                    string data1 = myHttp.DownloadString(sURL);
                    string sFields = "OwnerID,Created,credit,rac,url1,cpu,procs,gpu,os,boincversion,memory,cache,fps,mis,uprate,downrate,turnaround,url2,url3,contactTimes,lastcontact";
                    try
                    {
                        ConvertPageToRow(data1, "tempHostDetails", sFields, computerID.ToString());
                    }
                    catch (Exception ex5)
                    {
                        string test2 = ex5.Message;
                    }
                }
            }
            // Update the Unbanked Arm columns
            string sql2 = "Update TempHostDetails set arm = 1 where cpu like '%arm%' or cpu = ''";
            clsStaticHelper.mPD.Exec2(sql2);
            string sql10 = "Exec updateRosetta 'main'";
            clsStaticHelper.mPD.Exec2(sql10);
        }

        public static void ConvertPageToTable(string sData, string sTableName, string sColNames, string sPrimaryKey, string sOptForeignKey, string sOptForeignKeyValue)
        {
            string[] vRows;
            vRows = sData.Split(new string[] { "<tr" }, StringSplitOptions.None);
            string[] cols = Split(sColNames, ",");
            int colcount = UBound(cols) + 1;
            for (int x = 0; x <= UBound(vRows); x++)
            {
                string sRow;
                sRow = vRows[x];
                sRow = sRow.Replace("class=row1>", "");
                sRow = Replace(sRow, "</tr>", "");
                sRow = Replace(sRow, " align=right", "");
                sRow = Replace(sRow, " align=left", "");
                sRow = Replace(sRow, "</td>", "");
                sRow = Replace(sRow, "<a href=" + "\""  + "https://boinc.bakerlab.org/rosetta/show_user.php?", "");
                string sDummy20 = clsStaticHelper.ExtractXML(sRow, "<a href=\"https://boinc.bakerlab.org/rosetta/view_profile.php?", ">").ToString();
                if (sDummy20.Length > 1) sDummy20 = "<a href=\"https://boinc.bakerlab.org/rosetta/view_profile.php?" + sDummy20 + ">";
                sRow = Replace(sRow, sDummy20, "");
                sRow = Replace(sRow, "million ops/sec", "");
                string sDummy30 = clsStaticHelper.ExtractXML(sRow, "<img title=\"View the profile of", "\"").ToString();
                if (sDummy30.Length > 1) sDummy30 = "<img title=\"View the profile of" + sDummy30 + "\"";
                sRow = Replace(sRow, sDummy30, "");
                string sUserId = clsStaticHelper.ExtractXML(sRow, "userid=", "\"").ToString();
                sRow = Replace(sRow, "userid=" + sUserId, "<td>" + sUserId + "</td><td>");
                string sDummy = clsStaticHelper.ExtractXML(sRow, "<img", ">").ToString();
                sRow = Replace(sRow, sDummy, "");
                var sDummy2 = clsStaticHelper.ExtractXML(sRow, "</table", "</html").ToString();
                sRow = Replace(sRow, sDummy2, "");
                sRow = Replace(sRow, ">", "");
                sRow = Replace(sRow, "</td>", "");
                sRow = Replace(sRow, "</td", "");
                sRow = Replace(sRow, "\"", "");
                sRow = Replace(sRow, "'", "");
                sRow = Replace(sRow, "</html", "");
                sRow = Replace(sRow, "ID: ", "");
                sRow = Replace(sRow, "<br", "");
                string sDummy3 = clsStaticHelper.ExtractXML(sRow, "<a href=show_host_detail", ">").ToString();
                sRow = Replace(sRow, sDummy3, "");
                string sDummy4 = clsStaticHelper.ExtractXML(sRow, "<a href=results.php", ">").ToString();
                sRow = Replace(sRow, sDummy4, "");
                string sDummy5 = clsStaticHelper.ExtractXML(sRow, "<a href=http://boincstats.com", "Free-DC").ToString();
                sRow = Replace(sRow, sDummy5, "");
                sRow = Replace(sRow, "</small", "");
                sRow = Replace(sRow, "<small", "");
                sRow = Replace(sRow, "</table", "");
                string sDummy6 = clsStaticHelper.ExtractXML(sRow, "<a href=show_host", "<a").ToString();
                sRow = Replace(sRow, sDummy6, "");
                sRow = Replace(sRow, "href=http://boincstats.comFree-DC", "");
                string sDummy7 =clsStaticHelper.ExtractXML(sRow, "<a href=show_host", "</nobr").ToString();
                sRow = Replace(sRow, sDummy7, "");
                sRow = Replace(sRow, "<a href=show_host", "");
                sRow = Replace(sRow, "</nobr", "");
                sRow = Replace(sRow, "<a", "");
                sRow = Replace(sRow, "href=https://boinc.bakerlab.org/rosetta/view_profile.php?", "");
                sRow = Replace(sRow, " title=Top 25% in average credit", "");
                sRow = Replace(sRow, "</a", "");
                sRow = Replace(sRow, "valign=top height=24 ", "");
                sRow =Replace(sRow,"src=img/pct_25.png", "");
                sRow = Replace(sRow, "src=https://boinc.bakerlab.org/rosetta/img/head_20.png alt=Profile", "");
                sRow = Replace(sRow, "<img", "");
                string[] vRow;
                vRow = Split(sRow, "<td");
                string sValues = "";
                long valuecount = 0;
                string sPrimaryValue = "";
                for (int y = 1; y <= UBound(vRow); y++)
                {
                    string sMyValue = vRow[y];
                    sMyValue = sMyValue.Trim();
                    sMyValue = Replace(sMyValue, ",", "");
                    sMyValue = sMyValue.Trim();
                    sMyValue = Replace(sMyValue, "\r", "");
                    sMyValue = Replace(sMyValue, "\n", "");
                    sMyValue = Trim(sMyValue);
                    sMyValue = "'" + sMyValue + "'";
                    if (UBound(cols) > y - 2)
                    {
                        if (cols[y - 1] == sPrimaryKey)
                            sPrimaryValue = sMyValue;
                    }

                    valuecount += 1;
                    sValues += sMyValue + ",";
                }

                if (sValues.Length > 1)
                             sValues = sValues.Substring(0,sValues.Length - 1);
                string sql;
                string sOptCols = "";
                string sOptValues = "";
                if (sOptForeignKey.Length > 1)
                {
                    sOptCols = "," + sOptForeignKey;
                    sOptValues = ",'" + sOptForeignKeyValue + "'";
                }

                sql = "Insert into " + sTableName + " (id,added," + sColNames + sOptCols + ") values (newid(),getdate()," + sValues + sOptValues + ")";
                if (valuecount == colcount)
                {
                    string sql2 = "Delete from " + sTableName + " where " + sPrimaryKey + "=" + sPrimaryValue + "";
                    if ((sPrimaryValue != ""))
                    {
                       clsStaticHelper.mPD.Exec2(sql2, false);
                    }
                    clsStaticHelper.mPD.Exec2(sql);
                }
            }
        }

        public static void ConvertPageToRow(string sData, string sTableName, string sColNames, string sCPUID)
        {
            if (sData.Contains(">Unable to handle request<"))
            {
                return;
            }
            string sValues = "";
            string[] vRows;
            vRows = Split(sData, "<tr");
            string[] cols = Split(sColNames, ",");
            long colcount = UBound(cols) + 1;
            for (int x = 0; x <= UBound(vRows); x++)
            {
                string sRow;
                sRow = vRows[x];
                sRow = Replace(sRow, "class=row1>", "");
                sRow = Replace(sRow, "</tr>", "");
                sRow = Replace(sRow, "align=right", "");
                sRow = Replace(sRow, "align=left", "");
                sRow = Replace(sRow, "million ops/sec", "");
                sRow = Replace(sRow, "</td>", "");
                sRow = Replace(sRow, "<a href=" + "\"" + "https://boinc.bakerlab.org/rosetta/show_user.php?", "");
                string sUserId = clsStaticHelper.ExtractXML(sRow, "userid=", "\"").ToString();
                sRow = Replace(sRow, "userid=" + sUserId, "<td>" + sUserId + "</td><td>");
                var sDummy = clsStaticHelper.ExtractXML(sRow, "<img", ">").ToString();
                sRow = Replace(sRow, sDummy, "");
                var sDummy2 = clsStaticHelper.ExtractXML(sRow, "</table", "</html").ToString();
                sRow = Replace(sRow, sDummy2, "");
                sRow = Replace(sRow, "</a", "");
                sRow = Replace(sRow, "\"", "");
                sRow = Replace(sRow, "'", "");
                sRow = Replace(sRow, "</html", "");
                sRow = Replace(sRow, "ID: ", "");
                sRow = Replace(sRow, "<br", "");
                string sDummy3 = clsStaticHelper.ExtractXML(sRow, "<a href=show_host_detail", ">").ToString();
                sRow = Replace(sRow, sDummy3, "");
                string sDummy4 = clsStaticHelper.ExtractXML(sRow, "<a href=results.php", ">").ToString();
                sRow = Replace(sRow, sDummy4, "");
                string sDummy5 = clsStaticHelper.ExtractXML(sRow, "<a href=http://boincstats.com", "Free-DC").ToString();
                sRow = Replace(sRow, sDummy5, "");
                sRow = Replace(sRow, "</small", "");
                sRow = Replace(sRow, "<small", "");
                sRow = Replace(sRow, "</table", "");
                string sDummy6 = clsStaticHelper.ExtractXML(sRow, "<a href=show_host", "<a").ToString();
                sRow = Replace(sRow, sDummy6, "");
                sRow = Replace(sRow, "href=http://boincstats.comFree-DC", "");
                string sDummy7 = clsStaticHelper.ExtractXML(sRow, "<a href=show_host", "</nobr").ToString();
                sRow = Replace(sRow, sDummy7, "");
                sRow = Replace(sRow, "<a href=show_host", "");
                sRow = Replace(sRow, "</nobr", "");
                sRow = Replace(sRow, "<a", "");
                sRow = Replace(sRow, "<img", "");
                sRow = Replace(sRow, " style=padding-left:12px", "");
                sRow = Replace(sRow, "href=https://boinc.bakerlab.org/rosetta/view_profile.php?", "");
                sRow = Replace(sRow, "title=Top 25% in average credit valign=top height=24 src=img/pct_25.png", "");
                sRow = Replace(sRow, "title=Top 5% in average credit valign=top height=24 src=img/pct_5.png", "");
                sRow = Replace(sRow, "title=Top 1% in average credit valign=top height=24 src=img/pct_1.png", "");
                long valuecount = 0;
                string sDummy10 = clsStaticHelper.ExtractXML(sRow, "<td", ">").ToString();
                sRow = Replace(sRow, sDummy10, "");
                string sDummy11 = clsStaticHelper.ExtractXML(sRow, "<td", "<").ToString();
                sRow = Replace(sRow, sDummy11, "");
                sRow = Replace(sRow, "<td", "");
                sRow = Replace(sRow, ">", "");
                sRow = Replace(sRow, "\r", "");
                sRow = Replace(sRow, "\n", "");
                sRow = Replace(sRow, " days", "");
                string sMyValue = Trim(sRow);
                sMyValue = Trim(sMyValue);
                sMyValue = Replace(sMyValue, ",", "");
                sMyValue = Trim(sMyValue);
                sMyValue = Replace(sMyValue, "\r", "");
                sMyValue = Replace(sMyValue, "\n", "");
                sMyValue = Trim(sMyValue);
                sMyValue = "'" + sMyValue + "'";
                valuecount += 1;
                if (sMyValue.Length  < 200 & (!sMyValue.Contains( "<th")))
                {
                    sValues += sMyValue + ",";
                }
            }

            if (sValues.Length > 1)
            {
                sValues =sValues.Substring(0,sValues.Length - 1);
            }
            string sql;
            bool bCache = sData.Contains("Cache");
            if (bCache == false)
                sColNames = Replace(sColNames, "cache,", "");
            if (sValues.Contains("Anonymous"))
            {
                sColNames = sColNames.Replace( "url1,", "");
            }
            sql = "Insert into " + sTableName + " (id,added,computerid," + sColNames + ") values (newid(),getdate(),'" + sCPUID + "'," + sValues + ")";
            

            string sql2 = "Delete from " + sTableName + " where computerid = '" + sCPUID + "'";
            if ((sCPUID != ""))
                clsStaticHelper.mPD.Exec2(sql2, false);
            try
            {
                clsStaticHelper.mPD.ExecWithThrow2(sql, false, false);

            }
            catch (Exception ex)
            {
                string sTestErr = ex.Message;
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


