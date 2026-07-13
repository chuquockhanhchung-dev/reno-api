using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenoApi.Data;
using RenoApi.Domain;

namespace RenoApi.Controllers;

[ApiController]
[Route("api/recipes")]
[Authorize]
public class RecipesController(AppDb db) : ControllerBase
{
    /// Nhân viên: định lượng + cách làm (SOP), KHÔNG có cost.
    /// CHT: thêm cost từng dòng + tổng cost.
    [HttpGet]
    public async Task<List<RecipeDto>> All()
    {
        var manager = User.IsInRole("Manager");
        var list = await db.Recipes.Include(r => r.Lines).ThenInclude(l => l.Ingredient)
            .OrderBy(r => r.Category).ThenBy(r => r.Name).ToListAsync();
        return list.Select(r => ToDto(r, manager)).ToList();
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<RecipeDto>> Create(UpsertRecipeReq req)
    {
        var err = await Validate(req);
        if (err != null) return err;
        var r = new Recipe { Name = req.Name.Trim(), Category = req.Category.Trim(),
            Method = req.Method.Trim(), SellPrice = req.SellPrice,
            Lines = req.Lines.Select(l => new RecipeLine
                { IngredientId = l.IngredientId, Qty = l.Qty }).ToList() };
        db.Recipes.Add(r);
        await db.SaveChangesAsync();
        return ToDto(await Load(r.Id), true);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<RecipeDto>> Update(int id, UpsertRecipeReq req)
    {
        var r = await db.Recipes.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        var err = await Validate(req);
        if (err != null) return err;

        (r.Name, r.Category, r.Method, r.SellPrice) =
            (req.Name.Trim(), req.Category.Trim(), req.Method.Trim(), req.SellPrice);
        db.RecipeLines.RemoveRange(r.Lines);   // thay toàn bộ dòng NVL
        r.Lines = req.Lines.Select(l => new RecipeLine
            { IngredientId = l.IngredientId, Qty = l.Qty }).ToList();
        await db.SaveChangesAsync();
        return ToDto(await Load(id), true);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await db.Recipes.FindAsync(id);
        if (r == null) return NotFound();
        db.Recipes.Remove(r);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---------------- helpers ----------------
    async Task<Recipe> Load(int id) => await db.Recipes
        .Include(r => r.Lines).ThenInclude(l => l.Ingredient).FirstAsync(r => r.Id == id);

    async Task<ActionResult?> Validate(UpsertRecipeReq req)
    {
        if (string.IsNullOrWhiteSpace(req.Name) || req.Lines.Count == 0)
            return BadRequest(new { message = "Cần tên món và ít nhất 1 nguyên liệu" });
        var ids = req.Lines.Select(l => l.IngredientId).ToList();
        var count = await db.Ingredients.CountAsync(i => ids.Contains(i.Id));
        if (count != ids.Distinct().Count())
            return BadRequest(new { message = "Có NVL không tồn tại" });
        return null;
    }

    static RecipeDto ToDto(Recipe r, bool manager)
    {
        var lines = r.Lines.Select(l => new RecipeLineDto(
            l.IngredientId, l.Ingredient.Name, l.Ingredient.Unit, l.Qty,
            manager ? Math.Round(l.Qty * l.Ingredient.CostPerUnit) : null)).ToList();
        return new RecipeDto(r.Id, r.Name, r.Category, r.Method, r.SellPrice,
            manager ? Math.Round(r.Lines.Sum(l => l.Qty * l.Ingredient.CostPerUnit)) : null,
            lines);
    }
}
