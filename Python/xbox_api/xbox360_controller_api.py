from .action_group import ActionGroup
from .live_action_group import LiveActionGroup
import requests


class Xbox360ControllerAPI:
    """
    Intuitive API wrapper for Xbox 360 controller input execution.
    Supports sequential and parallel input sequences with automatic detection.
    Also supports live actions that execute immediately.
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

    def live_actions(self) -> LiveActionGroup:
        """
        Creates a new live action group for executing controller inputs in real-time.
        Actions execute immediately when called, without needing execute().

        Returns:
            LiveActionGroup: A new live action group instance
        """
        return LiveActionGroup(self.api_url)

    def start_recording(self, name: str, description: str = "") -> bool:
        """
        Start recording controller inputs from the physical controller.

        Args:
            name: Unique name for the recording
            description: Optional description of what the recording does

        Returns:
            bool: True if recording started successfully, False otherwise
        """
        try:
            response = requests.post(
                f"{self.api_url}/recording/start",
                json={"Name": name, "Description": description},
                timeout=5
            )
            response.raise_for_status()
            result = response.json()

            # Check for Success field explicitly
            if "Success" in result:
                success = result["Success"]
                if not success:
                    message = result.get("Message", "Unknown error")
                    print(f"Recording failed: {message}")
                return bool(success)

            # If Success field is missing but we got a 200 response, assume success
            # (This handles edge cases where the response format might differ)
            if response.status_code == 200:
                return True

            return False
        except requests.exceptions.RequestException as e:
            print(f"Error starting recording: {e}")
            if hasattr(e, 'response') and e.response is not None:
                try:
                    error_detail = e.response.json()
                    print(f"Error details: {error_detail}")
                except:
                    print(f"Error response text: {e.response.text}")
            return False

    def end_recording(self) -> dict | None:
        """
        End the current recording and save it to the database.

        Returns:
            dict: Recording information if successful, None otherwise
        """
        try:
            response = requests.post(
                f"{self.api_url}/recording/end",
                timeout=5
            )
            response.raise_for_status()
            result = response.json()
            if result.get("Success", False):
                return result.get("Recording")
            return None
        except requests.exceptions.RequestException as e:
            print(f"Error ending recording: {e}")
            return None

    def list_recordings(self) -> list[dict]:
        """
        List all saved recordings.

        Returns:
            list: List of recording information dictionaries
        """
        try:
            response = requests.get(
                f"{self.api_url}/recording/list",
                timeout=5
            )
            response.raise_for_status()
            result = response.json()
            if result.get("Success", False):
                return result.get("Recordings", [])
            return []
        except requests.exceptions.RequestException as e:
            print(f"Error listing recordings: {e}")
            return []

    def invoke_recording(self, name: str) -> bool:
        """
        Play back a saved recording on the virtual controller.

        Args:
            name: Name of the recording to play back

        Returns:
            bool: True if playback started successfully, False otherwise
        """
        try:
            response = requests.post(
                f"{self.api_url}/recording/playback/{name}",
                timeout=300  # Longer timeout for playback
            )
            response.raise_for_status()
            result = response.json()
            return result.get("Success", False)
        except requests.exceptions.RequestException as e:
            print(f"Error invoking recording: {e}")
            return False

    def delete_recording(self, name: str) -> bool:
        """
        Delete a saved recording from the database.

        Args:
            name: Name of the recording to delete

        Returns:
            bool: True if recording was deleted successfully, False if not found or error occurred
        """
        try:
            response = requests.delete(
                f"{self.api_url}/recording/{name}",
                timeout=5
            )
            response.raise_for_status()
            result = response.json()

            # Check for Success field explicitly
            if "Success" in result:
                success = result["Success"]
                if not success:
                    message = result.get("Message", "Unknown error")
                    print(f"Delete failed: {message}")
                return bool(success)

            # If Success field is missing but we got a 200 response, assume success
            if response.status_code == 200:
                return True

            return False
        except requests.exceptions.HTTPError as e:
            if e.response.status_code == 404:
                print(f"Recording '{name}' not found.")
            else:
                print(f"Error deleting recording: {e}")
            return False
        except requests.exceptions.RequestException as e:
            print(f"Error deleting recording: {e}")
            return False

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
