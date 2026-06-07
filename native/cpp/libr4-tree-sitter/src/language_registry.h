#pragma once

#include <string>
#include <string_view>

struct TSLanguage;

namespace libr4::ts {

const TSLanguage* resolve_language(std::string_view language_id, std::string& normalized_out);
const TSLanguage* detect_language(std::string_view path_hint, std::string& language_id_out);

}  // namespace libr4::ts
