import win32gui
import win32ui
import win32con
from PIL import Image
import ctypes
import time
import traceback


def find_window_by_title(title_contains: str) -> list[tuple[int, str]]:
    """
    Find a window by partial title match.
    Returns a list of tuples containing (window handle, window title).
    """
    def enum_windows_callback(hwnd: int, windows: list[tuple[int, str]]) -> bool:
        if win32gui.IsWindowVisible(hwnd):
            window_title = win32gui.GetWindowText(hwnd)
            if title_contains.lower() in window_title.lower():
                windows.append((hwnd, window_title))
        return True

    windows: list[tuple[int, str]] = []
    win32gui.EnumWindows(enum_windows_callback, windows)
    return windows


def capture_window_screenshot(hwnd: int) -> Image.Image | None:
    """
    Capture a screenshot of a specific window using PrintWindow API.
    This method works better with modern Windows apps and hardware-accelerated content.

    Args:
        hwnd: Window handle

    Returns:
        PIL Image object if successful, None otherwise
    """
    try:
        # Get window dimensions
        left, top, right, bottom = win32gui.GetWindowRect(hwnd)
        width = right - left
        height = bottom - top

        # Ensure window is not minimized
        if win32gui.IsIconic(hwnd):
            _ = win32gui.ShowWindow(hwnd, win32con.SW_RESTORE)
            time.sleep(0.1)  # Give window time to restore

        # Create device context for screen
        hwnd_dc = win32gui.GetWindowDC(hwnd)
        mfc_dc = win32ui.CreateDCFromHandle(hwnd_dc)
        save_dc = mfc_dc.CreateCompatibleDC()

        # Create bitmap
        bitmap = win32ui.CreateBitmap()
        bitmap.CreateCompatibleBitmap(mfc_dc, width, height)
        _ = save_dc.SelectObject(bitmap)

        # Use PrintWindow instead of BitBlt - works better with modern apps
        # PW_RENDERFULLCONTENT (0x2) ensures we get the full content even if window is layered
        user32 = ctypes.windll.user32
        PW_RENDERFULLCONTENT = 0x2

        result = int(user32.PrintWindow(hwnd, save_dc.GetSafeHdc(), PW_RENDERFULLCONTENT))  # type: ignore  # pyright: ignore[reportAny]

        if not result:
            # Fallback: try without PW_RENDERFULLCONTENT flag
            result = int(user32.PrintWindow(hwnd, save_dc.GetSafeHdc(), 0x0))  # type: ignore  # pyright: ignore[reportAny]
            if not result:
                # Last resort: try BitBlt
                save_dc.BitBlt((0, 0), (width, height), mfc_dc, (0, 0), win32con.SRCCOPY)

        # Convert to PIL Image
        bmp_info = bitmap.GetInfo()  # type: ignore  # pyright: ignore[reportUnknownMemberType, reportUnknownVariableType]
        bmp_str = bitmap.GetBitmapBits(True)
        width_val = int(bmp_info['bmWidth'])  # type: ignore  # pyright: ignore[reportUnknownArgumentType]
        height_val = int(bmp_info['bmHeight'])  # type: ignore  # pyright: ignore[reportUnknownArgumentType]
        img = Image.frombuffer(
            'RGB',
            (width_val, height_val),
            bmp_str, 'raw', 'BGRX', 0, 1
        )

        # Cleanup
        win32gui.DeleteObject(bitmap.GetHandle())
        save_dc.DeleteDC()
        mfc_dc.DeleteDC()
        _ = win32gui.ReleaseDC(hwnd, hwnd_dc)

        return img
    except Exception as e:
        print(f"Error capturing screenshot: {e}")
        traceback.print_exc()
        return None


def get_raw_xbox_app_screenshot(save_to_file: bool = False, output_path: str = "xbox_screenshot.png") -> Image.Image | None:
    """
    Capture a screenshot of the Xbox app window.

    Args:
        save_to_file: If True, save the screenshot to a file
        output_path: Path to save the screenshot (only used if save_to_file is True)

    Returns:
        PIL Image object if successful, None if window not found or capture failed
    """
    # Try to find Xbox app window
    # Common titles: "Xbox", "Xbox Console Companion", "Xbox Game Bar", etc.
    search_terms = ["Xbox", "Xbox Console", "Xbox Game"]

    found_windows: list[tuple[int, str]] = []

    for term in search_terms:
        windows = find_window_by_title(term)
        found_windows.extend(windows)

    if not found_windows:
        print("No Xbox app window found.")
        return None

    # Use the first found window
    hwnd, title = found_windows[0]
    print(f"Capturing screenshot of: {title}")

    img = capture_window_screenshot(hwnd)

    if img is not None and save_to_file:
        img.save(output_path)
        print(f"Screenshot saved to: {output_path}")

    return img
