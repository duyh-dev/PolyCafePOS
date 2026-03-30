using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace PolyCafeMenuWeb.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                
                if (role == "Manager")
                {
                    return RedirectToAction("Dashboard", "Manager");
                }
                else if (role == "Cashier")
                {
                    return RedirectToAction("Index", "POS");
                }
                else if (role == "Barista")
                {
                    return RedirectToAction("Queue", "Barista");
                }
            }
            
            return RedirectToAction("Login", "Auth");
        }
    }
}
