using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Configuration;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using DeeptiArt.Models;

namespace DeeptiArt.Controllers
{
    public class contactController : baseController
    {
        private dbdeeptiartsEntities db = new dbdeeptiartsEntities();

        // GET: contact
        public ActionResult Index()
        {
            return View(db.AboutDetailsTbls.FirstOrDefault());
        }

        [HttpPost]
        public ActionResult SendEnquiry(ContactEnquiryTbl enquiry)
        {
            if (ModelState.IsValid)
            {
                SmtpSection smtpSection = (SmtpSection)ConfigurationManager.GetSection("system.net/mailSettings/smtp");

                using (MailMessage mm = new MailMessage(smtpSection.From, "mail@deeptiart.com"))
                {
                    mm.Subject = "Contact Enquiry To Deepti Art";
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
      <p class='wlcmsg'><strong>Hi,</strong><br />This email is regarding a new contact inquiry received from Deepti Art.</p>
    </div>
    <div class='content'>
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
                db.ContactEnquiryTbls.Add(enquiry);
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
        
        [HttpPost]
        public ActionResult RequestYourArtwork(FormCollection enquiry, HttpPostedFileBase uimg)
        {
            SmtpSection smtpSection = (SmtpSection)ConfigurationManager.GetSection("system.net/mailSettings/smtp");

            using (MailMessage mm = new MailMessage(smtpSection.From, "mail@deeptiart.com"))
            {
                mm.Subject = "Custom Artwork Request Enquiry To Deepti Art";
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
          margin-bottom: 7px;
        }
        .header-logo {
          margin-bottom: 5px;
        }
        .wlcmsg {
          font-size: 17px;
          margin: 0;
          color: #000000;
          text-align: left;
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
          <p class='wlcmsg'><strong>Hi,</strong><br />This email is regarding a new custom artwork request inquiry received from Deepti Art.</p>
        </div>
        <div class='content'>
          <p><strong>Customer Name:</strong><br /> " + enquiry["uname"] + @"</p>
          <p><strong>Customer Mobile Number:</strong><br /> " + enquiry["umob"] + @"</p>
          <p><strong>Customer Email:</strong><br /> " + enquiry["uemail"] + @"</p>
          <p><strong>Custom Artwork Request Subject:</strong><br /> " + enquiry["usub"] + @"</p>
          <p><strong>Custom Artwork Request Message:</strong><br /> " + enquiry["umsg"] + @"</p>
          <p><strong>Requested Artwork Preview:</strong><br /></p>
          <img src='cid:imageAttachment' alt='Custom Artwork' width='400'>
        </div>
      </div>
    </body>
    </html>";

                // Set the HTML body
                AlternateView htmlView = AlternateView.CreateAlternateViewFromString(mm.Body, null, "text/html");
                mm.AlternateViews.Add(htmlView);

                // Create the image attachment
                if (uimg != null && uimg.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(uimg.FileName);
                    Attachment imageAttachment = new Attachment(uimg.InputStream, fileName)
                    {
                        ContentId = "imageAttachment"
                    };
                    mm.Attachments.Add(imageAttachment);
                }

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

            return RedirectToAction("Index");
        }
    }
}
