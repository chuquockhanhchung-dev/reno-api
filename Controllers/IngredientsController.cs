using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenoApi.Data;
using RenoApi.Domain;

namespace RenoApi.Controllers;

[ApiController]
[Route("api/ingredients")]
[Authorize]
public class IngredientsController(AppDb db) : ControllerBase
{
    /// Nhân viên chỉ thấy tên + đơn vị; CHT thấy đủ giá mua + cost/đơn vị
    [HttpGet]
    public async Task<List<IngredientDto>> All()
    {
        var manager = User.IsInRole("Manager");
        var list = await db.Ingredients.Where(i => i.IsActive)
            .OrderBy(i => i.Name).ToListAsync();
        return list.Select(i => new IngredientDto(i.Id, i.Name, i.Unit,
            manager ? i.PackQty : null,
            manager ? i.PackPrice : null,
            manager ? i.CostPerUnit : null)).ToList();
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<IngredientDto>> Create(UpsertIngredientReq req)
    {
        if (req.PackQty <= 0) return BadRequest(new { message = "Số lượng mua phải > 0" });
        var i = new Ingredient { Name = req.Name.Trim(), Unit = req.Unit.Trim(),
            PackQty = req.PackQty, PackPrice = req.PackPrice };
        db.Ingredients.Add(i);
        await db.SaveChangesAsync();
        return new IngredientDto(i.Id, i.Name, i.Unit, i.PackQty, i.PackPrice, i.CostPerUnit);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<IngredientDto>> Update(int id, UpsertIngredientReq req)
    {
        var i = await db.Ingredients.FindAsync(id);
        if (i == null) return NotFound();
        if (req.PackQty <= 0) return BadRequest(new { message = "Số lượng mua phải > 0" });
        (i.Name, i.Unit, i.PackQty, i.PackPrice) =
            (req.Name.Trim(), req.Unit.Trim(), req.PackQty, req.PackPrice);
        await db.SaveChangesAsync();
        return new IngredientDto(i.Id, i.Name, i.Unit, i.PackQty, i.PackPrice, i.CostPerUnit);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var i = await db.Ingredients.FindAsync(id);
        if (i == null) return NotFound();
        if (await db.RecipeLines.AnyAsync(l => l.IngredientId == id))
            return BadRequest(new { message = "NVL đang dùng trong công thức, không xóa được" });
        i.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
