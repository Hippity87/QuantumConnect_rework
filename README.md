## Overview

This repository hosts **two client-side apps** under one Blazor WebAssembly solution:

1. **Quantum Connect (Rework)** — a grid-based connect-style game with gravity and special moves, plus simple bot opponents for local testing.
2. **Weather app** — a lightweight UI for browsing recent temperature/humidity readings. It’s unrelated to the game but shares the same WASM shell for ease of development.


## Features

### Quantum Connect (Rework)
- Based on the original game idea by Víctor Uría Valle and Cristian Schuszter from CERN. Original game was in Java, my rework is with C#.
- Local bot play for testing:
  - `LexBot` — prioritizes immediate wins/blocks and early pattern pre-empts, then runs a pruned alpha–beta minimax over safe moves; a lightweight heuristic (centrality + height) is used only as a fallback (or when no safe moves exist, possibly with a bomb).
  - `RandomBot` — baseline for comparison.

### Weather App
- Minimal UI for browsing temperature/humidity samples.
- Simple, responsive list/tiles (exact UI may evolve).


## Tech Stack
- Blazor WebAssembly (C# / .NET 8)

> This repo documents only the client side. Any external data sources (e.g., sensor data) are out of scope here.


## Getting Started (Dev)

```bash
# Prerequisite: .NET 8 SDK
dotnet restore
dotnet build
dotnet run
```

- This should allow you to test the game locally but weatherapp won't work because **endpoints are not provided.**


## Development Notes

- This is a personal project; documentation aims to explain what’s inside rather than provide a reusable template.

- The Weather app and the game live side-by-side purely for convenience—expect minimal coupling and a straightforward UI layer.

© ProjectLexagon — Jesse Laine