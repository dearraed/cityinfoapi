﻿namespace CityInfo.API.Services
{
    public class CloudMailService : IMailService
    {
        private string? _mailTo = String.Empty;
        private string? _mailFrom = String.Empty;

        public CloudMailService(IConfiguration configuration)
        {
            _mailTo = configuration["mailSettings:mailToAddress"];
            _mailFrom = configuration["mailSetings:mailFromAddress"];
        }

        public void Send(string subject, string message)
        {
            // send mail - output to debug window
            Console.WriteLine($"Mail from {_mailFrom} to {_mailTo}, with {nameof(CloudMailService)}.");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine($"Message: {message}");
        }
    }
}

