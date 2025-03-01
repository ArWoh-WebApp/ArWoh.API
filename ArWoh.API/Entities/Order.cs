namespace ArWoh.API.Entities;

public class Order : BaseEntity
{
    public int TransactionId { get; set; }
    public bool IsPhysicalPrint { get; set; } // Nếu true, là đơn hàng in
    public string ShippingAddress { get; set; } // Địa chỉ giao hàng
    public string ShippingStatus { get; set; } // "Processing", "Shipped", "Delivered"

    public PaymentTransaction Transaction { get; set; }
}