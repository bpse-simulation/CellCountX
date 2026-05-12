import os
import sys
import json
import warnings
import contextlib
import tifffile
import torch
from cellpose import models
from cellpose.io import imread

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
        return model.eval(image)

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

        # 入力バリデーション
        if "path" not in data:
            raise ValueError("missing 'path' in input JSON")

        img_path = data["path"]
        if not os.path.exists(img_path):
            raise FileNotFoundError(f"image not found: {img_path}")

        # 画像読み込み
        image = imread(img_path)

        # GPU 判定
        request_gpu = data.get("gpu", False)
        use_gpu = bool(request_gpu) and can_use_gpu()

        # モデルロード
        model = load_model(use_gpu)

        # 推論
        masks, flows, styles = run_inference(model, image)

        # 出力パス生成
        folder = os.path.dirname(img_path)
        base = os.path.splitext(os.path.basename(img_path))[0]
        output_folder = data.get("output", folder)
        os.makedirs(output_folder, exist_ok=True)

        mask_path = os.path.join(output_folder, f"{base}_cp_masks.tif")
        tifffile.imwrite(mask_path, masks.astype("uint16"))

        # 結果返却
        result = {
            "count": int(masks.max()),
            "gpu_used": use_gpu,
            "mask_path": mask_path
        }
        print(json.dumps(result), flush=True)

    except Exception as e:
        # 内部ログは stderr に出す（C# 側には返さない）
        print(f"[ERROR] {repr(e)}", file=sys.stderr, flush=True)

        # C# 側には JSON で返す
        print(json.dumps({"error": str(e)}), flush=True)


if __name__ == "__main__":
    main()
