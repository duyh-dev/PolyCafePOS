using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PolyCafeMenuWeb.Data;
using PolyCafeMenuWeb.Models;

namespace PolyCafeMenuWeb.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {
        private readonly PolyCafeContext _context;

        public ManagerController(PolyCafeContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard(DateTime? startDate, DateTime? endDate)
        {
            // ===== DEFAULT DATE =====
            startDate ??= DateTime.Today.AddDays(-6);
            endDate ??= DateTime.Today;

            var start = startDate.Value.Date;
            var end = endDate.Value.Date.AddDays(1); // để lấy hết ngày cuối

            // ===== DOANH THU THEO KHOẢNG =====
            var rangeRevenue = await _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate < end && o.Status == "Completed")
                .SumAsync(o => o.TotalAmount);

            // ===== TỔNG ĐƠN =====
            var totalOrders = await _context.Orders
                .CountAsync(o => o.OrderDate >= start && o.OrderDate < end);

            // ===== DOANH THU THÁNG (lấy theo tháng của startDate) =====
            var startOfMonth = new DateTime(start.Year, start.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            var monthRevenue = await _context.Orders
                .Where(o => o.OrderDate >= startOfMonth && o.OrderDate < endOfMonth && o.Status == "Completed")
                .SumAsync(o => o.TotalAmount);

            // ===== TOTAL DRINK =====
            var totalDrinks = await _context.Drinks.CountAsync(d => d.IsActive);

            // ===== CHART (THEO RANGE LUÔN) =====
            var chartRaw = await _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate < end && o.Status == "Completed")
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(x => x.TotalAmount)
                })
                .ToListAsync();

            // đảm bảo đủ ngày (không bị thiếu ngày không có đơn)
            var chartData = new List<object>();
            for (var date = start; date < end; date = date.AddDays(1))
            {
                var found = chartRaw.FirstOrDefault(x => x.Date == date);
                chartData.Add(new
                {
                    Date = date.ToString("dd/MM"),
                    Revenue = found != null ? found.Revenue : 0
                });
            }

            // ===== TOP 5 =====
            var topDrinks = await _context.OrderDetails
                .Where(od => od.Order != null &&
                             od.Order.OrderDate >= start &&
                             od.Order.OrderDate < end &&
                             od.Order.Status == "Completed")
                .GroupBy(od => od.DrinkNameSnapshot)
                .Select(g => new
                {
                    DrinkName = g.Key,
                    TotalQuantity = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(5)
                .ToListAsync();

            // ===== VIEWBAG =====
            ViewBag.StartDate = start.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");

            ViewBag.RangeRevenue = rangeRevenue;
            ViewBag.SelectedDateRevenue = rangeRevenue; // dùng chung cho UI cũ
            ViewBag.MonthRevenue = monthRevenue;
            ViewBag.TotalOrdersOnSelectedDate = totalOrders;
            ViewBag.TotalDrinks = totalDrinks;

            ViewBag.ChartData = chartData;
            ViewBag.TopDrinks = topDrinks;

            return View();
        }

        // --- Categories CRUD ---
        public async Task<IActionResult> Categories()
        {
            return View(await _context.Categories.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> SaveCategory(Category category)
        {
            if (category.CategoryID == 0)
                _context.Categories.Add(category);
            else
                _context.Update(category);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Categories));
        }

        // --- Toppings CRUD ---
        public async Task<IActionResult> Toppings()
        {
            return View(await _context.Toppings.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> SaveTopping(Topping topping)
        {
            if (topping.ToppingID == 0)
                _context.Toppings.Add(topping);
            else
                _context.Update(topping);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Toppings));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTopping(int id)
        {
            var topping = await _context.Toppings.FindAsync(id);
            if (topping != null)
            {
                _context.Toppings.Remove(topping);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Toppings));
        }

        // --- Employees CRUD ---
        public async Task<IActionResult> Employees()
        {
            return View(await _context.Employees.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> SaveEmployee(Employee employee)
        {
            if (employee.EmployeeID == 0)
            {
                employee.CreatedAt = DateTime.Now;
                _context.Employees.Add(employee);
            }
            else
            {
                var existing = await _context.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeID == employee.EmployeeID);
                if (existing != null)
                {
                    employee.CreatedAt = existing.CreatedAt;
                    _context.Update(employee);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Employees));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Employees));
        }

        // --- Drinks & Variants CRUD ---
        public async Task<IActionResult> Drinks()
        {
            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            var drinks = await _context.Drinks
                .Include(d => d.Category)
                .Include(d => d.Variants)
                .ToListAsync();
            return View(drinks);
        }

        [HttpGet]
        public async Task<IActionResult> GetDrinkVariants(int id)
        {
            var variants = await _context.DrinkVariants
                .Where(v => v.DrinkID == id && v.IsActive)
                .Select(v => new { v.VariantID, v.SizeName, v.Price })
                .ToListAsync();
            return Json(variants);
        }

        [HttpPost]
        public async Task<IActionResult> SaveDrink(Drink drink)
        {
            if (drink.DrinkID == 0)
            {
                drink.CreatedAt = DateTime.Now;
                _context.Drinks.Add(drink);
            }
            else
            {
                var existing = await _context.Drinks
                    .Include(d => d.Variants)
                    .FirstOrDefaultAsync(d => d.DrinkID == drink.DrinkID);
                
                if (existing != null)
                {
                    existing.DrinkName = drink.DrinkName;
                    existing.CategoryID = drink.CategoryID;
                    existing.ImageUrl = drink.ImageUrl;
                    existing.IsActive = drink.IsActive;

                    // Update variants safely instead of drop-and-create
                    var submittedVariantIds = drink.Variants?.Select(v => v.VariantID).Where(id => id > 0).ToList() ?? new List<int>();

                    // 1. Soft delete missing variants
                    foreach (var ev in existing.Variants)
                    {
                        if (!submittedVariantIds.Contains(ev.VariantID))
                        {
                            ev.IsActive = false; // Mark inactive instead of deleting to save Order details referencing it
                        }
                    }

                    // 2. Add or update submitted variants
                    if (drink.Variants != null)
                    {
                        foreach (var v in drink.Variants)
                        {
                            if (v.VariantID == 0)
                            {
                                v.DrinkID = existing.DrinkID;
                                existing.Variants.Add(v); // New
                            }
                            else
                            {
                                var existingVariant = existing.Variants.FirstOrDefault(ev => ev.VariantID == v.VariantID);
                                if (existingVariant != null)
                                {
                                    existingVariant.SizeName = v.SizeName;
                                    existingVariant.Price = v.Price;
                                    existingVariant.IsActive = true;
                                }
                            }
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Drinks));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDrink(int id)
        {
            var drink = await _context.Drinks
                .Include(d => d.Variants)
                .FirstOrDefaultAsync(d => d.DrinkID == id);

            if (drink != null)
            {
                // Kiểm tra xem có OrderDetail nào đang tham chiếu đến các biến thể của sản phẩm này không
                var variantIds = drink.Variants.Select(v => (int?)v.VariantID).ToList();
                bool hasOrderReferences = variantIds.Any() &&
                    await _context.OrderDetails.AnyAsync(od => variantIds.Contains(od.VariantID));

                if (hasOrderReferences)
                {
                    // Soft delete: ẩn sản phẩm thay vì xóa hẳn để bảo toàn lịch sử đơn hàng
                    drink.IsActive = false;
                    foreach (var variant in drink.Variants)
                    {
                        variant.IsActive = false;
                    }
                    _context.Update(drink);
                }
                else
                {
                    // Không có đơn hàng nào tham chiếu → xóa thật sự
                    _context.DrinkVariants.RemoveRange(drink.Variants);
                    _context.Drinks.Remove(drink);
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Drinks));
        }

        // --- Reports ---
        public async Task<IActionResult> Reports(DateTime? startDate, DateTime? endDate)
        {
            startDate ??= DateTime.Today.AddDays(-30);
            endDate ??= DateTime.Today.AddDays(1).AddTicks(-1);

            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            var orders = await _context.Orders
                .Include(o => o.Employee)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Drink)
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.TotalRevenue = orders.Where(o => o.Status == "Completed").Sum(o => o.TotalAmount);
            ViewBag.CompletedOrders = orders.Count(o => o.Status == "Completed");
            ViewBag.PendingOrders = orders.Count(o => o.Status == "Pending");

            return View(orders);
        }

        public async Task<IActionResult> ExportExcel(DateTime? startDate, DateTime? endDate)
        {
            startDate ??= DateTime.Today.AddDays(-30);
            endDate ??= DateTime.Today.AddDays(1).AddTicks(-1);

            var orders = await _context.Orders
                .Include(o => o.Employee)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Drink)
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var totalRevenue = orders.Where(o => o.Status == "Completed").Sum(o => o.TotalAmount);
            var completedOrders = orders.Count(o => o.Status == "Completed");
            var pendingOrders = orders.Count(o => o.Status == "Pending");

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Báo cáo doanh thu");

            // === HEADER SECTION ===
            ws.Cell("A1").Value = "BÁO CÁO DOANH THU - POLYCAFE";
            ws.Range("A1:G1").Merge().Style
                .Font.SetBold(true)
                .Font.SetFontSize(16)
                .Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#2E7D32"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            ws.Row(1).Height = 35;

            // Date range info
            ws.Cell("A2").Value = $"Từ ngày: {startDate:dd/MM/yyyy}";
            ws.Cell("D2").Value = $"Đến ngày: {endDate:dd/MM/yyyy}";
            ws.Range("A2:C2").Merge().Style.Font.SetBold(true).Font.SetFontSize(11);
            ws.Range("D2:G2").Merge().Style.Font.SetBold(true).Font.SetFontSize(11);
            ws.Row(2).Height = 22;

            // Summary row
            ws.Cell("A3").Value = $"Tổng doanh thu (Hoàn thành): {totalRevenue:N0} ₫";
            ws.Range("A3:C3").Merge().Style.Font.SetBold(true).Font.SetFontColor(XLColor.FromHtml("#2E7D32")).Font.SetFontSize(11);
            ws.Cell("D3").Value = $"Đơn hoàn thành: {completedOrders}  |  Đơn chờ: {pendingOrders}";
            ws.Range("D3:G3").Merge().Style.Font.SetFontSize(11);
            ws.Row(3).Height = 22;

            // Blank separator row
            ws.Row(4).Height = 8;

            // === TABLE HEADER ===
            var headerRow = 5;
            string[] headers = { "STT", "Mã đơn hàng", "Ngày & Giờ", "Thu ngân", "Chi tiết sản phẩm", "Tổng tiền (₫)", "Trạng thái" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(headerRow, i + 1).Value = headers[i];
            }

            var headerRange = ws.Range(headerRow, 1, headerRow, 7);
            headerRange.Style
                .Font.SetBold(true)
                .Font.SetFontColor(XLColor.White)
                .Font.SetFontSize(11)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#424242"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Thin)
                .Border.SetOutsideBorderColor(XLColor.FromHtml("#616161"))
                .Border.SetInsideBorderColor(XLColor.FromHtml("#616161"));
            ws.Row(headerRow).Height = 28;

            // === DATA ROWS ===
            int row = headerRow + 1;
            int stt = 1;
            foreach (var order in orders)
            {
                // Build item details string
                var itemDetails = string.Join("\n", order.OrderDetails.Select(od =>
                    $"{od.Quantity}x {od.DrinkNameSnapshot} ({od.SizeSnapshot}) - {od.SubTotal:N0}₫"
                ));

                ws.Cell(row, 1).Value = stt;
                ws.Cell(row, 2).Value = order.OrderCode;
                ws.Cell(row, 3).Value = order.OrderDate.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(row, 4).Value = order.Employee?.FullName ?? "Không rõ";
                ws.Cell(row, 5).Value = itemDetails;
                ws.Cell(row, 6).Value = order.TotalAmount;
                ws.Cell(row, 7).Value = order.Status switch
                {
                    "Completed" => "Hoàn thành",
                    "Pending" => "Đang chờ",
                    "Cancelled" => "Đã hủy",
                    _ => order.Status
                };

                // Data row styling
                var dataRange = ws.Range(row, 1, row, 7);
                dataRange.Style
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetInsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetOutsideBorderColor(XLColor.FromHtml("#E0E0E0"))
                    .Border.SetInsideBorderColor(XLColor.FromHtml("#E0E0E0"))
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center);

                // Alternating row colors
                if (stt % 2 == 0)
                {
                    dataRange.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#F5F5F5"));
                }

                // Center-align STT, Date, Status columns
                ws.Cell(row, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(row, 3).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                ws.Cell(row, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                // Format currency column
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 6).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

                // Wrap text for item details
                ws.Cell(row, 5).Style.Alignment.SetWrapText(true);

                // Status color coding
                switch (order.Status)
                {
                    case "Completed":
                        ws.Cell(row, 7).Style.Font.SetFontColor(XLColor.FromHtml("#2E7D32")).Font.SetBold(true);
                        break;
                    case "Pending":
                        ws.Cell(row, 7).Style.Font.SetFontColor(XLColor.FromHtml("#F57F17")).Font.SetBold(true);
                        break;
                    case "Cancelled":
                        ws.Cell(row, 7).Style.Font.SetFontColor(XLColor.FromHtml("#C62828")).Font.SetBold(true);
                        break;
                }

                row++;
                stt++;
            }

            // === TOTAL ROW ===
            ws.Cell(row, 1).Value = "";
            ws.Range(row, 1, row, 5).Merge();
            ws.Cell(row, 1).Value = "TỔNG CỘNG";
            ws.Cell(row, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(12)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            ws.Cell(row, 6).Value = orders.Sum(o => o.TotalAmount);
            ws.Cell(row, 6).Style
                .Font.SetBold(true)
                .Font.SetFontSize(12)
                .NumberFormat.Format = "#,##0";
            ws.Cell(row, 6).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            ws.Range(row, 1, row, 7).Style
                .Fill.SetBackgroundColor(XLColor.FromHtml("#E8F5E9"))
                .Border.SetOutsideBorder(XLBorderStyleValues.Medium)
                .Border.SetOutsideBorderColor(XLColor.FromHtml("#2E7D32"));
            ws.Row(row).Height = 28;

            // Footer info
            row += 2;
            ws.Cell(row, 1).Value = $"Ngày xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            ws.Range(row, 1, row, 4).Merge().Style.Font.SetItalic(true).Font.SetFontColor(XLColor.Gray);

            // === COLUMN WIDTHS ===
            ws.Column(1).Width = 8;    // STT
            ws.Column(2).Width = 24;   // Mã đơn
            ws.Column(3).Width = 20;   // Ngày giờ
            ws.Column(4).Width = 20;   // Thu ngân
            ws.Column(5).Width = 45;   // Chi tiết SP
            ws.Column(6).Width = 18;   // Tổng tiền
            ws.Column(7).Width = 16;   // Trạng thái

            // Set default font for the entire worksheet
            ws.Style.Font.SetFontName("Arial");
            ws.Style.Font.SetFontSize(10);

            // Freeze the header row
            ws.SheetView.FreezeRows(headerRow);

            // Print settings
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.FitToPages(1, 0);
            ws.PageSetup.PrintAreas.Add("A1:G" + (row));

            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"BaoCaoDoanhThu_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
