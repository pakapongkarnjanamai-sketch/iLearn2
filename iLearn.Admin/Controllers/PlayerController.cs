using Microsoft.AspNetCore.Mvc;

namespace iLearn.Admin.Controllers
{
    public class PlayerController : Controller
    {
        // รับ id ของ Course หรือ Resource ที่จะเปิด
        public IActionResult Index(int id)
        {
            // ส่ง id ไปให้หน้า View เพื่อใช้ JavaScript เรียก API ดึงข้อมูลจริงต่อ
            ViewBag.ResourceId = id;

            // ใช้ Layout เดียวกับระบบหลัก แต่เราจะซ่อนเมนูด้วย CSS ในหน้า View
            return View();
        }
    }
}