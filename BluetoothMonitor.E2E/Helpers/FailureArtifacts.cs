using System.Text;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;

namespace BluetoothMonitor.E2E.Helpers;

public static class FailureArtifacts
{
    public static string RootDirectory { get; } = ResolveArtifactsRoot();

    private static string ResolveArtifactsRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "BluetoothMonitor.slnx")))
            {
                return Path.Combine(dir.FullName, "TestResults", "e2e");
            }

            dir = dir.Parent;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "TestResults", "e2e"));
    }

    public static string Write(
        string runId,
        string testName,
        string message,
        AutomationElement? window,
        Exception? exception = null
    )
    {
        var safeName = Sanitize(testName);
        var dir = Path.Combine(RootDirectory, runId, safeName);
        _ = Directory.CreateDirectory(dir);

        var screenshotPath = Path.Combine(dir, "screenshot.png");
        var treePath = Path.Combine(dir, "uia-tree.txt");
        var failurePath = Path.Combine(dir, "failure.md");

        try
        {
            if (window is not null)
            {
                Capture.Element(window).ToFile(screenshotPath);
            }
            else
            {
                Capture.Screen().ToFile(screenshotPath);
            }
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(dir, "screenshot-error.txt"), ex.ToString());
        }

        try
        {
            File.WriteAllText(treePath, DumpTree(window), Encoding.UTF8);
        }
        catch (Exception ex)
        {
            File.WriteAllText(treePath, $"UIA dump failed: {ex}", Encoding.UTF8);
        }

        var sb = new StringBuilder();
        _ = sb.AppendLine("# E2E failure");
        _ = sb.AppendLine();
        _ = sb.AppendLine($"**Test:** `{testName}`");
        _ = sb.AppendLine($"**Run:** `{runId}`");
        _ = sb.AppendLine();
        _ = sb.AppendLine("## Message");
        _ = sb.AppendLine();
        _ = sb.AppendLine(message);
        if (exception is not null)
        {
            _ = sb.AppendLine();
            _ = sb.AppendLine("## Exception");
            _ = sb.AppendLine();
            _ = sb.AppendLine("```");
            _ = sb.AppendLine(exception.ToString());
            _ = sb.AppendLine("```");
        }

        _ = sb.AppendLine();
        _ = sb.AppendLine("## Artifacts");
        _ = sb.AppendLine();
        _ = sb.AppendLine($"- Screenshot: `{screenshotPath}`");
        _ = sb.AppendLine($"- UIA tree: `{treePath}`");
        _ = sb.AppendLine();
        _ = sb.AppendLine("## Repro");
        _ = sb.AppendLine();
        _ = sb.AppendLine("```bash");
        _ = sb.AppendLine("dotnet build BluetoothMonitor.slnx -c Debug -p:Platform=x64");
        _ = sb.AppendLine(
            $"dotnet test BluetoothMonitor.E2E -c Debug -p:Platform=x64 --filter FullyQualifiedName~{safeName}"
        );
        _ = sb.AppendLine("```");
        _ = sb.AppendLine();
        _ = sb.AppendLine(
            "Read `failure.md`, `screenshot.png`, and `uia-tree.txt` before editing product code."
        );

        File.WriteAllText(failurePath, sb.ToString(), Encoding.UTF8);
        return failurePath;
    }

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return name.Replace('.', '_');
    }

    private static string DumpTree(AutomationElement? root, int maxDepth = 8)
    {
        if (root is null)
        {
            return "(no window)";
        }

        var sb = new StringBuilder();
        Dump(root, 0, maxDepth, sb);
        return sb.ToString();
    }

    private static void Dump(AutomationElement element, int depth, int maxDepth, StringBuilder sb)
    {
        if (depth > maxDepth)
        {
            return;
        }

        string id;
        string name;
        string controlType;
        try
        {
            id = element.Properties.AutomationId.ValueOrDefault ?? "";
            name = element.Properties.Name.ValueOrDefault ?? "";
            controlType = element.Properties.ControlType.ValueOrDefault.ToString();
        }
        catch
        {
            return;
        }

        _ = sb.Append(' ', depth * 2);
        _ = sb.AppendLine($"[{controlType}] Id=\"{id}\" Name=\"{Truncate(name, 60)}\"");

        AutomationElement[] children;
        try
        {
            children = element.FindAllChildren();
        }
        catch
        {
            return;
        }

        foreach (var child in children.Take(80))
        {
            Dump(child, depth + 1, maxDepth, sb);
        }
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
