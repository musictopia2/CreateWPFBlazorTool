string? visualStudioPath = FindVisualStudio();
if (visualStudioPath is null)
{
    Console.WriteLine("Visual Studio not found. Please install Visual Studio.");
    return;
}
ITemplateCreater create = new CreateSimpleTemplateClass();
CreateSimpleTemplateClass.TemplateName = "SimpleWPFBlazorApp";
if (IsTemplateInstalled(CreateSimpleTemplateClass.TemplateName) == false)
{
    if (await InstallTemplateIfMissingAsync("Musictopia.SimpleWPFBlazorApp", CreateSimpleTemplateClass.TemplateName) == false)
    {
        return; //if this failed, don't continue.
    }
}
string? name;
name = args.FirstOrDefault();
while (string.IsNullOrWhiteSpace(name))
{
    Console.WriteLine("Please provide a name for the project:");
    name = Console.ReadLine();
}
await create.CreateTemplateAsync(name, "");
// Get the current working directory
string currentDirectory = Directory.GetCurrentDirectory();
// Path to the project file you want to open
string projectFilePath = Path.Combine(currentDirectory, name, $"{name}.csproj");

// Check if the project file exists
if (!File.Exists(projectFilePath))
{
    Console.WriteLine($"Path {projectFilePath} does not exist");
    Console.ReadLine();
    return;
}
string solutionFilePath = Path.Combine(currentDirectory, name, $"{name}.slnx");
string solutionContent = $"""
    <Solution>
      <Project Path="{name}.csproj" />
    </Solution>
    """;
File.WriteAllText(solutionFilePath, solutionContent);
// Start Visual Studio with the specified solution file
ProcessStartInfo startInfo = new()
{
    FileName = visualStudioPath,
    Arguments = solutionFilePath,
    WorkingDirectory = Path.GetDirectoryName(solutionFilePath) // Set the working directory
};
Process.Start(startInfo);
string? FindVisualStudio()
{
    string programFilesx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
    string vswherePath = Path.Combine(programFilesx86, @"Microsoft Visual Studio\Installer\vswhere.exe");

    if (!File.Exists(vswherePath))
    {
        return null;
    }

    ProcessStartInfo psi = new()
    {
        FileName = vswherePath,
        Arguments = "-latest -requires Microsoft.Component.MSBuild -products * -property installationPath",
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var p = Process.Start(psi);
    string installPath = p!.StandardOutput.ReadToEnd().Trim();
    p.WaitForExit();

    if (string.IsNullOrWhiteSpace(installPath))
    {
        return null;
    }

    string devenv = Path.Combine(installPath, @"Common7\IDE\devenv.exe");

    return File.Exists(devenv) ? devenv : null;
}
bool IsTemplateInstalled(string templateShortName)
{
    ProcessStartInfo psi = new()
    {
        FileName = "dotnet",
        Arguments = "new --list",
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };
    using var p = Process.Start(psi);
    string output = p!.StandardOutput.ReadToEnd();
    p.WaitForExit();
    return output
        .Split('\n')
        .Any(line => line.Contains(templateShortName, StringComparison.OrdinalIgnoreCase));
}
async Task<bool> InstallTemplateIfMissingAsync(string templatePackage, string templateShortName)
{
    if (IsTemplateInstalled(templateShortName))
    {
        return true;
    }
    Console.WriteLine($"Template '{templateShortName}' not installed. Installing...");
    ProcessStartInfo psi = new()
    {
        FileName = "dotnet",
        Arguments = $"new install {templatePackage}",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };
    using var p = Process.Start(psi);
    string output = await p!.StandardOutput.ReadToEndAsync();
    string error = await p.StandardError.ReadToEndAsync();
    p.WaitForExit();

    Console.WriteLine(output);

    if (p.ExitCode != 0)
    {
        Console.WriteLine("Template installation failed.");
        Console.WriteLine(error);
        return false;
    }

    Console.WriteLine("Template installation completed.");
    return true;
}