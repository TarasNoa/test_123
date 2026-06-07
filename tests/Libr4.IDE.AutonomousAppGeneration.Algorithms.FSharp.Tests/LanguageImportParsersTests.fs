module LanguageImportParsersTests

open System.Collections.Generic
open Xunit
open Libr4.IDE.AutonomousAppGeneration.Algorithms.RepoGraph.LanguageImportParsers

[<Fact>]
let ``parseImports extracts python modules`` () =
    let content = "import os\nfrom utils.helpers import load\n"
    let imports = parseImports "app/main.py" (Some content)
    Assert.Contains("os", imports)
    Assert.Contains("utils/helpers", imports)

[<Fact>]
let ``parseImports extracts typescript relative imports`` () =
    let content = "import { x } from './components/Button';\n"
    let imports = parseImports "src/App.tsx" (Some content)
    Assert.Contains("components/Button", imports)

[<Fact>]
let ``parseImports extracts cpp includes`` () =
    let content = "#include \"utils/helper.h\"\n#include <vector>\n"
    let imports = parseImports "src/main.cpp" (Some content)
    Assert.Contains("utils/helper.h", imports)
    Assert.Contains("vector", imports)

[<Fact>]
let ``parseImports returns empty for unknown extension`` () =
    let imports = parseImports "readme.md" (Some "# title")
    Assert.Empty(imports)

[<Fact>]
let ``parseImports returns empty when content missing`` () =
    let imports = parseImports "src/App.ts" None
    Assert.Empty(imports)
