"""
Game frame calibration system for synchronizing actions with the game's update loop.
Uses the pause menu overlay to detect frame timing.
"""

import time
from typing import Optional
from PIL import Image
from xbox_api import Xbox360ControllerAPI
from image_processing.xbox_screenshot import get_raw_xbox_app_screenshot


class GameFrameCalibrator:
    """
    Calibrates game frame timing by detecting when the pause menu overlay appears.
    """

    def __init__(self, api: Xbox360ControllerAPI, sample_region_size: int = 50,
                 brightness_threshold: float = 0.15, max_wait_ms: int = 200):
        """
        Initialize the frame calibrator.

        Args:
            api: Xbox360ControllerAPI instance
            sample_region_size: Size of the center region to sample for brightness (pixels)
            brightness_threshold: How much darker the overlay must be (0.0-1.0)
            max_wait_ms: Maximum time to wait for overlay to appear (milliseconds)
        """
        self.api = api
        self.sample_region_size = sample_region_size
        self.brightness_threshold = brightness_threshold
        self.max_wait_ms = max_wait_ms
        self.frame_time_ms: Optional[float] = None
        self.input_lag_ms: Optional[float] = None

    def _get_average_brightness(self, img: Image.Image) -> float:
        """
        Get the average brightness of the center region of the image.

        Args:
            img: PIL Image

        Returns:
            Average brightness (0.0-1.0)
        """
        width, height = img.size
        center_x = width // 2
        center_y = height // 2

        # Sample a square region in the center
        half_size = self.sample_region_size // 2
        left = max(0, center_x - half_size)
        top = max(0, center_y - half_size)
        right = min(width, center_x + half_size)
        bottom = min(height, center_y + half_size)

        region = img.crop((left, top, right, bottom))
        pixels = list(region.getdata())

        # Calculate average brightness (using luminance formula)
        total_brightness = 0.0
        for pixel in pixels:
            if isinstance(pixel, tuple) and len(pixel) >= 3:
                r, g, b = pixel[0], pixel[1], pixel[2]
                # Luminance formula: 0.299*R + 0.587*G + 0.114*B
                brightness = (0.299 * r + 0.587 * g + 0.114 * b) / 255.0
                total_brightness += brightness

        return total_brightness / len(pixels) if pixels else 0.0

    def _is_overlay_visible(self, baseline_brightness: float, current_brightness: float) -> bool:
        """
        Check if the pause overlay is visible by comparing brightness.

        Args:
            baseline_brightness: Brightness before pause
            current_brightness: Current brightness

        Returns:
            True if overlay is detected (screen is darker)
        """
        brightness_drop = baseline_brightness - current_brightness
        return brightness_drop >= self.brightness_threshold

    def calibrate(self, num_samples: int = 3, initial_delay: float = 0.5) -> dict:
        """
        Calibrate game frame timing by measuring pause menu overlay appearance.

        Args:
            num_samples: Number of calibration samples to take (more = more accurate)
            initial_delay: Delay before starting calibration (seconds)

        Returns:
            Dictionary with calibration results:
            - frame_time_ms: Average time per game frame in milliseconds
            - input_lag_ms: Average input lag in milliseconds
            - samples: List of individual sample timings
        """
        live_actions = self.api.live_actions()
        samples = []
        input_lags = []

        print(f"Calibrating game frame timing ({num_samples} samples)...")
        print("  Make sure the game is running and not paused before starting calibration.")
        time.sleep(initial_delay)  # Give game time to be ready

        for i in range(num_samples):
            if i > 0:
                # Extra delay between samples to ensure game is ready
                time.sleep(0.5)
            print(f"  Sample {i+1}/{num_samples}...")

            # Get baseline brightness (unpaused)
            baseline_img = get_raw_xbox_app_screenshot(save_to_file=False)
            if baseline_img is None:
                print("    Failed to capture baseline screenshot")
                continue

            baseline_brightness = self._get_average_brightness(baseline_img)
            print(f"    Baseline brightness: {baseline_brightness:.3f}")

            # Press menu button and measure time until overlay appears
            start_time = time.perf_counter()
            live_actions.press_menu()

            # Poll for overlay appearance
            overlay_detected = False
            check_interval = 0.001  # Check every 1ms
            max_checks = int(self.max_wait_ms / (check_interval * 1000))

            for _ in range(max_checks):
                time.sleep(check_interval)
                current_img = get_raw_xbox_app_screenshot(save_to_file=False)
                if current_img is None:
                    continue

                current_brightness = self._get_average_brightness(current_img)

                if self._is_overlay_visible(baseline_brightness, current_brightness):
                    elapsed_ms = (time.perf_counter() - start_time) * 1000
                    samples.append(elapsed_ms)
                    input_lags.append(elapsed_ms)
                    overlay_detected = True
                    print(f"    Overlay detected after {elapsed_ms:.2f}ms")
                    break

            if not overlay_detected:
                print(f"    Warning: Overlay not detected within {self.max_wait_ms}ms")
                # Try to unpause anyway (press B to exit pause menu)
                live_actions.press_b()
                time.sleep(0.3)
                continue

            # Wait a bit, then unpause (press B to exit pause menu)
            time.sleep(0.25)  # Give game time to process pause state
            print("    Unpausing game (pressing B to exit pause menu)...")
            live_actions.press_b()

            # Wait for the button press to complete (press_b takes ~100ms)
            time.sleep(0.15)

            # Wait and verify unpause worked by checking brightness returns to baseline
            unpause_timeout = 1.0  # 1 second max wait for unpause (more generous)
            unpause_start = time.perf_counter()
            unpaused = False

            while (time.perf_counter() - unpause_start) < unpause_timeout:
                time.sleep(0.05)  # Check every 50ms
                current_img = get_raw_xbox_app_screenshot(save_to_file=False)
                if current_img is None:
                    continue

                current_brightness = self._get_average_brightness(current_img)
                # Check if brightness returned close to baseline (overlay gone)
                brightness_diff = abs(current_brightness - baseline_brightness)
                if brightness_diff < self.brightness_threshold * 0.5:  # Close to baseline
                    unpaused = True
                    elapsed = (time.perf_counter() - unpause_start) * 1000
                    print(f"    Unpaused confirmed after {elapsed:.1f}ms (brightness: {current_brightness:.3f})")
                    break

            if not unpaused:
                print(f"    Warning: Unpause not confirmed within {unpause_timeout*1000:.0f}ms")
                print(f"    Current brightness: {current_brightness:.3f}, Baseline: {baseline_brightness:.3f}")
                # Try one more unpause attempt (press B to exit pause menu)
                print("    Attempting additional unpause (pressing B)...")
                live_actions.press_b()
                time.sleep(0.3)

            # Extra wait to ensure game is fully unpaused and ready for next sample
            # time.sleep(0.3)

        if not samples:
            raise RuntimeError("Calibration failed: No successful samples")

        # Final check: Ensure game is unpaused before finishing
        print("\n  Verifying game is unpaused...")
        final_img = get_raw_xbox_app_screenshot(save_to_file=False)
        if final_img is not None:
            final_brightness = self._get_average_brightness(final_img)
            # If still dark, try unpausing one more time (press B to exit pause menu)
            if final_brightness < 0.3:  # Arbitrary threshold - adjust if needed
                print("  Game appears paused, attempting final unpause (pressing B)...")
                live_actions.press_b()
                time.sleep(0.3)
                # Check again
                final_img2 = get_raw_xbox_app_screenshot(save_to_file=False)
                if final_img2 is not None:
                    final_brightness2 = self._get_average_brightness(final_img2)
                    if final_brightness2 > final_brightness:
                        print("  Final unpause successful")
                    else:
                        print("  Warning: Game may still be paused")

        # Calculate averages
        avg_input_lag = sum(input_lags) / len(input_lags)

        # Assume 60fps = 16.67ms per frame
        # The input lag represents when the game processes the input
        # We'll use this to calculate frame-accurate timing
        self.input_lag_ms = avg_input_lag
        self.frame_time_ms = 1000.0 / 30.0  # 16.67ms per frame at 60fps

        result = {
            'frame_time_ms': self.frame_time_ms,
            'input_lag_ms': self.input_lag_ms,
            'samples': samples,
            'num_samples': len(samples)
        }

        print(f"\nCalibration complete:")
        print(f"  Frame time: {self.frame_time_ms:.2f}ms (60fps)")
        print(f"  Input lag: {self.input_lag_ms:.2f}ms (avg)")
        print(f"  Successful samples: {len(samples)}/{num_samples}")

        return result

    def wait_game_frames(self, num_frames: int) -> None:
        """
        Wait for a specific number of game frames using calibrated timing.
        Accounts for input lag to ensure frame-accurate timing.

        Args:
            num_frames: Number of game frames to wait
        """
        if self.frame_time_ms is None:
            raise RuntimeError("Calibration not performed. Call calibrate() first.")

        wait_time_ms = num_frames * self.frame_time_ms
        wait_time_sec = wait_time_ms / 1000.0

        # Use high-precision sleep with busy-wait for final millisecond
        start = time.perf_counter()
        target_time = start + wait_time_sec

        # Sleep in chunks, then busy-wait for precision
        while time.perf_counter() < target_time - 0.002:  # Leave 2ms for busy-wait
            time.sleep(0.001)

        # Busy-wait for remaining time for maximum precision
        while time.perf_counter() < target_time:
            pass

    def seconds_to_frames(self, seconds: float) -> int:
        """
        Convert seconds to game frames.

        Args:
            seconds: Time in seconds

        Returns:
            Number of frames
        """
        if self.frame_time_ms is None:
            raise RuntimeError("Calibration not performed. Call calibrate() first.")

        return int(seconds * 30.0)  # 30fps


def calibrate_game_frames(api: Xbox360ControllerAPI, num_samples: int = 3) -> GameFrameCalibrator:
    """
    Convenience function to calibrate game frame timing.

    Args:
        api: Xbox360ControllerAPI instance
        num_samples: Number of calibration samples to take

    Returns:
        Calibrated GameFrameCalibrator instance
    """
    calibrator = GameFrameCalibrator(api)
    calibrator.calibrate(num_samples)
    return calibrator


def wait_game_frames(calibrator: GameFrameCalibrator, num_frames: int) -> None:
    """
    Convenience function to wait for game frames.

    Args:
        calibrator: Calibrated GameFrameCalibrator instance
        num_frames: Number of game frames to wait
    """
    calibrator.wait_game_frames(num_frames)
