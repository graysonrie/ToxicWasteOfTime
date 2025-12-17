using Microsoft.AspNetCore.Mvc;
using ToxicWasteOfTime.Services;

namespace ToxicWasteOfTime.Controllers;

[ApiController]
[Route("api/xbox")]
public class XboxControllerController : ControllerBase
{
    private readonly XboxControllerService _service;

    public XboxControllerController(XboxControllerService service)
    {
        _service = service;
    }

    [HttpPost("actions/execute")]
    public async Task<IActionResult> ExecuteActions([FromBody] ActionRequest request)
    {
        try
        {
            var api = _service.GetAPI();
            var actionGroup = api.RecordActions();

            // Build actions from request
            BuildActions(actionGroup, request.Actions);

            // Execute
            await actionGroup.ExecuteAsync();

            return Ok(new { Success = true, Message = "Actions executed successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Message = ex.Message });
        }
    }

    private void BuildActions(ActionGroup group, List<ActionItem> actions)
    {
        foreach (var action in actions)
        {
            switch (action.Type.ToLower())
            {
                case "wait":
                    if (action.Milliseconds.HasValue)
                        group.Wait(action.Milliseconds.Value);
                    else
                        group.WaitTrivial();
                    break;
                case "pressa":
                    if (action.Milliseconds.HasValue)
                        group.HoldA(action.Milliseconds.Value);
                    else
                        group.PressA();
                    break;
                case "pressb":
                    if (action.Milliseconds.HasValue)
                        group.HoldB(action.Milliseconds.Value);
                    else
                        group.PressB();
                    break;
                case "pressx":
                    if (action.Milliseconds.HasValue)
                        group.HoldX(action.Milliseconds.Value);
                    else
                        group.PressX();
                    break;
                case "pressy":
                    if (action.Milliseconds.HasValue)
                        group.HoldY(action.Milliseconds.Value);
                    else
                        group.PressY();
                    break;
                case "pressleftshoulder":
                    group.PressLeftShoulder();
                    break;
                case "pressrightshoulder":
                    group.PressRightShoulder();
                    break;
                case "pressview":
                    group.PressView();
                    break;
                case "pressmenu":
                    group.PressMenu();
                    break;
                case "pressdpadup":
                    group.PressDPadUp();
                    break;
                case "pressdpaddown":
                    group.PressDPadDown();
                    break;
                case "pressdpadleft":
                    group.PressDPadLeft();
                    break;
                case "pressdpadright":
                    group.PressDPadRight();
                    break;
                case "holdleftstick":
                    if (action.X.HasValue && action.Y.HasValue && action.Milliseconds.HasValue)
                        group.HoldLeftStick(action.X.Value, action.Y.Value, action.Milliseconds.Value);
                    break;
                case "flickleftstick":
                    if (action.X.HasValue && action.Y.HasValue)
                        group.FlickLeftStick(action.X.Value, action.Y.Value);
                    break;
                case "holdrightstick":
                    if (action.X.HasValue && action.Y.HasValue && action.Milliseconds.HasValue)
                        group.HoldRightStick(action.X.Value, action.Y.Value, action.Milliseconds.Value);
                    break;
                case "flickrightstick":
                    if (action.X.HasValue && action.Y.HasValue)
                        group.FlickRightStick(action.X.Value, action.Y.Value);
                    break;
                case "presslefttrigger":
                    if (action.Milliseconds.HasValue)
                        group.HoldLeftTrigger(action.Milliseconds.Value);
                    else
                        group.PressLeftTrigger();
                    break;
                case "pressrighttrigger":
                    if (action.Milliseconds.HasValue)
                        group.HoldRightTrigger(action.Milliseconds.Value);
                    else
                        group.PressRightTrigger();
                    break;
            }
        }
    }
}

public class ActionRequest
{
    public required List<ActionItem> Actions { get; set; }
}

public class ActionItem
{
    public required string Type { get; set; }
    public double? X { get; set; }
    public double? Y { get; set; }
    public int? Milliseconds { get; set; }
}
