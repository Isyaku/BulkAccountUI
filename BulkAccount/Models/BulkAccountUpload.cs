using System.ComponentModel.DataAnnotations;

namespace BulkAccount.Models
{
    public class BulkAccountUpload
    {
        [Key]
        public string UploadId { get; set; }
        public string AccountType { get; set; }
        public string? StaffID { get; set; }
        public string? BranchCode { get; set; }
        public string? Status {get; set; }
        public string? FilePath { get; set; }
        public int? UploadedCount { get; set; }
        public int? CreatedCount { get; set; }
        public DateTime UploadDate { get; set; }
        public string? InitiatorEmail { get; set; }
        public string? RejectionReason { get; set;}
        public string? Instancez { get; set;}
    }
}
