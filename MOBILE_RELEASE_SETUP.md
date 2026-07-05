# Mobile Release Setup

Complete these steps before building floracotta v1.0 for Google Play and the App Store.

## Version and bundle IDs (configured in repo)

- **Version:** `1.0.0` (`ProjectSettings` → Player → Version)
- **Android package:** `com.wobblestone.floracotta`
- **iOS bundle ID:** `com.wobblestone.floracotta`
- **Android version code:** `1` (increment on every Play upload)
- **iOS build number:** `1` (increment on every App Store upload)

---

## Android release keystore

You still need to create and configure a release keystore (not in repo).

1. **Create a release keystore** (one-time):
   ```bash
   keytool -genkey -v -keystore floracotta-release.keystore -alias floracotta -keyalg RSA -keysize 2048 -validity 10000
   ```
   Store the keystore file and all passwords in a password manager. **You need the same keystore for every update.**

2. **Configure in Unity:**
   - Edit → Project Settings → Player → Android → Publishing Settings
   - Enable **Custom Keystore**
   - Set keystore path, keystore password, alias, and alias password

3. **Build:** File → Build Settings → Android → Build (use **AAB** for Google Play, not APK)

---

## iOS signing and build

1. **Apple Developer:** Ensure App ID `com.wobblestone.floracotta` exists in the developer portal.

2. **Unity:**
   - Edit → Project Settings → Player → iOS → Other Settings
   - Set signing team (Automatic or Manual with your profile)
   - Assign app icons and a launch screen/storyboard

3. **Build:** File → Build Settings → iOS → Build → open Xcode → Archive → TestFlight → App Store

---

## Unity Ads (production)

On the **Gems** component in the Gameplay scene:

| Field | Status |
|-------|--------|
| Android Game Id | `6056953` |
| iOS Game Id | `6056952` |
| Android Ad Unit Id | Set your production placement ID in Inspector |
| iOS Ad Unit Id | Set your production placement ID in Inspector |
| `_testMode` | `false` (set in scene) |

**Production behavior:**
- Rewarded ad completes → player earns seeds
- Ad load/show fails → `SeedPopup` shows “Ad unavailable. Try again later.” (no reward)
- Editor/Development builds still use the fake-ad fallback for local testing

Get IDs from [Unity Ads Dashboard](https://dashboard.unity3d.com/) → Operations → Monetization.

---

## Preflight checks (before every release build)

1. **Missing scripts:** Unity menu → Tools → Grid Game → Find Missing Scripts
2. **FMOD on device:** Music, SFX, score loop work on release build
3. **Device QA:** Follow [DEVICE_QA_CHECKLIST.md](DEVICE_QA_CHECKLIST.md)
4. **Privacy policy:** Host [PRIVACY_POLICY.md](PRIVACY_POLICY.md) and add URL to both stores
5. **Store listing:** Use copy from [STORE_LISTING.md](STORE_LISTING.md)

---

## Store submission checklist

### Google Play
- [ ] Upload signed AAB
- [ ] Privacy policy URL
- [ ] Data safety form (ads, local storage, no account)
- [ ] Content rating questionnaire
- [ ] Screenshots + 512×512 icon + descriptions

### Apple App Store
- [ ] TestFlight soak test
- [ ] App Privacy labels (match Google declarations)
- [ ] Screenshots (6.7", 6.5", 5.5" iPhone minimum)
- [ ] Age rating questionnaire
- [ ] Review notes: rewarded ads for seeds, tutorial, no login required

---

## Code changes included for release

- `InputHelper` — unified touch + mouse input
- `Button` / `UISlider` — work on touch without hover
- `Gems` — production ad failure UX, `_testMode` off
- `GameLog` — verbose logs gated in release builds
