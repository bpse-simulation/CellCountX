import numpy as np
from skimage.measure import regionprops

def remove_dead_cells(masks, image,
                      min_area=50,
                      max_circularity=0.85,
                      max_intensity=0.6,
                      min_variance=50):
    """
    Cellpose の masks と元画像 image を入力し、
    死細胞様オブジェクトを除去した新しい masks を返す。
    """

    new_mask = np.zeros_like(masks)
    label = 1

    for region in regionprops(masks, intensity_image=image):

        area = float(region.area)
        mean_intensity = float(region.mean_intensity)
        variance = float(np.var(region.intensity_image))

        # 円形度 = 4πA / P^2
        if region.perimeter == 0:
            circularity = 1.0
        else:
            circularity = float(4 * np.pi * area / (region.perimeter ** 2))

        # -------------------------
        # 除外条件（死細胞の特徴）
        # -------------------------
        is_dead = (
            area < min_area or
            circularity > max_circularity or
            mean_intensity > max_intensity or
            variance < min_variance
        )

        if not is_dead:
            new_mask[masks == region.label] = label
            label += 1

    return new_mask
