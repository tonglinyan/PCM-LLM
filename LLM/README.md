# LLM Inference Server

Python server using large language models (LLaMA, Qwen) to simulate character behavior with Theory of Mind, preference modeling, and emotion/action prediction.

## Project Overview

- **Multi-agent simulation** with preference updating and emotional modeling
- **MongoDB-based conversation history** with vector embeddings
- **Task types**: Question Answering (QA), Preference Updating (PU), Action Prediction (AP)
- **aiohttp server** on port 8888 for REST API

## Quick Start

### Requirements

```bash
cd LLM
pip install -r requirements.txt
```

Key dependencies: `torch`, `transformers`, `aiohttp`, `pymongo`, `vllm`

### Model Setup

Models download automatically from HuggingFace on first run:
- **LLaMA 3.1**: `meta-llama/Meta-Llama-3.1-8B-Instruct` (requires authentication)
- **Qwen 2.5**: `Qwen/Qwen2.5-7B-Instruct-1M`

For LLaMA models:
```bash
huggingface-cli login
```

### Configuration

Update MongoDB connection in `memory_management.py:72` if needed:
```python
# Default: mongodb://root:root@172.27.10.131:7001
```

### Running

```bash
# Quick start with default settings
./run.sh

# Or manually
torchrun --nproc_per_node 1 inference_server.py \
  --model_name llama3.1 \
  --model_size 8B \
  --history_saving \
  --remote
```

## Usage

### Available Models

- `llama3.1` - Meta LLaMA 3.1 8B/70B
- `qwen2.5` - Qwen2.5-7B-Instruct-1M

### Key Arguments

| Argument | Default | Description |
|----------|---------|-------------|
| `--model_name` | `llama3.1` | Model to use |
| `--model_size` | `8B` | Model size: `8B`, `70B` |
| `--history_saving` | `False` | Save to MongoDB |
| `--remote` | `False` | Run HTTP server (port 8888) |
| `--max_seq_len` | `8192` | Max sequence length |

### API Endpoints

- `POST /` - Main inference endpoint
- `POST /start` - Initialize conversation
- `GET /ws` - WebSocket endpoint

## See Also

- [Main Documentation](../README.md)
- [PCM Server](../PCM/README.md)
- [Unity Client](../Unity/README.md)

**Last Updated**: October 2025
