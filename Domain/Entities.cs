namespace RenoApi.Domain;

public enum StaffRole { Manager, Barista, Service }
public enum Shift { Morning, Afternoon, Evening }
public enum TaskPhase { Opening, Closing }
public enum TaskPriority { Required, Important, Routine }
public enum Review { Pass, Fail }

public class Store
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FullName { get; set; } = "";
    public StaffRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public int? StoreId { get; set; }
    public Store? Store { get; set; }
}

/// Mẫu công việc checklist (định nghĩa 1 lần) — theo file Vận_hành.xlsx
public class TaskTemplate
{
    public int Id { get; set; }
    public StaffRole Role { get; set; }          // việc của vị trí nào
    public TaskPhase Phase { get; set; }         // mở ca / đóng ca
    public string Title { get; set; } = "";      // hạng mục
    public string Standard { get; set; } = "";   // tiêu chuẩn / định mức
    public int Minutes { get; set; }
    public TaskPriority Priority { get; set; }
    public bool RequiresPhoto { get; set; }      // bắt buộc ảnh minh chứng
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// 1 ca làm của 1 nhân viên trong 1 ngày
public class WorkSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateOnly Date { get; set; }
    public Shift Shift { get; set; }
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public string? ManagerNote { get; set; }
    public List<TaskState> States { get; set; } = [];
}

/// Trạng thái 1 việc trong 1 ca
public class TaskState
{
    public int Id { get; set; }
    public int WorkSessionId { get; set; }
    public WorkSession Session { get; set; } = null!;
    public int TaskTemplateId { get; set; }
    public TaskTemplate Template { get; set; } = null!;
    public bool Done { get; set; }
    public DateTime? DoneAt { get; set; }
    public string? PhotoPath { get; set; }       // /photos/xxx.jpg
    public Review? Review { get; set; }          // Đạt / Không đạt — CHT set
    public string? ReviewNote { get; set; }
}

/// NVL: mua theo gói (100g giá 70.000) → cost/đơn vị = 700đ/g
public class Ingredient
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "g";      // g/ml/quả/hộp/gói/thìa
    public double PackQty { get; set; }
    public double PackPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public double CostPerUnit => PackQty == 0 ? 0 : PackPrice / PackQty;
}

public class Recipe
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Method { get; set; } = "";
    public double SellPrice { get; set; }
    public List<RecipeLine> Lines { get; set; } = [];
}

public class RecipeLine
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public int IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;
    public double Qty { get; set; }
}
