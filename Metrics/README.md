# Metrics & Analysis Tools

Python utilities and Jupyter notebooks for analyzing PCM-LLM simulation results.

## Project Overview

Analysis tools for:
- **Trajectory extraction** from MongoDB-stored PCM predictions
- **Statistical visualization** with confidence intervals and significance tests
- **Time series analysis** of preferences, emotions, and spatial dynamics
- **Experimental result processing** across conditions

## Files

### Core Scripts

- **`functions.py`**: Data processing and analysis functions (11 functions)
- **`plot.py`**: Statistical visualization and time-series plotting (7 functions)

### Jupyter Notebooks

- **`Game_result_analyse.ipynb`**: Statistical analysis of game outcomes (success rates, chi-square tests)
- **`LLM_evaluation.ipynb`**: Conversation history evaluation and outlier detection
- **`Trajectories_analysis.ipynb`**: Deep dive into PCM prediction trajectories and free energy

## Quick Start

### MongoDB Connection

Update connection string in notebooks:
```python
connection_string = "mongodb://root:root@172.27.10.131:7001"
```

**Databases**: `histories` (LLM logs), `trajectories` (PCM predictions)

### Output Structure

```
RawData/
└── 202506090749/
    ├── simulation107_llama3.1_clean.json     # Conversations
    ├── simulation107_llama3.1_actions.json   # Action predictions
    └── simulation107_0_*.json                # Trajectory trees
```

CSV exports saved to `Simulation_results/`:
- Preference dynamics per object
- Emotion trajectories (valence, certainty)
- Spatial statistics

## See Also

- [Main Documentation](../README.md)

**Last Updated**: October 2025
