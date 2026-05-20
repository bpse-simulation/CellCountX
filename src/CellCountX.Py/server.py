import os
import sys
import json
import warnings
import contextlib
import tifffile
import torch
import numpy as np
from cellpose import models
from cellpose.io import imread
from scipy.ndimage import binary_erosion

# server.py のあるディレクトリを import パスに追加
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

# 非接着細胞除去フィルタ
from nonadherent_filter import remove_nonadherent_cells

# ---------------------------------------------------------
# tqdm の stderr を無効化する
# ---------------------------------------------------------
@contextlib.contextmanager
def suppress_stderr():
    with open(os.devnull, "w") as devnull:
        old_stderr = sys.stderr
        sys.stderr = devnull
        try:
            yield
        finally:
            sys.stderr = old_stderr

# ---------------------------------------------------------
# GPU が使えるかどうかを判定
# ---------------------------------------------------------
def can_use_gpu() -> bool:
    return torch.cuda.is_available()

# ---------------------------------------------------------
# CellPose モデルをロード（初回ダウンロード時の tqdm を抑制）
# ---------------------------------------------------------
def load_model(use_gpu: bool):
    with suppress_stderr():
        return models.CellposeModel(gpu=use_gpu)

# ---------------------------------------------------------
# 推論実行（eval 時の tqdm も抑制）
# ---------------------------------------------------------
def run_inference(model, image):
    with suppress_stderr():
        return model.eval(image, channels=[0, 0])

# ---------------------------------------------------------
# JSON 入力を読み取る
# ---------------------------------------------------------
def read_input():
    line = sys.stdin.readline().strip()
    if not line:
        raise ValueError("no input")
    return json.loads(line)

# ---------------------------------------------------------
# マスク輪郭を赤色で重ねた画像を生成
# ---------------------------------------------------------
def create_overlay(image, masks):
    # image を RGB に変換
    if image.ndim == 2:
        rgb = np.stack([image, image, image], axis=-1)
    else:
        rgb = image.copy()

    rgb = rgb.astype(np.float32)
    rgb = rgb / (rgb.max() + 1e-6)  # 正規化
    rgb = (rgb * 255).clip(0, 255).astype(np.uint8)

    # 輪郭抽出
    boundaries = np.zeros_like(masks, dtype=bool)
    for label in range(1, masks.max() + 1):
        cell = (masks == label)
        eroded = binary_erosion(cell)
        boundary = cell ^ eroded
        boundaries |= boundary

    # 赤色で描画
    overlay = rgb.copy()
    overlay[boundaries] = [255, 0, 0]

    return overlay

# ---------------------------------------------------------
# メイン処理
# ---------------------------------------------------------
def main():
    warnings.filterwarnings("ignore")

    try:
        data = read_input()

        if "path" not in data:
            raise ValueError("missing 'path' in input JSON")

        img_path = data["path"]
        if not os.path.exists(img_path):
            raise FileNotFoundError(f"image not found: {img_path}")

        # 画像読み込み
        image = imread(img_path)

        # ★ image が RGB の場合は 2D に変換
        if image.ndim == 3:
            image_gray = image.mean(axis=2)
        else:
            image_gray = image

        # GPU 判定
        request_gpu = data.get("gpu", False)
        use_gpu = bool(request_gpu) and can_use_gpu()

        # モデルロード
        model = load_model(use_gpu)

        # Cellpose 推論
        masks, flows, styles = run_inference(model, image_gray)

        # 出力パス生成
        folder = os.path.dirname(img_path)
        base = os.path.splitext(os.path.basename(img_path))[0]
        output_folder = data.get("output", folder)
        os.makedirs(output_folder, exist_ok=True)

        # マスク保存
        mask_path = os.path.join(output_folder, f"{base}_cp_masks.tif")
        tifffile.imwrite(mask_path, masks.astype("uint16"))

        # Cellpose の元の細胞数
        original_count = int(masks.max())

        # masks が (H, W, 1) の場合は 2D に変換
        if masks.ndim == 3 and masks.shape[-1] == 1:
            masks = masks[:, :, 0]

        # ---------------------------------------------------------
        # 非接着細胞除去（後処理）
        # ---------------------------------------------------------
        remove_nonadherents = data.get("remove_nonadherents", False)

        if remove_nonadherents:
            params = {
                "min_area": data.get("min_area", 0),
                "max_circularity": data.get("max_circularity", 1),
                "max_intensity": data.get("max_intensity", 255),
                "min_variance": data.get("min_variance", 0)
            }

            masks = remove_nonadherent_cells(masks, image_gray, **params)

        # 非接着細胞除去後の細胞数
        filtered_count = int(masks.max())

        # ---------------------------------------------------------
        # 輪郭オーバーレイ画像を生成して保存
        # ---------------------------------------------------------
        overlay = create_overlay(image_gray, masks)
        overlay_path = os.path.join(output_folder, f"{base}_overlay.png")
        tifffile.imwrite(overlay_path, overlay)

        # ---------------------------------------------------------
        # 結果返却
        # ---------------------------------------------------------
        result = {
            "count": original_count,
            "filtered_count": filtered_count,
            "gpu_used": use_gpu,
            "mask_path": mask_path,
            "overlay_path": overlay_path,
            "nonadherents_removed": remove_nonadherents
        }

        print(json.dumps(result), flush=True)

    except Exception as e:
        print(f"[ERROR] {repr(e)}", file=sys.stderr, flush=True)
        print(json.dumps({"error": str(e)}), flush=True)


if __name__ == "__main__":
    main()
