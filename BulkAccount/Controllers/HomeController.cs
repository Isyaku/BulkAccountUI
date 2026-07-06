using BulkAccount.Controllers;
using BulkAccount;
using BulkAccount.Data;
using BulkAccount.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting.Internal;
using System.Diagnostics;


namespace BulkAccounts.Controllers
{
    public class HomeController : Controller
    {
        private readonly Microsoft.AspNetCore.Hosting.IHostingEnvironment hostingEnvironment;
        private readonly BulkAccountSolutionDbContext _context;
        public HomeController(Microsoft.AspNetCore.Hosting.IHostingEnvironment hostingEnvironment, BulkAccountSolutionDbContext context)
        {
            this.hostingEnvironment = hostingEnvironment;
            _context = context;
        }

        [HttpGet]
        public IActionResult Index(DateTime? fromDate, DateTime? toDate)
        {
            var user = HttpContext.Session.GetString("user");
            var branchName = HttpContext.Session.GetString("branchName");
            var bulkAcctApprover = HttpContext.Session.GetString("bulkAcctApprover");
            var branchCode = HttpContext.Session.GetString("branchCode");

            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var totalAccts = _context.BulkAccount.Count(a => a.AccountNo != null);

            int totalAcctsFromDate = 0;
            if (fromDate.HasValue && toDate.HasValue)
            {
                totalAcctsFromDate = _context.BulkAccount.Count(a =>
                    a.AccountNo != null &&
                    a.DateOpened >= fromDate.Value &&
                    a.DateOpened <= toDate.Value
                );
            }
            ViewBag.User = user;
            ViewBag.BulkAcctApprover = bulkAcctApprover;
            ViewBag.Branch = branchName;
            ViewBag.BranchCode = branchCode;
            ViewBag.TotalAccts = totalAccts;
            ViewBag.TotalAcctsFromDate = totalAcctsFromDate;
            return View();
        }
        
        public IActionResult DownloadTemplate()
        {
            //var templatesFolder = $"{Directory.GetCurrentDirectory()}\\wwwroot\\template";
            var templatesFolder = Path.Combine(hostingEnvironment.WebRootPath, "template");
            var templatesName = "Bulk_Account_Template.xlsx";
            var templatesType = "application/vnd.ms-excel";

            var memory = DownloadSingleFile(templatesName, templatesFolder);
            return File(memory.ToArray(), templatesType, templatesName);
        }
        public IActionResult DownloadUpload(string fileName)
        {
            //var templatesFolder = $"{Directory.GetCurrentDirectory()}\\wwwroot\\uploads";
            var templatesFolder = Path.Combine(hostingEnvironment.WebRootPath, "uploads");
            var templatesName = fileName;
            var templatesType = "application/vnd.ms-excel";

            var memory = DownloadSingleFile(templatesName, templatesFolder);
            return File(memory.ToArray(), templatesType, templatesName);
        }
        public MemoryStream DownloadSingleFile(string filename, string uploadPath)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), uploadPath, filename);
            var memory = new MemoryStream();

            if (System.IO.File.Exists(path))
            {
                var net = new System.Net.WebClient();
                var data = net.DownloadData(path);
                var content = new System.IO.MemoryStream(data);
                memory = content;
            }
            memory.Position = 0;
            return memory;
        }
        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}