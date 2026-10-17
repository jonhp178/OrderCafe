using OrderCafe.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OrderCafe.Controllers
{
    public class AccountController : Controller
    {
        CoffeeOrderDBEntities db = new CoffeeOrderDBEntities();
        // GET: Account
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(Customer _User)
        {
            if (ModelState.IsValid)
            {
                var check = db.Customers.FirstOrDefault(s => s.Email == _User.Email);
                if(check == null)
                {
                    db.Configuration.ValidateOnSaveEnabled = false;
                    db.Customers.Add(_User);
                    db.SaveChanges();
                    //neu dk kí thành công thì chuển cái register đó sang login
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.error = "email da duoc su dung vui long su dung emai khac!";
                    return View();
                }
            }
            return View();
        }
    }
}