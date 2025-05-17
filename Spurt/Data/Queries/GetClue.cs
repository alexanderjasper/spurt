using Microsoft.EntityFrameworkCore;
using Spurt.Domain.Categories;

namespace Spurt.Data.Queries;

public class GetClue(AppDbContext dbContext) : IGetClue
{
    public async Task<Clue?> Execute(Guid clueId)
    {
        return await dbContext.Clues
            .FirstOrDefaultAsync(c => c.Id == clueId);
    }
}

public interface IGetClue
{
    Task<Clue?> Execute(Guid userId);
}