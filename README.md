# PCM-LLM: Hybrid Cognitive Architecture for Virtual Human Simulation

A research platform combining **Projective Consciousness Model (PCM)** with **Large Language Models (LLMs)** for simulating Theory of Mind, social cognition, and collaborative behavior in virtual agents. The system integrates C# predictive coding, Python LLM inference, and Unity VR to create cognitively-rich virtual humans capable of collaborative treasure-hunting tasks.

[![Code License](https://img.shields.io/badge/Code_License-MIT-blue)](LICENSE)
[![Data License](https://img.shields.io/badge/Data_License-CC_BY_4.0-green)](DATA_LICENSE)

---

## 🎯 Project Overview

**PCM-LLM** is a research platform simulating virtual humans with Theory of Mind, emotional dynamics, active inference, natural language processing, and spatial cognition for collaborative treasure-hunting tasks.

---

## 📁 Repository Structure

```
PCM-LLM/
├── LLM/          # Python LLM inference server (port 8888)
├── PCM/          # C# predictive coding engine
├── Unity/        # Unity 3D VR environment
├── Metrics/      # Analysis tools and notebooks
└── RawData/      # Simulation outputs (gitignored)
```

---

## 🚀 Quick Start

### Prerequisites

**Required:**
- **.NET 6.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/6.0)
- **Python 3.8+** with pip
- **MongoDB** (for conversation storage)

**Optional (for full features):**
- **Unity 2022.3.12 LTS** (for VR visualization)
- **HuggingFace account** (for LLaMA models)

### Installation

#### 1. Set Up Python LLM Server
```bash
cd LLM
pip install -r requirements.txt

# For LLaMA models (requires HF authentication)
huggingface-cli login

# Start server
./run.sh
# Server will run on http://localhost:8888
```

#### 2. Set Up C# PCM Server
```bash
cd PCM
dotnet build flexop.csproj
dotnet run --project flexop.csproj
# Server will start with Swagger at http://localhost:<port>/swagger
```

#### 3. Configure MongoDB
Ensure MongoDB is accessible at the connection string specified in:
- `LLM/memory_management.py:72` (default: `mongodb://root:root@172.27.10.131:7001`)

---

## 📄 License

This project uses a dual-license approach:

### Code License: MIT

All source code (Python, C#, Unity scripts) is licensed under the **MIT License**.
[![Code License: MIT](https://img.shields.io/badge/Code_License-MIT-blue)](LICENSE)

### Data License: CC BY 4.0

Research data, simulation results, and experimental outputs are licensed under **Creative Commons Attribution 4.0 International (CC BY 4.0)**.
[![Data License: CC BY 4.0](https://img.shields.io/badge/Data_License-CC_BY_4.0-green)](DATA_LICENSE)

### Third-Party Components

**Excluded from MIT License:**

This repository does **not include** the following components. Users must obtain them separately and comply with their respective licenses:

1. **LLM Model Weights** (Not included)
   - LLaMA 3.1: Requires Meta license agreement
   - Qwen 2.5: Subject to Alibaba Cloud license
   - T5: Apache 2.0 (auto-downloaded from HuggingFace)

2. **Unity Asset Store Packages** (Not included)
   - Meta XR SDK / Oculus Integration
   - RootMotion FinalIK (Commercial)
   - SALSA LipSync (Commercial)
   - Character Creator Tools (Commercial)

3. **Runtime Dependencies** (Open Source)
   - .NET Runtime: MIT License
   - Python packages: See LLM/requirements.txt (Apache 2.0, BSD)
   - MongoDB: SSPL (must install separately)

### Citation

If you use this software or data in academic research, please cite:

```bibtex
@software{pcm_llm_2025,
  title = {PCM-LLM: Hybrid Cognitive Architecture for Virtual Human Simulation},
  author = {Tonglin Yan, Grégoire Sergeant-Perthuis, David Rudrauf},
  year = {2025},
  url = {https://github.com/tonglinyan/PCM-LLM.git},
  note = {Code: MIT License, Data: CC BY 4.0}
}
```

---

## See Also

- **[LLM Server](LLM/README.md)**: Python inference server details
- **[PCM Server](PCM/README.md)**: C# cognitive engine documentation
- **[Unity Client](Unity/README.md)**: VR environment setup
- **[Metrics](Metrics/README.md)**: Analysis tools and notebooks

---

**Last Updated**: October 2025
**Version**: 1.0.0