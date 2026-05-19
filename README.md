# 📘 CellCountX

高精度な細胞カウントを GUI で実行できる WPF アプリケーションです。  
バックエンドには **Python（Embeddable 版）+ CellPose + PyTorch** を使用し、  
画像フォルダを指定するだけで **バッチ処理・GPU 対応・CSV 出力** を自動で行います。

Python はアプリに完全同梱されているため、  
**ユーザー側で Python をインストールする必要はありません。**

---

# 🚀 特徴

### 🧠 高精度 CellPose 推論

- Embeddable Python + CellPose + PyTorch を同梱
- server.py に JSON を渡して推論を実行
- GPU が利用可能な環境では自動的に GPU を使用

### 🖼️ マスク画像 + 輪郭オーバーレイ画像を自動保存

- `{base}_cp_masks.tif`  
  CellPose のラベルマスク（uint16）
- `{base}_overlay.png`  
  **グレー画像に赤色の輪郭を重ねた可視化画像**

### ⚙️ WPF (MVVM) アーキテクチャ

- UI とロジックを完全分離
- MainViewModel が UI 状態を一元管理
- 非同期処理 + キャンセル対応

### 📦 Embeddable Python を ZIP から自動展開

- `python-3.10.11-embed-amd64.zip` を Release ビルド時に自動展開
- ZIP と同名フォルダに展開 → python/ にコピー
- `_pth` ファイルも自動生成

### 📊 バッチ処理 + CSV 出力

- 指定フォルダ内の画像を一括処理
- 進捗バー表示
- CSV 出力（ファイル名・細胞数）

### 🧹 タイムアウト + 安全な Kill

- Python が応答しない場合はプロセスを強制終了
- 次の画像へ自動的に進む

### 🔤 全角パス禁止

- CellPose の制約により、フォルダ名・ファイル名に全角文字が含まれる場合は中断

---

# 📂 プロジェクト構成

## WPF プロジェクト（CellCountX.Wpf）


```
CellCountX.Wpf/
├── View/
│   └── MainWindow.xaml
├── ViewModel/
│   └── MainViewModel.cs
├── Logic/
│   ├── PythonServer.cs
│   ├── PythonClient.cs
│   ├── BatchProcessor.cs
│   ├── JsonParser.cs
│   ├── CsvExporter.cs
│   └── PathValidator.cs
├── Model/
│   ├── BatchRequest.cs
│   └── CellResult.cs
├── python_embed/
│   └── python310._pth.template
├── python-3.10.11-embed-amd64.zip   ← Embeddable Python（ZIP のまま）
└── CellCountX.Wpf.csproj
```

## Python バックエンド（開発用）

```
CellCountX.Py/
├── server.py
├── deadcell_filter.py
└── cellpose/
├── Scripts/python.exe
└── Lib/site-packages/
```

### 配布時の構成（Release ビルド後）

```
CellCountX/
├── CellCountX.exe
├── server.py
├── deadcell_filter.py
└── python/
    ├── python.exe
    ├── python310.dll
    ├── python310.zip
    ├── python310._pth
    └── site-packages/
        ├── cellpose/
        ├── torch/
        ├── numpy/
        ├── scipy/
        └── ...
```

---

# 🖥️ 使い方

### 1. 画像フォルダを選択

CellPose で処理したい画像フォルダを指定します。

### 2. 出力フォルダを選択

CSV とマスク画像・オーバーレイ画像が保存されます。

### 3. GPU 使用の有無を選択

CUDA が利用可能な環境では高速化できます。

### 4. タイムアウト秒数を設定

Python が応答しない場合の強制終了時間です。

### 5. 「開始」ボタンでバッチ処理開始

進捗バーが動き、ログがリアルタイムに表示されます。

### 6. 「キャンセル」で即時中断

安全に Python プロセスを Kill します。

---

# 🖼️ 出力される画像

| 種類 | ファイル名 | 内容 |
|------|------------|------|
| マスク画像 | `{base}_cp_masks.tif` | CellPose のラベルマスク |
| 輪郭オーバーレイ画像 | `{base}_overlay.png` | グレー画像 + 赤色輪郭 |

---

# ⚠️ 注意事項

### 🔸 全角文字を含むパスは使用できません

CellPose の制約により、以下に全角文字が含まれる場合は処理を中断します：

- 入力フォルダ
- 出力フォルダ
- 画像ファイル名

### 🔸 Python 実行環境について

- 配布版は **python/ フォルダ内の Embeddable Python** を使用
- 開発時は `CellCountX.Py/` の venv を自動検出して使用

---

# 🧩 アーキテクチャ概要

## PythonServer

- Embeddable Python の python.exe を起動
- server.py に JSON を渡して推論を実行
- タイムアウト時はプロセスを Kill

## server.py（バックエンド）

- CellPose 推論
- 死細胞除去フィルタ（deadcell_filter.py）
- マスク画像保存
- **輪郭オーバーレイ画像保存**
- JSON で結果を返す

## BatchProcessor

- 画像フォルダを走査
- PythonServer を呼び出し
- CSV 出力
- 非同期 + キャンセル対応

## MainViewModel

- UI 状態管理
- コマンド（開始・キャンセル）
- ログ・進捗更新

---

# 📄 CSV 出力形式

| FileName | CellCount | FilteredCount | GPU |
|----------|-----------|---------------|-----|
| image001.png | 123 | 120 | True |
| image002.png | 98 | 98 | False |

---

# 🛠 開発者向け：Embeddable Python の準備

CellCountX は配布版で Embeddable Python を使用します。  
開発時には、リポジトリに含まれる以下の ZIP を展開してください。

```
CellCountX.Wpf/python-3.10.11-embed-amd64.zip
```

### 注意

- ZIP は Git に含める（軽量・バージョン固定のため）
- 展開後のフォルダは Git 管理しない
- Release ビルド時に ZIP → 展開 → python/ にコピーされます

---

# 🛠️ 開発者向け：CellPose バックエンド環境構築

```
python -m venv cellpose
cellpose\Scripts\activate
pip install cellpose
pip3 install torch torchvision --index-url https://download.pytorch.org/whl/cu126
pip install packaging
pip install -U scikit-image
```

---

# 🛠️ Release ビルドの自動化（csproj）

Release ビルド時に以下が自動実行されます：

1. **python-3.10.11-embed-amd64.zip を ZIP と同名フォルダに展開**
2. Embeddable Python を `(OutDir)/python/` にコピー
3. `_pth` ファイルをテンプレートから生成
4. venv から抽出した site-packages をコピー
5. `server.py` と `deadcell_filter.py` を exe と同じ場所にコピー
6. Clean 時に python/ と server.py を削除

---

# 📦 CellCountX の分割 ZIP を結合して展開する方法

GitHub の 2GB 制限を回避するため、  
CellCountX は **複数の ZIP（.zip.001, .zip.002, ...）に分割**されています。

### 1. すべての分割 ZIP をダウンロード

```
CellCountX-vX.Y.Z.zip.001
CellCountX-vX.Y.Z.zip.002
...
```

### 2. 7-Zip で `.zip.001` を展開（推奨）

自動的に `.zip.002` 以降も読み込まれます。

### 3. CellCountX.exe を実行

---

# 📜 ライセンス

- CellPose のライセンスに従います  
- 本アプリケーションは MIT ライセンスを推奨します

---

# 🙌 作者

- 開発: BPSE-Lab  
- アーキテクチャ設計: PythonServer / PythonClient / BatchProcessor / MVVM  
- 画像解析: CellPose + PyTorch