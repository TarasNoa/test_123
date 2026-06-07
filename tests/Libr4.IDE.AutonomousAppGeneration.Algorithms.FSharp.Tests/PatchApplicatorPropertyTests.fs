module PatchApplicatorPropertyTests

open FsCheck
open FsCheck.Xunit
open Libr4.IDE.AutonomousAppGeneration.Algorithms.Patching.PatchApplicator
open Libr4.IDE.AutonomousAppGeneration.Algorithms.Patching.UnifiedDiffParser

let private sanitizeLine (s: string) =
    s.Replace("\r", "").Replace("\n", " ").Trim()

[<Property(MaxTest = 80)>]
let ``applyExact replaces unique single line`` (oldLine: NonEmptyString) (newLine: NonEmptyString) =
    let oldLine = sanitizeLine oldLine.Get
    let newLine = sanitizeLine newLine.Get

    if oldLine = newLine || oldLine.Contains("@@") || newLine.Contains("@@") then
        true
    else
        let original = oldLine + "\n"

        let patch =
            sprintf
                """--- a/f.txt
+++ b/f.txt
@@ -1,1 +1,1 @@
-%s
+%s
"""
                oldLine
                newLine

        let diff = parse patch (Some "f.txt")
        let result = applyExact original diff

        result.Success
        && result.PatchedContent.Value = newLine + "\n"

[<Property(MaxTest = 50)>]
let ``parse always returns non-negative hunk counts`` (body: string) =
    let patch =
        sprintf
            """--- a/x.txt
+++ b/x.txt
%s"""
            body

    let diff = parse patch (Some "x.txt")
    diff.Hunks |> Array.forall (fun h -> h.OldCount >= 0 && h.NewCount >= 0)
