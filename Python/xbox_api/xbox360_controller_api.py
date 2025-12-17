from .action_group import ActionGroup


class Xbox360ControllerAPI:
    """
    Intuitive API wrapper for Xbox 360 controller input execution.
    Supports sequential and parallel input sequences with automatic detection.
    """

    def __init__(self, base_url: str = "http://localhost:5000"):
        """
        Initialize the Xbox 360 Controller API client.

        Args:
            base_url: Base URL of the C# Web API (default: http://localhost:5000)
        """
        self.base_url = base_url.rstrip('/')
        self.api_url = f"{self.base_url}/api/xbox"

    def record_actions(self) -> ActionGroup:
        """
        Creates a new action group for recording and executing controller inputs.

        Returns:
            ActionGroup: A new action group instance
        """
        return ActionGroup(self.api_url)

    @staticmethod
    def normalize_stick_value(value: float) -> int:
        """
        Converts a normalized stick value (-1 to 1) to a short value (-32768 to 32767).

        Args:
            value: Normalized stick value between -1.0 and 1.0

        Returns:
            int: Short value between -32768 and 32767
        """
        # Clamp value to -1 to 1 range
        value = max(-1.0, min(1.0, value))
        # Convert to short range
        return int(value * 32767)
