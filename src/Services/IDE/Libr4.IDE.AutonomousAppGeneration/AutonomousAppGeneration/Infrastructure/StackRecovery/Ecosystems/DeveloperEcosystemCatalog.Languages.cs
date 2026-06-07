namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Ecosystems;

/// <summary>Top ~50 programming languages (TIOBE / GitHub / industry usage).</summary>
public static partial class DeveloperEcosystemCatalog
{
    private static IEnumerable<EcosystemProfile> BuildLanguageProfiles()
    {
        yield return EcosystemProfileFactory.Language("python", "Python", "python", "py");
        yield return EcosystemProfileFactory.Language("java", "Java", "java");
        yield return EcosystemProfileFactory.Language("javascript", "JavaScript", "javascript", "js", "ecmascript");
        yield return EcosystemProfileFactory.Language("typescript", "TypeScript", "typescript", "ts");
        yield return EcosystemProfileFactory.Language("csharp", "C#", "c#", "csharp", "dotnet", ".net");
        yield return EcosystemProfileFactory.Language("cpp", "C++", "c++", "cpp", "cplusplus");
        yield return EcosystemProfileFactory.Language("c", "C", " c ", "c language", "gcc", "clang");
        yield return EcosystemProfileFactory.Language("go", "Go", "golang", "go");
        yield return EcosystemProfileFactory.Language("rust", "Rust", "rust", "cargo");
        yield return EcosystemProfileFactory.Language("kotlin", "Kotlin", "kotlin", "kt");
        yield return EcosystemProfileFactory.Language("swift", "Swift", "swift", "ios");
        yield return EcosystemProfileFactory.Language("php", "PHP", "php", "laravel", "symfony");
        yield return EcosystemProfileFactory.Language("ruby", "Ruby", "ruby", "rails");
        yield return EcosystemProfileFactory.Language("dart", "Dart", "dart", "flutter");
        yield return EcosystemProfileFactory.Language("scala", "Scala", "scala", "sbt");
        yield return EcosystemProfileFactory.Language("r", "R", " r ", "rstats", "r language");
        yield return EcosystemProfileFactory.Language("matlab", "MATLAB", "matlab", "octave");
        yield return EcosystemProfileFactory.Language("shell", "Shell", "shell", "sh", "bash script");
        yield return EcosystemProfileFactory.Language("bash", "Bash", "bash", "sh");
        yield return EcosystemProfileFactory.Language("powershell", "PowerShell", "powershell", "pwsh");
        yield return EcosystemProfileFactory.Language("lua", "Lua", "lua");
        yield return EcosystemProfileFactory.Language("haskell", "Haskell", "haskell", "ghc");
        yield return EcosystemProfileFactory.Language("elixir", "Elixir", "elixir", "phoenix");
        yield return EcosystemProfileFactory.Language("clojure", "Clojure", "clojure", "cljs");
        yield return EcosystemProfileFactory.Language("fsharp", "F#", "f#", "fsharp");
        yield return EcosystemProfileFactory.Language("zig", "Zig", "zig");
        yield return EcosystemProfileFactory.Language("v", "V", " v lang", "vlang");
        yield return EcosystemProfileFactory.Language("perl", "Perl", "perl");
        yield return EcosystemProfileFactory.Language("groovy", "Groovy", "groovy", "gradle");
        yield return EcosystemProfileFactory.Language("objectivec", "Objective-C", "objective-c", "objc");
        yield return EcosystemProfileFactory.Language("delphi", "Delphi", "delphi", "pascal");
        yield return EcosystemProfileFactory.Language("ada", "Ada", "ada");
        yield return EcosystemProfileFactory.Language("lisp", "Lisp", "lisp", "common lisp");
        yield return EcosystemProfileFactory.Language("scheme", "Scheme", "scheme");
        yield return EcosystemProfileFactory.Language("racket", "Racket", "racket");
        yield return EcosystemProfileFactory.Language("erlang", "Erlang", "erlang", "otp");
        yield return EcosystemProfileFactory.Language("cobol", "COBOL", "cobol");
        yield return EcosystemProfileFactory.Language("fortran", "Fortran", "fortran");
        yield return EcosystemProfileFactory.Language("julia", "Julia", "julia");
        yield return EcosystemProfileFactory.Language("prolog", "Prolog", "prolog");
        yield return EcosystemProfileFactory.Language("ocaml", "OCaml", "ocaml", "opam");
        yield return EcosystemProfileFactory.Language("elm", "Elm", "elm");
        yield return EcosystemProfileFactory.Language("crystal", "Crystal", "crystal");
        yield return EcosystemProfileFactory.Language("nim", "Nim", "nim");
        yield return EcosystemProfileFactory.Language("solidity", "Solidity", "solidity", "smart contract");
        yield return EcosystemProfileFactory.Language("verilog", "Verilog", "verilog", "systemverilog");
        yield return EcosystemProfileFactory.Language("vhdl", "VHDL", "vhdl");
        yield return EcosystemProfileFactory.Language("tcl", "Tcl", "tcl");
        yield return EcosystemProfileFactory.Language("awk", "AWK", "awk", "gawk");
        yield return EcosystemProfileFactory.Language("sql", "SQL", "sql", "plsql", "tsql");
        yield return EcosystemProfileFactory.Language("graphql", "GraphQL", "graphql", "gql");
        yield return EcosystemProfileFactory.Language("wasm", "WebAssembly", "webassembly", "wasm", "wat");
        yield return EcosystemProfileFactory.Language("assembly", "Assembly", "assembly", "asm", "x86", "arm asm");
        yield return EcosystemProfileFactory.Language("vbnet", "VB.NET", "vb.net", "vbnet", "visual basic");
        yield return EcosystemProfileFactory.Language("apex", "Apex", "apex", "salesforce apex");
        yield return EcosystemProfileFactory.Language("abap", "ABAP", "abap", "sap abap");
        yield return EcosystemProfileFactory.Language("dlang", "D", " d language", "dlang", "d programming");
        yield return EcosystemProfileFactory.Language("reasonml", "ReasonML", "reasonml", "reason ml");
        yield return EcosystemProfileFactory.Language("rescript", "ReScript", "rescript", "rescript lang");
        yield return EcosystemProfileFactory.Language("coffeescript", "CoffeeScript", "coffeescript", "coffee script");
        yield return EcosystemProfileFactory.Language("wolfram", "Wolfram Language", "wolfram", "mathematica", "wolfram language");
    }
}
