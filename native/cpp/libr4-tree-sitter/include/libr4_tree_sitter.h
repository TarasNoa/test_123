#pragma once

#include <stddef.h>

#ifdef _WIN32
#  ifdef LIBR4_TS_BUILD_DLL
#    define LIBR4_TS_API __declspec(dllexport)
#  else
#    define LIBR4_TS_API __declspec(dllimport)
#  endif
#else
#  define LIBR4_TS_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

/** Returns 0 when the library is loaded and grammars are registered. */
LIBR4_TS_API int libr4_ts_probe(void);

/**
 * Analyze source text. path_hint drives language detection (.py, .cs, .js, …).
 * language_override: optional ("python", "javascript", "c_sharp"); empty = detect from path.
 * out_json: heap-allocated UTF-8 JSON; free with libr4_ts_free_string.
 * Returns 0 on success, negative on hard failure.
 */
LIBR4_TS_API int libr4_ts_analyze_json(
    const char* path_hint,
    const char* source_utf8,
    const char* language_override,
    char** out_json);

LIBR4_TS_API void libr4_ts_free_string(char* ptr);

#ifdef __cplusplus
}
#endif
