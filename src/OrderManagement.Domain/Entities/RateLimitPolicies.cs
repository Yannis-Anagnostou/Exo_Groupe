using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.Entities
{
    public class RateLimitPolicies
    {
        public const string Auth = "auth";
        public const string Add = "Add";
        public const string Read = "read";
    }
}
