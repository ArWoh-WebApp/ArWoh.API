﻿using ArWoh.API.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArWoh.API.Entities
{
    public class Payment : BaseEntity
    {
        [Required]
        public int PaymentTransactionId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public PaymentGatewayEnum PaymentGateway { get; set; } // "PAYOS" hoặc "VNPAY"

        [Required]
        public PaymentStatusEnum PaymentStatus { get; set; } = PaymentStatusEnum.PENDING;

        public string? GatewayTransactionId { get; set; } // ID từ PAYOS/VNPAY nếu có

        [ForeignKey("PaymentTransactionId")]
        public PaymentTransaction PaymentTransaction { get; set; }
    }
}
