# Device QA Checklist — Floracotta v1.0

Run this on **at least 2 Android and 2 iOS devices** before store submission (include one small phone and one large/notch phone per platform).

**Build type:** Release (not Development Build)  
**Ads:** `_testMode` off, production ad unit IDs set  
**Tester name / device / OS / date:** _______________

---

## Install and first launch

| # | Test | Pass | Notes |
|---|------|------|-------|
| 1 | Fresh install opens to start screen | ☐ | |
| 2 | Tap start → difficulty → gameplay loads | ☐ | |
| 3 | Tutorial runs on first install (if not completed before) | ☐ | |
| 4 | Safe-area UI clears notch (top-right HUD, popups) | ☐ | |

## Core gameplay

| # | Test | Pass | Notes |
|---|------|------|-------|
| 5 | Pick up tile from hand (touch) | ☐ | |
| 6 | Drag and place tile on grid | ☐ | |
| 7 | Merge chain plays with correct timing | ☐ | |
| 8 | Score pop and HUD update | ☐ | |
| 9 | Undo works | ☐ | |
| 10 | Invalid placement feedback | ☐ | |

## Menus and HUD (all touch, no hover)

| # | Test | Pass | Notes |
|---|------|------|-------|
| 11 | Settings open/close + camera pan | ☐ | |
| 12 | Music and sound sliders (touch drag) | ☐ | |
| 13 | Seeds screen | ☐ | |
| 14 | Help screen | ☐ | |
| 15 | Bag view open/close | ☐ | |
| 16 | Tool shop open, leave animation completes | ☐ | |
| 17 | Mulligan button | ☐ | |
| 18 | Credits / High score screens | ☐ | |

## Economy and ads

| # | Test | Pass | Notes |
|---|------|------|-------|
| 19 | Passive seed earn over time | ☐ | |
| 20 | Spend seeds (e.g. mulligan when unaffordable shows popup) | ☐ | |
| 21 | Rewarded ad plays → seeds granted | ☐ | |
| 22 | Airplane mode → ad shows “unavailable”, **no** reward | ☐ | |

## Popups and win flow

| # | Test | Pass | Notes |
|---|------|------|-------|
| 23 | First-time unlock popup slides in/out | ☐ | |
| 24 | Repeat unlock tiny tab | ☐ | |
| 25 | Win: score pops → hold → win screen animates | ☐ | |
| 26 | Tap to restart works (touch) | ☐ | |

## Persistence

| # | Test | Pass | Notes |
|---|------|------|-------|
| 27 | Mid-game: kill app → relaunch → save resumes | ☐ | |
| 28 | Volume settings persist after restart | ☐ | |
| 29 | High score recorded after win | ☐ | |

## Performance and audio

| # | Test | Pass | Notes |
|---|------|------|-------|
| 30 | Stable feel (~60 FPS), no long freezes after merges | ☐ | |
| 31 | FMOD music/SFX on boot, no bank errors in log | ☐ | |
| 32 | Score loop stops after win | ☐ | |

## Regression (30 FPS throttle optional)

| # | Test | Pass | Notes |
|---|------|------|-------|
| 33 | Panel slides feel consistent at 30 FPS | ☐ | |
| 34 | Camera pan between screens feels correct | ☐ | |

---

## Sign-off

| Platform | Devices tested | Pass all critical? | Tester |
|----------|----------------|-------------------|--------|
| Android | | ☐ | |
| iOS | | ☐ | |

**Critical failures (block release):** touch input broken, ads grant free rewards on failure, crash on launch, save corrupted, unplayable on notch devices.

**After QA:** Complete store forms using [STORE_LISTING.md](STORE_LISTING.md) and [PRIVACY_POLICY.md](PRIVACY_POLICY.md).
