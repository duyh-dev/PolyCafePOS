using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolyCafeMenuWeb.Data;
using PolyCafeMenuWeb.Models;
using System.Security.Claims;

namespace PolyCafeMenuWeb.Controllers
{
    [Authorize(Roles = "Manager,Cashier")]
    public class POSController : Controller
    {
        private readonly PolyCafeContext _context;

        public POSController(PolyCafeContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            ViewBag.Toppings = await _context.Toppings.Where(t => t.IsActive).ToListAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDrinks(int? categoryId)
        {
            var query = _context.Drinks
                .Include(d => d.Variants)
                .Where(d => d.IsActive);

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(d => d.CategoryID == categoryId.Value);
            }

            var drinks = await query.Select(d => new
            {
                d.DrinkID,
                d.DrinkName,
                d.ImageUrl,
                Variants = d.Variants.Where(v => v.IsActive).Select(v => new { v.VariantID, v.SizeName, v.Price }).ToList()
            }).ToListAsync();

            return Json(drinks);
        }

        [HttpPost]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            try
            {
                var cashierIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(cashierIdStr, out int cashierId))
                {
                    return BadRequest("Invalid Cashier Session");
                }

                // Generate Order Code (e.g. ORD-20231024-001)
                string datePrefix = DateTime.Now.ToString("yyyyMMdd");
                int todayOrderCount = await _context.Orders.CountAsync(o => o.OrderDate.Date == DateTime.Today);
                string orderCode = $"ORD-{datePrefix}-{(todayOrderCount + 1).ToString("D3")}";

                var newOrder = new Order
                {
                    OrderCode = orderCode,
                    CashierID = cashierId,
                    OrderDate = DateTime.Now,
                    Status = "Pending", // Sent to Barista
                    TotalAmount = request.TotalAmount,
                    Note = request.Note
                };

                _context.Orders.Add(newOrder);
                await _context.SaveChangesAsync(); // To get OrderID

                // Track total explicitly in backend to avoid client manipulation
                decimal backendProcessedTotal = 0;

                foreach (var item in request.Items)
                {
                    var drink = await _context.Drinks.FindAsync(item.DrinkID);
                    var variant = await _context.DrinkVariants.FindAsync(item.VariantID);
                    
                    if (drink == null || variant == null) continue;

                    var orderDetail = new OrderDetail
                    {
                        OrderID = newOrder.OrderID,
                        DrinkID = drink.DrinkID,
                        VariantID = variant.VariantID,
                        DrinkNameSnapshot = drink.DrinkName,
                        SizeSnapshot = variant.SizeName,
                        PriceSnapshot = variant.Price,
                        Quantity = item.Quantity,
                        ImageSnapshot = drink.ImageUrl
                    };

                    decimal toppingTotal = 0;
                    
                    if (item.Toppings != null && item.Toppings.Any())
                    {
                        foreach (var tId in item.Toppings)
                        {
                            var topping = await _context.Toppings.FindAsync(tId);
                            if (topping != null)
                            {
                                var detailTopping = new OrderDetailTopping
                                {
                                    ToppingID = topping.ToppingID,
                                    ToppingNameSnapshot = topping.ToppingName,
                                    PriceSnapshot = topping.ExtraPrice,
                                    Quantity = item.Quantity // Topping quantity is bound to drink quantity
                                };
                                orderDetail.OrderDetailToppings.Add(detailTopping);
                                toppingTotal += (topping.ExtraPrice * item.Quantity);
                            }
                        }
                    }

                    orderDetail.ToppingTotal = toppingTotal;
                    orderDetail.SubTotal = (variant.Price * item.Quantity) + toppingTotal;
                    
                    backendProcessedTotal += orderDetail.SubTotal;
                    _context.OrderDetails.Add(orderDetail);
                }

                // Validate Payment (simulate payment creation)
                var defaultPaymentMethod = await _context.PaymentMethods.FirstOrDefaultAsync();
                
                var payment = new Payment
                {
                    OrderID = newOrder.OrderID,
                    MethodID = defaultPaymentMethod?.MethodID ?? 1, // Fallback
                    Amount = backendProcessedTotal,
                    CashGiven = request.CashGiven,
                    ChangeAmount = request.CashGiven - backendProcessedTotal,
                    Status = "Completed",
                    CreatedAt = DateTime.Now
                };

                // Update True total in case client manipulated it
                newOrder.TotalAmount = backendProcessedTotal;
                
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                return Json(new { success = true, orderId = newOrder.OrderID, orderCode = newOrder.OrderCode });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // --- View Order History for Cashier ---
        public async Task<IActionResult> History()
        {
            var cashierIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(cashierIdStr, out int cashierId))
            {
                var orders = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .Where(o => o.CashierID == cashierId && o.OrderDate.Date == DateTime.Today)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
                return View(orders);
            }
            return View(new List<Order>());
        }
    }

    public class CheckoutRequest
    {
        public decimal TotalAmount { get; set; }
        public decimal CashGiven { get; set; }
        public string? Note { get; set; }
        public List<CartItem> Items { get; set; } = new List<CartItem>();
    }

    public class CartItem
    {
        public int DrinkID { get; set; }
        public int VariantID { get; set; }
        public int Quantity { get; set; }
        public List<int> Toppings { get; set; } = new List<int>();
    }
}
