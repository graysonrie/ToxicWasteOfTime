using System.Diagnostics;
using SharpDX.XInput;
using ToxicWasteOfTime.Data;
using ToxicWasteOfTime.Models;
using Microsoft.EntityFrameworkCore;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using Nefarius.ViGEm.Client.Targets;
using Microsoft.Extensions.DependencyInjection;

namespace ToxicWasteOfTime.Services;

public class ControllerRecordingService : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private Controller? _xinputController;
    private CancellationTokenSource? _recordingCancellation;
    private Task? _recordingTask;
    private ControllerRecording? _currentRecording;
    private IServiceScope? _recordingScope;
    private RecordingDbContext? _currentDbContext;
    private bool _disposed = false;
    private bool _endedByViewButton = false;

    public ControllerRecordingService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        // Ensure database is created on startup
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RecordingDbContext>();
        dbContext.Database.EnsureCreated();
    }

    private RecordingDbContext GetDbContext()
    {
        // If we're in the middle of a recording, reuse the existing context
        if (_currentDbContext != null)
        {
            return _currentDbContext;
        }

        // Otherwise, create a new scope for this operation
        var scope = _scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<RecordingDbContext>();
    }

    private Controller GetXInputController()
    {
        // Check for connected controller (check on each use, not just construction)
        var controllers = new[] { new Controller(UserIndex.One), new Controller(UserIndex.Two),
                                  new Controller(UserIndex.Three), new Controller(UserIndex.Four) };
        var connected = controllers.FirstOrDefault(c => c.IsConnected);

        if (connected == null)
        {
            throw new InvalidOperationException("No Xbox controller found. Please connect a controller.");
        }

        return connected;
    }

    public bool IsRecording => _recordingTask != null && !_recordingTask.IsCompleted;

    public void StartRecording(string name, string? description = null)
    {
        if (IsRecording)
        {
            throw new InvalidOperationException("Already recording. Call EndRecording() first.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Recording name cannot be empty.", nameof(name));
        }

        // Check if controller is connected
        try
        {
            GetXInputController();
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException("No Xbox controller found. Please connect a controller before recording.");
        }

        // Create a scope for this recording session
        _recordingScope = _scopeFactory.CreateScope();
        _currentDbContext = _recordingScope.ServiceProvider.GetRequiredService<RecordingDbContext>();

        // Check if recording with this name already exists
        if (_currentDbContext.Recordings.Any(r => r.Name == name))
        {
            _recordingScope.Dispose();
            _recordingScope = null;
            _currentDbContext = null;
            throw new InvalidOperationException($"Recording with name '{name}' already exists.");
        }

        _currentRecording = new ControllerRecording
        {
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        _currentDbContext.Recordings.Add(_currentRecording);
        _currentDbContext.SaveChanges();

        _endedByViewButton = false;
        _recordingCancellation = new CancellationTokenSource();
        _recordingTask = Task.Run(() => RecordControllerInput(_recordingCancellation.Token));
    }

    public ControllerRecording EndRecording()
    {
        if (!IsRecording || _currentRecording == null)
        {
            throw new InvalidOperationException("Not currently recording.");
        }

        _recordingCancellation?.Cancel();
        _recordingTask?.Wait();
        _recordingTask = null;
        _recordingCancellation?.Dispose();
        _recordingCancellation = null;

        var recording = _currentRecording;
        _currentRecording = null;

        if (_currentDbContext != null)
        {
            _currentDbContext.SaveChanges();
            _currentDbContext = null;
        }

        _recordingScope?.Dispose();
        _recordingScope = null;

        return recording;
    }

    private void EndRecordingFromViewButton()
    {
        if (!IsRecording || _currentRecording == null)
        {
            return;
        }

        Console.WriteLine($"[Recording] View button pressed - automatically ending recording '{_currentRecording.Name}'");
        _endedByViewButton = true;

        // Cancel the recording task (this will cause the loop to exit)
        _recordingCancellation?.Cancel();

        // Note: We don't wait here because we're being called from within the recording loop
        // The recording will complete and save when the loop exits
    }

    private void RecordControllerInput(CancellationToken cancellationToken)
    {
        if (_currentRecording == null) return;

        var controller = GetXInputController();
        var stopwatch = Stopwatch.StartNew();
        var lastState = new State();
        bool lastViewButtonState = false;
        const int pollIntervalMs = 16; // ~60fps polling rate

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (controller.GetState(out var currentState))
                {
                    // Check for View button press (Back button) to end recording
                    var currentViewButtonState = (currentState.Gamepad.Buttons & GamepadButtonFlags.Back) != 0;
                    if (currentViewButtonState && !lastViewButtonState)
                    {
                        // View button was just pressed - end recording
                        EndRecordingFromViewButton();
                        // The cancellation token has been canceled, loop will exit on next iteration
                        break;
                    }
                    lastViewButtonState = currentViewButtonState;

                    // Only record if state changed (to reduce database size)
                    if (HasStateChanged(lastState, currentState))
                    {
                        var timestamp = stopwatch.ElapsedMilliseconds;
                        var inputEvent = ConvertStateToEvent(currentState, timestamp);
                        inputEvent.RecordingId = _currentRecording.Id;

                        _currentDbContext?.InputEvents.Add(inputEvent);
                        _currentDbContext?.SaveChanges(); // Save immediately for safety

                        lastState = currentState;
                    }
                }

                Thread.Sleep(pollIntervalMs);
            }
        }
        finally
        {
            // Ensure recording is saved when loop exits
            if (_currentRecording != null && _currentDbContext != null)
            {
                _currentDbContext.SaveChanges();

                // Log if this was triggered by View button
                if (_endedByViewButton)
                {
                    var eventCount = _currentRecording.Events.Count;
                    var duration = eventCount > 0 ? _currentRecording.Events.Max(e => e.TimestampMs) / 1000.0 : 0;
                    Console.WriteLine($"[Recording] Recording '{_currentRecording.Name}' saved successfully");
                    Console.WriteLine($"[Recording]   Events: {eventCount}");
                    Console.WriteLine($"[Recording]   Duration: {duration:F2} seconds");
                }
            }
        }
    }

    private bool HasStateChanged(State oldState, State newState)
    {
        var oldGamepad = oldState.Gamepad;
        var newGamepad = newState.Gamepad;

        return oldGamepad.Buttons != newGamepad.Buttons ||
               oldGamepad.LeftThumbX != newGamepad.LeftThumbX ||
               oldGamepad.LeftThumbY != newGamepad.LeftThumbY ||
               oldGamepad.RightThumbX != newGamepad.RightThumbX ||
               oldGamepad.RightThumbY != newGamepad.RightThumbY ||
               oldGamepad.LeftTrigger != newGamepad.LeftTrigger ||
               oldGamepad.RightTrigger != newGamepad.RightTrigger;
    }

    private ControllerInputEvent ConvertStateToEvent(State state, long timestampMs)
    {
        var gamepad = state.Gamepad;
        var buttons = gamepad.Buttons;

        return new ControllerInputEvent
        {
            TimestampMs = timestampMs,
            ButtonA = (buttons & GamepadButtonFlags.A) != 0,
            ButtonB = (buttons & GamepadButtonFlags.B) != 0,
            ButtonX = (buttons & GamepadButtonFlags.X) != 0,
            ButtonY = (buttons & GamepadButtonFlags.Y) != 0,
            ButtonLeftShoulder = (buttons & GamepadButtonFlags.LeftShoulder) != 0,
            ButtonRightShoulder = (buttons & GamepadButtonFlags.RightShoulder) != 0,
            ButtonBack = (buttons & GamepadButtonFlags.Back) != 0,
            ButtonStart = (buttons & GamepadButtonFlags.Start) != 0,
            ButtonLeftThumb = (buttons & GamepadButtonFlags.LeftThumb) != 0,
            ButtonRightThumb = (buttons & GamepadButtonFlags.RightThumb) != 0,
            DPadUp = (buttons & GamepadButtonFlags.DPadUp) != 0,
            DPadDown = (buttons & GamepadButtonFlags.DPadDown) != 0,
            DPadLeft = (buttons & GamepadButtonFlags.DPadLeft) != 0,
            DPadRight = (buttons & GamepadButtonFlags.DPadRight) != 0,
            LeftStickX = NormalizeStickValue(gamepad.LeftThumbX),
            LeftStickY = NormalizeStickValue(gamepad.LeftThumbY),
            RightStickX = NormalizeStickValue(gamepad.RightThumbX),
            RightStickY = NormalizeStickValue(gamepad.RightThumbY),
            LeftTrigger = gamepad.LeftTrigger / 255.0,
            RightTrigger = gamepad.RightTrigger / 255.0
        };
    }

    private static double NormalizeStickValue(short value)
    {
        // XInput sticks are -32768 to 32767
        return Math.Max(-1.0, Math.Min(1.0, value / 32767.0));
    }

    public List<RecordingInfo> ListRecordings()
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RecordingDbContext>();

        return dbContext.Recordings
            .Include(r => r.Events)
            .Select(r => new RecordingInfo
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                CreatedAt = r.CreatedAt,
                EventCount = r.Events.Count,
                DurationMs = r.Events.Any() ? r.Events.Max(e => e.TimestampMs) : 0
            })
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
    }

    public ControllerRecording? GetRecording(string name)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RecordingDbContext>();

        return dbContext.Recordings
            .Include(r => r.Events.OrderBy(e => e.TimestampMs))
            .FirstOrDefault(r => r.Name == name);
    }

    public bool DeleteRecording(string name)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RecordingDbContext>();

        var recording = dbContext.Recordings
            .FirstOrDefault(r => r.Name == name);

        if (recording == null)
        {
            return false;
        }

        // Delete the recording (events will be cascade deleted)
        dbContext.Recordings.Remove(recording);
        dbContext.SaveChanges();

        return true;
    }

    public async Task PlaybackRecordingAsync(string name, IXbox360Controller virtualController)
    {
        var recording = GetRecording(name);
        if (recording == null)
        {
            throw new InvalidOperationException($"Recording '{name}' not found.");
        }

        if (!recording.Events.Any())
        {
            throw new InvalidOperationException($"Recording '{name}' has no events.");
        }

        var events = recording.Events.OrderBy(e => e.TimestampMs).ToList();
        var startTime = Stopwatch.StartNew();
        var eventIndex = 0;

        while (eventIndex < events.Count)
        {
            var currentTime = startTime.ElapsedMilliseconds;
            var nextEvent = events[eventIndex];

            if (currentTime >= nextEvent.TimestampMs)
            {
                // Apply this event to the virtual controller
                ApplyEventToController(virtualController, nextEvent);
                eventIndex++;
            }
            else
            {
                // Wait until it's time for the next event
                var waitTime = nextEvent.TimestampMs - currentTime;
                if (waitTime > 0)
                {
                    await Task.Delay((int)waitTime);
                }
            }
        }
    }

    private void ApplyEventToController(IXbox360Controller controller, ControllerInputEvent evt)
    {
        // Buttons
        controller.SetButtonState(Xbox360Button.A, evt.ButtonA);
        controller.SetButtonState(Xbox360Button.B, evt.ButtonB);
        controller.SetButtonState(Xbox360Button.X, evt.ButtonX);
        controller.SetButtonState(Xbox360Button.Y, evt.ButtonY);
        controller.SetButtonState(Xbox360Button.LeftShoulder, evt.ButtonLeftShoulder);
        controller.SetButtonState(Xbox360Button.RightShoulder, evt.ButtonRightShoulder);
        controller.SetButtonState(Xbox360Button.Back, evt.ButtonBack);
        controller.SetButtonState(Xbox360Button.Start, evt.ButtonStart);

        // D-Pad
        controller.SetButtonState(Xbox360Button.Up, evt.DPadUp);
        controller.SetButtonState(Xbox360Button.Down, evt.DPadDown);
        controller.SetButtonState(Xbox360Button.Left, evt.DPadLeft);
        controller.SetButtonState(Xbox360Button.Right, evt.DPadRight);

        // Sticks
        controller.SetAxisValue(Xbox360Axis.LeftThumbX, Xbox360ControllerAPI.NormalizeStickValue(evt.LeftStickX));
        controller.SetAxisValue(Xbox360Axis.LeftThumbY, Xbox360ControllerAPI.NormalizeStickValue(evt.LeftStickY));
        controller.SetAxisValue(Xbox360Axis.RightThumbX, Xbox360ControllerAPI.NormalizeStickValue(evt.RightStickX));
        controller.SetAxisValue(Xbox360Axis.RightThumbY, Xbox360ControllerAPI.NormalizeStickValue(evt.RightStickY));

        // Triggers
        controller.SetSliderValue(Xbox360Slider.LeftTrigger, (byte)(evt.LeftTrigger * 255));
        controller.SetSliderValue(Xbox360Slider.RightTrigger, (byte)(evt.RightTrigger * 255));

        // Submit the report
        controller.SubmitReport();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _recordingCancellation?.Cancel();
            _recordingTask?.Wait();
            _recordingCancellation?.Dispose();
            _recordingScope?.Dispose();
            _disposed = true;
        }
    }
}

public class RecordingInfo
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public int EventCount { get; set; }
    public long DurationMs { get; set; }
}
