"""
Test script for pathfinding system.
"""
import time
from xbox_api import Xbox360ControllerAPI
from pathfinding import ControllerPathfinder


def test_pathfinding(
    image_path: str,
    initial_direction: float,
    move_speed: float = 1.0,
    look_sensitivity: float = 1.0,
    unit_scale: float = 1.0,
    execute: bool = True
):
    """
    Test the pathfinding system.

    Args:
        image_path: Path to the map image (e.g., "Assets/target_1.png")
        initial_direction: Initial facing direction in degrees (0=east, 90=north, 180=west, 270=south)
        move_speed: Character movement speed multiplier
        look_sensitivity: Look sensitivity multiplier
        unit_scale: Scale factor for converting pixels to game units
        execute: If True, execute the controller inputs; if False, just plan the path
    """
    print("=" * 60)
    print("Pathfinding Test")
    print("=" * 60)
    print(f"Image: {image_path}")
    print(f"Initial Direction: {initial_direction}°")
    print(f"Move Speed: {move_speed}")
    print(f"Look Sensitivity: {look_sensitivity}")
    print(f"Unit Scale: {unit_scale}")
    print()

    # Initialize API
    api = Xbox360ControllerAPI(base_url="http://localhost:5000")

    # Create pathfinder
    try:
        pathfinder = ControllerPathfinder(
            api=api,
            image_path=image_path,
            initial_direction=initial_direction,
            move_speed=move_speed,
            look_sensitivity=look_sensitivity,
            unit_scale=unit_scale
        )
    except Exception as e:
        print(f"Error initializing pathfinder: {e}")
        return

    # Visualize path
    print("Generating path visualization...")
    pathfinder.visualize_path("path_visualization.png")

    # Get path info
    path = pathfinder.get_path()
    if path:
        print(f"Path found: {len(path)} waypoints")
        print(f"Start: {path[0]}")
        print(f"End: {path[-1]}")
    else:
        print("No path found!")
        return

    if execute:
        print("\nWaiting 3 seconds before executing...")
        print("(Make sure the game is ready!)")
        time.sleep(3)

        # Execute pathfinding
        success = pathfinder.plan_and_execute()

        if success:
            print("\nPathfinding execution completed!")
        else:
            print("\nPathfinding execution failed!")
    else:
        print("\nPath planning complete (not executing)")
        print("Set execute=True to run the controller inputs")


if __name__ == "__main__":
    # Test with default values
    # Adjust these parameters as needed
    test_pathfinding(
        image_path="Assets/target_1.png",
        initial_direction=90,      # 0 = facing north
        move_speed=1.0,            # Adjust based on character speed
        look_sensitivity=1.0,      # Adjust based on game sensitivity
        unit_scale=1.0,            # Pixels per game unit
        execute=True               # Set to False to just visualize
    )

# invoke with python pathfinding_test.py