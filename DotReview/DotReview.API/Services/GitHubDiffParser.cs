using System.Text;

namespace DotReview.API.Services;

public class GitHubDiffParser
{
    public string ParseCSharpDiff(string diff)
    {
        if (string.IsNullOrWhiteSpace(diff))
        {
            return string.Empty;
        }

        var lines = diff.Split(
            '\n',
            StringSplitOptions.None);

        var result = new StringBuilder();

        bool isCSharpFile = false;

        foreach (var line in lines)
        {
            // New file section
            if (line.StartsWith("diff --git"))
            {
                isCSharpFile =
                    line.Contains(".cs b/") ||
                    line.EndsWith(".cs");

                continue;
            }

            if (!isCSharpFile)
            {
                continue;
            }

            // Skip diff metadata
            if (line.StartsWith("index ") ||
                line.StartsWith("--- ") ||
                line.StartsWith("+++ ") ||
                line.StartsWith("@@"))
            {
                continue;
            }

            // Keep added/removed lines
            if (line.StartsWith("+") ||
                line.StartsWith("-"))
            {
                result.AppendLine(line);
            }
        }

        return result.ToString();
    }
}