#include "ort_session.h"

#include <onnxruntime_cxx_api.h>

#if defined(_WIN32) && __has_include(<dml_provider_factory.h>)
#  include <dml_provider_factory.h>
#  define LIBR4_ORT_HAS_DML 1
#endif

#include <algorithm>
#include <array>
#include <cmath>
#include <cctype>
#include <cstring>
#include <sstream>
#include <unordered_map>

namespace libr4::ort {

namespace {

std::unique_ptr<Ort::Env>& GlobalEnv() {
    static std::unique_ptr<Ort::Env> env;
    return env;
}

std::string NormalizeEp(const std::string& ep) {
    std::string lower = ep;
    std::transform(lower.begin(), lower.end(), lower.begin(), [](unsigned char c) {
        return static_cast<char>(std::tolower(c));
    });
    return lower;
}

void AppendEp(Ort::SessionOptions& options, const std::string& ep_preference, std::string& error_out) {
    const auto ep = NormalizeEp(ep_preference);
    if (ep.empty() || ep == "cpu") {
        return;
    }

#if defined(LIBR4_ORT_HAS_DML)
    if (ep == "dml" || ep == "directml") {
        if (OrtSessionOptionsAppendExecutionProvider_DML(options, 0) != nullptr) {
            error_out = "DirectML EP registration failed";
        }
        return;
    }
#endif

    if (ep == "cuda" || ep == "gpu") {
        try {
            OrtCUDAProviderOptions cuda_options{};
            options.AppendExecutionProvider_CUDA(cuda_options);
            return;
        } catch (const Ort::Exception& ex) {
            error_out = std::string("CUDA EP unavailable: ") + ex.what();
            return;
        }
    }

    error_out = "Unknown execution provider preference: " + ep_preference;
}

void L2NormalizeRows(std::vector<float>& data, int batch, int hidden) {
    for (int b = 0; b < batch; ++b) {
        float norm = 0.0f;
        for (int h = 0; h < hidden; ++h) {
            const float v = data[b * hidden + h];
            norm += v * v;
        }
        norm = std::sqrt(norm);
        if (norm <= 1e-8f) {
            continue;
        }
        for (int h = 0; h < hidden; ++h) {
            data[b * hidden + h] /= norm;
        }
    }
}

}  // namespace

bool EnsureEnvironment(std::string& error_out) {
    try {
        if (!GlobalEnv()) {
            GlobalEnv() = std::make_unique<Ort::Env>(ORT_LOGGING_LEVEL_WARNING, "libr4_ort_ep");
        }
        return true;
    } catch (const Ort::Exception& ex) {
        error_out = ex.what();
        return false;
    }
}

std::vector<std::string> ListExecutionProviders() {
    std::vector<std::string> providers;
    try {
        providers = Ort::GetAvailableProviders();
    } catch (...) {
        providers.push_back("CPUExecutionProvider");
    }
    return providers;
}

OrtSessionHolder::OrtSessionHolder(void* session, void* env, void* allocator)
    : session_(session), env_(env), allocator_(allocator) {}

OrtSessionHolder::~OrtSessionHolder() {
    delete static_cast<Ort::Session*>(session_);
    delete static_cast<Ort::AllocatorWithDefaultOptions*>(allocator_);
}

std::unique_ptr<OrtSessionHolder> OrtSessionHolder::Create(
    const std::string& model_path,
    const std::string& ep_preference,
    std::string& error_out) {
    if (!EnsureEnvironment(error_out)) {
        return nullptr;
    }

    try {
        Ort::SessionOptions options;
        options.SetGraphOptimizationLevel(GraphOptimizationLevel::ORT_ENABLE_ALL);
        AppendEp(options, ep_preference, error_out);

        auto* env = GlobalEnv().get();
        auto* session = new Ort::Session(
            *env,
            model_path.c_str(),
            options);

        auto* allocator = new Ort::AllocatorWithDefaultOptions();
        return std::unique_ptr<OrtSessionHolder>(
            new OrtSessionHolder(session, env, allocator));
    } catch (const Ort::Exception& ex) {
        error_out = ex.what();
        return nullptr;
    }
}

bool OrtSessionHolder::BertEmbed(
    const int64_t* input_ids,
    const int64_t* attention_mask,
    const int64_t* token_type_ids,
    int batch,
    int seq_len,
    std::vector<float>& embeddings_out,
    int& hidden_dim_out,
    std::string& error_out) const {
    if (session_ == nullptr || batch <= 0 || seq_len <= 0) {
        error_out = "invalid_session_or_shape";
        return false;
    }

    try {
        auto* session = static_cast<Ort::Session*>(session_);
        auto* allocator = static_cast<Ort::AllocatorWithDefaultOptions*>(allocator_);

        const std::array<int64_t, 2> dims{ batch, seq_len };
        const size_t tensor_len = static_cast<size_t>(batch) * static_cast<size_t>(seq_len);

        Ort::MemoryInfo memory_info = Ort::MemoryInfo::CreateCpu(OrtArenaAllocator, OrtMemTypeDefault);

        Ort::Value ids_tensor = Ort::Value::CreateTensor<int64_t>(
            memory_info,
            const_cast<int64_t*>(input_ids),
            tensor_len,
            dims.data(),
            dims.size());

        Ort::Value mask_tensor = Ort::Value::CreateTensor<int64_t>(
            memory_info,
            const_cast<int64_t*>(attention_mask),
            tensor_len,
            dims.data(),
            dims.size());

        Ort::Value type_tensor = Ort::Value::CreateTensor<int64_t>(
            memory_info,
            const_cast<int64_t*>(token_type_ids),
            tensor_len,
            dims.data(),
            dims.size());

        std::vector<const char*> input_names;
        std::vector<Ort::Value> input_tensors;
        const auto input_name_storage = session->GetInputNames();

        struct NamedInput {
            std::string name;
            Ort::Value value;
        };
        std::vector<NamedInput> named_inputs;
        named_inputs.reserve(input_name_storage.size());

        for (const auto& name : input_name_storage) {
            const auto lower = NormalizeEp(name);
            if (lower.find("input_id") != std::string::npos) {
                named_inputs.push_back({ name, std::move(ids_tensor) });
            } else if (lower.find("attention") != std::string::npos) {
                named_inputs.push_back({ name, std::move(mask_tensor) });
            } else if (lower.find("token_type") != std::string::npos) {
                named_inputs.push_back({ name, std::move(type_tensor) });
            }
        }

        bool has_ids = false;
        bool has_mask = false;
        for (const auto& entry : named_inputs) {
            const auto lower = NormalizeEp(entry.name);
            has_ids = has_ids || lower.find("input_id") != std::string::npos;
            has_mask = has_mask || lower.find("attention") != std::string::npos;
        }
        if (!has_ids || !has_mask) {
            error_out = "model_missing_required_inputs";
            return false;
        }

        input_names.reserve(named_inputs.size());
        input_tensors.reserve(named_inputs.size());
        for (auto& entry : named_inputs) {
            input_names.push_back(entry.name.c_str());
            input_tensors.push_back(std::move(entry.value));
        }

        auto output_names = session->GetOutputNames();
        if (output_names.empty()) {
            error_out = "model_has_no_outputs";
            return false;
        }

        std::vector<const char*> output_name_ptrs;
        output_name_ptrs.reserve(output_names.size());
        for (const auto& name : output_names) {
            output_name_ptrs.push_back(name.c_str());
        }

        auto outputs = session->Run(
            Ort::RunOptions{ nullptr },
            input_names.data(),
            input_tensors.data(),
            input_tensors.size(),
            output_name_ptrs.data(),
            output_name_ptrs.size());

        if (outputs.empty() || !outputs[0].IsTensor()) {
            error_out = "unexpected_output";
            return false;
        }

        auto shape = outputs[0].GetTensorTypeAndShapeInfo().GetShape();
        if (shape.size() == 3) {
            const int64_t b = shape[0];
            const int64_t seq = shape[1];
            const int64_t hidden = shape[2];
            hidden_dim_out = static_cast<int>(hidden);
            embeddings_out.assign(static_cast<size_t>(b * hidden), 0.0f);

            const float* hidden_state = outputs[0].GetTensorData<float>();
            for (int64_t bi = 0; bi < b; ++bi) {
                float denom = 0.0f;
                for (int64_t si = 0; si < seq; ++si) {
                    if (attention_mask[bi * seq_len + si] == 0) {
                        continue;
                    }
                    denom += 1.0f;
                }
                if (denom <= 0.0f) {
                    denom = 1.0f;
                }

                for (int h = 0; h < hidden_dim_out; ++h) {
                    float sum = 0.0f;
                    for (int64_t si = 0; si < seq; ++si) {
                        if (attention_mask[bi * seq_len + si] == 0) {
                            continue;
                        }
                        sum += hidden_state[(bi * seq + si) * hidden + h];
                    }
                    embeddings_out[static_cast<size_t>(bi * hidden_dim_out + h)] = sum / denom;
                }
            }

            L2NormalizeRows(embeddings_out, batch, hidden_dim_out);
            return true;
        }

        if (shape.size() == 2) {
            hidden_dim_out = static_cast<int>(shape[1]);
            embeddings_out.assign(
                outputs[0].GetTensorData<float>(),
                outputs[0].GetTensorData<float>() + batch * hidden_dim_out);
            L2NormalizeRows(embeddings_out, batch, hidden_dim_out);
            return true;
        }

        error_out = "unsupported_output_rank";
        return false;
    } catch (const Ort::Exception& ex) {
        error_out = ex.what();
        return false;
    }
}

}  // namespace libr4::ort
