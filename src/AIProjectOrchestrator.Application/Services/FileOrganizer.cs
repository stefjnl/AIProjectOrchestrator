using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AIProjectOrchestrator.Domain.Models.Code;
using AIProjectOrchestrator.Domain.Services;

namespace AIProjectOrchestrator.Application.Services;

public class FileOrganizer : IFileOrganizer
{
    private readonly ILogger<FileOrganizer> _logger;

    public FileOrganizer(ILogger<FileOrganizer> logger)
    {
        _logger = logger;
    }

    public List<CodeArtifact> OrganizeGeneratedFiles(List<CodeArtifact> files)
    {
        foreach (var file in files)
        {
            // Organize by Clean Architecture structure matching the required pattern
            if (file.FileName.EndsWith("Controller.cs"))
                file.RelativePath = "Controllers/";
            else if (file.FileName.EndsWith("Service.cs") && !file.FileName.StartsWith("I"))
                file.RelativePath = "Services/";
            else if (file.FileName.StartsWith("I") && file.FileName.EndsWith("Service.cs"))
                file.RelativePath = "Services/Interfaces/";
            else if (file.FileName.EndsWith("Tests.cs"))
                file.RelativePath = "Tests/";
            else if (file.FileName.Contains("Model") || file.FileName.Contains("Request") || file.FileName.Contains("Response"))
                file.RelativePath = "Models/";
            else
                file.RelativePath = "Infrastructure/";

            // Set file type for filtering
            file.FileType = DetermineFileType(file.FileName);
        }

        return files;
    }

    public string DetermineFileType(string fileName)
    {
        if (fileName.EndsWith("Controller.cs"))
            return "Controller";
        if (fileName.EndsWith("Service.cs"))
            return "Service";
        if (fileName.EndsWith("Tests.cs"))
            return "Test";
        if (fileName.Contains("Model") || fileName.Contains("Request") || fileName.Contains("Response"))
            return "Model";
        return "Other";
    }

    public string SerializeCodeArtifacts(List<CodeArtifact> artifacts)
    {
        var sb = new StringBuilder();
        foreach (var artifact in artifacts)
        {
            sb.AppendLine($"# {artifact.FileName}");
            sb.AppendLine($"Type: {artifact.FileType}");
            sb.AppendLine($"Path: {artifact.RelativePath}");
            sb.AppendLine("```csharp");
            sb.AppendLine(artifact.Content);
            sb.AppendLine("```");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public string GenerateImplementationGuide(CodeGenerationResponse response)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Generated Code Implementation Guide");
        sb.AppendLine();
        sb.AppendLine("## Overview");
        sb.AppendLine("This package contains the generated code based on the user stories and requirements.");
        sb.AppendLine();
        sb.AppendLine("## Generated Files");
        foreach (var file in response.GeneratedFiles ?? new List<CodeArtifact>())
        {
            sb.AppendLine($"- {file.RelativePath}{file.FileName} ({file.FileType})");
        }
        sb.AppendLine();
        sb.AppendLine("## Test Files");
        foreach (var file in response.TestFiles ?? new List<CodeArtifact>())
        {
            sb.AppendLine($"- {file.RelativePath}{file.FileName} ({file.FileType})");
        }
        sb.AppendLine();
        sb.AppendLine("## Next Steps");
        sb.AppendLine("1. Review the generated code for correctness and completeness");
        sb.AppendLine("2. Run the tests to verify functionality");
        sb.AppendLine("3. Integrate the code into your project");
        sb.AppendLine("4. Make any necessary adjustments based on your specific requirements");
        return sb.ToString();
    }

    public async Task<byte[]?> GetGeneratedFilesZipAsync(
        Guid generationId,
        List<CodeArtifact> generatedFiles,
        List<CodeArtifact> testFiles,
        CancellationToken cancellationToken = default)
    {
        var allFiles = new List<CodeArtifact>();
        if (generatedFiles != null)
            allFiles.AddRange(generatedFiles);
        if (testFiles != null)
            allFiles.AddRange(testFiles);

        if (!allFiles.Any())
            return null;

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // Create the required folder structure
            foreach (var file in allFiles)
            {
                // Create folder structure matching the required pattern
                string entryPath;
                if (file.RelativePath.StartsWith("Controllers/"))
                {
                    entryPath = $"Generated Code Package/API/Controllers/{file.FileName}";
                }
                else if (file.RelativePath.StartsWith("Services/"))
                {
                    if (file.RelativePath.StartsWith("Services/Interfaces/"))
                    {
                        entryPath = $"Generated Code Package/Application/Interfaces/{file.FileName}";
                    }
                    else
                    {
                        entryPath = $"Generated Code Package/Application/Services/{file.FileName}";
                    }
                }
                else if (file.RelativePath.StartsWith("Models/"))
                {
                    entryPath = $"Generated Code Package/Domain/Models/{file.FileName}";
                }
                else if (file.RelativePath.StartsWith("Tests/"))
                {
                    entryPath = $"Generated Code Package/Tests/{file.FileName}";
                }
                else
                {
                    entryPath = $"Generated Code Package/Infrastructure/{file.FileName}";
                }

                var entry = archive.CreateEntry(entryPath);
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream);
                await writer.WriteAsync(file.Content);
            }

            // Add README with implementation guide
            var readmeEntry = archive.CreateEntry("Generated Code Package/README.md");
            using var readmeStream = readmeEntry.Open();
            using var readmeWriter = new StreamWriter(readmeStream);
            await readmeWriter.WriteAsync(GenerateImplementationGuide(new CodeGenerationResponse
            {
                GeneratedFiles = generatedFiles ?? new List<CodeArtifact>(),
                TestFiles = testFiles ?? new List<CodeArtifact>()
            })).ConfigureAwait(false);
        }

        return memoryStream.ToArray();
    }
}