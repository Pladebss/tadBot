
# Discord Booster Bot V3.1 (Local Notes)

## Quick start (Windows)
1) Install Node.js (LTS) so `node` and `npm` are available.
2) Clone this repo into a fixed folder.
3) Edit `config.json` (channel IDs, superUsers, mapping, restart paths, slash registration mode).
4) Copy `run_bot.sample.bat` to `run_bot.bat` and fill in your token.
5) Double-click `run_bot.bat`.

## IMPORTANT
- This bot requires the **Message Content Intent** enabled in the Discord Developer Portal for your bot,
  because it can detect boost strings in plaintext message content.
- `run_bot.bat` is gitignored; keep your token there.
