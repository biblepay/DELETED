using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using static USGDFramework.Shared;

namespace BiblePayPool2018
{
    public class Login : IRequiresSessionState
    {
        SystemObject Sys = null;

        public Login(SystemObject s)
        {
            Sys = s;
        }
       
        public WebReply Logoff()
        {
            Sys.SetObjectValue("Login","Caption1", "You have been successfully logged off.");
            Sys.SetObjectValue("Login","Password", "");
            Sys.SetObjectValue("Login","ApplicationMessage", "Logon");
            Sys.Organization = Guid.Parse("00000000-0000-0000-0000-000000000000");
            clsStaticHelper.StoreCookie("username", "");
            clsStaticHelper.StoreCookie("password", "");
            Sys.UserGuid = null;
            Sys.Username = "";
            return LoginSection();
        }


        public WebReply MenuLogon()
        {
            Sys.SetObjectValue("Login", "Caption1", "Enter Name and Password then press Login.");
            Sys.SetObjectValue("Login", "Password", "");
            return LoginSection();
        }

        public WebReply LoginSection()
        {
            // BIBLEPAY ACCOUNTABILITY
            if (HttpContext.Current.Request.Url.ToString().ToUpper().Contains("ACCOUNTABILITY"))
            {
                Login l = new Login(Sys);
                // Harness point 09252018
                bool bAuth = l.VerifyUser("guest", USGDFramework.clsStaticHelper.GetConfig("guestpassword"), ref Sys, false);
                Sys.IP = (HttpContext.Current.Request.UserHostAddress ?? "").ToString();
                // Start at the expense View Page when coming in from the Wallet accountability button
                Home h = new Home(Sys);
                return h.ExpenseList();
            }

            Section Login = new Section("Login", 3, Sys, this);
            Edit geUserName = new Edit("Login","Username", Sys);
            geUserName.CaptionText = "Username:";
            geUserName.Width = "width=140px";
            Login.AddControl(geUserName);
            Edit geBR1 = new Edit("Login", Edit.GEType.HTML, "br1","br1",Sys);
            Login.AddControl(geBR1);
            Edit geBR2 = new Edit("Login", Edit.GEType.HTML, "br2","br2",Sys);
            Login.AddControl(geBR2);
            Login.AddControl(geBR1);
            Edit gePassword = new Edit("Login", Edit.GEType.Password, "Password", "Password:", Sys);
            Login.AddControl(gePassword);
           
            if (geUserName.TextBoxValue.Length > 0 && gePassword.TextBoxValue.Length > 0  && Sys.GetObjectValue("Login","Caption1")==String.Empty)
            {
                gePassword.ErrorText = "<color=red>Invalid Username or Password";
            }

            string sMessage1 = Sys.GetObjectValue("Login", "Message1");
            if (sMessage1 != "")
            {
                gePassword.ErrorText = sMessage1;
            }


            Edit geBR3 = new Edit("Login", Edit.GEType.HTML,"br3","br3", Sys);
            Login.AddControl(geBR3);
            Edit geBR4 = new Edit("Login", Edit.GEType.HTML, "br4","br4",Sys);
            Login.AddControl(geBR4);

            Edit geBtnLogin = new Edit("Login",Edit.GEType.DoubleButton, "btnLogin", "Login", Sys);
            geBtnLogin.Name2 = "btnLogout";
            geBtnLogin.CaptionText2 = "Logout";
            Login.AddControl(geBtnLogin);

            Edit geBtnRegister = new Edit("Login", Edit.GEType.DoubleButton, "btnRegister", "Register", Sys);
            geBtnRegister.MaskBeginTD = true;
            geBtnRegister.MaskEndTD = true;

            geBtnRegister.Name2 = "btnResetPassword";
            geBtnRegister.CaptionText2 = "Reset Password";
            Login.AddControl(geBtnRegister);
            
            // New Row, and global caption:
            Edit geTR3 = new Edit("Login",Edit.GEType.TableRow, "Tr3", "", Sys);
            Login.AddControl(geTR3);
            Edit geSpan = new Edit("Login",Edit.GEType.Caption, "Caption1", "", Sys);
            Login.AddControl(geSpan);
            Sys.SetObjectValue("","ApplicationMessage", "Login");
            return Login.Render(this, true);
        }
        public WebReply btnLogout_Click()
        {
            return Logoff();
        }

        public WebReply btnResetPassword_Click()
        {
            string sUsername = Sys.GetObjectValue("Login", "Username");
            // Verify the user exists
            string sql = "Select id,email from Uz where username ='" + PurifySQL(sUsername,40) +  "'; ";
            string id = Sys._data.GetScalarString2(sql, "id");
            string sEmail = Sys._data.GetScalarString2(sql, "email");
            string sNarr = "";
            if (id.Length == 0)
            {
                sNarr = "Unable to retrieve a user with that username";
            }
            else if (!Sys.IsValidEmailFormat(sEmail))
            {
                sNarr = "Email address on file for this user is not valid.  Please send an e-mail to support@biblepay.org.";
            }
            else
            {
                sNarr = "Password reset instructions have been sent to the e-mail address on file.  Please click the link in your e-mail and follow the instructions in order to reset your password.";
                // Send the email here
                string sLink = HttpContext.Current.Request.Url.ToString();
                sLink = sLink.Substring(0, sLink.Length - 10);
                string sID1 = Guid.NewGuid().ToString();
                string sIP = (HttpContext.Current.Request.UserHostAddress ?? "").ToString();
                sql = "insert into passwordreset (id,userid,used,added,ip) values ('" + sID1 + "','" + id + "',0,getdate(),'" + sIP + "')";
                mPD.Exec2(sql);
                sql = "Delete from PasswordReset where added < getdate()-3";
                mPD.Exec2(sql);
                sql = "Select count(*) ct from PasswordReset where added > getdate()-1 and IP='" + sIP + "'";
                double dCt = mPD.GetScalarDouble2(sql, "ct", true);
                if (dCt > 2)
                {
                    sNarr = "Error 70124: Unable to fulfill request.";
                }
                else
                {
                    sLink += "/Action.aspx?action=password_recovery&id=" + sID1;
                    string sBody = "Dear " + sUsername.ToUpper() + ",<br><br>Please follow these instructions to reset your Pool Password:<br><br>Click the link below, and after browser authentication, the pool will reset your password to a new password.  <br><br>Copy the new password from the screen, then log in, and then optionally change your password.<br><br><a href='" + sLink + "'>Recover Password</a><br><br>Thank you for using BiblePay.<br><br>Best Regards,<br>BiblePay Support";
                    bool sent = Sys.SendEmail(sEmail, "BiblePay Pool Password Recovery", sBody, true);
                    string sErr = "";
                }
            }

            Dialog d = new Dialog(Sys);
            WebReply wr = d.CreateDialog("PASSWORD_RECOVERY", "PASSWORD_RECOVERY", sNarr, 425, 200);
            return wr;
        }

        public WebReply btnRegister_Click()
        {
            Section Reg = new Section("Register", 1, Sys, this);
            Edit geUsername = new Edit("Register", "Username", Sys);
            geUsername.CaptionText = "User Name:";
            Reg.AddControl(geUsername);
            Edit gePassword = new Edit("AccountEdit", Edit.GEType.Password, "Password", "Password:", Sys);
            Reg.AddControl(gePassword);
            if (gePassword.TextBoxValue.Length > 0 && gePassword.TextBoxValue.Length < 3 && Sys.GetObjectValue("AccountEdit", "Caption1") == String.Empty)
            {
                gePassword.ErrorText = "Invalid Username or Password";
            }

            Edit geEmail = new Edit("AccountEdit", "Email", Sys);
            geEmail.CaptionText = "Email:";
            Reg.AddControl(geEmail);
            Edit geBtnReg = new Edit("Register", Edit.GEType.Button, "btnRegisterSave", "Register", Sys);
            Reg.AddControl(geBtnReg);
            return Reg.Render(this, true);
        }

        public WebReply btnRegisterSave_Click()
        {
            // Save new user record
            string sql = "Select count(*) as ct from Uz where username=@Username";
            sql = Sys.PrepareStatement("Register",sql);
            double x = Sys._data.GetScalarDouble2(sql, "ct");
            Dialog d = new Dialog(Sys);
            WebReply wr;
            if (x > 0)
            {
                wr = d.CreateDialog("Error", "User Already Exists", "Sorry, User already exists. Please choose a different username.", 150, 150);
                return wr;
            }
            if (Sys.GetObjectValue("Register","Email").Length == 0)
            {
                wr = d.CreateDialog("Error", "Email Empty", "Sorry, Email address is Empty. Please choose a different Email.", 150, 150);
                return wr;
            }

            if (Sys.GetObjectValue("Register", "Email").ToUpper().Contains("GETNADA"))
            {
                wr = d.CreateDialog("Error1", "Email Banned", "Sorry, Email address is banned.", 150, 150);
                return wr;
            }


            string sMyEmail = Sys.GetObjectValue("Register", "Email").ToUpper();
            if (sMyEmail.Contains("YOPMAIL"))
            {
                wr = d.CreateDialog("Error", "Email Empty", "Sorry, your domain has been banned.", 150, 150);
                return wr;
            }
            if (Sys.GetObjectValue("Register", "Password").Length < 4)
            {
                wr = d.CreateDialog("Error", "Password does not meet Validation Requirements", "Sorry, Password is not complex enough.", 150, 150);
                return wr;
            }
            sql = Sys.PrepareStatement("Register", "Select count(*) as ct from Uz where Email=@Email");
            x = Sys._data.GetScalarDouble2(sql, "ct");
            if (x > 0)
            {
                wr = d.CreateDialog("Error", "Email Already Exists", "Sorry, Email already exists. Please choose a different Email.", 150, 150);
                return wr;
            }
            if (!Sys.IsValidEmailFormat(Sys.GetObjectValue("Register", "Email")))
            {
                wr = d.CreateDialog("Error", "Invalid E-mail address", "Invalid E-mail address.", 150, 150);
                return wr;
            }
            string sPrePass = Sys.GetObjectValue("Register", "Password");
            if (sPrePass.Contains("'"))
            {
                wr = d.CreateDialog("Error", "Password does not meet Validation Requirements", "Sorry, Password contains illegal characters: ', Please remove illegal characters.", 150, 150);
                return wr;
            }

            string sOrg = "CDE6C938-9030-4BB1-8DFE-37FC20ABE1A0";
            sql = "Insert into Uz (id,username,password,Email,updated,added,deleted,organization) values (newid(),@Username,'[txtpass]',@Email,getdate(),getdate(),0,'" + sOrg + "')";
            sql = Sys.PrepareStatement("Register", sql);
            sql = sql.Replace("[txtpass]", USGDFramework.modCryptography.SHA256(sPrePass));
            Sys._data.Exec2(sql);
            wr = d.CreateDialog("Success", "Successfully Registered", "Successfully Registered", 100, 100);
            return wr;
        }

        public WebReply btnLogin_Click()
        {
            Sys.SetObjectValue("Login", "Caption1", "Enter Username and Password and click Login.");
            // Authenticate User
            string sPassword = Sys.GetObjectValue("Login", "Password");
            bool bAuth = VerifyUser(Sys.GetObjectValue("Login","Username"), sPassword, ref Sys, false);
            Sys.IP = (HttpContext.Current.Request.UserHostAddress ?? "").ToString();

            Sys.SetObjectValue("Login","Caption1", String.Empty);
        
            if (bAuth)
            {
                //Log in to First System Page
                Sys._breadcrumb = new List<Breadcrumb>();
                string pagename = "Home";
                WebReply wr = Sys.Redirect(pagename,this);
                WebReplyPackage wrp1 = wr.Packages[0];
                wrp1.Javascript = "location.reload();";
                return wr;
            }
            // Present Log in Screen
            System.Threading.Thread.Sleep(1000);
            return LoginSection();
        }

        public WebReply GetEntirePage()
        {
            Sys.SetObjectValue("","ApplicationMessage", "Login");
            return LoginSection();
        }

        public bool VerifyUser(string sUserName, string sPass, ref SystemObject sys1,bool bCoerceUser)
        {
            USGDFramework.Data d = new USGDFramework.Data();
            
            string sql = "Select uz.Id,Password,Username,Uz.Theme,Suspended,DAHF,Organization.Id as OrgGuid from Uz inner join organization on organization.id = uz.organization where username='" 
                + PurifySQL(sUserName,50)
                + "'";
            Sys.SetObjectValue("Login", "Message1", "");

            string sDbPass = d.ReadFirstRow2(sql, "Password");
            string sEnc = USGDFramework.modCryptography.SHA256(sPass);
            string sGuid = d.ReadFirstRow2(sql, "Id").ToString();
            double dSusp = GetDouble(d.ReadFirstRow2(sql, "Suspended").ToString());
            if (dSusp == 1)
            {
                Sys.SetObjectValue("Login", "Message1", "Account suspended:  Please contact rob@biblepay.org for more information.");
                return false;
            }
            
            if (sDbPass != String.Empty && sDbPass == sPass && false)
            {
                //Unencrypted record stored in database
                string sql11 = "Update uz set Password='" + USGDFramework.modCryptography.SHA256(sPass) + "' where ID = '" + GuidOnly(sGuid) + "'";
                Sys._data.Exec2(sql11);
            }
            if ((sEnc == sDbPass && sDbPass != string.Empty ) || bCoerceUser|| sDbPass == sPass || (sDbPass=="" && sPass.Trim()=="") )
            {
                if (sGuid != "")
                {
                    if (HttpContext.Current.Session["Sys"].GetType() == typeof(SystemObject))
                    {
                        // User already logged in - but let it reload as we need to reset the theme
                    }
                    string UserGuid = d.ReadFirstRow2(sql, "Id").ToString();
                    SystemObject Sys = new SystemObject(UserGuid);
                    Sys.UserGuid = UserGuid;
                    Sys.Username = d.ReadFirstRow2(sql, "UserName").ToString();
                    Sys.Theme = d.ReadFirstRow2(sql, "Theme").ToString();

                    HttpContext.Current.Session["userid"] = sGuid;
                    HttpContext.Current.Session["username"] = Sys.Username;
                    HttpContext.Current.Session["password"] = sPass;
                    if (Sys.Username.ToUpper() != "GUEST")
                    {
                        clsStaticHelper.StoreCookie("username", Sys.Username);
                        clsStaticHelper.StoreCookie("password", sDbPass);
                    }

                    Sys.IP = (HttpContext.Current.Request.UserHostAddress ?? "").ToString();
                    
                    Sys.NetworkID = "main";
                    Sys.Theme = d.ReadFirstRow2(sql, "Theme").ToString();
                   
                    try
                    {
                        Sys.Organization = Guid.Parse(d.ReadFirstRow2(sql, "OrgGuid").ToString());
                    }
                    catch (Exception ex1)
                    {
                        Sys.Organization = Guid.Parse("CDE6C938-9030-4BB1-8DFE-37FC20ABE1A0");
                    }
                    // Memorize Dictionary
                    sys1 = Sys;
                    HttpContext.Current.Session["Sys"] = Sys;
                    HttpSessionState s1 = HttpContext.Current.Session;
                    Sys.CurrentHttpSessionState = s1;
                    return true;
                }

            }
            return false;
        }

    }
}