"""
Example usage of the Xbox 360 Controller API Python client.
This mirrors the C# API structure and usage patterns.
"""

import time
from xbox_api import Xbox360ControllerAPI


def main():
    # Initialize the API (defaults to http://localhost:5000)
    # You can specify a different URL if your C# API runs on a different port
    api = Xbox360ControllerAPI(base_url="http://localhost:5000")

    time.sleep(1)

    # Example 6: Similar to TestAction1 from C# project
    print("Example 6: TestAction1 equivalent")
    test_action = api.record_actions()
    test_action.press_menu()
    test_action.wait(3000)
    test_action.flick_left_stick(0, -1)
    test_action.wait_trivial()
    test_action.flick_left_stick(0, -1)
    test_action.wait_trivial()
    test_action.press_a()
    test_action.wait(1500)
    test_action.hold_right_stick(-1, 0, 1000)
    test_action.execute()


if __name__ == "__main__":
    main()
