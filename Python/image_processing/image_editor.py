from PIL import Image


def _is_black_pixel(pixel: tuple[int, ...] | int | float, threshold: int) -> bool:
    """Check if a pixel is considered black (all channels below threshold)."""
    if isinstance(pixel, (int, float)):
        # Grayscale mode
        return int(pixel) < threshold
    # RGB/RGBA mode - check if all channels are below threshold
    return all(int(channel) < threshold for channel in pixel[:3])


def extract_inner_image(image: Image.Image, threshold: int = 30, min_content_ratio: float = 0.1) -> Image.Image:
    """
    Extract the inner image by removing black borders/margins.

    Args:
        image: PIL Image with black borders
        threshold: RGB threshold for considering a pixel as "black" (0-255, default: 30)
                  Pixels with all RGB values below this threshold are considered black.
        min_content_ratio: Minimum ratio of non-black pixels in a row/column to consider it as content (0.0-1.0, default: 0.1)
                          This helps ignore small UI elements like window buttons.

    Returns:
        Cropped PIL Image with black borders removed
    """
    # Convert to RGB if needed
    if image.mode not in ('RGB', 'RGBA', 'L'):
        image = image.convert('RGB')

    width, height = image.size
    pixels = image.load()

    # Find top border - require significant content (not just a few pixels)
    top = 0
    for y in range(height):
        non_black_count = 0
        for x in range(width):
            pixel = pixels[x, y]  # type: ignore
            if not _is_black_pixel(pixel, threshold):  # type: ignore
                non_black_count += 1
        # Consider it content if enough pixels are non-black
        if non_black_count >= width * min_content_ratio:
            top = y
            break
    else:
        # Entire image is black
        return image

    # Find bottom border
    bottom = height
    for y in range(height - 1, -1, -1):
        non_black_count = 0
        for x in range(width):
            pixel = pixels[x, y]  # type: ignore
            if not _is_black_pixel(pixel, threshold):  # type: ignore
                non_black_count += 1
        if non_black_count >= width * min_content_ratio:
            bottom = y + 1
            break

    # Find left border
    left = 0
    for x in range(width):
        non_black_count = 0
        for y in range(height):
            pixel = pixels[x, y]  # type: ignore
            if not _is_black_pixel(pixel, threshold):  # type: ignore
                non_black_count += 1
        if non_black_count >= height * min_content_ratio:
            left = x
            break

    # Find right border
    right = width
    for x in range(width - 1, -1, -1):
        non_black_count = 0
        for y in range(height):
            pixel = pixels[x, y]  # type: ignore
            if not _is_black_pixel(pixel, threshold):  # type: ignore
                non_black_count += 1
        if non_black_count >= height * min_content_ratio:
            right = x + 1
            break

    # Crop the image to the bounding box
    cropped = image.crop((left, top, right, bottom))

    return cropped