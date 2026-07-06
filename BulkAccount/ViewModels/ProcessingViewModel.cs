using BulkAccount.Models;

namespace BulkAccount.ViewModels
{
    public class ProcessingViewModel
    {
        public List<Account> AccountUpload { get; set; }
        public string Message { get; set; }
    }
}
