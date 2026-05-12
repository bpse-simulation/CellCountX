using System.Diagnostics;
using System.IO;
using System.Text;

namespace CellCountX.Wpf.Model;

public class PythonServer
{
    private readonly string _pythonExe;
    private readonly string _serverScript;
    private readonly string _workingDir;

    private Process? _process;

    public PythonServer()
    {
        // ---------------------------------------------------------
        // 配布版を優先
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
        // 開発版 fallback
        // ---------------------------------------------------------
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

        throw new Exception("python.exe と server.py が見つかりません。");
    }

    // ---------------------------------------------------------
    // キャンセル要求
    // ---------------------------------------------------------
    public void RequestCancel()
    {
        try { _process?.Kill(); } catch { }
    }

    // ---------------------------------------------------------
    // PythonServer を非同期化するために Task.Run でラップする
    // ---------------------------------------------------------
    public Task<PythonServerResult> RunOnceAsync(string json, int timeoutSeconds)
    {
        return Task.Run(() => RunOnce(json, timeoutSeconds));
    }

    // ---------------------------------------------------------
    // Python を 1 回起動して JSON を渡し、結果を受け取る
    // ---------------------------------------------------------
    public PythonServerResult RunOnce(string json, int timeoutSeconds)
    {
        if (!File.Exists(_pythonExe))
            return PythonServerResult.Error($"python.exe が見つかりません: {_pythonExe}");

        if (!File.Exists(_serverScript))
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

        // タイムアウト待ち（UI 依存なし）
        bool exited = _process.WaitForExit(timeoutSeconds * 1000);

        if (!exited)
        {
            try { _process.Kill(); } catch { }
            return PythonServerResult.Error("Python process timeout");
        }

        string outStr = stdout.ToString().Trim();
        string errStr = stderr.ToString().Trim();

        if (!string.IsNullOrEmpty(errStr))
            return PythonServerResult.Error(errStr);

        return PythonServerResult.Success(outStr);
    }
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
        => new PythonServerResult { IsError = false, Output = output };

    public static PythonServerResult Error(string msg)
        => new PythonServerResult { IsError = true, ErrorMessage = msg };
}
