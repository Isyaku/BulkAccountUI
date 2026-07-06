using BulkAccount.Data;
using BulkAccount.ViewModels;
using BulkAccount.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using BulkAccount.Helpers;

namespace BulkAccount.Controllers
{
    public class BulkAccountDownloadController : Controller
    {
        private readonly BulkAccountSolutionDbContext _context;
        public BulkAccountDownloadController(BulkAccountSolutionDbContext context)
        {
            _context = context;            
        }

        [Route("BulkAccountDownload/DownloadExcel")]
        public IActionResult DownloadExcel()
        {
            var user = HttpContext.Session.GetString("user");
            var branchCode = HttpContext.Session.GetString("branchCode");
            var branchName = HttpContext.Session.GetString("branchName");
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }
            ViewBag.User = user;
            ViewBag.Branch = branchName;
            ViewBag.BranchCode = branchCode;

            DownloadViewModel downloadListModel = new DownloadViewModel();
            downloadListModel.BulkAccountUploadList = new List<SelectListItem>();

            QueryHelper getList = new QueryHelper();
            var uploadList = getList.GetBulkAccoutUploads(user, _context);

            downloadListModel.BulkAccountUploadList.Add(new SelectListItem
            {
                Text = "Select Uploaded Document",
                Value = ""
            });

            foreach (var item in uploadList)
            {
                downloadListModel.BulkAccountUploadList.Add(new SelectListItem
                {
                    Text = item.FilePath,
                    Value = item.UploadId
                });
            }

            return View(downloadListModel);
        }

        [HttpGet]
        public async Task<FileResult> ExportAccountsToExcel(string uploadId)
        {
            //var fileName = "Bulk_Account.xlsx";
            var upload = _context.BulkAccountUpload.FirstOrDefault(a => a.UploadId == uploadId);
            var fileName = upload.FilePath;

            QueryHelper getList = new QueryHelper();
            var accounts =  getList.GetBulkAccount(uploadId, _context);

            return GenerateExcel(fileName, accounts);
        }
        private FileResult GenerateExcel(string filename, IEnumerable<Account> Accounts)
        {
            DataTable dt = new DataTable("Account");
            dt.Columns.AddRange(new DataColumn[]
            {
                new DataColumn("AccountName"),
                new DataColumn("AccountNo"),
                new DataColumn("Cif"),
                new DataColumn("BranchCode"),
                new DataColumn("FailureReason"),
                new DataColumn("Phone"),
                new DataColumn("Bvn"),
                new DataColumn("Dob"),
                new DataColumn("Address"),
                new DataColumn("Email"),
                new DataColumn("IDType"),
                new DataColumn("IDNumber"),
                new DataColumn("GlCode"),
                new DataColumn("MktByID")
            });

            foreach (var account in Accounts)
            {
                dt.Rows.Add(
                    account.AccountName,
                    account.AccountNo,
                    account.Cif,
                    account.BranchCode,
                    account.FailureReason,
                    account.Phone,
                    account.Bvn,
                    account.Dob,
                    account.Address,
                    account.Email,
                    account.IDType,
                    account.IDNumber,
                    account.GlCode,
                    account.MktByID
                    );
            }
            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.AddWorksheet(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
                }
            }
        }        
    }
}
