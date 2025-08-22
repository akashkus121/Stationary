using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using Stationary.Data;
using Stationary.Models;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Stationary.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Login
        public IActionResult Login()
        {
            return View();
        }
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            return View(); // Create Product page
        }


        public IActionResult Products()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            return View(_db.Products.ToList());
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product, IFormFile image)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            if (image != null && image.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                string fileName = Path.GetFileName(image.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                product.ImagePath = "/images/" + fileName;
            }

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            return RedirectToAction("Products");
        }

        // Reports
        [HttpGet]
        public IActionResult Reports(DateTime? date)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            DateTime selected = (date ?? DateTime.Today).Date;
            DateTime start = selected;
            DateTime end = selected.AddDays(1);

            var orders = _db.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.Date >= start && o.Date < end)
                .ToList();

            var userIds = orders.Select(o => o.UserId).Distinct().ToList();
            var users = _db.Users.Where(u => userIds.Contains(u.Id)).ToDictionary(u => u.Id, u => u.Username);

            var vm = new SalesReportViewModel
            {
                SelectedDate = selected,
                TotalOrders = orders.Count,
                TotalSalesAmount = orders.Sum(o => o.TotalAmount),
                TotalItemsSold = orders.Sum(o => o.OrderItems != null ? o.OrderItems.Sum(i => i.Quantity) : 0),
                Rows = orders.OrderByDescending(o => o.Date).Select(o => new SalesReportRow
                {
                    OrderId = o.Id,
                    OrderDate = o.Date,
                    Username = users.TryGetValue(o.UserId, out var name) ? name : "User#" + o.UserId,
                    Items = o.OrderItems != null ? o.OrderItems.Sum(i => i.Quantity) : 0,
                    Amount = o.TotalAmount
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public IActionResult ReportsExcel(DateTime? date)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            DateTime selected = (date ?? DateTime.Today).Date;
            DateTime start = selected;
            DateTime end = selected.AddDays(1);

            var orders = _db.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.Date >= start && o.Date < end)
                .ToList();

            var userIds = orders.Select(o => o.UserId).Distinct().ToList();
            var users = _db.Users.Where(u => userIds.Contains(u.Id)).ToDictionary(u => u.Id, u => u.Username);

            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Sales Report");
                int row = 1;
                sheet.Cell(row, 1).Value = $"Sales Report - {selected:yyyy-MM-dd}";
                sheet.Range(row, 1, row, 5).Merge().Style.Font.SetBold().Font.FontSize = 14;
                row += 2;

                // Headers
                sheet.Cell(row, 1).Value = "Order ID";
                sheet.Cell(row, 2).Value = "Date";
                sheet.Cell(row, 3).Value = "User";
                sheet.Cell(row, 4).Value = "Items";
                sheet.Cell(row, 5).Value = "Amount";
                sheet.Range(row, 1, row, 5).Style.Font.SetBold();
                row++;

                foreach (var o in orders.OrderBy(o => o.Date))
                {
                    sheet.Cell(row, 1).Value = o.Id;
                    sheet.Cell(row, 2).Value = o.Date.ToString("yyyy-MM-dd HH:mm");
                    sheet.Cell(row, 3).Value = users.TryGetValue(o.UserId, out var name) ? name : "User#" + o.UserId;
                    sheet.Cell(row, 4).Value = o.OrderItems?.Sum(i => i.Quantity) ?? 0;
                    sheet.Cell(row, 5).Value = o.TotalAmount;
                    row++;
                }

                row++;
                sheet.Cell(row, 4).Value = "Total Orders:";
                sheet.Cell(row, 5).Value = orders.Count;
                row++;
                sheet.Cell(row, 4).Value = "Total Items:";
                sheet.Cell(row, 5).Value = orders.Sum(o => o.OrderItems != null ? o.OrderItems.Sum(i => i.Quantity) : 0);
                row++;
                sheet.Cell(row, 4).Value = "Total Sales:";
                sheet.Cell(row, 5).Value = orders.Sum(o => o.TotalAmount);

                sheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    var fileName = $"SalesReport_{selected:yyyyMMdd}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        [HttpGet]
        public IActionResult ReportsPdf(DateTime? date)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            DateTime selected = (date ?? DateTime.Today).Date;
            DateTime start = selected;
            DateTime end = selected.AddDays(1);

            var orders = _db.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.Date >= start && o.Date < end)
                .OrderBy(o => o.Date)
                .ToList();

            var userIds = orders.Select(o => o.UserId).Distinct().ToList();
            var users = _db.Users.Where(u => userIds.Contains(u.Id)).ToDictionary(u => u.Id, u => u.Username);

            byte[] pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Header().Text($"Sales Report - {selected:yyyy-MM-dd}").SemiBold().FontSize(18);
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Order ID").SemiBold();
                            header.Cell().Element(CellStyle).Text("Date").SemiBold();
                            header.Cell().Element(CellStyle).Text("User").SemiBold();
                            header.Cell().Element(CellStyle).Text("Items").SemiBold();
                            header.Cell().Element(CellStyle).Text("Amount").SemiBold();

                            static IContainer CellStyle(IContainer container) => container.PaddingVertical(4).DefaultTextStyle(x => x.FontSize(10));
                        });

                        foreach (var o in orders)
                        {
                            table.Cell().Element(Cell).Text(o.Id.ToString());
                            table.Cell().Element(Cell).Text(o.Date.ToString("yyyy-MM-dd HH:mm"));
                            table.Cell().Element(Cell).Text(users.TryGetValue(o.UserId, out var name) ? name : ("User#" + o.UserId));
                            table.Cell().Element(Cell).Text((o.OrderItems?.Sum(i => i.Quantity) ?? 0).ToString());
                            table.Cell().Element(Cell).Text(o.TotalAmount.ToString("0.00"));

                            static IContainer Cell(IContainer container) => container.PaddingVertical(3).DefaultTextStyle(x => x.FontSize(10));
                        }
                    });

                    var totalOrders = orders.Count;
                    var totalItems = orders.Sum(o => o.OrderItems != null ? o.OrderItems.Sum(i => i.Quantity) : 0);
                    var totalSales = orders.Sum(o => o.TotalAmount);

                    page.Footer().AlignRight().Text($"Totals — Orders: {totalOrders} | Items: {totalItems} | Sales: {totalSales:0.00}").FontSize(10);
                });
            }).GeneratePdf();

            var pdfName = $"SalesReport_{selected:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", pdfName);
        }

    }
}
