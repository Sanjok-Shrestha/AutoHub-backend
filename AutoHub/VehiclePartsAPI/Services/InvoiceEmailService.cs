using MailKit.Net.Smtp;
using MimeKit;
using VehicleManagementSystem.VehiclePartsAPI.DTOs;

namespace VehicleManagementSystem.VehiclePartsAPI.Services
{
    public class InvoiceEmailService
    {
        private readonly IConfiguration _config;

        public InvoiceEmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendInvoiceEmailAsync(InvoiceResponseDto invoice)
        {
            var email = new MimeMessage();

            email.From.Add(new MailboxAddress(
                _config["EmailSettings:SenderName"],
                _config["EmailSettings:SenderEmail"]
            ));

            email.To.Add(new MailboxAddress(
                invoice.CustomerName,
                invoice.CustomerEmail
            ));

            email.Subject = $"Invoice #{invoice.Id} - AutoHub";

            // Build items table rows
            var rows = "";
            foreach (var item in invoice.Items)
            {
                rows += $@"
                    <tr>
                        <td style='padding:8px'>{item.PartName}</td>
                        <td style='padding:8px'>{item.Quantity}</td>
                        <td style='padding:8px'>Rs.{item.UnitPrice}</td>
                        <td style='padding:8px'>Rs.{item.Subtotal}</td>
                    </tr>";
            }

            email.Body = new TextPart("html")
            {
                Text = $@"
                    <h2>AutoHub - Invoice #{invoice.Id}</h2>
                    <p>Dear {invoice.CustomerName},</p>
                    <p>Thank you for your purchase!</p>

                    <table border='1' cellpadding='5' style='border-collapse:collapse'>
                        <tr style='background:#f0f0f0'>
                            <th style='padding:8px'>Part</th>
                            <th style='padding:8px'>Qty</th>
                            <th style='padding:8px'>Unit Price</th>
                            <th style='padding:8px'>Subtotal</th>
                        </tr>
                        {rows}
                    </table>

                    <p>Total Amount: Rs.{invoice.TotalAmount}</p>
                    <p>Final Amount: Rs.{invoice.FinalAmount}</p>
                    {(invoice.IsDiscountApplied ? "<p style='color:green'>10% Loyalty Discount Applied!</p>" : "")}

                    <p>Thank you for choosing AutoHub!</p>
                "
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config["EmailSettings:SmtpHost"],
                int.Parse(_config["EmailSettings:SmtpPort"]!),
                false
            );
            await smtp.AuthenticateAsync(
                _config["EmailSettings:SenderEmail"],
                _config["EmailSettings:Password"]
            );
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}