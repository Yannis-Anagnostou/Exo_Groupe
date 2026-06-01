using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace OrderManagement.Application.Services
{
    public interface IClaimService
    {
        int GetUserId(ClaimsPrincipal user);
        bool IsAdmin(ClaimsPrincipal user);
    }
}
