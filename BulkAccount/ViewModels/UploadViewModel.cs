using BulkAccount.Models;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace BulkAccount.ViewModels
{
    public class UploadError
    {
        public string message { get; set; }
    }
    public class UploadViewModel
    {
        [Required]
        [Display(Name = "Account Type")]
        public AccountType AccountType { get; set; }

        [Required]
        [Display(Name = "Excel File")]
        public IFormFile File { get; set; }

        public List<UploadError> Errors { get; set;}


    }
}
