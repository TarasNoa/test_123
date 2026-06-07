module PatchApplicatorTests

open Xunit
open Libr4.IDE.AutonomousAppGeneration.Algorithms.Patching.PatchApplicator
open Libr4.IDE.AutonomousAppGeneration.Algorithms.Patching.UnifiedDiffParser

[<Fact>]
let ``applyExact replaces matching hunk`` () =
    let original = "line1\nline2\nline3\n"
    let patch =
        """--- a/file.txt
+++ b/file.txt
@@ -1,3 +1,3 @@
 line1
-line2
+line2-updated
 line3
"""
    let diff = parse patch (Some "file.txt")
    let result = applyExact original diff

    Assert.True(result.Success)
    Assert.Equal("line1\nline2-updated\nline3\n", result.PatchedContent.Value)

[<Fact>]
let ``applyExact fails when hunk missing`` () =
    let original = "alpha\nbeta\n"
    let patch =
        """--- a/x.txt
+++ b/x.txt
@@ -1,2 +1,2 @@
 missing
-old
+new
"""
    let diff = parse patch (Some "x.txt")
    let result = applyExact original diff

    Assert.False(result.Success)
    Assert.NotNull(result.ConflictReport)

[<Fact>]
let ``parse extracts target path from diff header`` () =
    let patch =
        """--- a/src/app.ts
+++ b/src/app.ts
@@ -1,1 +1,1 @@
-old
+new
"""
    let diff = parse patch None
    Assert.True(diff.TargetPath.Value.Contains("src/app.ts"))
    Assert.Equal(1, diff.Hunks.Length)
