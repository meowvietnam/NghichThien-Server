using Microsoft.AspNetCore.Mvc;

namespace MTS.Controllers;

public class Controller : ControllerBase
{
    protected bool TryGetId(out int id)
    {
        var idString = User.FindFirst("charId")?.Value;
        return int.TryParse(idString, out id);
    }
    protected IActionResult Success(object data)
    {
        return Ok(new
        {
            Success = true,
            Data = data
        });
    }
    protected IActionResult Fail(string message)
    {
        return BadRequest(new
        {
            Success = false, 
            Message = message
        });
    }
}