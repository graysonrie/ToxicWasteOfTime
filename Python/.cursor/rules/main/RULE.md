---
alwaysApply: true
---
This project is a test to see if a Python robot can control an offline third-person shooter using Xbox inputs. This project is specifically for research purposes only.
Use the xbox_api module to make inputs. The control scheme is:
Left Stick moves the character
Right Stick is used to look around
A is used to jump
Right Trigger shoots the weapon

Note that if the Left Stick is held forward, the character moves in the direction they are facing, if it is held to the right or left, they will strafe, etc.

The 'target' images in the Assets folder represent top-down views of the game, where giving a spawn point (blue square), wall collisions (red lines) and a target (yellow square), and given what direction the player is initially facing in relation to the image (example: 0 is facing east, 90 is facing north, 180 is west, etc) the program should map out the inputs needed to get to that point