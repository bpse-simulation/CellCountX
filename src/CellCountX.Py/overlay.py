import numpy as np
from scipy.ndimage import binary_erosion

# ---------------------------------------------------------
# 通常 overlay（赤）
# ---------------------------------------------------------
def create_overlay(image, masks):
    if image.ndim == 2:
        rgb = np.stack([image, image, image], axis=-1)
    else:
        rgb = image.copy()

    rgb = rgb.astype(np.float32)
    rgb = rgb / (rgb.max() + 1e-6)
    rgb = (rgb * 255).clip(0, 255).astype(np.uint8)

    boundaries = np.zeros_like(masks, dtype=bool)
    for label in range(1, masks.max() + 1):
        cell = (masks == label)
        eroded = binary_erosion(cell)
        boundary = cell ^ eroded
        boundaries |= boundary

    overlay = rgb.copy()
    overlay[boundaries] = [255, 0, 0]
    return overlay

# ---------------------------------------------------------
# RF overlay（緑=keep / 赤=remove）
# ---------------------------------------------------------
def create_overlay_rf(image, keep_mask, remove_mask):
    if image.ndim == 2:
        rgb = np.stack([image, image, image], axis=-1)
    else:
        rgb = image.copy()

    rgb = rgb.astype(np.float32)
    rgb = rgb / (rgb.max() + 1e-6)
    rgb = (rgb * 255).clip(0, 255).astype(np.uint8)

    overlay = rgb.copy()

    # keep → 緑
    boundaries_keep = np.zeros_like(keep_mask, dtype=bool)
    for label in np.unique(keep_mask):
        if label == 0:
            continue
        cell = (keep_mask == label)
        eroded = binary_erosion(cell)
        boundary = cell ^ eroded
        boundaries_keep |= boundary

    overlay[boundaries_keep] = [0, 255, 0]

    # remove → 赤
    boundaries_remove = np.zeros_like(remove_mask, dtype=bool)
    for label in np.unique(remove_mask):
        if label == 0:
            continue
        cell = (remove_mask == label)
        eroded = binary_erosion(cell)
        boundary = cell ^ eroded
        boundaries_remove |= boundary

    overlay[boundaries_remove] = [255, 0, 0]

    return overlay
