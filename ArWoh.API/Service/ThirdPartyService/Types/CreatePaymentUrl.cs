namespace ArWoh.API.Service.ThirdPartyService.Types;

public class CreatePaymentResponse
{
    public string PaymentUrl { get; set; }
}

public class CreatePaymentRequest
{
    public int PaymentId { get; set; }
    public string? ReturnUrl { get; set; }
    public string? PaymentMethod { get; set; }
}