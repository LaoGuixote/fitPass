using System.Net.Mail;
using System.Net;

namespace fitPass
{
    public class email
    {
        private readonly SmtpClient client;

        public email()
        {
            client = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential("fitpassinformation@gmail.com", "lsip oihe thbc vrzq"),
                EnableSsl = true
            };
        }

        public void SendMail(string to, string subject, string body)
        {
                var mail = new MailMessage("fitpassinformatiom@gmail.com", to, subject, body);
                mail.IsBodyHtml = true; // 若要寄送 HTML 格式郵件可設定為 true
                client.Send(mail);
        }
        public void Send(MailMessage mail)
        {
            client.Send(mail);
        }

    }
}
