"""
Play back a saved controller recording.

Usage:
    python invoke_recording.py <name>

Arguments:
    name: Name of the recording to play back
"""

import sys
from xbox_api import Xbox360ControllerAPI


def main():
    if len(sys.argv) < 2:
        print("Usage: python invoke_recording.py <name>")
        print("Example: python invoke_recording.py jump_sequence")
        sys.exit(1)

    name = sys.argv[1]

    api = Xbox360ControllerAPI(base_url="http://localhost:5000")

    print(f"Playing back recording '{name}'...")

    success = api.invoke_recording(name)

    if success:
        print("Playback completed successfully!")
    else:
        print(f"Failed to play back recording '{name}'.")
        print("Make sure the recording exists. Use 'python list_recordings.py' to see available recordings.")
        sys.exit(1)


if __name__ == "__main__":
    main()
