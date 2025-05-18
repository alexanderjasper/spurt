using NSubstitute;
using Spurt.Data.Commands;
using Spurt.Data.Queries;
using Spurt.Domain.Categories;
using Spurt.Domain.Games;
using Spurt.Domain.Games.Commands;
using Spurt.Domain.Players;
using Spurt.Domain.Users;

namespace Spurt.Tests.Integration;

public class GamePlayWorkflowTests
{
    // Real implementations
    private readonly StartGame _startGame;
    private readonly SelectClue _selectClue;
    private readonly PressBuzzer _pressBuzzer;
    private readonly JudgeAnswer _judgeAnswer;

    // Mocked data layer
    private readonly IGetGame _getGame;
    private readonly IGetClue _getClue;
    private readonly IUpdateGame _updateGame;
    private readonly IGameHubNotificationService _gameHubNotificationService;

    // Test data
    private readonly Guid _creatorId = Guid.NewGuid();
    private readonly Guid _player2Id = Guid.NewGuid();
    private readonly Guid _player3Id = Guid.NewGuid();
    private Game _game;
    private readonly string _gameCode = "GAME123";
    private readonly Player _creator;
    private readonly Player _player2;
    private readonly Player _player3;
    private readonly Category _creatorCategory;
    private readonly Category _player2Category;
    private readonly Category _player3Category;
    private readonly Clue _clue1;
    private readonly Clue _clue2;

    public GamePlayWorkflowTests()
    {
        // Initialize test data - Create a game with 3 players, each with their categories
        _game = new Game
        {
            Id = Guid.NewGuid(),
            Code = _gameCode,
            State = GameState.WaitingForCategories,
            Players = []
        };

        _creator = new Player
        {
            Id = Guid.NewGuid(),
            UserId = _creatorId,
            User = new User { Id = _creatorId, Name = "Creator" },
            GameId = _game.Id,
            Game = _game,
            IsCreator = true,
            Score = 0
        };

        _player2 = new Player
        {
            Id = Guid.NewGuid(),
            UserId = _player2Id,
            User = new User { Id = _player2Id, Name = "Player 2" },
            GameId = _game.Id,
            Game = _game,
            IsCreator = false,
            Score = 0
        };

        _player3 = new Player
        {
            Id = Guid.NewGuid(),
            UserId = _player3Id,
            User = new User { Id = _player3Id, Name = "Player 3" },
            GameId = _game.Id,
            Game = _game,
            IsCreator = false,
            Score = 0
        };

        _creatorCategory = new Category
        {
            Id = Guid.NewGuid(),
            Title = "Creator's Category",
            PlayerId = _creator.Id,
            Player = _creator,
            IsSubmitted = true,
            Clues = []
        };

        _player2Category = new Category
        {
            Id = Guid.NewGuid(),
            Title = "Player 2's Category",
            PlayerId = _player2.Id,
            Player = _player2,
            IsSubmitted = true,
            Clues = []
        };

        _player3Category = new Category
        {
            Id = Guid.NewGuid(),
            Title = "Player 3's Category",
            PlayerId = _player3.Id,
            Player = _player3,
            IsSubmitted = true,
            Clues = []
        };

        _creator.Category = _creatorCategory;
        _player2.Category = _player2Category;
        _player3.Category = _player3Category;

        _clue1 = new Clue
        {
            Id = Guid.NewGuid(),
            Answer = "Answer 1",
            Question = "Question 1",
            PointValue = 100,
            IsAnswered = false,
            CategoryId = _player2Category.Id,
            Category = _player2Category
        };

        _clue2 = new Clue
        {
            Id = Guid.NewGuid(),
            Answer = "Answer 2",
            Question = "Question 2",
            PointValue = 200,
            IsAnswered = false,
            CategoryId = _player3Category.Id,
            Category = _player3Category
        };

        _player2Category.Clues.Add(_clue1);
        _player3Category.Clues.Add(_clue2);

        _game.Players.Add(_creator);
        _game.Players.Add(_player2);
        _game.Players.Add(_player3);

        // Configure mocks
        _getGame = Substitute.For<IGetGame>();
        _getGame.Execute(_gameCode).Returns(_ => _game);
        _getGame.Execute(_gameCode, Arg.Any<bool>()).Returns(_ => _game);

        _getClue = Substitute.For<IGetClue>();
        _getClue.Execute(_clue1.Id).Returns(_clue1);
        _getClue.Execute(_clue2.Id).Returns(_clue2);

        _updateGame = Substitute.For<IUpdateGame>();
        _updateGame.Execute(Arg.Any<Game>()).Returns(callInfo => {
            // Update our reference to maintain consistent state
            _game = callInfo.Arg<Game>();
            return _game;
        });

        _gameHubNotificationService = Substitute.For<IGameHubNotificationService>();

        // Create real implementations with mocked dependencies
        _startGame = new StartGame(_getGame, _updateGame, _gameHubNotificationService);
        _selectClue = new SelectClue(_getGame, _getClue, _updateGame, _gameHubNotificationService);
        _pressBuzzer = new PressBuzzer(_getGame, _updateGame, _gameHubNotificationService);
        _judgeAnswer = new JudgeAnswer(_getGame, _updateGame, _gameHubNotificationService);
    }

    [Fact]
    public async Task CompleteGamePlayWorkflow_CorrectAnswer_Success()
    {
        // Step 1: Start the game
        var game = await _startGame.Execute(_gameCode, _creatorId);
        
        // Verify game is started
        Assert.Equal(GameState.InProgress, game.State);
        Assert.Equal(_creator.Id, game.CurrentChoosingPlayerId);
        
        // Step 2: Creator selects a clue from player2's category
        game = await _selectClue.Execute(_gameCode, _clue1.Id);
        
        // Verify clue is selected
        Assert.Equal(GameState.ClueSelected, game.State);
        Assert.Equal(_clue1.Id, game.SelectedClueId);
        
        // Step 3: Player 3 presses the buzzer
        game = await _pressBuzzer.Execute(_gameCode, _player3.Id);
        
        // Verify buzzer is pressed
        Assert.Equal(GameState.BuzzerPressed, game.State);
        Assert.Equal(_player3.Id, game.BuzzedPlayerId);
        
        // Step 4: Player 2 (clue owner) judges the answer as correct
        game = await _judgeAnswer.Execute(_gameCode, _player2.Id, true);
        
        // Verify player 3 got points and now has control
        Assert.Equal(GameState.InProgress, game.State);
        var player3 = game.Players.First(p => p.Id == _player3.Id);
        Assert.Equal(100, player3.Score); // Points awarded
        Assert.Equal(_player3.Id, game.CurrentChoosingPlayerId); // Player 3 has control
        Assert.Null(game.SelectedClue); // Clue is cleared
        Assert.Null(game.BuzzedPlayerId); // Buzzer state is cleared
        
        // Verify necessary dependencies were called
        await _updateGame.Received(4).Execute(Arg.Any<Game>()); // Once for each step
        await _gameHubNotificationService.Received(4).NotifyGameUpdated(Arg.Any<Game>());
    }
    
    [Fact]
    public async Task GamePlayWorkflow_IncorrectAnswer_DoesNotAwardPoints()
    {
        // Step 1: Start the game
        var game = await _startGame.Execute(_gameCode, _creatorId);
        
        // Step 2: Creator selects a clue
        game = await _selectClue.Execute(_gameCode, _clue1.Id);
        
        // Step 3: Player 3 presses the buzzer
        game = await _pressBuzzer.Execute(_gameCode, _player3.Id);
        
        // Step 4: Player 2 (clue owner) judges the answer as incorrect
        game = await _judgeAnswer.Execute(_gameCode, _player2.Id, false);
        
        // Verify:
        Assert.Equal(GameState.ClueSelected, game.State); // Back to clue selected state
        var player3 = game.Players.First(p => p.Id == _player3.Id);
        Assert.Equal(0, player3.Score); // No points awarded
        Assert.Equal(_creator.Id, game.CurrentChoosingPlayerId); // Creator still has control
        Assert.Null(game.BuzzedPlayerId); // Buzzer state is cleared
        
        await _updateGame.Received(4).Execute(Arg.Any<Game>());
        await _gameHubNotificationService.Received(4).NotifyGameUpdated(Arg.Any<Game>());
    }
    
    [Fact]
    public async Task GamePlayWorkflow_ClueOwnerCannotBuzz_ThrowsException()
    {
        // Step 1: Start the game
        var game = await _startGame.Execute(_gameCode, _creatorId);
        
        // Step 2: Creator selects a clue
        game = await _selectClue.Execute(_gameCode, _clue1.Id);
        
        // Step 3: Player 2 (clue owner) tries to press the buzzer on their own clue
        // This should throw an exception
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _pressBuzzer.Execute(_gameCode, _player2.Id));
        
        // Game state should remain unchanged
        Assert.Equal(GameState.ClueSelected, game.State);
        Assert.Null(game.BuzzedPlayerId);
    }
    
    [Fact]
    public async Task GamePlayWorkflow_OnlyClueOwnerCanJudge_ThrowsException()
    {
        // Step 1: Start the game
        var game = await _startGame.Execute(_gameCode, _creatorId);
        
        // Step 2: Creator selects a clue
        game = await _selectClue.Execute(_gameCode, _clue1.Id);
        
        // Step 3: Player 3 presses the buzzer
        game = await _pressBuzzer.Execute(_gameCode, _player3.Id);
        
        // Step 4: Player 3 (not clue owner) tries to judge the answer
        // This should throw an exception
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _judgeAnswer.Execute(_gameCode, _player3.Id, true));
        
        // Game state should remain unchanged
        Assert.Equal(GameState.BuzzerPressed, game.State);
        Assert.Equal(_player3.Id, game.BuzzedPlayerId);
    }
    
    [Fact]
    public async Task CompleteGamePlayWorkflow_MultipleTurns_PointsAccumulate()
    {
        // First turn - reset game state for this test
        _game = new Game
        {
            Id = Guid.NewGuid(),
            Code = _gameCode,
            State = GameState.WaitingForCategories,
            Players = []
        };
        
        // Re-add players to the game
        _creator.Game = _game;
        _player2.Game = _game;
        _player3.Game = _game;
        _game.Players.Add(_creator);
        _game.Players.Add(_player2);
        _game.Players.Add(_player3);
        
        var game = await _startGame.Execute(_gameCode, _creatorId);
        game = await _selectClue.Execute(_gameCode, _clue1.Id);
        game = await _pressBuzzer.Execute(_gameCode, _player3.Id);
        game = await _judgeAnswer.Execute(_gameCode, _player2.Id, true);
        
        // Second turn - Player 3 selects clue2, Creator buzzes in, Player 3 judges
        game = await _selectClue.Execute(_gameCode, _clue2.Id);
        game = await _pressBuzzer.Execute(_gameCode, _creatorId);
        game = await _judgeAnswer.Execute(_gameCode, _player3.Id, true);
        
        // Verify points and control
        var player3 = game.Players.First(p => p.Id == _player3.Id);
        var creator = game.Players.First(p => p.Id == _creatorId);
        
        Assert.Equal(100, player3.Score); // Points from first turn
        Assert.Equal(200, creator.Score); // Points from second turn
        Assert.Equal(_creatorId, game.CurrentChoosingPlayerId); // Creator has control again
        Assert.Equal(GameState.InProgress, game.State);
    }
} 