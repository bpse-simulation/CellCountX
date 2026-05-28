import os
import sys
import json
import warnings
import contextlib
import tifffile
import torch
from cellpose import models
from cellpose.io import imread

# server.py のあるフォルダを import パスに追加
base_dir = os.path.dirname(os.path.abspath(sys.argv[0]))
sys.path.insert(0, base_dir)

from rf_filter import (
    RF_MODEL,
    extract_features_and_predict,
    relabel_sequential
)

from overlay import (
    create_overlay,
    create_overlay_rf
)

# ---------------------------------------------------------
# tqdm の stderr を無効化
# ---------------------------------------------------------
@contextlib.contextmanager
def suppress_stderr():
    with open(os.devnull, "w") as devnull:
        old = sys.stderr
        sys.stderr = devnull
        try:
            yield
        finally:
            sys.stderr = old

def can_use_gpu():
    return torch.cuda.is_available()

def load_model(use_gpu):
    with suppress_stderr():
        return models.CellposeModel(gpu=use_gpu)

def run_inference(model, image, flow_th, cellprob_th):
    with suppress_stderr():
        return model.eval(
            image,
            flow_threshold=flow_th,
            cellprob_threshold=cellprob_th
        )

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

        img_path = data["path"]
        if not os.path.exists(img_path):
            raise FileNotFoundError(f"image not found: {img_path}")

        image = imread(img_path)

        # RGB → グレースケール
        if image.ndim == 3:
            if image.shape[2] == 4:
                rgb = image[:, :, 1:4]
                image_gray = rgb.mean(axis=2)
            else:
                image_gray = image.mean(axis=2)
        else:
            image_gray = image

        # Cellpose
        use_gpu = bool(data.get("gpu", False)) and can_use_gpu()
        model = load_model(use_gpu)

        flow_th = float(data.get("flow_threshold", 0.4))
        cellprob_th = float(data.get("cellprob_threshold", 0.0))

        masks, flows, styles = run_inference(
            model, image, flow_th, cellprob_th
        )

        # 出力パス
        folder = os.path.dirname(img_path)
        base = os.path.splitext(os.path.basename(img_path))[0]
        output_folder = data.get("output", folder)
        os.makedirs(output_folder, exist_ok=True)

        # マスク保存
        mask_path = os.path.join(output_folder, f"{base}_cp_masks.tif")
        tifffile.imwrite(mask_path, masks.astype("uint16"))

        original_count = int(masks.max())

        if masks.ndim == 3 and masks.shape[-1] == 1:
            masks = masks[:, :, 0]

        # ---------------------------------------------------------
        # RF フィルタ
        # ---------------------------------------------------------
        use_rf = data.get("use_rf_filter", False)

        if use_rf:
            _, _, pred, keep_mask, remove_mask = extract_features_and_predict(
                image_gray, masks, RF_MODEL
            )
            masks = relabel_sequential(keep_mask)
        else:
            keep_mask = None
            remove_mask = None

        filtered_count = int(masks.max())

        # ---------------------------------------------------------
        # overlay
        # ---------------------------------------------------------
        if use_rf:
            overlay = create_overlay_rf(image_gray, keep_mask, remove_mask)
        else:
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
            "rf_filter_used": use_rf,
        }

        print(json.dumps(result), flush=True)

    except Exception as e:
        print(f"[ERROR] {repr(e)}", file=sys.stderr)
        print(json.dumps({"error": str(e)}), flush=True)


if __name__ == "__main__":
    main()
