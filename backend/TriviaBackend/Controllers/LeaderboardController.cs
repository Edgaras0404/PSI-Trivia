using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TriviaBackend.Data;
using TriviaBackend.Exceptions;
using TriviaBackend.Models.Entities;
using TriviaBackend.Services;

namespace TriviaBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaderboardController(PlayerService _PlayerService, ILogger<ExceptionHandler> _logger) : ControllerBase
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
                throw new UpdatePlayerStatsException("Error while updating player statistics");
            }

            return Ok(new
            {
                username = player.Username,
                newElo = player.Elo,
                totalGames = player.GamesPlayed
            });
        }
    }
}