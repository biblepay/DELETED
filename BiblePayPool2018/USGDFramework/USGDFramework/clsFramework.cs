using System;

using System.Data.SqlClient;
using System.Data.Sql;
using System.Data;
using Microsoft.VisualBasic;
using System.Web;
using System.Net;

namespace USGDFramework
{
    public static class clsStaticHelper
    {

        public static string GetConfig(string key)
        {
            // Tested
            try
            {
                string sPath = "c:\\inetpub\\wwwroot\\biblepaypool2018\\biblepaypool.ini";

                using (System.IO.StreamReader sr = new System.IO.StreamReader(sPath))
                {
                    while (sr.EndOfStream == false)
                    {
                        string sTemp = sr.ReadLine();
                        int iPos = sTemp.IndexOf("=");
                        if (iPos > 0)
                        {
                            string sIniKey = sTemp.Substring(0, iPos);
                            string sIniValue = sTemp.Substring(iPos + 1, sTemp.Length - iPos - 1);
                            sIniValue = sIniValue.Replace("\"", "");
                            if (sIniKey.ToLower().Contains("_e"))
                            {
                                sIniValue = modCryptography.Des3DecryptData2(sIniValue);
                            }
                            if (key.ToLower() == sIniKey.ToLower())
                            {
                                sr.Close();
                                return sIniValue;
                            }
                        }
                    }
                    sr.Close();
                }
            }
            catch(Exception ex)
            {
                //
            }
            
            return "";
        }

    }
    public class Data
    {

        public string sSQLConn()
        {
            return "Server=" + clsStaticHelper.GetConfig("DatabaseHost")
            + ";" + "Database=" + clsStaticHelper.GetConfig("DatabaseName")
            + ";MultipleActiveResultSets=true;" + "Uid=" + clsStaticHelper.GetConfig("DatabaseUser")
            + ";pwd=" + clsStaticHelper.GetConfig("DatabasePass_E");
        }

	    public Data()
	    {
	        // Constructor goes here; since we use SQL Server connection pooling, dont create connection here, for best practices create connection at usage point and destroy connection after Using goes out of scope - see GetDataTable
            // This keeps the pool->databaseserver connection count < 10.  
	    }


        public void ExecResilient2(string sql,bool bLog=true)
        {
            // All this does is try up to 3 times in case a deadlock occurs during execution- and only logs the error on the last try; dont use this anywhere you would have a duplication issue
            if (bLog)             TxLog(sql);
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(sSQLConn()))
                    {
                        con.Open();
                        SqlCommand myCommand = new SqlCommand(sql, con);
                        myCommand.ExecuteNonQuery();
                        return;
                    }

                }
                catch (Exception ex)
                {
                    if (i == 2)
                    {
                        if (ex.Message.Contains("Update Work Set") && ex.Message.Contains("was deadlocked on lock resources"))
                        {
                            // Rerun the transaction.
                            return;
                        }

                        Log(" EXECRESILIENT: " + sql + "," + ex.Message);
                    }
                }
            }
        }

        public void ExecWithThrow2(string sql, bool bLogErr, bool bLog=true)
        {
            try
            {
                if (bLog)                 TxLog(sql);
                using (SqlConnection con = new SqlConnection(sSQLConn()))
                {
                    con.Open();
                    SqlCommand myCommand = new SqlCommand(sql, con);
                    myCommand.ExecuteNonQuery();
                }

            }
            catch (Exception ex)
            {
                if (bLogErr) Log(" EXECWithThrow: " + sql + "," + ex.Message);
                throw (ex);
            }

        }
        public void Exec2(string sql, bool bLog=true, bool bLogError = true)
        {
            try
            {
                if (bLog)  TxLog(sql);
                using (SqlConnection con = new SqlConnection(sSQLConn()))
                {
                    con.Open();
                    SqlCommand myCommand = new SqlCommand(sql, con);

                    myCommand.ExecuteNonQuery();
                }

            }
            catch (Exception ex)
            {
                //EXEC: exec InsWork 'main','Execution Timeout Expired.  The timeout period elapsed prior to completion of the operation or the server is not responding.
                if (sql.Contains("InsWork") && ex.Message.Contains("Execution Timeout Expired"))
                {
                    // Nothing we can do, server is deadlocking, maybe upgrade the server.
                    return;
                }
                if (sql.Contains("Insert into Block_distribution") && ex.Message.Contains("Violation of UNIQUE KEY constraint"))
                {
                    return;
                }
                
                if (bLogError)                 Log(" EXEC: " + sql + "," + ex.Message);
            }

        }
        public void ExecWithTimeout2(string sql, double lTimeout)
	    {
            try
            {
                TxLog(sql);
                using (SqlConnection con = new SqlConnection(sSQLConn()))
                {
                    con.Open();
                    SqlCommand myCommand = new SqlCommand(sql, con);
                    myCommand.CommandTimeout = (int)lTimeout;
                    myCommand.ExecuteNonQuery();
                }

            }
            catch(Exception ex)
            {
                Log(" EXECWithTimeout: " + sql + "," + ex.Message);
            }

	    }

        public DataTable GetDataTableWithNoLog2(string sql)
        {
            try
            {
                TxLog(sql);
                using (SqlConnection con = new SqlConnection(sSQLConn()))
                {
                    con.Open();

                    SqlDataAdapter a = new SqlDataAdapter(sql, con);
                    DataTable t = new DataTable();
                    a.Fill(t);
                    return t;
                }
            }
            catch (Exception ex)
            {
                // 
            }
            DataTable dt = new DataTable();
            return dt;
        }

        public DataTable GetDataTable2(string sql,bool bLog=true)
        {
            try
            {
                if (bLog)                 TxLog(sql);
                using (SqlConnection con = new SqlConnection(sSQLConn()))
                {
                    con.Open();

                    SqlDataAdapter a = new SqlDataAdapter(sql, con);
                    DataTable t = new DataTable();
                    a.Fill(t);
                    return t;
                }
            }
            catch(Exception ex)
            {
                Log("GetDataTable:" + sql + "," + ex.Message);
            }
            DataTable dt = new DataTable();
            return dt;
        }


        public double GetScalarDoubleWithNoLog2(string sql, object vCol)
        {
            TxLog(sql);
            DataTable dt1 = GetDataTableWithNoLog2(sql);
            try
            {
                if (dt1.Rows.Count > 0)
                {
                    object oOut = null;
                    if (vCol.GetType().ToString() == "System.String")
                    {
                        oOut = dt1.Rows[0][vCol.ToString()];
                    }
                    else
                    {
                        oOut = dt1.Rows[0][Convert.ToInt32(vCol)];
                    }
                    double dOut = Convert.ToDouble("0" + oOut.ToString());
                    return dOut;
                }
            }
            catch (Exception ex)
            {
            }
            return 0;
        }

        public double GetScalarDouble2(string sql, object vCol, bool bLog  = true)
        {
            TxLog(sql);
            DataTable dt1 = GetDataTable2(sql,bLog);
            try
            {
                if (dt1.Rows.Count > 0)
                {
                    object oOut = null;
                    if (vCol.GetType().ToString() == "System.String")
                    {
                        oOut = dt1.Rows[0][vCol.ToString()];
                    }
                    else
                    {
                        oOut = dt1.Rows[0][Convert.ToInt32(vCol)];
                    }
                    double dOut = Convert.ToDouble("0" + oOut.ToString());
                    return dOut;
                }
            }
            catch (Exception ex)
            {
            }
            return 0;
        }


        public string GetScalarString2(string sql, object vCol, bool bLog=true)
        {
            if (bLog) TxLog(sql);
            DataTable dt1 = GetDataTable2(sql);
            try
            {
                if (dt1.Rows.Count > 0)
                {
                    object oOut = null;
                    if (vCol.GetType().ToString() == "System.String")
                    {
                        oOut = dt1.Rows[0][vCol.ToString()];
                    }
                    else
                    {
                        oOut = dt1.Rows[0][Convert.ToInt32(vCol)];
                    }
                    return oOut.ToString();
                }
            }
            catch (Exception ex)
            {
            }
            return "";
        }




        public SqlDataReader GetDataReader2(string sql)
	    {
            try
            {
                TxLog(sql);
                using (SqlConnection con = new SqlConnection(sSQLConn()))
                {
                    con.Open();
                    SqlDataReader myReader = default(SqlDataReader);
                    SqlCommand myCommand = new SqlCommand(sql, con);
                    myReader = myCommand.ExecuteReader();
                    return myReader;
                }
            }
            catch (Exception ex)
            {
                Log("GetDataReader:" + ex.Message + "," + sql);
            }
            SqlDataReader dr = default(SqlDataReader);
            return dr;
	    }

        
        public string ReadFirstRow2(string sql, object vCol, bool bLog = true)
	    {

            try
            {
                if (bLog)                 TxLog(sql);
                using (SqlConnection con = new SqlConnection(sSQLConn()))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        //cmd.CommandTimeout = 6000;

                        SqlDataReader dr = cmd.ExecuteReader();
                        if (!dr.HasRows | dr.FieldCount == 0) return string.Empty;
                        while (dr.Read())
                        {
                            if (vCol is String)
                            {
                                return dr[(string)vCol].ToString();
                            }
                            else
                            {
                                return dr[(int)vCol].ToString();
                            }
                        }
                    }
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Log("Readfirstrow: " + sql + ", " + ex.Message);
            }
            return "";
        }
        
        



        public static void TxLog(string sData)
        {
            try
            {
                string sPath = null;
                string sDocRoot = clsStaticHelper.GetConfig("LogPath");
                string sToday = DateTime.Now.ToString("MMddyyyy");
                
                sPath = sDocRoot + "txlog"+ sToday + ".dat";
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


        public static void Log(string sData)
        {
            try
            {
                string sPath = null;
                string sDocRoot = clsStaticHelper.GetConfig("LogPath");
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
        

        private string GetMenuRow(string sGuid, string sMenuName, string sLink, bool bActiveHasSub, bool bHasSub, bool bLIEndTag, 
            string sButtonID, string sMenuClick, long ExternalLink, string sMethod)
        {

            string sOut = "";
            string sClass = "";
            if (bActiveHasSub)
            {
                sClass = " class='active has-sub' ";
            }
            if (bHasSub)
            {
                sClass = " class='has-sub' ";
            }
            string sHREF = string.Empty;
            //If this is an external Link, call the event to replace the contents otherwise call the Event to raise the event into the program
            string sOnclick = null;
            if (ExternalLink==1)
            {
                sOnclick = "send('na', '','LINK', '', '', '" + sLink + "', link_complete);";
                //Display the resource
                sOnclick = sLink;
                sOnclick = "validate(this,'link','" + sLink + "');";
                sOnclick = "FrameNav('" + sLink + "','" + sMethod + "','" + sGuid + "');";
                if (sLink.StartsWith("http"))
                {
                    sOnclick = "window.navigate('" + sLink + "');";
                    sOnclick = "location.href='" + sLink + "';";
                }
            }
            else
            {
                sOnclick = "send('na', '','LINKEVENT','','','" + sLink + "',linkevent_complete);";
                //Fire the event into the .NET app
                sOnclick = sLink;
                sOnclick = "validate(this,'link','" + sLink + "');";
                sOnclick = "FrameNav('" + sLink + "','" + sMethod + "','" + sGuid + "');";
            }
            sOut += "<li " + sClass + "><a onclick=" + Strings.Chr(34) + sOnclick + Strings.Chr(34) + ">" + "<span>" + sMenuName + "</span></a>" + Constants.vbCrLf;
            if (bLIEndTag)
                sOut = sOut + "</li>";
            return sOut;
        }


        public string GetTopLevelMenu(string sURL1)
        {
            // Depending on context, filter the menu
            bool bAcc = (sURL1.ToUpper().Contains("ACCOUNTABILITY"));
            bool bDAHF = (sURL1.ToUpper().Contains("DAHF"));

            string sWhere = bAcc ? " where Accountability=1 and DAHF <> 1" : " where DAHF <> 1";
            if (bDAHF) sWhere = " where DAHF=1";

            string sql = "select * from menu " + sWhere + " order by hierarchy, ordinal";
            string sUser = USGDFramework.clsStaticHelper.GetConfig("DatabaseName");
            
            DataTable dt = GetDataTable2(sql);
            string sOut = "<div id='cssmenu'> <UL> ";
            string[] vHierarchy = null;
            string sArgs = null;
            string sLastRootMenuName = "?";
            string sRootMenuName = "";
            bool bULOpen = false;
            string sButtonID = "btn1";
            int x = 0;
            for (int y = 0; y < dt.Rows.Count; y++)
            {
                vHierarchy =  Strings.Split(dt.Rows[y]["Hierarchy"].ToString(), "/");
                for (x = 0; x <= Information.UBound(vHierarchy); x++)
                {
                    string sMenuName = vHierarchy[x].ToString();
                    if (string.IsNullOrEmpty(sMenuName))
                        sMenuName = vHierarchy[x + 1].ToString();
                    if (x == 0)
                    {
                        sRootMenuName = vHierarchy[x].ToString();
                        if (string.IsNullOrEmpty(sRootMenuName))
                            sRootMenuName = vHierarchy[x + 1].ToString();
                    }
                    string sURL = dt.Rows[y]["Classname"].ToString();
                    string sMethod = (dt.Rows[y]["Method"] ?? "").ToString();
                    string sGuid = (dt.Rows[y]["id"].ToString());
                    long ExternalLink = 1;
                    sArgs = sURL;
                    if (x == 0 & sLastRootMenuName != sRootMenuName)
                    {
                        if (bULOpen)
                        {
                            bULOpen = false;
                            sOut += "</UL>";
                        }
                        sOut += GetMenuRow(sGuid,sMenuName, sURL, true, false, false, sButtonID, sArgs, ExternalLink,sMethod);
                    }
                    else if (x != 0)
                    {
                        if (!bULOpen) { sOut += "<UL>"; bULOpen = true; }
                        sOut += GetMenuRow(sGuid,sMenuName, sURL, false, false, false, sButtonID, sArgs, ExternalLink,sMethod);
                    }
                    if (x == 0)
                        sLastRootMenuName = sRootMenuName;
                }

            }
            if (bULOpen)
                sOut += "</UL>";

            sOut = sOut + "</UL>";
            sOut += "<table class='last'>";
            for (x = 1; x <= 10; x++)
            {
                sOut += "<tr><td>&nbsp;</td></tr>";
            }
            sOut += "</table> </div>";
            return sOut;

        }
    }
}
