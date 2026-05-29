using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace OrderManagement.Application.DTOs.Products
{
    public class CreateProductDTOs
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        public int Stock { get; set; } = 0;

        [Range(1, int.MaxValue)]
        public int CategoryId { get; set; }
    }
}
