import numpy as np
from scipy.ndimage import binary_erosion

# ---------------------------------------------------------
# 通常 overlay（緑のみ）
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
    overlay[boundaries] = [0, 255, 0]
    return overlay


# ---------------------------------------------------------
# 境界細胞除去 overlay（緑=keep / 赤=edge removed）
# ---------------------------------------------------------
def create_overlay_removed(image, keep_mask, remove_mask):
    """
    original_masks : Cellpose の元マスク
    keep_mask      : 境界除去後に残った細胞
    remove_mask    : 境界除去で除去された細胞
    """

    # グレースケール → RGB
    if image.ndim == 2:
        rgb = np.stack([image, image, image], axis=-1)
    else:
        rgb = image.copy()

    rgb = rgb.astype(np.float32)
    rgb = rgb / (rgb.max() + 1e-6)
    rgb = (rgb * 255).clip(0, 255).astype(np.uint8)

    overlay = rgb.copy()

    # ---------------------------------------------------------
    # 1. 緑 = keep_mask の輪郭
    # ---------------------------------------------------------
    boundaries_keep = np.zeros_like(keep_mask, dtype=bool)
    for label in np.unique(keep_mask):
        if label == 0:
            continue
        cell = (keep_mask == label)
        eroded = binary_erosion(cell)
        boundary = cell ^ eroded
        boundaries_keep |= boundary

    overlay[boundaries_keep] = [0, 255, 0]   # 緑

    # ---------------------------------------------------------
    # 2. 赤 = remove_mask の輪郭（境界除去された細胞）
    # ---------------------------------------------------------
    boundaries_removed = np.zeros_like(remove_mask, dtype=bool)
    for label in np.unique(remove_mask):
        if label == 0:
            continue
        cell = (remove_mask == label)
        eroded = binary_erosion(cell)
        boundary = cell ^ eroded
        boundaries_removed |= boundary

    overlay[boundaries_removed] = [255, 0, 0]   # 赤

    return overlay