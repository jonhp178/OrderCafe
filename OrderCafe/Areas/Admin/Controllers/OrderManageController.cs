using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using OrderCafe.Models;

namespace OrderCafe.Areas.Admin.Controllers
{
    [AdminAuthorize]
    public class OrderManageController : Controller
    {
        private readonly CoffeeOrderDBEntities db = new CoffeeOrderDBEntities();

        private static readonly string[] OrderStatuses =
        {
            // Database chỉ cho phép các trạng thái này trong CK_OrderPro_Status.
            "Pending", "Confirmed", "Shipping", "Completed", "Cancelled"
        };

        public ActionResult Index(string status = "Pending")
        {
            if (!OrderStatuses.Contains(status)) status = "Pending";
            ViewBag.Status = status;
            var orders = db.OrderProes.AsNoTracking()
                .Include(o => o.Customer)
                .Where(o => o.Status == status)
                .OrderByDescending(o => o.OrderDate)
                .ToList();
            return View(orders);
        }

        public ActionResult Details(int id)
        {
            var order = db.OrderProes.AsNoTracking()
                .Include(o => o.Customer).Include(o => o.AdminUser)
                .Include(o => o.OrderDetails.Select(d => d.Product))
                .Include(o => o.Payments)
                .FirstOrDefault(o => o.OrderId == id);
            if (order == null) return HttpNotFound();
            return View(order);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Confirm(int id)
        {
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                var order = FindForUpdate(id);
                if (order == null) return HttpNotFound();
                if (order.Status != "Pending") return Reject(id, "Đơn không còn ở trạng thái chờ xác nhận.");
                if (order.PaymentMethod == "VNPAY" && order.PaymentStatus != "Paid")
                    return Reject(id, "Đơn VNPAY chỉ được xác nhận sau khi thanh toán thành công.");

                var email = Session["Email"] as string;
                var adminId = db.AdminUsers.Where(a => a.Email == email).Select(a => (int?)a.AdminId).FirstOrDefault();
                if (!adminId.HasValue) return Reject(id, "Không xác định được tài khoản admin đang đăng nhập.");

                order.Status = "Confirmed";
                order.AdminId = adminId.Value;
                db.SaveChanges();
                transaction.Commit();
                TempData["Success"] = "Đã xác nhận đơn hàng.";
                return RedirectToAction("Details", new { id });
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Cancel(int id)
        {
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                var order = FindForUpdate(id);
                if (order == null) return HttpNotFound();
                if (order.Status != "Pending" && order.Status != "Confirmed")
                    return Reject(id, "Chỉ có thể huỷ đơn đang chờ hoặc đã xác nhận.");

                bool onlinePaid = order.PaymentMethod == "VNPAY" && order.PaymentStatus == "Paid";
                order.Status = "Cancelled";
                db.SaveChanges();
                transaction.Commit();
                TempData[onlinePaid ? "Error" : "Success"] = onlinePaid
                    ? "Đơn đã thanh toán online, cần hoàn tiền thủ công"
                    : "Đã huỷ đơn hàng.";
                return RedirectToAction("Details", new { id });
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ChangeStatus(int id, string newStatus)
        {
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                var order = FindForUpdate(id);
                if (order == null) return HttpNotFound();

                // Database dùng một trạng thái Shipping cho giai đoạn chuẩn bị và giao hàng.
                bool allowed = (order.Status == "Confirmed" && newStatus == "Shipping")
                    || (order.Status == "Shipping" && newStatus == "Completed");
                if (!allowed) return Reject(id, "Chuyển trạng thái không hợp lệ hoặc đơn đã được cập nhật.");

                order.Status = newStatus;
                if (newStatus == "Completed" && order.PaymentMethod == "COD")
                {
                    order.PaymentStatus = "Paid";
                    var pendingPayments = db.Payments.Where(p => p.OrderId == id && p.Status == "Pending").ToList();
                    foreach (var payment in pendingPayments)
                    {
                        payment.Status = "Paid";
                        payment.PaidAt = DateTime.Now;
                    }
                }

                db.SaveChanges();
                transaction.Commit();
                TempData["Success"] = "Đã cập nhật trạng thái đơn hàng.";
                return RedirectToAction("Details", new { id });
            }
        }

        private OrderPro FindForUpdate(int id)
        {
            // Nạp lại bản ghi ngay trước khi kiểm tra và lưu để tránh xử lý theo dữ liệu cũ.
            return db.OrderProes.FirstOrDefault(o => o.OrderId == id);
        }

        private ActionResult Reject(int id, string message)
        {
            TempData["Error"] = message;
            return RedirectToAction("Details", new { id });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
