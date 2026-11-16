using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace OrderCafe.Controllers
{
    public class ChatController : Controller
    {
        private static readonly HttpClient Client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(25)
        };

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Reply(string message)
        {
            message = (message ?? "").Trim();

            if (message.Length == 0 || message.Length > 300)
            {
                return Json(new { reply = "Bạn hãy nhập từ 1 đến 300 ký tự." });
            }

            var apiKey = Environment.GetEnvironmentVariable("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");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return Json(new { reply = "Máy chủ chưa có cấu hình API cho chatbot." });
            }

            var payload = new
            {
                model = "gpt-5.6-terra",
                instructions =
                    "Bạn là trợ lý hỗ trợ khách của OrderCafe. Trả lời ngắn bằng tiếng Việt. " +
                    "Khách chọn món trong menu, thêm vào giỏ hàng rồi vào trang thanh toán. " +
                    "Trang thanh toán có COD (trả tiền khi nhận hàng) và VNPay Sandbox (chế độ thử nghiệm). " +
                    "Khi khách hỏi tên món hoặc giá, hướng dẫn họ bấm Xem menu. " +
                    "Nếu thiếu thông tin về cửa hàng, hãy nói rõ.",
                input = message,
                store = false,
                reasoning = new { effort = "none" },
                max_output_tokens = 300
            };

            try
            {
                using (var request = new HttpRequestMessage(
                    HttpMethod.Post, "https://api.openai.com/v1/responses"))
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", apiKey);

                    request.Content = new StringContent(
                        JsonConvert.SerializeObject(payload),
                        Encoding.UTF8,
                        "application/json");

                    using (var response = await Client.SendAsync(request))
                    {
                        response.EnsureSuccessStatusCode();

                        var data = JObject.Parse(await response.Content.ReadAsStringAsync());
                        var parts = data.SelectTokens("output[*].content[*]")
                            .Where(item => (string)item["type"] == "output_text")
                            .Select(item => (string)item["text"]);

                        var reply = string.Join("\n", parts).Trim();

                        return Json(new
                        {
                            reply = string.IsNullOrEmpty(reply)
                                ? "Bạn có thể hỏi lại theo cách khác được không?"
                                : reply
                        });
                    }
                }
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return Json(new { reply = "AI đang tạm gián đoạn. Bạn thử gửi lại sau nhé." });
            }
        }
    }
}