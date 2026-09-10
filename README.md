# Psychological Duel Game (Unity 6)

A single-player/multiplayer psychological tension game built in **Unity 6 (URP)**. The project centers around non-GUI/immersive observation mechanics, micro-behavior detection, and real-time tension management where players evaluate target subjects through physiological indicators and strategic choices.

---

## 🛠️ Tech Stack & Architecture

* **Engine**: Unity 6 (Universal Render Pipeline - URP)
* **Architecture**: Event-driven decoupled architecture powered by `GameEventManager`
* **UI Framework**: TextMeshPro & URP Post-Processing driven UI
* **Version Control**: CLI Git workflow (clean repository tracking, `.gitignore` filtered for Unity caches)

---

## 🎯 Core Mechanics & Features

* **Physiological Tracking (SAN / HEAT)**: Real-time calculation and visualization of Sanity (SAN) and Psychological Stress/Heat (HEAT) for both the player and the target employee.
* **Tactile Observation System**: Time-freeze inspection mechanics allowing point-and-click raycasting to analyze subtle physical details (e.g., sweat, body language cues).
* **Decoupled Event Management**: UI elements and game status systems react dynamically to global state updates via C# Action delegates (`Action<int, float, float>`).
* **Dynamic Post-Processing**: Integrated URP visual effects (Vignette, Motion Blur) triggered directly by high-stress physiological thresholds.

---

## 📁 Repository Structure

```text
Assets/
├── Scripts/
│   ├── UI/
│   │   └── HUDStatusController.cs     # Manages HUD status bars and smooth UI interpolation
│   ├── Core/
│   │   └── GameEventManager.cs        # Global event definitions and subscriber logic
│   └── Mechanics/                     # Raycasting inspection & time-freeze systems
├── ScriptableObjects/                 # Employee profiles and baseline states
└── Settings/                          # URP assets and Post-Processing profiles
