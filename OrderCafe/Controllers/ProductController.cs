using OrderCafe.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace OrderCafe.Controllers
{
    public class ProductController : Controller
    {
        // GET: Product
        CoffeeOrderDBEntities SPdb = new CoffeeOrderDBEntities();
        public ActionResult Index(int? maTL)
        {
            var ds = SPdb.Products.Include("Category").AsQueryable();
            if (maTL.HasValue) ds = ds.Where(s => s.CategoryId == maTL.Value);
            ViewBag.Title = "Danh sách sách";
            return View(ds.ToList());
        }

        public ActionResult MenuTheLoai()
        {
            return PartialView("CategoryPd", SPdb.Categories.ToList());
        }
    }
}