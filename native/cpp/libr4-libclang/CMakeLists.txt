cmake_minimum_required(VERSION 3.20)

find_path(LIBR4_CLANG_INCLUDE_DIR
  NAMES clang-c/Index.h
  PATHS
    /usr/include
    /usr/lib/llvm-*/include
    /usr/local/include
    "$ENV{LIBCLANG_PATH}/include"
    "C:/Program Files/LLVM/include"
)

find_library(LIBR4_CLANG_LIBRARY
  NAMES clang libclang
  PATHS
    /usr/lib
    /usr/lib/llvm-*/lib
    /usr/lib/x86_64-linux-gnu
    /usr/local/lib
    "$ENV{LIBCLANG_PATH}/lib"
    "C:/Program Files/LLVM/lib"
)

if(NOT LIBR4_CLANG_INCLUDE_DIR OR NOT LIBR4_CLANG_LIBRARY)
  message(FATAL_ERROR "libclang not found — install libclang-dev (Linux) or set LIBCLANG_PATH")
endif()

add_library(libr4_libclang SHARED
  src/libr4_libclang.cpp
  src/clang_analyzer.cpp
)

target_include_directories(libr4_libclang PUBLIC
  ${CMAKE_CURRENT_SOURCE_DIR}/include
  ${LIBR4_CLANG_INCLUDE_DIR}
)

target_link_libraries(libr4_libclang PRIVATE "${LIBR4_CLANG_LIBRARY}")

if(WIN32)
  target_compile_definitions(libr4_libclang PRIVATE LIBR4_CL_BUILD_DLL)
endif()

set_target_properties(libr4_libclang PROPERTIES
  OUTPUT_NAME "libr4_libclang"
  POSITION_INDEPENDENT_CODE ON
)

if(UNIX AND NOT APPLE)
  target_link_options(libr4_libclang PRIVATE -Wl,--exclude-libs,ALL)
endif()
