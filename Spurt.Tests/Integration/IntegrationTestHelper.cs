using Microsoft.Extensions.DependencyInjection;
using Spurt.Domain.Categories;
using Spurt.Domain.Categories.Commands;
using Spurt.Domain.Games;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Users;
using Spurt.Domain.Users.Commands;

namespace Spurt.Tests.Integration;

public class IntegrationTestHelper(TestDbContextFixture.TestEnvironment testEnv)
{
    public async Task<Game> CreateGame(int numberOfPlayers)
    {
        if (numberOfPlayers < 2) throw new ArgumentException("Number of players must be at least 2.");

        // Get real implementations from the test environment's service provider
        var registerUser = testEnv.ServiceProvider.GetRequiredService<RegisterUser>();
        var createGame = testEnv.ServiceProvider.GetRequiredService<CreateGame>();
        var joinGame = testEnv.ServiceProvider.GetRequiredService<JoinGame>();
        var saveCategory = testEnv.ServiceProvider.GetRequiredService<ISaveCategory>();

        // Step 1: Set up a game
        var users = new List<User>();
        for (var i = 0; i < numberOfPlayers; i++)
        {
            var user = await registerUser.Execute($"Player {i + 1}");
            users.Add(user);
        }

        var game = await createGame.Execute(users[0].Id);
        for (var i = 0; i < numberOfPlayers; i++)
        {
            if (i > 0) game = await joinGame.Execute(game.Code, users[i].Id);

            var player = game.Players.Single(p => p.UserId == users[i].Id);
            var category = new Category
            {
                Title = "",
                PlayerId = player.Id,
                Player = player,
                Clues = [],
            };
            foreach (var pointValue in new[] { 100, 200, 300, 400, 500 })
                category.Clues.Add(new Clue
                {
                    Answer = "",
                    Question = "",
                    PointValue = pointValue,
                    CategoryId = category.Id,
                    Category = category,
                });
            await saveCategory.Execute(category, true);
        }

        return game;
    }
}