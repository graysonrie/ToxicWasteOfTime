"""
Example of using game frame calibration for deterministic timing.
This replaces time.sleep() with frame-accurate waiting.
"""

from xbox_api import Xbox360ControllerAPI
from game_timing import calibrate_game_frames, wait_game_frames

def main():
    # Initialize the API
    api = Xbox360ControllerAPI(base_url="http://localhost:5000")

    # Calibrate game frame timing (do this once at startup)
    # This measures the actual game frame timing by detecting pause menu overlay
    print("Calibrating game frame timing...")
    calibrator = calibrate_game_frames(api, num_samples=3)

    # Helper function to convert seconds to frames (for readability)
    def frames(seconds: float) -> int:
        return calibrator.seconds_to_frames(seconds)

    # Now use frame-based timing instead of sleep
    live_actions = api.live_actions()

    # Your original test_waypoint.py code, but using frame timing:
    live_actions.hold_left_stick(0, 1)  # walk forward
    wait_game_frames(calibrator, frames(1.0))  # Wait 1 second

    live_actions.hold_right_stick(-1, 0)
    wait_game_frames(calibrator, frames(0.1))  # Wait 0.1 seconds
    live_actions.cancel_right_stick()

    wait_game_frames(calibrator, frames(0.2))
    live_actions.hold_right_stick(-1, 0)
    wait_game_frames(calibrator, frames(0.1))
    live_actions.cancel_right_stick()

    wait_game_frames(calibrator, frames(0.2))
    live_actions.hold_right_stick(-1, 0)
    wait_game_frames(calibrator, frames(0.05))  # Wait 0.05 seconds
    live_actions.cancel_right_stick()

    wait_game_frames(calibrator, frames(1.0))
    live_actions.hold_a()
    wait_game_frames(calibrator, frames(0.5))
    live_actions.cancel_hold_a()

    live_actions.press_right_shoulder()
    wait_game_frames(calibrator, frames(0.5))
    live_actions.hold_right_stick(1, 0)  # walk right
    wait_game_frames(calibrator, frames(0.25))
    live_actions.cancel_right_stick()

    wait_game_frames(calibrator, frames(3.0))
    live_actions.hold_a()
    wait_game_frames(calibrator, frames(1.0))
    live_actions.cancel_hold_a()

    wait_game_frames(calibrator, frames(1.0))
    live_actions.complete()


if __name__ == "__main__":
    main()
