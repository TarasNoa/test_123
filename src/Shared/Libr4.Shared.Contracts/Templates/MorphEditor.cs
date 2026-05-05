namespace Libr4.Shared.Contracts.Templates;

/// <summary>
/// Morph editor for token-efficient code editing.
/// Based on Fragments' Morph integration.
/// </summary>
public class MorphEditor
{
    /// <summary>
    /// Applies morph edits to existing code.
    /// </summary>
    /// <param name="originalCode">The original code.</param>
    /// <param name="morphEdit">The morph edit schema.</param>
    /// <returns>The edited code.</returns>
    public string ApplyEdit(string originalCode, MorphEditSchema morphEdit)
    {
        if (string.IsNullOrWhiteSpace(originalCode))
            return morphEdit.Edit;

        if (string.IsNullOrWhiteSpace(morphEdit.Edit))
            return originalCode;

        // Parse the edit to extract the changes
        var edits = ParseMorphEdit(morphEdit.Edit);
        
        // Apply each edit sequentially
        var result = originalCode;
        foreach (var edit in edits)
        {
            result = ApplySingleEdit(result, edit);
        }

        return result;
    }

    /// <summary>
    /// Parses a morph edit string into individual edits.
    /// </summary>
    private List<MorphEditSegment> ParseMorphEdit(string edit)
    {
        var segments = new List<MorphEditSegment>();
        var parts = edit.Split("// ... existing code ...", StringSplitOptions.None);

        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i].Trim();
            
            if (string.IsNullOrEmpty(part))
                continue;

            // Even indices are actual edits, odd indices are unchanged code markers
            if (i % 2 == 0)
            {
                segments.Add(new MorphEditSegment
                {
                    Type = MorphEditType.Edit,
                    Content = part
                });
            }
            else
            {
                segments.Add(new MorphEditSegment
                {
                    Type = MorphEditType.Unchanged,
                    Content = part
                });
            }
        }

        return segments;
    }

    /// <summary>
    /// Applies a single morph edit to the code.
    /// </summary>
    private string ApplySingleEdit(string code, MorphEditSegment edit)
    {
        if (edit.Type == MorphEditType.Unchanged)
            return code;

        if (edit.Type == MorphEditType.Edit)
        {
            // For simplicity, we'll just append the edit
            // In a real implementation, this would use context matching
            return code + "\n" + edit.Content;
        }

        return code;
    }

    /// <summary>
    /// Generates a morph edit from original and modified code.
    /// </summary>
    public MorphEditSchema GenerateEdit(
        string originalCode,
        string modifiedCode,
        string filePath,
        string instruction)
    {
        // Simple implementation - in production this would use diff algorithms
        var edit = $"// ... existing code ...\n{modifiedCode}\n// ... existing code ...";

        return new MorphEditSchema
        {
            Commentary = $"Applying edit: {instruction}",
            Instruction = instruction,
            Edit = edit,
            FilePath = filePath,
            IsValid = true
        };
    }

    /// <summary>
    /// Validates that a morph edit is properly formatted.
    /// </summary>
    public bool ValidateEditFormat(string edit)
    {
        if (string.IsNullOrWhiteSpace(edit))
            return false;

        // Check for the special marker
        if (!edit.Contains("// ... existing code ..."))
            return false;

        // Check that the edit is not just markers
        var parts = edit.Split("// ... existing code ...", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return false;

        return true;
    }

    /// <summary>
    /// Estimates token savings from using morph editing.
    /// </summary>
    public int EstimateTokenSavings(string originalCode, string morphEdit)
    {
        // Rough estimation: original code length - morph edit length
        var originalTokens = EstimateTokens(originalCode);
        var editTokens = EstimateTokens(morphEdit);
        
        return Math.Max(0, originalTokens - editTokens);
    }

    /// <summary>
    /// Rough token estimation (approximately 4 characters per token).
    /// </summary>
    private int EstimateTokens(string text)
    {
        return text.Length / 4;
    }
}

/// <summary>
/// Type of morph edit segment.
/// </summary>
internal enum MorphEditType
{
    Edit,
    Unchanged
}

/// <summary>
/// A segment of a morph edit.
/// </summary>
internal record MorphEditSegment
{
    public MorphEditType Type { get; init; }
    public string Content { get; init; } = string.Empty;
}

/// <summary>
/// Service for managing morph-based editing workflows.
/// </summary>
public class MorphEditingService
{
    private readonly MorphEditor _editor;

    public MorphEditingService()
    {
        _editor = new MorphEditor();
    }

    /// <summary>
    /// Applies a morph edit to code with validation.
    /// </summary>
    public (string EditedCode, bool Success, string? Error) ApplyMorphEdit(
        string originalCode,
        MorphEditSchema morphEdit)
    {
        var validator = new CodeFragmentSchemaValidator();
        var (isValid, errors) = validator.Validate(morphEdit);

        if (!isValid)
        {
            return (originalCode, false, string.Join(", ", errors));
        }

        if (!_editor.ValidateEditFormat(morphEdit.Edit))
        {
            return (originalCode, false, "Invalid morph edit format");
        }

        try
        {
            var editedCode = _editor.ApplyEdit(originalCode, morphEdit);
            var tokenSavings = _editor.EstimateTokenSavings(originalCode, morphEdit.Edit);

            return (editedCode, true, $"Applied edit. Estimated token savings: {tokenSavings}");
        }
        catch (Exception ex)
        {
            return (originalCode, false, $"Error applying edit: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates a morph edit instruction for the LLM.
    /// </summary>
    public string GenerateMorphInstruction(string filePath, string changeDescription)
    {
        return $@"
Edit the file {filePath} to: {changeDescription}

Use the following format for your edit:
// ... existing code ...
YOUR_EDIT_HERE
// ... existing code ...

Be concise and only include the specific lines that need to change.
Use '// ... existing code ...' markers to represent unchanged code.
";
    }
}
