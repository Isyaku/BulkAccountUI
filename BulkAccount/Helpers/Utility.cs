using System.Security.Cryptography;
using System.Text;

namespace BulkAccount.Helpers
{
    public class Utility
    {
        public void WriteToLog(string content)
        {
            QueryHelper getConfiguration = new QueryHelper();
            var errorpath = getConfiguration.ConfigHelper("appConfiguration:errorPath");
            if (!Directory.Exists(errorpath))
            {
                Directory.CreateDirectory(errorpath);
            }

            string today = DateTime.Now.ToString("dd-MM-yyyy");

            string path = errorpath + "\\Errlog" + today + ".txt";
            using (StreamWriter sw = File.AppendText(path))
            {
                sw.WriteLine(DateTime.Now + "--------------------" + content);
            }
        }
        public string SendNotificationEmail(string emailaddress, string name, string message)
        {
            string templatePath = $"{Directory.GetCurrentDirectory()}\\wwwroot\\template\\NotificationMail.htm";
            string strMessage = File.ReadAllText(templatePath);
            string mailBody = string.Empty;
            mailBody = strMessage;
            mailBody = mailBody.Replace("#FullName#", name);
            mailBody = mailBody.Replace("#Message#", message);
            try
            {
                JaizServiceReference.JaizHelperClient service = new JaizServiceReference.JaizHelperClient();
                JaizServiceReference.EmailObject obj = new JaizServiceReference.EmailObject
                {
                    Attachment = null,
                    EmailAddress = emailaddress,
                    EmailContent = mailBody,
                    FromAddress = "platform@jaizbankplc.com",
                    HasAttachment = 0,
                    SenderId = "SRVMGT",
                    Subject = "Bulk Account Solution Notification"
                };

                return service.SendEmailViaHelper(obj).ToString();
            }
            catch (Exception ex)
            {                
                return null;
            }
        }
        public string DecryptTextWithPrivateKey(string encryptedText)
        {
            try
            {
                var xml = "<RSAKeyValue><Modulus>zX0HSeWFFywVOdnoOF3MC+uL8YOvLoA+vLEPBmZ2U7AWuZKer0gzk49mk2v87tBchEzDgfr7z5icgAStf9MVPhR3FckZ/WGRY0ifTH4bkPFtpMmonH755rcMSsszgrguVDyMjeizoEhFvkyUFR30LyXOSqynLYkZBfi2Z8/O01M=</Modulus><Exponent>AQAB</Exponent><P>6i86nisKd3z/A+D+2mWAa1AdXaDVL7qJ78DhHOOEFGbGCKnK16rKbb+R+L8WcsmKVgqhhczEWGbCFymLUynTDQ==</P><Q>4KF2Yjxv/hXFhe6r289gyMFHkWFm9gpSIb8Vah9aruwuB6EOtAL7BFhfdnaTQuo2XEs6v4+OijFV4oADwd0n3w==</Q><DP>WUEa5EGfQZ9ASqgsOezJnxzvtEmiNwivndMzeSE1q9jnzVF5X+1WLbH/3oBl++XYdaajnS1IADFZ9B3/Xfjo2Q==</DP><DQ>qhI1Qm1F0ZcERMIOhk79lSGZIP4g6TmpM3msKfvxOa0BsK8FJc9347NRG6ztE+WmILyojy6Omhx+TQ3lSls5+w==</DQ><InverseQ>B0tAXbxa10gEGokheNErVaBJm/lkZIc9M4B6sFgoSVjXCwtSyIBkRZXH8py4YAQmu6eMmGoAtaaWf2oKgAeOGQ==</InverseQ><D>pC3DBw20uoDkLKan3XFDuDpoQ3zdGKAqgARPZuOyosbMQVSeKJnda4ZlhGABZKVhZesXQeDQFFtwnvAd10VFcDfAFEIuLmBzvU3jAvV+oOcAr5v41n4v3OPMLP+WhUj7hemWTY8NQH1jdo1gBRz0bcsga8Vnjy79UCo5j2gciBE=</D></RSAKeyValue>";

                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(xml);

                    byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.Pkcs1);

                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred during decryption: " + ex.Message);
                return null;
            }
        }
    }
}
