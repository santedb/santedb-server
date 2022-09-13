using NUnit.Framework;
using SanteDB.Core;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Mail;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.Test.Persistence.Mail
{
    /// <summary>
    /// Persistence of mailboxes and sending of mail test
    /// </summary>
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class LocalMailManagementPersistenceTest : DataPersistenceTest
    {

        /// <summary>
        /// Can send a mail message
        /// </summary>
        [Test]
        public void TestCanSendMailMessage()
        {
            // Create the TO user
            var securityService = ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>();
            var identityService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            var mailService = ApplicationServiceContext.Current.GetService<IMailMessageService>();
            Assert.IsNotNull(securityService);
            Assert.IsNotNull(mailService);

            // Construct a mail message and send as system
            using (AuthenticationContext.EnterSystemContext())
            {


                var toUser = securityService.CreateUser(new Core.Model.Security.SecurityUser()
                {
                    UserName = "TEST_MAIL_TO1",
                    Email = "test@test.com"
                }, "@Foo123!!");
                Assert.IsNotNull(toUser);


                var mailMessage = new MailMessage("SYSTEM", "TEST_MAIL_TO1", "This is a test", "This is a test message / alert sent within SanteDB", MailMessageFlags.HighPriority);
                var afterSent = mailService.Send(mailMessage);
                Assert.IsTrue(mailMessage.RcptToXml.Contains(toUser.Key.Value), "Message does not contain RCPT TO");
                Assert.AreEqual(mailMessage.Subject, afterSent.Subject);
                Assert.AreEqual(mailMessage.Body, afterSent.Body);

                mailMessage = new MailMessage("SYSTEM", "TEST_MAIL_TO1", "This is another test", "This is another test message / alert sent within SanteDB", MailMessageFlags.LowPriority);
                afterSent = mailService.Send(mailMessage);

                // Should have created an inbox
                var inbox = mailService.GetMailboxes(toUser.Key).FirstOrDefault(o => o.Name == Mailbox.INBOX_NAME);
                Assert.IsNotNull(inbox);
                Assert.AreEqual(toUser.Key, inbox.OwnerKey);
                Assert.AreEqual("TEST_MAIL_TO1", inbox.LoadProperty(o => o.Owner)?.UserName);

            }

            // Now authenticate as our user and check the mail
            using (AuthenticationContext.EnterContext(identityService.Authenticate("TEST_MAIL_TO1", "@Foo123!!")))
            {
                var inbox = mailService.GetMailboxes().Where(o => o.Name == Mailbox.INBOX_NAME).FirstOrDefault();
                Assert.IsNotNull(inbox);
                Assert.AreEqual(2, inbox.LoadProperty(o => o.Messages).Count);
                var messages = mailService.GetMessages(inbox.Key.Value);
                Assert.AreEqual(2, messages.Count());

                // Now we want to test the sorting and search of the mailbox
                Assert.AreEqual(1, messages.Where(s => s.Subject == "This is a test").Count());
                Assert.AreEqual(0, messages.Where(s => s.Body == "This is a test").Count());
                Assert.AreEqual("This is another test", messages.Where(s => s.Flags == MailMessageFlags.LowPriority).First().Subject);

                Assert.AreEqual(MailMessageFlags.HighPriority, messages.OrderByDescending(o => o.Flags).First().Flags);

            }

        }

        /// <summary>
        /// Tests that the appropriate permissions are applied when creating a mailbox 
        /// </summary>
        [Test]
        public void TestCanCreateMailbox()
        {
            // Create the TO user
            var securityService = ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>();
            var identityService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            var roleService = ApplicationServiceContext.Current.GetService<IRoleProviderService>();
            var mailService = ApplicationServiceContext.Current.GetService<IMailMessageService>();
            Assert.IsNotNull(securityService);
            Assert.IsNotNull(mailService);

            // Construct a mail message and send as system
            using (AuthenticationContext.EnterSystemContext())
            {

                // Create a user
                var toUser = securityService.CreateUser(new Core.Model.Security.SecurityUser()
                {
                    UserName = "TEST_MAIL_TO2",
                    Email = "test2@test.com"
                }, "@Foo123!!");
                roleService.AddUsersToRoles(new string[] { "TEST_MAIL_TO2" }, new string[] { "USERS", "CLINICAL_STAFF" }, AuthenticationContext.SystemPrincipal);
                Assert.IsNotNull(toUser);

                // SYSTEM can create a mailbox for USER
                var inbox = mailService.CreateMailbox(Mailbox.INBOX_NAME, toUser.Key);
                // SYSTEM can read mailbox for user
                mailService.GetMailboxes(toUser.Key);
                // SYSTEM can read mail messages for user
                Assert.AreEqual(0, mailService.GetMessages(inbox.Key.Value).Count());

            }

            // As user
            using (AuthenticationContext.EnterContext(identityService.Authenticate("TEST_MAIL_TO2", "@Foo123!!")))
            {
                // User cannot create mailbox for SYSTEM
                Assert.Throws<PolicyViolationException>(() => mailService.CreateMailbox(Mailbox.INBOX_NAME, Guid.Parse(AuthenticationContext.SystemUserSid)));
                // User cannot read mailboxes for SYSTEM
                Assert.Throws<PolicyViolationException>(() => mailService.GetMailboxes(Guid.Parse(AuthenticationContext.SystemUserSid)));

                // User can create their own mailbox
                var mailbox = mailService.CreateMailbox("FOO!");
                Assert.AreEqual(2, mailService.GetMailboxes().Count());

                // User can send mail to SYSTEM
                var mail = mailService.Send(new MailMessage(String.Empty, "SYSTEM;TEST_MAIL_TO2", "Test from FOO", "This is a test!"));
                Assert.AreEqual("TEST_MAIL_TO2", mail.From);
                Assert.AreEqual(3, mailService.GetMailboxes().Count()); // Will not have a SENT folder


            }

            // As SYSTEM
            using (AuthenticationContext.EnterSystemContext())
            {
                var mailboxes = mailService.GetMailboxes().Where(o=>o.Name== Mailbox.INBOX_NAME).FirstOrDefault();
                var messages = mailService.GetMessages(mailboxes.Key.Value);
                Assert.GreaterOrEqual(messages.Count(), 1);
                Assert.AreEqual("Test from FOO", messages.First().Subject);
                Assert.AreEqual("TEST_MAIL_TO2", messages.First().From);
                Assert.AreEqual("SYSTEM;TEST_MAIL_TO2", messages.First().To);

            }

            // As user test moving
            using (AuthenticationContext.EnterContext(identityService.Authenticate("TEST_MAIL_TO2", "@Foo123!!")))
            {
                // User can create their own mailbox
                var mailboxes = mailService.GetMailboxes();
                Mailbox fooMailbox = mailboxes.FirstOrDefault(o => o.Name == "FOO!"), inbox = mailboxes.FirstOrDefault(o => o.Name == Mailbox.INBOX_NAME);
                var message = mailService.GetMessages(inbox.Key.Value).First();
                Assert.AreEqual(1, mailService.GetMessages(inbox.Key.Value).Count());

                mailService.MoveMessage(inbox.Key.Value, message.Key.Value, fooMailbox.Key.Value);
                Assert.AreEqual(1, mailService.GetMessages(fooMailbox.Key.Value).Count());
                Assert.AreEqual(0, mailService.GetMessages(inbox.Key.Value).Count());

                // Copy and test delete
                mailService.MoveMessage(fooMailbox.Key.Value, message.Key.Value, inbox.Key.Value, true);
                Assert.AreEqual(1, mailService.GetMessages(fooMailbox.Key.Value).Count());
                Assert.AreEqual(1, mailService.GetMessages(inbox.Key.Value).Count());

                // Delete from FOO
                mailService.DeleteMessage(fooMailbox.Key.Value, message.Key.Value);
                Assert.AreEqual(0, mailService.GetMessages(fooMailbox.Key.Value).Count());
                Assert.AreEqual(1, mailService.GetMessages(inbox.Key.Value).Count());

                // Delete the FOO mailbox
                mailService.DeleteMailbox(fooMailbox.Key.Value);
                Assert.AreEqual(2, mailService.GetMailboxes().Count());

            }
        }



    }
}
