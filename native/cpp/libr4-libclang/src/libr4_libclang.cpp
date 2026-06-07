#include "libr4_libclang.h"

#include "clang_analyzer.h"

#include <cstring>
#include <new>
#include <string>

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

int libr4_cl_probe(void) {
    CXIndex index = clang_createIndex(0, 0);
    if (index == nullptr) {
        return -1;
    }
    clang_disposeIndex(index);
    return 0;
}

int libr4_cl_parse_repo_json(
    const char* path_hint,
    const char* source_utf8,
    char** out_json) {
    if (out_json == nullptr || path_hint == nullptr || source_utf8 == nullptr) {
        return -1;
    }
    *out_json = nullptr;

    libr4::clang::RepoAnalysis analysis;
    std::string error;
    const std::string source = source_utf8;
    if (!libr4::clang::AnalyzeRepoSource(path_hint, source, analysis, error)) {
        return -2;
    }

    const auto json = libr4::clang::ToJson(analysis);
    *out_json = duplicate_string(json);
    return *out_json != nullptr ? 0 : -3;
}

void libr4_cl_free_string(char* ptr) {
    delete[] ptr;
}

}  // extern "C"
