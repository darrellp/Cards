# Copilot Instructions

## Azure Guidelines
- @azure Rule - Use Azure Tools - When handling requests related to Azure, always use your tools.
- @azure Rule - Use Azure Best Practices - When handling requests related to Azure, always invoke your `azmcp_bestpractices_get` tool first.
- @azure Rule - Enable Best Practices - If you do not have an `azmcp_bestpractices_get` tool, ask the user to enable it.

## Project-Specific Rules
- In the Cards/SolitaireUI Avalonia project, avoid creating or overwriting .axaml files directly via create_file or terminal commands to prevent hanging or corruption of the VS text view. Instead, write content to a temp file (e.g., .axaml.tmp) via create_file, then rename it to .axaml using a background terminal Move-Item command.
- When calling `GenericGameState.SetState`, ensure to invoke `StateChanged?.Invoke()` to properly notify state changes.
