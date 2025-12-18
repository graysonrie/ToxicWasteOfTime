"""
Start recording controller inputs from a physical Xbox controller.

Usage:
    python start_recording.py <name> [description]

Arguments:
    name: Unique name for the recording
    description: Optional description of what the recording does
"""

import sys
from xbox_api import Xbox360ControllerAPI


def main():
    if len(sys.argv) < 2:
        print("Usage: python start_recording.py <name> [description]")
        print("Example: python start_recording.py jump_sequence 'Performs a jump while moving'")
        sys.exit(1)

    name = sys.argv[1]
    description = sys.argv[2] if len(sys.argv) > 2 else ""

    api = Xbox360ControllerAPI(base_url="http://localhost:5000")

    print(f"Starting recording '{name}'...")
    if description:
        print(f"Description: {description}")

    success = api.start_recording(name, description)

    if success:
        print(f"Recording started successfully!")
        print("Use your physical Xbox controller now. Press the View button to stop recording.")
    else:
        print("Failed to start recording.")
        sys.exit(1)


if __name__ == "__main__":
    main()
