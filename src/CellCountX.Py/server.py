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

# server.py のあるフォルダを import パスに追加
base_dir = os.path.dirname(os.path.abspath(sys.argv[0]))
sys.path.insert(0, base_dir)

from remove_edge_cells import remove_edge_cells

from overlay import (
    create_overlay,
    create_overlay_removed
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

# ---------------------------------------------------------
# Cellpose: モデルロード
# ---------------------------------------------------------
def load_model(use_gpu, custom_model_path):
    with suppress_stderr():
        if custom_model_path:
            # GUI で指定された .npy モデルを使用
            return models.CellposeModel(
                gpu=use_gpu,
                pretrained_model=custom_model_path
            )
        else:
            # デフォルトモデル（cpsam_v2）
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
# ラベル付け直し
# ---------------------------------------------------------
def relabel_sequential(mask):
    unique_labels = np.unique(mask)
    unique_labels = unique_labels[unique_labels != 0]

    new_mask = np.zeros_like(mask)
    for new_id, old_id in enumerate(unique_labels, start=1):
        new_mask[mask == old_id] = new_id

    return new_mask

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

        # ---------------------------------------------------------
        # Cellpose モデルロード
        # ---------------------------------------------------------
        use_gpu = bool(data.get("gpu", False)) and can_use_gpu()
        custom_model_path = data.get("custom_model", None)

        model = load_model(use_gpu, custom_model_path)

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

        # 安全な squeeze
        masks = np.squeeze(masks)

        # ---------------------------------------------------------
        # 境界細胞除去（4方向 ON/OFF）
        # ---------------------------------------------------------
        use_edge_filter = data.get("use_edge_filter", False)

        use_top    = data.get("use_edge_top", True)
        use_bottom = data.get("use_edge_bottom", True)
        use_left   = data.get("use_edge_left", True)
        use_right  = data.get("use_edge_right", True)

        margin = int(data.get("edge_margin", 2))

        original_masks = masks.copy()

        if use_edge_filter:
            cleaned_masks, edge_labels = remove_edge_cells(
                masks,
                use_top=use_top,
                use_bottom=use_bottom,
                use_left=use_left,
                use_right=use_right,
                margin=margin
            )
            masks = relabel_sequential(cleaned_masks)

            edge_removed_mask = np.zeros_like(original_masks)
            for lbl in edge_labels:
                edge_removed_mask[original_masks == lbl] = lbl
        else:
            edge_removed_mask = None

        filtered_count = int(masks.max())

        # ---------------------------------------------------------
        # overlay
        # ---------------------------------------------------------
        if use_edge_filter:
            overlay = create_overlay_removed(
                image_gray,
                cleaned_masks,     # keep_mask（境界除去後のマスク）
                edge_removed_mask  # remove_mask（境界除去された細胞）
            )
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
            "edge_filter_used": use_edge_filter,
            "edge_top": use_top,
            "edge_bottom": use_bottom,
            "edge_left": use_left,
            "edge_right": use_right,
            "edge_margin": margin,
            "custom_model_used": bool(custom_model_path)
        }

        print(json.dumps(result), flush=True)

    except Exception as e:
        print(f"[ERROR] {repr(e)}", file=sys.stderr)
        print(json.dumps({"error": str(e)}), flush=True)


if __name__ == "__main__":
    main()
