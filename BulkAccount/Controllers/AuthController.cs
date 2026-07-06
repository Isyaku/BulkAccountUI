using BulkAccount.Data;
using BulkAccount.Helpers;
using BulkAccount.Models;
using BulkAccount.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BulkAccount.Controllers
{
    public class AuthController : Controller
    {
        Utility util = new Utility();
        private readonly BulkAccountSolutionDbContext _context;
        private readonly ILogger<BulkAccountUploadController> _logger;
        public AuthController(BulkAccountSolutionDbContext context, ILogger<BulkAccountUploadController> logger)
        {
            _context = context;
            _logger = logger;
        }
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {

            try
            {
                var userName = util.DecryptTextWithPrivateKey(model.UserName);
                var userPassword = util.DecryptTextWithPrivateKey(model.Password);

                //TEST LOGIN
                if (userName.ToLower() == "im04220")
                {
                    //APPROVER
                    HttpContext.Session.SetString("user", userName);
                    HttpContext.Session.SetString("bulkAcctApprover", "BulkAcctApprover");
                    HttpContext.Session.SetString("ApproverEmail", "");
                    HttpContext.Session.SetString("branchCode", "");
                    HttpContext.Session.SetString("branchName", "");

                    return RedirectToAction("Index", "Home");
                }

                if (userName.ToLower() == "bm04220" || userName.ToLower() == "mm04220")
                {
                    //INITIATOR
                    HttpContext.Session.SetString("user", userName);
                    HttpContext.Session.SetString("fullName", "Isyaku Mustapha");
                    HttpContext.Session.SetString("InitiatorEmail", "OR04220@jaizbankplc.com");
                    HttpContext.Session.SetString("bulkAcctCreator", "BulkAcctCreator");
                    HttpContext.Session.SetString("branchCode", "");
                    HttpContext.Session.SetString("branchName", "");

                    return RedirectToAction("Index", "Home");
                }

                var isValidationSuccessful = ValidateUser(userName, userPassword);
                var isBulkAcctCreator = HttpContext.Session.GetString("bulkAcctCreator");
                var isBulkAcctApprover = HttpContext.Session.GetString("bulkAcctApprover");


                if (ModelState.IsValid)
                {
                    if (!isValidationSuccessful)
                    {
                        ModelState.AddModelError("InvalidUsernameOrPassword", "The user name or password provided is incorrect.");
                    }
                    else if (isBulkAcctCreator == null && isBulkAcctApprover == null)
                    {
                        ModelState.AddModelError("Unauthorized", "You don't have access to Bulk Accounts Solutions");
                    }
                    else if (isValidationSuccessful && (isBulkAcctCreator != null || isBulkAcctApprover != null))
                    {
                        if (isBulkAcctApprover != null)
                        {
                            try
                            {
                                var existingApprover = _context.AccountApprover.FirstOrDefault(a => a.StaffID == userName.Trim().ToUpper());
                                if (existingApprover == null)
                                {
                                    var approver = new AccountApprover
                                    {
                                        StaffID = model.UserName.Trim().ToUpper(),
                                        StaffName = HttpContext.Session.GetString("fullName"),
                                        StaffEmail = HttpContext.Session.GetString("ApproverEmail")
                                    };
                                    _context.AccountApprover.Add(approver);
                                    _context.SaveChangesAsync();
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError("Error getting approvers", ex.Message);
                                util.WriteToLog($"Error getting approvers {model.UserName}::::{ex.Message}");
                            }
                        }
                        HttpContext.Session.SetString("user", userName);
                        return RedirectToAction("Index", "Home");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error logging in user", ex.Message);
                util.WriteToLog($"Error logging in user {model.UserName}::::{ex.Message}");
            }

            return View();
        }

        public IActionResult LogOut()
        {
            HttpContext.Session.Clear();
            HttpContext.Session.Remove("user");
            return RedirectToAction("Login", "Auth");
        }
        private bool ValidateUser(string username, string password)
        {
            var userValidation = new JaizAuthService.JaizRoleManagerServiceClient(0);
            var logModel = new JaizAuthService.LogonModel()
            {
                username = username,
                password = password,
                appID = 66,
                //ipAddress = SystemIpInfo.GetUserIp(System.Web.HttpContext.Current.Request),
                //appIDSpecified = true
            };

            var result = new JaizAuthService.LoginResult();

            try
            {
                result = userValidation.ValidateADUser2FA(logModel);

                if (result.loggedIn)
                {
                    if (result.roles[0] == "BulkAcctCreator")
                    {
                        var userDetails = userValidation.GetUser(username);
                        string FullName = userDetails.fullname;
                        string UserEmail = userDetails.email;

                        HttpContext.Session.SetString("fullName", FullName);
                        HttpContext.Session.SetString("InitiatorEmail", UserEmail);
                        HttpContext.Session.SetString("bulkAcctCreator", "BulkAcctCreator");
                        HttpContext.Session.SetString("branchCode", result.branches[0].branch_code.ToString());
                        HttpContext.Session.SetString("branchName", result.branches[0].branch_name.ToString());

                        return true;
                    }
                    else if (result.roles[0] == "BulkAcctApprover")
                    {
                        var userDetails = userValidation.GetUser(username);
                        string FullName = userDetails.fullname;
                        string UserEmail = userDetails.email;

                        HttpContext.Session.SetString("bulkAcctApprover", "BulkAcctApprover");
                        HttpContext.Session.SetString("ApproverEmail", UserEmail);
                        HttpContext.Session.SetString("branchCode", result.branches[0].branch_code.ToString());
                        HttpContext.Session.SetString("branchName", result.branches[0].branch_name.ToString());

                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Validating User", ex.Message);
                util.WriteToLog($"Error Validating {username}::::{ex.Message}");
            }
            return result.loggedIn;
        }
    }
}