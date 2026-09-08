import numpy as np
from scipy.ndimage import binary_erosion

# ---------------------------------------------------------
# 通常 overlay（緑のみ）
# ---------------------------------------------------------
def create_overlay(image, masks):
    # グレースケール → RGB（OpenCV は BGR で扱うが、ここでは3ch画像として扱う）
    if image.ndim == 2:
        rgb = np.stack([image, image, image], axis=-1)
    else:
        rgb = image.copy()

    # 0–255 に正規化
    rgb = rgb.astype(np.float32)
    rgb = rgb / (rgb.max() + 1e-6)
    rgb = (rgb * 255).clip(0, 255).astype(np.uint8)

    # 境界抽出
    boundaries = np.zeros_like(masks, dtype=bool)
    for label in range(1, masks.max() + 1):
        cell = (masks == label)
        eroded = binary_erosion(cell)
        boundary = cell ^ eroded
        boundaries |= boundary

    overlay = rgb.copy()

    # 注意：
    # OpenCV(cv2.imwrite) は BGR 形式で保存するため、
    # 緑は [0,255,0]（RGB/BGR 共通）で問題なし。
    overlay[boundaries] = [0, 255, 0]   # 緑（BGR）

    return overlay


# ---------------------------------------------------------
# 境界細胞除去 overlay（緑=keep / 赤=edge removed）
# ---------------------------------------------------------
def create_overlay_removed(image, keep_mask, remove_mask):
    """
    keep_mask      : 境界除去後に残った細胞（採用）
    remove_mask    : 境界除去で除去された細胞（除去）
    """

    # グレースケール → RGB（OpenCV は BGR として扱う）
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

    # 緑（BGR）
    overlay[boundaries_keep] = [0, 255, 0]

    # ---------------------------------------------------------
    # 2. 赤 = remove_mask の輪郭
    # ---------------------------------------------------------
    # 注意：
    # OpenCV(cv2.imwrite) は BGR 形式で保存するため、
    # 赤は [0,0,255] を指定する必要がある。
    # （RGB の赤 [255,0,0] を指定すると青として保存される）
    boundaries_removed = np.zeros_like(remove_mask, dtype=bool)
    for label in np.unique(remove_mask):
        if label == 0:
            continue
        cell = (remove_mask == label)
        eroded = binary_erosion(cell)
        boundary = cell ^ eroded
        boundaries_removed |= boundary

    overlay[boundaries_removed] = [0, 0, 255]   # 赤（BGR）

    return overlay
