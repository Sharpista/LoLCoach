using LoLCoach.Api.Application;
using Microsoft.AspNetCore.Mvc;

namespace LoLCoach.Api.Controllers;

[ApiController]
[Route("api/players")]
public sealed class PlayersController(SearchPlayerHandler searchPlayerHandler, SyncPlayerMatchesHandler syncPlayerMatchesHandler) : ControllerBase
{
    /// <summary>Searches for a Riot account and upserts its local player record.</summary>
    /// <param name="command">Riot ID and the LoL platform to search.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The local player record for the account.</returns>
    [HttpPost("search")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(PlayerDto), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest,
        "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound,
        "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests,
        "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable,
        "application/problem+json")]
    public async Task<ActionResult<PlayerDto>> Search(SearchPlayerCommand command, CancellationToken cancellationToken)
        => Ok(await searchPlayerHandler.SearchAsync(command, cancellationToken));

    /// <summary>Imports up to 20 recent Riot matches for a persisted player.</summary>
    /// <param name="id">Local player identifier.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>Import counters for new, skipped and failed matches.</returns>
    [HttpPost("{id:guid}/matches/sync")]
    [ProducesResponseType(typeof(SyncPlayerMatchesResult), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound,
        "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests,
        "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable,
        "application/problem+json")]
    public async Task<ActionResult<SyncPlayerMatchesResult>> SyncMatches(Guid id, CancellationToken cancellationToken)
        => Ok(await syncPlayerMatchesHandler.SyncAsync(new SyncPlayerMatchesCommand(id), cancellationToken));
}
