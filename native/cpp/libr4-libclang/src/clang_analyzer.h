#pragma once

#include <string>
#include <vector>

namespace libr4::clang {

struct RepoAnalysis {
    bool parse_ok = false;
    std::vector<std::string> includes;
    int function_count = 0;
    int lines_of_code = 0;
};

bool AnalyzeRepoSource(
    const std::string& path_hint,
    const std::string& source,
    RepoAnalysis& out,
    std::string& error_out);

std::string ToJson(const RepoAnalysis& analysis);

}  // namespace libr4::clang
