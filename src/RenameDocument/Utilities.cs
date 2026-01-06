public static class Utilities
{
    public static string SanitizeFileName(string name)
    {
        foreach (var c in System.IO.Path.GetInvalidFileNameChars())
            name = name.Replace(c, '-');
        return name;
    }

    public static string MakeIncrementalPath(string path)
    {
        var dir = System.IO.Path.GetDirectoryName(path) ?? throw new InvalidOperationException("No directory");
        var baseName = System.IO.Path.GetFileNameWithoutExtension(path);
        var ext = System.IO.Path.GetExtension(path);
        var i = 1;
        string cand;
        do
        {
            cand = System.IO.Path.Combine(dir, $"{baseName} ({i}){ext}");
            i++;
        } while (System.IO.File.Exists(cand));
        return cand;
    }
}