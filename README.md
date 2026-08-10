# 📘 CellCountX — Cellpose を GUI から扱える画像解析アプリ

CellCountX は **Cellpose + PyTorch** をバックエンドに用いて、  
GUI から画像フォルダを指定するだけで **細胞セグメンテーション・カウント・境界除去・CSV 出力・輪郭オーバーレイ生成**を行う WPF アプリケーションです。

---

## 🧩 配布版の種類

### 🟩 フル版（Python 同梱）

- Embeddable Python + Cellpose + PyTorch を同梱  
- Python のインストール不要  
- ダウンロード後すぐに Cellpose が利用可能  
- サイズは大きめ（数 GB）

### 🟦 軽量版（Python 非同梱）

- Python は同梱されない  
- ユーザーの Python（conda / PATH）を自動検出  
- サイズが非常に小さい（約 540KB）

---

## 🚀 主な機能

### 🧠 Cellpose 推論（GPU 対応）

- フル版：同梱 Python を使用  
- 軽量版：ユーザー環境の Python を自動検出  
- GPU（CUDA / ROCm / DirectML / MPS）利用可否を自動判定

### 🔍 Python 環境チェック

起動時に以下を自動判定します：

- Python の存在  
- Cellpose の import 可否  
- GPU 利用可否  
- Cellpose バージョン  

Python が利用できない場合は「開始」ボタンが無効化されます。

---

## 🧹 画像端の細胞除去（境界除去）

Cellpose は画像端の細胞を途切れた状態で検出することがあります。  
CellCountX では以下の設定により **境界細胞を除去**できます：

- 上端 / 下端 / 左端 / 右端  
- マージン（px）設定（初期値 2px）

除去された細胞はオーバーレイ画像で **赤色の輪郭**として描画されます。

---

## 🎨 輪郭オーバーレイ画像の生成（CellCountX 独自機能）

Cellpose のマスクをもとに、CellCountX が元画像へ輪郭を重ねた画像を生成します。

- 採用された細胞 → 緑の輪郭  
- 境界除去された細胞 → 赤の輪郭  

生成されるファイル（保存オプションが有効な場合）：

- `{base}_overlay.png` — CellCountX が生成する輪郭オーバーレイ画像  

---

## 🧩 Cellpose 標準出力（保存オプションが有効な場合）

- `{base}_cp_masks.png` — Cellpose 標準のマスク画像  
- `{base}_seg.npy` — flows / masks / styles を含む Cellpose 標準形式  

※ `{base}` は入力画像ファイル名（拡張子なし）

---

## 📄 解析設定ログ（settings.json）

「開始」ボタンを押すと、推論に使用した設定が `settings.json` として保存されます。

記録内容（概要）：

- Cellpose モデル  
- 境界除去設定  
- タイムアウト  
- GPU/CPU 使用状況  
- 入力・出力パス  

解析の再現性を確保するためのログです。

---

## 📊 バッチ処理 + CSV 出力

- フォルダ内の画像を一括処理  
- 進捗バー表示  
- `cells.csv` を出力  
  - FileName  
  - CellCount（Cellpose の検出数）  
  - FilteredCount（境界除去後の細胞数）

---

## 🧩 出力ファイル一覧

CellCountX は、選択した保存オプションに応じて以下のファイルを出力します。  
※ `{base}` は入力画像ファイル名（拡張子なし）を表します。

| 種類 | ファイル名 | 内容 |
| --- | --- | --- |
| 輪郭オーバーレイ画像 | `{base}_overlay.tif` | CellCountX が生成する輪郭画像（緑＝採用 / 赤＝境界除去） |
| マスク画像 | `{base}_cp_masks.png` | Cellpose 標準のマスク画像 |
| seg.npy | `{base}_seg.npy` | flows / masks / styles を含む Cellpose 標準形式 |
| 解析設定ログ | `settings.json` | Cellpose 推論に使用した設定の記録 |
| 解析結果 | `cells.csv` | ファイル名・CellCount・FilteredCount の一覧 |

---

## 🖥️ 使い方

1. 画像フォルダを選択  
2. 出力フォルダを選択  
3. 詳細設定を開く  
   - GPU 使用  
   - 境界細胞除去  
   - 保存オプション（マスク / seg.npy / オーバーレイ）  
   - タイムアウト  
4. 詳細設定を閉じる  
5. 「開始」でバッチ処理開始  
   - `settings.json` と `cells.csv` が保存されます  
6. 「キャンセル」で即時中断

---

## ⚠️ 注意事項

- Cellpose が Unicode パスに対応していないため、全角パスは使用不可  
- フル版は同梱 Python を使用  
- 軽量版はユーザー環境の Python を自動検出  

---

## ⚙️ WPF (MVVM) アーキテクチャ

- UI とロジックを分離  
- 非同期処理 + キャンセル対応  
- PythonServer → PythonClient → BatchProcessor の三層構造  
- 状態に応じて UI を自動制御

---

## 🧩 アーキテクチャ概要

### PythonServer（C#）

- Python を起動して server.py を実行  
- Cellpose import / GPU 利用可否を判定  
- 推論を JSON で送受信  
- タイムアウト時はプロセスを Kill

### server.py（Python）

- Cellpose 推論  
- 境界細胞除去  
- マスク・seg.npy 保存  
- オーバーレイ画像生成  
- 結果を JSON で返却

### BatchProcessor

- 画像フォルダを走査  
- PythonServer を呼び出し  
- CSV 出力  
- 非同期 + キャンセル対応

---

## 📂 プロジェクト構成

### WPF プロジェクト（CellCountX.Wpf）

```bash
CellCountX.Wpf/
├── View/
├── ViewModel/
├── Logic/
├── Model/
├── python_embed/
└── CellCountX.Wpf.csproj
```

### Python バックエンド（開発用）

```bash
CellCountX.Py/
├── server.py
├── get_cellpose_info.py
├── remove_edge_cells.py
├── overlay.py
└── cellpose/
```

### 配布時の構成（フル版）

```bash
CellCountX/
├── CellCountX.exe
├── server.py
├── get_cellpose_info.py
├── remove_edge_cells.py
├── overlay.py
└── python/
    └── site-packages/
```

### 配布時の構成（軽量版）

```bash
CellCountX/
├── CellCountX.exe
├── server.py
├── get_cellpose_info.py
├── remove_edge_cells.py
└── overlay.py
```

---

## 📄 CSV 出力形式

| FileName | CellCount | FilteredCount |
| --- | --- | --- |
| image001.png | 123 | 120 |
| image002.png | 98 | 95 |

---

## 🛠️ 開発者向け：Cellpose バックエンド環境構築

```bash
python -m venv cellpose
cellpose\Scripts\activate
pip install cellpose
pip uninstall torch
pip3 install torch torchvision --index-url https://download.pytorch.org/whl/cu126
pip install packaging
```

---

## 🛠️ Release ビルドの自動化（GitHub Actions）

- フル版：Python runtime を同梱し ZIP を分割
- 軽量版：Python 非同梱で軽量 ZIP を生成

---

## 📦 分割 ZIP の結合方法

GitHub の 2GB 制限を回避するため、CellCountX のフル版は
**複数の分割 ZIP（.zip.001, .zip.002, ...）として配布**されています。

### 1. すべての分割 ZIP をダウンロード

例：

```
CellCountX-vX.Y.Z.zip.001
CellCountX-vX.Y.Z.zip.002
```

> ⚠️ **すべて同じフォルダに保存してください。**
1つでも欠けていると結合できません。
>

### 2. ZIP を結合して展開

#### 方法 A（推奨）：7-Zip で `.zip.001` を開く

1. `.zip.001` を右クリック
2. **7-Zip → 「展開」** を選択
3. `.zip.002` 以降も自動的に読み込まれます

> 最も簡単で確実な方法です。
> 

#### 方法 B（上級者向け）：コマンドラインで結合

> ⚠️ **PowerShell では動作しません。必ず cmd.exe を使用してください。**
PowerShell は `copy /b` を内部コマンドとして扱わないためエラーになります。
> 
1. Windows の検索で **cmd** と入力し「コマンドプロンプト」を開く
2. 分割 ZIP があるフォルダへ移動
3. 以下を実行：
    
    ```bash
    copy /b CellCountX-vX.Y.Z.zip.001 + CellCountX-vX.Y.Z.zip.002 CellCountX.zip
    ```
    
4. 結合された `CellCountX.zip` を展開：
    
    ```bash
    7z x CellCountX.zip
    ```
    

---

## 📜 ライセンス

MIT ライセンスを推奨します。
Cellpose のライセンスに従います。

---

## 🙌 作者

- 開発: BPSE-Lab
- アーキテクチャ設計: PythonServer / PythonClient / BatchProcessor / MVVM
- 画像解析: Cellpose + PyTorch