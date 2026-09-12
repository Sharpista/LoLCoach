using LoLCoach.Api.Application;
using Microsoft.AspNetCore.Mvc;

namespace LoLCoach.Api.Controllers;

[ApiController]
[Route("api/players")]
public sealed class PlayersController(SearchPlayerHandler handler) : ControllerBase
{
    /// <summary>Searches for a Riot account and upserts its local player record.</summary>
    /// <param name="command">Riot ID and the LoL platform to search.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The local player record for the account.</returns>
    [HttpPost("search")]
    [Consumes("application/json")]
    [Produces("application/json", "application/problem+json")]
    [ProducesResponseType(typeof(PlayerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest,
        "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound,
        "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests,
        "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable,
        "application/problem+json")]
    public async Task<ActionResult<PlayerDto>> Search(SearchPlayerCommand command, CancellationToken cancellationToken)
        => Ok(await handler.SearchAsync(command, cancellationToken));
}
