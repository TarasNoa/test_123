#pragma once

#include <string>
#include <string_view>
#include <vector>

struct TSLanguage;

namespace libr4::ts {

struct PlaceholderFinding {
    int line = 0;
    std::string type;
    std::string message;
};

struct Metrics {
    int cyclomatic = 1;
    int nesting_depth = 0;
    int max_depth = 0;
    int function_count = 0;
    int lines_of_code = 0;
};

struct AnalysisResult {
    std::string language;
    bool parse_ok = false;
    std::string error;
    Metrics metrics;
    std::vector<PlaceholderFinding> placeholders;
};

AnalysisResult analyze_source(
    const TSLanguage* language,
    const std::string& language_id,
    std::string_view path_hint,
    std::string_view source);

std::string to_json(const AnalysisResult& result);

}  // namespace libr4::ts
