using System.ComponentModel.DataAnnotations;

namespace BulkAccount.Models
{
    public class AccountApprover
    {
        [Key]
        public string StaffID { get; set; }
        public string StaffName { get; set; }
        public string StaffEmail { get; set; }

    }
    public class BulkAccountActionLog
    {
        public int Id { get; set; }
        public string StaffID { get; set; }
        public DateTime ActionDate { get; set; }
        public string ActionType { get; set; }

    }
}
