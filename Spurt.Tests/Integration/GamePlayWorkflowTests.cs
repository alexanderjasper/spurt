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
        using var testEnv = _fixture.CreateTestEnvironment();
        var helper = new IntegrationTestHelper(testEnv);
        var game = await helper.CreateGame(3);
        var startGame = testEnv.ServiceProvider.GetRequiredService<StartGame>();
        var selectClue = testEnv.ServiceProvider.GetRequiredService<SelectClue>();
        var pressBuzzer = testEnv.ServiceProvider.GetRequiredService<PressBuzzer>();
        var judgeAnswer = testEnv.ServiceProvider.GetRequiredService<JudgeAnswer>();

        // 1: Start the game
        var creatorPlayer = game.Players.Single(p => p.IsCreator);
        game = await startGame.Execute(game.Code, creatorPlayer.UserId);

        Assert.Equal(GameState.InProgress, game.State);
        Assert.Equal(creatorPlayer.Id, game.CurrentChoosingPlayerId);

        // 2: Creator selects a clue from player2's category
        var player2 = game.Players.First(p => !p.IsCreator);
        var clue = player2.Category!.Clues.First();
        game = await selectClue.Execute(game.Code, clue.Id);

        Assert.Equal(GameState.ClueSelected, game.State);
        Assert.Equal(clue.Id, game.SelectedClueId);

        // 3: Player 3 presses the buzzer
        var player3 = game.Players.Single(p => p.Id != creatorPlayer.Id && p.Id != player2.Id);
        game = await pressBuzzer.Execute(game.Code, player3.Id);

        Assert.Equal(GameState.BuzzerPressed, game.State);
        Assert.Equal(player3.Id, game.BuzzedPlayerId);

        // 4: Player 2 (clue owner) judges the answer as correct
        game = await judgeAnswer.Execute(game.Code, player2.Id, true);

        Assert.Equal(GameState.InProgress, game.State);
        Assert.Equal(100, player3.GetScore());
        Assert.Equal(player3.Id, game.CurrentChoosingPlayerId);
        Assert.Null(game.SelectedClue);
        Assert.Null(game.BuzzedPlayerId);
    }

    [Fact]
    public async Task GamePlayWorkflow_IncorrectAnswer_DoesNotAwardPoints()
    {
        using var testEnv = _fixture.CreateTestEnvironment();
        var helper = new IntegrationTestHelper(testEnv);
        var game = await helper.CreateGame(3);
        var startGame = testEnv.ServiceProvider.GetRequiredService<StartGame>();
        var selectClue = testEnv.ServiceProvider.GetRequiredService<SelectClue>();
        var pressBuzzer = testEnv.ServiceProvider.GetRequiredService<PressBuzzer>();
        var judgeAnswer = testEnv.ServiceProvider.GetRequiredService<JudgeAnswer>();

        // 1: Start the game
        var creatorPlayer = game.Players.Single(p => p.IsCreator);
        game = await startGame.Execute(game.Code, creatorPlayer.UserId);

        // 2: Creator selects a clue from player2's category
        var player2 = game.Players.First(p => !p.IsCreator);
        var clue = player2.Category!.Clues.First();
        game = await selectClue.Execute(game.Code, clue.Id);

        // 3: Player 3 presses the buzzer
        var player3 = game.Players.Single(p => p.Id != creatorPlayer.Id && p.Id != player2.Id);
        game = await pressBuzzer.Execute(game.Code, player3.Id);

        // 4: Player 2 (clue owner) judges the answer as incorrect
        game = await judgeAnswer.Execute(game.Code, player2.Id, false);

        // Verify:
        Assert.Equal(GameState.ClueSelected, game.State);
        Assert.Equal(0, player3.GetScore());
        Assert.Equal(creatorPlayer.Id, game.CurrentChoosingPlayerId);
        Assert.Null(game.BuzzedPlayerId);
    }

    [Fact]
    public async Task GamePlayWorkflow_ClueOwnerCannotBuzz_ThrowsException()
    {
        using var testEnv = _fixture.CreateTestEnvironment();
        var helper = new IntegrationTestHelper(testEnv);
        var game = await helper.CreateGame(3);
        var startGame = testEnv.ServiceProvider.GetRequiredService<StartGame>();
        var selectClue = testEnv.ServiceProvider.GetRequiredService<SelectClue>();
        var pressBuzzer = testEnv.ServiceProvider.GetRequiredService<PressBuzzer>();

        // 1: Start the game
        var creatorPlayer = game.Players.Single(p => p.IsCreator);
        game = await startGame.Execute(game.Code, creatorPlayer.UserId);

        // 2: Creator selects a clue from player2's category
        var player2 = game.Players.First(p => !p.IsCreator);
        var clue = player2.Category!.Clues.First();
        game = await selectClue.Execute(game.Code, clue.Id);

        // 3: Player 2 (clue owner) tries to press the buzzer on their own clue
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await pressBuzzer.Execute(game.Code, player2.Id));
    }

    [Fact]
    public async Task GamePlayWorkflow_OnlyClueOwnerCanJudge_ThrowsException()
    {
        using var testEnv = _fixture.CreateTestEnvironment();
        var helper = new IntegrationTestHelper(testEnv);
        var game = await helper.CreateGame(3);
        var startGame = testEnv.ServiceProvider.GetRequiredService<StartGame>();
        var selectClue = testEnv.ServiceProvider.GetRequiredService<SelectClue>();
        var pressBuzzer = testEnv.ServiceProvider.GetRequiredService<PressBuzzer>();
        var judgeAnswer = testEnv.ServiceProvider.GetRequiredService<JudgeAnswer>();

        // 1: Start the game
        var player1 = game.Players.Single(p => p.IsCreator);
        game = await startGame.Execute(game.Code, player1.UserId);

        // 2: Creator selects a clue from player2's category
        var player2 = game.Players.First(p => !p.IsCreator);
        var clue = player2.Category!.Clues.First();
        game = await selectClue.Execute(game.Code, clue.Id);

        // 3: Player 3 presses the buzzer
        var player3 = game.Players.Single(p => p.Id != player1.Id && p.Id != player2.Id);
        game = await pressBuzzer.Execute(game.Code, player3.Id);

        // 4: Player 3 (not clue owner) tries to judge answer
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await judgeAnswer.Execute(game.Code, player3.Id, true));
    }

    [Fact]
    public async Task CompleteGamePlayWorkflow_MultipleTurns_PointsAccumulate()
    {
        using var testEnv = _fixture.CreateTestEnvironment();
        var helper = new IntegrationTestHelper(testEnv);
        var game = await helper.CreateGame(3);
        var startGame = testEnv.ServiceProvider.GetRequiredService<StartGame>();
        var selectClue = testEnv.ServiceProvider.GetRequiredService<SelectClue>();
        var pressBuzzer = testEnv.ServiceProvider.GetRequiredService<PressBuzzer>();
        var judgeAnswer = testEnv.ServiceProvider.GetRequiredService<JudgeAnswer>();
        var player1 = game.Players.Single(p => p.IsCreator);
        var player2 = game.Players.First(p => !p.IsCreator);
        var player3 = game.Players.Single(p => p.Id != player1.Id && p.Id != player2.Id);
        var clue1 = player2.Category!.Clues.First();
        var clue2 = player3.Category!.Clues.First();

        // 1: Start the game and play a round with player 3 answering correctly
        game = await startGame.Execute(game.Code, player1.UserId);
        game = await selectClue.Execute(game.Code, clue1.Id);
        game = await pressBuzzer.Execute(game.Code, player3.Id);
        game = await judgeAnswer.Execute(game.Code, player2.Id, true);
        Assert.Equal(0, player1.GetScore());
        Assert.Equal(0, player2.GetScore());
        Assert.Equal(100, player3.GetScore());

        // 2: Now player 3 selects a clue
        game = await selectClue.Execute(game.Code, clue2.Id);

        // 3: Player 1 (creator) presses the buzzer
        game = await pressBuzzer.Execute(game.Code, player1.Id);

        // 4: Player 3 (clue owner) judges answer as correct
        game = await judgeAnswer.Execute(game.Code, player3.Id, true);

        Assert.Equal(100, player1.GetScore());
        Assert.Equal(0, player2.GetScore());
        Assert.Equal(100, player3.GetScore());
        Assert.Equal(player1.Id, game.CurrentChoosingPlayerId);
    }

    [Fact]
    public async Task GamePlayWorkflow_LastClueAnswered_TransitionsToFinishedState()
    {
        using var testEnv = _fixture.CreateTestEnvironment();
        var helper = new IntegrationTestHelper(testEnv);
        var game = await helper.CreateGame(2);
        var startGame = testEnv.ServiceProvider.GetRequiredService<StartGame>();
        var selectClue = testEnv.ServiceProvider.GetRequiredService<SelectClue>();
        var pressBuzzer = testEnv.ServiceProvider.GetRequiredService<PressBuzzer>();
        var judgeAnswer = testEnv.ServiceProvider.GetRequiredService<JudgeAnswer>();
        var player1 = game.Players.Single(p => p.IsCreator);
        var player2 = game.Players.Single(p => !p.IsCreator);
        game = await startGame.Execute(game.Code, player1.UserId);

        // Answer all clues except the last one from each player
        var player1Clues = player1.Category!.Clues.OrderBy(c => c.PointValue).ToList();
        var player2Clues = player2.Category!.Clues.OrderBy(c => c.PointValue).ToList();

        // Answer 4 out of 5 clues from player1's category (player2 answers them)
        for (var i = 0; i < 4; i++)
        {
            game = await selectClue.Execute(game.Code, player1Clues[i].Id);
            game = await pressBuzzer.Execute(game.Code, player2.Id);
            game = await judgeAnswer.Execute(game.Code, player1.Id, true);
        }

        // Answer 4 out of 5 clues from player2's category (player1 answers them)
        for (var i = 0; i < 4; i++)
        {
            game = await selectClue.Execute(game.Code, player2Clues[i].Id);
            game = await pressBuzzer.Execute(game.Code, player1.Id);
            game = await judgeAnswer.Execute(game.Code, player2.Id, true);
        }

        // Now only 2 clues remain - the last one from each player
        var lastCluePlayer1 = player1Clues[4]; // 500 points clue
        var lastCluePlayer2 = player2Clues[4]; // 500 points clue

        // Player1 has control, selects player2's last clue
        game = await selectClue.Execute(game.Code, lastCluePlayer2.Id);
        game = await pressBuzzer.Execute(game.Code, player1.Id);
        game = await judgeAnswer.Execute(game.Code, player2.Id, true);

        // Now player1 has control and selects the very last clue
        game = await selectClue.Execute(game.Code, lastCluePlayer1.Id);
        game = await pressBuzzer.Execute(game.Code, player2.Id);
        game = await judgeAnswer.Execute(game.Code, player1.Id, true);

        // Verify game is now in Finished state
        Assert.Equal(GameState.Finished, game.State);

        // Verify scores (each player answered 5 clues worth 100+200+300+400+500 = 1500 points)
        Assert.Equal(1500, player1.GetScore());
        Assert.Equal(1500, player2.GetScore());
    }

    [Fact]
    public async Task GamePlayWorkflow_WhenOnlyAnsweringPlayerHasClues_OtherPlayerBecomesChooser()
    {
        using var testEnv = _fixture.CreateTestEnvironment();
        var helper = new IntegrationTestHelper(testEnv);
        var game = await helper.CreateGame(3);
        var startGame = testEnv.ServiceProvider.GetRequiredService<StartGame>();
        var selectClue = testEnv.ServiceProvider.GetRequiredService<SelectClue>();
        var pressBuzzer = testEnv.ServiceProvider.GetRequiredService<PressBuzzer>();
        var judgeAnswer = testEnv.ServiceProvider.GetRequiredService<JudgeAnswer>();
        var player1 = game.Players.Single(p => p.IsCreator);
        var player2 = game.Players.First(p => !p.IsCreator);
        var player3 = game.Players.Single(p => p.Id != player1.Id && p.Id != player2.Id);
        game = await startGame.Execute(game.Code, player1.UserId);

        // Answer all clues from player2 and player3 except one from player2
        var remainingClue = player2.Category!.Clues.First();
        foreach (var clue in player2.Category!.Clues.Where(c => c.Id != remainingClue.Id))
        {
            game = await selectClue.Execute(game.Code, clue.Id);
            game = await pressBuzzer.Execute(game.Code, player1.Id);
            game = await judgeAnswer.Execute(game.Code, player2.Id, true);
        }

        foreach (var clue in player3.Category!.Clues)
        {
            game = await selectClue.Execute(game.Code, clue.Id);
            game = await pressBuzzer.Execute(game.Code, player1.Id);
            game = await judgeAnswer.Execute(game.Code, player3.Id, true);
        }

        // Now player1 selects the last remaining clue from player2
        game = await selectClue.Execute(game.Code, remainingClue.Id);
        game = await pressBuzzer.Execute(game.Code, player1.Id);
        game = await judgeAnswer.Execute(game.Code, player2.Id, true);

        // Since only player1 has remaining clues, either player2 or player3 should become the chooser
        Assert.NotEqual(player1.Id, game.CurrentChoosingPlayerId);
        Assert.True(game.CurrentChoosingPlayerId == player2.Id || game.CurrentChoosingPlayerId == player3.Id);
    }

    [Fact]
    public async Task GamePlayWorkflow_WhenOtherPlayersHaveClues_AnsweringPlayerBecomesChooser()
    {
        using var testEnv = _fixture.CreateTestEnvironment();
        var helper = new IntegrationTestHelper(testEnv);
        var game = await helper.CreateGame(3);
        var startGame = testEnv.ServiceProvider.GetRequiredService<StartGame>();
        var selectClue = testEnv.ServiceProvider.GetRequiredService<SelectClue>();
        var pressBuzzer = testEnv.ServiceProvider.GetRequiredService<PressBuzzer>();
        var judgeAnswer = testEnv.ServiceProvider.GetRequiredService<JudgeAnswer>();
        var player1 = game.Players.Single(p => p.IsCreator);
        var player2 = game.Players.First(p => !p.IsCreator);
        var player3 = game.Players.Single(p => p.Id != player1.Id && p.Id != player2.Id);

        // Start the game and answer some clues from player1's category
        game = await startGame.Execute(game.Code, player1.UserId);
        var answeredClue = player1.Category!.Clues.First();
        game = await selectClue.Execute(game.Code, answeredClue.Id);
        game = await pressBuzzer.Execute(game.Code, player2.Id);
        game = await judgeAnswer.Execute(game.Code, player1.Id, true);
        var targetClue = player2.Category!.Clues.First();
        game = await selectClue.Execute(game.Code, targetClue.Id);
        game = await pressBuzzer.Execute(game.Code, player3.Id);
        game = await judgeAnswer.Execute(game.Code, player2.Id, true);

        // Since other players still have clues, player3 (who answered correctly) should become chooser
        Assert.Equal(player3.Id, game.CurrentChoosingPlayerId);
    }
}