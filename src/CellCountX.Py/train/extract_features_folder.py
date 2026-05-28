import numpy as np
import csv
from skimage.measure import regionprops
from skimage.io import imread
from pathlib import Path
import argparse


def extract_region_features_from_folders(image_dir, masks_dir, csv_path):
    image_dir = Path(image_dir)
    masks_dir = Path(masks_dir)

    feature_rows = []

    # image_dir 内の .jpg をすべて処理
    for image_path in sorted(image_dir.glob("*.jpg")):
        name = image_path.stem  # 例: "001"

        # 対応する mask ファイル名
        mask_path = masks_dir / f"{name}_cp_masks.tif"

        if not mask_path.exists():
            print(f"Warning: mask not found for {name}_cp_masks.tif, skipping")
            continue

        # 画像読み込み
        image = imread(image_path)
        if image.ndim == 3:
            if image.shape[2] == 4:
                # ARGB → RGB 部分だけ使う
                rgb = image[:, :, 1:4]
                image = rgb.mean(axis=2)
            else:
                # 通常の RGB
                image = image.mean(axis=2)  # grayscale

        # マスク読み込み
        masks = imread(mask_path)
        if masks.ndim == 3:
            masks = masks[:, :, 0]

        for region in regionprops(masks, intensity_image=image):

            area = float(region.area)
            mean_intensity = float(region.mean_intensity)
            variance = float(np.var(region.intensity_image))

            # 円形度 = 4πA / P^2
            if region.perimeter == 0:
                circularity = 1.0
            else:
                circularity = float(4 * np.pi * area / (region.perimeter ** 2))

            feature_rows.append([
                name,               # 画像名（拡張子なし）
                region.label,       # ラベル番号
                area,
                circularity,
                mean_intensity,
                variance
            ])

    # ---------------------------------------------
    # CSV に保存
    # ---------------------------------------------
    with open(csv_path, "w", newline="") as f:
        writer = csv.writer(f)
        writer.writerow([
            "filename",
            "label",
            "area",
            "circularity",
            "mean_intensity",
            "variance"
        ])
        writer.writerows(feature_rows)

    print(f"Saved {len(feature_rows)} rows to {csv_path}")


# ---------------------------------------------------------
# コマンドライン実行用
# python extract_features_folder.py --image_dir input --masks_dir output
# ---------------------------------------------------------
if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Extract region features from .jpg images and corresponding _cp_masks.tif masks.")
    parser.add_argument("--image_dir", required=True, help="Folder containing .jpg image files")
    parser.add_argument("--masks_dir", required=True, help="Folder containing *_cp_masks.tif mask files")
    parser.add_argument("--csv", default="all_objects_features.csv", help="Output CSV file")

    args = parser.parse_args()

    extract_region_features_from_folders(args.image_dir, args.masks_dir, args.csv)
