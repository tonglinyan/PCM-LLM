using Microsoft.AspNetCore.Mvc;
using PCM.Schemas;
[ApiController]
[Route("api/speak")]

public class Speak : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> PostData([FromBody] TextImageData data, [FromQuery] int agentId)
    {
        try
        {
            Console.WriteLine(agentId);
            if (agentId == 0)
                return Ok(await SharedResources.manager.Verbal.ArtificialAgentAnswers(data));
            return Ok(await SharedResources.manager.Verbal.ParticipantAsks(data));
        }
        catch
        {
            // En cas d'erreur, renvoyez une r�ponse d'erreur
            return BadRequest($"PCM is not running");
        }
    }
}
