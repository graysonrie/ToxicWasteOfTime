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
    private CancellationTokenSource? _playbackCancellation;
    private Task? _playbackTask;
    private IXbox360Controller? _currentPlaybackController;
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

    public bool IsPlaybackActive => _playbackTask != null && !_playbackTask.IsCompleted;

    public async Task PlaybackRecordingAsync(string name, IXbox360Controller virtualController)
    {
        // Cancel any existing playback
        if (IsPlaybackActive)
        {
            CancelPlayback();
        }

        var recording = GetRecording(name);
        if (recording == null)
        {
            throw new InvalidOperationException($"Recording '{name}' not found.");
        }

        if (!recording.Events.Any())
        {
            throw new InvalidOperationException($"Recording '{name}' has no events.");
        }

        _playbackCancellation = new CancellationTokenSource();
        _currentPlaybackController = virtualController;
        var cancellationToken = _playbackCancellation.Token;

        // Store the task so we can track it (set synchronously before Task.Run starts)
        // This ensures IsPlaybackActive returns true immediately
        var playbackTask = Task.Run(async () =>
        {
            try
            {
            var events = recording.Events.OrderBy(e => e.TimestampMs).ToList();
            var startTime = Stopwatch.StartNew();
            var eventIndex = 0;

            while (eventIndex < events.Count && !cancellationToken.IsCancellationRequested)
            {
                // Get current time with high precision (using TotalMilliseconds for sub-millisecond precision)
                var currentTimeMs = startTime.Elapsed.TotalMilliseconds;
                var nextEvent = events[eventIndex];
                var targetTimeMs = nextEvent.TimestampMs;

                // Check if we need to wait
                if (currentTimeMs < targetTimeMs)
                {
                    var waitTimeMs = targetTimeMs - currentTimeMs;
                    await PreciseDelayAsync(waitTimeMs, cancellationToken);
                    // Re-check time after delay (may have overshot slightly)
                    currentTimeMs = startTime.Elapsed.TotalMilliseconds;
                }

                // Check cancellation again after delay
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                // Apply all events that should occur at this timestamp (or very close to it)
                // This handles cases where multiple events have the same timestamp
                // We batch them together to apply in a single SubmitReport for better precision
                var eventsToApply = new List<ControllerInputEvent>();

                while (eventIndex < events.Count && !cancellationToken.IsCancellationRequested)
                {
                    var eventTimeMs = events[eventIndex].TimestampMs;

                    // Collect events that are due (within 1ms tolerance to account for timing variations)
                    if (eventTimeMs <= currentTimeMs + 1.0)
                    {
                        eventsToApply.Add(events[eventIndex]);
                        eventIndex++;
                    }
                    else
                    {
                        // Next event is in the future, break to wait
                        break;
                    }
                }

                // Apply all collected events at once (more efficient and precise)
                if (eventsToApply.Count > 0 && !cancellationToken.IsCancellationRequested)
                {
                    ApplyEventsToController(virtualController, eventsToApply);
                }
            }

            // Always zero out inputs when playback ends (whether completed or cancelled)
            if (virtualController != null)
            {
                ZeroOutControllerInputs(virtualController);
            }
        }
        finally
        {
            _playbackCancellation?.Dispose();
            _playbackCancellation = null;
            _currentPlaybackController = null;
            _playbackTask = null;
        }
        }, cancellationToken);

        // Set the task reference immediately so IsPlaybackActive works
        _playbackTask = playbackTask;

        await _playbackTask;
    }

    public void CancelPlayback()
    {
        if (!IsPlaybackActive)
        {
            return;
        }

        _playbackCancellation?.Cancel();

        // Wait for playback to finish (with timeout)
        try
        {
            _playbackTask?.Wait(TimeSpan.FromSeconds(1));
        }
        catch (AggregateException)
        {
            // Task was cancelled, which is expected
        }

        // Zero out inputs immediately
        if (_currentPlaybackController != null)
        {
            ZeroOutControllerInputs(_currentPlaybackController);
        }

        _playbackCancellation?.Dispose();
        _playbackCancellation = null;
        _currentPlaybackController = null;
        _playbackTask = null;
    }

    private void ZeroOutControllerInputs(IXbox360Controller controller)
    {
        // Zero out all sticks
        controller.SetAxisValue(Xbox360Axis.LeftThumbX, 0);
        controller.SetAxisValue(Xbox360Axis.LeftThumbY, 0);
        controller.SetAxisValue(Xbox360Axis.RightThumbX, 0);
        controller.SetAxisValue(Xbox360Axis.RightThumbY, 0);

        // Zero out all triggers
        controller.SetSliderValue(Xbox360Slider.LeftTrigger, 0);
        controller.SetSliderValue(Xbox360Slider.RightTrigger, 0);

        // Release all buttons
        controller.SetButtonState(Xbox360Button.A, false);
        controller.SetButtonState(Xbox360Button.B, false);
        controller.SetButtonState(Xbox360Button.X, false);
        controller.SetButtonState(Xbox360Button.Y, false);
        controller.SetButtonState(Xbox360Button.LeftShoulder, false);
        controller.SetButtonState(Xbox360Button.RightShoulder, false);
        controller.SetButtonState(Xbox360Button.Back, false);
        controller.SetButtonState(Xbox360Button.Start, false);
        controller.SetButtonState(Xbox360Button.Up, false);
        controller.SetButtonState(Xbox360Button.Down, false);
        controller.SetButtonState(Xbox360Button.Left, false);
        controller.SetButtonState(Xbox360Button.Right, false);

        // Submit the report to apply all changes
        controller.SubmitReport();
    }

    private async Task PreciseDelayAsync(double milliseconds, CancellationToken cancellationToken = default)
    {
        if (milliseconds <= 0) return;

        var sw = Stopwatch.StartNew();
        var targetMs = milliseconds;

        while (sw.Elapsed.TotalMilliseconds < targetMs && !cancellationToken.IsCancellationRequested)
        {
            var remaining = targetMs - sw.Elapsed.TotalMilliseconds;

            if (remaining > 10)
            {
                // For longer delays, use async sleep to avoid blocking
                try
                {
                    await Task.Delay((int)(remaining - 5), cancellationToken); // Leave 5ms for busy-wait precision
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
            else if (remaining > 1)
            {
                // For medium delays, use Thread.Sleep with cancellation check
                if (cancellationToken.IsCancellationRequested) break;
                Thread.Sleep((int)remaining);
            }
            else
            {
                // For very short delays (< 1ms), use spin wait for maximum precision
                if (cancellationToken.IsCancellationRequested) break;
                Thread.SpinWait(100);
            }
        }
    }

    private void ApplyEventToController(IXbox360Controller controller, ControllerInputEvent evt)
    {
        ApplyEventsToController(controller, new[] { evt });
    }

    private void ApplyEventsToController(IXbox360Controller controller, IEnumerable<ControllerInputEvent> events)
    {
        // Use the last event's state (most recent) for each input
        // This handles cases where multiple events at the same timestamp might have different states
        ControllerInputEvent? lastEvent = null;
        foreach (var evt in events)
        {
            lastEvent = evt;
        }

        if (lastEvent == null) return;

        // Buttons
        controller.SetButtonState(Xbox360Button.A, lastEvent.ButtonA);
        controller.SetButtonState(Xbox360Button.B, lastEvent.ButtonB);
        controller.SetButtonState(Xbox360Button.X, lastEvent.ButtonX);
        controller.SetButtonState(Xbox360Button.Y, lastEvent.ButtonY);
        controller.SetButtonState(Xbox360Button.LeftShoulder, lastEvent.ButtonLeftShoulder);
        controller.SetButtonState(Xbox360Button.RightShoulder, lastEvent.ButtonRightShoulder);
        controller.SetButtonState(Xbox360Button.Back, lastEvent.ButtonBack);
        controller.SetButtonState(Xbox360Button.Start, lastEvent.ButtonStart);

        // D-Pad
        controller.SetButtonState(Xbox360Button.Up, lastEvent.DPadUp);
        controller.SetButtonState(Xbox360Button.Down, lastEvent.DPadDown);
        controller.SetButtonState(Xbox360Button.Left, lastEvent.DPadLeft);
        controller.SetButtonState(Xbox360Button.Right, lastEvent.DPadRight);

        // Sticks
        controller.SetAxisValue(Xbox360Axis.LeftThumbX, Xbox360ControllerAPI.NormalizeStickValue(lastEvent.LeftStickX));
        controller.SetAxisValue(Xbox360Axis.LeftThumbY, Xbox360ControllerAPI.NormalizeStickValue(lastEvent.LeftStickY));
        controller.SetAxisValue(Xbox360Axis.RightThumbX, Xbox360ControllerAPI.NormalizeStickValue(lastEvent.RightStickX));
        controller.SetAxisValue(Xbox360Axis.RightThumbY, Xbox360ControllerAPI.NormalizeStickValue(lastEvent.RightStickY));

        // Triggers
        controller.SetSliderValue(Xbox360Slider.LeftTrigger, (byte)(lastEvent.LeftTrigger * 255));
        controller.SetSliderValue(Xbox360Slider.RightTrigger, (byte)(lastEvent.RightTrigger * 255));

        // Submit the report once for all events at this timestamp
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

            CancelPlayback();
            _playbackTask?.Wait();

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
