"""
Example of using the controller recording and playback feature.
"""

from xbox_api import Xbox360ControllerAPI
import time


def main():
    api = Xbox360ControllerAPI(base_url="http://localhost:5000")

    # Example 1: Record controller inputs
    print("Starting recording...")
    api.start_recording("jump_and_move", "Performs a jump while moving forward")

    print("Recording started! Use your physical Xbox controller now.")
    print("The system will capture all your inputs...")
    time.sleep(10)  # Record for 10 seconds (or however long you want)

    print("Ending recording...")
    recording_info = api.end_recording()
    if recording_info:
        print(f"Recording saved: {recording_info['Name']}")
        print(f"  Description: {recording_info.get('Description', 'N/A')}")
        print(f"  Duration: {recording_info.get('DurationMs', 0) / 1000:.2f} seconds")
        print(f"  Events: {recording_info.get('EventCount', 0)}")

    # Example 2: List all recordings
    print("\nListing all recordings...")
    recordings = api.list_recordings()
    for rec in recordings:
        print(f"  - {rec['Name']}: {rec.get('Description', 'No description')}")
        print(f"    Duration: {rec.get('DurationMs', 0) / 1000:.2f}s, Events: {rec.get('EventCount', 0)}")

    # Example 3: Play back a recording
    if recordings:
        print(f"\nPlaying back '{recordings[0]['Name']}'...")
        success = api.invoke_recording(recordings[0]['Name'])
        if success:
            print("Playback completed!")
        else:
            print("Playback failed!")


if __name__ == "__main__":
    main()
