# Unity VR Environment

Unity project implementing a VR-enabled treasure-hunting game for studying human-AI collaboration with virtual agents.

## Project Overview

- **Main Scene**: `Assets/Scenes/FindTheTreasure.unity`
- **Batch Simulation**: `Assets/Scenes/SimulationBatch.unity`
- **Characters**: Anne, Matthieu (with facial animation and gaze systems)
- **Features**: VR support, speech synthesis, emotion expression, interactive objects

## Quick Start

### Requirements

- **Unity 2021.3 LTS** or later
- **Third-party packages** (not included, obtain separately):
  - Meta XR SDK / Oculus Integration (for VR, optional)
  - RootMotion FinalIK (Asset Store, paid)
  - SALSA LipSync (Asset Store, paid)

### Setup

1. Open `Unity/` folder in Unity Hub
2. Install required packages from Asset Store/official sources
3. Open `Assets/Scenes/FindTheTreasure.unity`
4. Configure LLM and PCM server connections

**Note**: Project runs on desktop without VR packages. VR support is optional.

## License

- ✅ Virtual human models and game scripts included
- ❌ Commercial packages excluded (must purchase separately)

## See Also

- [Main Documentation](../README.md)
- [PCM Server](../PCM/README.md)
- [LLM Server](../LLM/README.md)

**Last Updated**: October 2025
