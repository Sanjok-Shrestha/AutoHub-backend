namespace AutoHub.API.Models;

public class Credit
{
    public int Id { get; set; }

    
    public int? CustomerId { get; set; }  
    public Customer? Customer { get; set; }  


    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public bool IsPaid { get; set; } = false;

    public int? SaleId { get; set; }
    public Sale? Sale { get; set; }  

 
    public DateTime? LastReminderSent { get; set; }
}