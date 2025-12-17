import os
import time
from image_processing.xbox_screenshot import get_raw_xbox_app_screenshot
from image_processing.image_editor import extract_inner_image


def main() -> None:
    print("Searching for Xbox app window...")
    time.sleep(1)

    # Capture screenshot (don't save the original with borders)
    img = get_raw_xbox_app_screenshot(save_to_file=False)

    if img is not None:
        print(f"Original image size: {img.size}")

        # Extract inner image (remove black borders)
        print("Extracting inner image (removing black borders)...")
        cropped_img = extract_inner_image(img, threshold=30)

        # Save the cropped image
        output_path = "xbox_screenshot_cropped.png"
        cropped_img.save(output_path)

        print(f"Success! Cropped screenshot saved to: {os.path.abspath(output_path)}")
        print(f"Cropped image size: {cropped_img.size}")
    else:
        print("Failed to capture screenshot.")


if __name__ == "__main__":
    main()
