using System.Security.Claims;
using OrderManagement.Application.DTOs.Auth;

namespace OrderManagement.Application.Services;

public interface IAuthService
{
    Task<string> RegisterAsync(RegisterDto registerDto);
    Task<string> LoginAsync(LoginDto loginDto);
    Task<MeDto> GetCurrentUserAsync(ClaimsPrincipal principal);
}
