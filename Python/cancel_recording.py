"""
Cancel any currently playing recording and zero out all controller inputs.

Usage:
    python cancel_recording.py
"""

from xbox_api import Xbox360ControllerAPI


def main():
    api = Xbox360ControllerAPI(base_url="http://localhost:5000")

    print("Cancelling playback...")

    success = api.cancel_playback()

    if success:
        print("Playback cancelled successfully! All inputs have been zeroed out.")
    else:
        print("Failed to cancel playback.")
        print("This may mean no playback was active.")
        exit(1)


if __name__ == "__main__":
    main()
