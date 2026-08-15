using System;
using System.Collections.Generic;
using System.Linq;
using Timberborn.ModdingTools.Common;
using UnityEditor;
using UnityEngine;

namespace Timberborn.ModdingTools.ModBuilding {
  public static class ModBuilderCli {

    public static void RunCleanBuild() {
      var enabledMods = GetModsToBuild();

      if (!enabledMods.Any()) {
        Debug.LogWarning("No enabled mods found. Nothing to build.");
        return;
      }

      var modBuilderSettings = new ModBuilderSettings(buildCode: true,
                                                      buildWindowsAssetBundle: true,
                                                      buildMacAssetBundle: true,
                                                      deleteFiles: true,
                                                      buildZipArchive: HasZipFlag(),
                                                      compatibilityVersion: string.Empty);

      var buildSucceeded = new ModBuilder(enabledMods, modBuilderSettings).Build();
      if (!buildSucceeded) {
        throw new Exception("Mod build failed.");
      }

      Debug.Log($"Built mods: {string.Join(", ", enabledMods.Select(mod => mod.Name))}");
      Debug.Log("CLI clean build completed successfully.");
    }

    private static ModDefinition[] GetModsToBuild() {
      var requestedModNames = GetRequestedModNames();
      var allMods = new ModFinder().GetAllMods().ToArray();
      if (requestedModNames.Count == 0) {
        var controlsPersistence = new ModBuilderControlsPersistence();
        return allMods.Where(controlsPersistence.IsModEnabled).ToArray();
      }

      var selectedMods = allMods.Where(mod => requestedModNames.Contains(mod.Name)).ToArray();
      var missingMods = requestedModNames.Except(selectedMods.Select(mod => mod.Name),
                                                 StringComparer.OrdinalIgnoreCase);
      foreach (var missingMod in missingMods) {
        Debug.LogWarning($"Requested mod \"{missingMod}\" was not found.");
      }
      return selectedMods;
    }

    private static HashSet<string> GetRequestedModNames() {
      var args = Environment.GetCommandLineArgs();
      var modNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      for (var index = 0; index < args.Length; index++) {
        var argument = args[index];
        if (argument is "-mod" or "--mod") {
          if (index + 1 < args.Length) {
            modNames.Add(args[index + 1]);
            index++;
          }
          continue;
        }

        if (argument is "-mods" or "--mods") {
          if (index + 1 < args.Length) {
            foreach (var modName in args[index + 1].Split(',')) {
              if (!string.IsNullOrWhiteSpace(modName)) {
                modNames.Add(modName.Trim());
              }
            }
            index++;
          }
        }
      }

      return modNames;
    }

    private static bool HasZipFlag() {
      var args = Environment.GetCommandLineArgs();
      return args.Any(argument => argument is "-zip" or "--zip");
    }

  }
}
