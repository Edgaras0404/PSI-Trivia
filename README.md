# PSI-Trivia

# Backend Services Architecture

## Core Backend Services

### Game Session Management Service
- Manage active game instances, player connections, and game state transitions using in-memory caching (Redis/MemoryCache)

### Question Distribution Engine
- Timer-based service that automatically fetches and broadcasts questions to all players in a session

### Real-time Answer Validation API
- Process incoming answers, validate correctness, and calculate scores with millisecond timing accuracy

### Player Connection Hub
- SignalR hub managing WebSocket connections, handling disconnections/reconnections, and maintaining active player lists

## Data Management Services

### Question Pool Repository
- Database layer with CRUD operations for questions, categories, difficulty levels, and question metadata

### User Performance Analytics Service
- Background service tracking player statistics, calculating ELO ratings, and updating skill tiers

### Game History Persistence Service
- Async service logging all game events, player actions, and results for audit trails and analytics

## Background Processing

### Leaderboard Calculation Engine
- Background job processing that recalculates rankings, updates seasonal leaderboards, and maintains historical data

### Achievement Processing Service
- Event-driven service that monitors player actions and awards achievements/badges based on defined criteria

## Infrastructure Services

### Game State Synchronization Service
- Ensures consistency across distributed instances and handles failover scenarios

### Game Modes
- Free for all and teams
