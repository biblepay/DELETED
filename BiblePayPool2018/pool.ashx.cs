using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Web;


namespace BiblePayPool2018
{
    public class USGD : BaseHttpHandler
    {
        
        WebReply GenericException(SystemObject sys, string Body, string Title)
        {
            Dialog d = new Dialog(sys);
            WebReply wr = d.CreateDialog("ErrDialog1", Title, Body, 200, 200);
            return wr;
        }

        private List<SystemObject.LookupValue> SortableListToLookupValueList(string[] vLookups)
        {
            List<SystemObject.LookupValue> lLV = new List<SystemObject.LookupValue>();
              for (int i = 0; i < vLookups.Length; i++)
              {
                            string[] vCols = vLookups[i].Split(new string[] { "[COL]" }, StringSplitOptions.None);
                            if (vCols.Length > 2)
                            {
                                string sColName = vCols[0];
                                string sColValue = vCols[1];
                                string sUSGDID = vCols[2];
                                string sUSGDValue = vCols[3];
                                string sUSGDCaption = vCols[4];
                                string sUSGDName = vCols[5];

                                SystemObject.LookupValue v = new SystemObject.LookupValue();
                                v.Value = sUSGDValue;
                                v.Caption = sUSGDCaption;
                                v.ID = sUSGDID;
                                v.Name = sUSGDName;

                                lLV.Add(v);
                            }
             }
             return lLV;
        }

        public override void HandleRequest(System.Web.HttpContext context)
        {

            // Deserialize the strongly typed javascript object back to c# object
            WebObj g = new WebObj();
            g = JsonConvert.DeserializeObject<WebObj>(sPostData);
            SystemObject Sys = (SystemObject)HttpContext.Current.Session["Sys"];
            if (g.action == "postdiv" || g.action=="formload")
            {
                // Now we store the uploaded values on the business objects
                string[] vRows = g.body.Split(new string[] { "[ROW]" }, StringSplitOptions.None);
                for (int i = 0; i < vRows.Length - 1; i++)
                {
                    string[] vCols = vRows[i].Split(new string[] { "[COL]" }, StringSplitOptions.None);
                    string sName = vCols[0];
                    string sValue = vCols[1];
                    string sUSGDID = vCols[2];
                    string sUSGDVALUE = vCols[3];
                    WebObj fnew = new WebObj();
                    fnew.value = sValue;
                    fnew.name = sName;
                    fnew.usgdid = sUSGDID;
                    //Verify divname == sectioname
                    Sys.UpdateObject(g.divname,ref fnew);

                }
            // and we reply with a replacement div for this section only
                if (g.eventname=="sortevent" || g.eventname=="dropevent")
                {
                    string sPost = g.guid;
                    if (g.eventname=="dropevent")
                    {
                        // Get the guid of the section
                        string sSection = Sys.ExtractXML(sPost, "[DROPPABLE]", "[/DROPPABLE]");
                        string sLastCoord = "";
                        string sCurrCoord = "";
                        string sql = "Select * FROM SECTION WHERE Name='" + sSection + "' and deleted=0";
                        DataTable dt2 = Sys._data.GetDataTable(sql);
                        string sSectionGuid = dt2.Rows[0]["id"].ToString();
                        string sCurrFields = dt2.Rows[0]["Fields"].ToString();
                        string sData = Sys.ExtractXML(sPost, "[DATA]", "[/DATA]");
                        string[] vRows2 = sData.Split(new string[] { "[ROW]" }, StringSplitOptions.None);
                        string sCols = "";
                        int iCoordCounter = 0;

                        for (int i = 0; i < vRows2.Length-1; i++)
                        {
                            string sRow = vRows2[i];
                            string[] vCols = sRow.Split(new string[] { "[COL]" }, StringSplitOptions.None);

                            string sName = vCols[4];
                            string sCaption = vCols[3];
                            bool bIsCoord = false;

                            if (sName.Contains("coords"))
                            {
                                bIsCoord = true;
                                sCurrCoord = sName;
                                if (sCurrCoord != sLastCoord) 
                                sLastCoord = sName;
                                iCoordCounter++;
                            }

                            if (sName.Length > 0 && !bIsCoord)
                            {
                                sCols += sName + ",";
                                iCoordCounter = 0;
                            }
                      
                            if (iCoordCounter > 1)
                            {
                                sCols += ",";
                            }
                        }
                        string[] vF = sCurrFields.Split(new string[] { "," }, StringSplitOptions.None);
                        string[] vTargetFields = sCols.Split(new string[] { "," },StringSplitOptions.None);
                        bool bFail = false;
                        for (int iSource = 0; iSource < vF.Length; iSource++ )
                        {
                            if (vF[iSource].Length > 0)
                            {
                                bool bFound = false;
                                for (int iTarget = 0; iTarget < vTargetFields.Length; iTarget++)
                                {
                                    if (vTargetFields[iTarget].Length > 0)
                                    {
                                        if (vTargetFields[iTarget].ToLower() == vF[iSource].ToLower())
                                        {
                                            bFound = true;
                                            break;
                                        }
                                    }
                                }
                                if (!bFound)
                                {
                                    bFail = true;
                                }
                            }
                          
                        }

                        if (!bFail)
                        {
                            sCols = sCols.Substring(0, sCols.Length - 1);
                            string sSql = "Update Section set Fields='" + sCols + "' where id = '" + sSectionGuid + "'";
                            Sys._data.Exec(sSql);
                        }
                    }
                    else if (sPost.Contains("[SORTABLE]"))
                    {
                        sPost = sPost.Replace("[SORTABLE]", "");
                        string[] vRows2 = sPost.Split(new string[] { "[ROWSET]" }, StringSplitOptions.None);
                        string sSection = vRows2[0];
                        string sName = vRows2[1];
                        string sLeft = vRows2[2];
                        string sRight = vRows2[3];
                        // Reconstitute the Lists

                        string[] vLookups = sLeft.Split(new string[] { "[ROW]" }, StringSplitOptions.None);
                        string[] vLookupsRight = sRight.Split(new string[] { "[ROW]" }, StringSplitOptions.None);

                        List<SystemObject.LookupValue> lLVLeft = new List<SystemObject.LookupValue>();
                        List<SystemObject.LookupValue> lLVRight= new List<SystemObject.LookupValue>();
                        lLVLeft =  SortableListToLookupValueList(vLookups);
                        lLVRight = SortableListToLookupValueList(vLookupsRight);

                        WebObj oLeft = new WebObj();
                        oLeft.name = sName;
                        oLeft.divname = sSection;
                        oLeft.LookupValues = lLVLeft;

                        WebObj oRight = new WebObj();
                        oRight.name = sName + "_2";
                        oRight.divname = sSection;
                        oRight.LookupValues = lLVRight;

                        Sys.UpdateObject(sSection, ref oLeft);
                        Sys.UpdateObject(sSection, ref oRight);
                        
                    }
                }

                if (g.eventname == "buttonevent" || true)
                {
                    //Raise the event in the class, then return with some data.
                    Sys.LastWebObject = g;
                    var type1 = Type.GetType(g.classname);
                    if (type1 == null)
                    {
                        // Give the user the Nice Dialog showing that the object is null
                        WebReply wr1 = GenericException(Sys,"Web Class Not Found: " + g.classname, "Web Class Not Found");
                        g.divname = "divErrors";
                        g.body = wr1.Packages[0].HTML;
                        g.javascript = wr1.Packages[0].Javascript;
                        string myJason = JsonConvert.SerializeObject(g);
                        context.Response.Write(myJason);
                        return;
                    }
                    if (g.guid=="")
                    {
                        string[] vCol = g.body.Split(new string[] { "[COL]" }, StringSplitOptions.None);
                        if (vCol.Length > 0)
                        {
                            //g.guid = vCol[0];
                        }
                        g.guid = "0";
                    }
                    if (Sys.Organization.ToString() == "00000000-0000-0000-0000-000000000000")
                    {
                        //Session has expired
                        //throw new Exception("Session has expired."); (Deprecated)
                    }
                    if (g.eventname == "expand")
                    {
                        bool Expanded = !(Sys.GetObjectValue(g.divname,"ExpandableSection" + g.classname + g.methodname) == "UNEXPANDED" ? false : true);
                        Sys.SetObjectValue(g.divname,"ExpandableSection" + g.classname + g.methodname, Expanded ? "EXPANDED" : "UNEXPANDED");
                    }
                    else if (g.eventname.ToLower()=="orderbyclick")
                    {
                        //Toggle the Order by
                        string sOrderByClass = Sys.GetObjectValue(g.divname,"OrderByClass" + Sys.LastWebObject.guid);
                        string desc = "";
                        if (sOrderByClass == "up")
                        {
                            sOrderByClass = "down";
                        }
                        else
                        {
                            sOrderByClass = "up";
                            desc = " desc";
                        }
                        Sys.SetObjectValue(g.divname,"OrderByClass" + Sys.LastWebObject.guid, sOrderByClass);
                        g.orderby = Sys.LastWebObject.guid + " " + desc;
                    }
                    try
                    {
                        if (Sys.Organization.ToString() == "00000000-0000-0000-0000-000000000000")
                        {
                            // User is not logged in ( If the user has a valid session, use it first ):
                            string sTheUser = clsStaticHelper.GetCookie("username");
                            string sThePass = clsStaticHelper.GetCookie("password");
                            if (sTheUser != "" && sThePass != "")
                            {
                                Login l = new Login(Sys);
                                bool bMyDepersist = l.VerifyUser(sTheUser, sThePass, ref Sys, false);
                            }
                        }
                    }
                    catch(Exception ex5)
                    {

                    }
                    try
                    {
                        //Store the event information in Sys, so the receiver can access it (before invoking):
                        if (Sys.Organization.ToString() == "00000000-0000-0000-0000-000000000000" && g.classname != "BiblePayPool2018.Login")
                        {
                            if (System.Diagnostics.Debugger.IsAttached && false)
                            {
                                // instantiate the user based on the debug user guid.
                                Login l = new Login(Sys);
                            }
                            if (HttpContext.Current.Request.Url.ToString().ToUpper().Contains("ACCOUNTABILITY"))
                            {
                                Login l = new Login(Sys);
                                bool bAuth = l.VerifyUser("guest", "guest", ref Sys, false);
                                // Start at the expense View Page when coming in from the Wallet accountability button
                                g.classname = "BiblePayPool2018.Home";
                                g.methodname = "ExpenseList";
                            }
                            g.classname = "BiblePayPool2018.Login";
                            g.methodname = "LoginSection";
                        }

                        if (Sys.Username=="" && g.classname != "BiblePayPool2018.Login")
                        {
                            // Redirect to Login Page
                            g.classname = "BiblePayPool2018.Login";
                            g.methodname = "LoginSection";
                        }

                        type1 = Type.GetType(g.classname);
                        object myObject1 = Activator.CreateInstance(type1, Sys);
                        MethodInfo methodInfo1 = type1.GetMethod(g.methodname);
                        WebRequest wRequest = new WebRequest();
                        wRequest.eventName = g.eventname;
                        wRequest.action = g.action;
                        object[] parametersArray = new object[] { wRequest };
                        WebReply wr = (WebReply)methodInfo1.Invoke(myObject1, null);
                        List<WebObj> woReplies = new List<WebObj>();
                        bool bClearScreen = false;
                        if (g.eventname.ToLower() == "formevent") bClearScreen = true;
                        int iInstanceCount = 0;
                        int iPackageCount = wr.Packages.Count;
                        foreach (WebReplyPackage wrp in wr.Packages)
                        {
                            WebObj woInstance = new WebObj();
                            woInstance.ClearScreen = wrp.ClearScreen;
                            woInstance.SingleTable = wrp.SingleUITable;
                            iInstanceCount++;
                            if (iInstanceCount == 1 && bClearScreen) woInstance.ClearScreen = true;
                            woInstance.body = wrp.HTML;
                            woInstance.javascript = wrp.Javascript;
                            woInstance.doappend = wrp.doappend;
                            // If we are clearing the screen, add a breadcrumb
                            if (woInstance.ClearScreen)
                            {
                                Sys.AddBreadcrumb(wrp.SectionName, g.classname, g.methodname, true);
                                Sys.SetObjectValue("", "ApplicationMessage", wrp.SectionName);
                            }
                            woInstance.breadcrumb = Sys.GetBreadcrumbTrailHTML();
                            woInstance.breadcrumbdiv = "divbreadcrumb";
                            woInstance.ApplicationMessage = Sys.GetObjectValue("", "ApplicationMessage");
                            woInstance.divname = wrp.SectionName;
                            woInstance.action = "refresh";
                            woReplies.Add(woInstance);
                        }
                        string myJason1 = JsonConvert.SerializeObject(woReplies);
                        context.Response.Write(myJason1);
                    }
                    catch (Exception ex)
                    {
                        WebReply wr1 = GenericException(Sys, "Web Class Not Found: " + g.classname, "Web Class Not Found");
                        g.divname = "divErrors";
                        g.body = wr1.Packages[0].HTML;
                        g.javascript = wr1.Packages[0].Javascript;
                        string myJason = JsonConvert.SerializeObject(g);
                        context.Response.Write(myJason);
                    }
                }
            }
        }
    }
}