"""
Controller pathfinder that integrates pathfinding with Xbox controller API.
"""
from typing import List
from .pathfinder import Pathfinder
from xbox_api import Xbox360ControllerAPI


class ControllerPathfinder:
    """
    High-level interface for pathfinding and executing controller inputs.
    """

    def __init__(
        self,
        api: Xbox360ControllerAPI,
        image_path: str,
        initial_direction: float,
        move_speed: float = 1.0,
        look_sensitivity: float = 1.0,
        unit_scale: float = 1.0
    ):
        """
        Initialize the controller pathfinder.

        Args:
            api: Xbox360ControllerAPI instance
            image_path: Path to the map image
            initial_direction: Initial facing direction in degrees (0=east, 90=north, 180=west, 270=south)
            move_speed: Character movement speed multiplier
            look_sensitivity: Look sensitivity multiplier
            unit_scale: Scale factor for converting pixels to game units
        """
        self.api = api
        self.pathfinder = Pathfinder(
            image_path=image_path,
            initial_direction=initial_direction,
            move_speed=move_speed,
            look_sensitivity=look_sensitivity,
            unit_scale=unit_scale
        )

    def plan_and_execute(self) -> bool:
        """
        Find path and execute controller inputs to navigate to target.

        Returns:
            True if path was found and executed, False otherwise
        """
        # Find path
        path = self.pathfinder.find_path()

        if not path:
            print("No path found from spawn to target!")
            return False

        print(f"Path found with {len(path)} waypoints")

        # Generate controller inputs
        inputs = self.pathfinder.generate_controller_inputs(path)

        if not inputs:
            print("No controller inputs generated!")
            return False

        print(f"Generated {len(inputs)} controller input steps")

        # Execute inputs
        actions = self.api.record_actions()

        for i, input_data in enumerate(inputs):
            move_x, move_y = input_data['move']
            look_x, look_y = input_data['look']
            duration = input_data['duration']

            # Hold both sticks simultaneously for this step
            actions.hold_left_stick(move_x, move_y, duration)
            actions.hold_right_stick(look_x, look_y, duration)

            # Wait after each step to ensure sequential execution
            if i < len(inputs) - 1:
                actions.wait(10)  # Small gap between steps

        # Execute all actions
        actions.execute()
        print("Controller inputs executed!")

        return True

    def get_path(self) -> List[tuple[int, int]]:
        """Get the planned path without executing."""
        return self.pathfinder.find_path()

    def visualize_path(self, output_path: str = "path_visualization.png"):
        """
        Visualize the path on the map image.

        Args:
            output_path: Path to save the visualization
        """
        from PIL import ImageDraw

        path = self.pathfinder.find_path()

        if not path:
            print("No path to visualize!")
            return

        # Create a copy of the image
        vis_image = self.pathfinder.image.copy()
        draw = ImageDraw.Draw(vis_image)

        # Draw path
        for i in range(len(path) - 1):
            start = path[i]
            end = path[i + 1]
            draw.line([start, end], fill=(0, 255, 0), width=3)

        # Draw spawn and target
        spawn = self.pathfinder.spawn_pos
        target = self.pathfinder.target_pos
        draw.ellipse([spawn[0]-5, spawn[1]-5, spawn[0]+5, spawn[1]+5], fill=(0, 0, 255))
        draw.ellipse([target[0]-5, target[1]-5, target[0]+5, target[1]+5], fill=(255, 255, 0))

        vis_image.save(output_path)
        print(f"Path visualization saved to {output_path}")
