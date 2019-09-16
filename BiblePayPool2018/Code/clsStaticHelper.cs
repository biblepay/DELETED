using Bitnet.Client;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Web;
using static USGDFramework.Shared;

namespace BiblePayPool2018
{
    public class clsStaticHelper
    {

        public static BiblePayPool2018.WebReply mAboutWebReply = null;
        public static HttpServerUtility mHttpServer;

        public static string AppCache(string sKey, HttpApplicationState ha)
        {
            try
            {
                string sOut = null;
                sOut = ReadKey(sKey, ha);
                return sOut;
            }
            catch (Exception ex)
            {
                Log("Err while AppCache accessing " + sKey + " " + ex.Message);
                return "";
            }
        }

        public static void AppCache(string sKey, string sValue, HttpServerUtility server, HttpApplicationState ha)
        {
            UpdateKey(sKey, sValue, ha);
        }

        public static List<string> GetPoolAddress(string sNetworkID, HttpServerUtility server, HttpApplicationState ha)
        {
            object a = ha["pooladdresses_" + sNetworkID];
            if (a == null)
            {
                MemorizePoolAddresses(sNetworkID, server, ha);
            }
            List<string> pa = (List<string>)a;
            return pa;
        }
        
        public static string GetCookie(string sKey)
        {
            HttpCookie _pool = HttpContext.Current.Request.Cookies["credentials_" + sKey];
            if (_pool != null)
            {
                string sOut = (_pool.Value ?? string.Empty).ToString();
                return sOut;
            }
            return "";
        }

        public static void StoreCookie(string sKey, string sValue)
        {
            try
            {
                HttpCookie _pool = new HttpCookie("credentials_" + sKey);
                _pool[sKey] = sValue;
                _pool.Expires = DateTime.Now.AddDays(7);
                if (HttpContext.Current != null)
                {
                    HttpContext.Current.Response.Cookies.Add(_pool);
                }
                HttpContext.Current.Response.Cookies["credentials_" + sKey].Value = sValue;
            }
            catch (Exception ex)
            {
                string sError = ex.Message;


            }
        }

        public static void UpdateKey(string sKey, string sValue, HttpApplicationState ha)
        {
            ha[sKey] = sValue;
        }

        private double GetHPS(string sMinerGuid, string sNetworkID)
        {
            string sql = "Select isnull(boxhps" + sNetworkID + ",0) As hps from miners where id ='" +  GuidOnly(sMinerGuid) + "'";
            double dct = GetDouble(mPD.ReadFirstRow2(sql, 0));
            return dct;
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

        private static List<string> MemorizePoolAddresses(string sNetworkID, HttpServerUtility server, HttpApplicationState ha)
        {
            string sql = "Select * From PoolAddresses where network='" + sNetworkID + "'";
            DataTable dt = mPD.GetDataTable2(sql, false);
            List<string> a = new List<string>();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                a.Add(dt.Rows[i]["Address"].ToString());
            }
            ha["pooladdresses_" + sNetworkID] = a;
            return a;
        }

        public static string GetNextPoolAddress(string sNetworkID, HttpServerUtility server, HttpApplicationState ha)
        {
            try
            {
                double d = 1;
                List<string> pa = GetPoolAddress(sNetworkID, server, ha);
                double iFactor = pa.Count / 1000.001; //addresscount divided by loglimit length
                int iPtr = (int)(Math.Round(iFactor * d, 0) - 1);
                iPtr = 0;
                string sAddress = pa[iPtr];
                return sAddress;
            }
            catch (Exception ex)
            {
                List<string> pa1 = MemorizePoolAddresses(sNetworkID, server, ha);
                return pa1[0];
            }
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
                 Log(" ONS .. ");
                return "";
            }
            sOut = (ha[sKey] ?? "").ToString();
            return sOut;
        }
    }
}