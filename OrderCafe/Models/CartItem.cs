using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OrderCafe.Models
{
    public class CartItem
    {
        // Mã sản phẩm
        public int ProductId { get; set; }

        // Tên sản phẩm
        public string ProductName { get; set; }

        // Tên file hoặc đường dẫn ảnh
        public string ImageUrl { get; set; }

        // Đơn giá
        public decimal UnitPrice { get; set; }

        // Số lượng mua
        public int Quantity { get; set; }

        // Thành tiền tự tính, không cần gán giá trị
        public decimal TotalPrice
        {
            get
            {
                return UnitPrice * Quantity;
            }
        }
    }
}