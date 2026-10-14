"""
Standalone OpenCV Window Test for MityCAM-120MXS
Target Script: Assets/Python/window_test.py

Loads configuration from Assets/Settings/camera_settings.json

Install: pip install harvesters opencv-python numpy
Run:     python window_test.py
Quit:    Press 'q' or ESC in the preview window
"""

import json
import os
from pathlib import Path
import time
import cv2

# Resolve paths relative to Assets/Settings/camera_settings.json
PROJECT_ROOT = Path(__file__).parent.resolve()
CONFIG_PATH = PROJECT_ROOT / "camera_settings.json"


def load_config():
    """Load settings from Assets/Settings/camera_settings.json with fallback defaults."""
    defaults = {
        "camera": {
            "cti_path": "../Python/bin/GenTL.dll",
            "pixel_format": "BayerRG8",
            "exposure_us": 10000.0,
            "frame_rate": 30.0,
            "gain": None
        },
        "roi": {
            "width": 1920,
            "height": 1080,
            "center": True,
            "offset_x": 0,
            "offset_y": 0
        }
    }

    if CONFIG_PATH.exists():
        try:
            with open(CONFIG_PATH, "r") as f:
                print(f"[window_test] Loaded configuration from {CONFIG_PATH}")
                return json.load(f)
        except Exception as e:
            print(f"[warn] Failed to parse {CONFIG_PATH}: {e}. Using fallback defaults.")
    else:
        print(f"[warn] Config file not found at {CONFIG_PATH}. Using fallback defaults.")

    return defaults


cfg = load_config()

# Resolve GenTL.dll path
CTI_PATH = str((PROJECT_ROOT / cfg["camera"]["cti_path"]).resolve())
BIN_DIR = Path(CTI_PATH).parent

SENSOR_W, SENSOR_H = 13272, 9176

# Ensure GenTL.dll dependencies (libusbK, GenApi) are resolvable
if BIN_DIR.exists():
    os.add_dll_directory(str(BIN_DIR))
    os.chdir(str(BIN_DIR))

from harvesters.core import Harvester  # noqa: E402


def try_set(node_map, name, value):
    """Set a camera node map attribute safely with warning catch."""
    try:
        getattr(node_map, name).value = value
    except Exception as e:
        print(f"[warn] Could not set {name} = {value!r}: {e}")


def configure(n, cfg):
    """Configure camera parameters using loaded JSON settings."""
    c_cfg = cfg["camera"]
    r_cfg = cfg["roi"]

    # Reset offsets first so new width/height are never out-of-bounds rejected
    try_set(n, "OffsetX", 0)
    try_set(n, "OffsetY", 0)

    # Alignment rules: Width must be multiple of 8, Height multiple of 4
    w = max(32, min(r_cfg["width"], SENSOR_W)) // 8 * 8
    h = max(4, min(r_cfg["height"], SENSOR_H)) // 4 * 4

    try_set(n, "Width", w)
    try_set(n, "Height", h)

    if r_cfg["center"]:
        try_set(n, "OffsetX", ((SENSOR_W - w) // 2) // 8 * 8)
        try_set(n, "OffsetY", ((SENSOR_H - h) // 2) // 4 * 4)
    else:
        try_set(n, "OffsetX", r_cfg["offset_x"] // 8 * 8)
        try_set(n, "OffsetY", r_cfg["offset_y"] // 4 * 4)

    try_set(n, "PixelFormat", c_cfg["pixel_format"])
    try_set(n, "AcquisitionMode", "Continuous")
    try_set(n, "TriggerMode", "Off")
    try_set(n, "ExposureTime", c_cfg["exposure_us"])
    try_set(n, "AcquisitionFrameRate", c_cfg["frame_rate"])
    if c_cfg["gain"]:
        try_set(n, "PGAGain", c_cfg["gain"])


def to_display(raw, pixel_format):
    """Convert raw 8-bit frame to BGR format for OpenCV display."""
    if pixel_format.startswith("Bayer"):
        return cv2.cvtColor(raw, cv2.COLOR_BAYER_BG2BGR)
    elif len(raw.shape) == 2:
        return cv2.cvtColor(raw, cv2.COLOR_GRAY2BGR)
    return raw.copy()


def main():
    h = Harvester()
    h.add_file(CTI_PATH)
    h.update()

    if not h.device_info_list:
        print("[window_test] No cameras found. Check CTI path, USB 3 connection, and camera power.")
        h.reset()
        return

    print("Found devices:")
    for i, d in enumerate(h.device_info_list):
        print(f"  [{i}] {d}")

    ia = h.create(0)
    try:
        node_map = ia.remote_device.node_map
        configure(node_map, cfg)
        print(f"Streaming {node_map.Width.value}x{node_map.Height.value} ({node_map.PixelFormat.value})")

        cv2.namedWindow("MityCAM-120MXS Test", cv2.WINDOW_NORMAL)
        cv2.resizeWindow("MityCAM-120MXS Test", 1280, 720)

        ia.start()
        frames, t0, fps = 0, time.time(), 0.0

        while True:
            try:
                buf = ia.fetch(timeout=5)
            except Exception as e:
                print(f"[warn] fetch failed: {e}")
                continue

            with buf:
                comp = buf.payload.components[0]
                raw = comp.data.reshape(comp.height, comp.width)
                img = to_display(raw, node_map.PixelFormat.value)

            frames += 1
            now = time.time()
            if now - t0 >= 1.0:
                fps = frames / (now - t0)
                frames, t0 = 0, now

            # Overlay FPS counter on top-left of preview image
            cv2.putText(
                img,
                f"{fps:.1f} FPS",
                (30, 60),
                cv2.FONT_HERSHEY_SIMPLEX,
                1.2,
                (0, 255, 0),
                3
            )
            cv2.imshow("MityCAM-120MXS Test", img)

            key = cv2.waitKey(1) & 0xFF
            if key in (ord("q"), 27):
                break
            if cv2.getWindowProperty("MityCAM-120MXS Test", cv2.WND_PROP_VISIBLE) < 1:
                break

    finally:
        try:
            ia.stop()
        except Exception:
            pass
        ia.destroy()
        h.reset()
        cv2.destroyAllWindows()


if __name__ == "__main__":
    main()