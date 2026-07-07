# 🧬 Smart Genes

> *"Ever wanted your tortured artists to always be very neurotic and too smart? Now you can have that."*

A RimWorld mod that dynamically generates a forced-trait gene for every trait in your game — vanilla or modded — giving you surgical control over your xenotypes.

[![Steam Workshop](https://img.shields.io/badge/Steam_Workshop-Subscribe-1b2838?style=flat&logo=steam&logoColor=white)](https://steamcommunity.com/sharedfiles/filedetails/?id=3287315209)
[![RimWorld](https://img.shields.io/badge/RimWorld-1.5_%7C_1.6-red?style=flat)](https://store.steampowered.com/app/294100/RimWorld/)
[![Requires Biotech](https://img.shields.io/badge/Requires-Biotech-orange?style=flat)](https://store.steampowered.com/app/1826140/RimWorld__Biotech/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow?style=flat)](LICENSE)

---

## What It Does

When the game loads, Smart Genes scans every `TraitDef` present from vanilla RimWorld, Biotech, and any mods you have installed, and generates a gene for each one. Equip a pawn with that gene, and they'll always have that trait, no matter what.

Multi-degree traits (like Neurotic / Very Neurotic, or Trigger-Happy / Careful Shooter) each get their **own dedicated gene**, one per degree, so nothing is skipped.

---

## Features

- **Full trait coverage** — one gene per trait degree, including multi-level traits that previous versions missed
- **Mod-compatible** — automatically picks up traits from any mod, as long as Smart Genes loads after it
- **Clean loot pools** — generated genes have `selectionWeight = 0`, so they won't show up in random xenogerm or genepack drops
- **Stays out of your way** — genes are sorted to the bottom of the xenotype editor's gene picker and collapsed into their own category

---

## Installation

### Steam Workshop *(recommended)*
[Subscribe here](https://steamcommunity.com/sharedfiles/filedetails/?id=3287315209). Enable in your mod list and load **after** any mods whose traits you want covered.

### Manual
1. Download the latest release from the [Releases](../../releases) page
2. Extract into your RimWorld `Mods/` folder
3. Enable in the mod manager, placing Smart Genes **after** any trait-adding mods

---

## Load Order

```
RimWorld (Core)
Biotech
[any mods that add traits]
Smart Genes          ← goes here
```

Smart Genes reads `TraitDef`s at startup. Any mod loaded after it won't have its traits picked up.

---

## Compatibility

| Version | Status |
|---------|--------|
| RimWorld 1.6 | ✅ Tested |
| RimWorld 1.5 | ✅ Tested |
| RimWorld 1.4 and below | ❓ Untested (requires Biotech) |

**Mod compatibility:** Works with any mod that adds traits via standard `TraitDef`s. May log a harmless warning if another mod already defines a forced-trait gene with the same `defName`.

---

## Notes

- There are a **lot** of genes. Collapsing the SmartGenes category in the xenotype editor is strongly recommended.
- Genes are prefixed `SG_ForcedTrait_` to avoid conflicts with other mods.
- This mod generates genes at runtime — there is no static XML list of genes bundled with it.

---

## Building from Source

Requires the [RimWorld modding environment](https://rimworldwiki.com/wiki/Modding_Tutorials/Setting_up_a_solution) and a reference to:
- `Assembly-CSharp.dll` (from your RimWorld install)
- `UnityEngine.CoreModule.dll`
- `Verse.dll` / `RimWorld.dll`

```bash
# Clone the repo
git clone https://github.com/theAxolotlAntics/RimWorldMod-SmartGenes.git

# Build with your IDE or dotnet
dotnet build
```

Output DLL goes in `Assemblies/`.

---

## Contributing

Bug reports and pull requests are welcome. If a trait from a specific mod isn't being picked up, please include:
- The mod name and Steam ID
- The trait's `defName` (visible in dev mode)
- Your mod load order

---

## Changelog

See [Change Notes on Steam](https://steamcommunity.com/sharedfiles/filedetails/changelog/3287315209) for the full history.

| Version | Notes |
|---------|-------|
| 3.1 | Fixed multi-degree traits being skipped (Neurotic, Trigger-Happy, etc.); added `selectionWeight = 0` to keep genes out of loot pools |
| 3.0 | 1.6 support |
| Earlier | See Steam change notes |

---

## License

[MIT](LICENSE) — use freely, just credit the original mod.

---

*Made by [Axolotl](https://steamcommunity.com/profiles/76561199216208436)*
