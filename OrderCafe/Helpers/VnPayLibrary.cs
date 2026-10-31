using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace OrderCafe.Helpers
{
    public class VnPayLibrary
    {
        private readonly SortedDictionary<string, string> requestData =
            new SortedDictionary<string, string>(StringComparer.Ordinal);

        private readonly SortedDictionary<string, string> responseData =
            new SortedDictionary<string, string>(StringComparer.Ordinal);

        // Thêm tham số gửi sang VNPay.
        public void AddRequestData(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Tên tham số không được rỗng.");
            }

            requestData[key] = value;
        }

        // Thêm tham số nhận về từ VNPay.
        // Không đưa chữ ký vào dữ liệu dùng để tính lại chữ ký.
        public void AddResponseData(string key, string value)
        {
            if (string.IsNullOrEmpty(key)
                || !key.StartsWith("vnp_", StringComparison.Ordinal)
                || key == "vnp_SecureHash"
                || key == "vnp_SecureHashType")
            {
                return;
            }

            responseData[key] = value;
        }

        // Đọc một tham số trong kết quả VNPay trả về.
        public string GetResponseData(string key)
        {
            string value;

            return responseData.TryGetValue(key, out value)
                ? value
                : string.Empty;
        }

        // Tạo URL có chữ ký để chuyển khách sang VNPay.
        public string CreateRequestUrl(string paymentUrl, string hashSecret)
        {
            if (string.IsNullOrWhiteSpace(paymentUrl))
            {
                throw new ArgumentException("Chưa cấu hình PaymentUrl.");
            }

            if (string.IsNullOrWhiteSpace(hashSecret))
            {
                throw new ArgumentException("Chưa cấu hình HashSecret.");
            }

            string query = BuildQuery(requestData);

            if (query.Length == 0)
            {
                throw new InvalidOperationException(
                    "Chưa có tham số thanh toán.");
            }

            string signature = HmacSha512(hashSecret, query);

            string separator = paymentUrl.Contains("?")
                ? (paymentUrl.EndsWith("?") || paymentUrl.EndsWith("&")
                    ? string.Empty
                    : "&")
                : "?";

            return paymentUrl
                + separator
                + query
                + "&vnp_SecureHash="
                + signature;
        }

        // Chỉ xác thực tính toàn vẹn của kết quả.
        // Chữ ký đúng chưa có nghĩa là thanh toán thành công.
        public bool ValidateSignature(string inputHash, string hashSecret)
        {
            if (string.IsNullOrWhiteSpace(inputHash)
                || inputHash.Length != 128
                || string.IsNullOrWhiteSpace(hashSecret))
            {
                return false;
            }

            string query = BuildQuery(responseData);

            if (query.Length == 0)
            {
                return false;
            }

            string expectedHash = HmacSha512(hashSecret, query);
            string receivedHash = inputHash.ToLowerInvariant();

            int difference = 0;

            for (int i = 0; i < expectedHash.Length; i++)
            {
                difference |= expectedHash[i] ^ receivedHash[i];
            }

            return difference == 0;
        }

        // Các dictionary đã sắp xếp theo tên tham số.
        private static string BuildQuery(
            SortedDictionary<string, string> data)
        {
            var builder = new StringBuilder();

            foreach (var item in data)
            {
                if (string.IsNullOrEmpty(item.Value)
                    || item.Key == "vnp_SecureHash"
                    || item.Key == "vnp_SecureHashType")
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append("&");
                }

                builder.Append(WebUtility.UrlEncode(item.Key));
                builder.Append("=");
                builder.Append(WebUtility.UrlEncode(item.Value));
            }

            return builder.ToString();
        }

        private static string HmacSha512(string secret, string data)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashBytes = hmac.ComputeHash(dataBytes);

                return BitConverter.ToString(hashBytes)
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }
    }
}