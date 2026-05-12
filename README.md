# CellCountX  
高精度な細胞カウントを GUI で実行できる WPF アプリケーションです。  
バックエンドには Python + CellPose を使用し、画像フォルダを指定するだけで  
**バッチ処理・GPU 対応・CSV 出力** を自動で行います。

---

## 🚀 特徴

- **WPF (MVVM) アーキテクチャ**
  - UI とロジックを完全分離
  - MainViewModel が UI 状態を一元管理

- **Python + CellPose による高精度カウント**
  - PythonServer が python.exe + server.py を安全に起動
  - PythonClient が JSON ベースで通信
  - JSON パースは JsonParser が担当

- **バッチ処理**
  - 指定フォルダ内の画像を一括処理
  - 進捗バー表示
  - CSV 出力（ファイル名・カウント）

- **非同期処理**
  - UI が固まらない
  - キャンセルが即時反映

- **タイムアウト対応**
  - Python 側が応答しない場合はプロセスを Kill
  - タイムアウトはログに記録し、次のファイルへ進む

- **全角パス禁止**
  - CellPose の制約により、フォルダ名・ファイル名に全角文字が含まれる場合は
    **最初に中断してログを出す**

- **ログ自動スクロール**
  - LogText が更新されるたびに ScrollViewer が最下部へ移動

---

## 📂 プロジェクト構成

### Visual Studio プロジェクト（WPF 本体）

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
└── CellCountX.Wpf.csproj
```

### Python バックエンド（VS 管理外フォルダ）

```
CellCountX.Py/
├── server.py
└── cellpose/
    └── Scripts/python.exe
```

### 配布時の構成（exe と同階層）

```
CellCountX/
├── CellCountX.exe
├── server.py
└── python/
    └── python.exe
```

---

## 🖥️ 使い方

### 1. 画像フォルダを選択  
「参照…」ボタンから画像フォルダを指定します。

### 2. 出力フォルダを選択  
CSV が保存されるフォルダを指定します。

### 3. GPU 使用の有無を選択  
GPU が利用可能な環境ではチェックを入れると高速化できます。

### 4. タイムアウト秒数を設定  
Python が応答しない場合の強制終了時間です。

### 5. 「開始」ボタンを押す  
バッチ処理が開始されます。

### 6. 「キャンセル」ボタンで中断可能  
即時に処理が停止し、開始ボタンが再び押せるようになります。

---

## ⚠️ 注意事項

### 🔸 全角文字を含むパスは使用できません  
CellPose の制約により、以下に全角文字が含まれる場合は処理を中断します：

- 入力フォルダ
- 出力フォルダ
- 画像ファイル名

### 🔸 Python 実行環境について  
アプリ内に同梱された python.exe + server.py を優先的に使用します。  
開発環境では `CellCountX.Py` フォルダの Python を自動検出します。

---

## 🧩 アーキテクチャ概要

### PythonServer  
- python.exe + server.py を起動  
- JSON を渡して結果を受け取る  
- タイムアウト時はプロセスを Kill  
- UI 依存なし（MessageBox などは使用しない）

### PythonClient  
- PythonServer の結果を `PythonResponse` に変換  
- エラーを吸収して上位に返す

### BatchProcessor  
- 画像フォルダを走査  
- PythonClient を使って 1 ファイルずつ処理  
- JSON パース  
- CSV 出力  
- ログ・進捗・完了イベントを発行  
- 非同期 + キャンセル対応

### MainViewModel  
- UI 状態（IsRunning, ProgressValue, LogText）を管理  
- コマンド（開始・キャンセル）を提供  
- BatchProcessor のイベントを受け取り UI に反映

### MainWindow  
- ViewModel の PropertyChanged を監視し  
  LogScrollViewer.ScrollToEnd() を実行  
- UI のみを担当（ロジックなし）

---

## 📄 CSV 出力形式

| FileName | CellCount |
| --- | --- |
| image001.png | 123 |
| image002.png | 98 |

---

## 🛠️ 開発者向け：CellPose バックエンド環境構築  
CellCountX は Python + CellPose をバックエンドとして使用します。  
開発者は CellPose 公式 GitHub の **Option 2: Installation Instructions with python's venv** に従って、  
独立した Python 実行環境（venv）を構築してください。

以下は CellCountX 用に最適化した手順です。

---

### 1. Python をインストール  
Python 3.8〜3.12 のいずれかを推奨します。

### 2. CellCountX.Py フォルダを作成  
Visual Studio プロジェクトとは別に、任意の場所に以下のフォルダを作成します：

```
CellCountX.Py/
```

※ このフォルダは **VS のプロジェクトに含めません**（巨大化・依存衝突を避けるため）。

### 3. venv を作成  
CellCountX.Py 内で仮想環境を作成します。

```bash
python -m venv cellpose
```

### 4. venv を有効化
Windows:

```bash
cellpose\Scripts\activate
```

Linux/Mac:

```bash
source cellpose/bin/activate
```

### 5. pip を最新化

```bash
pip install --upgrade pip
```

### 6. CellPose をインストール
CellPose 公式の venv 手順に従い、pip でインストールします。

```bash
pip install cellpose
```

GUI が不要な場合はこれだけで十分です。

GPU を使用する場合は PyTorch を別途インストールしてください（CUDA 版）。

```bash
pip3 install torch torchvision --index-url https://download.pytorch.org/whl/cu126
```

### 7. server.py を配置
CellCountX.Py フォルダに server.py を配置します：

```
CellCountX.Py/
├── server.py
└── venv/
    └── Scripts/python.exe
```

### 8. WPF 側の PythonServer が自動検出
CellCountX.Wpf の PythonServer は、開発時に以下の順で python.exe を探します：

1. CellCountX.Py/venv/Scripts/python.exe
2. 配布時の ./python/python.exe
3. システムの PATH（通常は使わない）

そのため、開発者は CellCountX.Py/cellpose を作るだけで OK です。

### ビルド要件

- .NET 10.0
- Windows 10/11
- WPF
- Python 3.10（同梱版を使用するため通常は不要）

### デバッグ時の Python 検出

```
CellCountX.Py/
├── cellpose/Scripts/python.exe
└── server.py
```

が自動的に検出されます。

---

## 📜 ライセンス

CellPose のライセンスに従います。  
本アプリケーション自体は MIT ライセンスを推奨します。

---

## 🙌 作者

- 開発: BPSE-Lab
- アーキテクチャ設計: PythonServer / PythonClient / BatchProcessor / MVVM  
- 画像解析: CellPose
