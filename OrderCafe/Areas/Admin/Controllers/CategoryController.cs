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
            var category = db.Categories.ToList();
            return View(category);
        }

        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Create(Category category)
        {
            try
            {
                db.Categories.Add(category);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch
            {
                return Content("loi tao category");
            }
        }

        [HttpGet]
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