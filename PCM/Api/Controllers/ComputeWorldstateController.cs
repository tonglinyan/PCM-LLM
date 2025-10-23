using Microsoft.AspNetCore.Mvc;
using static PCM.Core.Interfacing;
using flexop.Api.Dtos;
using flexop.Api.Mappers;
using Newtonsoft.Json;
[ApiController]
[Route("api/compute-worldstate")]
public class ComputeWorldstateController : ControllerBase
{
    [HttpPost]
    public IActionResult PostData([FromBody] WorldStateDto inputWorldstate)
    {
        try
        {
            InputMapper mapper = new();
            Input input = mapper.Map(inputWorldstate);
            //string center0 = JsonConvert.SerializeObject(inputWorldstate.Agents[0].Body.Center);
            //string orientation0 = JsonConvert.SerializeObject(inputWorldstate.Agents[0].Body.Orientation);
            //string center1 = JsonConvert.SerializeObject(inputWorldstate.Agents[1].Body.Center);
            //string orientation1 = JsonConvert.SerializeObject(inputWorldstate.Agents[1].Body.Orientation);
            Console.WriteLine("------------------ Worldstate reçu : ------------------");
            Console.WriteLine(JsonConvert.SerializeObject(input));
            //Console.WriteLine("Agent 1 :");
            //Console.WriteLine("center : " + center0);
            //Console.WriteLine("orientation : " + orientation0);
            //Console.WriteLine("Agent 2 :"); 
            //Console.WriteLine("center : " + center1);
            //Console.WriteLine("orientation : " + orientation1);
            Console.WriteLine("-------------------------------------------------------");
            //Console.Write(input.TimeStamp);
            string jsonString = SharedResources.manager.ComputeWorldstate(input);
            //Console.WriteLine("Output: " + jsonString);
            return Ok(jsonString); //PCM_OUTPUT
        }
        catch (Exception ex)
        {
            // En cas d'erreur, renvoyez une réponse d'erreur
            return BadRequest($"Erreur lors du traitement des donn�es : {ex.Message}");
        }
    }
}
