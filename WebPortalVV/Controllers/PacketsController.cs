using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebPortalVV.Models;

namespace WebPortalVV.Controllers
{
    public class PacketsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Packets
        public ActionResult Index()
        {
            return View(db.Packets.ToList());
        }

        // GET: Packets/Details/5
        public ActionResult Details(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Packet packet = db.Packets.Find(id);
            if (packet == null)
            {
                return HttpNotFound();
            }
            return View(packet);
        }

        // GET: Packets/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Packets/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "idPacket,packetPrice,packetName,numberOfTokens")] Packet packet)
        {
            if (ModelState.IsValid)
            {
                db.Packets.Add(packet);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(packet);
        }

        // GET: Packets/Edit/5
        public ActionResult Edit(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Packet packet = db.Packets.Find(id);
            if (packet == null)
            {
                return HttpNotFound();
            }
            return View(packet);
        }

        // POST: Packets/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "idPacket,packetPrice,packetName,numberOfTokens")] Packet packet)
        {
            if (ModelState.IsValid)
            {
                db.Entry(packet).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(packet);
        }

        // GET: Packets/Delete/5
        public ActionResult Delete(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Packet packet = db.Packets.Find(id);
            if (packet == null)
            {
                return HttpNotFound();
            }
            return View(packet);
        }

        // POST: Packets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(long id)
        {
            Packet packet = db.Packets.Find(id);
            db.Packets.Remove(packet);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
