using OrderCafe.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OrderCafe.Controllers
{
    public class AdminController : Controller
    {
        CoffeeOrderDBEntities db = new CoffeeOrderDBEntities();
        // GET: Admin
        public ActionResult Index()
        {
            return View();
        }

        
    }
}