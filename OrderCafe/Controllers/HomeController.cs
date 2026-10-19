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
            return View("GiaoDien");
        }

        public ActionResult DangNhap()
        {
            return View();
        }

        



    }
}
