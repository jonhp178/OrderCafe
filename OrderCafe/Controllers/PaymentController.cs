using OrderCafe.Helpers;
using System;
using OrderCafe.Models;
using System.Data.Entity;
using System.Linq;
using System.Configuration;
using System.Globalization;
using System.Web.Mvc;

namespace OrderCafe.Controllers
{
    public class PaymentController : Controller
    {
        private readonly CoffeeOrderDBEntities db = new CoffeeOrderDBEntities();

        // Số tiền cố định để kiểm tra Sandbox.
        private const long TestAmount = 10000;

        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create()
        {
            string tmnCode =
                ConfigurationManager.AppSettings["Vnpay:TmnCode"];

            string hashSecret =
                ConfigurationManager.AppSettings["Vnpay:HashSecret"];

            string paymentUrl =
                ConfigurationManager.AppSettings["Vnpay:PaymentUrl"];

            string returnUrl =
                ConfigurationManager.AppSettings["Vnpay:ReturnUrl"];

            if (string.IsNullOrWhiteSpace(tmnCode)
                || string.IsNullOrWhiteSpace(hashSecret)
                || string.IsNullOrWhiteSpace(paymentUrl)
                || string.IsNullOrWhiteSpace(returnUrl))
            {
                return Content(
                    "Thiếu cấu hình VNPay. Kiểm tra Web.config và secrets.xml.",
                    "text/plain");
            }

            // Bản thử này chỉ cho phép cổng Sandbox.
            if (paymentUrl !=
                "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html")
            {
                return Content(
                    "Bản thử yêu cầu PaymentUrl của VNPay Sandbox.",
                    "text/plain");
            }

            DateTime now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
                DateTime.UtcNow, "SE Asia Standard Time");

            string transactionRef = Guid.NewGuid().ToString("N");
            string ipAddress = Request.UserHostAddress;

            if (string.IsNullOrWhiteSpace(ipAddress)
                || ipAddress == "::1")
            {
                ipAddress = "127.0.0.1";
            }

            var vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", tmnCode);

            // VNPay yêu cầu số tiền nhân 100.
            vnpay.AddRequestData(
                "vnp_Amount",
                (TestAmount * 100).ToString(
                    CultureInfo.InvariantCulture));

            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_TxnRef", transactionRef);
            vnpay.AddRequestData(
                "vnp_OrderInfo",
                "Thanh toan thu OrderCafe " + transactionRef);

            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
            vnpay.AddRequestData("vnp_IpAddr", ipAddress);

            vnpay.AddRequestData(
                "vnp_CreateDate",
                now.ToString("yyyyMMddHHmmss",
                    CultureInfo.InvariantCulture));

            vnpay.AddRequestData(
                "vnp_ExpireDate",
                now.AddMinutes(15).ToString(
                    "yyyyMMddHHmmss",
                    CultureInfo.InvariantCulture));

            string url = vnpay.CreateRequestUrl(
                paymentUrl, hashSecret);

            return Redirect(url);
        }

        // Tạo link VNPay cho đúng đơn hàng của khách đang đăng nhập.
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Pay(int id)
        {
            var email = Session["Email"] as string;
            if (!string.Equals(Session["role"] as string, "Customer", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(email))
                return CustomerLoginRedirect(id);

            var customerId = db.Customers.Where(customer => customer.Email == email)
                .Select(customer => (int?)customer.UserId)
                .FirstOrDefault();
            if (!customerId.HasValue) return CustomerLoginRedirect(id);

            var order = db.OrderProes.Include(item => item.Payments)
                .FirstOrDefault(item => item.OrderId == id && item.UserId == customerId.Value);
            if (order == null) return HttpNotFound();
            if (order.PaymentMethod != "VNPAY" || order.Status != "Pending")
                return new HttpStatusCodeResult(400, "Đơn hàng không thể thanh toán VNPay.");
            if (order.PaymentStatus == "Paid")
            {
                TempData["Success"] = "Đơn hàng đã được thanh toán. Vui lòng chờ admin xác nhận.";
                return RedirectToAction("Details", "Order", new { id });
            }

            var payment = order.Payments.OrderByDescending(item => item.PaymentId).FirstOrDefault();
            if (payment == null) return new HttpStatusCodeResult(400, "Không tìm thấy giao dịch của đơn hàng.");

            string tmnCode = ConfigurationManager.AppSettings["Vnpay:TmnCode"];
            string hashSecret = ConfigurationManager.AppSettings["Vnpay:HashSecret"];
            string paymentUrl = ConfigurationManager.AppSettings["Vnpay:PaymentUrl"];
            string returnUrlSetting = ConfigurationManager.AppSettings["Vnpay:ReturnUrl"];

            if (string.IsNullOrWhiteSpace(tmnCode) || string.IsNullOrWhiteSpace(hashSecret)
                || string.IsNullOrWhiteSpace(paymentUrl) || string.IsNullOrWhiteSpace(returnUrlSetting))
            {
                TempData["Error"] = "Thiếu cấu hình VNPay. Kiểm tra TmnCode, HashSecret và ReturnUrl.";
                return RedirectToAction("Details", "Order", new { id });
            }

            if (paymentUrl != "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html")
            {
                TempData["Error"] = "Bản này chỉ hỗ trợ VNPay Sandbox.";
                return RedirectToAction("Details", "Order", new { id });
            }

            Uri returnBase;
            if (!Uri.TryCreate(returnUrlSetting, UriKind.Absolute, out returnBase))
            {
                TempData["Error"] = "ReturnUrl trong cấu hình VNPay không hợp lệ.";
                return RedirectToAction("Details", "Order", new { id });
            }

            // Cấu hình hiện tại là domain gốc; thêm đúng action nhận kết quả VNPay.
            string returnUrl = new Uri(returnBase, "/Payment/OrderReturn").AbsoluteUri;
            string transactionRef = Guid.NewGuid().ToString("N");
            payment.TransactionCode = transactionRef;
            payment.Status = "Pending";
            payment.ResponseCode = null;
            payment.PaidAt = null;
            order.PaymentStatus = "Pending";
            db.SaveChanges();

            DateTime now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
                DateTime.UtcNow, "SE Asia Standard Time");
            string ipAddress = Request.UserHostAddress;
            if (string.IsNullOrWhiteSpace(ipAddress) || ipAddress == "::1")
                ipAddress = "127.0.0.1";

            var vnpay = new VnPayLibrary();
            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", tmnCode);
            vnpay.AddRequestData("vnp_Amount", ((long)payment.Amount * 100).ToString(CultureInfo.InvariantCulture));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_TxnRef", transactionRef);
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang " + order.OrderId);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
            vnpay.AddRequestData("vnp_IpAddr", ipAddress);
            vnpay.AddRequestData("vnp_CreateDate", now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture));
            vnpay.AddRequestData("vnp_ExpireDate", now.AddMinutes(15).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture));

            return Redirect(vnpay.CreateRequestUrl(paymentUrl, hashSecret));
        }

        [HttpGet]
        public ActionResult Return()
        {
            string hashSecret =
                ConfigurationManager.AppSettings["Vnpay:HashSecret"];

            string tmnCode =
                ConfigurationManager.AppSettings["Vnpay:TmnCode"];

            var vnpay = new VnPayLibrary();

            foreach (string key in Request.QueryString.AllKeys)
            {
                if (key == null
                    || !key.StartsWith(
                        "vnp_", StringComparison.Ordinal))
                {
                    continue;
                }

                // Từ chối tham số VNPay bị lặp.
                string[] values = Request.QueryString.GetValues(key);

                if (values == null || values.Length != 1)
                {
                    return new HttpStatusCodeResult(
                        400, "Duplicate payment parameter");
                }

                vnpay.AddResponseData(key, values[0]);
            }

            bool validSignature = vnpay.ValidateSignature(
                Request.QueryString["vnp_SecureHash"],
                hashSecret);

            if (!validSignature)
            {
                return Content(
                    "Chữ ký VNPay không hợp lệ.",
                    "text/plain");
            }

            string expectedAmount = (TestAmount * 100).ToString(
                CultureInfo.InvariantCulture);

            if (vnpay.GetResponseData("vnp_TmnCode") != tmnCode
                || vnpay.GetResponseData("vnp_Amount")
                    != expectedAmount)
            {
                return Content(
                    "Mã website hoặc số tiền không khớp.",
                    "text/plain");
            }

            bool success =
                vnpay.GetResponseData("vnp_ResponseCode") == "00"
                && vnpay.GetResponseData(
                    "vnp_TransactionStatus") == "00";

            // Chỉ hiển thị kết quả thử, chưa cập nhật database.
            return Content(
                success
                    ? "VNPay báo giao dịch thử 10.000đ thành công."
                    : "Giao dịch thử chưa thành công hoặc đã bị hủy.",
                "text/plain");
        }

        // VNPay quay lại đây; chỉ cập nhật đơn khi chữ ký và số tiền hợp lệ.
        [HttpGet]
        public ActionResult OrderReturn()
        {
            string hashSecret = ConfigurationManager.AppSettings["Vnpay:HashSecret"];
            string tmnCode = ConfigurationManager.AppSettings["Vnpay:TmnCode"];
            var vnpay = new VnPayLibrary();

            foreach (string key in Request.QueryString.AllKeys)
            {
                if (key == null || !key.StartsWith("vnp_", StringComparison.Ordinal)) continue;
                string[] values = Request.QueryString.GetValues(key);
                if (values == null || values.Length != 1)
                    return new HttpStatusCodeResult(400, "Tham số VNPay bị lặp.");
                vnpay.AddResponseData(key, values[0]);
            }

            if (!vnpay.ValidateSignature(Request.QueryString["vnp_SecureHash"], hashSecret))
                return new HttpStatusCodeResult(400, "Chữ ký VNPay không hợp lệ.");

            string transactionRef = vnpay.GetResponseData("vnp_TxnRef");
            if (string.IsNullOrWhiteSpace(transactionRef))
                return new HttpStatusCodeResult(400, "Thiếu mã giao dịch VNPay.");

            var payment = db.Payments.Include(item => item.OrderPro)
                .FirstOrDefault(item => item.TransactionCode == transactionRef);
            if (payment == null || payment.Method != "VNPAY" || payment.OrderPro == null
                || payment.OrderPro.PaymentMethod != "VNPAY")
                return new HttpStatusCodeResult(404, "Không tìm thấy đơn hàng VNPay.");

            long expectedAmount = (long)payment.Amount * 100;
            if (vnpay.GetResponseData("vnp_TmnCode") != tmnCode
                || vnpay.GetResponseData("vnp_Amount") != expectedAmount.ToString(CultureInfo.InvariantCulture))
                return new HttpStatusCodeResult(400, "Thông tin đơn hàng hoặc số tiền không khớp.");

            if (payment.Status != "Paid")
            {
                bool success = vnpay.GetResponseData("vnp_ResponseCode") == "00"
                    && vnpay.GetResponseData("vnp_TransactionStatus") == "00";
                payment.Status = success ? "Paid" : "Failed";
                payment.ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                payment.PaidAt = success ? (DateTime?)DateTime.Now : null;
                payment.OrderPro.PaymentStatus = success ? "Paid" : "Failed";
                db.SaveChanges();
            }

            TempData[payment.Status == "Paid" ? "Success" : "Error"] = payment.Status == "Paid"
                ? "Thanh toán VNPay thành công. Đơn hàng đang chờ admin xác nhận."
                : "Thanh toán VNPay chưa thành công. Bạn có thể thử thanh toán lại.";
            return RedirectToAction("Details", "Order", new { id = payment.OrderId });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }

        private ActionResult CustomerLoginRedirect(int orderId)
        {
            string returnUrl = Url.Action("Details", "Order", new { id = orderId });
            return RedirectToAction("Index", "Account", new { returnUrl });
        }
    }
}
