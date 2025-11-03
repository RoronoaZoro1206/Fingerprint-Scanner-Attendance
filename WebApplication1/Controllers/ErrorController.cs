using Microsoft.AspNetCore.Mvc;

namespace Fingerprint_Attendance_System.Controllers
{
    public class ErrorController : Controller
    {
        public IActionResult AccessDenied()
        {
            return View();
        }

        public new IActionResult NotFound()
        {
            return View();
        }
    }
}
