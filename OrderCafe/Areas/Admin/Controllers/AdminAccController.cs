using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using OrderCafe.Helpers;
using OrderCafe.Models;

namespace OrderCafe.Areas.Admin.Controllers
{
    public class AdminAccController : Controller
    {
        private CoffeeOrderDBEntities db = new CoffeeOrderDBEntities();

        // GET: Admin/AdminAcc
        public ActionResult Index()
        {
            return View(db.AdminUsers.ToList());
        }

        // GET: Admin/AdminAcc/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AdminUser adminUser = db.AdminUsers.Find(id);
            if (adminUser == null)
            {
                return HttpNotFound();
            }
            return View(adminUser);
        }

        // GET: Admin/AdminAcc/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Admin/AdminAcc/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "AdminId,Username,FullName,PasswordHash,Phone,Role,IsActive,CreatedAt,Email")] AdminUser adminUser)
        {
            // Kiểm tra mật khẩu người dùng nhập trước khi thay nó bằng BCrypt hash.
            if (!string.IsNullOrWhiteSpace(adminUser.PasswordHash)
                && !System.Text.RegularExpressions.Regex.IsMatch(
                    adminUser.PasswordHash, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,15}$"))
            {
                ModelState.AddModelError("PasswordHash",
                    "Mật khẩu cần 8–15 ký tự, có chữ thường, chữ hoa và chữ số.");
            }

            if (ModelState.IsValid)
            {
                // Form nhận mật khẩu thường; trước khi lưu phải thay bằng BCrypt hash.
                adminUser.PasswordHash = PasswordHelper.Hash(adminUser.PasswordHash);
                db.AdminUsers.Add(adminUser);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(adminUser);
        }

        // GET: Admin/AdminAcc/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AdminUser adminUser = db.AdminUsers.Find(id);
            if (adminUser == null)
            {
                return HttpNotFound();
            }
            return View(adminUser);
        }

        // POST: Admin/AdminAcc/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "AdminId,Username,FullName,PasswordHash,Phone,Role,IsActive,CreatedAt,Email")] AdminUser adminUser)
        {
            // Khi sửa thông tin, mật khẩu mới là tùy chọn. Bỏ yêu cầu mật khẩu cũ.
            ModelState.Remove("PasswordHash");

            if (!string.IsNullOrWhiteSpace(adminUser.PasswordHash)
                && !System.Text.RegularExpressions.Regex.IsMatch(
                    adminUser.PasswordHash, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,15}$"))
            {
                ModelState.AddModelError("PasswordHash", "Mật khẩu cần 8–15 ký tự, có chữ thường, chữ hoa và chữ số.");
            }

            if (ModelState.IsValid)
            {
                // Lấy bản ghi đang có để không ghi đè hash bằng ô mật khẩu để trống.
                var existingAdmin = db.AdminUsers.Find(adminUser.AdminId);
                if (existingAdmin == null)
                {
                    return HttpNotFound();
                }

                existingAdmin.Username = adminUser.Username;
                existingAdmin.FullName = adminUser.FullName;
                existingAdmin.Phone = adminUser.Phone;
                existingAdmin.Role = adminUser.Role;
                existingAdmin.IsActive = adminUser.IsActive;
                existingAdmin.CreatedAt = adminUser.CreatedAt;
                existingAdmin.Email = adminUser.Email;

                // Chỉ băm và cập nhật khi người quản trị nhập mật khẩu mới.
                if (!string.IsNullOrWhiteSpace(adminUser.PasswordHash))
                {
                    existingAdmin.PasswordHash = PasswordHelper.Hash(adminUser.PasswordHash);
                }

                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(adminUser);
        }

        // GET: Admin/AdminAcc/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AdminUser adminUser = db.AdminUsers.Find(id);
            if (adminUser == null)
            {
                return HttpNotFound();
            }
            return View(adminUser);
        }

        // POST: Admin/AdminAcc/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                var adminuser = db.AdminUsers.Where(c => c.AdminId == id).FirstOrDefault();
                db.AdminUsers.Remove(adminuser);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch
            {
                return Content("khong xoa dc vi lien quan den bang khac");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
