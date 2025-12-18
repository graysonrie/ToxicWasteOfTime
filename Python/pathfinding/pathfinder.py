"""
Pathfinding system for navigating from spawn to target in top-down map images.
"""
import math
from typing import Tuple, List, Optional
from PIL import Image
import numpy as np


class Pathfinder:
    """
    Pathfinding system that reads a top-down map image and generates controller inputs
    to navigate from spawn (blue square) to target (yellow square) avoiding walls (red lines).
    """

    def __init__(
        self,
        image_path: str,
        initial_direction: float,
        move_speed: float = 1.0,
        look_sensitivity: float = 1.0,
        unit_scale: float = 1.0
    ):
        """
        Initialize the pathfinder.

        Args:
            image_path: Path to the map image
            initial_direction: Initial facing direction in degrees (0=east, 90=north, 180=west, 270=south)
            move_speed: Character movement speed multiplier (affects how long to hold stick)
            look_sensitivity: Look sensitivity multiplier (affects right stick movement)
            unit_scale: Scale factor for converting pixels to game units
        """
        self.image_path = image_path
        self.initial_direction = math.radians(initial_direction)
        self.move_speed = move_speed
        self.look_sensitivity = look_sensitivity
        self.unit_scale = unit_scale

        # Load and parse the image
        self.image = Image.open(image_path).convert('RGB')
        self.width, self.height = self.image.size
        self.pixels = np.array(self.image)

        # Extract map elements
        self.spawn_pos = self._find_spawn()
        self.target_pos = self._find_target()
        self.walls = self._find_walls()

        # Create collision map
        self.collision_map = self._create_collision_map()

    def _find_spawn(self) -> Tuple[int, int]:
        """Find the blue square (spawn position)."""
        # Blue is typically (0, 0, 255) or similar
        blue_mask = (self.pixels[:, :, 2] > 200) & (self.pixels[:, :, 0] < 100) & (self.pixels[:, :, 1] < 100)
        blue_coords = np.where(blue_mask)

        if len(blue_coords[0]) == 0:
            raise ValueError("Could not find blue spawn square in image")

        # Return center of blue region
        y_center = int(np.mean(blue_coords[0]))
        x_center = int(np.mean(blue_coords[1]))
        return (x_center, y_center)

    def _find_target(self) -> Tuple[int, int]:
        """Find the yellow square (target position)."""
        # Yellow is typically high red and green, low blue
        yellow_mask = (self.pixels[:, :, 0] > 200) & (self.pixels[:, :, 1] > 200) & (self.pixels[:, :, 2] < 100)
        yellow_coords = np.where(yellow_mask)

        if len(yellow_coords[0]) == 0:
            raise ValueError("Could not find yellow target square in image")

        # Return center of yellow region
        y_center = int(np.mean(yellow_coords[0]))
        x_center = int(np.mean(yellow_coords[1]))
        return (x_center, y_center)

    def _find_walls(self) -> List[Tuple[int, int]]:
        """Find red lines (walls)."""
        # Red is typically (255, 0, 0) or similar
        red_mask = (self.pixels[:, :, 0] > 200) & (self.pixels[:, :, 1] < 100) & (self.pixels[:, :, 2] < 100)
        red_coords = np.where(red_mask)

        # Return list of wall pixel coordinates
        walls = list(zip(red_coords[1], red_coords[0]))  # (x, y)
        return walls

    def _create_collision_map(self) -> np.ndarray:
        """Create a binary collision map (True = wall, False = walkable)."""
        collision = np.zeros((self.height, self.width), dtype=bool)

        # Mark wall pixels
        for x, y in self.walls:
            if 0 <= x < self.width and 0 <= y < self.height:
                collision[y, x] = True

        # Expand walls slightly to account for character size
        # Simple dilation without scipy
        for _ in range(3):
            expanded = collision.copy()
            for y in range(1, self.height - 1):
                for x in range(1, self.width - 1):
                    if collision[y, x]:
                        # Expand to neighbors
                        for dy in [-1, 0, 1]:
                            for dx in [-1, 0, 1]:
                                if 0 <= y + dy < self.height and 0 <= x + dx < self.width:
                                    expanded[y + dy, x + dx] = True
            collision = expanded

        return collision

    def _heuristic(self, a: Tuple[int, int], b: Tuple[int, int]) -> float:
        """Calculate Manhattan distance heuristic."""
        return abs(a[0] - b[0]) + abs(a[1] - b[1])

    def _get_neighbors(self, pos: Tuple[int, int]) -> List[Tuple[int, int]]:
        """Get walkable neighbors of a position."""
        x, y = pos
        neighbors = []

        # 8-directional movement
        for dx in [-1, 0, 1]:
            for dy in [-1, 0, 1]:
                if dx == 0 and dy == 0:
                    continue

                nx, ny = x + dx, y + dy
                if 0 <= nx < self.width and 0 <= ny < self.height:
                    if not self.collision_map[ny, nx]:
                        neighbors.append((nx, ny))

        return neighbors

    def find_path(self) -> List[Tuple[int, int]]:
        """
        Find path from spawn to target using A* algorithm.

        Returns:
            List of (x, y) coordinates representing the path
        """
        start = self.spawn_pos
        goal = self.target_pos

        # A* algorithm
        open_set = {start}
        came_from = {}
        g_score = {start: 0}
        f_score = {start: self._heuristic(start, goal)}

        while open_set:
            # Find node with lowest f_score
            current = min(open_set, key=lambda x: f_score.get(x, float('inf')))

            if current == goal:
                # Reconstruct path
                path = []
                while current in came_from:
                    path.append(current)
                    current = came_from[current]
                path.append(start)
                path.reverse()
                return path

            open_set.remove(current)

            for neighbor in self._get_neighbors(current):
                tentative_g = g_score[current] + self._heuristic(current, neighbor)

                if neighbor not in g_score or tentative_g < g_score[neighbor]:
                    came_from[neighbor] = current
                    g_score[neighbor] = tentative_g
                    f_score[neighbor] = tentative_g + self._heuristic(neighbor, goal)
                    open_set.add(neighbor)

        # No path found
        return []

    def _normalize_angle(self, angle: float) -> float:
        """Normalize angle to [-π, π]."""
        while angle > math.pi:
            angle -= 2 * math.pi
        while angle < -math.pi:
            angle += 2 * math.pi
        return angle

    def _calculate_move_input(
        self,
        current_pos: Tuple[int, int],
        target_pos: Tuple[int, int],
        current_facing: float
    ) -> Tuple[float, float]:
        """
        Calculate left stick input to move from current_pos to target_pos.

        The left stick moves relative to the character's facing direction:
        - Forward (0, 1) moves in the direction the character is facing
        - Right (1, 0) strafes right relative to facing
        - Back (0, -1) moves backward
        - Left (-1, 0) strafes left

        Args:
            current_pos: Current position (x, y)
            target_pos: Target position (x, y)
            current_facing: Current facing direction in radians

        Returns:
            (x, y) stick values in range [-1, 1]
        """
        # Calculate direction to target in world coordinates
        dx = target_pos[0] - current_pos[0]
        dy = target_pos[1] - current_pos[1]

        if dx == 0 and dy == 0:
            return (0.0, 0.0)

        # In image coordinates, y increases downward, so we invert it for math coordinates
        # Image: (0,0) top-left, y increases down
        # Math: (0,0) center, y increases up
        target_angle = math.atan2(-dy, dx)  # Negative dy because image y is inverted

        # Calculate relative angle from current facing
        # This tells us which direction to move relative to where we're facing
        relative_angle = self._normalize_angle(target_angle - current_facing)

        # Convert to stick input
        # Forward movement = cos(relative_angle) when relative_angle is 0
        # Strafe right = sin(relative_angle) when relative_angle is 90 degrees
        stick_x = math.sin(relative_angle)  # Left/right strafe
        stick_y = math.cos(relative_angle)   # Forward/backward movement

        # Normalize to ensure we don't exceed 1.0
        magnitude = math.sqrt(stick_x**2 + stick_y**2)
        if magnitude > 1.0:
            stick_x /= magnitude
            stick_y /= magnitude

        return (stick_x, stick_y)

    def _calculate_look_input(
        self,
        current_facing: float,
        target_facing: float
    ) -> Tuple[float, float]:
        """
        Calculate right stick input to look from current_facing to target_facing.

        Args:
            current_facing: Current facing direction in radians
            target_facing: Target facing direction in radians

        Returns:
            (x, y) stick values in range [-1, 1]
        """
        # Calculate angle difference
        angle_diff = self._normalize_angle(target_facing - current_facing)

        # Convert to stick input (x = horizontal, y = vertical)
        # In game: positive x = look right, positive y = look up
        # But in top-down: we need to map angle to stick
        stick_x = math.sin(angle_diff) * self.look_sensitivity
        stick_y = -math.cos(angle_diff) * self.look_sensitivity  # Negative because y is inverted in screen coords

        # Clamp to [-1, 1]
        stick_x = max(-1.0, min(1.0, stick_x))
        stick_y = max(-1.0, min(1.0, stick_y))

        return (stick_x, stick_y)

    def generate_controller_inputs(self, path: List[Tuple[int, int]], step_time_ms: int = 50) -> List[dict]:
        """
        Generate controller inputs to follow the path.

        Args:
            path: List of (x, y) positions to follow
            step_time_ms: Time in milliseconds for each step

        Returns:
            List of controller input dictionaries
        """
        if len(path) < 2:
            return []

        inputs = []
        current_facing = self.initial_direction

        for i in range(len(path) - 1):
            current_pos = path[i]
            next_pos = path[i + 1]

            # Calculate direction to next position
            dx = next_pos[0] - current_pos[0]
            dy = next_pos[1] - current_pos[1]
            target_facing = math.atan2(-dy, dx)  # Negative dy for image coordinates

            # Calculate how long to move (based on distance and speed)
            distance = math.sqrt(dx**2 + dy**2)
            move_duration = int((distance / self.unit_scale) / self.move_speed * 1000)  # Convert to ms
            move_duration = max(step_time_ms, move_duration)  # Minimum step time

            # Calculate move input (left stick)
            move_x, move_y = self._calculate_move_input(current_pos, next_pos, current_facing)

            # Calculate look input (right stick) - look toward target
            look_x, look_y = self._calculate_look_input(current_facing, target_facing)

            # Add natural looking behavior - slight random variation
            import random
            look_variation = 0.1
            look_x += random.uniform(-look_variation, look_variation)
            look_y += random.uniform(-look_variation, look_variation)
            look_x = max(-1.0, min(1.0, look_x))
            look_y = max(-1.0, min(1.0, look_y))

            # Store input
            inputs.append({
                'move': (move_x, move_y),
                'look': (look_x, look_y),
                'duration': move_duration,
                'target_facing': target_facing
            })

            # Update current facing
            current_facing = target_facing

        return inputs
