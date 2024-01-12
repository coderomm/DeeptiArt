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
    public class signupController : baseController
    {
        private readonly dbdeeptiartsEntities db = new dbdeeptiartsEntities();

        [Route("signup", Name = "signup")]
        public ActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        [Route("signup", Name = "signupPost")]
        public ActionResult Signup(RegisteredUsersTbl user)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid model data." });
                }
                if (db.RegisteredUsersTbls.Any(x => x.Email == user.Email))
                {
                    return Json(new { success = false, message = "Account with this email already registered." });
                }
                if (user.Email == null)
                {
                    return Json(new { success = false, message = "Email can not be empty." });
                }
                if (user.Mobile == null)
                {
                    return Json(new { success = false, message = "Mobile can not be empty." });
                }
                string signupOTP = GenerateOtp();

                if (SendOtpEmail(signupOTP, user.Email))
                {
                    StoreSignupDataEmail(signupOTP, user.Email, user.Mobile);
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

        [Route("signup/verify-otp", Name = "VerifysignupOTP")]
        public ActionResult VerifysignupOTP()
        {
            return View();
        }

        [HttpPost]
        [Route("signup/verify-otp", Name = "VerifysignupOTPPost")]
        public ActionResult VerifysignupOTP(string verifyOTP)
        {
            var signupData = GetStoredSignupDataEmail();

            if (signupData != null && string.Equals(verifyOTP, signupData.OTP))
            {
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Invalid OTP." });
        }

        [Route("signup/password", Name = "Password")]
        public ActionResult Password()
        {
            return View();
        }

        [HttpPost]
        [Route("signup/password", Name = "PasswordPost")]
        public ActionResult Password(string Password)
        {
            var signupData = GetStoredSignupDataEmail();
            if (signupData != null)
            {
                var user = new RegisteredUsersTbl
                {
                    UserName = signupData.Email,
                    Mobile = signupData.Mobile,
                    Password = Password,
                    Email = signupData.Email,
                    rts = DateTime.Now
                };
                db.RegisteredUsersTbls.Add(user);
                db.SaveChanges();
                ClearStoredSignupDataEmail();

                return Json(new { success = true });
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
                    mm.Subject = "Deepti Art Signup Verification - Your OTP is Inside";
                    string resetLink = "https://deeptiart.com/user/account/signup/verify-otp";
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
      <p class='wlcmsg'><strong>Hi,</strong><br />Your OTP for Signup validation: <strong>{otp}</strong></p>
      <p style='color:#e63946;'>Note: OTP is valid for only 2 minutes.</p>
      <p>Click the following button to validate your OTP:</p>
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

        private void StoreSignupDataEmail(string otp, string email, string mobile)
        {
            System.Web.HttpContext.Current.Session.Timeout = 2;
            System.Web.HttpContext.Current.Session["signupDataEmail"] = new { OTP = otp, Email = email, Mobile = mobile };
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