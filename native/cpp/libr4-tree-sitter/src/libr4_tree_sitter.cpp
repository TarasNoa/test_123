#include "libr4_tree_sitter.h"

#include "analyzer.h"
#include "language_registry.h"

#include <cstring>
#include <new>
#include <string>
#include <string_view>

namespace {

char* duplicate_string(const std::string& value) {
    char* buffer = new (std::nothrow) char[value.size() + 1];
    if (buffer == nullptr) {
        return nullptr;
    }
    std::memcpy(buffer, value.c_str(), value.size() + 1);
    return buffer;
}

}  // namespace

extern "C" {

int libr4_ts_probe(void) {
    std::string language_id;
    return libr4::ts::detect_language("probe.py", language_id) != nullptr ? 0 : -1;
}

int libr4_ts_analyze_json(
    const char* path_hint,
    const char* source_utf8,
    const char* language_override,
    char** out_json) {
    if (out_json == nullptr) {
        return -1;
    }
    *out_json = nullptr;

    const std::string_view path = path_hint != nullptr ? std::string_view(path_hint) : std::string_view{};
    const std::string_view source = source_utf8 != nullptr ? std::string_view(source_utf8) : std::string_view{};
    const std::string_view override_lang = language_override != nullptr
        ? std::string_view(language_override)
        : std::string_view{};

    std::string language_id;
    const TSLanguage* language = nullptr;

    if (!override_lang.empty()) {
        language = libr4::ts::resolve_language(override_lang, language_id);
    } else {
        language = libr4::ts::detect_language(path, language_id);
    }

    if (language == nullptr) {
        language_id = override_lang.empty() ? "unknown" : std::string(override_lang);
    }

    const auto result = libr4::ts::analyze_source(language, language_id, path, source);
    const std::string json = libr4::ts::to_json(result);

    *out_json = duplicate_string(json);
    return *out_json != nullptr ? 0 : -2;
}

void libr4_ts_free_string(char* ptr) {
    delete[] ptr;
}

}  // extern "C"
