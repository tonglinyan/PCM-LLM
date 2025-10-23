using Microsoft.AspNetCore.Mvc;
using Schemas = PCM.Schemas;
[ApiController]
[Route("api/initialize-parameters")]
public class InitializeParametersController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> PostData([FromBody] Schemas.Parameters _params, [FromQuery] Schemas.ArtificialAgentMode AA_Mode)
    {
        try
        {
            await SharedResources.manager.Init(_params, AA_Mode);
            return Ok("ok");
        }
        catch (Exception ex)
        {
            // En cas d'erreur, renvoyez une r�ponse d'erreur
            return BadRequest($"Erreur lors du traitement des donn�es : {ex.Message}");
        }
    }
}
