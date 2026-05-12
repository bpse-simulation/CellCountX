using System.IO;

namespace CellCountX.Wpf.Logic;

public static class PathValidator
{
    public static bool ContainsNonAscii(string path)
        => path.Any(c => c > 127);

    public static bool IsImageFile(string path)
    {
        string ext = Path.GetExtension(path).ToLower();
        return ext is ".png" or ".bmp" or ".tif" or ".tiff" or ".jpg" or ".jpeg";
    }
}
