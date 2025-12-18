from cgi import test
import time
from xbox_api import Xbox360ControllerAPI
from time import sleep

def main():
    # Initialize the API (defaults to http://localhost:5000)
    # You can specify a different URL if your C# API runs on a different port
    api = Xbox360ControllerAPI(base_url="http://localhost:5000")

    sleep(1)

    # Manual executing of target_1.png
    # Character is facing 90 (north)
    print("Example 6: TestAction1 equivalent")
    live_actions = api.live_actions()

    live_actions.hold_left_stick(0, 1) # walk forward
    # sleep(1)
    # live_actions.hold_right_stick(-1,0)
    # sleep(.1)
    # live_actions.cancel_right_stick()

    # sleep(.2)
    # live_actions.hold_right_stick(-1,0)
    # sleep(.1)
    # live_actions.cancel_right_stick()

    # sleep(.2)
    # live_actions.hold_right_stick(-1,0)
    # sleep(.05)
    # live_actions.cancel_right_stick()
    # sleep(1)
    # live_actions.hold_a()
    # sleep(.5)
    # live_actions.cancel_hold_a()
    # live_actions.press_right_shoulder()
    # sleep(.5)
    # live_actions.hold_right_stick(1,0) # walk right
    # sleep(.25)
    # live_actions.cancel_right_stick()
    # sleep(3)
    # live_actions.hold_a()
    # sleep(1)
    # live_actions.cancel_hold_a()

    sleep(1)
    live_actions.complete()


if __name__ == "__main__":
    main()