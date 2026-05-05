# How to View Generated Application Code

## Overview

The autonomous app generation orchestrator generates complete applications with source code, configuration files, and build artifacts. This guide shows you how to retrieve and view the generated code.

## API Endpoints

### 1. Start Generation

**Endpoint:** `POST /api/ide/app-generation/start`

**Request:**
```bash
curl -X POST http://localhost:5199/api/ide/app-generation/start \
  -H "Content-Type: application/json" \
  -d '{
    "userRequest": "сгенерируй приложение мобильного банкинга"
  }'
```

**Response:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Planning",
  "applicationName": "Mobile Banking App",
  "iterations": 0,
  "maxIterations": 5,
  "succeeded": false,
  "failureReason": null
}
```

**Extract the `id`** - This is your generation ID that you'll use to retrieve the full report.

### 2. Retrieve Full Report with Generated Code

**Endpoint:** `GET /api/ide/app-generation/{id}`

**Request:**
```bash
curl http://localhost:5199/api/ide/app-generation/550e8400-e29b-41d4-a716-446655440000
```

**Response Structure:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Completed",
  "failureReason": null,
  "plan": {
    "applicationName": "Mobile Banking App",
    "applicationDescription": "A secure mobile banking application...",
    "techStack": {
      "languages": ["Python", "TypeScript"],
      "frameworks": ["FastAPI", "React Native"],
      "databases": ["PostgreSQL"],
      "infrastructure": ["Docker", "Kubernetes"],
      "rationale": "..."
    },
    "phases": [...],
    "requiredAgents": [...],
    "runtimeImage": "python:3.12-slim",
    "buildCommands": ["pip install -r requirements.txt"],
    "testCommands": ["pytest tests/"],
    "maxIterations": 5
  },
  "iterations": [
    {
      "id": "...",
      "number": 1,
      "succeeded": true,
      "errorCount": 0,
      "appliedFixes": [],
      "startedAt": "2026-04-20T12:33:00Z",
      "completedAt": "2026-04-20T12:35:00Z"
    }
  ],
  "files": [
    {
      "relativePath": "main.py",
      "language": "python",
      "content": "#!/usr/bin/env python3\n...",
      "updatedAt": "2026-04-20T12:35:00Z"
    },
    {
      "relativePath": "requirements.txt",
      "language": "text",
      "content": "fastapi==0.104.1\npydantic==2.5.0\n...",
      "updatedAt": "2026-04-20T12:35:00Z"
    },
    {
      "relativePath": "tests/test_main.py",
      "language": "python",
      "content": "import pytest\n...",
      "updatedAt": "2026-04-20T12:35:00Z"
    }
  ],
  "outstandingErrors": [],
  "startedAt": "2026-04-20T12:33:00Z",
  "completedAt": "2026-04-20T12:35:00Z"
}
```

## Accessing Generated Files

### Via cURL

```bash
# Get the full report
curl http://localhost:5199/api/ide/app-generation/550e8400-e29b-41d4-a716-446655440000 | jq '.files'

# Extract a specific file
curl http://localhost:5199/api/ide/app-generation/550e8400-e29b-41d4-a716-446655440000 | \
  jq '.files[] | select(.relativePath == "main.py") | .content'
```

### Via PowerShell

```powershell
# Get the full report
$report = Invoke-RestMethod -Uri "http://localhost:5199/api/ide/app-generation/550e8400-e29b-41d4-a716-446655440000"

# List all generated files
$report.files | ForEach-Object { Write-Host "$($_.relativePath) ($($_.language))" }

# View a specific file
$mainFile = $report.files | Where-Object { $_.relativePath -eq "main.py" }
Write-Host $mainFile.content
```

### Via C# / .NET

```csharp
using System.Net.Http.Json;
using System.Text.Json;

var client = new HttpClient();
var response = await client.GetAsync("http://localhost:5199/api/ide/app-generation/550e8400-e29b-41d4-a716-446655440000");
var json = await response.Content.ReadAsStringAsync();

var report = JsonSerializer.Deserialize<AppGenerationReport>(json, 
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

// List all files
foreach (var file in report.Files)
{
    Console.WriteLine($"{file.RelativePath} ({file.Language})");
    Console.WriteLine($"Size: {file.Content.Length} bytes");
    Console.WriteLine($"Updated: {file.UpdatedAt}");
    Console.WriteLine(file.Content);
    Console.WriteLine("---");
}
```

### Via Python

```python
import requests
import json

response = requests.get("http://localhost:5199/api/ide/app-generation/550e8400-e29b-41d4-a716-446655440000")
report = response.json()

# List all files
for file in report['files']:
    print(f"{file['relativePath']} ({file['language']})")
    print(f"Size: {len(file['content'])} bytes")
    print(f"Updated: {file['updatedAt']}")
    print(file['content'])
    print("---")

# Save files to disk
import os
for file in report['files']:
    os.makedirs(os.path.dirname(file['relativePath']), exist_ok=True)
    with open(file['relativePath'], 'w') as f:
        f.write(file['content'])
```

## Understanding the Response

### Plan Section
Contains the generation plan with:
- **applicationName**: Name of the generated application
- **techStack**: Languages, frameworks, databases used
- **runtimeImage**: Docker image for isolated execution
- **buildCommands**: Commands to build the application
- **testCommands**: Commands to test the application
- **phases**: Orchestration phases with agent assignments

### Files Section
Array of generated source files, each containing:
- **relativePath**: File path relative to project root
- **language**: Programming language (python, javascript, typescript, etc.)
- **content**: Full file content as text
- **updatedAt**: Last modification timestamp

### Iterations Section
Shows the generation iterations:
- **number**: Iteration number (1, 2, 3, etc.)
- **succeeded**: Whether the iteration succeeded
- **errorCount**: Number of errors encountered
- **appliedFixes**: List of fixes applied in this iteration
- **startedAt/completedAt**: Timing information

## Example: Saving Generated Code to Disk

### PowerShell Script

```powershell
param(
    [string]$GenerationId,
    [string]$OutputPath = "./generated-app"
)

$baseUrl = "http://localhost:5199"
$report = Invoke-RestMethod -Uri "$baseUrl/api/ide/app-generation/$GenerationId"

Write-Host "Generating: $($report.plan.applicationName)"
Write-Host "Status: $($report.status)"
Write-Host "Tech Stack: $($report.plan.techStack.languages -join ', ')"
Write-Host "Files: $($report.files.Count)"

# Create output directory
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

# Save each file
foreach ($file in $report.files) {
    $filePath = Join-Path $OutputPath $file.relativePath
    $fileDir = Split-Path $filePath
    
    New-Item -ItemType Directory -Path $fileDir -Force | Out-Null
    Set-Content -Path $filePath -Value $file.content
    
    Write-Host "✓ $($file.relativePath)"
}

Write-Host "`nGenerated files saved to: $OutputPath"
```

### Bash Script

```bash
#!/bin/bash

GENERATION_ID=$1
OUTPUT_PATH=${2:-.generated-app}
BASE_URL="http://localhost:5199"

if [ -z "$GENERATION_ID" ]; then
    echo "Usage: $0 <generation-id> [output-path]"
    exit 1
fi

# Get the report
REPORT=$(curl -s "$BASE_URL/api/ide/app-generation/$GENERATION_ID")

# Extract info
APP_NAME=$(echo "$REPORT" | jq -r '.plan.applicationName')
STATUS=$(echo "$REPORT" | jq -r '.status')
TECH_STACK=$(echo "$REPORT" | jq -r '.plan.techStack.languages | join(", ")')
FILE_COUNT=$(echo "$REPORT" | jq '.files | length')

echo "Generating: $APP_NAME"
echo "Status: $STATUS"
echo "Tech Stack: $TECH_STACK"
echo "Files: $FILE_COUNT"

# Create output directory
mkdir -p "$OUTPUT_PATH"

# Save each file
echo "$REPORT" | jq -r '.files[] | @base64' | while read file_b64; do
    FILE=$(echo "$file_b64" | base64 -d)
    REL_PATH=$(echo "$FILE" | jq -r '.relativePath')
    CONTENT=$(echo "$FILE" | jq -r '.content')
    
    FILE_PATH="$OUTPUT_PATH/$REL_PATH"
    FILE_DIR=$(dirname "$FILE_PATH")
    
    mkdir -p "$FILE_DIR"
    echo "$CONTENT" > "$FILE_PATH"
    
    echo "✓ $REL_PATH"
done

echo ""
echo "Generated files saved to: $OUTPUT_PATH"
```

## Viewing Generated Code in Real-Time

The generated files are stored in the orchestrator's in-memory repository. To view them:

1. **Start generation** with a POST request
2. **Poll the report endpoint** to check status
3. **When status is "Completed"**, retrieve all files

### Status Values
- `Planning` - LLM is creating the plan
- `Generating` - Code is being generated
- `Executing` - Build/test commands running in isolated runtime
- `Completed` - Generation succeeded
- `Failed` - Generation failed (check `failureReason`)

## Integration with IDE

The generated files can be:
1. **Displayed in the IDE** - Show file tree and content
2. **Saved to disk** - Export to local filesystem
3. **Opened in editor** - Load into code editor
4. **Compared with original** - Diff view for changes
5. **Executed** - Run build/test commands

## Example: Full Workflow

```csharp
// 1. Start generation
var startResponse = await client.PostAsJsonAsync(
    "http://localhost:5199/api/ide/app-generation/start",
    new { userRequest = "сгенерируй приложение мобильного банкинга" });

var startData = await startResponse.Content.ReadAsAsync<dynamic>();
var generationId = startData.id;

// 2. Poll for completion
var report = null;
while (true) {
    var reportResponse = await client.GetAsync(
        $"http://localhost:5199/api/ide/app-generation/{generationId}");
    
    report = await reportResponse.Content.ReadAsAsync<AppGenerationReport>();
    
    if (report.Status == "Completed" || report.Status == "Failed") {
        break;
    }
    
    await Task.Delay(1000); // Wait 1 second before polling again
}

// 3. Display results
Console.WriteLine($"Status: {report.Status}");
Console.WriteLine($"App: {report.Plan.ApplicationName}");
Console.WriteLine($"Files: {report.Files.Count}");

// 4. Save files
foreach (var file in report.Files) {
    var path = Path.Combine("output", file.RelativePath);
    Directory.CreateDirectory(Path.GetDirectoryName(path));
    File.WriteAllText(path, file.Content);
}
```

## Troubleshooting

### No files generated
- Check if status is still "Planning" or "Generating"
- Wait for status to become "Completed"
- Check `failureReason` if status is "Failed"

### Empty file content
- Verify the file was actually generated (check `files` array)
- Some files might be binary (not shown in response)
- Check file size in the response

### Generation failed
- Check `outstandingErrors` array for error details
- Review `failureReason` field
- Check iteration details for what went wrong

## See Also

- `TEST_RESULTS.md` - Integration test results
- `docs/IDE/IsolatedRuntimeArchitecture.md` - Architecture documentation
- `/api/ide/app-generation/start` - Start generation endpoint
- `/api/ide/app-generation/{id}` - Get report endpoint
