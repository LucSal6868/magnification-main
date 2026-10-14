"""
MityCAM-120MXS Harvester Streamer with JSON Config Loader
Target Script: Assets/Python/main.py
"""

import json
import os
from pathlib import Path
import struct
import cv2
import numpy as np
from multiprocessing.shared_memory import SharedMemory

PROJECT_ROOT = Path(__file__).parent.resolve()
CONFIG_PATH = PROJECT_ROOT / "camera_settings.json"


def load_config():
    """Loads camera settings from JSON with fallback defaults."""
    defaults = {
        "camera": {
            "cti_path": "bin/GenTL.dll",
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
        },
        "streaming": {
            "shm_name": "MityCamSharedMemory",
            "downscale_output": False,
            "target_width": 1920,
            "target_height": 1080
        }
    }

    if CONFIG_PATH.exists():
        try:
            with open(CONFIG_PATH, "r") as f:
                data = json.load(f)
                print(f"[Python] Loaded configuration from {CONFIG_PATH.name}")
                return data
        except Exception as e:
            print(f"[warn] Failed to parse {CONFIG_PATH.name}: {e}. Using defaults.")

    return defaults


# Load Settings
cfg = load_config()

BIN_DIR = PROJECT_ROOT / Path(cfg["camera"]["cti_path"]).parent
CTI_PATH = str(PROJECT_ROOT / cfg["camera"]["cti_path"])
SHM_NAME = cfg["streaming"]["shm_name"]

SENSOR_W, SENSOR_H = 13272, 9176

if BIN_DIR.exists():
    os.add_dll_directory(str(BIN_DIR))
    os.chdir(str(BIN_DIR))

from harvesters.core import Harvester  # noqa: E402


def try_set(node_map, name, value):
    try:
        getattr(node_map, name).value = value
    except Exception as e:
        print(f"[warn] Could not set {name} = {value!r}: {e}")


def configure(node_map, cfg):
    c_cfg = cfg["camera"]
    r_cfg = cfg["roi"]

    # Reset offsets to zero before configuring dimensions
    try_set(node_map, "OffsetX", 0)
    try_set(node_map, "OffsetY", 0)

    # Validate alignment constraints (Width % 8, Height % 4)
    w = max(32, min(r_cfg["width"], SENSOR_W)) // 8 * 8
    h = max(4, min(r_cfg["height"], SENSOR_H)) // 4 * 4

    try_set(node_map, "Width", w)
    try_set(node_map, "Height", h)

    if r_cfg["center"]:
        try_set(node_map, "OffsetX", ((SENSOR_W - w) // 2) // 8 * 8)
        try_set(node_map, "OffsetY", ((SENSOR_H - h) // 2) // 4 * 4)
    else:
        try_set(node_map, "OffsetX", r_cfg["offset_x"] // 8 * 8)
        try_set(node_map, "OffsetY", r_cfg["offset_y"] // 4 * 4)

    try_set(node_map, "PixelFormat", c_cfg["pixel_format"])
    try_set(node_map, "AcquisitionMode", "Continuous")
    try_set(node_map, "TriggerMode", "Off")
    try_set(node_map, "ExposureTime", c_cfg["exposure_us"])
    try_set(node_map, "AcquisitionFrameRate", c_cfg["frame_rate"])
    if c_cfg["gain"]:
        try_set(node_map, "PGAGain", c_cfg["gain"])


def to_rgb(raw, pixel_format):
    if pixel_format.startswith("Bayer"):
        return cv2.cvtColor(raw, cv2.COLOR_BAYER_BG2RGB)
    elif len(raw.shape) == 2:
        return cv2.cvtColor(raw, cv2.COLOR_GRAY2RGB)
    return cv2.cvtColor(raw, cv2.COLOR_BGR2RGB)


def main():
    h = Harvester()
    h.add_file(CTI_PATH)
    h.update()

    if not h.device_info_list:
        print("[Python] No cameras found.")
        h.reset()
        return

    ia = h.create(0)
    shm = None

    try:
        node_map = ia.remote_device.node_map
        configure(node_map, cfg)

        s_cfg = cfg["streaming"]
        downscale = s_cfg["downscale_output"]
        out_w = s_cfg["target_width"] if downscale else node_map.Width.value
        out_h = s_cfg["target_height"] if downscale else node_map.Height.value

        HEADER_SIZE = 12
        frame_size = out_w * out_h * 3
        total_size = HEADER_SIZE + frame_size

        try:
            shm = SharedMemory(name=SHM_NAME, create=True, size=total_size)
        except FileExistsError:
            shm = SharedMemory(name=SHM_NAME)

        print(f"[Python] Streaming {out_w}x{out_h} to shared memory '{SHM_NAME}'")

        ia.start()
        frame_count = 0

        while True:
            try:
                buf = ia.fetch(timeout=5)
            except Exception as e:
                print(f"[warn] fetch failed: {e}")
                continue

            with buf:
                comp = buf.payload.components[0]
                raw = comp.data.reshape(comp.height, comp.width)
                rgb_img = to_rgb(raw, node_map.PixelFormat.value)

            if downscale:
                rgb_img = cv2.resize(rgb_img, (out_w, out_h), interpolation=cv2.INTER_AREA)

            frame_count = (frame_count + 1) % 4294967295

            header = struct.pack("<III", out_w, out_h, frame_count)
            shm.buf[:HEADER_SIZE] = header
            shm.buf[HEADER_SIZE:HEADER_SIZE + frame_size] = rgb_img.tobytes()

    except KeyboardInterrupt:
        print("[Python] Stopping stream...")
    finally:
        try:
            ia.stop()
        except Exception:
            pass
        ia.destroy()
        h.reset()
        if shm is not None:
            shm.close()
            shm.unlink()


if __name__ == "__main__":
    main()