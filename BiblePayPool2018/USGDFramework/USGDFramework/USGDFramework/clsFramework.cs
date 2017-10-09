using System;
using ConfigurationSettings = System.Configuration.ConfigurationManager;
using System.Data.SqlClient;
using System.Data.Sql;
using System.Data;
using Microsoft.VisualBasic;
using System.Web;
using System.Net;

namespace USGDFramework
{


    public class Data
    {
       // private SqlConnection xsqlConnection = null;

        public string sSQLConn = "Server=" + ConfigurationSettings.AppSettings["DatabaseHost"]
            + ";" + "Database=" + ConfigurationSettings.AppSettings["DatabaseName"]
            + ";MultipleActiveResultSets=true;" + "Uid=" + ConfigurationSettings.AppSettings["DatabaseUser"]
            + ";pwd=" + ConfigurationSettings.AppSettings["DatabasePass"];

	    public Data()
	    {
	        // Constructor goes here; since we use SQL Server connection pooling, dont create connection here, for best practices create connection at usage point and destroy connection after Using goes out of scope - see GetDataTable
            // This keeps the pool->databaseserver connection count < 10.  
	    }


        public void ExecResilient(string sql)
        {
            // All this does is try up to 3 times in case a deadlock occurs during execution- and only logs the error on the last try; dont use this anywhere you would have a duplication issue
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(sSQLConn))
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
                        Log(" EXECRESILIENT: " + sql + "," + ex.Message);
                    }
                }
            }
        }


        public void Exec(string sql)
	    {
            try
            {
                using (SqlConnection con = new SqlConnection(sSQLConn))
                {
                    con.Open();
                    SqlCommand myCommand = new SqlCommand(sql, con);
                    myCommand.ExecuteNonQuery();
                }

            }
            catch(Exception ex)
            {
                Log(" EXEC: " + sql + "," + ex.Message);
            }

	    }

        public DataTable GetDataTable(string sql)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(sSQLConn))
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


        public double GetScalarDouble(string sql, object vCol)
        {
            DataTable dt1 = GetDataTable(sql);
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


        public string GetScalarString(string sql, object vCol)
        {
            DataTable dt1 = GetDataTable(sql);
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




        public SqlDataReader GetDataReader(string sql)
	    {
            try
            {
                using (SqlConnection con = new SqlConnection(sSQLConn))
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

        
        public string ReadFirstRow(string sql, object vCol)
	    {

            try
            {
                using (SqlConnection con = new SqlConnection(sSQLConn))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
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

        

        private string GetMenuRow(string sMenuName, string sLink, bool bActiveHasSub, bool bHasSub, bool bLIEndTag, 
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
                sOnclick = "FrameNav('" + sLink + "','" + sMethod + "');";
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
                sOnclick = "FrameNav('" + sLink + "','" + sMethod + "');";
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
            string sWhere = bAcc ? " where Accountability=1" : " ";
            string sql = "select * from menu " + sWhere + " order by hierarchy,ordinal";
            DataTable dt = GetDataTable(sql);
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
                    long ExternalLink = 1;
                    sArgs = sURL;
                    if (x == 0 & sLastRootMenuName != sRootMenuName)
                    {
                        if (bULOpen)
                        {
                            bULOpen = false;
                            sOut += "</UL>";
                        }
                        sOut += GetMenuRow(sMenuName, sURL, true, false, false, sButtonID, sArgs, ExternalLink,sMethod);
                    }
                    else if (x != 0)
                    {
                        if (!bULOpen) { sOut += "<UL>"; bULOpen = true; }
                        sOut += GetMenuRow(sMenuName, sURL, false, false, false, sButtonID, sArgs, ExternalLink,sMethod);
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
