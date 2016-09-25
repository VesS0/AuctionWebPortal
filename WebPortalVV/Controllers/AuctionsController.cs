using Microsoft.AspNet.SignalR;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using WebPortalVV.Hubs;
using WebPortalVV.Models;

namespace WebPortalVV.Controllers
{
    public class AuctionsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private UserModel dbUser = new UserModel();
   

        public AuctionStatus SOLD { get; private set; }

        public ActionResult Index(string currentFilter, string currentFilterMin, string currentFilterMax, string currentFilterStatus, string searchString, string min, string max, string state, int? page)
        {
            //checkState();
            var auctions = from a in db.Auctions
                           where a.status!= AuctionStatus.DRAFT
                           && a.status!=AuctionStatus.DELETED
                           select a;

            if (searchString != null || min != null || max != null || state != null)
                page = 1;
            else
            {
                searchString = currentFilter;
                min = currentFilterMin;
                max = currentFilterMax;
                state = currentFilterStatus;
            }


            ViewBag.CurrentFilter = searchString;
            ViewBag.CurrentFilterMin = min;
            ViewBag.CurrentFilterMax = max;
            ViewBag.currentFilterStatus = state;

            if (!String.IsNullOrEmpty(searchString))
            {
                var strings = searchString.Split(' ');
                foreach (var splitString in strings)
                    auctions = auctions.Where(s => s.product.productName.Contains(splitString));
            }
            if (!String.IsNullOrEmpty(min))
            {
                double val = Double.Parse(min);
                auctions = auctions.Where(a => a.startingPrice >= val);
            }
            if (!String.IsNullOrEmpty(max))
            {
                double val = Double.Parse(max);
                auctions = auctions.Where(a => a.startingPrice <= val);
            }
            if (!String.IsNullOrEmpty(state))
            {
                AuctionStatus status = (AuctionStatus)Enum.Parse(typeof(AuctionStatus), state);
                auctions = auctions.Where(a => a.status == status);
            }

            auctions = auctions.OrderByDescending(a => a.timeOpened).ThenBy(a => a.timeCreated);

            int pageSize = 8;
           // if (String.IsNullOrEmpty(searchString) && String.IsNullOrEmpty(min) && String.IsNullOrEmpty(max) && String.IsNullOrEmpty(state))
           //     pageSize = 4;
            int pageNumber = (page ?? 1);
            return View(auctions.ToPagedList(pageNumber, pageSize));
        }

        [System.Web.Mvc.Authorize(Roles = "Admin")]
        public ActionResult Auctions(int? page)
        {
            //checkState();
            var auctions = from a in db.Auctions
                           where a.status != AuctionStatus.DELETED
                           select a;

            auctions = auctions.OrderByDescending(a => a.timeCreated);

            int pageSize = 8;
            int pageNumber = (page ?? 1);
            return View(auctions.ToPagedList(pageNumber, pageSize));
        }

        public void checkState()
        {
            var auctions = from a in db.Auctions
                           where a.status == AuctionStatus.OPEN
                           && a.timeClosed <= DateTime.Now
                           select a;
            foreach (Auction a in auctions)
            {
                if (a.bids != null && a.bids.Count > 0) { 
                    a.status = AuctionStatus.SOLD;
                }
                else { 
                    a.status = AuctionStatus.EXPIRED;
                }
                db.Entry(a).State = EntityState.Modified;
            }
            db.SaveChanges();
        }

        // GET: Auctions/Details/5
        public ActionResult Details(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Auction auction = db.Auctions.Find(id);
            if (auction == null)
            {
                return HttpNotFound();
            }
            return View(auction);
        }

        // GET: Auctions/Create
        [System.Web.Mvc.Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            List<SelectListItem> ddl =new List<SelectListItem>();
            foreach (var pr in db.Products.ToList())
            {
                if (notInAuctionOrExpired(pr.idProduct))
                    ddl.Add(new SelectListItem() { Text = pr.productName, Value = pr.idProduct.ToString() });
            }

            ViewBag.products = ddl;

            return View();
        }

        public bool notInAuctionOrExpired(long idProduct)
        {

           var r= (from a in db.Auctions
                    where a.product.idProduct == idProduct && 
                    !(a.status==AuctionStatus.EXPIRED)
                    select a).ToList();

            if (r.Count>0) return false;
            return true;
        }

        [System.Web.Mvc.Authorize(Roles = "User")]
        public string Bid(int id, string username)
        {
            if (System.Web.HttpContext.Current.User != null && System.Web.HttpContext.Current.User.IsInRole("User"))
            {
                UserModel dbUser = new UserModel();
                AspNetUser user = dbUser.AspNetUsers.Where(u => u.UserName == username).FirstOrDefault();
                if (user.Tokens < 1)
                    return "No more tokens!";

                Auction auct = db.Auctions.Find(id);
                if (auct.status != AuctionStatus.OPEN || auct.timeClosed <= DateTime.Now) return "Bid failed. Auction is no longer open";
                Bid bid = new Bid();

                bid.timeSent = DateTime.Now;
                bid.auction = auct;
                bid.biddingUser = db.Users.Find(user.Id);

                auct.bids.Add(bid);
                string userBefore = auct.lastBidder;
                auct.lastBidder = username;
                auct.totalPriceIncrease = auct.totalPriceIncrease + auct.bidIncrement;

                if ((auct.timeClosed - DateTime.Now).Value.TotalSeconds <= 10)
                    auct.timeClosed = DateTime.Now.AddSeconds(10);

                user.Tokens = user.Tokens - 1;
                try
                {
                    //if (auct.status != AuctionStatus.OPEN || auct.timeClosed<=DateTime.Now) return "Bid failed. Auction is no longer opened";
                    db.Bids.Add(bid);
                    db.Entry(auct).State = EntityState.Modified;
                    db.SaveChanges();
                    try
                    {
                        dbUser.Entry(user).State = EntityState.Modified;
                        dbUser.SaveChanges();
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        return "Bid failed, User concurency error";
                    }
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    db.Bids.Remove(bid);
                    db.Entry(auct).State = EntityState.Unchanged;
                    dbUser.Entry(user).State = EntityState.Unchanged;
                    auct.bids.Remove(bid);
                    auct.lastBidder = userBefore;
                    auct.totalPriceIncrease = auct.totalPriceIncrease - auct.bidIncrement;
                    user.Tokens = user.Tokens + 1;

                    return "Bid Failed, Auction concurency error";
                }
               

                /*try
                {
                    db.Bids.Add(bid);
                    db.Entry(auct).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    return "Bid failed. Try again";
                }*/
                return "OK";
            }
            else return "Log in first!";

        }

        // POST: Auctions/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [System.Web.Mvc.Authorize(Roles = "Admin")]
        public ActionResult Create(AuctionCreate auctionCreate)
        {
            if (ModelState.IsValid)
            {
                Auction auction = getAuctionFromAuctionCreate(auctionCreate);

                db.Auctions.Add(auction);
                db.SaveChanges();
                return RedirectToAction("Auctions");
            }

            return View(auctionCreate);
        }

        private AuctionCreate getAuctionCreateFromAuction(Auction auction)
        {
            AuctionCreate auctionCreate = new AuctionCreate();

            auctionCreate.idAuction = auction.product.idProduct;
            //auction.product = new Product();
            //Product pk=(Product)a.First( );

            ////pk.auctions.Add(auction);

            //auction.product.image = pk.image;
            //auction.product.ImageToUpload = pk.ImageToUpload;
            //auction.product.productName = pk.productName;

            auctionCreate.secondsLasting = auction.secondsLasting;
            auctionCreate.startingPrice = auction.startingPrice;

            auctionCreate.bidIncrement = auction.bidIncrement;
            return auctionCreate;
        }


        private Auction getAuctionFromAuctionCreate(AuctionCreate auctionCreate)
        {
            Auction auction = new Auction();

            auction.product = (Product)((from p in db.Products where p.idProduct == auctionCreate.idProduct select p).ToList().First());
            //auction.product = new Product();
            //Product pk=(Product)a.First( );

            ////pk.auctions.Add(auction);

            //auction.product.image = pk.image;
            //auction.product.ImageToUpload = pk.ImageToUpload;
            //auction.product.productName = pk.productName;

            auction.secondsLasting = auctionCreate.secondsLasting;
            auction.startingPrice = auctionCreate.startingPrice;

            auction.timeCreated = DateTime.Now;
            auction.timeOpened = null;
            auction.timeClosed = null;
            auction.status = AuctionStatus.DRAFT;
            auction.bidIncrement = auctionCreate.bidIncrement;
            return auction;
        }

        // GET: Auctions/Edit/5
        [System.Web.Mvc.Authorize(Roles = "Admin")]
        public ActionResult Edit(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Auction auction = db.Auctions.Find(id);
            if (auction == null)
            {
                return HttpNotFound();
            }
            return View(auction);
        }

        // POST: Auctions/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [System.Web.Mvc.Authorize(Roles = "Admin")]
        public ActionResult Edit([Bind(Include = "idAuction,secondsLasting,timeCreated,timeOpened,timeClosed,startingPrice,totalPriceIncrease,bidIncrement,status,RowVersion")] Auction auction)
        {

            if (ModelState.IsValid)
            {
                db.Entry(auction).State = EntityState.Modified;
                try {
                    db.SaveChanges();
                }
                catch (Exception e){ }
                return RedirectToAction("Auctions");
            }
            return View(auction);
        }

        [System.Web.Mvc.Authorize(Roles = "Admin")]
        public ActionResult ChangePrice(int startingPrice, long idAuction)
        {
            ChangePriceViewModel cpvm = new ChangePriceViewModel();
            cpvm.idAuction = idAuction;
            cpvm.startingPrice = startingPrice;

            return View(cpvm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePrice(ChangePriceViewModel model)
        {

            if (ModelState.IsValid)
            {
                Auction auction = db.Auctions.Find(model.idAuction);
                auction.startingPrice = model.startingPrice;
                db.Entry(auction).State = EntityState.Modified;
                db.SaveChanges();
               
                return RedirectToAction("Auctions");
            }
            return View(model);
        }


        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[System.Web.Mvc.Authorize(Roles = "Admin")]
        //public async Task<ActionResult> Edit(int? id, byte[] rowVersion)
        //{
        //    string[] fieldsToBind = new string[] { "idAuction,secondsLasting,timeCreated,timeOpened,timeClosed,startingPrice,totalPriceIncrease,bidIncrement,status,RowVersion" };

        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }

        //    var auctionToUpdate = await db.Auctions.FindAsync(id);
        //    if (auctionToUpdate == null)
        //    {
        //        Auction deletedAuction = new Auction();
        //        TryUpdateModel(deletedAuction, fieldsToBind);
        //        ModelState.AddModelError(string.Empty,
        //            "Unable to save changes. The department was deleted by another user.");
        //        //ViewBag.InstructorID = new SelectList(db.Instructors, "ID", "FullName", deletedDepartment.InstructorID);
        //        return View(deletedAuction);
        //    }

        //    if (TryUpdateModel(auctionToUpdate, fieldsToBind))
        //    {
        //        try
        //        {
        //            db.Entry(auctionToUpdate).OriginalValues["RowVersion"] = rowVersion;
        //            await db.SaveChangesAsync();

        //            return RedirectToAction("Index");
        //        }
        //        catch (DbUpdateConcurrencyException ex)
        //        {
        //            var entry = ex.Entries.Single();
        //            var clientValues = (Auction)entry.Entity;
        //            var databaseEntry = entry.GetDatabaseValues();
        //            if (databaseEntry == null)
        //            {
        //                ModelState.AddModelError(string.Empty,
        //                    "Unable to save changes. The department was deleted by another user.");
        //            }
        //            else
        //            {
        //                var databaseValues = (Auction)databaseEntry.ToObject();

        //                //if (databaseValues.bidIncrement != clientValues.bidIncrement)
        //                //    ModelState.AddModelError("Name", "Current value: "
        //                //        + databaseValues.bidIncrement);
        //                //if (databaseValues. != clientValues.Budget)
        //                //    ModelState.AddModelError("Budget", "Current value: "
        //                //        + String.Format("{0:c}", databaseValues.Budget));
        //                //if (databaseValues.StartDate != clientValues.StartDate)
        //                //    ModelState.AddModelError("StartDate", "Current value: "
        //                //        + String.Format("{0:d}", databaseValues.StartDate));
        //                //if (databaseValues.InstructorID != clientValues.InstructorID)
        //                //    ModelState.AddModelError("InstructorID", "Current value: "
        //                //        + db.Instructors.Find(databaseValues.InstructorID).FullName);
        //                //ModelState.AddModelError(string.Empty, "The record you attempted to edit "
        //                //    + "was modified by another user after you got the original value. The "
        //                //    + "edit operation was canceled and the current values in the database "
        //                //    + "have been displayed. If you still want to edit this record, click "
        //                //    + "the Save button again. Otherwise click the Back to List hyperlink.");
        //                //departmentToUpdate.RowVersion = databaseValues.RowVersion;
        //            }
        //        }
        //        catch (RetryLimitExceededException /* dex */)
        //        {
        //            //Log the error (uncomment dex variable name and add a line here to write a log.
        //            ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
        //        }
        //    }
        //    //ViewBag.InstructorID = new SelectList(db.Instructors, "ID", "FullName", auctionToUpdate.InstructorID);
        //    return View(auctionToUpdate);
        //}

        private void trySaving()
        {
            bool saveFailed;
            do
            {
                saveFailed = false;
                try
                {
                    db.SaveChanges();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    saveFailed = true;

                    // Get the current entity values and the values in the database 
                    var entry = ex.Entries.Single();
                    var currentValues = entry.CurrentValues;
                    var databaseValues = entry.GetDatabaseValues();

                    // Choose an initial set of resolved values. In this case we 
                    // make the default be the values currently in the database. 
                   // var resolvedValues = databaseValues.Clone();

                    // Have the user choose what the resolved values should be 
                    //HaveUserResolveConcurrency(currentValues, databaseValues, resolvedValues);

                    // Update the original values with the database values and 
                    // the current values with whatever the user choose. 
                    //entry.OriginalValues.SetValues(databaseValues);
                    //entry.CurrentValues.SetValues(resolvedValues);
                }
            } while (saveFailed);

        }

        public void HaveUserResolveConcurrency(DbPropertyValues currentValues,
                                       DbPropertyValues databaseValues,
                                       DbPropertyValues resolvedValues)
        {
            // Show the current, database, and resolved values to the user and have 
            // them edit the resolved values to get the correct resolution. 
        }








        // GET: Auctions/Delete/5
        [System.Web.Mvc.Authorize(Roles = "Admin")]
        public ActionResult Delete(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Auction auction = db.Auctions.Find(id);
            if (auction == null)
            {
                return HttpNotFound();
            }
            return View(auction);
        }

        // POST: Auctions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [System.Web.Mvc.Authorize(Roles = "Admin")]
        public ActionResult DeleteConfirmed(int id)
        {
            Auction auction = db.Auctions.Find(id);
            if (auction.status == AuctionStatus.READY)
            {
                auction.status = AuctionStatus.DELETED;
                db.Entry(auction).State = EntityState.Modified;
                db.SaveChanges();
            }
            return RedirectToAction("Auctions");
        }

        // GET: Auctions/Ready/5
        [System.Web.Mvc.Authorize(Roles = "Admin")]
        public ActionResult Ready(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Auction auction = db.Auctions.Find(id);
            if (auction == null)
            {
                return HttpNotFound();
            }
            if (auction.status == AuctionStatus.DRAFT)
            {
                auction.status = AuctionStatus.READY;

                db.Entry(auction).State = EntityState.Modified;
                db.SaveChanges();
            }
            return RedirectToAction("Auctions");
        }
        // GET: Auctions/Open/5
        [System.Web.Mvc.Authorize(Roles = "Admin")]
        public ActionResult Open(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Auction auction = db.Auctions.Find(id);
            if (auction == null)
            {
                return HttpNotFound();
            }
            if (auction.status == AuctionStatus.READY)
            {
                auction.status = AuctionStatus.OPEN;
                auction.timeOpened = DateTime.Now;
                auction.timeClosed = DateTime.Now.AddSeconds(auction.secondsLasting);

                db.Entry(auction).State = EntityState.Modified;
                db.SaveChanges();

                Task.Factory.StartNew(() =>
                {
                    ApplicationDbContext newDB = new ApplicationDbContext();
                    Auction task = null;
                    Boolean finished = false;
                    while (!finished)
                    {
                        newDB = new ApplicationDbContext();
                        task = newDB.Auctions.Find(id);
                        int time = (int)(task.timeClosed - DateTime.Now).Value.TotalSeconds;
                        if (time > 0)
                            Thread.Sleep(time * 1000);
                        else
                            finished = true;
                    }

                    if (task.bids.Count > 0) { 
                        task.status = AuctionStatus.SOLD;
                    }
                    else { 
                        task.status = AuctionStatus.EXPIRED;
                    }
                    newDB.Entry(task).State = EntityState.Modified;
                    newDB.SaveChanges();

                    var hub = GlobalHost.ConnectionManager.GetHubContext<AuctionHub>();
                    hub.Clients.All.closeAuction(id);
                });
            }
            return RedirectToAction("Auctions");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        [System.Web.Mvc.Authorize(Roles = "User")]
        public ActionResult WonAuctions(int? page)
        {
            AspNetUser user = dbUser.AspNetUsers.Where(u => u.UserName == User.Identity.Name).FirstOrDefault();

            var auctions = (from a in db.Auctions
                           where a.status == AuctionStatus.SOLD
                           select a).ToList();

            List<Auction> myAyctions = new List<Auction>();

            foreach (Auction a in auctions)
            {
                DateTime maxDate = a.bids.Max(m => m.timeSent);
                Bid bid = a.bids.Where(m => m.timeSent == maxDate).FirstOrDefault();
                if (bid.biddingUser.Id == user.Id)
                {
                    myAyctions.Add(a);
                }
            }

            int pageSize = 8;
            int pageNumber = (page ?? 1);
            return View(myAyctions.ToPagedList(pageNumber, pageSize));
        }
    }
}
