using BulkAccount.Models;
using Newtonsoft.Json;
using NINServiceReference;
using System.Net;

namespace BulkAccount.Helpers
{
    public class Validation
    {
        public async Task<(string firstName, string otherNames, string surName, string DOB, string sex)> GetNINDetails(string nin)
        {
            var jzService_1 = new JaizHelperSoapClient(0);

            var response = await jzService_1.SearchNIMCAsync(nin);

            var data = response.data?.FirstOrDefault();

            if (data == null)
                return (null, null, null, null, null);

            return (data.firstname, data.middlename, data.surname, data.birthdate, data.gender);
        }

    }
}
