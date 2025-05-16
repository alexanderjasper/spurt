using Microsoft.EntityFrameworkCore;
using Spurt.Domain.Categories;

namespace Spurt.Data.Commands;

public class UpdateCategory(AppDbContext dbContext) : IUpdateCategory
{
    public async Task<Category> Execute(Category category)
    {
        dbContext.Categories.Update(category);
        await dbContext.SaveChangesAsync();
        return category;
    }
}

public interface IUpdateCategory
{
    Task<Category> Execute(Category category);
} 