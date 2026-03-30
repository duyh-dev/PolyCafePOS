using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolyCafeMenuWeb.Data;
using PolyCafeMenuWeb.Models;

namespace PolyCafeMenuWeb.Controllers
{
    [Authorize(Roles = "Manager,Barista")]
    public class BaristaController : Controller
    {
        private readonly PolyCafeContext _context;

        public BaristaController(PolyCafeContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Queue()
        {
            var pendingOrders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.OrderDetailToppings)
                .Where(o => o.Status == "Pending" && o.OrderDate.Date == DateTime.Today)
                .OrderBy(o => o.OrderDate)
                .ToListAsync();

            return View(pendingOrders);
        }

        [HttpPost]
        public async Task<IActionResult> MarkCompleted(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null && order.Status == "Pending")
            {
                order.Status = "Completed";
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Order not found or already processed" });
        }
    }
}
