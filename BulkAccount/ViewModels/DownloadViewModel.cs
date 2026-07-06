using BulkAccount.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkAccount.ViewModels
{
    public class DownloadViewModel
    {
        public BulkAccountUpload BulkAccoutUpload { get; set; }
        public List<SelectListItem> BulkAccountUploadList { get; set; }
    }
}
