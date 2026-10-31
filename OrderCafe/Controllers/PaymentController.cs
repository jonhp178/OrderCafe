using OrderCafe.Helpers;
using System;
using System.Configuration;
using System.Globalization;
using System.Web.Mvc;

namespace OrderCafe.Controllers
{
    public class PaymentController : Controller
    {
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
    }
}