#!/usr/bin/env bash
# Optional DeepSpeed fine-tune launcher for Libr4 exported JSONL datasets.
# Requires: deepspeed, transformers, datasets, and a local base model checkpoint.
set -euo pipefail

DATASET_ROOT="${1:-.libr4/fine-tuning/datasets/django/train.jsonl}"
BASE_MODEL="${2:-deepseek-ai/deepseek-coder-1.3b-base}"
OUTPUT_DIR="${3:-.libr4/fine-tuning/checkpoints/run1}"

if [[ ! -f "$DATASET_ROOT" ]]; then
  echo "Dataset not found: $DATASET_ROOT" >&2
  exit 1
fi

echo "Fine-tuning stub"
echo "  dataset=$DATASET_ROOT"
echo "  base_model=$BASE_MODEL"
echo "  output_dir=$OUTPUT_DIR"
echo
echo "Wire your DeepSpeed training script here (deepspeed --num_gpus=1 train.py ...)."
echo "Expected JSONL fields: instruction, output"
