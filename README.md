# 📘 CellCountX — CellPose を GUI から扱える画像解析アプリ

CellCountX は、**CellPose + PyTorch** をバックエンドに用いて、GUI から画像フォルダを指定するだけで **セルセグメンテーション・カウント・CSV 出力・輪郭オーバーレイ生成**を行う WPF アプリケーションです。

Python は **Embeddable Python** としてアプリに同梱されており、ユーザー側で Python をインストールする必要はありません。

---

## 🚀 主な特徴

### 🧠 CellPose を利用した細胞セグメンテーション

- Embeddable Python + CellPose + PyTorch を同梱
- server.py に JSON を渡して推論を実行
- GPU が利用可能な環境では CUDA を使用（任意）

### 🎨 輪郭オーバーレイ画像の自動生成

CellPose のマスクをもとに、元画像（グレースケール）へ輪郭を重ねた画像を生成します。

- **RF フィルタ未使用時**  
  → すべての細胞を **赤色**で輪郭表示

- **RF フィルタ使用時**  
  → **RF=1（残す） → 緑の輪郭**  
  → **RF=0（除外） → 赤の輪郭**

生成されるファイル：

- `{base}_overlay.png`  
- `{base}_cp_masks.tif`

### 🧪 RF（Random Forest）による細胞フィルタリング

CellPose のマスクに対して、**Random Forest モデル（rf_keep_adherent.pkl）** を用いた細胞分類を行えます。

使用する特徴量（rf_filter.py に準拠）：

| 特徴量 | 説明 |
|--------|------|
| **area** | 細胞領域の面積 |
| **circularity** | 円形度（4πA/P²） |
| **mean_intensity** | 平均輝度 |
| **variance** | 輝度分散 |

RF の出力：

- **1（keep） → 緑の輪郭で表示**  
- **0（remove） → 赤の輪郭で表示**

### ⚙️ WPF (MVVM) アーキテクチャ

- UI とロジックを分離
- MainViewModel が UI 状態を管理
- 非同期処理 + キャンセル対応

### 📦 Embeddable Python を ZIP から自動展開

- `python-3.10.11-embed-amd64.zip` を Release ビルド時に自動展開
- `_pth` ファイルをテンプレートから生成
- site-packages（CellPose など）を自動コピー（__pycache__ は除外）

### 📊 バッチ処理 + CSV 出力

- 指定フォルダ内の画像を一括処理
- 進捗バー表示
- CSV 出力（ファイル名・CellPose カウント・RF フィルタ後のカウント）

### 🧹 タイムアウト + 安全な Kill

- Python が応答しない場合はプロセスを強制終了
- 次の画像へ自動的に進む  
（※ タイムアウト設定は **メニュー → 詳細設定** に移動）

### 🔤 全角パス禁止

CellPose の制約により、フォルダ名・ファイル名に全角文字が含まれる場合は中断します。

---

## 📂 プロジェクト構成

### WPF プロジェクト（CellCountX.Wpf）

```
CellCountX.Wpf/
├── View/
├── ViewModel/
├── Logic/
├── Model/
├── python_embed/
│   └── python310._pth.template
├── python-3.10.11-embed-amd64.zip
└── CellCountX.Wpf.csproj
```

### Python バックエンド（開発用）

```
CellCountX.Py/
├── server.py
├── rf_filter.py
├── overlay.py
├── rf_keep_adherent.pkl
└── cellpose/
```

### 配布時の構成（Release ビルド後）

```
CellCountX/
├── CellCountX.exe
├── server.py
├── rf_filter.py
├── overlay.py
├── rf_keep_adherent.pkl
└── python/
    ├── python.exe
    ├── python310.dll
    ├── python310.zip
    ├── python310._pth
    └── site-packages/
```


---

## 🖥️ 使い方

1. **画像フォルダを選択**  
2. **出力フォルダを選択**  
3. **GPU 使用の有無を選択**  
4. **接着細胞フィルタリングの有無を選択**
5. **「開始」ボタンでバッチ処理開始**  
6. **「キャンセル」で即時中断**

※ タイムアウトは **メニュー → 詳細設定** から変更できます。

---

## 🖼️ 出力される画像

| 種類 | ファイル名 | 内容 |
|------|------------|------|
| マスク画像 | `{base}_cp_masks.tif` | CellPose のラベルマスク |
| 輪郭オーバーレイ画像 | `{base}_overlay.png` | RF 使用時は緑（keep）/ 赤（remove）で輪郭表示 |

---

## ⚠️ 注意事項

### 🔸 全角文字を含むパスは使用不可

CellPose が Unicode パスに対応していないため。

### 🔸 Python 実行環境について

- 配布版は **python/ フォルダ内の Embeddable Python** を使用
- 開発時は `CellCountX.Py/` の venv を自動検出して使用

---

## 🧩 アーキテクチャ概要

### PythonServer（C#）

- Embeddable Python の python.exe を起動
- server.py に JSON を渡して推論
- タイムアウト時はプロセスを Kill

### server.py（Python）

- CellPose 推論
- **RF フィルタによる細胞分類**
- マスク画像保存
- 輪郭オーバーレイ画像生成（緑/赤）
- JSON で結果を返す

### BatchProcessor

- 画像フォルダを走査
- PythonServer を呼び出し
- CSV 出力
- 非同期 + キャンセル対応

---

## 📄 CSV 出力形式

| FileName | CellCount | FilteredCount |
|----------|-----------|---------------|
| image001.png | 123 | 120 |
| image002.png | 98 | 98 |

---

## 🛠 開発者向け：Embeddable Python の準備

```
CellCountX.Wpf/python-3.10.11-embed-amd64.zip
```

- ZIP は Git に含める  
- 展開後のフォルダは Git 管理しない  
- Release ビルド時に ZIP → 展開 → python/ にコピーされる

---

## 🛠️ 開発者向け：CellPose バックエンド環境構築

```bash
python -m venv cellpose
cellpose\Scripts\activate
pip install cellpose
pip install torch torchvision --index-url https://download.pytorch.org/whl/cu126
pip install packaging
pip install scikit-image
pip install scikit-learn
```

---

## 🛠️ Release ビルドの自動化（csproj）

Release ビルド時に以下が自動実行されます：

1. Embeddable Python ZIP を展開
2. python/ にコピー
3. `_pth` を生成
4. site-packages をコピー（__pycache__ は除外）
5. server.py / rf_filter.py / overlay.py / RF モデルを exe と同じ場所に配置
6. Clean 時に python/ と関連ファイルを削除

---

## 📦 分割 ZIP の結合方法

GitHub の 2GB 制限を回避するため、CellCountX は**複数の分割 ZIP（.zip.001, .zip.002, ...）として配布**されています。

以下の手順で **1つの ZIP に結合 → 展開** してください。

### 1. すべての分割 ZIP をダウンロード

例：

```
CellCountX-vX.Y.Z.zip.001
CellCountX-vX.Y.Z.zip.002
```

> ⚠️ **すべて同じフォルダに保存してください。**  
> 1つでも欠けていると結合できません。

### 2. ZIP を結合して展開

#### 方法 A（推奨）：7-Zip で `.zip.001` を開く

1. `.zip.001` を右クリック  
2. **7-Zip → 「展開」** を選択  
3. `.zip.002` 以降も自動的に読み込まれます

> 最も簡単で確実な方法です。

#### 方法 B（上級者向け）：コマンドラインで結合

> ⚠️ **PowerShell では動作しません。必ず cmd.exe を使用してください。**  
> PowerShell は `copy /b` を内部コマンドとして扱わないためエラーになります。

1. Windows の検索で **cmd** と入力し「コマンドプロンプト」を開く  
2. 分割 ZIP があるフォルダへ移動  
3. 以下を実行：

    ```cmd
    copy /b CellCountX-vX.Y.Z.zip.001 + CellCountX-vX.Y.Z.zip.002 + CellCountX-vX.Y.Z.zip.003 CellCountX.zip
    ```

4. 結合された `CellCountX.zip` を展開：

    ```cmd
    7z x CellCountX.zip
    ```

### 3. CellCountX.Wpf.exe を実行

展開されたフォルダ内の `CellCountX.Wpf.exe` を起動してください。

---

## 📚 RF モデル学習（CellCountX.Py/train）

CellCountX では、CellPose の出力マスクに対して **Random Forest による細胞分類（接着 / 非接着 / ゴミ）** を行うため、学習済みモデル `rf_keep_adherent.pkl` を使用します。

このモデルは `CellCountX.Py/train` フォルダ内のスクリプトを使って作成できます。

```
CellCountX.Py/
└── train/
    ├── extract_features_folder.py
    ├── rf_filter_batch.py
    ├── train_rf_keep_adherent.py
    └── all_objects_features.csv   ← 特徴量 + ラベルの教師データ
```

### 1. extract_features_folder.py

**CellPose のマスクから特徴量を抽出し、CSV を作成するスクリプト**

CellPose が生成した `{name}_cp_masks.tif` と元画像（.jpg）を入力として、各オブジェクト（細胞）について以下の特徴量を抽出します。

| 特徴量 | 説明 |
| --- | --- |
| area | 細胞領域の面積 |
| circularity | 円形度（4πA/P²） |
| mean_intensity | 平均輝度 |
| variance | 輝度分散 |

出力される CSV（例：`all_objects_features.csv`）には、**画像名・ラベル番号・4つの特徴量**が行単位で記録されます。

> この CSV に対して、あなたが手動で「接着 / 非接着 / ゴミ」のラベルを付けます  
> （train_rf_keep_adherent.py が利用）

### 2. train_rf_keep_adherent.py

**Random Forest モデル（rf_keep_adherent.pkl）を学習するスクリプト**

`all_objects_features.csv` を読み込み、あなたが付けたラベルをもとに **接着（1） vs 非接着＋ゴミ（0）** の 2 クラス分類モデルを学習します。

- 使用する特徴量
  - area
  - circularity
  - mean_intensity
  - variance
- モデル
  - RandomForestClassifier
  - n_estimators=300
  - class_weight="balanced"

出力：

```
rf_keep_adherent.pkl   ← CellCountX が使用する学習済みモデル
rf_keep_adherent.json  ← JSON 形式のモデル（任意）
```

### 3. rf_filter_batch.py

**学習済み RF モデルを使って、フォルダ内の画像を一括分類するスクリプト**

CellPose のマスクと元画像を読み込み、`rf_keep_adherent.pkl` を使って各オブジェクトを分類します。

- **RF=1 → 残す（緑）**
- **RF=0 → 除外（赤）**

出力：

- `{name}_overlay.png`（緑/赤の輪郭オーバーレイ）
- `rf_features.csv`（特徴量 + RF 判定）
- `rf_counts.csv`（before / after の個数）

> CellCountX 本体が行っている処理と同じロジックです  
> （CellPoseで処理済みのマスク画像をバッチ処理したい場合に便利）

### 4. モデル学習の流れ（まとめ）

1. CellPose でマスクを生成
2. `extract_features_folder.py` で特徴量 CSV を作成
3. CSV に手動でラベル（1=接着, 0/2=除外）を付ける
4. `train_rf_keep_adherent.py` で RF モデルを学習
5. `rf_keep_adherent.pkl` を CellCountX.exe と同じフォルダに置く

CellCountX は起動時に `rf_keep_adherent.pkl` を読み込み、 **RF フィルタを使った細胞分類（緑/赤の輪郭）**を行います。

---

## 🧬 RF フィルタ（rf_filter.py）

CellPose のマスクに対して、**Random Forest による細胞分類**を行います。

### 使用する特徴量

- area  
- circularity  
- mean_intensity  
- variance  

### 出力

- `CellCount`（CellPose の生データ）
- `FilteredCount`（RF による分類後の細胞数）
- オーバーレイ画像（緑＝keep、赤＝remove）

---

## 📜 ライセンス

- CellPose のライセンスに従います  
- 本アプリケーションは MIT ライセンスを推奨します

---

## 🙌 作者

- 開発: BPSE-Lab  
- アーキテクチャ設計: PythonServer / PythonClient / BatchProcessor / MVVM  
- 画像解析: CellPose + PyTorch