namespace Spurt.Data.Commands;

public class ClearContext(AppDbContext dbContext) : IClearContext
{
    public void Execute()
    {
        dbContext.ChangeTracker.Clear();
    }
}

public interface IClearContext
{
    void Execute();
}