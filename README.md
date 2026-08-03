# 📘 CellCountX — CellPose を GUI から扱える画像解析アプリ

CellCountX は、**CellPose + PyTorch** をバックエンドに用いて、GUI から画像フォルダを指定するだけで  
**セルセグメンテーション・カウント・CSV 出力・輪郭オーバーレイ生成**を行う WPF アプリケーションです。

Python は **Embeddable Python** としてアプリに同梱されており、ユーザー側で Python をインストールする必要はありません。

---

## 🚀 主な特徴

### 🧠 CellPose を利用した細胞セグメンテーション

- Embeddable Python + CellPose + PyTorch を同梱
- server.py に JSON を渡して推論を実行
- GPU が利用可能な環境では CUDA を使用（任意）

### 🎨 輪郭オーバーレイ画像の自動生成（緑＝採用 / 赤＝除外）

CellPose のマスクをもとに、元画像（グレースケール）へ輪郭を重ねた画像を生成します。

- **採用された細胞 → 緑の輪郭**
- **画像端で途切れた細胞（除外） → 赤の輪郭**

生成されるファイル：

- `{base}_overlay.png`  
- `{base}_cp_masks.tif`

### 🧹 画像端の細胞除去（境界除去）

CellPose は画像端にある細胞を「途切れた状態」で検出することがあります。  
CellCountX では、以下の設定により **画像端の細胞を自動除去**できます。

- 上端の細胞を除去  
- 下端の細胞を除去  
- 左端の細胞を除去  
- 右端の細胞を除去  
- **マージン（px）設定**  
  - CellPose の境界は端から 1〜2px 内側に生成されるため、初期値は 2px を推奨

除去された細胞は **赤色の輪郭**でオーバーレイ画像に描画されます。

### ⚙️ WPF (MVVM) アーキテクチャ

- UI とロジックを分離
- MainViewModel が UI 状態を管理
- 非同期処理 + キャンセル対応

### 📦 Embeddable Python を ZIP から自動展開

- `python-3.12.10-embed-amd64.zip` を Release ビルド時に自動展開
- `_pth` ファイルをテンプレートから生成
- site-packages（CellPose など）を自動コピー（__pycache__ は除外）

### 📊 バッチ処理 + CSV 出力

- 指定フォルダ内の画像を一括処理
- 進捗バー表示
- CSV 出力（ファイル名・CellPose カウント・境界除去後のカウント）

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
│   └── python312._pth.template
├── python-3.12.10-embed-amd64.zip
└── CellCountX.Wpf.csproj
```

### Python バックエンド（開発用）

```
CellCountX.Py/
├── server.py
├── remove_edge_cells.py
├── overlay.py
└── cellpose/
```

### 配布時の構成（Release ビルド後）

```
CellCountX/
├── CellCountX.exe
├── server.py
├── remove_edge_cells.py
├── overlay.py
└── python/
    ├── python.exe
    ├── python312.dll
    ├── python312.zip
    ├── python312._pth
    └── site-packages/
```



---

## 🖥️ 使い方

1. **画像フォルダを選択**  
2. **出力フォルダを選択**  
3. **GPU 使用の有無を選択**  
4. **境界細胞除去の設定（任意）**  
   - 上 / 下 / 左 / 右  
   - マージン（px）
5. **「開始」ボタンでバッチ処理開始**  
6. **「キャンセル」で即時中断**

※ タイムアウトは **メニュー → 詳細設定** から変更できます。

---

## 🖼️ 出力される画像

| 種類 | ファイル名 | 内容 |
|------|------------|------|
| マスク画像 | `{base}_cp_masks.tif` | CellPose のラベルマスク |
| 輪郭オーバーレイ画像 | `{base}_overlay.png` | 緑＝採用 / 赤＝除外（境界除去） |

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
- 画像端の細胞除去（境界除去）
- マスク画像保存
- 輪郭オーバーレイ画像生成（緑＝採用 / 赤＝除外）
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
| image002.png | 98 | 95 |

※ FilteredCount は **境界除去後の細胞数**です。

---

## 🛠 開発者向け：Embeddable Python の準備

```
CellCountX.Wpf/python-3.12.10-embed-amd64.zip
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
```

---

## 🛠️ Release ビルドの自動化（csproj）

Release ビルド時に以下が自動実行されます：

1. Embeddable Python ZIP を展開
2. python/ にコピー
3. `_pth` を生成
4. site-packages をコピー（__pycache__ は除外）
5. server.py / remove_edge_cells.py / overlay.py を exe と同じ場所に配置
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

## 📜 ライセンス

- CellPose のライセンスに従います  
- 本アプリケーションは MIT ライセンスを推奨します

---

## 🙌 作者

- 開発: BPSE-Lab  
- アーキテクチャ設計: PythonServer / PythonClient / BatchProcessor / MVVM  
- 画像解析: CellPose + PyTorch