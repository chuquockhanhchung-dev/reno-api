using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenoApi.Data;
using RenoApi.Domain;

namespace RenoApi.Controllers;

/// Báo cáo ngày/tuần/tháng — chỉ Cửa hàng trưởng
[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Manager")]
public class ReportsController(AppDb db) : ControllerBase
{
    /// ?days=1 (hôm nay) / 7 / 30
    [HttpGet("summary")]
    public async Task<SummaryDto> Summary([FromQuery] int days = 7)
    {
        var from = DateOnly.FromDateTime(DateTime.Now).AddDays(-(days - 1));
        var sessions = await db.WorkSessions
            .Include(s => s.User)
            .Include(s => s.States)
            .Where(s => s.Date >= from)
            .ToListAsync();
        // chỉ tính ca có hoạt động
        sessions = sessions.Where(s => s.CheckIn != null || s.States.Any(t => t.Done)).ToList();

        var staff = sessions.GroupBy(s => s.User)
            .Select(g => new StaffReportDto(
                g.Key.Id, g.Key.FullName, g.Key.Role,
                Shifts: g.Count(),
                Done: g.Sum(s => s.States.Count(t => t.Done)),
                Total: g.Sum(s => s.States.Count),
                Missed: g.Sum(s => s.States.Count(t => !t.Done)),
                Fail: g.Sum(s => s.States.Count(t => t.Review == Review.Fail))))
            .OrderByDescending(x => x.Total == 0 ? 0 : (double)x.Done / x.Total)
            .ToList();

        var total = staff.Sum(x => x.Total);
        var done = staff.Sum(x => x.Done);
        return new SummaryDto(
            Sessions: sessions.Count,
            Missed: staff.Sum(x => x.Missed),
            Fail: staff.Sum(x => x.Fail),
            CompletionRate: total == 0 ? 0 : Math.Round(done * 100.0 / total, 1),
            Staff: staff);
    }
}
