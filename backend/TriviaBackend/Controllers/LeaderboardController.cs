using Microsoft.AspNetCore.Mvc;
using TriviaBackend.Exceptions;
using TriviaBackend.Models.Entities;
using TriviaBackend.Services.Interfaces.DB;
using TriviaBackend.Services.Implementations;

namespace TriviaBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaderboardController(IPlayerService _PlayerService, ILogger<ExceptionHandler> _logger) : ControllerBase
    {
        /// <summary>
        /// Get global ranking of players by elo
        /// </summary>
        /// <param name="top"></param>
        /// <returns></returns>
        [HttpGet("global")]
        public async Task<ActionResult<IEnumerable<object>>> GetGlobalLeaderboard([FromQuery] int top = 100)
        {
            var players = await _PlayerService.GetAllPlayersAsync();

            players.Sort();

            var leaderboard = players
                .Take(top)
                .Select((player, index) => new
                {
                    rank = index + 1,
                    username = player.Username,
                    elo = player.Elo,
                    totalPoints = player.TotalPoints,
                    gamesPlayed = player.GamesPlayed,
                    joinDate = player.Created
                });

            return Ok(leaderboard);
        }

        /// <summary>
        /// Get the position of a player in the global leaderboard
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        [HttpGet("rank/{username}")]
        public async Task<ActionResult<object>> GetPlayerRank(string username)
        {
            var targetPlayer = await _PlayerService.GetPlayerByUsernameAsync(username);

            if (targetPlayer == null)
            {
                _logger.LogError("ERROR: Player not found");
                return NotFound("Player not found");
            }

            var allPlayers = await _PlayerService.GetAllPlayersAsync();

            allPlayers.Sort();

            var rank = allPlayers.FindIndex(p => p.Username == username) + 1;

            return Ok(new
            {
                username = targetPlayer.Username,
                rank = rank,
                elo = targetPlayer.Elo,
                gamesPlayed = targetPlayer.GamesPlayed,
                totalPlayers = allPlayers.Count
            });
        }

        /// <summary>
        /// Update the elo of a player and increase GamesPlayed
        /// </summary>
        /// <param name="statsUpdate"></param>
        /// <returns></returns>
        [HttpPost("update-stats")]
        public async Task<ActionResult> UpdatePlayerStats([FromBody] PlayerStatsUpdate statsUpdate)
        {
            var player = await _PlayerService.GetPlayerByUsernameAsync(statsUpdate.Username);

            if (player == null)
            {
                _logger.LogError("ERROR: Player not found");
                return NotFound("Player not found");
            }

            player.Elo += statsUpdate.EloChange;
            player.GamesPlayed++;

            try
            {
                await _PlayerService.UpdatePlayerAsync(player);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR updating player statistics: {ex.Message}");
                throw new PlayerStatsUpdateException("Error while updating player statistics");
            }

            return Ok(new
            {
                username = player.Username,
                newElo = player.Elo,
                totalGames = player.GamesPlayed
            });
        }

        [HttpGet("statistics/{username}")]
        public async Task<ActionResult<object>> GetLeaderboardStatistics()
        {
            var players = await _PlayerService.GetAllPlayersAsync();

            if (players == null || players.Count == 0)
            {
                return Ok(new
                {
                    averagePoints = 0,
                    totalPoints = 0,
                    topPlayer = (string?)null,
                    playerCount = 0
                });
            }

            var gamePlayers = players.Select(p => new GamePlayer
            {
                Id = 0, // Temporary ID for statistics
                Name = p.Username,
                CurrentGameScore = p.TotalPoints,
                CorrectAnswersInGame = p.GamesPlayed // Using GamesPlayed as approximation
            }).ToList();

            var statsCalculator = new StatisticsCalculator<GamePlayer, int>();

            var avgPoints = statsCalculator.CalculateAverage(gamePlayers, gp => gp.CurrentGameScore);
            var totalPoints = statsCalculator.CalculateTotal(gamePlayers, gp => gp.CurrentGameScore);
            var topPlayer = statsCalculator.FindTopPerformer(gamePlayers, gp => gp.CurrentGameScore);

            return Ok(new
            {
                averagePoints = avgPoints,
                totalPoints = totalPoints,
                topPlayer = topPlayer?.Name,
                playerCount = players.Count
            });
        }
    }
}