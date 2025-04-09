using System.ComponentModel.DataAnnotations;

namespace ArWoh.API.DTOs.PaymentDTOs;

public class CancelPaymentDto
{
    [Required]
    public string Reason { get; set; }
}