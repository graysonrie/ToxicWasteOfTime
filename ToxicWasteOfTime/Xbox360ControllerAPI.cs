using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace ToxicWasteOfTime;

/// <summary>
/// Intuitive API wrapper for Xbox 360 controller input execution.
/// Supports sequential and parallel input sequences with automatic detection.
/// </summary>
public class Xbox360ControllerAPI {
    private readonly IXbox360Controller _controller;

    public Xbox360ControllerAPI(IXbox360Controller controller) {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }

    /// <summary>
    /// Gets the underlying controller instance (for internal use).
    /// </summary>
    internal IXbox360Controller GetController() => _controller;

    /// <summary>
    /// Creates a new action group for recording and executing controller inputs.
    /// </summary>
    public ActionGroup RecordActions() {
        return new ActionGroup(_controller);
    }

    /// <summary>
    /// Creates a new live action group for executing controller inputs in real-time.
    /// Actions execute immediately when called.
    /// </summary>
    public LiveActionGroup LiveActions() {
        return new LiveActionGroup(_controller);
    }

    /// <summary>
    /// Converts a normalized stick value (-1 to 1) to a short value (-32768 to 32767).
    /// </summary>
    internal static short NormalizeStickValue(double value) {
        // Clamp value to -1 to 1 range
        value = Math.Max(-1.0, Math.Min(1.0, value));
        // Convert to short range
        return (short)(value * 32767);
    }
}

/// <summary>
/// Action group for recording and executing controller inputs.
/// Actions are scheduled by timestep, allowing precise timing and overlap.
/// </summary>
public class ActionGroup {
    private readonly IXbox360Controller _controller;
    private readonly List<ScheduledAction> _scheduledActions = new();
    private static readonly SemaphoreSlim _submitLock = new SemaphoreSlim(1, 1);
    private int _currentTimestep = 0;

    internal ActionGroup(IXbox360Controller controller) {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }

    /// <summary>
    /// Sets the timestep for subsequent actions. Actions will be scheduled to start at this timestep.
    /// </summary>
    public ActionGroup SetTimestep(int milliseconds) {
        _currentTimestep = milliseconds;
        return this;
    }

    /// <summary>
    /// Waits for the specified duration. This breaks parallel grouping - actions after a Wait() execute sequentially.
    /// [DEPRECATED: Use SetTimestep instead for better control]
    /// </summary>
    public ActionGroup Wait(int milliseconds) {
        _currentTimestep += milliseconds;
        return this;
    }

    /// <summary>
    ///  Wait a very small time
    /// [DEPRECATED: Use SetTimestep instead]
    /// </summary>
    /// <returns></returns>
    public ActionGroup WaitTrivial() {
        return Wait(25);
    }

    /// <summary>
    /// Executes all queued actions based on their scheduled timesteps.
    /// </summary>
    public async Task ExecuteAsync() {
        if (_scheduledActions.Count == 0) return;

        var allTasks = new List<Task>();
        var startTime = System.Diagnostics.Stopwatch.StartNew();

        // Group actions by timestep to handle parallel actions at the same time
        var actionsByTimestep = _scheduledActions
            .GroupBy(a => a.TimestepMs)
            .OrderBy(g => g.Key)
            .ToList();

        foreach (var timestepGroup in actionsByTimestep) {
            var timestep = timestepGroup.Key;
            var actionsAtTimestep = timestepGroup.ToList();

            // Schedule all actions at this timestep
            allTasks.Add(Task.Run(async () => {
                // Wait until the timestep is reached
                var elapsed = startTime.ElapsedMilliseconds;
                var remainingDelay = timestep - elapsed;
                if (remainingDelay > 0) {
                    await Task.Delay((int)remainingDelay);
                }

                // Execute all actions at this timestep (they should execute in parallel)
                // For actions at the same timestep, we need to set all values together
                await _submitLock.WaitAsync();
                try {
                    foreach (var scheduledAction in actionsAtTimestep) {
                        scheduledAction.Action.Action(_controller);
                    }
                    _controller.SubmitReport();
                }
                finally {
                    _submitLock.Release();
                }

                // Schedule releases for each action
                var releaseTasks = actionsAtTimestep.Select(async scheduledAction => {
                    var action = scheduledAction.Action;
                    if (action.DurationMs > 0 && action.ReleaseAction != null) {
                        await Task.Delay(action.DurationMs);
                        await _submitLock.WaitAsync();
                        try {
                            action.ReleaseAction(_controller);
                            _controller.SubmitReport();
                        }
                        finally {
                            _submitLock.Release();
                        }
                    }
                });
                await Task.WhenAll(releaseTasks);
            }));
        }

        // Wait for all actions to complete
        await Task.WhenAll(allTasks);
    }


    /// <summary>
    /// Synchronous version of ExecuteAsync.
    /// </summary>
    public void Execute() {
        ExecuteAsync().GetAwaiter().GetResult();
    }


    private void AddAction(Action<IXbox360Controller> action, int durationMs = 0, Action<IXbox360Controller>? releaseAction = null) {
        _scheduledActions.Add(new ScheduledAction {
            TimestepMs = _currentTimestep,
            Action = new InputAction {
                Type = ActionType.Input,
                Action = action,
                DurationMs = durationMs,
                ReleaseAction = releaseAction
            }
        });
    }

    // Button methods

    public ActionGroup PressA() => HoldA(100);
    public ActionGroup HoldA(int milliseconds) {
        AddAction(pad => {
            pad.SetButtonState(Xbox360Button.A, true);
            pad.SubmitReport();
        }, milliseconds, pad => {
            pad.SetButtonState(Xbox360Button.A, false);
            pad.SubmitReport();
        });
        return this;
    }

    public ActionGroup PressB() => HoldB(100);
    public ActionGroup HoldB(int milliseconds) {
        AddAction(pad => {
            pad.SetButtonState(Xbox360Button.B, true);
            pad.SubmitReport();
        }, milliseconds, pad => {
            pad.SetButtonState(Xbox360Button.B, false);
            pad.SubmitReport();
        });
        return this;
    }

    public ActionGroup PressX() => HoldX(100);
    public ActionGroup HoldX(int milliseconds) {
        AddAction(pad => {
            pad.SetButtonState(Xbox360Button.X, true);
            pad.SubmitReport();
        }, milliseconds, pad => {
            pad.SetButtonState(Xbox360Button.X, false);
            pad.SubmitReport();
        });
        return this;
    }

    public ActionGroup PressY() => HoldY(100);
    public ActionGroup HoldY(int milliseconds) {
        AddAction(pad => {
            pad.SetButtonState(Xbox360Button.Y, true);
            pad.SubmitReport();
        }, milliseconds, pad => {
            pad.SetButtonState(Xbox360Button.Y, false);
            pad.SubmitReport();
        });
        return this;
    }

    public ActionGroup PressLeftShoulder() {
        AddAction(pad => {
            pad.SetButtonState(Xbox360Button.LeftShoulder, true);
            pad.SubmitReport();
        }, 100, pad => {
            pad.SetButtonState(Xbox360Button.LeftShoulder, false);
            pad.SubmitReport();
        });
        return this;
    }

    public ActionGroup PressRightShoulder() {
        AddAction(pad => {
            pad.SetButtonState(Xbox360Button.RightShoulder, true);
            pad.SubmitReport();
        }, 100, pad => {
            pad.SetButtonState(Xbox360Button.RightShoulder, false);
            pad.SubmitReport();
        });
        return this;
    }

    public ActionGroup PressView() {
        AddAction(pad => {
            pad.SetButtonState(Xbox360Button.Back, true);
            pad.SubmitReport();
        }, 100, pad => {
            pad.SetButtonState(Xbox360Button.Back, false);
            pad.SubmitReport();
        });
        return this;
    }

    public ActionGroup PressMenu() {
        AddAction(pad => {
            pad.SetButtonState(Xbox360Button.Start, true);
            pad.SubmitReport();
        }, 100, pad => {
            pad.SetButtonState(Xbox360Button.Start, false);
            pad.SubmitReport();
        });
        return this;
    }

    // D-Pad methods

    public ActionGroup PressDPadUp() {
        AddAction(pad => {
            pad.SetButtonState(Xbox360Button.Up, true);
            pad.SubmitReport();
        }, 100, pad => {
            pad.SetButtonState(Xbox360Button.Up, false);
            pad.SubmitReport();
        });
        return this;
    }

    public ActionGroup PressDPadDown() {
        AddAction(pad => {
            pad.SetButtonState(Xbox360Button.Down, true);
            pad.SubmitReport();
        }, 100, pad => {
            pad.SetButtonState(Xbox360Button.Down, false);
            pad.SubmitReport();
        });
        return this;
    }

    public ActionGroup PressDPadLeft() {
        AddAction(pad => {
            pad.SetButtonState(Xbox360Button.Left, true);
            pad.SubmitReport();
        }, 100, pad => {
            pad.SetButtonState(Xbox360Button.Left, false);
            pad.SubmitReport();
        });
        return this;
    }

    public ActionGroup PressDPadRight() {
        AddAction(pad => {
            pad.SetButtonState(Xbox360Button.Right, true);
            pad.SubmitReport();
        }, 100, pad => {
            pad.SetButtonState(Xbox360Button.Right, false);
            pad.SubmitReport();
        });
        return this;
    }

    // Stick methods (using -1 to 1 range)

    public ActionGroup HoldLeftStick(double x, double y, int milliseconds) {
        var xValue = Xbox360ControllerAPI.NormalizeStickValue(x);
        var yValue = Xbox360ControllerAPI.NormalizeStickValue(y);
        AddAction(pad => {
            pad.SetAxisValue(Xbox360Axis.LeftThumbX, xValue);
            pad.SetAxisValue(Xbox360Axis.LeftThumbY, yValue);
            pad.SubmitReport();
        }, milliseconds, pad => {
            pad.SetAxisValue(Xbox360Axis.LeftThumbX, 0);
            pad.SetAxisValue(Xbox360Axis.LeftThumbY, 0);
            pad.SubmitReport();
        });
        return this;
    }

    public ActionGroup FlickLeftStick(double x, double y) {
        var xValue = Xbox360ControllerAPI.NormalizeStickValue(x);
        var yValue = Xbox360ControllerAPI.NormalizeStickValue(y);
        AddAction(pad => {
            pad.SetAxisValue(Xbox360Axis.LeftThumbX, xValue);
            pad.SetAxisValue(Xbox360Axis.LeftThumbY, yValue);
            pad.SubmitReport();
        }, 50, pad => {
            pad.SetAxisValue(Xbox360Axis.LeftThumbX, 0);
            pad.SetAxisValue(Xbox360Axis.LeftThumbY, 0);
            pad.SubmitReport();
        });
        return this;
    }

    public ActionGroup HoldRightStick(double x, double y, int milliseconds) {
        var xValue = Xbox360ControllerAPI.NormalizeStickValue(x);
        var yValue = Xbox360ControllerAPI.NormalizeStickValue(y);
        AddAction(pad => {
            pad.SetAxisValue(Xbox360Axis.RightThumbX, xValue);
            pad.SetAxisValue(Xbox360Axis.RightThumbY, yValue);
            pad.SubmitReport();
        }, milliseconds, pad => {
            pad.SetAxisValue(Xbox360Axis.RightThumbX, 0);
            pad.SetAxisValue(Xbox360Axis.RightThumbY, 0);
            pad.SubmitReport();
        });
        return this;
    }

    public ActionGroup FlickRightStick(double x, double y) {
        var xValue = Xbox360ControllerAPI.NormalizeStickValue(x);
        var yValue = Xbox360ControllerAPI.NormalizeStickValue(y);
        AddAction(pad => {
            pad.SetAxisValue(Xbox360Axis.RightThumbX, xValue);
            pad.SetAxisValue(Xbox360Axis.RightThumbY, yValue);
            pad.SubmitReport();
        }, 50, pad => {
            pad.SetAxisValue(Xbox360Axis.RightThumbX, 0);
            pad.SetAxisValue(Xbox360Axis.RightThumbY, 0);
            pad.SubmitReport();
        });
        return this;
    }

    // Trigger methods

    public ActionGroup PressLeftTrigger() => HoldLeftTrigger(100);
    public ActionGroup HoldLeftTrigger(int milliseconds) {
        AddAction(pad => {
            pad.SetSliderValue(Xbox360Slider.LeftTrigger, 255);
            pad.SubmitReport();
        }, milliseconds, pad => {
            pad.SetSliderValue(Xbox360Slider.LeftTrigger, 0);
            pad.SubmitReport();
        });
        return this;
    }

    public ActionGroup PressRightTrigger() => HoldRightTrigger(100);
    public ActionGroup HoldRightTrigger(int milliseconds) {
        AddAction(pad => {
            pad.SetSliderValue(Xbox360Slider.RightTrigger, 255);
            pad.SubmitReport();
        }, milliseconds, pad => {
            pad.SetSliderValue(Xbox360Slider.RightTrigger, 0);
            pad.SubmitReport();
        });
        return this;
    }

    private enum ActionType {
        Input,
        Wait
    }

    private class InputAction {
        public ActionType Type { get; set; }
        public required Action<IXbox360Controller> Action { get; set; }
        public int DurationMs { get; set; }
        public Action<IXbox360Controller>? ReleaseAction { get; set; }
    }

    private class ScheduledAction {
        public int TimestepMs { get; set; }
        public required InputAction Action { get; set; }
    }
}
