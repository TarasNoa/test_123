#pragma once

#include <stddef.h>

#ifdef _WIN32
#  ifdef LIBR4_CL_BUILD_DLL
#    define LIBR4_CL_API __declspec(dllexport)
#  else
#    define LIBR4_CL_API __declspec(dllimport)
#  endif
#else
#  define LIBR4_CL_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

/** Returns 0 when libclang is linked and usable. */
LIBR4_CL_API int libr4_cl_probe(void);

/**
 * Parse C/C++ source for #include dependencies and lightweight metrics.
 * path_hint: virtual filename (e.g. src/main.cpp) — drives language detection.
 * out_json: heap UTF-8 JSON; free with libr4_cl_free_string.
 */
LIBR4_CL_API int libr4_cl_parse_repo_json(
    const char* path_hint,
    const char* source_utf8,
    char** out_json);

LIBR4_CL_API void libr4_cl_free_string(char* ptr);

#ifdef __cplusplus
}
#endif
