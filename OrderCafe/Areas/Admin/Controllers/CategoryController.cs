using OrderCafe.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OrderCafe.Areas.Admin.Controllers
{
    public class CategoryController : Controller
    {
        CoffeeOrderDBEntities db = new CoffeeOrderDBEntities();
        // GET: Admin/Category
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Edit(int id)
        {
            var category = db.Categories.Where(c => c.CategoryId == id).FirstOrDefault();
            return View(category);
        }

        [HttpPost]
        public ActionResult Edit(int id, Category category)
        {
            db.Entry(category).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}