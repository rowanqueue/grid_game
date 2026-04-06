#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

/// <summary>
/// Post-processes the iOS Xcode project to fix Swift/Unity Ads build issues:
/// - Sets ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES on both targets
/// - Sets Team ID on UnityFramework (uses PlayerSettings.iOS.appleDeveloperTeamID)
/// - Adds a Run Script that copies Swift dylibs to root Frameworks, signs them, and removes invalid nested Frameworks
/// </summary>
public static class iOSBuildFix
{
    const int CallbackOrder = 100;

    [PostProcessBuild(CallbackOrder)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS) return;

        string projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        PBXProject project = new PBXProject();
        project.ReadFromFile(projectPath);

        string mainTargetGuid = project.GetUnityMainTargetGuid();
        string frameworkTargetGuid = project.GetUnityFrameworkTargetGuid();

        // 1. Set ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES = YES (Swift dylibs get embedded in UnityFramework)
        project.AddBuildProperty(mainTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
        if (!string.IsNullOrEmpty(frameworkTargetGuid))
            project.AddBuildProperty(frameworkTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");

        // 2. Set Team ID on UnityFramework (fixes startup crash when not set)
        string teamId = PlayerSettings.iOS.appleDeveloperTeamID;
        if (!string.IsNullOrEmpty(teamId))
        {
            if (!string.IsNullOrEmpty(frameworkTargetGuid))
                project.SetTeamId(frameworkTargetGuid, teamId);
            project.SetTeamId(mainTargetGuid, teamId);
        }
        else
        {
            UnityEngine.Debug.LogWarning("[iOSBuildFix] Set your Apple Team ID in Edit > Project Settings > Player > iOS > Other Settings > Apple Developer Team ID");
        }

        // 3. Add Run Script: copy Swift dylibs to root, sign them, remove invalid nested Frameworks
        string shellScript = GetSwiftFixScript();
        project.AddShellScriptBuildPhase(mainTargetGuid, "Fix Swift Frameworks for App Store", "/bin/sh", shellScript);

        project.WriteToFile(projectPath);
        UnityEngine.Debug.Log("[iOSBuildFix] Applied Swift/UnityFramework fixes to Xcode project.");
    }

    static string GetSwiftFixScript()
    {
        // Use (char)35 for '#' to avoid C# preprocessor interpreting it as a directive
        char h = (char)35;
        return h + @" Copy Swift dylibs from UnityFramework into app's root Frameworks folder
APP_PATH=""${TARGET_BUILD_DIR}/${WRAPPER_NAME}""
ROOT_FRAMEWORKS=""${APP_PATH}/Frameworks""
UF_PATH=""${ROOT_FRAMEWORKS}/UnityFramework.framework""
UF_FRAMEWORKS=""${UF_PATH}/Frameworks""

if [ -d ""$UF_PATH"" ]; then
  echo ""Copying Swift dylibs from UnityFramework to root Frameworks""
  mkdir -p ""$ROOT_FRAMEWORKS""
  find ""$UF_PATH"" -maxdepth 4 -name ""libswift*.dylib"" | while IFS= read -r f; do
    cp -f ""$f"" ""$ROOT_FRAMEWORKS/"" && echo ""Copied $(basename ""$f"")""
  done

  " + h + @" Remove the invalid nested Frameworks folder from UnityFramework (Apple rejects it)
  if [ -d ""$UF_FRAMEWORKS"" ]; then
    echo ""Removing invalid UnityFramework.framework/Frameworks""
    rm -rf ""$UF_FRAMEWORKS""
  fi
fi

" + h + @" Code-sign all dylibs in root Frameworks
if [ -d ""$ROOT_FRAMEWORKS"" ] && [ -n ""$EXPANDED_CODE_SIGN_IDENTITY"" ]; then
  echo ""Signing dylibs in Frameworks""
  for dylib in ""$ROOT_FRAMEWORKS""/*.dylib; do
    if [ -f ""$dylib"" ]; then
      codesign --force --sign ""$EXPANDED_CODE_SIGN_IDENTITY"" --preserve-metadata=identifier,entitlements ""$dylib""
      echo ""Signed $(basename ""$dylib"")""
    fi
  done
fi
";
    }
}
#endif
