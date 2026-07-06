using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Text;
using BulkAccount.Data;
using BulkAccount.ViewModels;
using BulkAccount.Models;
using ClosedXML.Excel;
using BulkAccount.Helpers;
using System.Globalization;
using System.Text.RegularExpressions;
using Oracle.ManagedDataAccess.Client;
using System.Reflection.Emit;
using Microsoft.Data.SqlClient;

namespace BulkAccount.Controllers
{
    public class BulkAccountUploadController : Controller
    {
        private readonly BulkAccountSolutionDbContext _context;
        private readonly ILogger<BulkAccountUploadController> _logger;
        private readonly Microsoft.AspNetCore.Hosting.IHostingEnvironment hostingEnvironment;
        Utility util = new Utility();
        public BulkAccountUploadController(BulkAccountSolutionDbContext context, Microsoft.AspNetCore.Hosting.IHostingEnvironment hostingEnvironment, ILogger<BulkAccountUploadController> logger)
        {
            _context = context;
            _logger = logger;
            this.hostingEnvironment = hostingEnvironment;
        }
        public void DeleteUpload(string Id)
        {
            List<Account> accountsToRemove = _context.BulkAccount.Where(x => x.UploadId == Id).ToList();
            if (accountsToRemove.Any())
            {
                _context.BulkAccount.RemoveRange(accountsToRemove);
                _context.SaveChanges();
            }

            var uploadToRemove = _context.BulkAccountUpload.FirstOrDefault(x => x.UploadId == Id);
            if (uploadToRemove != null)
            {
                _context.BulkAccountUpload.Remove(uploadToRemove);
                _context.SaveChanges();
            }
        }

        private bool SetSessionData()
        {
            var user = HttpContext.Session.GetString("user");
            if (user == null)
            {
                return false;
            }
            ViewBag.User = user;
            ViewBag.Branch = HttpContext.Session.GetString("branchName");
            ViewBag.BranchCode = HttpContext.Session.GetString("branchCode");
            return true;
        }

        [HttpGet]
        public IActionResult UserUploads()
        {
            if (!SetSessionData())
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = HttpContext.Session.GetString("user");

            var bulkUploads = _context.BulkAccountUpload.Where(b => b.StaffID == user).ToList();
            
            return View(bulkUploads);
        }

        [Route("BulkAccountUpload/UploadExcel")]
        public IActionResult UploadExcel()
        {
            if (!SetSessionData())
            {
                return RedirectToAction("Login", "Auth");
            }
            return View();
        }

        [HttpPost]
        [Route("BulkAccountUpload/UploadExcel")]
        public async Task<IActionResult> UploadExcel(UploadViewModel uploadViewModel)
        {
            if (!SetSessionData())
            {
                return RedirectToAction("Login", "Auth");
            }

            var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();            

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (ModelState.IsValid)
            {
                List<UploadError> errors = new List<UploadError>();
                var uploadId = Guid.NewGuid().ToString();
                string AcountTypeFromDropDown = Convert.ToString(uploadViewModel.AccountType).Trim().ToLower();
                try
                {
                    var user = HttpContext.Session.GetString("user");
                    var file = uploadViewModel.File;
                    var uploadsFolder = Path.Combine(hostingEnvironment.WebRootPath, "uploads");

                    if (file != null && file.Length > 0)
                    {
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        var fileName = $"{timestamp}_{Path.GetFileName(file.FileName)}";

                        if (fileName.Length > 50)
                        {
                            var extension = Path.GetExtension(fileName);
                            fileName = fileName.Substring(0, 50 - extension.Length) + extension;
                        }

                        var filePath = Path.Combine(uploadsFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        DataTable dt = new DataTable();

                        using (var workbook = new XLWorkbook(filePath))
                        {
                            var worksheet = workbook.Worksheet(1);
                            bool firstRow = true;

                            foreach (var row in worksheet.RowsUsed())
                            {
                                if (firstRow)
                                {
                                    // Add columns to DataTable
                                    foreach (var cell in row.Cells())
                                    {
                                        dt.Columns.Add(cell.GetFormattedString());
                                    }
                                    firstRow = false;
                                }
                                else
                                {
                                    // Add rows to DataTable
                                    var newRow = dt.NewRow();
                                    int i = 0;

                                    foreach (var cell in row.Cells(1, dt.Columns.Count))
                                    {
                                        // IMPORTANT: Preserve Excel formatting (date → "21-Jan-2014")
                                        newRow[i] = cell.GetFormattedString();
                                        i++;
                                    }

                                    dt.Rows.Add(newRow);
                                }
                            }
                        }


                        DataRow[] dataRows = dt.Select();

                        int uploadSize = config.GetValue<int>("appConfiguration:uploadSize");

                        if (dataRows.Length > uploadSize)
                        {
                            ViewBag.Error = $"Upload failed: The Excel file contains {dataRows.Length} records. Maximum allowed is 5000.";
                            util.WriteToLog($"Upload rejected — file contains {dataRows.Length} records (limit is 5000).");

                            // Optionally delete file if uploaded
                            if (System.IO.File.Exists(filePath))
                                System.IO.File.Delete(filePath);

                            uploadViewModel.Errors = new List<UploadError>
                            {
                                new UploadError { message = $"Error: The uploaded file exceeds the 5000-row limit ({dataRows.Length} rows found)." }
                            };

                            ViewBag.User = HttpContext.Session.GetString("user");
                            return View(uploadViewModel);
                        }
                        int rowNumber = 1;

                        var bulkAccountUpload = new BulkAccountUpload
                        {
                            UploadId = uploadId,
                            AccountType = AcountTypeFromDropDown.ToLower(),
                            StaffID = user,
                            BranchCode = dataRows[1]["BranchCode"].ToString(),
                            Status = "Pending",
                            FilePath = fileName.Trim(),
                            UploadedCount = dataRows.Count(),
                            UploadDate = DateTime.Now,
                            InitiatorEmail = HttpContext.Session.GetString("InitiatorEmail")
                        };
                        _context.BulkAccountUpload.Add(bulkAccountUpload);
                        await _context.SaveChangesAsync();

                        foreach (DataRow row in dataRows)
                        {
                            rowNumber++;
                            if (!string.IsNullOrEmpty(row["Bvn"].ToString()))
                            {
                                var selectedAccTypeGLCode = AcountTypeFromDropDown == "savings" ? "210801" : AcountTypeFromDropDown == "salary" ? "210101" : AcountTypeFromDropDown == "corporate" ? "210201" : AcountTypeFromDropDown == "kids" ? "210806"  : "210808";
                                var accountType = row["AccountType"].ToString().ToLower().Trim();

                                if (AcountTypeFromDropDown == accountType)
                                {
                                    if (AcountTypeFromDropDown == "savings" || AcountTypeFromDropDown == "salary")
                                    {
                                        if (string.IsNullOrEmpty(row["NIN"].ToString()))
                                        {
                                            errors.Add(new UploadError { message = $"Error: Please enter NIN for Savings/Salary accounts in row number {rowNumber} with BVN: {row["Bvn"]}" });
                                        }
                                    }

                                    var now = DateTime.Now;
                                    var valueDate = now.ToString("dd/MM/yyyy").Replace("/", "");
                                    var BVN_withDate = $"{row["Bvn"].ToString()}{valueDate}";
                                    var accountIDType = string.IsNullOrWhiteSpace(row["IDType"].ToString()) ? "1" : row["IDType"].ToString();
                                    var accountIDNumber = string.IsNullOrWhiteSpace(row["IDNumber"].ToString()) ? BVN_withDate : row["IDNumber"].ToString();

                                    string inputDate = row["DateofBirth"].ToString()?.Trim();

                                    string dob = null;

                                    // Acceptable formats
                                    string allowedFormats = "dd-MMM-yyyy";

                                    if (DateTime.TryParseExact(inputDate, allowedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                                    {
                                        dob = inputDate; // Keep the original format

                                        var account = new Account
                                        {
                                            FName = row["FirstName"].ToString(),
                                            OName = row["OtherName"].ToString(),
                                            SName = row["SurName"].ToString(),
                                            AccountName = row["AccountName"].ToString()!,
                                            Sex = row["sex"].ToString(),
                                            Dob = dob,
                                            Address = row["Address"].ToString()!,
                                            Phone = row["Phone"].ToString()!,
                                            Title = (row["Title"].ToString().ToLower() == "mr") ? 23 : 24,
                                            Bvn = row["Bvn"].ToString()!,
                                            BranchCode = row["BranchCode"].ToString()!,
                                            Status = "0",
                                            FailureReason = "Not Approved",
                                            IDType = accountIDType,
                                            IDNumber = accountIDNumber,
                                            GlCode = selectedAccTypeGLCode,
                                            MktByID = row["MktByID"].ToString()!,
                                            MktForID = row["MktByID"].ToString(),
                                            UploadId = uploadId,
                                            NIN = row["NIN"].ToString()!
                                        };

                                        _context.BulkAccount.Add(account);
                                        await _context.SaveChangesAsync();
                                    }
                                    else
                                    {
                                        errors.Add(new UploadError
                                        {
                                            message = $"Error: Invalid date of birth in row number {rowNumber} with BVN: {row["Bvn"]}. Please use formats like 09-Aug-1958 and 25-Jul-1958."
                                        });
                                    }
                                }
                                else
                                {
                                    errors.Add(new UploadError { message = $"Error: Invalid account type in row number {rowNumber} with BVN: {row["Bvn"]}" });
                                }
                            }
                            else
                            {
                                errors.Add(new UploadError { message = $"Error: No BVN on row number {rowNumber}" });
                            }
                        }
                        if (errors.Count != 0)
                        {
                            DeleteUpload(uploadId);
                            ViewBag.User = HttpContext.Session.GetString("user");
                            uploadViewModel.Errors = errors;
                            return View(uploadViewModel);
                        }

                        ViewBag.SuccessMessage = "File uploaded successfully!";

                        var actionLog = new BulkAccountActionLog
                        {
                            StaffID = user,
                            ActionDate = DateTime.Now,
                            ActionType = $"Initiated a bulk account request with ID {uploadId}"
                        };
                        _context.BulkAccountActionLog.Add(actionLog);
                        await _context.SaveChangesAsync();

                        var approvers = _context.AccountApprover.Where(b => b.StaffEmail != null).ToList();
                        foreach (var approver in approvers)
                        {
                            util.SendNotificationEmail(approver.StaffEmail, approver.StaffName, "You have bulk accounts creation request awaiting your approval.");
                        }
                    }
                }

                catch (Exception ex)
                {
                    ViewBag.Error = ex.Message;
                    DeleteUpload(uploadId);
                    _logger.LogError($"Unable to Upload Excel:::::::{ex.Message}");
                    util.WriteToLog("Unable to Upload Excel");
                }
            }
            ViewBag.User = HttpContext.Session.GetString("user");
            uploadViewModel.Errors = new List<UploadError>();
            return View(uploadViewModel);
        }

        public async Task<IActionResult> UpdateInstance(string uploadId, string instanz)
        {
            int rowsAffected = 0;
            var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

            using (SqlConnection sqlConn = new SqlConnection(config.GetConnectionString("CAO_DbConn")))
            {
                string query = @"UPDATE BulkAccountUpload SET Instancez = @intancez WHERE UploadId = @uploadId";

                using (SqlCommand sqlcmd = new SqlCommand(query, sqlConn))
                {
                    sqlcmd.Parameters.AddWithValue("@intancez", instanz);
                    sqlcmd.Parameters.AddWithValue("@uploadId", uploadId);

                    await sqlConn.OpenAsync();
                    rowsAffected = sqlcmd.ExecuteNonQuery();
                }
            }
           
            if(rowsAffected > 0) { ViewBag.SuccessMessage = "Update completed successfully."; } else { ViewBag.ErrorMessage = "Update failed. No record was modified."; ; }
            return RedirectToAction("UserUploads");
            
        }
    }
}
