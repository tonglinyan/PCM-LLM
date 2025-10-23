using Microsoft.AspNetCore.Mvc;
[ApiController]
[Route("api/stop")]
public class StopController : ControllerBase
{
    [HttpPost]
    public IActionResult PostData()
    {
        try
        {
            SharedResources.manager.StopPCM();
            return Ok("ok");
        }
        catch
        {
            // En cas d'erreur, renvoyez une r�ponse d'erreur
            return BadRequest($"PCM is not running");
        }
    }
}
