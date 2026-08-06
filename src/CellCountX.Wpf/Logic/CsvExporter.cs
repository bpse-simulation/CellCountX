using CellCountX.Wpf.Model;
using System.IO;
using System.Text;

namespace CellCountX.Wpf.Logic;

public class CsvExporter
{
    public string Save(List<CellResult> results, string outputFolder, string fileName = "cells.csv")
    {
        Directory.CreateDirectory(outputFolder);

        string mainCsv = Path.Combine(outputFolder, fileName);

        var lines = new List<string>
        {
            "FileName,CellCount,FilteredCellCount"
        };

        foreach (var r in results)
        {
            string file = Escape(r.FileName);
            string count = r.CellCount.ToString();
            string filteredCount = r.FilteredCellCount.ToString();
            lines.Add($"{file},{count},{filteredCount}");
        }

        try
        {
            // 通常保存
            File.WriteAllLines(mainCsv, lines, Encoding.UTF8);
            return mainCsv; // 正常保存
        }
        catch (IOException)
        {
            // Excel が開いている → 別名保存
            string fallback = Path.Combine(
                outputFolder,
                Path.GetFileNameWithoutExtension(fileName) + $"_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            );

            File.WriteAllLines(fallback, lines, Encoding.UTF8);

            return fallback; // 別名保存パスを返す
        }
    }

    private static string Escape(string s)
    {
        // カンマ・スペース・ダブルクォートを含む場合は囲む
        if (s.Contains(',') || s.Contains(' ') || s.Contains('"'))
        {
            string escaped = s.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }
        return s;
    }
}
