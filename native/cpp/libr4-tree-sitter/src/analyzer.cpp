#include "analyzer.h"

#include <tree_sitter/api.h>

#include <algorithm>
#include <cctype>
#include <regex>
#include <sstream>
#include <string>
#include <string_view>
#include <vector>

namespace libr4::ts {

namespace {

std::string json_escape(std::string_view value) {
    std::ostringstream out;
    out << '"';
    for (char ch : value) {
        switch (ch) {
        case '\\': out << "\\\\"; break;
        case '"': out << "\\\""; break;
        case '\n': out << "\\n"; break;
        case '\r': out << "\\r"; break;
        case '\t': out << "\\t"; break;
        default:
            if (static_cast<unsigned char>(ch) < 0x20) {
                out << "\\u" << std::hex << static_cast<int>(static_cast<unsigned char>(ch));
            } else {
                out << ch;
            }
        }
    }
    out << '"';
    return out.str();
}

struct Metrics {
    int cyclomatic = 1;
    int nesting_depth = 0;
    int max_depth = 0;
    int function_count = 0;
    int lines_of_code = 0;
};

bool is_branching_type(std::string_view type) {
    static const char* kTypes[] = {
        "if_statement", "while_statement", "for_statement", "for_in_statement",
        "elif_clause", "except_clause", "catch_clause", "conditional_expression",
        "switch_statement", "case_statement", "do_statement", "else_clause",
        "ternary_expression", "&&", "||"
    };
    for (const char* t : kTypes) {
        if (type == t) return true;
    }
    return false;
}

bool is_function_type(std::string_view type) {
    static const char* kTypes[] = {
        "function_definition", "method_declaration", "constructor_declaration",
        "function_declaration", "method_definition", "arrow_function",
        "local_function", "function_expression"
    };
    for (const char* t : kTypes) {
        if (type == t) return true;
    }
    return false;
}

void walk_metrics(TSNode node, int depth, Metrics& metrics) {
    metrics.max_depth = std::max(metrics.max_depth, depth);

    const char* type = ts_node_type(node);
    if (type != nullptr) {
        const std::string_view type_view(type);
        if (is_branching_type(type_view)) {
            metrics.cyclomatic += 1;
        }
        if (is_function_type(type_view)) {
            metrics.function_count += 1;
        }
    }

    const uint32_t count = ts_node_named_child_count(node);
    for (uint32_t i = 0; i < count; ++i) {
        walk_metrics(ts_node_named_child(node, i), depth + 1, metrics);
    }
}

std::vector<PlaceholderFinding> scan_placeholders(std::string_view source) {
    static const std::regex pattern(
        R"((TODO|FIXME|HACK|XXX|PLACEHOLDER)\s*[:\-]?\s*(.*))",
        std::regex_constants::icase);

    std::vector<PlaceholderFinding> findings;
    std::size_t line = 1;
    std::size_t start = 0;

    while (start <= source.size()) {
        const auto end = source.find('\n', start);
        const auto line_view = source.substr(
            start,
            end == std::string_view::npos ? std::string_view::npos : end - start);

        std::smatch match;
        const std::string line_str(line_view);
        if (std::regex_search(line_str, match, pattern)) {
            PlaceholderFinding finding;
            finding.line = static_cast<int>(line);
            finding.type = match[1].str();
            finding.message = match[2].matched ? match[2].str() : std::string{};
            if (!finding.message.empty()) {
                findings.push_back(std::move(finding));
            } else {
                finding.message = line_str;
                findings.push_back(std::move(finding));
            }
        }

        if (end == std::string_view::npos) break;
        start = end + 1;
        line += 1;
    }

    return findings;
}

int count_lines(std::string_view source) {
    if (source.empty()) return 0;
    int lines = 1;
    for (char ch : source) {
        if (ch == '\n') lines += 1;
    }
    return lines;
}

std::string build_json(
    const std::string& language,
    bool parse_ok,
    const Metrics& metrics,
    const std::vector<PlaceholderFinding>& placeholders) {
    std::ostringstream json;
    json << '{'
         << "\"language\":" << json_escape(language) << ','
         << "\"parseOk\":" << (parse_ok ? "true" : "false") << ','
         << "\"complexity\":{"
         << "\"cyclomaticComplexity\":" << metrics.cyclomatic << ','
         << "\"nestingDepth\":" << metrics.max_depth << ','
         << "\"functionCount\":" << metrics.function_count << ','
         << "\"linesOfCode\":" << metrics.lines_of_code
         << "},"
         << "\"placeholders\":[";

    for (std::size_t i = 0; i < placeholders.size(); ++i) {
        const auto& p = placeholders[i];
        if (i > 0) json << ',';
        json << '{'
             << "\"line\":" << p.line << ','
             << "\"type\":" << json_escape(p.type) << ','
             << "\"message\":" << json_escape(p.message)
             << '}';
    }

    json << "]}";
    return json.str();
}

}  // namespace

AnalysisResult analyze_source(
    const TSLanguage* language,
    const std::string& language_id,
    std::string_view path_hint,
    std::string_view source) {
    AnalysisResult result;
    result.language = language_id;
    result.placeholders = scan_placeholders(source);
    result.metrics.lines_of_code = count_lines(source);

    if (language == nullptr) {
        result.parse_ok = false;
        result.error = "unsupported_language";
        return result;
    }

    TSParser* parser = ts_parser_new();
    ts_parser_set_language(parser, language);

    const std::string source_owned(source);
    TSTree* tree = ts_parser_parse_string(
        parser,
        nullptr,
        source_owned.c_str(),
        static_cast<uint32_t>(source_owned.size()));

    if (tree == nullptr) {
        ts_parser_delete(parser);
        result.parse_ok = false;
        result.error = "parse_failed";
        return result;
    }

    const TSNode root = ts_tree_root_node(tree);
    result.parse_ok = !ts_node_is_error(root) && ts_node_has_error(root) == false;

    walk_metrics(root, 0, result.metrics);
    result.metrics.nesting_depth = result.metrics.max_depth;

    ts_tree_delete(tree);
    ts_parser_delete(parser);
    (void)path_hint;
    return result;
}

std::string to_json(const AnalysisResult& result) {
    return build_json(
        result.language,
        result.parse_ok,
        result.metrics,
        result.placeholders);
}

}  // namespace libr4::ts
