#include "libr4_ort_ep.h"

#include "ort_session.h"

#include <cstring>
#include <new>
#include <sstream>
#include <string>
#include <unordered_map>

namespace {

char* duplicate_string(const std::string& value) {
    char* buffer = new (std::nothrow) char[value.size() + 1];
    if (buffer == nullptr) {
        return nullptr;
    }
    std::memcpy(buffer, value.c_str(), value.size() + 1);
    return buffer;
}

std::string providers_to_json(const std::vector<std::string>& providers) {
    std::ostringstream json;
    json << '[';
    for (std::size_t i = 0; i < providers.size(); ++i) {
        if (i > 0) json << ',';
        json << '"';
        for (char ch : providers[i]) {
            if (ch == '"') json << "\\\"";
            else json << ch;
        }
        json << '"';
    }
    json << ']';
    return json.str();
}

struct SessionEntry {
    std::unique_ptr<libr4::ort::OrtSessionHolder> holder;
};

std::unordered_map<Libr4OrtSession, SessionEntry>& SessionMap() {
    static std::unordered_map<Libr4OrtSession, SessionEntry> map;
    return map;
}

Libr4OrtSession NextSessionHandle() {
    static Libr4OrtSession next = reinterpret_cast<Libr4OrtSession>(1);
    next = reinterpret_cast<Libr4OrtSession>(
        reinterpret_cast<std::uintptr_t>(next) + 1);
    return next;
}

}  // namespace

extern "C" {

int libr4_ort_probe(void) {
    std::string error;
    return libr4::ort::EnsureEnvironment(error) ? 0 : -1;
}

int libr4_ort_list_providers_json(char** out_json) {
    if (out_json == nullptr) {
        return -1;
    }
    *out_json = nullptr;

    std::string error;
    if (!libr4::ort::EnsureEnvironment(error)) {
        return -2;
    }

    const auto json = providers_to_json(libr4::ort::ListExecutionProviders());
    *out_json = duplicate_string(json);
    return *out_json != nullptr ? 0 : -3;
}

int libr4_ort_session_create(
    const char* model_path,
    const char* ep_preference,
    Libr4OrtSession* out_session) {
    if (out_session == nullptr || model_path == nullptr) {
        return -1;
    }
    *out_session = nullptr;

    std::string error;
    const std::string ep = ep_preference != nullptr ? ep_preference : "";
    auto holder = libr4::ort::OrtSessionHolder::Create(model_path, ep, error);
    if (!holder) {
        return -2;
    }

    const auto handle = NextSessionHandle();
    SessionMap()[handle] = SessionEntry{ std::move(holder) };
    *out_session = handle;
    return 0;
}

void libr4_ort_session_destroy(Libr4OrtSession session) {
    SessionMap().erase(session);
}

int libr4_ort_bert_embed(
    Libr4OrtSession session,
    const int64_t* input_ids,
    const int64_t* attention_mask,
    const int64_t* token_type_ids,
    int batch,
    int seq_len,
    float** out_embeddings,
    int* out_hidden_dim) {
    if (out_embeddings == nullptr || out_hidden_dim == nullptr) {
        return -1;
    }
    *out_embeddings = nullptr;
    *out_hidden_dim = 0;

    const auto it = SessionMap().find(session);
    if (it == SessionMap().end() || it->second.holder == nullptr) {
        return -2;
    }

    std::vector<float> embeddings;
    int hidden = 0;
    std::string error;
    if (!it->second.holder->BertEmbed(
            input_ids,
            attention_mask,
            token_type_ids,
            batch,
            seq_len,
            embeddings,
            hidden,
            error)) {
        return -3;
    }

    auto* buffer = new (std::nothrow) float[embeddings.size()];
    if (buffer == nullptr) {
        return -4;
    }
    std::memcpy(buffer, embeddings.data(), embeddings.size() * sizeof(float));
    *out_embeddings = buffer;
    *out_hidden_dim = hidden;
    return 0;
}

void libr4_ort_free_string(char* ptr) {
    delete[] ptr;
}

void libr4_ort_free_floats(float* ptr) {
    delete[] ptr;
}

}  // extern "C"
