using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenoApi.Data;
using RenoApi.Domain;

namespace RenoApi.Controllers;

[ApiController]
[Route("api/sessions")]
[Authorize]
public class SessionsController(AppDb db, IWebHostEnvironment env) : ControllerBase
{
    int Uid => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    bool IsManager => User.IsInRole("Manager");

    static SessionDto ToDto(WorkSession s) => new(
        s.Id, s.Date.ToString("yyyy-MM-dd"), s.Shift, s.UserId, s.User.FullName,
        s.User.Role, s.CheckIn, s.CheckOut, s.ManagerNote,
        s.States.OrderBy(t => t.Template.SortOrder)
            .Select(t => new TaskStateDto(t.TaskTemplateId, t.Done, t.DoneAt,
                t.PhotoPath, t.Review, t.ReviewNote)).ToList());

    /// Lấy-hoặc-tạo ca làm của CHÍNH MÌNH trong 1 ngày/ca (kèm checklist theo vị trí)
    [HttpPost("open")]
    public async Task<ActionResult<SessionDto>> Open(OpenSessionReq req)
    {
        var date = req.Date == null
            ? DateOnly.FromDateTime(DateTime.Now)
            : DateOnly.Parse(req.Date);

        var s = await Query().FirstOrDefaultAsync(x =>
            x.UserId == Uid && x.Date == date && x.Shift == req.Shift);
        if (s != null) return ToDto(s);

        var me = await db.Users.FindAsync(Uid);
        var templates = await db.TaskTemplates
            .Where(t => t.IsActive && t.Role == me!.Role).ToListAsync();

        s = new WorkSession
        {
            UserId = Uid, Date = date, Shift = req.Shift,
            States = templates.Select(t => new TaskState { TaskTemplateId = t.Id }).ToList(),
        };
        db.WorkSessions.Add(s);
        await db.SaveChangesAsync();
        return ToDto(await Query().FirstAsync(x => x.Id == s.Id));
    }

    /// Danh sách ca — CHT xem tất cả, nhân viên chỉ ca của mình
    [HttpGet]
    public async Task<List<SessionDto>> List([FromQuery] string? from, [FromQuery] string? to)
    {
        var q = Query();
        if (!IsManager) q = q.Where(s => s.UserId == Uid);
        if (from != null) { var f = DateOnly.Parse(from); q = q.Where(s => s.Date >= f); }
        if (to != null) { var t = DateOnly.Parse(to); q = q.Where(s => s.Date <= t); }
        var list = await q.OrderByDescending(s => s.Date).ToListAsync();
        // chỉ trả ca có hoạt động (đã chấm công hoặc đã tích việc)
        return list.Where(s => s.CheckIn != null || s.States.Any(t => t.Done))
            .Select(ToDto).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SessionDto>> One(int id)
    {
        var s = await Query().FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return NotFound();
        if (!IsManager && s.UserId != Uid) return Forbid();
        return ToDto(s);
    }

    [HttpPost("{id:int}/checkin")]
    public async Task<IActionResult> CheckIn(int id)
    {
        var s = await db.WorkSessions.FindAsync(id);
        if (s == null) return NotFound();
        if (s.UserId != Uid) return Forbid();
        s.CheckIn ??= DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/checkout")]
    public async Task<IActionResult> CheckOut(int id)
    {
        var s = await db.WorkSessions.FindAsync(id);
        if (s == null) return NotFound();
        if (s.UserId != Uid) return Forbid();
        if (s.CheckIn == null) return BadRequest(new { message = "Chưa chấm công vào ca" });
        s.CheckOut ??= DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// Tick / bỏ tick 1 việc. Việc yêu cầu ảnh phải upload ảnh TRƯỚC rồi mới tick.
    [HttpPut("{id:int}/tasks/{templateId:int}")]
    public async Task<IActionResult> Toggle(int id, int templateId, ToggleTaskReq req)
    {
        var (err, st) = await GetOwnState(id, templateId);
        if (err != null) return err;

        if (req.Done && st!.Template.RequiresPhoto && st.PhotoPath == null)
            return BadRequest(new { code = "PHOTO_REQUIRED",
                message = "Việc này bắt buộc chụp ảnh minh chứng trước khi tích" });

        st!.Done = req.Done;
        st.DoneAt = req.Done ? DateTime.UtcNow : null;
        if (!req.Done) { DeletePhoto(st); st.Review = null; st.ReviewNote = null; }
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// Upload ảnh minh chứng (multipart/form-data, field "file")
    [HttpPost("{id:int}/tasks/{templateId:int}/photo")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<object>> Photo(int id, int templateId, IFormFile file)
    {
        var (err, st) = await GetOwnState(id, templateId);
        if (err != null) return err;
        if (file.Length == 0) return BadRequest(new { message = "File rỗng" });

        DeletePhoto(st!);
        var name = $"{id}_{templateId}_{DateTime.Now:yyyyMMddHHmmss}.jpg";
        var path = Path.Combine(env.ContentRootPath, "wwwroot", "photos", name);
        await using (var fs = System.IO.File.Create(path))
            await file.CopyToAsync(fs);
        st!.PhotoPath = $"/photos/{name}";
        await db.SaveChangesAsync();
        return new { photoPath = st.PhotoPath };
    }

    /// Cửa hàng trưởng duyệt 1 việc: Đạt / Không đạt (+ ghi chú). review = null để bỏ đánh giá.
    [HttpPut("{id:int}/tasks/{templateId:int}/review")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> ReviewTask(int id, int templateId, ReviewTaskReq req)
    {
        var st = await db.TaskStates.FirstOrDefaultAsync(
            t => t.WorkSessionId == id && t.TaskTemplateId == templateId);
        if (st == null) return NotFound();
        st.Review = req.Review;
        st.ReviewNote = req.Note;
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// Nhận xét chung cuối ca của cửa hàng trưởng
    [HttpPut("{id:int}/manager-note")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> ManagerNote(int id, ManagerNoteReq req)
    {
        var s = await db.WorkSessions.FindAsync(id);
        if (s == null) return NotFound();
        s.ManagerNote = req.Note;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---------------- helpers ----------------
    IQueryable<WorkSession> Query() => db.WorkSessions
        .Include(s => s.User)
        .Include(s => s.States).ThenInclude(t => t.Template)
        .AsSplitQuery();

    async Task<(ActionResult? err, TaskState? st)> GetOwnState(int sessionId, int templateId)
    {
        var st = await db.TaskStates
            .Include(t => t.Session)
            .Include(t => t.Template)
            .FirstOrDefaultAsync(t => t.WorkSessionId == sessionId
                                   && t.TaskTemplateId == templateId);
        if (st == null) return (NotFound(), null);
        if (st.Session.UserId != Uid) return (Forbid(), null);
        if (st.Session.CheckIn == null)
            return (BadRequest(new { code = "NOT_CHECKED_IN",
                message = "Chưa chấm công vào ca" }), null);
        if (st.Session.CheckOut != null)
            return (BadRequest(new { code = "SESSION_CLOSED",
                message = "Ca đã kết, checklist đã khóa" }), null);
        return (null, st);
    }

    void DeletePhoto(TaskState st)
    {
        if (st.PhotoPath == null) return;
        var p = Path.Combine(env.ContentRootPath, "wwwroot",
            st.PhotoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(p)) System.IO.File.Delete(p);
        st.PhotoPath = null;
    }
}
