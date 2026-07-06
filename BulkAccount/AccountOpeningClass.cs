using BulkAccount.Controllers;
using BulkAccount.Data;
using BulkAccount.Helpers;
using BulkAccount.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RestSharp;
using System.Data;
using System.Net;

namespace BulkAccount
{
    public class AccountOpeningClass
    {
        private readonly IServiceProvider _serviceProvider;
        QueryHelper getConfiguration = new QueryHelper();
        Utility util = new Utility();
        private readonly ILogger<BulkAccountUploadController> _logger;
        public AccountOpeningClass(IServiceProvider serviceProvider, ILogger<BulkAccountUploadController> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        private BVNResponse GetBVNDetails(string bvn)
        {
            string result = string.Empty;
            var BVNCheckURL = getConfiguration.ConfigHelper("appConfiguration:BVNCheckURL");
            string postData = JsonConvert.SerializeObject(new { bvn });
            var request = (HttpWebRequest)WebRequest.Create(BVNCheckURL);
            request.Method = "POST";
            request.ContentType = "application/json";

            try
            {
                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(postData);
                }

                var response = (HttpWebResponse)request.GetResponse();
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting BVN details", ex.Message);
                util.WriteToLog($"Error getting BVN details::::{bvn}");
            }

            return JsonConvert.DeserializeObject<BVNResponse>(result);
        }
        
        private async Task<CreateAccountResponse> CreateAccountAsync(BulkAccountSolutionDbContext context, AccountOpeningRequest request)
        {
            var url = getConfiguration.ConfigHelper("appConfiguration:AccountOpeningURL");
            var auth = "JAIZAUTHtygb7858gbuy75tvybh64B6BED96FC2097E06B42E53C3D6E3409D626AC91FC3C8E0A011E11ACD08ED76E75867990898065567u6577867hjg7889";

            try
            {
                var client = new RestClient(url);
                var restRequest = new RestRequest("AccountCreationNew", Method.Post);
                restRequest.AddHeader("content-type", "application/json");
                restRequest.AddHeader("Authorization", auth);
                restRequest.AddHeader("SIGNATURE_METH", "SHA256");
                restRequest.AddParameter("application/json", JsonConvert.SerializeObject(request), ParameterType.RequestBody);

                var response = client.Execute(restRequest);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var accountResponse = JsonConvert.DeserializeObject<CreateAccountResponse>(response.Content.ToString());

                    if (accountResponse.responseCode == "00")
                    {
                        return accountResponse;                        
                    }
                    else
                    {
                        await HandleAccountCreationFailures(context, response.Content, request.bvn);
                        return null;
                    }                    
                }
                else
                {
                    await UpdateFailureReason(context, request.bvn, "Unable to create account, try again later.", "0");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to reach CreateAccount API", ex.Message);
                util.WriteToLog("Unable to reach CreateAccount API" + ex.Message);
            }

            return new CreateAccountResponse();
        }

        private async Task HandleAccountCreationFailures(BulkAccountSolutionDbContext context, string responseContent, string bvn)
        {
            if (responseContent.Contains("Invalid BVN"))
            {
                await UpdateFailureReason(context, bvn, "Invalid BVN", "4");
            }
            else if (responseContent.Contains("Invalid DOB"))
            {
                await UpdateFailureReason(context, bvn, "Account holder must be 18 years and above", "6");
            }
            else if (responseContent.ToUpper().Contains("TIERED"))
            {
                await UpdateFailureReason(context, bvn, "Cust has acct. Tiered acct can't be created", "6");
            }
            else if (responseContent.Contains("This customer has an existing savings"))
            {
                await UpdateFailureReason(context, bvn, "This customer has an existing savings account with Jaiz Bank.", "6");
            }
            else if (responseContent.Contains("problem with cif"))
            {
                await UpdateFailureReason(context, bvn, "Error creating account, problem with cif", "6");
            }
            else if (responseContent.Contains("Address cannot be empty"))
            {
                await UpdateFailureReason(context, bvn, "Address cannot be empty", "6");
            }
            else if (responseContent.Contains("Telephone cannot be empty"))
            {
                await UpdateFailureReason(context, bvn, "Telephone not on BVN", "6");
            }
            else { await UpdateFailureReason(context, bvn, "Unable to create account, try again later.", "0"); }

        }
        private async Task UpdateFailureReason(BulkAccountSolutionDbContext context, string bvn, string reason, string status)
        {
            context.BulkAccount.Where(v => v.Bvn == bvn).ExecuteUpdate(setters => setters
                .SetProperty(b => b.FailureReason, b => reason)
                .SetProperty(b => b.Status, b => status));
            await context.SaveChangesAsync();
        }
        private async Task UpdateAccountDetails(BulkAccountSolutionDbContext context, string bvn, string accountNo, string cif, string status)
        {
            var details = context.BulkAccount.FirstOrDefault(a => a.Bvn == bvn);

            if (details != null)
            {
                details.AccountNo = accountNo;
                details.Cif = cif;
                details.Status = status;
                details.DateOpened = DateTime.Now;
                await context.SaveChangesAsync();
            }
        }
        public async Task OpenAccountsNewSingleWithBVN(HttpContext httpContext, string uploadId)
        {
            var itemsWithStatusZero = 0;            

            using (var scope = _serviceProvider.CreateScope())
            {
                AccountOpeningRequest accountRequest = new AccountOpeningRequest();
                CreateAccountResponse accountResponse = new CreateAccountResponse();
                var context = scope.ServiceProvider.GetRequiredService<BulkAccountSolutionDbContext>();
                itemsWithStatusZero = context.BulkAccount.Where(item => item.Status == "0" && item.UploadId == uploadId).Count();

                do
                {
                    var accounts = context.BulkAccount.Where(item => item.Status == "0" && item.UploadId == uploadId).Take(400).ToList();
                    foreach (var account in accounts)
                    {
                        if (string.IsNullOrEmpty(account.Bvn)) continue;

                        var bvnResponse = GetBVNDetails(account.Bvn);

                        if (bvnResponse == null) continue;

                        //VALIDATION
                        if (!string.IsNullOrEmpty(bvnResponse.firstName))
                        {
                            if (account.FName.ToLower().Trim() == bvnResponse.firstName.ToLower().Trim())
                            {
                                if (account.OName.ToLower().Trim() == bvnResponse.middleName.ToLower().Trim())
                                {
                                    if (account.SName.ToLower().Trim() == bvnResponse.lastName.ToLower().Trim())
                                    {
                                        DateTime dFromTable = Convert.ToDateTime(account.Dob);
                                        DateTime dFromBVN = Convert.ToDateTime(bvnResponse.dateOfBirth);

                                        if (dFromTable == dFromBVN)
                                        {

                                            string _gender = (account.Sex == "M" || account.Sex == "Male" || account.Sex == "M" || account.Sex == "MALE") ? "Male" : "Female";
                                            if (_gender.ToLower().Trim() == bvnResponse.gender.ToLower().Trim())
                                            {

                                                context.BulkAccount.Where(c => c.Bvn == account.Bvn).ExecuteUpdate(setters => setters.SetProperty(b => b.Status, b => "2"));

                                            string mont = dFromBVN.Month.ToString();
                                            string dayy = dFromBVN.Day.ToString();
                                            if (mont.Length == 1)
                                            {
                                                mont = "0" + mont;
                                            }
                                            if (dayy.Length == 1)
                                            {
                                                dayy = "0" + dayy;
                                            }
                                            string dooob = dFromBVN.Year.ToString() + "-" + mont + "-" + dayy;

                                            if (string.IsNullOrEmpty(account.Phone))
                                            {
                                                account.Phone = bvnResponse.phoneNumber1;
                                            }

                                            account.Sex = bvnResponse.gender.ToUpper() == "MALE" ? "M" : "F";
                                            account.Title = bvnResponse.gender.ToUpper() == "MALE" ? 23 : 24;

                                            if (string.IsNullOrEmpty(account.IDType))
                                            {
                                                account.IDType = "1";
                                            }

                                            if (string.IsNullOrEmpty(account.IDNumber))
                                            {
                                                account.IDNumber = account.Bvn + DateTime.Now.Year.ToString() + DateTime.Now.Day.ToString() + DateTime.Now.Millisecond.ToString();
                                            }

                                            if (string.IsNullOrEmpty(account.MktByID))
                                            {
                                                account.MktByID = "99999007";
                                            }

                                            accountRequest = new AccountOpeningRequest
                                            {
                                                //    ac.idno = DateTime.Now.Year.ToString() + DateTime.Now.Day.ToString() + DateTime.Now.Millisecond.ToString();
                                                //ac.idtype = "7";
                                                accountName = $"{bvnResponse.firstName} {bvnResponse.middleName} {bvnResponse.lastName}",
                                                addref = account.Address,
                                                address = account.Address,
                                                branchcode = account.BranchCode.ToString(),
                                                cif = "",
                                                curencycode = "566",
                                                secondname = bvnResponse.middleName,
                                                firstname = bvnResponse.firstName,
                                                lastname = bvnResponse.lastName,
                                                glcode = account.GlCode,
                                                idtype = account.IDType,
                                                idno = account.IDNumber,
                                                idexpirydate = "9999-01-01",
                                                bvn = account.Bvn,
                                                marital = "S",
                                                sex = account.Sex,
                                                telephone = bvnResponse.phoneNumber1,
                                                dob = dooob,
                                                title = account.Title,
                                                ecosector = "8",
                                                division = 22,
                                                dept = 223,
                                                externalAccountNo = "",
                                                externalPartyCode = "",
                                                marketedbyid = account.MktByID,
                                                marketedforid = account.MktForID
                                            };

                                            accountResponse = await CreateAccountAsync(context, accountRequest);

                                            if (accountResponse != null)
                                            {
                                                itemsWithStatusZero--;
                                                await UpdateAccountDetails(context, account.Bvn, accountResponse.accountNo, accountResponse.cif, "1");
                                                context.BulkAccount.Where(c => c.Bvn == account.Bvn)
                                                    .ExecuteUpdate(setters => setters
                                                    .SetProperty(b => b.AccountName, b => accountRequest.accountName)
                                                    .SetProperty(b => b.FName, b => accountRequest.firstname)
                                                    .SetProperty(b => b.OName, b => accountRequest.lastname)
                                                    .SetProperty(b => b.FailureReason, b => "")
                                                    .SetProperty(b => b.Phone, b => accountRequest.telephone));                                                
                                            }
                                            else
                                            {
                                                itemsWithStatusZero--;
                                                util.WriteToLog("Unable to create account for " + account.Bvn);                                              
                                            }
                                            }
                                            else
                                            {
                                                itemsWithStatusZero--;
                                                await UpdateFailureReason(context, account.Bvn, $"Supplied gender [ " + account.Sex + " ] is different to gender on BVN.", "8");
                                            }
                                        }
                                        else
                                        {
                                            itemsWithStatusZero--;
                                            await UpdateFailureReason(context, account.Bvn, $"Supplied date of birth [ {account.Dob} ] is different to date of birth on BVN.", "7");
                                        }
                                    }
                                    else
                                    {
                                        itemsWithStatusZero--;
                                        await UpdateFailureReason(context, account.Bvn, $"Supplied surname [ " + account.SName + " ] is different from surname on BVN.", "5");
                                    }
                                }
                                else
                                {
                                    itemsWithStatusZero--;
                                    await UpdateFailureReason(context, account.Bvn, $"Supplied middlename [ " + account.OName + " ] is different from middlename on BVN.", "4");
                                }
                            }
                            else
                            {
                                itemsWithStatusZero--;
                                await UpdateFailureReason(context, account.Bvn, $"Supplied firstname [ " + account.FName + " ] is different with firstname on BVN.", "3"); 
                            }
                        }
                        else
                        {
                            itemsWithStatusZero--;
                            await UpdateFailureReason(context, account.Bvn, $"This BVN : " + account.Bvn + " is invalid.", "4");
                        }
                    }
                    if (itemsWithStatusZero == 0)
                    {
                        var userEmail = context.BulkAccountUpload.Where(c => c.UploadId == uploadId).FirstOrDefault();
                        util.SendNotificationEmail(userEmail.InitiatorEmail, "Initiator", "Your bulk account creation is complete.");
                        util.WriteToLog($"Completed account processing for upload:::: {uploadId}");
                        context.BulkAccountUpload.Where(c => c.UploadId == uploadId).ExecuteUpdate(setters => setters.SetProperty(b => b.Status, b => "Completed"));
                    }
                } while (itemsWithStatusZero > 0);
            }
        }
    }
}


