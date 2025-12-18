namespace ToxicWasteOfTime.Models;

public class ControllerRecording
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public virtual ICollection<ControllerInputEvent> Events { get; set; } = new List<ControllerInputEvent>();
}

public class ControllerInputEvent
{
    public int Id { get; set; }
    public int RecordingId { get; set; }
    public virtual ControllerRecording? Recording { get; set; }
    public long TimestampMs { get; set; } // Milliseconds since recording started

    // Button states (true = pressed)
    public bool ButtonA { get; set; }
    public bool ButtonB { get; set; }
    public bool ButtonX { get; set; }
    public bool ButtonY { get; set; }
    public bool ButtonLeftShoulder { get; set; }
    public bool ButtonRightShoulder { get; set; }
    public bool ButtonBack { get; set; }
    public bool ButtonStart { get; set; }
    public bool ButtonLeftThumb { get; set; }
    public bool ButtonRightThumb { get; set; }
    public bool DPadUp { get; set; }
    public bool DPadDown { get; set; }
    public bool DPadLeft { get; set; }
    public bool DPadRight { get; set; }

    // Stick positions (-1.0 to 1.0)
    public double LeftStickX { get; set; }
    public double LeftStickY { get; set; }
    public double RightStickX { get; set; }
    public double RightStickY { get; set; }

    // Triggers (0.0 to 1.0)
    public double LeftTrigger { get; set; }
    public double RightTrigger { get; set; }
}
