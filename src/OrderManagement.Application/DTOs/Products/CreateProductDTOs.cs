using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.DTOs.Products
{
    public class CreateProductDTOs
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int Stock { get; set; } = 0;
    }
}
