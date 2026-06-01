// This example demonstrates property dependencies being captured
using System.Collections.Generic;

namespace PropertyDependencyExample
{
    // Service classes
    public class UserService
    {
        // Property dependency on UserRepository
        public UserRepository UserRepository { get; set; }
        
        // Field dependency on EmailService
        private EmailService _emailService;
        
        // Constructor parameter dependency on NotificationService
        public UserService(NotificationService notificationService)
        {
        }
    }

    public class UserRepository
    {
        // Property dependency on DatabaseConnection
        public DatabaseConnection Connection { get; set; }
    }

    public class EmailService
    {
        // Property dependency on SmtpClient
        public SmtpClient MailClient { get; set; }
    }

    public class NotificationService
    {
        // Property dependency on EmailService
        public EmailService EmailService { get; set; }
    }

    public class DatabaseConnection
    {
    }

    public class SmtpClient
    {
    }

    // Usage in another class
    public class Application
    {
        // This property creates a dependency on UserService
        public UserService Service { get; set; }
    }
}
