cmake_minimum_required(VERSION 3.20)

set(LIBR4_ORT_VERSION "1.20.1")
set(LIBR4_ORT_ROOT "${CMAKE_BINARY_DIR}/onnxruntime-${LIBR4_ORT_VERSION}")

if(WIN32)
  set(LIBR4_ORT_ARCHIVE "onnxruntime-win-x64-${LIBR4_ORT_VERSION}.zip")
else()
  set(LIBR4_ORT_ARCHIVE "onnxruntime-linux-x64-${LIBR4_ORT_VERSION}.tgz")
endif()

set(LIBR4_ORT_URL "https://github.com/microsoft/onnxruntime/releases/download/v${LIBR4_ORT_VERSION}/${LIBR4_ORT_ARCHIVE}")
set(LIBR4_ORT_ARCHIVE_PATH "${CMAKE_BINARY_DIR}/${LIBR4_ORT_ARCHIVE}")

if(NOT EXISTS "${LIBR4_ORT_ROOT}/include/onnxruntime_cxx_api.h")
  message(STATUS "Downloading ONNX Runtime ${LIBR4_ORT_VERSION} from ${LIBR4_ORT_URL}")
  file(DOWNLOAD "${LIBR4_ORT_URL}" "${LIBR4_ORT_ARCHIVE_PATH}" SHOW_PROGRESS TLS_VERIFY ON)

  if(WIN32)
    file(ARCHIVE_EXTRACT INPUT "${LIBR4_ORT_ARCHIVE_PATH}" DESTINATION "${CMAKE_BINARY_DIR}")
  else()
    execute_process(
      COMMAND ${CMAKE_COMMAND} -E tar xzf "${LIBR4_ORT_ARCHIVE_PATH}"
      WORKING_DIRECTORY "${CMAKE_BINARY_DIR}"
      RESULT_VARIABLE _ort_extract_rc)
    if(NOT _ort_extract_rc EQUAL 0)
      message(FATAL_ERROR "Failed to extract ONNX Runtime archive")
    endif()
  endif()
endif()

if(WIN32)
  set(LIBR4_ORT_LIB "${LIBR4_ORT_ROOT}/lib/onnxruntime.lib")
  set(LIBR4_ORT_DLL "${LIBR4_ORT_ROOT}/lib/onnxruntime.dll")
else()
  set(LIBR4_ORT_LIB "${LIBR4_ORT_ROOT}/lib/libonnxruntime.so")
  set(LIBR4_ORT_DLL "${LIBR4_ORT_ROOT}/lib/libonnxruntime.so")
endif()

add_library(libr4_ort_ep SHARED
  src/libr4_ort_ep.cpp
  src/ort_session.cpp
)

target_include_directories(libr4_ort_ep PUBLIC
  ${CMAKE_CURRENT_SOURCE_DIR}/include
  ${LIBR4_ORT_ROOT}/include
)

target_link_libraries(libr4_ort_ep PRIVATE "${LIBR4_ORT_LIB}")

if(WIN32)
  target_compile_definitions(libr4_ort_ep PRIVATE LIBR4_ORT_BUILD_DLL)
endif()

set_target_properties(libr4_ort_ep PROPERTIES
  OUTPUT_NAME "libr4_ort_ep"
  POSITION_INDEPENDENT_CODE ON
)

add_custom_command(TARGET libr4_ort_ep POST_BUILD
  COMMAND ${CMAKE_COMMAND} -E copy_if_different
          "${LIBR4_ORT_DLL}"
          "$<TARGET_FILE_DIR:libr4_ort_ep>"
  COMMENT "Copy onnxruntime runtime next to libr4_ort_ep"
)
