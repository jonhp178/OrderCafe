using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using OrderCafe.Models;

namespace OrderCafe.Controllers
{
    public class OrderController : Controller
    {
        private readonly CoffeeOrderDBEntities db = new CoffeeOrderDBEntities();

        // Mở form checkout và điền sẵn thông tin khách đã đăng nhập.
        [HttpGet]
        public ActionResult Checkout()
        {
            var customerId = GetCustomerId();
            if (!customerId.HasValue) return LoginRedirect(Url.Action("Checkout", "Order"));

            var cart = Session["Cart"] as List<CartItem>;
            if (cart == null || cart.Count == 0)
            {
                TempData["CartMessage"] = "Giỏ hàng đang trống.";
                return RedirectToAction("Index", "Cart");
            }
            RefreshCartPrices(cart);

            var customer = db.Customers.AsNoTracking().FirstOrDefault(c => c.UserId == customerId.Value);
            if (customer == null) return RedirectToAction("Index", "Account");

            return View(new OrderCheckoutViewModel
            {
                ReceiverName = customer.FullName,
                ReceiverPhone = customer.Phone,
                PaymentMethod = "COD"
            });
        }

        // Tạo đơn Pending; giá và tên món luôn lấy lại từ database.
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Checkout(OrderCheckoutViewModel model)
        {
            var customerId = GetCustomerId();
            if (!customerId.HasValue) return LoginRedirect(Url.Action("Checkout", "Order"));

            var cart = Session["Cart"] as List<CartItem>;
            if (cart == null || cart.Count == 0)
            {
                TempData["CartMessage"] = "Giỏ hàng đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            if (model.PaymentMethod != "COD" && model.PaymentMethod != "VNPAY")
                ModelState.AddModelError("PaymentMethod", "Vui lòng chọn phương thức thanh toán hợp lệ.");

            if (!ModelState.IsValid)
            {
                RefreshCartPrices(cart);
                return View(model);
            }

            var productIds = cart.Select(item => item.ProductId).Distinct().ToList();
            var products = db.Products.Where(p => productIds.Contains(p.ProductId)).ToList();

            if (products.Count != productIds.Count || products.Any(p => !p.IsAvailable))
            {
                TempData["CartMessage"] = "Có món vừa ngừng bán. Vui lòng kiểm tra lại giỏ hàng.";
                return RedirectToAction("Index", "Cart");
            }

            if (cart.Any(item => item.Quantity < 1 || item.Quantity > 99))
            {
                TempData["CartMessage"] = "Số lượng món không hợp lệ. Vui lòng kiểm tra lại giỏ hàng.";
                return RedirectToAction("Index", "Cart");
            }

            var productById = products.ToDictionary(product => product.ProductId);
            long totalAmount = cart.Sum(item => (long)productById[item.ProductId].Price * item.Quantity);
            if (totalAmount <= 0 || totalAmount > int.MaxValue)
            {
                TempData["CartMessage"] = "Tổng tiền đơn hàng không hợp lệ.";
                return RedirectToAction("Index", "Cart");
            }

            var order = new OrderPro
            {
                OrderDate = DateTime.Now,
                ReceiverName = model.ReceiverName.Trim(),
                ReceiverPhone = model.ReceiverPhone.Trim(),
                ShippingAddress = model.ShippingAddress.Trim(),
                Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim(),
                TotalAmount = (int)totalAmount,
                Status = "Pending",
                PaymentMethod = model.PaymentMethod,
                PaymentStatus = "Pending", // Constraint database dùng Pending cho đơn chưa thanh toán.
                UserId = customerId.Value
            };

            foreach (var item in cart)
            {
                var product = productById[item.ProductId];
                order.OrderDetails.Add(new OrderDetail
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    UnitPrice = product.Price,
                    Quantity = item.Quantity
                });
            }

            order.Payments.Add(new Payment
            {
                Method = model.PaymentMethod,
                Amount = (int)totalAmount,
                Status = "Pending"
            });

            db.OrderProes.Add(order);
            db.SaveChanges();

            Session["Cart"] = new List<CartItem>();
            TempData["Success"] = model.PaymentMethod == "COD"
                ? "Đặt hàng thành công. Đơn đang chờ admin xác nhận."
                : "Đơn hàng đã tạo. Hãy thanh toán VNPay để admin có thể xác nhận.";
            return RedirectToAction("Details", new { id = order.OrderId });
        }

        // Danh sách đơn của riêng khách đang đăng nhập.
        [HttpGet]
        public ActionResult Index()
        {
            var customerId = GetCustomerId();
            if (!customerId.HasValue) return LoginRedirect(Url.Action("Index", "Order"));

            var orders = db.OrderProes.AsNoTracking()
                .Where(order => order.UserId == customerId.Value)
                .OrderByDescending(order => order.OrderDate)
                .ToList();
            return View(orders);
        }

        [HttpGet]
        public ActionResult Details(int id)
        {
            var customerId = GetCustomerId();
            if (!customerId.HasValue) return LoginRedirect(Url.Action("Details", "Order", new { id }));

            var order = GetCustomerOrder(id, customerId.Value);
            if (order == null) return HttpNotFound();
            return View(order);
        }

        // Trang chi tiết gọi action này định kỳ để nhận trạng thái admin vừa cập nhật.
        [HttpGet]
        public ActionResult CheckStatus(int id)
        {
            var customerId = GetCustomerId();
            if (!customerId.HasValue) return new HttpStatusCodeResult(401);

            var order = db.OrderProes.AsNoTracking()
                .Where(item => item.OrderId == id && item.UserId == customerId.Value)
                .Select(item => new { item.Status, item.PaymentMethod, item.PaymentStatus })
                .FirstOrDefault();
            if (order == null) return HttpNotFound();

            return Json(new
            {
                status = order.Status,
                message = order.Status == "Cancelled" && order.PaymentMethod == "VNPAY"
                    && order.PaymentStatus == "Paid"
                    ? "Đơn đã huỷ nhưng đã thanh toán online. Vui lòng liên hệ cửa hàng để xử lý hoàn tiền."
                    : GetStatusMessage(order.Status)
            }, JsonRequestBehavior.AllowGet);
        }

        private int? GetCustomerId()
        {
            if (!string.Equals(Session["role"] as string, "Customer", StringComparison.OrdinalIgnoreCase))
                return null;

            var email = Session["Email"] as string;
            if (string.IsNullOrWhiteSpace(email)) return null;

            return db.Customers.Where(customer => customer.Email == email)
                .Select(customer => (int?)customer.UserId)
                .FirstOrDefault();
        }

        private ActionResult LoginRedirect(string returnUrl)
        {
            return RedirectToAction("Index", "Account", new { returnUrl });
        }

        private void RefreshCartPrices(List<CartItem> cart)
        {
            var productIds = cart.Select(item => item.ProductId).Distinct().ToList();
            var products = db.Products.AsNoTracking()
                .Where(product => productIds.Contains(product.ProductId))
                .ToDictionary(product => product.ProductId);

            foreach (var item in cart)
            {
                Product product;
                if (!products.TryGetValue(item.ProductId, out product)) continue;
                item.ProductName = product.ProductName;
                item.ImageUrl = product.ImageUrl;
                item.UnitPrice = product.Price;
            }

            Session["Cart"] = cart;
        }

        private OrderPro GetCustomerOrder(int orderId, int customerId)
        {
            return db.OrderProes.AsNoTracking()
                .Include(order => order.OrderDetails)
                .Include(order => order.Payments)
                .FirstOrDefault(order => order.OrderId == orderId && order.UserId == customerId);
        }

        private static string GetStatusMessage(string status)
        {
            switch (status)
            {
                case "Pending": return "Đơn hàng đang chờ admin xác nhận.";
                case "Confirmed": return "Admin đã xác nhận đơn hàng của bạn.";
                case "Shipping": return "Đơn hàng đang được chuẩn bị / giao.";
                case "Completed": return "Đơn hàng đã hoàn thành.";
                case "Cancelled": return "Đơn hàng đã bị huỷ.";
                default: return "Trạng thái đơn hàng: " + status;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }

    public class OrderCheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên người nhận.")]
        [StringLength(100)]
        public string ReceiverName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại cần có 10 số và bắt đầu bằng 0.")]
        public string ReceiverPhone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
        [StringLength(255)]
        public string ShippingAddress { get; set; }

        [StringLength(255)]
        public string Note { get; set; }

        [Required]
        public string PaymentMethod { get; set; }
    }
}
