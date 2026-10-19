using OrderCafe.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OrderCafe.Areas.Admin.Controllers
{
    public class ProductsController : Controller
    {
        CoffeeOrderDBEntities db = new CoffeeOrderDBEntities();
        // GET: Admin/Products
        public ActionResult Index()
        {
            var products = db.Products.ToList();
            return View(products);
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            var product = db.Products.Where(c => c.CategoryId == id).FirstOrDefault();
            return View(product);
        }

        [HttpPost]
        public ActionResult Edit(int id, Product product)
        {
            db.Entry(product).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Delete(int? id)
        {
            if(id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            var product = db.Products.Find(id);
            if(product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            var product = db.Products.Find(id);
            db.Products.Remove(product);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}