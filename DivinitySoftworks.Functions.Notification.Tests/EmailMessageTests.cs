using OneOf;
using System.Net.Mail;
using System.Text.Json;
using Xunit;

namespace DivinitySoftworks.Core.Net.Mail.Tests {
    public class EmailMessageTests {

        public EmailMessageTests() {
        }

        private static OneOf<EmailMessage, EmailTemplateMessage> ConvertJsonToObject(string json) {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("Template", out _))
                return JsonSerializer.Deserialize<EmailTemplateMessage>(json)!;
            return JsonSerializer.Deserialize<EmailMessage>(json)!;
        }

        string _testjson = @"{
  ""Sender"": ""mortgage@mail.divinity-softworks.com"",
  ""To"": [],
  ""CC"": [],
  ""BCC"": [
    ""\""Michael Keeman\"" <m.keeman@outlook.com>""
  ],
  ""Subject"": ""Amazon SES test (AWS SDK for .NET)"",
  ""Template"": ""test"",
  ""Parameters"": {},
  ""Priority"": 0,
  ""Attachments"": [],
  ""SentDate"": ""2025-01-01T10:24:18.4907358Z"",
  ""ReplyTo"": [],
  ""Headers"": null
}";

        [Fact]
        public void Should_Serialize_And_Deserialize_EmailMessage() {

            try {
                var test = ConvertJsonToObject(_testjson);
            }
            catch (Exception exception) {

                throw exception;
            }

            // Arrange: Create an EmailMessage object
            var emailMessage = new EmailMessage(
                new MailAddress("sender@example.com"),
                "Test Subject"
            ) {
                To = new List<MailAddress> { new MailAddress("recipient@example.com", "Hallo World") },
                CC = new List<MailAddress> { new MailAddress("cc@example.com") },
                BCC = new List<MailAddress> { new MailAddress("bcc@example.com") },
                HtmlBody = "<h1>Hello</h1>",
                TextBody = "Hello",
                Priority = MailPriority.High,
                SentDate = DateTime.UtcNow,
                ReplyTo = new List<MailAddress> { new MailAddress("replyto@example.com") },
                Headers = new Dictionary<string, string> { { "X-Custom-Header", "Value" } }
            };

            // Act: Serialize the EmailMessage to JSON
            var json = JsonSerializer.Serialize(emailMessage);

            // Assert: Ensure the serialized JSON is not empty
            Assert.False(string.IsNullOrEmpty(json));

            // Act: Deserialize the JSON back to an EmailMessage object
            var deserializedEmailMessage = JsonSerializer.Deserialize<EmailMessage>(json);

            // Assert: Ensure the deserialized object has the same properties as the original
            Assert.NotNull(deserializedEmailMessage);
            Assert.Equal(emailMessage.Sender.ToString(), deserializedEmailMessage?.Sender.ToString());
            Assert.Equal(emailMessage.Subject, deserializedEmailMessage?.Subject);
            Assert.Equal(emailMessage.To.Count, deserializedEmailMessage?.To.Count);
            Assert.Equal(emailMessage.CC.Count, deserializedEmailMessage?.CC.Count);
            Assert.Equal(emailMessage.BCC.Count, deserializedEmailMessage?.BCC.Count);
            Assert.Equal(emailMessage.HtmlBody, deserializedEmailMessage?.HtmlBody);
            Assert.Equal(emailMessage.TextBody, deserializedEmailMessage?.TextBody);
            Assert.Equal(emailMessage.Priority, deserializedEmailMessage?.Priority);
            Assert.Equal(emailMessage.SentDate, deserializedEmailMessage?.SentDate);
            Assert.Equal(emailMessage.ReplyTo.Count, deserializedEmailMessage?.ReplyTo.Count);
            Assert.Equal(emailMessage.Headers["X-Custom-Header"], deserializedEmailMessage?.Headers?["X-Custom-Header"]);
        }

        [Fact]
        public void Should_Deserialize_EmailMessage_With_Empty_Lists() {
            // Arrange: Create an EmailMessage with empty lists
            var json = @"{
                ""Sender"": ""sender@example.com"",
                ""Subject"": ""Empty Lists Test"",
                ""To"": [],
                ""CC"": [],
                ""BCC"": [],
                ""HtmlBody"": ""<h1>Hello</h1>"",
                ""TextBody"": ""Hello"",
                ""Priority"": 3,
                ""SentDate"": ""2024-12-31T00:00:00Z"",
                ""ReplyTo"": [],
                ""Headers"": {}
            }";

            // Act: Deserialize the JSON to EmailMessage
            var emailMessage = JsonSerializer.Deserialize<EmailMessage>(json);

            // Assert: Ensure lists are empty
            Assert.NotNull(emailMessage);
            Assert.Empty(emailMessage?.To);
            Assert.Empty(emailMessage?.CC);
            Assert.Empty(emailMessage?.BCC);
            Assert.Empty(emailMessage?.ReplyTo);
        }

        [Fact]
        public void Should_Deserialize_EmailMessage_With_Null_Properties() {
            // Arrange: Create JSON with null properties
            var json = @"{
                ""Sender"": null,
                ""Subject"": null,
                ""To"": null,
                ""CC"": null,
                ""BCC"": null,
                ""HtmlBody"": null,
                ""TextBody"": null,
                ""Priority"": 0,
                ""SentDate"": ""2024-12-31T00:00:00Z"",
                ""ReplyTo"": null,
                ""Headers"": null
            }";

            // Act: Deserialize the JSON to EmailMessage
            var emailMessage = JsonSerializer.Deserialize<EmailMessage>(json);

            // Assert: Ensure null properties are correctly handled
            Assert.NotNull(emailMessage);
            Assert.Null(emailMessage?.Sender);
            Assert.Null(emailMessage?.Subject);
            Assert.Null(emailMessage?.To);
            Assert.Null(emailMessage?.CC);
            Assert.Null(emailMessage?.BCC);
            Assert.Null(emailMessage?.HtmlBody);
            Assert.Null(emailMessage?.TextBody);
            Assert.Equal(MailPriority.Normal, emailMessage?.Priority); // Default value
            Assert.Null(emailMessage?.ReplyTo);
            Assert.Null(emailMessage?.Headers);
        }

        [Fact]
        public void Should_Serialize_EmailMessage_With_Null_Lists_To_Json() {
            // Arrange: Create EmailMessage with null lists
            var emailMessage = new EmailMessage(new MailAddress("sender@example.com"), "Test Subject") {
                To = null,
                CC = null,
                BCC = null,
                ReplyTo = null
            };

            // Act: Serialize the EmailMessage to JSON
            var json = JsonSerializer.Serialize(emailMessage);

            // Assert: Ensure lists are serialized as null
            Assert.Contains("\"To\":null", json);
            Assert.Contains("\"CC\":null", json);
            Assert.Contains("\"BCC\":null", json);
            Assert.Contains("\"ReplyTo\":null", json);
        }
    }
}
