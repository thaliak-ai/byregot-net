# byregot-net

A Dalamud plugin that registers a trained RL agent as a named solver in [Artisan](https://github.com/PunishXIV/Artisan). The agent selects crafting actions step-by-step based on live game state, running entirely in-process via embedded ONNX models — no external server required.

## Requirements

- [Artisan](https://github.com/PunishXIV/Artisan) with external solver IPC support

## How it works

On load the plugin registers itself in Artisan's solver dropdown under a configurable name (default: `RL Agent`). When a craft starts with this solver selected, Artisan calls the plugin on each step with the current craft and step state as JSON. The plugin builds the observation vector, runs inference against the embedded ONNX model, and returns the chosen action.

Two models are embedded:
- **Normal** (`obs_dim=28`) — standard recipes
- **Expert** (`obs_dim=42`) — expert/cosmic recipes with condition-availability flags

The correct model is selected automatically based on the recipe type.

## Installation

Add this repository to your Dalamud custom plugin repositories and install **byregot-net**.

## Configuration

On first install a setup window will appear to configure the solver name and opt-in to anonymous data sharing. Both settings can be changed later via the plugin settings window in Dalamud.

## License

Source code is licensed under the [MIT License](LICENSE) — copyright (c) 2026 thaliak-ai.

The embedded model weights (`craft_agent_normal.onnx`, `craft_agent_expert.onnx`) are included for convenience. If you redistribute them or build on them, please credit **thaliak-ai / byregot-net** as the source.
