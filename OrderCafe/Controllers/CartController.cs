using OrderCafe.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OrderCafe.Controllers
{
    public class CartController : Controller
    {
        private readonly CoffeeOrderDBEntities db =
            new CoffeeOrderDBEntities();

        // Lấy giỏ hiện tại; chưa có thì tạo giỏ rỗng.
        private List<CartItem> GetCart()
        {
            var cart = Session["Cart"] as List<CartItem>;

            if (cart == null)
            {
                cart = new List<CartItem>();
                Session["Cart"] = cart;
            }

            return cart;
        }
        // Hiển thị trang giỏ hàng.
        [HttpGet]
        public ActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToCart(int id)
        {
            // Lấy thông tin và giá từ database.
            var product = db.Products.Find(id);

            if (product == null)
            {
                return HttpNotFound();
            }

            if (!product.IsAvailable)
            {
                TempData["CartMessage"] = "Sản phẩm này hiện ngừng bán.";
                return RedirectToAction("Index");
            }

            var cart = GetCart();

            var item = cart.FirstOrDefault(x => x.ProductId == id);

            if (item == null)
            {
                cart.Add(new CartItem
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    ImageUrl = product.ImageUrl,
                    UnitPrice = product.Price,
                    Quantity = 1
                });
            }
            else if (item.Quantity < 99)
            {
                item.Quantity++;
            }
            else
            {
                TempData["CartMessage"] =
                    "Mỗi món được chọn tối đa 99 sản phẩm.";
            }

            Session["Cart"] = cart;

            return RedirectToAction("Index");
        }


        // Cập nhật số lượng.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateCart(int id, int quantity)
        {
            if (quantity < 1 || quantity > 99)
            {
                TempData["CartMessage"] =
                    "Số lượng phải từ 1 đến 99.";

                return RedirectToAction("Index");
            }

            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == id);

            if (item != null)
            {
                item.Quantity = quantity;
            }

            Session["Cart"] = cart;

            return RedirectToAction("Index");
        }


        // Xóa sản phẩm khỏi giỏ.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveFromCart(int id)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == id);

            if (item != null)
            {
                cart.Remove(item);
            }

            Session["Cart"] = cart;

            return RedirectToAction("Index");
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
