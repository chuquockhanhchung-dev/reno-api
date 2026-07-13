using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenoApi.Data;
using RenoApi.Domain;

namespace RenoApi.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public class TasksController(AppDb db) : ControllerBase
{
    /// Nhân viên: chỉ việc của vị trí mình. CHT: tất cả.
    [HttpGet]
    public async Task<List<TaskTemplateDto>> All()
    {
        var q = db.TaskTemplates.Where(t => t.IsActive);
        if (!User.IsInRole("Manager"))
        {
            var role = Enum.Parse<StaffRole>(User.FindFirstValue(ClaimTypes.Role)!);
            q = q.Where(t => t.Role == role);
        }
        return await q.OrderBy(t => t.SortOrder)
            .Select(t => new TaskTemplateDto(t.Id, t.Role, t.Phase, t.Title,
                t.Standard, t.Minutes, t.Priority, t.RequiresPhoto, t.SortOrder))
            .ToListAsync();
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<TaskTemplateDto>> Create(UpsertTaskReq req)
    {
        var t = new TaskTemplate
        {
            Role = req.Role, Phase = req.Phase, Title = req.Title.Trim(),
            Standard = req.Standard.Trim(), Minutes = req.Minutes,
            Priority = req.Priority, RequiresPhoto = req.RequiresPhoto,
            SortOrder = req.SortOrder,
        };
        db.TaskTemplates.Add(t);
        await db.SaveChangesAsync();
        return new TaskTemplateDto(t.Id, t.Role, t.Phase, t.Title, t.Standard,
            t.Minutes, t.Priority, t.RequiresPhoto, t.SortOrder);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Update(int id, UpsertTaskReq req)
    {
        var t = await db.TaskTemplates.FindAsync(id);
        if (t == null) return NotFound();
        (t.Role, t.Phase, t.Title, t.Standard, t.Minutes, t.Priority, t.RequiresPhoto, t.SortOrder)
            = (req.Role, req.Phase, req.Title.Trim(), req.Standard.Trim(),
               req.Minutes, req.Priority, req.RequiresPhoto, req.SortOrder);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// Xóa mềm — checklist cũ trong lịch sử vẫn giữ nguyên
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var t = await db.TaskTemplates.FindAsync(id);
        if (t == null) return NotFound();
        t.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
