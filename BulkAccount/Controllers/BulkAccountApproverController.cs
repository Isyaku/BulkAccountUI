using BulkAccount.Data;
using BulkAccount.Helpers;
using BulkAccount.Models;
using BulkAccount.ViewModels;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.Data;

namespace BulkAccount.Controllers
{
    public class BulkAccountApprover : Controller
    {
        private readonly BulkAccountSolutionDbContext _context;
        Utility util = new Utility();
        public BulkAccountApprover(BulkAccountSolutionDbContext context)
        {
            _context = context;
        }
        private bool SetSessionData()
        {
            var user = HttpContext.Session.GetString("user");
            if (user == null)
            {
                return false;
            }
            ViewBag.User = user;
            ViewBag.BulkAcctApprover = HttpContext.Session.GetString("bulkAcctApprover");
            ViewBag.Branch = HttpContext.Session.GetString("branchName");
            ViewBag.BranchCode = HttpContext.Session.GetString("branchCode");
            return true;
        }
        public IActionResult Index()
        {
            if (!SetSessionData())
            {
                return RedirectToAction("Login", "Auth");
            }

            var bulkUploads = _context.BulkAccountUpload.Where(b => b.Status == "Pending").OrderByDescending(b => b.UploadDate).ToList();
            return View(bulkUploads);
        }

        [HttpGet]
        public IActionResult ApproveReject(string uploadID)
        {
            if (!SetSessionData())
            {
                return RedirectToAction("Login", "Auth");
            }

            var bulkUpload = _context.BulkAccountUpload.FirstOrDefault(b => b.UploadId == uploadID);
            if (bulkUpload == null)
            {
                return NotFound();
            }
            return View(bulkUpload);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveReject(string uploadID, bool isApproved, string rejectionReason)
        {
            if (!SetSessionData())
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                var upload = await _context.BulkAccountUpload
                    .FirstOrDefaultAsync(e => e.UploadId == uploadID);

                if (upload == null)
                {
                    return NotFound();
                }

                var user = HttpContext.Session.GetString("user");

                var accounts = await _context.BulkAccount.Where(e => e.UploadId == uploadID).ToListAsync();
                string message = "";

                if (isApproved)
                {
                    upload.Status = "Approved";
                    message = "Kindly wait for a notification upon completion!";

                    foreach (var account in accounts)
                    {
                        account.FailureReason = "Ready for processing";
                    }

                    _context.BulkAccountActionLog.Add(new BulkAccountActionLog
                    {
                        StaffID = user,
                        ActionDate = DateTime.Now,
                        ActionType = $"Approved a bulk account request with ID {uploadID}"
                    });
                }
                else
                {
                    if (upload.Status != "Rejected" && upload.Status != "Completed")
                    {
                        upload.Status = "Rejected";
                        upload.RejectionReason = rejectionReason;

                        foreach (var account in accounts)
                        {
                            account.FailureReason = "Rejected";
                        }

                        _context.BulkAccountActionLog.Add(new BulkAccountActionLog
                        {
                            StaffID = user,
                            ActionDate = DateTime.Now,
                            ActionType = $"Rejected a bulk account request with ID {uploadID}"
                        });
                    }
                }

                await _context.SaveChangesAsync();

                var model = new ProcessingViewModel
                {
                    AccountUpload = accounts,
                    Message = message
                };

                return View("CompleteProcessing", model);
            }
            catch (Exception ex)
            {
                util.WriteToLog($"Unable to process accounts: {ex.Message}");
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }

    }
}

