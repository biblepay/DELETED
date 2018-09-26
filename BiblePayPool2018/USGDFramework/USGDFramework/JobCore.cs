using System;
using System.Collections.Generic;
using System.Net.Mail;
using Microsoft.VisualBasic;
using ConfigurationSettings = System.Configuration.ConfigurationManager;

namespace USGDFramework
{

    public static class Stakeholders
    {
        public enum ErrorType
        {
            Assumption,
            DebugException,
            Exception,
            Information,
            Metric,
            Specific
        }
    
        public struct ErrorIncident
        { 
            public ErrorType ErrorTyp;
            public DateTime Time;
            public string Info;
        }

        public static Dictionary<ErrorIncident, string> dictIncidents = new Dictionary<ErrorIncident, string>();
        public static int[] iErrorCount = new int[6];

        public static void LogMsg(ErrorType ErrType, string sMessage)
        {
            ErrorIncident e = new ErrorIncident();
            e.ErrorTyp = ErrType;
            e.Time = System.DateTime.Now;
            e.Info = sMessage;
            dictIncidents.Add(e, e.Info);
            iErrorCount[Convert.ToInt32(ErrType)] += 1;
        }

        public static object GetMessageBodyBasedOnIncidents()
 	    {

	    	Array items = default(Array);
		    items = System.Enum.GetNames(typeof(ErrorType));
		    string sBody = string.Empty;
		    long iCount = 0;

		    foreach (string item in items) 
            {
			    iCount = 0;
			    foreach (KeyValuePair<ErrorIncident, string> kvp in dictIncidents) 
                {
				    if (kvp.Key.ErrorTyp.ToString() == item) 
                    {
					    iCount += 1;
					    if ((iCount == 1)) 
                        {
						    string sPlural = (iErrorCount[(int)kvp.Key.ErrorTyp] > 1 ? "'s" : "");

                            sBody += Constants.vbCrLf + kvp.Key.ErrorTyp.ToString()
                                + sPlural
                                + " (" + iErrorCount[(int)kvp.Key.ErrorTyp].ToString()
                                    + "): "
                                + Constants.vbCrLf + Constants.vbCrLf + Constants.vbCrLf + "<br>";
						    sBody = sBody.Replace("Information's", "Information Items");

					    }

                        sBody += kvp.Key.Time.ToString() + ": " + kvp.Key.Info + Constants.vbCrLf + Constants.vbCrLf + "<br>";
				    }
			    }
		    }

    		return sBody;
	    }
    }


    public class JobCore
    {

        public object SendEmail(string ToAddress, string FromAddress, string sFromName, string MessageSubject, string MessageBody)
        {
            string MessageHead = "<html><head>";
            MessageHead = MessageHead + "<style>";
            MessageHead = MessageHead + "body {background-color:#F7F7F7; color:#000; font-family:arial,verdana,sans-serif; font-size:12px;}";
            MessageHead = MessageHead + "</style></head><body>";
            string MessageFoot = "</body></html>";
            MessageBody = MessageHead + MessageBody + MessageFoot;
            string ReturnMessage = "";
            MailMessage mm = new MailMessage();
            MailAddress ma = new MailAddress(FromAddress, sFromName);
            mm.From = ma;
            dynamic vTo = Strings.Split(ToAddress, ";");
            for (int x = 0; x <= Information.UBound(vTo); x++)
            {
                mm.To.Add(new MailAddress(vTo[x]));
            }
            SmtpClient smtp = new SmtpClient(clsStaticHelper.GetConfig("MailHost"));

            var _with1 = smtp;
            _with1.EnableSsl = true;
            _with1.Credentials = new System.Net.NetworkCredential(clsStaticHelper.GetConfig("FromEmail"), 
                clsStaticHelper.GetConfig("SMTPPassword_E"));
            mm.Subject = MessageSubject;
            mm.Body = MessageBody;
            mm.IsBodyHtml = true;
            try
            {
                smtp.Send(mm);
                ReturnMessage = "Email has been dispatched";
            }
            catch (Exception ex)
            {
                ReturnMessage = "We're sorry, there has been an error: " + ex.Message;
            }
            return Strings.Replace(Strings.Left(ReturnMessage, 200), "'", "''");
        }

    }
}
