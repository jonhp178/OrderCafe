using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OrderCafe.Models;

namespace OrderCafe.Areas.Admin.Controllers
{
    public class homeController : Controller
    {
        private readonly CoffeeOrderDBEntities db = new CoffeeOrderDBEntities();
        // GET: Admin/Home
        public ActionResult Index() // Đây là trang mở ra khi vào /Admin.
        {
            ViewBag.ProductCount = db.Products.Count(); // Đếm số sản phẩm để hiển thị trên thẻ tổng quan.
            ViewBag.CategoryCount = db.Categories.Count(); // Đếm số thể loại để hiển thị trên thẻ tổng quan.
            ViewBag.OrderCount = db.OrderProes.Count(); // Đếm tổng số đơn hàng hiện có.
            ViewBag.PendingCount = db.OrderProes.Count(order => order.Status == "Pending"); // Đếm đơn đang chờ xác nhận.

            var recentOrders = db.OrderProes // Bắt đầu lấy danh sách đơn hàng.
                .OrderByDescending(order => order.OrderDate) // Xếp đơn mới nhất lên trước.
                .Take(5) // Chỉ lấy 5 đơn để bảng dashboard gọn hơn.
                .ToList(); // Chạy truy vấn và lấy dữ liệu về để hiển thị.

            return View(recentOrders); // Gửi 5 đơn hàng đó sang trang Index.cshtml.
        }

        protected override void Dispose(bool disposing) // Đóng kết nối khi Controller làm việc xong.
        {
            if (disposing) // Chỉ đóng kết nối khi hệ thống yêu cầu giải phóng tài nguyên.
            {
                db.Dispose(); // Giải phóng kết nối cơ sở dữ liệu.
            }

            base.Dispose(disposing); // Báo cho Controller cha hoàn tất việc dọn dẹp.
        }
    }
}