"""
Play back a saved controller recording repeatedly until interrupted.

Usage:
    python invoke_recording_repeat.py <name>

Arguments:
    name: Name of the recording to play back repeatedly

Press CTRL-C to stop playback and exit.
"""

import sys
import time
import signal
from xbox_api import Xbox360ControllerAPI


def main():
    if len(sys.argv) < 2:
        print("Usage: python invoke_recording_repeat.py <name>")
        print("Example: python invoke_recording_repeat.py jump_sequence")
        print("\nPress CTRL-C to stop playback and exit.")
        sys.exit(1)

    name = sys.argv[1]
    api = Xbox360ControllerAPI(base_url="http://localhost:5000")

    # Flag to track if we should continue
    should_continue = True

    def signal_handler(sig, frame):
        """Handle CTRL-C gracefully"""
        nonlocal should_continue
        print("\n\nInterrupt received. Cancelling playback and exiting...")
        should_continue = False
        # Cancel any active playback
        api.cancel_playback()
        sys.exit(0)

    # Register signal handler for CTRL-C
    signal.signal(signal.SIGINT, signal_handler)

    print(f"Starting repeated playback of '{name}'...")
    print("Press CTRL-C to stop.\n")

    playback_count = 0

    try:
        while should_continue:
            playback_count += 1
            print(f"Playback #{playback_count} of '{name}'...", end=" ", flush=True)

            # Start playback (blocking - waits for completion)
            # Note: CTRL-C will not work during playback, only between playbacks
            try:
                success = api.invoke_recording(name, wait_for_completion=True)
            except KeyboardInterrupt:
                # Handle CTRL-C if it somehow gets through
                signal_handler(None, None)
                break

            if success:
                print("Completed")
            else:
                print("Failed")
                print(f"Failed to play back recording '{name}'.")
                print("Make sure the recording exists. Use 'python list_recordings.py' to see available recordings.")
                break

            if not should_continue:
                break

            # Sleep for 1 second before next playback (CTRL-C works here)
            if should_continue:
                try:
                    time.sleep(1)
                except KeyboardInterrupt:
                    # Handle CTRL-C during sleep
                    signal_handler(None, None)
                    break

    except KeyboardInterrupt:
        # Fallback handler in case signal handler doesn't work
        print("\n\nInterrupt received. Cancelling playback and exiting...")
        api.cancel_playback()
        sys.exit(0)
    except Exception as e:
        print(f"\nError during playback: {e}")
        api.cancel_playback()
        sys.exit(1)


if __name__ == "__main__":
    main()
