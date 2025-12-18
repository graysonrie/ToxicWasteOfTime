import requests
from typing import Optional


class LiveActionGroup:
    """
    Live action group for executing controller inputs in real-time.
    Actions execute immediately when called, without needing execute().
    """

    def __init__(self, api_url: str):
        """
        Initialize a live action group.

        Args:
            api_url: Base URL for the Xbox controller API
        """
        self.api_url = api_url

    def _execute_live_action(self, action_type: str, x: Optional[float] = None,
                            y: Optional[float] = None, milliseconds: Optional[int] = None) -> bool:
        """
        Execute a live action immediately via HTTP.

        Args:
            action_type: Type of action to execute
            x: Optional X value for stick actions
            y: Optional Y value for stick actions
            milliseconds: Optional duration for hold actions

        Returns:
            bool: True if execution was successful, False otherwise
        """
        payload = {"Type": action_type}
        if x is not None:
            payload["X"] = x
        if y is not None:
            payload["Y"] = y
        if milliseconds is not None:
            payload["Milliseconds"] = milliseconds

        try:
            response = requests.post(
                f"{self.api_url}/live/action",
                json=payload,
                timeout=5
            )
            response.raise_for_status()
            result = response.json()
            return result.get("Success", False)
        except requests.exceptions.RequestException as e:
            print(f"Error executing live action: {e}")
            return False

    def complete(self) -> 'LiveActionGroup':
        """
        Signals that all live actions are complete and zeros out all inputs.
        This resets all buttons, sticks, and triggers to their neutral state.

        Returns:
            LiveActionGroup: Self for method chaining
        """
        self._execute_live_action("complete")
        return self

    # Button methods

    def press_a(self) -> 'LiveActionGroup':
        """Press A button (100ms hold)."""
        self._execute_live_action("pressa")
        return self

    def hold_a(self) -> 'LiveActionGroup':
        """
        Hold A button (continues until cancel_hold_a() is called).

        Returns:
            LiveActionGroup: Self for method chaining
        """
        self._execute_live_action("holda")
        return self

    def cancel_hold_a(self) -> 'LiveActionGroup':
        """Cancel/zero out the A button."""
        self._execute_live_action("cancelholda")
        return self

    def press_b(self) -> 'LiveActionGroup':
        """Press B button (100ms hold)."""
        self._execute_live_action("pressb")
        return self

    def hold_b(self) -> 'LiveActionGroup':
        """
        Hold B button (continues until cancel_hold_b() is called).

        Returns:
            LiveActionGroup: Self for method chaining
        """
        self._execute_live_action("holdb")
        return self

    def cancel_hold_b(self) -> 'LiveActionGroup':
        """Cancel/zero out the B button."""
        self._execute_live_action("cancelholdb")
        return self

    def press_x(self) -> 'LiveActionGroup':
        """Press X button (100ms hold)."""
        self._execute_live_action("pressx")
        return self

    def hold_x(self) -> 'LiveActionGroup':
        """
        Hold X button (continues until cancel_hold_x() is called).

        Returns:
            LiveActionGroup: Self for method chaining
        """
        self._execute_live_action("holdx")
        return self

    def cancel_hold_x(self) -> 'LiveActionGroup':
        """Cancel/zero out the X button."""
        self._execute_live_action("cancelholdx")
        return self

    def press_y(self) -> 'LiveActionGroup':
        """Press Y button (100ms hold)."""
        self._execute_live_action("pressy")
        return self

    def hold_y(self) -> 'LiveActionGroup':
        """
        Hold Y button (continues until cancel_hold_y() is called).

        Returns:
            LiveActionGroup: Self for method chaining
        """
        self._execute_live_action("holdy")
        return self

    def cancel_hold_y(self) -> 'LiveActionGroup':
        """Cancel/zero out the Y button."""
        self._execute_live_action("cancelholdy")
        return self

    def press_left_shoulder(self) -> 'LiveActionGroup':
        """Press left shoulder button."""
        self._execute_live_action("pressleftshoulder")
        return self

    def press_right_shoulder(self) -> 'LiveActionGroup':
        """Press right shoulder button."""
        self._execute_live_action("pressrightshoulder")
        return self

    def press_view(self) -> 'LiveActionGroup':
        """Press View button (Back button)."""
        self._execute_live_action("pressview")
        return self

    def press_menu(self) -> 'LiveActionGroup':
        """Press Menu button (Start button)."""
        self._execute_live_action("pressmenu")
        return self

    # D-Pad methods

    def press_dpad_up(self) -> 'LiveActionGroup':
        """Press D-Pad up."""
        self._execute_live_action("pressdpadup")
        return self

    def press_dpad_down(self) -> 'LiveActionGroup':
        """Press D-Pad down."""
        self._execute_live_action("pressdpaddown")
        return self

    def press_dpad_left(self) -> 'LiveActionGroup':
        """Press D-Pad left."""
        self._execute_live_action("pressdpadleft")
        return self

    def press_dpad_right(self) -> 'LiveActionGroup':
        """Press D-Pad right."""
        self._execute_live_action("pressdpadright")
        return self

    # Stick methods

    def hold_left_stick(self, x: float, y: float) -> 'LiveActionGroup':
        """
        Hold left stick at specified position (executes immediately).

        Args:
            x: X axis value (-1.0 to 1.0)
            y: Y axis value (-1.0 to 1.0)

        Returns:
            LiveActionGroup: Self for method chaining
        """
        self._execute_live_action("holdleftstick", x=x, y=y)
        return self

    def cancel_left_stick(self) -> 'LiveActionGroup':
        """Cancel/zero out the left stick."""
        self._execute_live_action("cancelleftstick")
        return self

    def flick_left_stick(self, x: float, y: float) -> 'LiveActionGroup':
        """
        Quick flick of left stick (50ms, executes immediately).

        Args:
            x: X axis value (-1.0 to 1.0)
            y: Y axis value (-1.0 to 1.0)

        Returns:
            LiveActionGroup: Self for method chaining
        """
        self._execute_live_action("flickleftstick", x=x, y=y)
        return self

    def hold_right_stick(self, x: float, y: float) -> 'LiveActionGroup':
        """
        Hold right stick at specified position (executes immediately).

        Args:
            x: X axis value (-1.0 to 1.0)
            y: Y axis value (-1.0 to 1.0)

        Returns:
            LiveActionGroup: Self for method chaining
        """
        self._execute_live_action("holdrightstick", x=x, y=y)
        return self

    def cancel_right_stick(self) -> 'LiveActionGroup':
        """Cancel/zero out the right stick."""
        self._execute_live_action("cancelrightstick")
        return self

    def flick_right_stick(self, x: float, y: float) -> 'LiveActionGroup':
        """
        Quick flick of right stick (50ms, executes immediately).

        Args:
            x: X axis value (-1.0 to 1.0)
            y: Y axis value (-1.0 to 1.0)

        Returns:
            LiveActionGroup: Self for method chaining
        """
        self._execute_live_action("flickrightstick", x=x, y=y)
        return self

    # Trigger methods

    def press_left_trigger(self) -> 'LiveActionGroup':
        """Press left trigger (100ms hold)."""
        self._execute_live_action("presslefttrigger")
        return self

    def hold_left_trigger(self) -> 'LiveActionGroup':
        """
        Hold left trigger (continues until cancel_hold_left_trigger() is called).

        Returns:
            LiveActionGroup: Self for method chaining
        """
        self._execute_live_action("holdlefttrigger")
        return self

    def cancel_hold_left_trigger(self) -> 'LiveActionGroup':
        """Cancel/zero out the left trigger."""
        self._execute_live_action("cancelholdlefttrigger")
        return self

    def press_right_trigger(self) -> 'LiveActionGroup':
        """Press right trigger (100ms hold)."""
        self._execute_live_action("pressrighttrigger")
        return self

    def hold_right_trigger(self) -> 'LiveActionGroup':
        """
        Hold right trigger (continues until cancel_hold_right_trigger() is called).

        Returns:
            LiveActionGroup: Self for method chaining
        """
        self._execute_live_action("holdrighttrigger")
        return self

    def cancel_hold_right_trigger(self) -> 'LiveActionGroup':
        """Cancel/zero out the right trigger."""
        self._execute_live_action("cancelholdrighttrigger")
        return self
