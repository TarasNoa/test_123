#include <stdarg.h>
#include <stdbool.h>
#include <stdint.h>
#include <stdlib.h>

typedef struct PolyglotExecutor PolyglotExecutor;

struct PolyglotExecutor *executor_create(uint64_t timeout_ms,
                                         uintptr_t max_output_bytes,
                                         const char *project_root);

int executor_execute(struct PolyglotExecutor *executor,
                     const char *language,
                     const char *code,
                     char **out_stdout,
                     char **out_stderr,
                     int *out_exit_code,
                     bool *out_timed_out);

void executor_free_string(char *s);

void executor_destroy(struct PolyglotExecutor *executor);

int executor_run_shell(const char *project_root,
                       const char *command,
                       uint64_t timeout_ms,
                       uintptr_t max_output_bytes,
                       char **out_stdout,
                       char **out_stderr,
                       int *out_exit_code,
                       bool *out_timed_out);
