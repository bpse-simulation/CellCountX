using System.Diagnostics;
using System.IO;
using System.Text;

namespace CellCountX.Wpf.Model;

public class PythonServer
{
    private string _pythonExe = "";
    private string _serverScript = "";
    private string _workingDir = "";
    private string _infoScript = "";

    private Process? _process;

    // ---------------------------------------------------------
    // Python 環境チェックの結果を表すクラス
    // ---------------------------------------------------------
    public class PythonEnvCheckResult
    {
        public bool IsAvailable { get; set; }
        public string Message { get; set; } = "";
    }

    // ---------------------------------------------------------
    // 非同期で呼び出す Python 環境チェック
    // ---------------------------------------------------------
    public PythonEnvCheckResult CheckPythonEnvironment()
    {
#if DEBUG
        string baseDir = AppContext.BaseDirectory;
        string devRoot = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\.."));
        string devPython = Path.Combine(devRoot, "CellCountX.Py", "cellpose", "Scripts", "python.exe");
        string devServer = Path.Combine(devRoot, "CellCountX.Py", "server.py");
        string devInfo = Path.Combine(devRoot, "CellCountX.Py", "get_cellpose_info.py");

        if (File.Exists(devPython) && File.Exists(devServer) && File.Exists(devInfo))
        {
            _pythonExe = devPython;
            _serverScript = devServer;
            _infoScript = devInfo;
            _workingDir = Path.Combine(devRoot, "CellCountX.Py");

            return new PythonEnvCheckResult
            {
                IsAvailable = true,
                Message = "Debug：開発用 Python を使用します。"
            };
        }

        return new PythonEnvCheckResult
        {
            IsAvailable = false,
            Message = "Debug モード：開発用 Python が見つかりません。"
        };
#else
        if (TryFindBundledPython())
            return new PythonEnvCheckResult { IsAvailable = true, Message = "同梱 Python を使用します。" };

        if (TryFindCondaCellpose())
            return new PythonEnvCheckResult { IsAvailable = true, Message = "Conda の CellPose 環境を使用します。" };

        if (TryFindExistingPython())
            return new PythonEnvCheckResult { IsAvailable = true, Message = "既存の Python 環境を使用します。" };

        return new PythonEnvCheckResult
        {
            IsAvailable = false,
            Message = "CellPose を実行できる Python が見つかりません。Python をセットアップしてください。"
        };
#endif
    }

    // ---------------------------------------------------------
    // conda Python の CellPose 環境を探す
    // ---------------------------------------------------------
    private bool TryFindCondaCellpose()
    {
        string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        string[] candidates =
        [
            Path.Combine(user, "miniconda3", "envs", "cellpose", "python.exe"),
            Path.Combine(user, "anaconda3", "envs", "cellpose", "python.exe")
        ];

        foreach (var exe in candidates)
        {
            if (!File.Exists(exe))
                continue;

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = "-c \"import cellpose\"",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using var p = Process.Start(psi);
                if (p == null) continue;

                if (p.WaitForExit(10000) && p.ExitCode == 0)
                {
                    // Python は OK → 次に server.py / version script の存在を確認
                    string baseDir = AppContext.BaseDirectory;

                    string server = Path.Combine(baseDir, "server.py");
                    string info = Path.Combine(baseDir, "get_cellpose_info.py");

                    if (!File.Exists(server) || !File.Exists(info))
                    {
                        // Cellpose はあるが、アプリ側のスクリプトが欠けている
                        return false;
                    }

                    _pythonExe = exe;
                    _workingDir = Path.GetDirectoryName(exe)!;
                    _serverScript = server;
                    _infoScript = info;

                    return true;
                }
            }
            catch { }
        }

        return false;
    }

    // ---------------------------------------------------------
    // PATH 上の Python + CellPose を探す
    // ---------------------------------------------------------
    private bool TryFindExistingPython()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = "-c \"import cellpose\"",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var p = Process.Start(psi);
            if (p == null) return false;

            if (p.WaitForExit(10000) && p.ExitCode == 0)
            {
                // Python は OK → 次に server.py / version script の存在を確認
                string baseDir = AppContext.BaseDirectory;

                string server = Path.Combine(baseDir, "server.py");
                string info = Path.Combine(baseDir, "get_cellpose_info.py");

                if (!File.Exists(server) || !File.Exists(info))
                {
                    // Cellpose はあるが、アプリ側のスクリプトが欠けている
                    return false;
                }

                _pythonExe = "python";
                _workingDir = baseDir;
                _serverScript = server;
                _infoScript = info;

                return true;
            }
        }
        catch { }

        return false;
    }

    // ---------------------------------------------------------
    // 同梱 Python（特別版）
    // ---------------------------------------------------------
    private bool TryFindBundledPython()
    {
        string baseDir = AppContext.BaseDirectory;

        string distPython = Path.Combine(baseDir, "python", "python.exe");
        string distServer = Path.Combine(baseDir, "server.py");
        string distInfoScript = Path.Combine(baseDir, "get_cellpose_info.py");

        if (File.Exists(distPython) && File.Exists(distServer) && File.Exists(distInfoScript))
        {
            _pythonExe = distPython;
            _serverScript = distServer;
            _workingDir = baseDir;
            _infoScript = distInfoScript;
            return true;
        }

        return false;
    }

    // ---------------------------------------------------------
    // Cellpose バージョンおよび GPU 利用可能性の取得
    // ---------------------------------------------------------
    public (string? cellposeVersion, bool? gpuAvailable) GetCellposeInfo()
    {
        if (string.IsNullOrEmpty(_pythonExe) || string.IsNullOrEmpty(_infoScript))
            return (null, null);

        if (!File.Exists(_infoScript))
            return (null, null);

        var psi = new ProcessStartInfo
        {
            FileName = _pythonExe,
            Arguments = $"\"{_infoScript}\"",
            WorkingDirectory = _workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        try
        {
            using var p = Process.Start(psi);
            if (p == null)
                return (null, null);

            string output = p.StandardOutput.ReadToEnd().Trim();
            string error = p.StandardError.ReadToEnd().Trim();

            p.WaitForExit(3000);

            if (!string.IsNullOrEmpty(error))
                return (null, null);

            var json = System.Text.Json.JsonDocument.Parse(output);
            string? ver = json.RootElement.GetProperty("cellpose_version").GetString();
            bool gpu = json.RootElement.GetProperty("gpu_available").GetBoolean();
            //string? gpu_backend = json.RootElement.GetProperty("gpu_backend").GetString();

            return (ver, gpu);
        }
        catch
        {
            return (null, null);
        }
    }

    // ---------------------------------------------------------
    // 「安全な Kill」メソッド
    // ---------------------------------------------------------
    private static void SafeKill(Process? p)
    {
        if (p == null) return;

        try
        {
            if (!p.HasExited)
                p.Kill(entireProcessTree: true);
        }
        catch { }
    }

    // ---------------------------------------------------------
    // キャンセル要求
    // ---------------------------------------------------------
    public void RequestCancel()
    {
        SafeKill(_process);
    }

    // ---------------------------------------------------------
    // PythonServer を非同期化するために Task.Run でラップする
    // ---------------------------------------------------------
    public async Task<PythonServerResult> RunOnceAsync(string json, int timeoutSeconds, CancellationToken token)
    {
        using (token.Register(RequestCancel))
        {
            try
            {
                return await Task.Run(() => RunOnce(json, timeoutSeconds), token);
            }
            catch (OperationCanceledException)
            {
                return PythonServerResult.Error("キャンセルされました");
            }
        }
    }

    // ---------------------------------------------------------
    // Python を 1 回起動して JSON を渡し、結果を受け取る
    // ---------------------------------------------------------
    public PythonServerResult RunOnce(string json, int timeoutSeconds)
    {
        if (string.IsNullOrEmpty(_pythonExe))
            return PythonServerResult.Error("有効な Python 環境が設定されていません。");

        if (!File.Exists(_pythonExe))
            return PythonServerResult.Error($"python.exe が見つかりません: {_pythonExe}");

        if (string.IsNullOrEmpty(_serverScript) || !File.Exists(_serverScript))
            return PythonServerResult.Error($"server.py が見つかりません: {_serverScript}");

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

        _process = new Process { StartInfo = psi };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        _process.OutputDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                stdout.AppendLine(e.Data);
        };

        _process.ErrorDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                stderr.AppendLine(e.Data);
        };

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        using (var sw = _process.StandardInput)
        {
            sw.WriteLine(json);
        }

        // タイムアウト待ち
        bool exited = _process.WaitForExit(timeoutSeconds * 1000);

        if (!exited)
        {
            SafeKill(_process);
            return PythonServerResult.Error("Python process timeout");
        }

        string outStr = stdout.ToString().Trim();
        string errStr = stderr.ToString().Trim();

        if (!string.IsNullOrEmpty(errStr))
            return PythonServerResult.Error(errStr);

        return PythonServerResult.Success(outStr);
    }

    // ---------------------------------------------------------
    // PythonServer の戻り値（成功/失敗）
    // ---------------------------------------------------------
    public class PythonServerResult
    {
        public bool IsError { get; set; }
        public string ErrorMessage { get; set; } = "";
        public string Output { get; set; } = "";

        public static PythonServerResult Success(string output)
            => new() { IsError = false, Output = output };

        public static PythonServerResult Error(string msg)
            => new() { IsError = true, ErrorMessage = msg };
    }
}
