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
        while True:

            right_x = random_range(-1, 1)
            right_y = random_range(-1, 1)
            live_actions.hold_right_stick(right_x, right_y) # random direction
            sleep(random_range(.2,.6))
            live_actions.cancel_right_stick()
            # Start moving forward
            left_x = random_range(-1, 1)
            live_actions.hold_left_stick(left_x, 0) # walk forward
            live_actions.cancel_right_stick()
            live_actions.press_right_trigger()

            # Jump while moving
            sleep(random_range(.1,.5))
            live_actions.press_a()  # Quick jump
            sleep(random_range(.1,.3))
            live_actions.press_right_trigger()

            # Continue moving and turn
            left_x = random_range(-1, 1)
            live_actions.hold_left_stick(left_x, 0) # keep moving forward
            right_x = random_range(-1, 1)
            live_actions.press_a()  # Quick jump
            right_y = random_range(-1, 1)
            live_actions.hold_right_stick(right_x, right_y) # random direction
            live_actions.press_right_trigger()
            sleep(random_range(.2,.6))
            live_actions.press_right_trigger()
            live_actions.cancel_right_stick()

            # Another jump
            live_actions.press_a()  # Quick jump
            live_actions.press_right_trigger()
            sleep(random_range(.1,.3))
            live_actions.press_right_trigger()

            # Keep moving
            left_x = random_range(-1, 1)
            live_actions.hold_left_stick(left_x, 0) # walk forward
            live_actions.cancel_right_stick()
            live_actions.press_right_trigger()
            live_actions.press_a()  # Quick jump
            sleep(random_range(.1,.4))
            live_actions.press_right_trigger()
            live_actions.press_a()  # Quick jump

            live_actions.press_b()
            live_actions.press_a()  # Quick jump
            if irandom_range(0,1) == 0:
                live_actions.press_dpad_right()
            else:
                live_actions.press_dpad_left()

            # Continue moving forward
            left_x = random_range(-1, 1)
            live_actions.hold_left_stick(left_x, 0) # walk forward
            right_x = random_range(-1, 1)
            right_y = random_range(-1, 1)
            live_actions.hold_right_stick(right_x, right_y) # random direction
            live_actions.press_right_trigger()
            live_actions.press_a()  # Quick jump
            sleep(random_range(.2,.5))
            live_actions.press_a()  # Quick jump

            # Jump again
            live_actions.press_right_trigger()
            sleep(random_range(.1,.2))

            right_x = random_range(-1, 1)
            right_y = random_range(-1, 1)
            live_actions.hold_right_stick(right_x, right_y) # random direction
            live_actions.press_right_trigger()
            sleep(random_range(.2,.6))

            live_actions.press_a()  # Quick jump
            live_actions.cancel_right_stick()
            live_actions.press_right_trigger()

            live_actions.press_right_trigger()
            sleep(random_range(.1,.3))
            live_actions.press_right_trigger()

            # Keep moving
            left_x = random_range(-1, 1)
            live_actions.hold_left_stick(left_x, 0) # walk forward
            sleep(random_range(.3,.8))
            live_actions.press_right_trigger()

            live_actions.press_left_shoulder()
            live_actions.press_right_trigger()
            sleep(random_range(.1,.3))
            live_actions.press_right_trigger()

            # Continue moving and turning
            left_x = random_range(-1, 1)
            live_actions.hold_left_stick(left_x, 0) # walk forward
            right_x = random_range(-1, 1)
            right_y = random_range(-1, 1)
            live_actions.press_right_trigger()
            live_actions.hold_right_stick(right_x, right_y) # random direction
            sleep(random_range(.2,.5))

            live_actions.press_right_trigger()
            # Another jump
            live_actions.press_a()  # Quick jump
            sleep(random_range(.1,.2))

            # Keep moving
            left_x = random_range(-1, 1)
            live_actions.hold_left_stick(left_x, 0) # walk forward
            live_actions.press_right_shoulder()
            sleep(random_range(.3,.6))

            live_actions.press_right_trigger()
            sleep(random_range(1,2))

    except KeyboardInterrupt:
        print('\nCtrl-C received! Exiting loop and completing actions...')

    live_actions.complete()


if __name__ == "__main__":
    main()