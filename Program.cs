using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

var EntriesToIgnore = new string[] { ".git", ".vs", "bin", "obj", "ext" };
var ExtensionsToIgnore = new string[] { ".sln" };

var BuildConfigurations = new string[] { "Debug|Any CPU", "Release|Any CPU" };

const string SolutionFolderGuid = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";
const string ProjectEntryGuid = "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}";

return Main(args);

int Main(string[] args)
{
    if (!(args.Length >= 1))
    {
        Console.Error.WriteLine("Please provide a the directory to generate the sln file for.");
        return 1;
    }

    var inputDirectory = Path.GetFullPath(args[0]);

    if (!Directory.Exists(inputDirectory))
    {
        Console.Error.WriteLine($"{args[0]} is not a directory.");
        return 1;
    }

    var inputDirectoryName = Path.GetFileName(inputDirectory.TrimEnd(Path.DirectorySeparatorChar));

    var outputPath = Path.Combine(inputDirectory, inputDirectoryName + ".sln");
    Console.WriteLine($"Writing {outputPath}...");

    var rootEntry = new SolutionEntry(inputDirectory, inputDirectoryName, SolutionEntryType.Directory, null!);

    CollectSolutionEntries(inputDirectory, rootEntry);

    WriteSolutionFile(outputPath, rootEntry);

    return 0;
}

void CollectSolutionEntries(string directory, SolutionEntry parent)
{
    var entryPaths = Directory.GetFileSystemEntries(directory);

    var projectEntryPaths = entryPaths.Where(x => x.EndsWith(".csproj"));

    if (projectEntryPaths.Any())
    {
        foreach (var projectEntryPath in projectEntryPaths)
        {
            var entryName = Path.GetFileName(projectEntryPath);
            parent.Parent.Children.Add(new SolutionEntry(projectEntryPath, entryName, SolutionEntryType.Project, parent.Parent));
        }

        parent.Parent.Children.Remove(parent);
        
        return;
    }

    foreach (var entryPath in entryPaths)
    {
        var entryName = Path.GetFileName(entryPath);

        if (EntriesToIgnore.Contains(entryName) || ExtensionsToIgnore.Contains(Path.GetExtension(entryPath)))
            continue;

        SolutionEntry? entry = null;

        if (File.GetAttributes(entryPath).HasFlag(FileAttributes.Directory))
        {
            entry = new (entryPath, entryName, SolutionEntryType.Directory, parent);
            parent.Children.Add(entry!);
            CollectSolutionEntries(entryPath, entry);
        }
        else
        {
            entry = new (entryPath, entryName, SolutionEntryType.File, parent);
            parent.Children.Add(entry!);
        }
    }
}

void PrintTree(SolutionEntry entry, string padding = "")
{
    string attribute = entry.Type switch 
    {
        SolutionEntryType.Directory => "D",
        SolutionEntryType.File => "F",
        SolutionEntryType.Project => "P",
        _ => "?"
    };

    Console.WriteLine($"{padding}{attribute} {entry.Name}");

    foreach (var child in entry.Children)
    {
        PrintTree(child, padding + "  ");
    }
}

void WriteSolutionFile(string outputPath, SolutionEntry rootEntry)
{
    var output = new StringBuilder(1024);

    output.AppendLine(
@"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 16
VisualStudioVersion = 16.0.30611.23
MinimumVisualStudioVersion = 10.0.40219.1");

    WriteSolutionEntry(output, rootEntry);
    WriteGlobalSection(output, rootEntry);

    File.WriteAllText(outputPath, output.ToString());
}

void WriteSolutionEntry(StringBuilder output, SolutionEntry entry)
{
    switch (entry.Type)
    {
        case SolutionEntryType.Directory:
            output.AppendLine($"Project(\"{SolutionFolderGuid}\") = \"{entry.Name}\", \"{entry.Name}\", \"{entry.Guid}\"");
            
            if (entry.Children.Any(x => x.Type == SolutionEntryType.File))
            {
                output.AppendLine("\tProjectSection(SolutionItems) = preProject");
                
                foreach (var child in entry.Children.Where(x => x.Type == SolutionEntryType.File))
                    output.AppendLine($"\t\t{child.RelativePath} = {child.RelativePath}");

                output.AppendLine("\tEndProjectSection");
            }
            
            output.AppendLine("EndProject");
            
            foreach (var child in entry.Children.Where(x => x.Type != SolutionEntryType.File))
                WriteSolutionEntry(output, child);

            break;

        case SolutionEntryType.Project:
            output.AppendLine($"Project(\"{ProjectEntryGuid}\") = \"{entry.Name}\", \"{entry.RelativePath}\", \"{entry.Guid}\"");
            output.AppendLine("EndProject");
            break;
    }
}

void WriteGlobalSection(StringBuilder output, SolutionEntry rootEntry)
{
    output.AppendLine("Global");

    output.AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");

    foreach (var buildConfiguration in BuildConfigurations)
        output.AppendLine($"\t\t{buildConfiguration} = {buildConfiguration}");

    output.AppendLine("\tEndGlobalSection");

    output.AppendLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");

    foreach (var project in rootEntry.GetProjects())
    {
        foreach (var buildConfiguration in BuildConfigurations)
        {
            output.AppendLine($"\t\t{project.Guid}.{buildConfiguration}.ActiveCfg = {buildConfiguration}");
            output.AppendLine($"\t\t{project.Guid}.{buildConfiguration}.Build.0 = {buildConfiguration}");
        }
    }

    output.AppendLine("\tEndGlobalSection");

    output.AppendLine("\tGlobalSection(SolutionProperties) = preSolution");
    output.AppendLine("\t\tHideSolutionNode = FALSE");
    output.AppendLine("\tEndGlobalSection");

    output.AppendLine("\tGlobalSection(NestedProjects) = preSolution");

    foreach (var entry in rootEntry.Children.Where(x => x.Type != SolutionEntryType.File))
        WriteNestedProject(output, entry);
    
    output.AppendLine("\tEndGlobalSection");

    output.AppendLine("\tGlobalSection(ExtensibilityGlobals) = postSolution");
    output.AppendLine($"\t\tSolutionGuid = {Utils.GetGuidFromString(rootEntry.Name + ".sln")}");
    output.AppendLine("\tEndGlobalSection");

    output.AppendLine("EndGlobal");
}

void WriteNestedProject(StringBuilder output, SolutionEntry entry)
{
    output.AppendLine($"\t\t{entry.Guid} = {entry.Parent.Guid}");

    foreach (var child in entry.Children.Where(x => x.Type != SolutionEntryType.File))
        WriteNestedProject(output, child);
}

public static class Utils
{
    public static string GetGuidFromString(string input)
    {
        using SHA1 hash = SHA1.Create();
        byte[] bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(input));
        var output = new Guid(bytes.Take(16).ToArray()).ToString().ToUpper();
        return $"{{{output}}}";
    }
}

enum SolutionEntryType
{
    Directory,

    File,

    Project
}

record SolutionEntry(string Path, string Name, SolutionEntryType Type, SolutionEntry Parent)
{
    public List<SolutionEntry> Children { get; } = new();

    private string? _guid = null;

    public string Guid
    {
        get
        {
            if (_guid == null)
                _guid = Utils.GetGuidFromString(RelativePath);                

            return _guid;
        }
    }

    public string RelativePath
    {
        get
        {
            SolutionEntry rootEntry = this;

            while (rootEntry.Parent != null)
                rootEntry = rootEntry.Parent;

            return Path.Substring(rootEntry.Path.Length);
        }
    }

    public IEnumerable<SolutionEntry> GetProjects()
    {
        if (Type == SolutionEntryType.Project)
            yield return this;

        foreach (var child in Children)
        {
            foreach (var project in child.GetProjects())
                yield return project;
        }
    }
}