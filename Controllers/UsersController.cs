using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenoApi.Data;
using RenoApi.Domain;

namespace RenoApi.Controllers;

/// Quản lý tài khoản nhân viên — chỉ Cửa hàng trưởng (trừ đổi mật khẩu cá nhân)
[ApiController]
[Route("api/users")]
[Authorize(Roles = "Manager")]
public class UsersController(AppDb db) : ControllerBase
{
    [HttpGet]
    public async Task<List<UserDto>> All() =>
        await db.Users.OrderBy(u => u.Role).ThenBy(u => u.FullName)
            .Select(u => new UserDto(u.Id, u.Username, u.FullName, u.Role, u.IsActive))
            .ToListAsync();

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserReq req)
    {
        var username = req.Username.Trim().ToLower();
        if (await db.Users.AnyAsync(u => u.Username == username))
            return BadRequest(new { message = "Tên đăng nhập đã tồn tại" });
        if (req.Password.Trim().Length < 6)
            return BadRequest(new { message = "Mật khẩu tối thiểu 6 ký tự" });

        var u = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            FullName = req.FullName.Trim(),
            Role = req.Role,
        };
        db.Users.Add(u);
        await db.SaveChangesAsync();
        return new UserDto(u.Id, u.Username, u.FullName, u.Role, u.IsActive);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(int id, UpdateUserReq req)
    {
        var u = await db.Users.FindAsync(id);
        if (u == null) return NotFound();
        if (req.FullName != null) u.FullName = req.FullName.Trim();
        if (req.Role != null) u.Role = req.Role.Value;
        if (req.IsActive != null) u.IsActive = req.IsActive.Value;
        if (!string.IsNullOrWhiteSpace(req.Password))
        {
            if (req.Password.Trim().Length < 6)
                return BadRequest(new { message = "Mật khẩu tối thiểu 6 ký tự" });
            u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);
        }
        await db.SaveChangesAsync();
        return new UserDto(u.Id, u.Username, u.FullName, u.Role, u.IsActive);
    }

    /// Nhân viên (mọi vai trò) tự đổi mật khẩu của chính mình
    [HttpPut("me/password")]
    [Authorize]   // ghi đè [Authorize(Roles=Manager)] ở cấp class
    public async Task<IActionResult> ChangeMyPassword(ChangePasswordReq req)
    {
        var uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var u = await db.Users.FindAsync(uid);
        if (u == null) return NotFound();
        if (!BCrypt.Net.BCrypt.Verify(req.OldPassword, u.PasswordHash))
            return BadRequest(new { message = "Mật khẩu hiện tại không đúng" });
        if (req.NewPassword.Trim().Length < 6)
            return BadRequest(new { message = "Mật khẩu mới tối thiểu 6 ký tự" });
        u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword.Trim());
        await db.SaveChangesAsync();
        return NoContent();
    }
}