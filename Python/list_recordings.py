"""
List all saved controller recordings.

Usage:
    python list_recordings.py
"""

from xbox_api import Xbox360ControllerAPI


def main():
    api = Xbox360ControllerAPI(base_url="http://localhost:5000")

    print("Fetching recordings...")
    recordings = api.list_recordings()

    if not recordings:
        print("No recordings found.")
        return

    print(f"\nFound {len(recordings)} recording(s):\n")

    for i, rec in enumerate(recordings, 1):
        print(f"{i}. {rec['Name']}")
        if rec.get('Description'):
            print(f"   Description: {rec['Description']}")

        duration = rec.get('DurationMs', 0) / 1000.0
        event_count = rec.get('EventCount', 0)
        created_at = rec.get('CreatedAt', 'Unknown')

        print(f"   Duration: {duration:.2f} seconds")
        print(f"   Events: {event_count}")
        print(f"   Created: {created_at}")
        print()


if __name__ == "__main__":
    main()
