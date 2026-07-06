using BulkAccount.Data;
using BulkAccount.Models;
using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using System.Web.Mvc;

namespace BulkAccount.Helpers
{
    public class QueryHelper
    {
        public IQueryable<BulkAccountUpload> GetBulkAccoutUploads(string uploader, BulkAccountSolutionDbContext _context)
        {
            var sql = $"SELECT UploadId, StaffID, BranchCode, UploadDate, Status, FilePath, UploadedCount, CreatedCount, InitiatorEmail, RejectionReason, AccountType, Instancez FROM BulkAccountUpload WHERE StaffID = '{uploader}' ORDER BY UploadDate DESC";
            var result = _context.BulkAccountUpload.FromSqlRaw(sql);
            return result;
        }
        public IQueryable<Account> GetBulkAccount(string uploadId, BulkAccountSolutionDbContext _context)
        {
            var sql = $"SELECT AccountID,FName,SName,OName,AccountName,Sex,Dob,Address,Phone,Title,Bvn,NIN,AccountNo,Cif,DateOpened,BranchCode,Status,FailureReason,IDNumber,Email,GlCode,MktByID,MktForID,OtherField,IDType,UploadId FROM BulkAccount Where UploadId = '{uploadId}'";
            var result = _context.BulkAccount.FromSqlRaw(sql);
            return result;
        }
        public string ConfigHelper(string key)
        {
            var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
            .Build();

            // Read the configuration value
            string result = configuration[$"{key}"];
            return result;
        }
    }
}
