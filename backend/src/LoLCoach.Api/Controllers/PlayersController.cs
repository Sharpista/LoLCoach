using LoLCoach.Api.Application;
using Microsoft.AspNetCore.Mvc;

namespace LoLCoach.Api.Controllers;

[ApiController]
[Route("api/players")]
public sealed class PlayersController(SearchPlayerHandler handler) : ControllerBase
{
    [HttpPost("search")]
    public async Task<ActionResult<PlayerDto>> Search(SearchPlayerCommand command, CancellationToken cancellationToken)
        => Ok(await handler.SearchAsync(command, cancellationToken));
}
