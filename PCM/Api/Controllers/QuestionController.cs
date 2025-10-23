using Microsoft.AspNetCore.Mvc;
[ApiController]
[Route("api/question-after-trial")]
public class QuestionAfterTrial : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> PostData()
    {
        try
        {
            await SharedResources.manager.Verbal.LastQuestion();
            return Ok("ok");
        }
        catch
        {
            return BadRequest($"Questioning failed");
        }
    }
}
