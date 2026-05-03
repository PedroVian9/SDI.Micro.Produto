namespace SDI.Back.API.Data;

public static class EnvFileLoader
{
    public static void LoadFromSolutionOrProjectRoot(string contentRootPath)
    {
        LoadIfExists(Path.Combine(contentRootPath, ".env"));

        var parentDirectory = Directory.GetParent(contentRootPath)?.FullName;
        if (!string.IsNullOrWhiteSpace(parentDirectory))
        {
            LoadIfExists(Path.Combine(parentDirectory, ".env"));
        }
    }

    private static void LoadIfExists(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim().Trim('"');

            if (!string.IsNullOrWhiteSpace(key) &&
                string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
