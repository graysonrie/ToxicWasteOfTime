from cgi import test
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

            dir = random_range(-1, 1)
            live_actions.hold_right_stick(dir, 0) # turn a random amount
            sleep(random_range(.2,.6))
            live_actions.cancel_right_stick()
            # Start moving forward
            live_actions.hold_left_stick(0, 1) # walk forward
            live_actions.cancel_right_stick()

            # Jump while moving
            sleep(random_range(.1,.5))
            live_actions.press_a()  # Quick jump
            sleep(random_range(.1,.3))

            # Continue moving and turn
            live_actions.hold_left_stick(0, 1) # keep moving forward
            dir = random_range(-1, 1)

            live_actions.hold_right_stick(dir, 0) # turn a random amount
            sleep(random_range(.2,.6))
            live_actions.cancel_right_stick()

            # Another jump
            live_actions.press_a()  # Quick jump
            sleep(random_range(.1,.3))

            # Keep moving
            live_actions.hold_left_stick(0, 1) # walk forward
            live_actions.cancel_right_stick()
            sleep(random_range(.1,.4))

            live_actions.press_b()
            if irandom_range(0,1) == 0:
                live_actions.press_dpad_right()
            else:
                live_actions.press_dpad_left()

            # Continue moving forward
            live_actions.hold_left_stick(0, 1) # walk forward
            dir = random_range(-1, 1)
            live_actions.hold_right_stick(dir, 0) # turn a random amount
            sleep(random_range(.2,.5))

            # Jump again
            live_actions.press_a()  # Quick jump
            sleep(random_range(.1,.2))

            live_actions.hold_right_stick(dir, 0) # turn a random amount
            sleep(random_range(.2,.6))
            live_actions.cancel_right_stick()

            live_actions.press_right_trigger()
            sleep(random_range(.1,.3))

            # Keep moving
            live_actions.hold_left_stick(0, 1) # walk forward
            live_actions.press_y()
            sleep(random_range(.3,.8))

            live_actions.press_left_shoulder()
            sleep(random_range(.1,.3))

            # Continue moving and turning
            live_actions.hold_left_stick(0, 1) # walk forward
            dir = random_range(-.5, .5)
            live_actions.hold_right_stick(dir, 0) # turn a random amount
            sleep(random_range(.2,.5))

            # Another jump
            live_actions.press_a()  # Quick jump
            sleep(random_range(.1,.2))

            # Keep moving
            live_actions.hold_left_stick(0, 1) # walk forward
            live_actions.press_right_shoulder()
            sleep(random_range(.3,.6))

            live_actions.hold_right_trigger()
            sleep(random_range(1,2))

    except KeyboardInterrupt:
        print('\nCtrl-C received! Exiting loop and completing actions...')

    live_actions.complete()


if __name__ == "__main__":
    main()