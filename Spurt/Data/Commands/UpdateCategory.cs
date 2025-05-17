using Spurt.Domain.Categories;

namespace Spurt.Data.Commands;

public class UpdateCategory(AppDbContext dbContext) : IUpdateCategory
{
    public async Task<Category> Execute(Category category)
    {
        var result = dbContext.Categories.Update(category);
        await dbContext.SaveChangesAsync();
        return result.Entity;
    }
}

public interface IUpdateCategory
{
    Task<Category> Execute(Category category);
}