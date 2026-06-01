using OrderManagement.Application.Services;
using OrderManagement.Application.Exceptions;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace OrderManagement.Infrastructure.Services
{
    public class ClaimService : IClaimService
    {
        public int GetUserId(ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("Token invalid");

            return int.TryParse(claim.Value, out int id)
                ? id
                : throw new UnauthorizedAccessException("UserId invalide.");
        }

        public bool IsAdmin(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Role)?.Value == "Admin";
        }
    }
}
