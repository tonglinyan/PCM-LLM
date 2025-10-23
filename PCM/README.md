# Projective Consciousness Model (PCM)

C# ASP.NET Core server implementing the Projective Consciousness Model for cognitive simulation with free-energy minimization and belief inference.

## Project Overview

PCM performs predictive coding computations for virtual agents:
- Belief inference and Theory of Mind
- Action prediction via free-energy minimization
- Spatial cognition and vision
- LLM integration for verbal behavior

## Quick Start

### Requirements

- **.NET 6.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/6.0)
- **MongoDB** (optional, for trajectory logging)

### Installation

```bash
cd PCM
dotnet build flexop.csproj
dotnet run --project flexop.csproj
```

Server starts with Swagger UI at `http://localhost:<port>/swagger`

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/initialize-parameters` | POST | Initialize simulation with agent config |
| `/api/compute-worldstate` | POST | Compute agent predictions from world state |
| `/api/question` | POST | Handle participant questions |
| `/api/speak` | POST | Process agent utterances |
| `/api/stop` | POST | Stop simulation |

**Example**:
```bash
curl -X POST "http://localhost:5000/api/initialize-parameters?AA_Mode=Verbal" \
  -H "Content-Type: application/json" \
  -d @config.json
```

## Architecture

- **`Core/`**: Free-energy engine, prediction algorithms, geometry
- **`Verbal/`**: LLM integration (HTTP client to Python server at port 8888)
- **`Api/`**: REST controllers and data transfer objects

## Dependencies

Auto-installed via NuGet:
- MathNet.Numerics (5.0.0)
- MongoDB.Driver (3.1.0)
- Newtonsoft.Json (13.0.3)

## See Also

- [Main Documentation](../README.md)
- [LLM Server](../LLM/README.md)
- [Unity Client](../Unity/README.md)

**Last Updated**: October 2025
