using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers
{
    public class ErrorController : Controller
    {
        public IActionResult AccessDenied()
        {
            return View();
        }

        public IActionResult NotFound()
        {
            return View();
        }
    }
}
