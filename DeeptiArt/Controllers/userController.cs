using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Configuration;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using DeeptiArt.Models;

namespace DeeptiArt.Controllers
{
    [RoutePrefix("user/account")]
    public class userController : baseController
    {
        private readonly dbdeeptiartsEntities db = new dbdeeptiartsEntities();

        [Route("account-profile", Name = "accountprofile")]
        public ActionResult accountprofile()
        {
            if (Session["userid"] == null)
            {
                return RedirectToAction("Index", "Home");
            }
            int userId = Convert.ToInt32(Session["userid"]);
            return View(db.RegisteredUsersTbls.Find(userId));
        }

        [HttpPost]
        [Route("account-profile", Name = "accountprofilepost")]
        public ActionResult accountprofile(RegisteredUsersTbl model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid model data." });
                }

                if (model.First_Name == null)
                {
                    return Json(new { success = false, message = "First Name can not be empty." });
                }
                if (model.Last_Name == null)
                {
                    return Json(new { success = false, message = "Last name can not be empty." });
                }
                if (model.UserName == null)
                {
                    return Json(new { success = false, message = "Username can not be empty." });
                }
                if (model.State == null)
                {
                    return Json(new { success = false, message = "State can not be empty." });
                }
                if (model.City == null)
                {
                    return Json(new { success = false, message = "City can not be empty." });
                }
                if (model.Email == null)
                {
                    return Json(new { success = false, message = "Email can not be empty." });
                }
                if (model.Mobile == null)
                {
                    return Json(new { success = false, message = "Mobile can not be empty." });
                }
                if (model.Address == null)
                {
                    return Json(new { success = false, message = "Address can not be empty." });
                }
                db.Entry(model).State = EntityState.Modified;
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending OTP: {ex.Message}");
                return Json(new { success = false, message = "An error occurred. Please try again later." });
            }
        }

        [Route("account-orders", Name = "accountorders")]
        public ActionResult accountorders()
        {
            int userId = Convert.ToInt32(Session["userid"]);
            if (Session["userid"] == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var query = from order in db.OrderTbls
                        join orderDetail in db.OrderDetailsTbls on order.Id equals orderDetail.OrderID
                        join shipping in db.ShippingDetailsTbls on order.ShippingID equals shipping.ShippingID
                        join billing in db.BillingDetailsTbls on order.BillingID equals billing.Id
                        join product in db.ProductTbls on orderDetail.ProductID equals product.Id
                        join user in db.RegisteredUsersTbls on order.CustomerID equals user.Id
                        join frame in db.FrameTbls on orderDetail.FrameID equals frame.Id
                        where order.CustomerID == userId
                        select new CheckoutViewModel
                        {
                            ProductTbl = product,
                            UserTbl = user,
                            FrameTbl = frame,
                            OrderTbl = order,
                            OrderDetailsTbl = orderDetail,
                            ShippingDetailsTbl = shipping,
                            BillingDetailsTbl = billing
                        };
            var orderedItems = query.ToList();
            ViewBag.MyOrders = orderedItems;
            return View(db.RegisteredUsersTbls.Find(userId));
        }

        [Route("change-password", Name = "changepassword")]
        public ActionResult ChangePassword()
        {
            if (Session["userid"] == null)
            {
                return RedirectToAction("Index", "Home");
            }
            int userId = Convert.ToInt32(Session["userid"]);
            return View(db.RegisteredUsersTbls.Find(userId));
        }

        [HttpPost]
        [Route("change-password", Name = "changepasswordpost")]
        public ActionResult ChangePassword(string OldPassword, string NewPassword, string ConfirmPassword)
        {
            try
            {
                if (Session["userid"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid model data." });
                }

                if (string.IsNullOrWhiteSpace(OldPassword))
                {
                    return Json(new { success = false, message = "Please enter your old password." });
                }

                if (string.IsNullOrWhiteSpace(NewPassword))
                {
                    return Json(new { success = false, message = "Please enter your new password." });
                }

                // Retrieve the user from the database
                var userId = Convert.ToInt32(Session["userid"]);
                var userInDB = db.RegisteredUsersTbls.Find(userId);

                // Verify the old password
                if (!VerifyPassword(OldPassword, userInDB.Password))
                {
                    return Json(new { success = false, message = "Invalid old password." });
                }

                // Ensure the new password meets your criteria (e.g., length, complexity)
                if (!IsPasswordValid(NewPassword))
                {
                    return Json(new { success = false, message = "Please enter a valid password that meets the criteria." });
                }

                // Verify the new password matches the confirmation
                if (NewPassword != ConfirmPassword)
                {
                    return Json(new { success = false, message = "New passwords do not match." });
                }

                // Update the user's password
                userInDB.Password = NewPassword;
                db.Entry(userInDB).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error changing password: {ex.Message}");
                return Json(new { success = false, message = "An error occurred. Please try again later." });
            }
        }

        private bool VerifyPassword(string enteredPassword, string userInDBPassword)
        {
            var userId = Convert.ToInt32(Session["userid"]);
            var userInDB = db.RegisteredUsersTbls.Find(userId);
            if (enteredPassword == userInDB.Password)
            {
                return true;
            }
            return false;
        }

        private bool IsPasswordValid(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                return false;
            }

            if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
            {
                return false;
            }

            return true;
        }

        public ActionResult Logout()
        {
            Session.Abandon();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public ActionResult AddReview(ReviewTable model, HttpPostedFileBase profilephoto)
        {
            if (ModelState.IsValid)
            {
                if (profilephoto != null && profilephoto.ContentLength > 0)
                {
                    int a = db.ReviewTables.Any() ? db.ReviewTables.Max(x => x.Id) + 1 : 1;
                    string fileName = "ReviewerProfileImage" + a + Path.GetExtension(profilephoto.FileName);
                    model.profilephoto = fileName;
                    QualityLevel(profilephoto.InputStream, fileName);
                }
                model.rts = DateTime.Now;
                db.ReviewTables.Add(model);
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        private void QualityLevel(Stream stream, string fname)
        {
            System.Drawing.Image photo = new Bitmap(stream);

            Bitmap bmp1 = new Bitmap(photo, 250, 250);
            ImageCodecInfo jgpEncoder = GetEncoderFrame(ImageFormat.Jpeg);
            System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
            EncoderParameters myEncoderParameters = new EncoderParameters(1);
            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 90L);
            myEncoderParameters.Param[0] = myEncoderParameter;
            bmp1.Save(Server.MapPath("~/Content/Assets/projectImages/ReviewerProfileImage/" + fname), jgpEncoder, myEncoderParameters);
            bmp1.Dispose();
        }

        private ImageCodecInfo GetEncoderFrame(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        [HttpPost]
        public ActionResult AskAboutProduct(ProductEnquiryTbl enquiry, FormCollection enq)
        {
            if (ModelState.IsValid)
            {
                SmtpSection smtpSection = (SmtpSection)ConfigurationManager.GetSection("system.net/mailSettings/smtp");

                using (MailMessage mm = new MailMessage(smtpSection.From, "mail@deeptiart.com"))
                {
                    mm.Subject = "Art Information Enquiry To Deepti Art";
                    mm.Body = @"<!DOCTYPE html>
<html>
<head>
  <style>
    body {
      font-family: Verdana, Arial, sans-serif;
      background-color: #f5f5f5;
      margin: 0;
      padding: 0;
    }
    .container {
      max-width: 600px;
      margin: 0 auto;
      padding: 15px;
      background-color: #ffffff;
      box-shadow: 0 3px 10px rgba(0, 0, 0, 0.1);
    }
    .header {
      color: #ffffff;
      padding: 10px 0;
      text-align: center;
      border-radius: 5px 5px 0 0;
      margin-bottom:7px;
    }
    .header-logo{
      margin-bottom:5px;
    }
    .wlcmsg {
      font-size: 17px;
      margin: 0;
      color: #000000;
      text-align:left;
    }
    .content {
      padding: 13px;
      border: 1px solid #e0e0e0;
      border-radius: 5px;
    }
    p {
      font-size: 15px;
      margin: 0 0 15px;
    }
    strong {
      font-weight: bold;
    }
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <h1 class='header-logo'><img src='https://deeptiart.com/Content/assets/projectImages/About/logo.png' alt='Deepti Art Logo' width='200'></h1>
      <p class='wlcmsg'><strong>Hi,</strong><br />This email is regarding a new inquiry received from Deepti Art.</p>
    </div>
    <div class='content'>
      <p><strong>Customer Enquired Art Id:</strong> " + enquiry.PId + @"</p>
      <p><strong>Customer Enquired Art Name:</strong><br /> " + enq["artName"] + @"</p>
      <p><strong>Customer Name:</strong><br /> " + enquiry.uname + @"</p>
      <p><strong>Customer Mobile Number:</strong><br /> " + enquiry.umob + @"</p>
      <p><strong>Customer Email:</strong><br /> " + enquiry.uemail + @"</p>
      <p><strong>Customer Enquiry Subject:</strong><br /> " + enquiry.usub + @"</p>
      <p><strong>Customer Enquiry Message:</strong><br /> " + enquiry.umsg + @"</p>
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

                enquiry.rts = DateTime.Now;
                db.ProductEnquiryTbls.Add(enquiry);
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
}