cmake_minimum_required(VERSION 3.20)

include(FetchContent)

set(FETCHCONTENT_QUIET OFF)

FetchContent_Declare(
  tree-sitter
  GIT_REPOSITORY https://github.com/tree-sitter/tree-sitter.git
  GIT_TAG v0.24.7
  GIT_SHALLOW TRUE
)

FetchContent_Declare(
  tree-sitter-python
  GIT_REPOSITORY https://github.com/tree-sitter/tree-sitter-python.git
  GIT_TAG v0.23.4
  GIT_SHALLOW TRUE
)

FetchContent_Declare(
  tree-sitter-javascript
  GIT_REPOSITORY https://github.com/tree-sitter/tree-sitter-javascript.git
  GIT_TAG v0.23.1
  GIT_SHALLOW TRUE
)

FetchContent_Declare(
  tree-sitter-c-sharp
  GIT_REPOSITORY https://github.com/tree-sitter/tree-sitter-c-sharp.git
  GIT_TAG v0.23.1
  GIT_SHALLOW TRUE
)

FetchContent_MakeAvailable(
  tree-sitter
  tree-sitter-python
  tree-sitter-javascript
  tree-sitter-c-sharp
)

add_library(tree-sitter-lib STATIC
  ${tree-sitter_SOURCE_DIR}/lib/src/lib.c
)
target_include_directories(tree-sitter-lib PUBLIC
  ${tree-sitter_SOURCE_DIR}/lib/include
  ${tree-sitter_SOURCE_DIR}/lib/src
)

function(libr4_add_grammar target_name src_dir)
  set(sources "${src_dir}/src/parser.c")
  if(EXISTS "${src_dir}/src/scanner.c")
    list(APPEND sources "${src_dir}/src/scanner.c")
  endif()
  add_library(${target_name} STATIC ${sources})
  target_include_directories(${target_name} PUBLIC "${src_dir}/src")
  target_link_libraries(${target_name} PUBLIC tree-sitter-lib)
endfunction()

libr4_add_grammar(tree-sitter-grammar-python "${tree-sitter-python_SOURCE_DIR}")
libr4_add_grammar(tree-sitter-grammar-javascript "${tree-sitter-javascript_SOURCE_DIR}")
libr4_add_grammar(tree-sitter-grammar-c-sharp "${tree-sitter-c-sharp_SOURCE_DIR}")

add_library(libr4_tree_sitter SHARED
  src/libr4_tree_sitter.cpp
  src/analyzer.cpp
  src/language_registry.cpp
)

target_include_directories(libr4_tree_sitter PUBLIC
  ${CMAKE_CURRENT_SOURCE_DIR}/include
)

target_link_libraries(libr4_tree_sitter PRIVATE
  tree-sitter-lib
  tree-sitter-grammar-python
  tree-sitter-grammar-javascript
  tree-sitter-grammar-c-sharp
)

if(WIN32)
  target_compile_definitions(libr4_tree_sitter PRIVATE LIBR4_TS_BUILD_DLL)
endif()

set_target_properties(libr4_tree_sitter PROPERTIES
  OUTPUT_NAME "libr4_tree_sitter"
  POSITION_INDEPENDENT_CODE ON
)

if(UNIX AND NOT APPLE)
  target_link_options(libr4_tree_sitter PRIVATE -Wl,--exclude-libs,ALL)
endif()
