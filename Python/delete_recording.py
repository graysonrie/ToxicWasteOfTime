"""
Delete a saved controller recording.

Usage:
    python delete_recording.py <name>

Arguments:
    name: Name of the recording to delete
"""

import sys
from xbox_api import Xbox360ControllerAPI


def main():
    if len(sys.argv) < 2:
        print("Usage: python delete_recording.py <name>")
        print("Example: python delete_recording.py jump_sequence")
        sys.exit(1)

    name = sys.argv[1]

    api = Xbox360ControllerAPI(base_url="http://localhost:5000")

    print(f"Deleting recording '{name}'...")

    success = api.delete_recording(name)

    if success:
        print(f"Recording '{name}' deleted successfully!")
    else:
        print(f"Failed to delete recording '{name}'.")
        print("Make sure the recording exists. Use 'python list_recordings.py' to see available recordings.")
        sys.exit(1)


if __name__ == "__main__":
    main()
