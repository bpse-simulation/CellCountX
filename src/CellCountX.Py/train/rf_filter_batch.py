import numpy as np
import csv
from pathlib import Path
import pickle
from skimage.measure import regionprops
from skimage.io import imread, imsave
from skimage.morphology import binary_erosion
import argparse

# ---------------------------------------------------------
# 緑（残す）・赤（除外）で輪郭を描画
# ---------------------------------------------------------
def create_overlay(image, keep_mask, remove_mask):
    if image.ndim == 2:
        rgb = np.stack([image, image, image], axis=-1)
    else:
        rgb = image.copy()

    rgb = rgb.astype(np.float32)
    rgb = rgb / (rgb.max() + 1e-6)
    rgb = (rgb * 255).clip(0, 255).astype(np.uint8)

    overlay = rgb.copy()

    # 残す（緑）
    boundaries_keep = np.zeros_like(keep_mask, dtype=bool)
    for label in range(1, keep_mask.max() + 1):
        cell = (keep_mask == label)
        eroded = binary_erosion(cell)
        boundary = cell ^ eroded
        boundaries_keep |= boundary
    overlay[boundaries_keep] = [0, 255, 0]

    # 除外（赤）
    boundaries_remove = np.zeros_like(remove_mask, dtype=bool)
    for label in range(1, remove_mask.max() + 1):
        cell = (remove_mask == label)
        eroded = binary_erosion(cell)
        boundary = cell ^ eroded
        boundaries_remove |= boundary
    overlay[boundaries_remove] = [255, 0, 0]

    return overlay


# ---------------------------------------------------------
# 特徴量抽出 + RF 推論
# ---------------------------------------------------------
def extract_features_and_predict(img, labels, clf):
    labels = labels.astype(np.int32)
    props = regionprops(labels, intensity_image=img)

    feature_list = []
    object_ids = []

    for p in props:
        area = float(p.area)
        mean_intensity = float(p.mean_intensity)
        variance = float(np.var(p.intensity_image))

        # 円形度 = 4πA / P^2
        if p.perimeter == 0:
            circularity = 1.0
        else:
            circularity = float(4 * np.pi * area / (p.perimeter ** 2))

        pix = np.asarray(p.intensity_image, dtype=np.float32).ravel()
        pix = pix[pix > 0]

        feature_list.append([area, circularity, mean_intensity, variance])
        object_ids.append(p.label)

    if len(feature_list) == 0:
        return [], [], [], np.zeros_like(labels), np.zeros_like(labels)

    X = np.array(feature_list, dtype=np.float32)
    pred = clf.predict(X)

    # RF=1 → 残す（緑）
    # RF=0 → 除外（赤）
    keep_mask = np.zeros_like(labels)
    remove_mask = np.zeros_like(labels)

    for obj_id, keep in zip(object_ids, pred):
        if keep == 1:
            keep_mask[labels == obj_id] = obj_id
        else:
            remove_mask[labels == obj_id] = obj_id

    return object_ids, feature_list, pred, keep_mask, remove_mask


# ---------------------------------------------------------
# バッチ処理
# ---------------------------------------------------------
def batch_process(image_dir, label_dir, model_path, csv_features, csv_counts, output_dir):
    image_dir = Path(image_dir)
    label_dir = Path(label_dir)
    output_dir = Path(output_dir)
    output_dir.mkdir(exist_ok=True)

    with open(model_path, "rb") as f:
        clf = pickle.load(f)

    # -------------------------
    # CSV: 特徴量
    # -------------------------
    f_feat = open(csv_features, "w", newline="")
    writer_feat = csv.writer(f_feat)
    writer_feat.writerow([
        "filename", "object_id",
        "area", "circularity", "mean_intensity", "variance",
        "rf_pred"
    ])

    # -------------------------
    # CSV: before / after
    # -------------------------
    f_cnt = open(csv_counts, "w", newline="")
    writer_cnt = csv.writer(f_cnt)
    writer_cnt.writerow(["filename", "before_count", "after_count"])

    # -------------------------
    # 画像ループ
    # -------------------------
    for img_path in sorted(image_dir.glob("*.jpg")):
        name = img_path.stem
        label_path = label_dir / f"{name}_cp_masks.tif"

        if not label_path.exists():
            print(f"Warning: label not found for {name}, skipping")
            continue

        img = imread(img_path)
        labels = imread(label_path)

        # RGB → グレースケール
        if img.ndim == 3:
            if img.shape[2] == 4:
                img_gray = img[:, :, :3].mean(axis=2)
            else:
                img_gray = img.mean(axis=2)
        else:
            img_gray = img.astype(np.float32)

        object_ids, feature_list, pred, keep_mask, remove_mask = \
            extract_features_and_predict(img_gray, labels, clf)

        before = len(object_ids)
        after = int(np.sum(pred == 1))  # RF=1 が残す

        # overlay 保存
        overlay = create_overlay(img_gray, keep_mask, remove_mask)
        out_path = output_dir / f"{name}_overlay.png"
        imsave(out_path, overlay)

        # 特徴量 CSV 書き込み
        for obj_id, feats, p in zip(object_ids, feature_list, pred):
            area, circ, mean_int, var = feats
            writer_feat.writerow([name, obj_id, area, circ, mean_int, var, int(p)])

        # before / after CSV 書き込み
        writer_cnt.writerow([name, before, after])

        print(f"{name}: before={before}, after={after}, overlay saved={out_path}")

    f_feat.close()
    f_cnt.close()
    print(f"Saved CSVs:\n  {csv_features}\n  {csv_counts}")


# ---------------------------------------------------------
# コマンドライン実行
# ---------------------------------------------------------
if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Batch RF filtering for label images.")
    parser.add_argument("--image_dir", required=True, help="Folder containing original images")
    parser.add_argument("--label_dir", required=True, help="Folder containing label images")
    parser.add_argument("--model", required=True, help="RandomForest model (.pkl)")
    parser.add_argument("--csv_features", default="rf_features.csv", help="Output CSV file")
    parser.add_argument("--csv_counts", default="rf_counts.csv", help="Output CSV file")
    parser.add_argument("--output_dir", default="rf_filter", help="Output CSV file")

    args = parser.parse_args()

    batch_process(args.image_dir, args.label_dir, args.model, args.csv_features, args.csv_counts, args.output_dir)
