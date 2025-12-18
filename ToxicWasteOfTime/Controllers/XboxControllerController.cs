using Microsoft.AspNetCore.Mvc;
using ToxicWasteOfTime.Services;

namespace ToxicWasteOfTime.Controllers;

[ApiController]
[Route("api/xbox")]
public class XboxControllerController : ControllerBase
{
    private readonly XboxControllerService _service;
    private readonly ControllerRecordingService _recordingService;

    public XboxControllerController(XboxControllerService service, ControllerRecordingService recordingService)
    {
        _service = service;
        _recordingService = recordingService;
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

    [HttpPost("live/action")]
    public async Task<IActionResult> ExecuteLiveAction([FromBody] LiveActionItem request)
    {
        try
        {
            var api = _service.GetAPI();
            var liveGroup = api.LiveActions();

            // Execute the live action immediately
            BuildLiveAction(liveGroup, request);

            return Ok(new { Success = true, Message = "Live action executed" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Message = ex.Message });
        }
    }

    private void BuildLiveAction(LiveActionGroup group, LiveActionItem action)
    {
        switch (action.Type.ToLower())
        {
            case "pressa":
                group.PressA();
                break;
            case "holda":
                group.HoldA();
                break;
            case "cancelholda":
                group.CancelHoldA();
                break;
            case "pressb":
                group.PressB();
                break;
            case "holdb":
                group.HoldB();
                break;
            case "cancelholdb":
                group.CancelHoldB();
                break;
            case "pressx":
                group.PressX();
                break;
            case "holdx":
                group.HoldX();
                break;
            case "cancelholdx":
                group.CancelHoldX();
                break;
            case "pressy":
                group.PressY();
                break;
            case "holdy":
                group.HoldY();
                break;
            case "cancelholdy":
                group.CancelHoldY();
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
                if (action.X.HasValue && action.Y.HasValue)
                    group.HoldLeftStick(action.X.Value, action.Y.Value);
                break;
            case "cancelleftstick":
                group.CancelLeftStick();
                break;
            case "flickleftstick":
                if (action.X.HasValue && action.Y.HasValue)
                    group.FlickLeftStick(action.X.Value, action.Y.Value);
                break;
            case "holdrightstick":
                if (action.X.HasValue && action.Y.HasValue)
                    group.HoldRightStick(action.X.Value, action.Y.Value);
                break;
            case "cancelrightstick":
                group.CancelRightStick();
                break;
            case "flickrightstick":
                if (action.X.HasValue && action.Y.HasValue)
                    group.FlickRightStick(action.X.Value, action.Y.Value);
                break;
            case "presslefttrigger":
                group.PressLeftTrigger();
                break;
            case "holdlefttrigger":
                group.HoldLeftTrigger();
                break;
            case "cancelholdlefttrigger":
                group.CancelHoldLeftTrigger();
                break;
            case "pressrighttrigger":
                group.PressRightTrigger();
                break;
            case "holdrighttrigger":
                group.HoldRightTrigger();
                break;
            case "cancelholdrighttrigger":
                group.CancelHoldRightTrigger();
                break;
            case "complete":
                group.Complete();
                break;
        }
    }

    private void BuildActions(ActionGroup group, List<ActionItem> actions)
    {
        foreach (var action in actions)
        {
            // Set timestep if provided (for regular actions, this comes from the TimestepMs field)
            if (action.TimestepMs.HasValue)
            {
                group.SetTimestep(action.TimestepMs.Value);
            }

            switch (action.Type.ToLower())
            {
                case "settimestep":
                    // Timestep already set above
                    break;
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
                    Console.WriteLine("Pressing Right Shoulder");
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

    [HttpPost("recording/start")]
    public IActionResult StartRecording([FromBody] StartRecordingRequest request)
    {
        try
        {
            _recordingService.StartRecording(request.Name, request.Description);
            return Ok(new { Success = true, Message = $"Recording '{request.Name}' started" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Message = ex.Message });
        }
    }

    [HttpPost("recording/end")]
    public IActionResult EndRecording()
    {
        try
        {
            var recording = _recordingService.EndRecording();
            return Ok(new {
                Success = true,
                Message = $"Recording '{recording.Name}' ended",
                Recording = new {
                    Id = recording.Id,
                    Name = recording.Name,
                    Description = recording.Description,
                    CreatedAt = recording.CreatedAt,
                    EventCount = recording.Events.Count,
                    DurationMs = recording.Events.Any() ? recording.Events.Max(e => e.TimestampMs) : 0
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Message = ex.Message });
        }
    }

    [HttpGet("recording/list")]
    public IActionResult ListRecordings()
    {
        try
        {
            var recordings = _recordingService.ListRecordings();
            return Ok(new { Success = true, Recordings = recordings });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Message = ex.Message });
        }
    }

    [HttpPost("recording/playback/{name}")]
    public async Task<IActionResult> PlaybackRecording(string name)
    {
        try
        {
            var virtualController = _service.GetController();

            await _recordingService.PlaybackRecordingAsync(name, virtualController);

            return Ok(new { Success = true, Message = $"Recording '{name}' playback completed" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Message = ex.Message });
        }
    }

    [HttpDelete("recording/{name}")]
    public IActionResult DeleteRecording(string name)
    {
        try
        {
            var deleted = _recordingService.DeleteRecording(name);

            if (deleted)
            {
                return Ok(new { Success = true, Message = $"Recording '{name}' deleted successfully" });
            }
            else
            {
                return NotFound(new { Success = false, Message = $"Recording '{name}' not found" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Success = false, Message = ex.Message });
        }
    }
}

public class StartRecordingRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
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
    public int? TimestepMs { get; set; }
}

public class LiveActionItem
{
    public required string Type { get; set; }
    public double? X { get; set; }
    public double? Y { get; set; }
    public int? Milliseconds { get; set; }
}
