namespace BulkAccount.Models
{
    public class CreateAccountResponse
    {
        public string responseCode { get; set; }
        public string responseMessage { get; set; }
        public string cif { get; set; }
        public string accountNo { get; set; }
    }
}
