using DeeptiArt.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Configuration;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;

namespace DeeptiArt.Controllers
{
    [RoutePrefix("user/account")]
    public class loginController : baseController
    {
        private readonly dbdeeptiartsEntities db = new dbdeeptiartsEntities();

        [Route("login", Name = "login")]
        public ActionResult Login()
        {
            string referringUrl = Request.UrlReferrer?.ToString();
            Session["ReferringUrl"] = referringUrl;
            return View();
        }

        [HttpPost]
        [Route("login", Name = "loginPost")]
        public ActionResult Login(RegisteredUsersTbl user)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid model data." });
                }

                if (user.Email == null)
                {
                    return Json(new { success = false, message = "Email can not be empty." });
                }

                var useremailInDb = db.RegisteredUsersTbls.Any(m => m.Email == user.Email);

                if (!useremailInDb)
                {
                    return Json(new { success = false, message = "User with this email does not found." });
                }

                if (user.Password == null)
                {
                    return Json(new { success = false, message = "Password can not be empty." });
                }

                var userInDb = db.RegisteredUsersTbls.FirstOrDefault(m => m.Email == user.Email && m.Password == user.Password);
                if (userInDb != null)
                {
                    Session["useremail"] = userInDb.Email;
                    Session["usermobile"] = userInDb.Mobile;
                    Session["userusername"] = userInDb.UserName;
                    Session["userid"] = userInDb.Id;
                    string referringUrl = Session["ReferringUrl"]?.ToString();
                    return Json(new { success = true, redirectUrl = referringUrl });
                }
                return Json(new { success = false, message = "Invalid email or password." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending OTP: {ex.Message}");
                return Json(new { success = false, message = "An error occurred. Please try again later." });
            }
        }

        [Route("forgot-password", Name = "ForgotPassword")]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [Route("forgot-password", Name = "ForgotPasswordPost")]
        public ActionResult ForgotPassword(RegisteredUsersTbl user)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid model data." });
                }

                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    return Json(new { success = false, message = "Please enter a valid email." });
                }
                var loginInfo = db.RegisteredUsersTbls.FirstOrDefault(m => m.Email == user.Email);
                if (loginInfo == null)
                {
                    return Json(new { success = false, message = "Account with this email not found." });
                }
                string signupOTP = GenerateOtp();

                if (SendOtpEmail(signupOTP, user.Email))
                {
                    StoreSignupDataEmail(signupOTP, user.Email);
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to send OTP. Please try again later." });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending OTP: {ex.Message}");
                return Json(new { success = false, message = "An error occurred. Please try again later." });
            }
        }

        [HttpPost]
        [Route("forgot-password/verify-otp", Name = "VerifyForgotPasswordOTPEmailPost")]
        public ActionResult VerifyForgotPasswordOTPEmail(string verifyOTP)
        {
            var signupData = GetStoredSignupDataEmail();

            // Log values for debugging
            System.Diagnostics.Debug.WriteLine($"Entered OTP: {verifyOTP}");
            System.Diagnostics.Debug.WriteLine($"Stored OTP: {signupData?.OTP}");

            if (signupData != null && string.Equals(verifyOTP, signupData.OTP))
            {
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Invalid OTP." });
        }


        [Route("forgot-password/reset-password", Name = "ResetPassword")]
        public ActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        [Route("forgot-password/reset-password", Name = "ResetPasswordPost")]
        public ActionResult ResetPassword(string Password)
        {
            var signupData = GetStoredSignupDataEmail();
            if (signupData != null)
            {
                string email = signupData.Email;
                var user = db.RegisteredUsersTbls.FirstOrDefault(x => x.Email == email);

                if (user != null)
                {
                    user.Password = Password;
                    db.SaveChanges();

                    return Json(new { success = true });
                }
            }
            return Json(new { success = false, message = "Invalid request." });
        }


        public bool SendOtpEmail(string otp, string email)
        {
            try
            {
                SmtpSection smtpSection = (SmtpSection)ConfigurationManager.GetSection("system.net/mailSettings/smtp");
                using (MailMessage mm = new MailMessage(smtpSection.From, email))
                {
                    mm.Subject = "Deepti Art Password Reset - Your OTP is Inside";
                    string resetLink = "https://deeptiart.com/user/account/forgot-password";

                    mm.Body = $@"
<!DOCTYPE html>
<html>
<head>
  <style>
    body {{
      font-family: 'Verdana', 'Arial', sans-serif;
      background-color: #f5f5f5;
      margin: 0;
      padding: 0;
    }}
    .container {{
      max-width: 600px;
      margin: 0 auto;
      padding: 15px;
      background-color: #ffffff;
      box-shadow: 0 3px 10px rgba(0, 0, 0, 0.1);
    }}
    .header {{
      background-color: #ffffff;
      padding: 10px 0;
      text-align: center;
      border-radius: 5px 5px 0 0;
      margin-bottom: 10px;
    }}
    .header-logo img {{
      max-width: 200px;
    }}
    .wlcmsg {{
      font-size: 17px;
      margin: 0;
      color: #000;
      text-align: left;
    }}
    .content {{
      padding: 13px;
      border: 1px solid #e0e0e0;
      border-radius: 0 0 5px 5px;
      background-color: #fff;
    }}
    p {{
      font-size: 16px;
      margin: 0 0 15px;
    }}
    strong {{
      font-weight: bold;
    }}
    .btn {{
      display: inline-block;
      padding: 10px 20px;
      background-color: #e63946;
      color: #fff !important;
      text-decoration: none;
      border-radius: 3px;
      font-weight: 700;
    }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <h1 class='header-logo'><img src='https://deeptiart.com/Content/assets/projectImages/About/logo.png' alt='Deepti Art Logo' width='200'></h1>
    </div>
    <div class='content'>
      <p class='wlcmsg'><strong>Hi,</strong><br />Your OTP for password reset: <strong>{otp}</strong></p>
      <p style='color:#e63946;'>Note: OTP is valid for only 2 minutes.</p>
      <p>Click the following button to validate your password reset OTP:</p>
      <a class='btn' href='{resetLink}'>Verify OTP</a>
    </div>
  </div>
</body>
</html>";
                    mm.IsBodyHtml = true;

                    using (SmtpClient smtp = new SmtpClient())
                    {
                        smtp.Host = smtpSection.Network.Host;
                        smtp.EnableSsl = false;
                        NetworkCredential networkCred = new NetworkCredential(smtpSection.Network.UserName, smtpSection.Network.Password);
                        smtp.UseDefaultCredentials = true;
                        smtp.Credentials = networkCred;
                        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtp.Port = smtpSection.Network.Port;
                        smtp.Send(mm);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending password reset email: {ex.Message}");
                return false;
            }
        }

        private void StoreSignupDataEmail(string otp, string email)
        {
            System.Web.HttpContext.Current.Session.Timeout = 2;
            System.Web.HttpContext.Current.Session["signupDataEmail"] = new { OTP = otp, Email = email };
        }

        private dynamic GetStoredSignupDataEmail()
        {
            return Session["signupDataEmail"] as dynamic;
        }

        private void ClearStoredSignupDataEmail()
        {
            Session.Remove("signupDataEmail");
        }

        private string GenerateOtp()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}