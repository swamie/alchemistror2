# Alchemist (Risk of Rain 2 survivor mod)

A survivor built around **stacking damage over time and debuffs**. She throws potions, cuts enemies open, infects the wounds, then cashes everything in at once.

> **Status: 0.1.0, playable prototype.** All the gameplay code is here. The model, icons and VFX are **borrowed vanilla placeholders** (see [What is placeholder](#what-is-placeholder)).

---

## Kit at a glance

| Slot | Skill | What it does |
|---|---|---|
| Passive | **Potion Belt** | Carry 3 potions picked in the loadout. **Scroll wheel** swaps the active one. Starts with the Clockwork Crow. |
| Primary | **Throw Potion** | Lobs the active potion (Commando grenade arc and speed). Shatters on impact for **200%** plus the potion's effect. |
| Secondary | **Cutthroat Knife** | Very close slash, **360%**, **always Lacerates**. 4s cooldown. |
| Secondary (alt) | **Thrown Elbow** | Very close hit, **250%**, **stuns** and **confuses** for 3s (target fights its own allies). 7s cooldown. |
| Utility | **Vapour Vault** | Smash a fume vial at your feet and vault backwards. Cloud slows enemies 40% for 5s, you get cloaked speed. 7s cooldown. |
| Special | **Transmute** | Every enemy within 25m takes **150% of the DoT damage you still had ticking on it**, plus **300%**. Your DoTs are used up. 12s cooldown. |

### Infect combo

1. Cut with **Cutthroat Knife** (applies Laceration, a bleed).
2. Hit that enemy with **any potion**.
3. The cuts are consumed and become **Infected Wound**: 240% base damage per second for 6s, +50% per extra cut.

### The 9 potions

Pick any 3 (duplicates allowed) on the three **Potion Belt** rows of the loadout screen.

| Potion | Effect |
|---|---|
| **Plague Flask** | Leaves a plague pool for 6s. Every tick inside adds a Plague DoT stack, so it ramps the longer they stand in it. |
| **Hellfire Tonic** | Ignites (vanilla burn, works with Ignition Tank). |
| **Corrosive Vitriol** | Corroded: -6 armor per stack, up to 10. |
| **Frostbite Draught** | Chilled: -12% speed per stack. At 5 stacks the enemy freezes. |
| **Hemlock Philter** | Enfeebled: enemy deals 30% less damage and attacks 25% slower for 6s. |
| **Catalyst Elixir** | +50% hit damage per unique debuff on the target, and extends your DoTs on it by 2s. |
| **Miasma Spores** | Contagious: if it dies, its DoTs and debuffs jump to enemies within 15m. |
| **Sanguine Draught** | Bloodlet: DoT damage it takes heals you for 15%. |
| **Volatile Concoction** | No debuff. Bigger, 320% blast. |

### Clockwork Crow (exclusive red item)

- She spawns with it every run. Nothing else can get it.
- Can't be scrapped, printed, duplicated, rerolled, stolen, copied, or dropped by chests (`canRemove = false`, `WorldUnique`, `CannotSteal`, `CannotCopy`, `CannotDuplicate`, `AIBlacklist`).
- Every 3.5s it pecks the **most afflicted** enemy within 45m for 150%, adds one more stack of every DoT already on it, and extends its debuffs by 1s.

### Skins

| Skin | Unlock |
|---|---|
| Apothecary | Default |
| Plaguewarden | Beat the game or obliterate on Monsoon (mastery) |
| Verdigris | Have 5 different debuffs on one enemy at once |
| Ashen Herbalist | Kill 5 enemies with a single Transmute |

---

## What is placeholder

Code can't make 3D art, so these are vanilla stand-ins that need real assets:

- **Model and animations:** a recoloured **Bandit** (hat and long coat is the closest vanilla plague doctor silhouette). A proper female plague-doctor model without the mask needs to be made in Blender, rigged, and set up in a Unity project.
- **Skins:** colour palettes over the placeholder model. Once a real model exists, each skin becomes its own material set / mesh.
- **Potion models:** Commando's grenade, tinted per potion.
- **Plague pool and fume cloud:** Acrid's acid pool, tinted.
- **Crow:** Wax Quail's bird model, painted gunmetal.
- **Icons:** skill icons are Bandit's, buff icons reuse vanilla ones, skin icons are generated colour swatches.
- **Potion belt HUD:** a simple on-screen box row. Can be turned off in config.

### Replacing the placeholder model

1. Build the model in Blender, export FBX, and set up a Unity 2021.3 project with the RoR2 ThunderKit or the "Henry" survivor template.
2. Put the model, materials, icons and VFX in an AssetBundle.
3. Load the bundle in `AlchemistPlugin.Awake` and swap `GameAssets.BanditBody` usage in `AlchemistSurvivor.Init` for your own body prefab. Everything else (skills, potions, crow, achievements) does not care which model is used.

---

## Building

Needs the **.NET SDK 6 or newer**.

```bash
./build.sh          # Linux / macOS
./build.ps1         # Windows PowerShell
```

This restores the stripped game and BepInEx reference libraries from NuGet (`nuget.org` and `nuget.bepinex.dev`, see `nuget.config`) and writes `build/Alchemist.zip`.

Offline build: point it at a folder holding the reference dlls (game `Managed` folder dlls, `BepInEx.dll`, `0Harmony.dll`, and the R2API dlls):

```bash
./build.sh /path/to/dlls
```

## Installing

- **r2modman / Thunderstore Mod Manager:** Settings, Import local mod, pick `build/Alchemist.zip`. It pulls the R2API dependencies.
- **Manual:** copy `AlchemistMod.dll` into `BepInEx/plugins/Alchemist/` and install the R2API modules listed in `thunderstore/manifest.json`.

## Config

`BepInEx/config/swamie.Alchemist.cfg`

- Scroll wheel switching on/off, invert scroll
- Optional next / previous potion keys (useful for controller remapping)
- Show / hide the potion belt HUD

## Code map

```
src/AlchemistMod/
  AlchemistPlugin.cs        entry point, load order
  AlchemistConfig.cs        config file entries
  Content/                  content pack, vanilla asset loading, all English text
  Survivor/                 body, skill slots, skins, achievements
  Potions/                  the 9 potions, their projectiles, scroll-wheel selector
  SkillStates/              throw, knife, elbow, vault, transmute
  Effects/                  buffs, DoTs, damage-type hooks, confusion
  Items/                    Clockwork Crow
```

## Known gaps

- Not yet play-tested in game. It compiles against the RoR2 reference libraries; expect tuning and a few fixes on first launch.
- No AI master, so Artifact of Vengeance will not spawn an Alchemist doppelganger.
- Controller: scroll has no default controller bind yet; use the next / previous key config with a remapper.
- Loadout rows for the potion belt may show a generic row title.
