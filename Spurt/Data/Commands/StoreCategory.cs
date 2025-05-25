using Spurt.Domain.Categories;

namespace Spurt.Data.Commands;

public class StoreCategory(AppDbContext dbContext) : IStoreCategory
{
    public async Task<Category> Execute(Category category)
    {
        var isNew = !dbContext.Categories.Any(c => c.Id == category.Id);
        var result = isNew
            ? (await dbContext.Categories.AddAsync(category)).Entity
            : dbContext.Categories.Update(category).Entity;

        await dbContext.SaveChangesAsync();
        return result;
    }
}

public interface IStoreCategory
{
    Task<Category> Execute(Category category);
}