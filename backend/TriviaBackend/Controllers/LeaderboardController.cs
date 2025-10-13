using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TriviaBackend.Data;
using TriviaBackend.Models.Entities;

namespace TriviaBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaderboardController : ControllerBase
    {
        private readonly TriviaDbContext _context;

        public LeaderboardController(TriviaDbContext context)
        {
            _context = context;
        }

        [HttpGet("global")]
        public async Task<ActionResult<IEnumerable<object>>> GetGlobalLeaderboard([FromQuery] int top = 100)
        {
            var players = await _context.Users
                .OfType<Player>()
                .ToListAsync();

            players.Sort();

            var leaderboard = players
                .Take(top)
                .Select((player, index) => new
                {
                    rank = index + 1,
                    username = player.Username,
                    elo = player.Elo,
                    gamesPlayed = player.GamesPlayed,
                    joinDate = player.Created
                });

            return Ok(leaderboard);
        }

        [HttpGet("rank/{username}")]
        public async Task<ActionResult<object>> GetPlayerRank(string username)
        {
            var targetPlayer = await _context.Users
                .OfType<Player>()
                .FirstOrDefaultAsync(p => p.Username == username);

            if (targetPlayer == null)
                return NotFound("Player not found");

            var allPlayers = await _context.Users
                .OfType<Player>()
                .ToListAsync();

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

        [HttpPost("update-stats")]
        public async Task<ActionResult> UpdatePlayerStats([FromBody] PlayerStatsUpdate statsUpdate)
        {
            var player = await _context.Users
                .OfType<Player>()
                .FirstOrDefaultAsync(p => p.Username == statsUpdate.Username);

            if (player == null)
                return NotFound("Player not found");

            player.Elo += statsUpdate.EloChange;
            player.GamesPlayed++;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                username = player.Username,
                newElo = player.Elo,
                totalGames = player.GamesPlayed
            });
        }
    }

    public class PlayerStatsUpdate
    {
        public string Username { get; set; } = string.Empty;
        public int EloChange { get; set; }
        public int PointsEarned { get; set; }
    }
}