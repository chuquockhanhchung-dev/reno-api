using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenoApi.Auth;
using RenoApi.Data;

namespace RenoApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDb db, TokenService tokens) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginRes>> Login(LoginReq req)
    {
        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Username == req.Username.Trim().ToLower() && u.IsActive);
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Sai tên đăng nhập hoặc mật khẩu" });

        return new LoginRes(tokens.Create(user),
            new UserDto(user.Id, user.Username, user.FullName, user.Role, user.IsActive));
    }
}
