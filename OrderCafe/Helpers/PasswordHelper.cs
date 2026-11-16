using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BC = BCrypt.Net.BCrypt;

namespace OrderCafe.Helpers
{
    public class PasswordHelper
    {
        public static string Hash(string password) // Nhận mật khẩu người dùng nhập và trả về chuỗi hash.
        {
            return BC.HashPassword(password); // Băm mật khẩu và để thư viện tự tạo salt.
        }

        public static bool Verify(string password, string savedHash) // Kiểm tra mật khẩu nhập vào có khớp hash đã lưu không.
        {
            if (string.IsNullOrWhiteSpace(password)) // Mật khẩu trống thì không cho đăng nhập.
            {
                return false; // Trả kết quả sai ngay, không gọi BCrypt với chuỗi rỗng.
            }

            if (string.IsNullOrWhiteSpace(savedHash)) // Database chưa có hash thì tài khoản không thể xác thực.
            {
                return false; // Trả kết quả sai để không phát sinh lỗi.
            }

            try // Dùng try để xử lý dữ liệu cũ không phải chuỗi BCrypt hợp lệ.
            {
                return BC.Verify(password, savedHash); // BCrypt tự so khớp mật khẩu với salt nằm trong hash.
            }
            catch (BCrypt.Net.SaltParseException) // Xảy ra nếu giá trị trong database không có định dạng BCrypt.
            {
                return false; // Không đăng nhập bằng dữ liệu không hợp lệ.
            }
        }

        // Dùng khi đăng nhập để chuyển dần tài khoản cũ đang lưu mật khẩu dạng chữ thường sang BCrypt.
        public static bool VerifyAndUpgrade(string password, string savedPassword, out string upgradedPassword)
        {
            upgradedPassword = savedPassword;

            // Tài khoản đã dùng BCrypt: chỉ cần kiểm tra mật khẩu.
            if (Verify(password, savedPassword))
            {
                return true;
            }

            // Không cho phép dùng nguyên chuỗi hash làm mật khẩu nếu hash bị lỗi định dạng.
            if (!string.IsNullOrEmpty(savedPassword) && savedPassword.StartsWith("$2"))
            {
                return false;
            }

            // Tài khoản cũ: nếu mật khẩu nhập trùng chính xác dữ liệu cũ thì băm lại ngay.
            if (!string.IsNullOrEmpty(password) && password == savedPassword)
            {
                upgradedPassword = Hash(password);
                return true;
            }

            return false;
        }
    }
}
