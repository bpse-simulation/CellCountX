import os
import sys
import json
import warnings
import contextlib
import tifffile
import torch
from cellpose import models
from cellpose.io import imread

# 死細胞除去フィルタ
from deadcell_filter import remove_dead_cells

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
            # 平均化（または任意のチャネルを使用）
            image = image.mean(axis=2)

        # GPU 判定
        request_gpu = data.get("gpu", False)
        use_gpu = bool(request_gpu) and can_use_gpu()

        # モデルロード
        model = load_model(use_gpu)

        # Cellpose 推論
        masks, flows, styles = run_inference(model, image)

        # Cellpose の元の細胞数
        original_count = int(masks.max())

        # masks が (H, W, 1) の場合は 2D に変換
        if masks.ndim == 3 and masks.shape[-1] == 1:
            masks = masks[:, :, 0]

        # ---------------------------------------------------------
        # 死細胞除去（後処理）
        # ---------------------------------------------------------
        remove_dead = data.get("remove_dead", False)

        if remove_dead:
            params = {
                "min_area": data.get("min_area", 50),
                "max_circularity": data.get("max_circularity", 0.85),
                "max_intensity": data.get("max_intensity", 0.6),
                "min_variance": data.get("min_variance", 50)
            }

            masks = remove_dead_cells(masks, image, **params)

        # 死細胞除去後の細胞数
        filtered_count = int(masks.max())

        # 出力パス生成
        folder = os.path.dirname(img_path)
        base = os.path.splitext(os.path.basename(img_path))[0]
        output_folder = data.get("output", folder)
        os.makedirs(output_folder, exist_ok=True)

        mask_path = os.path.join(output_folder, f"{base}_cp_masks.tif")
        tifffile.imwrite(mask_path, masks.astype("uint16"))

        # ---------------------------------------------------------
        # 結果返却
        # ---------------------------------------------------------
        result = {
            "count": original_count,          # ★ Cellpose の元の細胞数
            "filtered_count": filtered_count, # ★ 死細胞除去後の細胞数
            "gpu_used": use_gpu,
            "mask_path": mask_path,
            "dead_removed": remove_dead
        }

        print(json.dumps(result), flush=True)

    except Exception as e:
        print(f"[ERROR] {repr(e)}", file=sys.stderr, flush=True)
        print(json.dumps({"error": str(e)}), flush=True)


if __name__ == "__main__":
    main()
