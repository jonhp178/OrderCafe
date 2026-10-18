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

        [HttpPost]
        public ActionResult Authen(string Email, string PasswordHash)
        {
            // 1. Kiểm tra xem có phải Admin đăng nhập không
            var checkAd = db.AdminUsers.FirstOrDefault(s => s.Email.Equals(Email) && s.PasswordHash.Equals(PasswordHash));
            if (checkAd != null)
            {
                Session["FullName"] = checkAd.FullName;
                Session["Email"] = checkAd.Email;
                Session["role"] = checkAd.Role; // Hoặc gán cứng là "Admin"

                return RedirectToAction("Index", "Category", new { area = "Admin" }); // Chuyển đến trang quản lý của Admin
            }

            // 2. Nếu không phải Admin, kiểm tra tiếp trong bảng Customers
            var checkCust = db.Customers.FirstOrDefault(s => s.Email.Equals(Email) && s.PasswordHash.Equals(PasswordHash));
            if (checkCust != null)
            {
                Session["FullName"] = checkCust.FullName;
                Session["Email"] = checkCust.Email;
                Session["role"] = "Customer";

                return RedirectToAction("GiaoDien", "Home"); // Chuyển về trang chủ khách hàng
            }

            // 3. Nếu kiểm tra cả 2 bảng đều không thấy -> Sai thông tin đăng nhập
            ViewBag.LoginErrorMess = "Error email or password! Try again please!";
            return View("Index");
        }
        //public ActionResult Authen(Customer customer, AdminUser adminuser)
        //{
        //    var checkAd = db.AdminUsers.Where(s => s.Email.Equals(adminuser.Email)
        //    && s.PasswordHash.Equals(adminuser.PasswordHash)).FirstOrDefault();
        //    if (checkAd != null)
        //    {
        //        Session["FullName"] = checkAd.FullName;
        //        Session["Email"] = checkAd.Email;
        //        Session["role"] = checkAd.Role;
        //        return RedirectToAction("AdminUser", "Home");
        //    }

        //    var check = db.Customers.Where(s => s.Email.Equals(customer.Email) 
        //    && s.PasswordHash.Equals(customer.PasswordHash)).FirstOrDefault();      
        //    if (check != null)
        //    {
        //        customer.LoginErrorMess = ("error email or password! try again please!");
        //        return View("Index", customer);
        //    }
        //    else
        //    {
        //        Session["FullName"] = check.FullName;
        //        Session["Email"] = check.Email;
        //        return RedirectToAction("GiaoDien", "Home");
        //    }
        //    return View();
        //}

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("GiaoDien", "Home");
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