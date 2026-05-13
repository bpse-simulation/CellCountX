# 📘 CellCountX

高精度な細胞カウントを GUI で実行できる WPF アプリケーションです。
バックエンドには **Python（Embeddable 版）+ CellPose + PyTorch** を使用し、
画像フォルダを指定するだけで **バッチ処理・GPU 対応・CSV 出力** を自動で行います。

Python はアプリに完全同梱されているため、
**ユーザー側で Python をインストールする必要はありません。**

## 🚀 特徴

- **WPF (MVVM) アーキテクチャ**
    - UI とロジックを完全分離
    - MainViewModel が UI 状態を一元管理
- **Python + CellPose による高精度カウント**
    - Embeddable Python を同梱（python/ フォルダ）
    - server.py を通じて CellPose + PyTorch を実行
    - JSON ベースで通信し、堅牢で拡張性の高い構成
- **GPU 対応**
    - CUDA が利用可能な環境では自動的に GPU を使用
    - GPU 使用率が上昇することを確認済み
- **バッチ処理**
    - 指定フォルダ内の画像を一括処理
    - 進捗バー表示
    - CSV 出力（ファイル名・カウント）
- **非同期処理**
    - UI が固まらない
    - キャンセルが即時反映
- **タイムアウト対応**
    - Python 側が応答しない場合はプロセスを Kill
    - ログに記録し、次のファイルへ進む
- **全角パス禁止**
    - CellPose の制約により、フォルダ名・ファイル名に全角文字が含まれる場合は中断
- **ログ自動スクロール**
    - LogText 更新時に ScrollViewer が最下部へ移動

## 📂 プロジェクト構成

### WPF プロジェクト（CellCountX.Wpf）

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
├── python_embed/                   ← site-packages（テンプレート）
├── python-3.10.11-embed-amd64.zip  ← Embeddable Python
└── CellCountX.Wpf.csproj
```

### Python バックエンド（開発用 venv）

```
CellCountX.Py/
├── server.py
└── cellpose/
    ├── Scripts/python.exe
    └── Lib/site-packages/
```

### 配布時の構成（Release ビルド後）

```
CellCountX/
├── CellCountX.exe
├── server.py
└── python/
    ├── python.exe
    ├── python310.dll
    ├── python310.zip
    ├── python310._pth
    └── site-packages/
        ├── cellpose/
        ├── torch/
        │   └── lib/*.dll
        ├── imagecodecs/
        ├── numpy/
        ├── scipy/
        └── ...
```

## 🖥️ 使い方

### 1. 画像フォルダを選択

「参照…」ボタンから画像フォルダを指定します。

### 2. 出力フォルダを選択

CSV が保存されるフォルダを指定します。

### 3. GPU 使用の有無を選択

GPU が利用可能な環境では高速化できます。

### 4. タイムアウト秒数を設定

Python が応答しない場合の強制終了時間です。

### 5. 「開始」ボタンを押す

バッチ処理が開始されます。

### 6. 「キャンセル」ボタンで中断可能

即時に処理が停止します。

## ⚠️ 注意事項

### 🔸 全角文字を含むパスは使用できません

CellPose の制約により、以下に全角文字が含まれる場合は処理を中断します：

- 入力フォルダ
- 出力フォルダ
- 画像ファイル名

### 🔸 Python 実行環境について

- 配布版は **python/ フォルダ内の Embeddable Python** を使用
- 開発時は `CellCountX.Py/venv` を自動検出して使用

## 🧩 アーキテクチャ概要

### PythonServer

- python.exe + server.py を起動
- JSON を渡して結果を受け取る
- タイムアウト時はプロセスを Kill
- UI 非依存

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
- UI のみを担当

## 📄 CSV 出力形式

| FileName | CellCount |
| --- | --- |
| image001.png | 123 |
| image002.png | 98 |

## 🛠 開発者向け：Embeddable Python の準備

CellCountX は配布版で Embeddable Python を使用します。
開発時には、リポジトリに含まれる以下の zip を展開してください。

```
CellCountX.Wpf/python-3.10.11-embed-amd64.zip
```

### 展開先

```
CellCountX.Wpf/python-3.10.11-embed-amd64/
```

展開後、python_embed は次のようになります：

```
python-3.10.11-embed-amd64/
  python.exe
  python310.dll
  python310.zip
  python310._pth
  ...
```

### 注意

- zip は **Git に含める（軽量・バージョン固定のため）**
- 展開後の python_embed/ は **Git 管理しない**
- Release ビルド時に python_embed → python/ に自動コピーされます

## 🛠️ 開発者向け：CellPose バックエンド環境構築

CellCountX は **Embeddable Python を配布版に同梱**しますが、
開発者は venv を使って CellPose + PyTorch を構築し、
**site-packages を抽出して python/ にコピー**します。

### 1. venv を作成

```
python -m venv cellpose
cellpose\Scripts\activate
```

### 2. ライブラリをインストール

```
pip install cellpose
pip3 install torch torchvision --index-url https://download.pytorch.org/whl/cu126
```

## 🛠️ 開発者向け：Release ビルドの自動化

CellCountX.Wpf.csproj には、配布版を自動生成するためのビルドタスクが含まれています。
Release ビルドを実行すると、以下の処理が自動で行われます。

### 1. Embeddable Python のコピー

`python-3.10.11-embed-amd64/` に展開された Embeddable Python 一式が  
出力フォルダの `python/` にコピーされます。

```
(OutDir)/python/
  python.exe
  python310.dll
  python310.zip
  ...
```

### 2. `_pth` ファイルの生成

`python310._pth.template` を元に、  
実行時に必要な `python310._pth` が自動生成されます。

テンプレート内容：

```
python310.zip
.
./site-packages
import site
```

### 3. site-packages のコピー

開発用 venv から抽出した `python_embed/site-packages/` が  
そのまま `(OutDir)/python/site-packages/` にコピーされます。

```
(OutDir)/python/site-packages/
  cellpose/
  torch/lib/*.dll
  imagecodecs/
  numpy/
  scipy/
  ...
```

### 4. server.py のコピー

バックエンド処理を行う `server.py` が  
出力フォルダ直下にコピーされます。

### 5. Clean 時の自動削除

`Clean` を実行すると、  
出力フォルダ内の `python/` と `server.py` が自動削除されます。

---

これらの処理により、Release ビルドを行うだけで  
**配布可能な完全版 CellCountX が自動生成されます。**

## 📜 ライセンス

- CellPose のライセンスに従います
- 本アプリケーション自体は MIT ライセンスを推奨します

## 🙌 作者

- 開発: BPSE-Lab
- アーキテクチャ設計: PythonServer / PythonClient / BatchProcessor / MVVM
- 画像解析: CellPose + PyTorch