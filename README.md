# oth-settings-hub

**SettingsHub**: an [Over the Hill](docs/games/over-the-hill/README.md) MelonLoader mod that puts
every mod's settings in one menu. Press **F5**: one tab per mod. WWTHP mods draw their own tab
(and stop using their own menu key while the hub runs); every other mod with MelonPreferences
gets a plain editor tab (toggles, sliders, key capture, text).

```bash
python3 -m venv .venv && .venv/bin/pip install -r requirements-dev.txt
tools/check.sh                             # all checks, no deploy
cd SettingsHub && dotnet build -c Release  # build + deploy to the game
```

Details: [docs/project/README.md](docs/project/README.md). Agents: start at [AGENTS.md](AGENTS.md).
