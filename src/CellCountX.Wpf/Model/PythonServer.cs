using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;

namespace CellCountX.Wpf.Model;

public class PythonServer
{
    private readonly string _pythonExe;
    private readonly string _serverScript;
    private readonly string _workingDir;

    // 「今後はタイムアウト確認をしない」フラグ
    private static bool _disableTimeoutPrompt = false;

    private Process? process;
    private bool cancelRequested = false;

    public void RequestCancel()
    {
        cancelRequested = true;
        try { process?.Kill(); } catch { }
    }

    public PythonServer()
    {
        // ---------------------------------------------------------
        // 1. 配布版（AppContext.BaseDirectory/python/）を優先
        // ---------------------------------------------------------
        string baseDir = AppContext.BaseDirectory;

        string distPython = Path.Combine(baseDir, "python", "python", "python.exe");
        string distServer = Path.Combine(baseDir, "server.py");

        if (File.Exists(distPython) && File.Exists(distServer))
        {
            _pythonExe = distPython;
            _serverScript = distServer;
            _workingDir = baseDir;
            return;
        }

        // ---------------------------------------------------------
        // 2. 開発版（src/CellCountX.Py/）を fallback として使用
        // ---------------------------------------------------------
        // baseDir = bin/Debug/net10.0-windows/
        // → 4階層戻ると src/
        string devRoot = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\"));
        string devPython = Path.Combine(devRoot, "CellCountX.Py", "cellpose", "Scripts", "python.exe");
        string devServer = Path.Combine(devRoot, "CellCountX.Py", "server.py");

        if (File.Exists(devPython) && File.Exists(devServer))
        {
            _pythonExe = devPython;
            _serverScript = devServer;
            _workingDir = Path.Combine(devRoot, "CellCountX.Py");
            return;
        }

        // ---------------------------------------------------------
        // 3. どちらも見つからない場合はエラー
        // ---------------------------------------------------------
        throw new Exception("python.exe と server.py が見つかりません。\n" +
                            "配布版: <exe>/python/venv/Scripts/python.exe\n" +
                            "開発版: src/CellCountX.Py/venv/Scripts/python.exe");
    }

    // ---------------------------------------------------------
    // Python を 1 回起動して JSON を渡し、結果を受け取る
    // ---------------------------------------------------------
    public string RunOnce(string json, int timeoutSeconds)
    {
        cancelRequested = false;

        if (!File.Exists(_pythonExe))
            throw new FileNotFoundException($"python.exe が見つかりません: {_pythonExe}");

        if (!File.Exists(_serverScript))
            throw new FileNotFoundException($"server.py が見つかりません: {_serverScript}");

        var psi = new ProcessStartInfo
        {
            FileName = _pythonExe,
            Arguments = $"\"{_serverScript}\"",
            WorkingDirectory = _workingDir,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        process = new Process
        {
            StartInfo = psi
        };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                errorBuilder.AppendLine(e.Data);
        };

        // 起動
        process.Start();

        // 出力の非同期読み取り開始
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // JSON を Python に送信
        using (var sw = process.StandardInput)
        {
            sw.WriteLine(json);
        }

        // タイムアウト値をミリ秒に変換
        var timeoutMs = timeoutSeconds * 1000;

        while (true)
        {
            if (cancelRequested)
            {
                return "{\"error\": \"User cancelled\"}";
            }

            if (_disableTimeoutPrompt)
            {
                // タイムアウト確認なしで無限待ち
                // ただし、ユーザーがキャンセルを要求した場合はすぐに終了する
                while (!process.WaitForExit(1000))
                {
                    if (cancelRequested)
                    {
                        return "{\"error\": \"User cancelled\"}";
                    }
                }
                break;
            }

            if (process.WaitForExit(timeoutMs))
            {
                // 正常終了
                break;
            }

            // タイムアウト発生
            var result = MessageBox.Show(
                "Python の処理が規定時間内に終了しませんでした。\n" +
                "処理を中断して Python プロセスを強制終了しますか？",
                "タイムアウト",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.OK)
            {
                try { process.Kill(); } catch { }
                return "{\"error\": \"Python process timeout\"}";
            }
            else
            {
                // キャンセル → 今後どうするか確認
                var result2 = MessageBox.Show(
                    "今後、この処理ではタイムアウト確認を行わず待ち続けますか？\n\n" +
                    "「はい」→ 今後はタイムアウト確認なし（無限待ち）\n" +
                    "「いいえ」→ 今回だけ続行（次のタイムアウトで再度確認）",
                    "タイムアウト設定",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result2 == MessageBoxResult.Yes)
                {
                    _disableTimeoutPrompt = true;
                    continue;
                }
                else
                {
                    // 今回だけ続行
                    continue;
                }
            }
        }

        // 結果取得
        string output = outputBuilder.ToString().Trim();
        string error = errorBuilder.ToString().Trim();

        if (!string.IsNullOrEmpty(error))
        {
            return $"{{\"error\": \"{Escape(error)}\"}}";
        }

        process.Dispose();
        return output;
    }

    // JSON 文字列に安全に埋め込むためのエスケープ
    private static string Escape(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
