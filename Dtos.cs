using RenoApi.Domain;

namespace RenoApi;

// ---------- Auth ----------
public record LoginReq(string Username, string Password);
public record UserDto(int Id, string Username, string FullName, StaffRole Role, bool IsActive);
public record LoginRes(string AccessToken, UserDto User);
public record CreateUserReq(string Username, string Password, string FullName, StaffRole Role);
public record UpdateUserReq(string? FullName, StaffRole? Role, bool? IsActive, string? Password);
public record ChangePasswordReq(string OldPassword, string NewPassword);
// ---------- Checklist ----------
public record TaskTemplateDto(int Id, StaffRole Role, TaskPhase Phase, string Title,
    string Standard, int Minutes, TaskPriority Priority, bool RequiresPhoto, int SortOrder);
public record UpsertTaskReq(StaffRole Role, TaskPhase Phase, string Title, string Standard,
    int Minutes, TaskPriority Priority, bool RequiresPhoto, int SortOrder);

// ---------- Ca làm ----------
public record OpenSessionReq(string? Date, Shift Shift);   // Date: "yyyy-MM-dd", null = hôm nay
public record TaskStateDto(int TaskTemplateId, bool Done, DateTime? DoneAt,
    string? PhotoPath, Review? Review, string? ReviewNote);
public record SessionDto(int Id, string Date, Shift Shift, int UserId, string UserFullName,
    StaffRole UserRole, DateTime? CheckIn, DateTime? CheckOut, string? ManagerNote,
    List<TaskStateDto> States);
public record ToggleTaskReq(bool Done);
public record ReviewTaskReq(Review? Review, string? Note);
public record ManagerNoteReq(string Note);

// ---------- NVL & Công thức ----------
public record IngredientDto(int Id, string Name, string Unit,
    double? PackQty, double? PackPrice, double? CostPerUnit);   // giá = null với nhân viên
public record UpsertIngredientReq(string Name, string Unit, double PackQty, double PackPrice);
public record RecipeLineDto(int IngredientId, string IngredientName, string Unit,
    double Qty, double? LineCost);
public record RecipeDto(int Id, string Name, string Category, string Method,
    double SellPrice, double? TotalCost, List<RecipeLineDto> Lines);
public record UpsertRecipeLineReq(int IngredientId, double Qty);
public record UpsertRecipeReq(string Name, string Category, string Method,
    double SellPrice, List<UpsertRecipeLineReq> Lines);

// ---------- Báo cáo ----------
public record StaffReportDto(int UserId, string FullName, StaffRole Role,
    int Shifts, int Done, int Total, int Missed, int Fail);
public record SummaryDto(int Sessions, int Missed, int Fail, double CompletionRate,
    List<StaffReportDto> Staff);
