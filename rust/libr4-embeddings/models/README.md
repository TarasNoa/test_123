# ONNX Embedding Models

Place the following files in this directory before building the Docker image:

## Required files

| File | Source | Size |
|------|--------|------|
| `minilm-l6-v2.onnx` | [HuggingFace: sentence-transformers/all-MiniLM-L6-v2](https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2) | ~90 MB |
| `tokenizer.json` | Same repo, `tokenizer.json` file | ~700 KB |

## Download commands

```bash
# Install huggingface_hub (Python)
pip install huggingface_hub

python -c "
from huggingface_hub import hf_hub_download
hf_hub_download('sentence-transformers/all-MiniLM-L6-v2', 'onnx/model.onnx', local_dir='.')
hf_hub_download('sentence-transformers/all-MiniLM-L6-v2', 'tokenizer.json', local_dir='.')
import os; os.rename('onnx/model.onnx', 'minilm-l6-v2.onnx')
"
```

## Multilingual model (optional, better for Russian text)

```bash
python -c "
from huggingface_hub import hf_hub_download
hf_hub_download('intfloat/multilingual-e5-small', 'onnx/model.onnx', local_dir='.')
hf_hub_download('intfloat/multilingual-e5-small', 'tokenizer.json', local_dir='ml-tokenizer')
import os; os.rename('onnx/model.onnx', 'multilingual-e5-small.onnx')
"
```
