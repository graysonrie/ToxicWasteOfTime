import requests
from typing import List, Optional, Dict, Any


class ActionGroup:
    """
    Action group for recording and executing controller inputs.
    Actions without a Wait() between them execute in parallel.
    """

    def __init__(self, api_url: str):
        """
        Initialize an action group.

        Args:
            api_url: Base URL for the Xbox controller API
        """
        self.api_url = api_url
        self._actions: List[Dict[str, Any]] = []

    def wait(self, milliseconds: int) -> 'ActionGroup':
        """
        Waits for the specified duration. This breaks parallel grouping -
        actions after a Wait() execute sequentially.

        Args:
            milliseconds: Duration to wait in milliseconds

        Returns:
            ActionGroup: Self for method chaining
        """
        self._actions.append({
            "Type": "wait",
            "Milliseconds": milliseconds
        })
        return self

    def wait_trivial(self) -> 'ActionGroup':
        """
        Wait a very small time (25ms).

        Returns:
            ActionGroup: Self for method chaining
        """
        return self.wait(25)

    # Button methods

    def press_a(self) -> 'ActionGroup':
        """Press A button (100ms hold)."""
        return self.hold_a(100)

    def hold_a(self, milliseconds: int) -> 'ActionGroup':
        """
        Hold A button for specified duration.

        Args:
            milliseconds: Duration to hold in milliseconds

        Returns:
            ActionGroup: Self for method chaining
        """
        self._actions.append({
            "Type": "pressa",
            "Milliseconds": milliseconds
        })
        return self

    def press_b(self) -> 'ActionGroup':
        """Press B button (100ms hold)."""
        return self.hold_b(100)

    def hold_b(self, milliseconds: int) -> 'ActionGroup':
        """
        Hold B button for specified duration.

        Args:
            milliseconds: Duration to hold in milliseconds

        Returns:
            ActionGroup: Self for method chaining
        """
        self._actions.append({
            "Type": "pressb",
            "Milliseconds": milliseconds
        })
        return self

    def press_x(self) -> 'ActionGroup':
        """Press X button (100ms hold)."""
        return self.hold_x(100)

    def hold_x(self, milliseconds: int) -> 'ActionGroup':
        """
        Hold X button for specified duration.

        Args:
            milliseconds: Duration to hold in milliseconds

        Returns:
            ActionGroup: Self for method chaining
        """
        self._actions.append({
            "Type": "pressx",
            "Milliseconds": milliseconds
        })
        return self

    def press_y(self) -> 'ActionGroup':
        """Press Y button (100ms hold)."""
        return self.hold_y(100)

    def hold_y(self, milliseconds: int) -> 'ActionGroup':
        """
        Hold Y button for specified duration.

        Args:
            milliseconds: Duration to hold in milliseconds

        Returns:
            ActionGroup: Self for method chaining
        """
        self._actions.append({
            "Type": "pressy",
            "Milliseconds": milliseconds
        })
        return self

    def press_left_shoulder(self) -> 'ActionGroup':
        """Press left shoulder button."""
        self._actions.append({
            "Type": "pressleftshoulder"
        })
        return self

    def press_right_shoulder(self) -> 'ActionGroup':
        """Press right shoulder button."""
        self._actions.append({
            "Type": "pressrightshoulder"
        })
        return self

    def press_view(self) -> 'ActionGroup':
        """Press View button (Back button)."""
        self._actions.append({
            "Type": "pressview"
        })
        return self

    def press_menu(self) -> 'ActionGroup':
        """Press Menu button (Start button)."""
        self._actions.append({
            "Type": "pressmenu"
        })
        return self

    # D-Pad methods

    def press_dpad_up(self) -> 'ActionGroup':
        """Press D-Pad up."""
        self._actions.append({
            "Type": "pressdpadup"
        })
        return self

    def press_dpad_down(self) -> 'ActionGroup':
        """Press D-Pad down."""
        self._actions.append({
            "Type": "pressdpaddown"
        })
        return self

    def press_dpad_left(self) -> 'ActionGroup':
        """Press D-Pad left."""
        self._actions.append({
            "Type": "pressdpadleft"
        })
        return self

    def press_dpad_right(self) -> 'ActionGroup':
        """Press D-Pad right."""
        self._actions.append({
            "Type": "pressdpadright"
        })
        return self

    # Stick methods (using -1 to 1 range)

    def hold_left_stick(self, x: float, y: float, milliseconds: int) -> 'ActionGroup':
        """
        Hold left stick at specified position for duration.

        Args:
            x: X axis value (-1.0 to 1.0)
            y: Y axis value (-1.0 to 1.0)
            milliseconds: Duration to hold in milliseconds

        Returns:
            ActionGroup: Self for method chaining
        """
        self._actions.append({
            "Type": "holdleftstick",
            "X": x,
            "Y": y,
            "Milliseconds": milliseconds
        })
        return self

    def flick_left_stick(self, x: float, y: float) -> 'ActionGroup':
        """
        Quick flick of left stick (50ms).

        Args:
            x: X axis value (-1.0 to 1.0)
            y: Y axis value (-1.0 to 1.0)

        Returns:
            ActionGroup: Self for method chaining
        """
        self._actions.append({
            "Type": "flickleftstick",
            "X": x,
            "Y": y
        })
        return self

    def hold_right_stick(self, x: float, y: float, milliseconds: int) -> 'ActionGroup':
        """
        Hold right stick at specified position for duration.

        Args:
            x: X axis value (-1.0 to 1.0)
            y: Y axis value (-1.0 to 1.0)
            milliseconds: Duration to hold in milliseconds

        Returns:
            ActionGroup: Self for method chaining
        """
        self._actions.append({
            "Type": "holdrightstick",
            "X": x,
            "Y": y,
            "Milliseconds": milliseconds
        })
        return self

    def flick_right_stick(self, x: float, y: float) -> 'ActionGroup':
        """
        Quick flick of right stick (50ms).

        Args:
            x: X axis value (-1.0 to 1.0)
            y: Y axis value (-1.0 to 1.0)

        Returns:
            ActionGroup: Self for method chaining
        """
        self._actions.append({
            "Type": "flickrightstick",
            "X": x,
            "Y": y
        })
        return self

    # Trigger methods

    def press_left_trigger(self) -> 'ActionGroup':
        """Press left trigger (100ms hold)."""
        return self.hold_left_trigger(100)

    def hold_left_trigger(self, milliseconds: int) -> 'ActionGroup':
        """
        Hold left trigger for specified duration.

        Args:
            milliseconds: Duration to hold in milliseconds

        Returns:
            ActionGroup: Self for method chaining
        """
        self._actions.append({
            "Type": "presslefttrigger",
            "Milliseconds": milliseconds
        })
        return self

    def press_right_trigger(self) -> 'ActionGroup':
        """Press right trigger (100ms hold)."""
        return self.hold_right_trigger(100)

    def hold_right_trigger(self, milliseconds: int) -> 'ActionGroup':
        """
        Hold right trigger for specified duration.

        Args:
            milliseconds: Duration to hold in milliseconds

        Returns:
            ActionGroup: Self for method chaining
        """
        self._actions.append({
            "Type": "pressrighttrigger",
            "Milliseconds": milliseconds
        })
        return self

    def execute(self) -> bool:
        """
        Executes all queued actions by sending them to the API.

        Returns:
            bool: True if execution was successful, False otherwise
        """
        if not self._actions:
            return True  # No actions to execute

        try:
            response = requests.post(
                f"{self.api_url}/actions/execute",
                json={"Actions": self._actions},
                timeout=30
            )
            response.raise_for_status()
            result = response.json()
            return result.get("Success", False)
        except requests.exceptions.RequestException as e:
            print(f"Error executing actions: {e}")
            return False

    async def execute_async(self) -> bool:
        """
        Asynchronous version of execute (for use with async/await).
        Note: This is a placeholder - you may want to use httpx or aiohttp for true async.

        Returns:
            bool: True if execution was successful, False otherwise
        """
        # For now, just call the synchronous version
        # You can replace this with httpx or aiohttp if needed
        return self.execute()
