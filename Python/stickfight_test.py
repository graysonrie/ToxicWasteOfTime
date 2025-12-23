import time
from xbox_api import Xbox360ControllerAPI
from time import sleep
import random
def main():
    # Initialize the API (defaults to http://localhost:5000)
    # You can specify a different URL if your C# API runs on a different port
    api = Xbox360ControllerAPI(base_url="http://localhost:5000")

    sleep(1)

    print("Executing random actions")
    live_actions = api.live_actions()

    def random_range(min, max):
        return random.uniform(min, max)

    def irandom_range(min, max):
        return random.randint(min, max)

    try:
        # Track trigger state for alternating
        use_right_trigger = True

        # Track left stick direction (-1 or 1)
        left_stick_x = random.choice([-1, 1])  # Start with random direction
        left_stick_hold_time = 0
        left_stick_duration = random_range(0.05, 3.0)  # Very short to a few seconds

        # Track right stick update timing
        right_stick_update_time = 0
        right_stick_update_interval = random_range(0.05, 0.2)  # Update frequently

        # Track A button press timing
        a_press_time = 0
        a_press_interval = random_range(0.1, 0.4)  # Press A frequently

        # Track X button press timing
        x_press_time = 0
        x_press_interval = random_range(2.0, 5.0)  # Press X occasionally

        # Track trigger press timing
        trigger_press_time = 0
        trigger_press_interval = random_range(0.1, 0.3)  # Alternate triggers frequently

        start_time = time.time()

        # Initialize left stick position
        live_actions.hold_left_stick(left_stick_x, 0)

        while True:
            current_time = time.time()
            elapsed = current_time - start_time

            # Constantly move right stick between -1 and 1 for both X and Y
            if elapsed >= right_stick_update_time:
                right_x = random_range(-1, 1)
                right_y = random_range(-1, 1)
                live_actions.hold_right_stick(right_x, right_y)
                right_stick_update_time = elapsed + right_stick_update_interval
                right_stick_update_interval = random_range(0.05, 0.2)

            # Left stick: alternate between -1 and 1 on X axis for varying durations
            if elapsed >= left_stick_hold_time:
                # Switch direction
                left_stick_x = -1 if left_stick_x == 1 else 1
                live_actions.hold_left_stick(left_stick_x, 0)
                left_stick_duration = random_range(0.05, 3.0)  # Very short to a few seconds
                left_stick_hold_time = elapsed + left_stick_duration

            # Rapidly press A often
            if elapsed >= a_press_time:
                live_actions.press_a()
                a_press_interval = random_range(0.1, 0.4)
                a_press_time = elapsed + a_press_interval

            # Constantly alternate between right and left trigger (never at same time)
            if elapsed >= trigger_press_time:
                if use_right_trigger:
                    live_actions.press_right_trigger()
                else:
                    live_actions.press_left_trigger()
                use_right_trigger = not use_right_trigger  # Alternate
                trigger_press_interval = random_range(0.1, 0.3)
                trigger_press_time = elapsed + trigger_press_interval

            # Occasionally press X button
            if elapsed >= x_press_time:
                live_actions.press_x()
                x_press_interval = random_range(2.0, 5.0)
                x_press_time = elapsed + x_press_interval

            # Small sleep to prevent tight loop
            sleep(0.01)

    except KeyboardInterrupt:
        print('\nCtrl-C received! Exiting loop and completing actions...')

    live_actions.complete()


if __name__ == "__main__":
    main()