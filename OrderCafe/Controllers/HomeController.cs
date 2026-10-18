using OrderCafe.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OrderCafe.Controllers
{
    public class HomeController : Controller
    {

        
        public ActionResult GiaoDien()
        {
            return View();
        }

        public ActionResult DangNhap()
        {
            return View();
        }

        //[ChildActionOnly]
        //public ActionResult Menu() 
        //{
            
        //    return View(Product);
        //}


        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}
