#pragma once

#include <stddef.h>
#include <stdint.h>

#ifdef _WIN32
#  ifdef LIBR4_ORT_BUILD_DLL
#    define LIBR4_ORT_API __declspec(dllexport)
#  else
#    define LIBR4_ORT_API __declspec(dllimport)
#  endif
#else
#  define LIBR4_ORT_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

typedef void* Libr4OrtSession;

/** Initialize ORT env; returns 0 on success. */
LIBR4_ORT_API int libr4_ort_probe(void);

/** JSON array of available execution providers, e.g. ["CPUExecutionProvider","CUDAExecutionProvider"]. */
LIBR4_ORT_API int libr4_ort_list_providers_json(char** out_json);

/**
 * Create session from ONNX model path.
 * ep_preference: "cpu", "dml", "cuda", or "" for default (cpu).
 */
LIBR4_ORT_API int libr4_ort_session_create(
    const char* model_path,
    const char* ep_preference,
    Libr4OrtSession* out_session);

LIBR4_ORT_API void libr4_ort_session_destroy(Libr4OrtSession session);

/**
 * BERT-style embedding run (input_ids, attention_mask, token_type_ids).
 * out_embeddings: batch * hidden_dim floats, L2-normalized per row; free with libr4_ort_free_floats.
 */
LIBR4_ORT_API int libr4_ort_bert_embed(
    Libr4OrtSession session,
    const int64_t* input_ids,
    const int64_t* attention_mask,
    const int64_t* token_type_ids,
    int batch,
    int seq_len,
    float** out_embeddings,
    int* out_hidden_dim);

LIBR4_ORT_API void libr4_ort_free_string(char* ptr);
LIBR4_ORT_API void libr4_ort_free_floats(float* ptr);

#ifdef __cplusplus
}
#endif
