#pragma once

#include <memory>
#include <string>
#include <vector>

struct OrtSession;

namespace libr4::ort {

class OrtSessionHolder {
public:
    static std::unique_ptr<OrtSessionHolder> Create(
        const std::string& model_path,
        const std::string& ep_preference,
        std::string& error_out);

    ~OrtSessionHolder();

    bool BertEmbed(
        const int64_t* input_ids,
        const int64_t* attention_mask,
        const int64_t* token_type_ids,
        int batch,
        int seq_len,
        std::vector<float>& embeddings_out,
        int& hidden_dim_out,
        std::string& error_out) const;

private:
    explicit OrtSessionHolder(void* session, void* env, void* allocator);

    void* session_;
    void* env_;
    void* allocator_;
};

std::vector<std::string> ListExecutionProviders();
bool EnsureEnvironment(std::string& error_out);

}  // namespace libr4::ort
