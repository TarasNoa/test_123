#include "language_registry.h"

#include <algorithm>
#include <cctype>
#include <string>
#include <string_view>

extern "C" {
const TSLanguage* tree_sitter_python(void);
const TSLanguage* tree_sitter_javascript(void);
const TSLanguage* tree_sitter_c_sharp(void);
}

namespace libr4::ts {

namespace {

std::string to_lower(std::string value) {
    std::transform(value.begin(), value.end(), value.begin(), [](unsigned char ch) {
        return static_cast<char>(std::tolower(ch));
    });
    return value;
}

std::string extension_of(std::string_view path) {
    const auto slash = path.find_last_of("/\\");
    const auto dot = path.find_last_of('.');
    if (dot == std::string_view::npos || (slash != std::string_view::npos && dot < slash)) {
        return {};
    }
    return std::string(path.substr(dot + 1));
}

}  // namespace

const TSLanguage* resolve_language(std::string_view language_id, std::string& normalized_out) {
    normalized_out = to_lower(std::string(language_id));

    if (normalized_out == "python" || normalized_out == "py") {
        normalized_out = "python";
        return tree_sitter_python();
    }
    if (normalized_out == "javascript" || normalized_out == "js"
        || normalized_out == "jsx" || normalized_out == "typescript"
        || normalized_out == "ts" || normalized_out == "tsx") {
        normalized_out = "javascript";
        return tree_sitter_javascript();
    }
    if (normalized_out == "c_sharp" || normalized_out == "csharp" || normalized_out == "cs") {
        normalized_out = "c_sharp";
        return tree_sitter_c_sharp();
    }

    return nullptr;
}

const TSLanguage* detect_language(std::string_view path_hint, std::string& language_id_out) {
    const auto ext = to_lower(extension_of(path_hint));

    if (ext == "py") {
        language_id_out = "python";
        return tree_sitter_python();
    }
    if (ext == "js" || ext == "jsx" || ext == "ts" || ext == "tsx" || ext == "mjs" || ext == "cjs") {
        language_id_out = "javascript";
        return tree_sitter_javascript();
    }
    if (ext == "cs") {
        language_id_out = "c_sharp";
        return tree_sitter_c_sharp();
    }

    language_id_out.clear();
    return nullptr;
}

}  // namespace libr4::ts
