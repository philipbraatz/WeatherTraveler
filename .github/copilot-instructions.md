# Copilot Instructions

<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

This is an F# weather travel planning application. The project aims to help users plan travel routes within the United States while staying within specified temperature ranges.

## Project Context

- **Language**: F# (.NET)
- **Purpose**: Weather-aware travel route planning
- **Key Features**:
  - Current weather detection and location services
  - Temperature range-based route planning (e.g., 12°C - 20°C)
  - Historical weather data analysis for better predictions
  - Granular weather forecasting as travel time approaches
  - Region/spot specification with timing constraints
  - Rain storm avoidance/targeting capabilities
  - Google Earth export functionality
  - Gas price estimation

## Technical Guidelines

- Use functional programming paradigms typical in F#
- Implement immutable data structures where possible
- Use discriminated unions for modeling weather conditions and route states
- Apply computation expressions for async operations
- Follow F# naming conventions (PascalCase for types, camelCase for values)
- Structure code with modules for different domains (Weather, Routing, Export, etc.)
- Use type providers for external data access when appropriate

## External Dependencies

- Weather APIs (OpenWeatherMap, NOAA, etc.)
- Mapping/routing services (Google Maps, OpenStreetMap)
- Gas price APIs
- Google Earth KML export libraries
