using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace ToxicWasteOfTime;

/// <summary>
/// Live action group for executing controller inputs in real-time.
/// Actions execute immediately when called, without needing Execute().
/// </summary>
public class LiveActionGroup {
    private readonly IXbox360Controller _controller;
    private static readonly SemaphoreSlim _submitLock = new SemaphoreSlim(1, 1);

    internal LiveActionGroup(IXbox360Controller controller) {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }

    /// <summary>
    /// Signals that all live actions are complete and zeros out all inputs.
    /// This resets all buttons, sticks, and triggers to their neutral state.
    /// </summary>
    public void Complete() {
        ExecuteImmediate(pad => {
            // Zero out all sticks
            pad.SetAxisValue(Xbox360Axis.LeftThumbX, 0);
            pad.SetAxisValue(Xbox360Axis.LeftThumbY, 0);
            pad.SetAxisValue(Xbox360Axis.RightThumbX, 0);
            pad.SetAxisValue(Xbox360Axis.RightThumbY, 0);

            // Zero out all triggers
            pad.SetSliderValue(Xbox360Slider.LeftTrigger, 0);
            pad.SetSliderValue(Xbox360Slider.RightTrigger, 0);

            // Release all buttons
            pad.SetButtonState(Xbox360Button.A, false);
            pad.SetButtonState(Xbox360Button.B, false);
            pad.SetButtonState(Xbox360Button.X, false);
            pad.SetButtonState(Xbox360Button.Y, false);
            pad.SetButtonState(Xbox360Button.LeftShoulder, false);
            pad.SetButtonState(Xbox360Button.RightShoulder, false);
            pad.SetButtonState(Xbox360Button.Back, false);
            pad.SetButtonState(Xbox360Button.Start, false);
            pad.SetButtonState(Xbox360Button.Up, false);
            pad.SetButtonState(Xbox360Button.Down, false);
            pad.SetButtonState(Xbox360Button.Left, false);
            pad.SetButtonState(Xbox360Button.Right, false);
        });
    }

    private async Task ExecuteImmediateAsync(Action<IXbox360Controller> action) {
        await _submitLock.WaitAsync();
        try {
            action(_controller);
            _controller.SubmitReport();
        }
        finally {
            _submitLock.Release();
        }
    }

    /// <summary>
    /// Synchronous version for immediate execution with guaranteed completion.
    /// </summary>
    private void ExecuteImmediate(Action<IXbox360Controller> action) {
        _submitLock.Wait();
        try {
            action(_controller);
            _controller.SubmitReport();
        }
        finally {
            _submitLock.Release();
        }
    }

    /// <summary>
    /// High-precision delay using Stopwatch for accurate timing.
    /// </summary>
    private void PreciseDelay(int milliseconds) {
        if (milliseconds <= 0) return;

        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < milliseconds) {
            // For very short delays, use Thread.Sleep(1) to avoid busy-waiting
            // For longer delays, use Thread.Sleep with remaining time
            var remaining = milliseconds - (int)sw.ElapsedMilliseconds;
            if (remaining > 1) {
                Thread.Sleep(Math.Min(remaining - 1, 10));
            } else {
                Thread.SpinWait(1000); // Very short delay, use spin wait
            }
        }
    }

    // Button methods

    public LiveActionGroup PressA() {
        // Execute synchronously to ensure completion before returning
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.A, true);
        });
        PreciseDelay(100);
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.A, false);
        });
        return this;
    }
    public LiveActionGroup HoldA() {
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.A, true);
        });
        return this;
    }
    public LiveActionGroup CancelHoldA() {
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.A, false);
        });
        return this;
    }

    public LiveActionGroup PressB() {
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.B, true);
        });
        PreciseDelay(100);
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.B, false);
        });
        return this;
    }
    public LiveActionGroup HoldB() {
        ExecuteImmediateAsync(pad => {
            pad.SetButtonState(Xbox360Button.B, true);
        }).Wait();
        return this;
    }
    public LiveActionGroup CancelHoldB() {
        ExecuteImmediateAsync(pad => {
            pad.SetButtonState(Xbox360Button.B, false);
        }).Wait();
        return this;
    }

    public LiveActionGroup PressX() {
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.X, true);
        });
        PreciseDelay(100);
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.X, false);
        });
        return this;
    }
    public LiveActionGroup HoldX() {
        ExecuteImmediateAsync(pad => {
            pad.SetButtonState(Xbox360Button.X, true);
        }).Wait();
        return this;
    }
    public LiveActionGroup CancelHoldX() {
        ExecuteImmediateAsync(pad => {
            pad.SetButtonState(Xbox360Button.X, false);
        }).Wait();
        return this;
    }

    public LiveActionGroup PressY() {
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.Y, true);
        });
        PreciseDelay(100);
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.Y, false);
        });
        return this;
    }
    public LiveActionGroup HoldY() {
        ExecuteImmediateAsync(pad => {
            pad.SetButtonState(Xbox360Button.Y, true);
        }).Wait();
        return this;
    }
    public LiveActionGroup CancelHoldY() {
        ExecuteImmediateAsync(pad => {
            pad.SetButtonState(Xbox360Button.Y, false);
        }).Wait();
        return this;
    }

    public LiveActionGroup PressLeftShoulder() {
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.LeftShoulder, true);
        });
        PreciseDelay(100);
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.LeftShoulder, false);
        });
        return this;
    }

    public LiveActionGroup PressRightShoulder() {
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.RightShoulder, true);
        });
        PreciseDelay(100);
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.RightShoulder, false);
        });
        return this;
    }

    public LiveActionGroup PressView() {
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.Back, true);
        });
        PreciseDelay(100);
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.Back, false);
        });
        return this;
    }

    public LiveActionGroup PressMenu() {
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.Start, true);
        });
        PreciseDelay(100);
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.Start, false);
        });
        return this;
    }

    // D-Pad methods

    public LiveActionGroup PressDPadUp() {
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.Up, true);
        });
        PreciseDelay(100);
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.Up, false);
        });
        return this;
    }

    public LiveActionGroup PressDPadDown() {
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.Down, true);
        });
        PreciseDelay(100);
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.Down, false);
        });
        return this;
    }

    public LiveActionGroup PressDPadLeft() {
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.Left, true);
        });
        PreciseDelay(100);
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.Left, false);
        });
        return this;
    }

    public LiveActionGroup PressDPadRight() {
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.Right, true);
        });
        PreciseDelay(100);
        ExecuteImmediate(pad => {
            pad.SetButtonState(Xbox360Button.Right, false);
        });
        return this;
    }

    // Stick methods

    public LiveActionGroup HoldLeftStick(double x, double y) {
        var xValue = Xbox360ControllerAPI.NormalizeStickValue(x);
        var yValue = Xbox360ControllerAPI.NormalizeStickValue(y);
        ExecuteImmediate(pad => {
            pad.SetAxisValue(Xbox360Axis.LeftThumbX, xValue);
            pad.SetAxisValue(Xbox360Axis.LeftThumbY, yValue);
        });
        return this;
    }

    public LiveActionGroup CancelLeftStick() {
        ExecuteImmediate(pad => {
            pad.SetAxisValue(Xbox360Axis.LeftThumbX, 0);
            pad.SetAxisValue(Xbox360Axis.LeftThumbY, 0);
        });
        return this;
    }

    public LiveActionGroup FlickLeftStick(double x, double y) {
        var xValue = Xbox360ControllerAPI.NormalizeStickValue(x);
        var yValue = Xbox360ControllerAPI.NormalizeStickValue(y);
        ExecuteImmediate(pad => {
            pad.SetAxisValue(Xbox360Axis.LeftThumbX, xValue);
            pad.SetAxisValue(Xbox360Axis.LeftThumbY, yValue);
        });
        PreciseDelay(50);
        ExecuteImmediate(pad => {
            pad.SetAxisValue(Xbox360Axis.LeftThumbX, 0);
            pad.SetAxisValue(Xbox360Axis.LeftThumbY, 0);
        });
        return this;
    }

    public LiveActionGroup HoldRightStick(double x, double y) {
        var xValue = Xbox360ControllerAPI.NormalizeStickValue(x);
        var yValue = Xbox360ControllerAPI.NormalizeStickValue(y);
        ExecuteImmediate(pad => {
            pad.SetAxisValue(Xbox360Axis.RightThumbX, xValue);
            pad.SetAxisValue(Xbox360Axis.RightThumbY, yValue);
        });
        return this;
    }

    public LiveActionGroup CancelRightStick() {
        ExecuteImmediate(pad => {
            pad.SetAxisValue(Xbox360Axis.RightThumbX, 0);
            pad.SetAxisValue(Xbox360Axis.RightThumbY, 0);
        });
        return this;
    }

    public LiveActionGroup FlickRightStick(double x, double y) {
        var xValue = Xbox360ControllerAPI.NormalizeStickValue(x);
        var yValue = Xbox360ControllerAPI.NormalizeStickValue(y);
        ExecuteImmediate(pad => {
            pad.SetAxisValue(Xbox360Axis.RightThumbX, xValue);
            pad.SetAxisValue(Xbox360Axis.RightThumbY, yValue);
        });
        PreciseDelay(50);
        ExecuteImmediate(pad => {
            pad.SetAxisValue(Xbox360Axis.RightThumbX, 0);
            pad.SetAxisValue(Xbox360Axis.RightThumbY, 0);
        });
        return this;
    }

    // Trigger methods

    public LiveActionGroup PressLeftTrigger() {
        ExecuteImmediate(pad => {
            pad.SetSliderValue(Xbox360Slider.LeftTrigger, 255);
        });
        PreciseDelay(100);
        ExecuteImmediate(pad => {
            pad.SetSliderValue(Xbox360Slider.LeftTrigger, 0);
        });
        return this;
    }
    public LiveActionGroup HoldLeftTrigger() {
        ExecuteImmediateAsync(pad => {
            pad.SetSliderValue(Xbox360Slider.LeftTrigger, 255);
        }).Wait();
        return this;
    }
    public LiveActionGroup CancelHoldLeftTrigger() {
        ExecuteImmediateAsync(pad => {
            pad.SetSliderValue(Xbox360Slider.LeftTrigger, 0);
        }).Wait();
        return this;
    }

    public LiveActionGroup PressRightTrigger() {
        ExecuteImmediate(pad => {
            pad.SetSliderValue(Xbox360Slider.RightTrigger, 255);
        });
        PreciseDelay(100);
        ExecuteImmediate(pad => {
            pad.SetSliderValue(Xbox360Slider.RightTrigger, 0);
        });
        return this;
    }
    public LiveActionGroup HoldRightTrigger() {
        ExecuteImmediateAsync(pad => {
            pad.SetSliderValue(Xbox360Slider.RightTrigger, 255);
        }).Wait();
        return this;
    }
    public LiveActionGroup CancelHoldRightTrigger() {
        ExecuteImmediateAsync(pad => {
            pad.SetSliderValue(Xbox360Slider.RightTrigger, 0);
        }).Wait();
        return this;
    }
}
