using CellCountX.Wpf.Model;
using System.IO;
using System.Text;

namespace CellCountX.Wpf.Logic;

public class CsvExporter
{
    public void Save(List<CellResult> results, string outputFolder)
    {
        Directory.CreateDirectory(outputFolder);

        string csvPath = Path.Combine(outputFolder, "cells.csv");

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

        File.WriteAllLines(csvPath, lines, Encoding.UTF8);
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
