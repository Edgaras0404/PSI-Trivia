using Microsoft.AspNetCore.Mvc;
using TriviaBackend.Models.Entities;
using TriviaBackend.Services.Interfaces.DB;

namespace TriviaBackend.Controllers
{
    /// <summary>
    /// Controller for handling actions related to clans
    /// </summary>
    /// <param name="_ClanService"></param>
    /// <param name="_UserService"></param>
    [Route("api/[controller]")]
    [ApiController]
    public class ClanController(IClanService _ClanService, IUserService _UserService) : Controller
    {
        [HttpGet("getclan/{clanId}")]
        public async Task<ActionResult<Clan>> GetClanByID(int clanId)
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

            if (user.ClanId == clan.Id)
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

            if (user.ClanId != clan.Id)
                return BadRequest("User is not in the clan");

            await _ClanService.RemoveMemberFromClanAsync(clan, user);    
            return Ok();
        }

        [HttpPost("create")]
        public async Task<ActionResult> CreateClan(string name)
        {
            if (await _ClanService.GetClanByNameAsync(name) != null)
                return Conflict($"Clan already exists with the name {name}");

            var clan = new Clan { Name = name };

            await _ClanService.CreateClanAsync(clan);

            return Ok();
        }

        [HttpPatch("renameClan")]
        public async Task<ActionResult> RenameClan(int clanId, string newName)
        {
            var clan = await _ClanService.GetClanByIdAsync(clanId);

            if (clan == null)
                return NotFound("Clan does not exist");

            await _ClanService.RenameClanAsync(clan, newName);
            return Ok();
        }

        [HttpDelete("delete")]
        public async Task<ActionResult> DeleteClan(int clanId)
        {
            var clan = await _ClanService.GetClanByIdAsync(clanId);

            if (clan == null)
                return NotFound("Clan does not exist");

            await _ClanService.DeleteClanAsync(clan);
            return NoContent();
        }
    }
}
