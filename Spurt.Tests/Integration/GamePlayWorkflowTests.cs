using Microsoft.Extensions.DependencyInjection;
using Spurt.Domain.Games;
using Spurt.Domain.Games.Commands;

namespace Spurt.Tests.Integration;

public class GamePlayWorkflowTests
{
    private readonly TestDbContextFixture _fixture = new();

    [Fact]
    public async Task CompleteGamePlayWorkflow_CorrectAnswer_Success()
    {
        // Create a fresh test environment for this test
        using var testEnv = _fixture.CreateTestEnvironment();
        var helper = new IntegrationTestHelper(testEnv);
        var game = await helper.CreateGame(3);

        // Get real implementations from the test environment's service provider
        var startGame = testEnv.ServiceProvider.GetRequiredService<StartGame>();
        var selectClue = testEnv.ServiceProvider.GetRequiredService<SelectClue>();
        var pressBuzzer = testEnv.ServiceProvider.GetRequiredService<PressBuzzer>();
        var judgeAnswer = testEnv.ServiceProvider.GetRequiredService<JudgeAnswer>();

        // Step 1: Start the game
        var creatorPlayer = game.Players.Single(p => p.IsCreator);
        game = await startGame.Execute(game.Code, creatorPlayer.UserId);

        // Verify game is started
        Assert.Equal(GameState.InProgress, game.State);
        Assert.Equal(creatorPlayer.Id, game.CurrentChoosingPlayerId);

        // Step 2: Creator selects a clue from player2's category
        var player2 = game.Players.First(p => !p.IsCreator);
        var clue = player2.Category!.Clues.First();
        game = await selectClue.Execute(game.Code, clue.Id);

        // Verify clue is selected
        Assert.Equal(GameState.ClueSelected, game.State);
        Assert.Equal(clue.Id, game.SelectedClueId);

        // Step 3: Player 3 presses the buzzer
        var player3 = game.Players.Single(p => p.Id != creatorPlayer.Id && p.Id != player2.Id);
        game = await pressBuzzer.Execute(game.Code, player3.Id);

        // Verify buzzer is pressed
        Assert.Equal(GameState.BuzzerPressed, game.State);
        Assert.Equal(player3.Id, game.BuzzedPlayerId);

        // Step 4: Player 2 (clue owner) judges the answer as correct
        game = await judgeAnswer.Execute(game.Code, player2.Id, true);

        // Verify player 3 got points and now has control
        Assert.Equal(GameState.InProgress, game.State);
        Assert.Equal(100, player3.Score);
        Assert.Equal(player3.Id, game.CurrentChoosingPlayerId);
        Assert.Null(game.SelectedClue);
        Assert.Null(game.BuzzedPlayerId);
    }

    [Fact]
    public async Task GamePlayWorkflow_IncorrectAnswer_DoesNotAwardPoints()
    {
        // Create a fresh test environment for this test
        using var testEnv = _fixture.CreateTestEnvironment();
        var helper = new IntegrationTestHelper(testEnv);
        var game = await helper.CreateGame(3);

        // Get real implementations from the test environment's service provider
        var startGame = testEnv.ServiceProvider.GetRequiredService<StartGame>();
        var selectClue = testEnv.ServiceProvider.GetRequiredService<SelectClue>();
        var pressBuzzer = testEnv.ServiceProvider.GetRequiredService<PressBuzzer>();
        var judgeAnswer = testEnv.ServiceProvider.GetRequiredService<JudgeAnswer>();

        // Step 1: Start the game
        var creatorPlayer = game.Players.Single(p => p.IsCreator);
        game = await startGame.Execute(game.Code, creatorPlayer.UserId);

        // Step 2: Creator selects a clue from player2's category
        var player2 = game.Players.First(p => !p.IsCreator);
        var clue = player2.Category!.Clues.First();
        game = await selectClue.Execute(game.Code, clue.Id);

        // Step 3: Player 3 presses the buzzer
        var player3 = game.Players.Single(p => p.Id != creatorPlayer.Id && p.Id != player2.Id);
        game = await pressBuzzer.Execute(game.Code, player3.Id);

        // Step 4: Player 2 (clue owner) judges the answer as incorrect
        game = await judgeAnswer.Execute(game.Code, player2.Id, false);

        // Verify:
        Assert.Equal(GameState.ClueSelected, game.State);
        Assert.Equal(0, player3.Score);
        Assert.Equal(creatorPlayer.Id, game.CurrentChoosingPlayerId);
        Assert.Null(game.BuzzedPlayerId);
    }

    [Fact]
    public async Task GamePlayWorkflow_ClueOwnerCannotBuzz_ThrowsException()
    {
        // Create a fresh test environment for this test
        using var testEnv = _fixture.CreateTestEnvironment();
        var helper = new IntegrationTestHelper(testEnv);
        var game = await helper.CreateGame(3);

        // Get real implementations from the test environment's service provider
        var startGame = testEnv.ServiceProvider.GetRequiredService<StartGame>();
        var selectClue = testEnv.ServiceProvider.GetRequiredService<SelectClue>();
        var pressBuzzer = testEnv.ServiceProvider.GetRequiredService<PressBuzzer>();

        // Step 1: Start the game
        var creatorPlayer = game.Players.Single(p => p.IsCreator);
        game = await startGame.Execute(game.Code, creatorPlayer.UserId);

        // Step 2: Creator selects a clue from player2's category
        var player2 = game.Players.First(p => !p.IsCreator);
        var clue = player2.Category!.Clues.First();
        game = await selectClue.Execute(game.Code, clue.Id);

        // Step 3: Player 2 (clue owner) tries to press the buzzer on their own clue
        // This should throw an exception
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await pressBuzzer.Execute(game.Code, player2.Id));
    }

    [Fact]
    public async Task GamePlayWorkflow_OnlyClueOwnerCanJudge_ThrowsException()
    {
        // Create a fresh test environment for this test
        using var testEnv = _fixture.CreateTestEnvironment();
        var helper = new IntegrationTestHelper(testEnv);
        var game = await helper.CreateGame(3);

        // Get real implementations from the test environment's service provider
        var startGame = testEnv.ServiceProvider.GetRequiredService<StartGame>();
        var selectClue = testEnv.ServiceProvider.GetRequiredService<SelectClue>();
        var pressBuzzer = testEnv.ServiceProvider.GetRequiredService<PressBuzzer>();
        var judgeAnswer = testEnv.ServiceProvider.GetRequiredService<JudgeAnswer>();

        // Step 1: Start the game
        var player1 = game.Players.Single(p => p.IsCreator);
        game = await startGame.Execute(game.Code, player1.UserId);

        // Step 2: Creator selects a clue from player2's category
        var player2 = game.Players.First(p => !p.IsCreator);
        var clue = player2.Category!.Clues.First();
        game = await selectClue.Execute(game.Code, clue.Id);

        // Step 3: Player 3 presses the buzzer
        var player3 = game.Players.Single(p => p.Id != player1.Id && p.Id != player2.Id);
        game = await pressBuzzer.Execute(game.Code, player3.Id);

        // Step 4: Player 3 (not clue owner) tries to judge answer
        // This should throw an exception
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await judgeAnswer.Execute(game.Code, player3.Id, true));
    }

    [Fact]
    public async Task CompleteGamePlayWorkflow_MultipleTurns_PointsAccumulate()
    {
        // Create a fresh test environment for this test
        using var testEnv = _fixture.CreateTestEnvironment();
        var helper = new IntegrationTestHelper(testEnv);
        var game = await helper.CreateGame(3);

        // Get real implementations from the test environment's service provider
        var startGame = testEnv.ServiceProvider.GetRequiredService<StartGame>();
        var selectClue = testEnv.ServiceProvider.GetRequiredService<SelectClue>();
        var pressBuzzer = testEnv.ServiceProvider.GetRequiredService<PressBuzzer>();
        var judgeAnswer = testEnv.ServiceProvider.GetRequiredService<JudgeAnswer>();

        // Get player references
        var player1 = game.Players.Single(p => p.IsCreator);
        var player2 = game.Players.First(p => !p.IsCreator);
        var player3 = game.Players.Single(p => p.Id != player1.Id && p.Id != player2.Id);
        var clue1 = player2.Category!.Clues.First();
        var clue2 = player3.Category!.Clues.First();

        // Step 1: Start the game and play a round with player 3 answering correctly
        game = await startGame.Execute(game.Code, player1.UserId);
        game = await selectClue.Execute(game.Code, clue1.Id);
        game = await pressBuzzer.Execute(game.Code, player3.Id);
        game = await judgeAnswer.Execute(game.Code, player2.Id, true);

        // After first round: player 3 has 100 points and has control
        Assert.Equal(0, player1.Score);
        Assert.Equal(0, player2.Score);
        Assert.Equal(100, player3.Score);

        // Step 2: Now player 3 selects a clue
        game = await selectClue.Execute(game.Code, clue2.Id);

        // Step 3: Player 1 (creator) presses the buzzer
        game = await pressBuzzer.Execute(game.Code, player1.Id);

        // Step 4: Player 3 (clue owner) judges answer as correct
        game = await judgeAnswer.Execute(game.Code, player3.Id, true);

        // Verify final scores
        Assert.Equal(100, player1.Score);
        Assert.Equal(0, player2.Score);
        Assert.Equal(100, player3.Score);
        Assert.Equal(player1.Id, game.CurrentChoosingPlayerId);
    }
}