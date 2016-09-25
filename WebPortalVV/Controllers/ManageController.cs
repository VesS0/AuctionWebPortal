using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using WebPortalVV.Models;
using System.Data.Entity;
using System.Net.Mail;
using System.Collections.Generic;

namespace WebPortalVV.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        private ApplicationDbContext db = new ApplicationDbContext();
        private UserModel dbUser = new UserModel();

         public ManageController()
        {
        }

        public ManageController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set 
            { 
                _signInManager = value; 
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Manage/Index
        public async Task<ActionResult> Index(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
                : message == ManageMessageId.Error ? "An error has occurred."
                : message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
                : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
                : "";

            var userId = User.Identity.GetUserId();
           
            var model = new IndexViewModel
            {
                HasPassword = HasPassword(),
                PhoneNumber = await UserManager.GetPhoneNumberAsync(userId),
                TwoFactor = await UserManager.GetTwoFactorEnabledAsync(userId),
                Logins = await UserManager.GetLoginsAsync(userId),
                BrowserRemembered = await AuthenticationManager.TwoFactorBrowserRememberedAsync(userId),
                User = UserManager.FindById(userId)
            };
            return View(model);
        }

        //
        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            ManageMessageId? message;
            var result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("ManageLogins", new { Message = message });
        }

        //
        // GET: /Manage/AddPhoneNumber
        public ActionResult AddPhoneNumber()
        {
            return View();
        }

        //
        // POST: /Manage/AddPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            // Generate the token and send it
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), model.Number);
            if (UserManager.SmsService != null)
            {
                var message = new IdentityMessage
                {
                    Destination = model.Number,
                    Body = "Your security code is: " + code
                };
                await UserManager.SmsService.SendAsync(message);
            }
            return RedirectToAction("VerifyPhoneNumber", new { PhoneNumber = model.Number });
        }

        //
        // POST: /Manage/EnableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EnableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), true);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // POST: /Manage/DisableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DisableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), false);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // GET: /Manage/VerifyPhoneNumber
        public async Task<ActionResult> VerifyPhoneNumber(string phoneNumber)
        {
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), phoneNumber);
            // Send an SMS through the SMS provider to verify the phone number
            return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }

        //
        // POST: /Manage/VerifyPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePhoneNumberAsync(User.Identity.GetUserId(), model.PhoneNumber, model.Code);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.AddPhoneSuccess });
            }
            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "Failed to verify phone");
            return View(model);
        }

        //
        // GET: /Manage/RemovePhoneNumber
        public async Task<ActionResult> RemovePhoneNumber()
        {
            var result = await UserManager.SetPhoneNumberAsync(User.Identity.GetUserId(), null);
            if (!result.Succeeded)
            {
                return RedirectToAction("Index", new { Message = ManageMessageId.Error });
            }
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", new { Message = ManageMessageId.RemovePhoneSuccess });
        }

        //
        // GET: /Manage/ChangePassword
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
            }
            AddErrors(result);
            return View(model);
        }

        //
        // GET: /Manage/SetPassword
        public ActionResult SetPassword()
        {
            return View();
        }

        //
        // POST: /Manage/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
                if (result.Succeeded)
                {
                    var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                    if (user != null)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                    }
                    return RedirectToAction("Index", new { Message = ManageMessageId.SetPasswordSuccess });
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Manage/ManageLogins
        public async Task<ActionResult> ManageLogins(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user == null)
            {
                return View("Error");
            }
            var userLogins = await UserManager.GetLoginsAsync(User.Identity.GetUserId());
            var otherLogins = AuthenticationManager.GetExternalAuthenticationTypes().Where(auth => userLogins.All(ul => auth.AuthenticationType != ul.LoginProvider)).ToList();
            ViewBag.ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1;
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins = otherLogins
            });
        }

        //
        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new AccountController.ChallengeResult(provider, Url.Action("LinkLoginCallback", "Manage"), User.Identity.GetUserId());
        }

        //
        // GET: /Manage/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
            {
                return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
            }
            var result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
            return result.Succeeded ? RedirectToAction("ManageLogins") : RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }
        public ActionResult ChangeName()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeName(ChangeNameViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = UserManager.FindById(User.Identity.GetUserId());
                if (user.Name != model.Name)
                {
                    user.Name = model.Name;
                    UserManager.Update(user);
                }
                return RedirectToAction("Index");
            }
            return View(model);
        }

        public ActionResult ChangeSurname()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeSurname(ChangeSurnameViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = UserManager.FindById(User.Identity.GetUserId());
                if (model.Surname != user.Surname)
                {
                    user.Surname = model.Surname;
                    UserManager.Update(user);
                }
                return RedirectToAction("Index");
            }
            return View(model);
        }

        public ActionResult ChangeEmail()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeEmail(ChangeEmailViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = UserManager.FindById(User.Identity.GetUserId());
                if (user.Email != model.Email)
                {
                    user.Email = model.Email;
                    user.UserName = model.Email;
                    UserManager.Update(user);
                    return RedirectToAction("Index");
                }

            }
            return View(model);
        }





        public ActionResult AddTokens()
        {
            AspNetUser user = dbUser.AspNetUsers.Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
            user.Tokens = user.Tokens + 10;

            dbUser.Entry(user).State = EntityState.Modified;
            dbUser.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpGet]
        [AllowAnonymous]
        public HttpStatusCodeResult Payment(string clientid, string status, int amount, decimal enduserprice)
        {

            Order order = new Order();

            ConnectOrderAndPacket(order, (int)amount, (double)enduserprice);

            order.timeCreated = DateTime.Now;
            order.orderingUser = db.Users.Find(clientid);

            if (status == "success")
            {
                
                order.status = OrderStatus.APPROVED;
               

                db.Orders.Add(order);
                db.SaveChanges();

                //sendMail(User.Identity.Name);

                AspNetUser user = dbUser.AspNetUsers.Find(clientid);
                user.Tokens = user.Tokens + order.packet.numberOfTokens;

                dbUser.Entry(user).State = EntityState.Modified;
                dbUser.SaveChanges();

                /*AspNetUser user = dbUser.AspNetUsers.Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

                Order order = new Order();
                order.Tokens = (int)amount;
                order.Price = (double)enduserprice;
                order.Status = OrderState.Processing;
                order.CreationDT = DateTime.Now;
                if (user != null)
                    order.IDUser = user.Id;
                else
                    order.IDUser = clientid;

                db.Orders.Add(order);
                db.SaveChanges();*/
            }
            else
            {

                order.status = OrderStatus.CANCELED;

                db.Orders.Add(order);
                db.SaveChanges();
            }

            return new HttpStatusCodeResult(200);
        }

         public void ConnectOrderAndPacket(Order order, int tokens, double price)
        {
            List<Packet> packetList=db.Packets.ToList();

            foreach (Packet packet in packetList)
            {
                if (packet.numberOfTokens == tokens && packet.packetPrice == price) { 
                    order.packet = packet;
                    return;
                }
            }

            Packet newPacket = new Packet();

            newPacket.numberOfTokens = tokens;
            newPacket.packetPrice = price;
            newPacket.packetName = "New_Pack" + DateTime.Now.ToString();

            db.Packets.Add(newPacket);
            db.SaveChanges();
            order.packet = newPacket;


        }

        public ActionResult PaymentSucc()
        {
            //AspNetUser user = dbUser.AspNetUsers.Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

            //Order order = new Order();
            /////////////////////order.Tokens = 5;//
            /////////////////////order.Price = 50;//
            //order.status = OrderStatus.APPROVED;
            //order.timeCreated = DateTime.Now;
            //order.orderingUser = db.Users.Find(user.Id);

            //db.Orders.Add(order);
            //db.SaveChanges();

            ////sendMail(User.Identity.Name);

            //user.Tokens = user.Tokens + order.packet.numberOfTokens;

            //dbUser.Entry(user).State = EntityState.Modified;
            //dbUser.SaveChanges();
            /*
            AspNetUser user = dbUser.AspNetUsers.Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
            var orders = from o in db.Orders
                         where o.IDUser == user.Id
                         && o.Status == OrderState.Processing
                         select o;
            Order order = orders.FirstOrDefault();
            user.Tokens = user.Tokens + order.Tokens;
            order.Status = OrderState.Accepted;

            db.Entry(order).State = EntityState.Modified;
            dbUser.Entry(user).State = EntityState.Modified;
            db.SaveChanges();
            dbUser.SaveChanges();
            */
            try
            {
                sendMail(User.Identity.Name);
            }
            catch (Exception e) { }
            return RedirectToAction("Index");
        }

        public ActionResult PaymentFail()
        {
            //AspNetUser user = dbUser.AspNetUsers.Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

            //Order order = new Order();
            /////////////////////order.Tokens = 5;//
            /////////////////////order.Price = 50;//
            //order.status = OrderStatus.CANCELED;
            //order.timeCreated = DateTime.Now;
            //order.orderingUser = db.Users.Find(user.Id);

            //db.Orders.Add(order);
            //db.SaveChanges();
            /*
            AspNetUser user = dbUser.AspNetUsers.Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
            var orders = from o in db.Orders
                         where o.IDUser == user.Id
                         && o.Status == OrderState.Processing
                         select o;
            Order order = orders.FirstOrDefault();
            order.Status = OrderState.Denied;

            db.Entry(order).State = EntityState.Modified;
            db.SaveChanges();
            */
            return View();
        }


        public static void sendMail(string email)
        {
            MailMessage mail = new MailMessage();

            mail.From = new MailAddress("BidWhatYouNeed@auctions.com", "Order Succesful");
            mail.To.Add(email);
            mail.Subject = "Order Approved";
            mail.Body = "Tokens have been succesfully added to your account.";
            using (var smtp = new SmtpClient())
            {
                smtp.Send(mail);
            }

        }




        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        private bool HasPhoneNumber()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PhoneNumber != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }

#endregion
    }
}