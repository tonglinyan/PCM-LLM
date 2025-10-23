using Microsoft.AspNetCore.Mvc;
[ApiController]
[Route("api/participant-chooses")]
public class ParticipantChooses : ControllerBase
{
    [HttpPost]
    public IActionResult PostData()
    {
        try
        {
            SharedResources.manager.ParticipantChooses();
            return Ok("ok");
        }
        catch
        {
            // En cas d'erreur, renvoyez une r�ponse d'erreur
            return BadRequest($"PCM is not running");
        }
    }
}
