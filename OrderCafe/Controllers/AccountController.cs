using OrderCafe.Helpers;
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
        public ActionResult Index(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Authen(string Email, string PasswordHash, string returnUrl)
        {
            // Tìm admin theo email; không tìm theo cả email và mật khẩu nữa.
            var admin = db.AdminUsers.FirstOrDefault(user => user.Email == Email);

            // Chỉ đăng nhập nếu tìm thấy admin và mật khẩu khớp hash.
            string upgradedAdminPassword;
            if (admin != null && PasswordHelper.VerifyAndUpgrade(
                PasswordHash, admin.PasswordHash, out upgradedAdminPassword))
            {
                // Nếu đây là tài khoản cũ, lưu hash mới sau lần đăng nhập thành công.
                if (upgradedAdminPassword != admin.PasswordHash)
                {
                    admin.PasswordHash = upgradedAdminPassword;
                    db.SaveChanges();
                }

                Session["FullName"] = admin.FullName;
                Session["Email"] = admin.Email;
                Session["role"] = admin.Role;
                return RedirectToAction("Index", "home", new { area = "Admin" });
            }

            // Nếu email không phải admin hoặc mật khẩu không đúng, kiểm tra bảng khách hàng.
            var customer = db.Customers.FirstOrDefault(user => user.Email == Email);

            // Chỉ đăng nhập nếu tìm thấy khách và mật khẩu khớp hash.
            string upgradedCustomerPassword;
            if (customer != null && PasswordHelper.VerifyAndUpgrade(
                PasswordHash, customer.PasswordHash, out upgradedCustomerPassword))
            {
                // Nếu đây là tài khoản cũ, lưu hash mới sau lần đăng nhập thành công.
                if (upgradedCustomerPassword != customer.PasswordHash)
                {
                    customer.PasswordHash = upgradedCustomerPassword;
                    db.SaveChanges();
                }

                Session["FullName"] = customer.FullName;
                Session["Email"] = customer.Email;
                Session["role"] = "Customer";

                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("GiaoDien", "Home");
            }

            // Không tìm thấy tài khoản hoặc mật khẩu không khớp.
            ViewBag.LoginErrorMess = "Error email or password! Try again please!";
            ViewBag.ReturnUrl = returnUrl;
            return View("Index");
        }

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

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Register(Customer _User)
        {
            // Kiểm tra mật khẩu người dùng nhập trước khi thay nó bằng BCrypt hash.
            if (!string.IsNullOrWhiteSpace(_User.PasswordHash)
                && !System.Text.RegularExpressions.Regex.IsMatch(
                    _User.PasswordHash, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,15}$"))
            {
                ModelState.AddModelError("PasswordHash",
                    "Mật khẩu cần 8–15 ký tự, có chữ thường, chữ hoa và chữ số.");
            }

            if (ModelState.IsValid)
            {
                var check = db.Customers.FirstOrDefault(s => s.Email == _User.Email);
                if(check == null)
                {
                    // PasswordHash lúc này vẫn là mật khẩu người dùng vừa nhập.
                    // Băm trước khi lưu để database không giữ mật khẩu dạng chữ thường.
                    _User.PasswordHash = PasswordHelper.Hash(_User.PasswordHash);
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
