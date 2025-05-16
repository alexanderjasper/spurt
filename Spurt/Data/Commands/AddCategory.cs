using Spurt.Domain.Categories;

namespace Spurt.Data.Commands;

public class AddCategory(AppDbContext dbContext) : IAddCategory
{
    public async Task<Category> Execute(Category category)
    {
        await dbContext.Categories.AddAsync(category);
        await dbContext.SaveChangesAsync();
        return category;
    }
}

public interface IAddCategory
{
    Task<Category> Execute(Category category);
} 