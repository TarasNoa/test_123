cmake_minimum_required(VERSION 3.20)
project(libr4_cpp_native LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

if(NOT CMAKE_BUILD_TYPE AND NOT CMAKE_CONFIGURATION_TYPES)
  set(CMAKE_BUILD_TYPE Release CACHE STRING "Build type" FORCE)
endif()

add_subdirectory(libr4-tree-sitter)
add_subdirectory(libr4-ort-ep)

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

if(LIBR4_CLANG_INCLUDE_DIR AND LIBR4_CLANG_LIBRARY)
  add_subdirectory(libr4-libclang)
else()
  message(STATUS "libclang not found — skipping libr4-libclang (Wave 6.3)")
endif()
