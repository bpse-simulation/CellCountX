import numpy as np
from skimage.measure import regionprops

def remove_nonadherent_cells(masks, image,
                        min_area=0,
                        max_circularity=1,
                        max_intensity=1,
                        min_variance=0):
    """
    Cellpose の masks と元画像 image を入力し、
    非接着細胞様オブジェクトを除去した新しい masks を返す。
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
        # 除外条件（非接着細胞の特徴）
        # -------------------------
        is_nonadherents = (
            area < min_area or
            circularity > max_circularity or
            mean_intensity > max_intensity or
            variance < min_variance
        )

        if not is_nonadherents:
            new_mask[masks == region.label] = label
            label += 1

    return new_mask
