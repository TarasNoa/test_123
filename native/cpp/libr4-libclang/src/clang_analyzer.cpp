#include "clang_analyzer.h"

#include <clang-c/Index.h>

#include <algorithm>
#include <cctype>
#include <cstring>
#include <regex>
#include <sstream>

#if !defined(_WIN32)
#  include <strings.h>
#endif

namespace libr4::clang {

namespace {

bool EndsWithIgnoreCase(const std::string& value, const char* suffix) {
    const std::size_t suffix_len = std::strlen(suffix);
    if (value.size() < suffix_len) {
        return false;
    }
    const std::string tail = value.substr(value.size() - suffix_len);
#if defined(_WIN32)
    return _stricmp(tail.c_str(), suffix) == 0;
#else
    return strcasecmp(tail.c_str(), suffix) == 0;
#endif
}

bool IsCppPath(const std::string& path) {
    static const char* kExts[] = {
        ".c", ".cc", ".cpp", ".cxx", ".h", ".hh", ".hpp", ".hxx", ".m", ".mm"
    };
    for (const char* candidate : kExts) {
        if (EndsWithIgnoreCase(path, candidate)) {
            return true;
        }
    }
    return false;
}

std::string LanguageFlag(const std::string& path) {
    if (EndsWithIgnoreCase(path, ".c") || EndsWithIgnoreCase(path, ".h")) {
        return "-xc";
    }
    if (EndsWithIgnoreCase(path, ".m")) {
        return "-xobjective-c";
    }
    if (EndsWithIgnoreCase(path, ".mm")) {
        return "-xobjective-c++";
    }
    return "-xc++";
}

std::vector<std::string> RegexIncludes(const std::string& source) {
    static const std::regex include_re(
        R"((?:^|\n)\s*#\s*include\s+([<"])([^>"]+)[>"])",
        std::regex::ECMAScript);
    std::vector<std::string> includes;
    for (std::sregex_iterator it(source.begin(), source.end(), include_re), end; it != end; ++it) {
        includes.push_back((*it)[2].str());
    }
    return includes;
}

int CountLines(const std::string& source) {
    if (source.empty()) {
        return 0;
    }
    int lines = 1;
    for (char ch : source) {
        if (ch == '\n') {
            ++lines;
        }
    }
    return lines;
}

struct VisitorState {
    int function_count = 0;
};

CXChildVisitResult VisitCursor(CXCursor cursor, CXCursor /*parent*/, CXClientData client_data) {
    auto* state = static_cast<VisitorState*>(client_data);
    const auto kind = clang_getCursorKind(cursor);
    if (kind == CXCursor_FunctionDecl
        || kind == CXCursor_CXXMethod
        || kind == CXCursor_FunctionTemplate) {
        if (clang_Location_isFromMainFile(clang_getCursorLocation(cursor))) {
            ++state->function_count;
        }
    }
    return CXChildVisit_Recurse;
}

struct InclusionState {
    std::vector<std::string>* includes;
};

void InclusionVisitor(
    CXFile /*included_file*/,
    CXString* filename,
    CXSourceLocation* /* inclusion_stack */,
    unsigned /* include_len */,
    void* client_data) {
    if (filename == nullptr) {
        return;
    }
    auto* state = static_cast<InclusionState*>(client_data);
    const char* text = clang_getCString(*filename);
    if (text != nullptr && text[0] != '\0') {
        state->includes->push_back(text);
    }
}

}  // namespace

bool AnalyzeRepoSource(
    const std::string& path_hint,
    const std::string& source,
    RepoAnalysis& out,
    std::string& error_out) {
    out = RepoAnalysis{};
    out.lines_of_code = CountLines(source);

    if (!IsCppPath(path_hint)) {
        error_out = "unsupported_extension";
        return false;
    }

    out.includes = RegexIncludes(source);

    CXIndex index = clang_createIndex(0, 0);
    if (index == nullptr) {
        error_out = "clang_index_failed";
        out.parse_ok = !out.includes.empty();
        return out.parse_ok;
    }

    CXUnsavedFile unsaved{};
    unsaved.Filename = path_hint.c_str();
    unsaved.Contents = source.c_str();
    unsaved.Length = source.size();

    const std::string lang_flag = LanguageFlag(path_hint);
    const char* args[] = {
        lang_flag.c_str(),
        "-std=c++17",
        "-Wno-everything",
        "-ferror-limit=0"
    };

    CXTranslationUnit tu = clang_parseTranslationUnit(
        index,
        path_hint.c_str(),
        args,
        4,
        &unsaved,
        1,
        CXTranslationUnit_SingleFileParse | CXTranslationUnit_SkipFunctionBodies);

    if (tu != nullptr) {
        InclusionState inclusion_state{ &out.includes };
        clang_getInclusions(tu, InclusionVisitor, &inclusion_state);

        VisitorState visitor_state;
        const CXCursor root = clang_getTranslationUnitCursor(tu);
        clang_visitChildren(root, VisitCursor, &visitor_state);
        out.function_count = visitor_state.function_count;
        out.parse_ok = true;
        clang_disposeTranslationUnit(tu);
    } else {
        out.parse_ok = !out.includes.empty();
    }

    clang_disposeIndex(index);

    std::sort(out.includes.begin(), out.includes.end());
    out.includes.erase(
        std::unique(out.includes.begin(), out.includes.end()),
        out.includes.end());

    return true;
}

std::string ToJson(const RepoAnalysis& analysis) {
    std::ostringstream json;
    json << "{";
    json << "\"parse_ok\":" << (analysis.parse_ok ? "true" : "false") << ",";
    json << "\"function_count\":" << analysis.function_count << ",";
    json << "\"lines_of_code\":" << analysis.lines_of_code << ",";
    json << "\"includes\":[";
    for (std::size_t i = 0; i < analysis.includes.size(); ++i) {
        if (i > 0) {
            json << ',';
        }
        json << '"';
        for (char ch : analysis.includes[i]) {
            if (ch == '"') {
                json << "\\\"";
            } else if (ch == '\\') {
                json << "\\\\";
            } else {
                json << ch;
            }
        }
        json << '"';
    }
    json << "]}";
    return json.str();
}

}  // namespace libr4::clang
