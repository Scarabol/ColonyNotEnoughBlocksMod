using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Pipliz.JSON;

namespace ScarabolMods
{
  public static class ModLocalizationHelper
  {
    public static void localize(string localePath, string prefix)
    {
      localize(localePath, prefix, true);
    }

    public static void localize(string localePath, string keyprefix, bool verbose)
    {
      try {
        string[] files = Directory.GetFiles(localePath, "types.json", SearchOption.AllDirectories);
        foreach (string filepath in files) {
          try {
            JSONNode jsonFromMod;
            if (Pipliz.JSON.JSON.Deserialize(filepath, out jsonFromMod, false)) {
              string locName = Directory.GetParent(filepath).Name;
              log(string.Format("Found mod localization file for '{0}' localization", locName), verbose);
              string patchPath = MultiPath.Combine("gamedata", "localization", locName, "types.json");
              JSONNode jsonToPatch;
              if (Pipliz.JSON.JSON.Deserialize(patchPath, out jsonToPatch, false)) {
                foreach (KeyValuePair<string, JSONNode> entry in jsonFromMod.LoopObject()) {
                  string realkey = keyprefix + entry.Key;
                  if (!jsonToPatch.HasChild(realkey)) {
                    Pipliz.Log.Write(string.Format("Added translation '{0}' => '{1}' to '{2}'. This will only work AFTER a restart!!!", realkey, entry.Value, locName));
                  }
                  jsonToPatch.SetAs(realkey, entry.Value);
                }
                Pipliz.JSON.JSON.Serialize(patchPath, jsonToPatch);
                log(string.Format("Patched mod localization file '{0}/types.json' into '{1}'", locName, patchPath), verbose);
              } else {
                log(string.Format("Could not deserialize json from '{0}'", patchPath), verbose);
              }
            }
          } catch (Exception exception) {
            log(string.Format("Exception reading localization from {0}; {1}", filepath, exception.Message), verbose);
          }
        }
      } catch (DirectoryNotFoundException exception) {
        log(string.Format("Localization directory not found at {0}", localePath), verbose);
      }
    }

    private static void log(string msg, bool verbose) {
      if (verbose) {
        Pipliz.Log.Write(msg);
      }
    }
  }

  public static class MultiPath
  {
    public static string Combine(params string[] pathParts)
    {
      StringBuilder result = new StringBuilder();
      foreach (string part in pathParts) {
        result.Append(part.TrimEnd('/', '\\')).Append(Path.DirectorySeparatorChar);
      }
      return result.ToString().TrimEnd(Path.DirectorySeparatorChar);
    }
  }
}