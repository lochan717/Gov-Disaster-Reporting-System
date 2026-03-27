using Microsoft.AspNetCore.Mvc;

namespace SAMVAD.DMS.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Error()
    {
        ViewData["RequestId"] = HttpContext.TraceIdentifier;
        return View();
    }
}
