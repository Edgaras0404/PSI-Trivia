using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TriviaBackend.Models.Entities;
using TriviaBackend.Services.Interfaces.DB;

namespace TriviaBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClanController(IClanService _ClanService, IUserService _UserService) : Controller
    {
        [HttpGet("getmembers/{clanId}")]
        public async Task<ActionResult<BaseUser>> GetClanByID(int clanId)
        {
            var clan = await _ClanService.GetClanByIdAsync(clanId);

            if (clan == null)
                return NotFound();

            return Ok(clan);
        }

        [HttpPost("join/{clanId}")]
        public async Task<ActionResult> JoinClan(int clanId, string userId)
        {
            var user = await _UserService.GetUserByIdAsync(userId);

            if(user == null)
                return NotFound("User does not exist");

            var clan = await _ClanService.GetClanByIdAsync(clanId);

            if (clan == null)
                return NotFound("Clan does not exist");

            if (clan.MemberIds.Contains(userId) || user.ClanId == clan.Id)
                return BadRequest("User is already in the clan");

            await _ClanService.AddMemberToClanAsync(clan, user);
            return Ok();
        }

        [HttpDelete("leave/{clanId}")]
        public async Task<ActionResult> LeaveClan(int clanId, string userId)
        {
            var user = await _UserService.GetUserByIdAsync(userId);

            if (user == null)
                return NotFound("User does not exist");

            var clan = await _ClanService.GetClanByIdAsync(clanId);

            if (clan == null)
                return NotFound("Clan does not exist");

            if (!clan.MemberIds.Contains(userId) || user.ClanId != clan.Id)
                return BadRequest("User is not in the clan");

            await _ClanService.RemoveMemberFromClanAsync(clan, user);    
            return Ok();
        }
    }
}
