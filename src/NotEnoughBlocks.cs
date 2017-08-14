using System;
using System.IO;
using System.Collections.Generic;
using Pipliz;
using Pipliz.Chatting;
using Pipliz.JSON;
using Pipliz.Threading;
using Pipliz.APIProvider.Recipes;
using Pipliz.APIProvider.Jobs;
using NPC;

namespace ScarabolMods
{
  [ModLoader.ModManager]
  public static class NotEnoughBlocksModEntries
  {
    private static string MOD_PREFIX = "mods.scarabol.notenoughblocks.";
    public static string ModDirectory;
    private static string BlocksDirectory;

    [ModLoader.ModCallback(ModLoader.EModCallbackType.OnAssemblyLoaded, "scarabol.notenoughblocks.assemblyload")]
    public static void OnAssemblyLoaded(string path)
    {
      ModDirectory = Path.GetDirectoryName(path);
      BlocksDirectory = Path.Combine(ModDirectory, "blocks");
      foreach (string fullDirPath in Directory.GetDirectories(BlocksDirectory)) {
        string packageName = Path.GetFileName(fullDirPath);
        Pipliz.Log.Write(string.Format("Loading translations from package {0}", packageName));
        ModLocalizationHelper.localize(MultiPath.Combine(BlocksDirectory, packageName, "localization"), MOD_PREFIX + packageName + ".", false);
      }
    }

    [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterStartup, "scarabol.notenoughblocks.registercallbacks")]
    public static void AfterStartup()
    {
      Pipliz.Log.Write("Loaded NotEnoughBlocks Mod 1.2 by Scarabol");
    }

    [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterAddingBaseTypes, "scarabol.notenoughblocks.addrawtypes")]
    public static void AfterAddingBaseTypes()
    {
      // TODO this is realy hacky (maybe better in future ModAPI)
      foreach (string fullDirPath in Directory.GetDirectories(BlocksDirectory)) {
        string packageName = Path.GetFileName(fullDirPath);
        Pipliz.Log.Write(string.Format("Loading blocks from package {0}", packageName));
        string relativeTexturesPath = new Uri(MultiPath.Combine(Path.GetFullPath("gamedata"), "textures", "materials", "blocks", "albedo", "dummyfile")).MakeRelativeUri(new Uri(MultiPath.Combine(BlocksDirectory, packageName, "textures"))).OriginalString;
        Pipliz.Log.Write(string.Format("relative textures path is {0}", relativeTexturesPath));
        string relativeMeshesPath = new Uri(MultiPath.Combine(Path.GetFullPath("gamedata"), "meshes", "dummyfile")).MakeRelativeUri(new Uri(MultiPath.Combine(BlocksDirectory, packageName, "meshes"))).OriginalString;
        Pipliz.Log.Write(string.Format("relative meshes path is {0}", relativeMeshesPath));
        Pipliz.Log.Write(string.Format("Started loading '{0}' texture mappings...", packageName));
        JSONNode jsonTextureMapping;
        if (Pipliz.JSON.JSON.Deserialize(MultiPath.Combine(BlocksDirectory, packageName, "texturemapping.json"), out jsonTextureMapping, false)) {
          if (jsonTextureMapping.NodeType == NodeType.Object) {
            foreach (KeyValuePair<string,JSONNode> textureEntry in jsonTextureMapping.LoopObject()) {
              try {
                foreach (string textureType in new string[] { "albedo", "normal", "emissive", "height" }) {
                  string textureTypeValue = textureEntry.Value.GetAs<string>(textureType);
                  string realTextureTypeValue = textureTypeValue;
                  if (!textureTypeValue.Equals("neutral")) {
                    realTextureTypeValue = MultiPath.Combine(relativeTexturesPath, textureType, textureTypeValue);
                  }
                  Pipliz.Log.Write(string.Format("Rewriting {0} texture path from '{1}' to '{2}'", textureType, textureTypeValue, realTextureTypeValue));
                  textureEntry.Value.SetAs(textureType, realTextureTypeValue);
                }
                string realkey = MOD_PREFIX + packageName + "." + textureEntry.Key;
                Pipliz.Log.Write(string.Format("Adding texture mapping for '{0}'", realkey));
                ItemTypesServer.AddTextureMapping(realkey, textureEntry.Value);
              } catch (Exception exception) {
                Pipliz.Log.WriteError(string.Format("Exception while loading from {0}; {1}", "texturemapping.json", exception.Message));
              }
            }
          } else {
            Pipliz.Log.WriteError(string.Format("Expected json object in {0}, but got {1} instead", "texturemapping.json", jsonTextureMapping.NodeType));
          }
        }
        Pipliz.Log.Write(string.Format("Started loading '{0}' types...", packageName));
        JSONNode jsonTypes;
        if (Pipliz.JSON.JSON.Deserialize(MultiPath.Combine(BlocksDirectory, packageName, "types.json"), out jsonTypes, false)) {
          if (jsonTypes.NodeType == NodeType.Object) {
            foreach (KeyValuePair<string,JSONNode> typeEntry in jsonTypes.LoopObject()) {
              try {
                string icon;
                if (typeEntry.Value.TryGetAs("icon", out icon)) {
                  string realicon = MultiPath.Combine(BlocksDirectory, packageName, "icons", icon);
                  Pipliz.Log.Write(string.Format("Rewriting icon path from '{0}' to '{1}'", icon, realicon));
                  typeEntry.Value.SetAs("icon", realicon);
                }
                string mesh;
                if (typeEntry.Value.TryGetAs("mesh", out mesh)) {
                  string realmesh = Path.Combine(relativeMeshesPath, mesh);
                  Pipliz.Log.Write(string.Format("Rewriting mesh path from '{0}' to '{1}'", mesh, realmesh));
                  typeEntry.Value.SetAs("mesh", realmesh);
                }
                string parentType;
                if (typeEntry.Value.TryGetAs("parentType", out parentType)) {
                  string realParentType = MOD_PREFIX + packageName + "." + parentType;
                  Pipliz.Log.Write(string.Format("Rewriting parentType from '{0}' to '{1}'", parentType, realParentType));
                  typeEntry.Value.SetAs("parentType", realParentType);
                }
                foreach (string rotatable in new string[] { "rotatablex+", "rotatablex-", "rotatablez+", "rotatablez-" }) {
                  string key;
                  if (typeEntry.Value.TryGetAs(rotatable, out key)) {
                    string rotatablekey = MOD_PREFIX + packageName + "." + key.Substring(0, key.Length-2) + key.Substring(key.Length-2);
                    Pipliz.Log.Write(string.Format("Rewriting rotatable key '{0}' to '{1}'", key, rotatablekey));
                    typeEntry.Value.SetAs(rotatable, rotatablekey);
                  }
                }
                foreach (string side in new string[] { "sideall", "sidex+", "sidex-", "sidey+", "sidey-", "sidez+", "sidez-" }) {
                  string key;
                  if (typeEntry.Value.TryGetAs(side, out key)) {
                    if (!key.Equals("SELF")) {
                      string sidekey = MOD_PREFIX + packageName + "." + key.Substring(0, key.Length-2) + key.Substring(key.Length-2);
                      Pipliz.Log.Write(string.Format("Rewriting side key from '{0}' to '{1}'", key, sidekey));
                      typeEntry.Value.SetAs(side, sidekey);
                    }
                  }
                }
                string realkey = MOD_PREFIX + packageName + "." + typeEntry.Key;
                Pipliz.Log.Write(string.Format("Adding block type '{0}'", realkey));
                ItemTypes.AddRawType(realkey, typeEntry.Value);
              } catch (Exception exception) {
                Pipliz.Log.WriteError(string.Format("Exception while loading block type {0}; {1}", typeEntry.Key, exception.Message));
              }
            }
          } else {
            Pipliz.Log.WriteError(string.Format("Expected json object in {0}, but got {1} instead", "types.json", jsonTypes.NodeType));
          }
        }
      }
    }

    [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterItemTypesDefined, "scarabol.notenoughblocks.loadrecipes")]
    [ModLoader.ModCallbackProvidesFor("pipliz.apiprovider.registerrecipes")]
    public static void AfterItemTypesDefined()
    {
      foreach (string fullDirPath in Directory.GetDirectories(BlocksDirectory)) {
        string packageName = Path.GetFileName(fullDirPath);
        Pipliz.Log.Write(string.Format("Started loading '{0}' recipes...", packageName));
        try {
          JSONNode jsonCrafting;
          if (Pipliz.JSON.JSON.Deserialize(MultiPath.Combine(BlocksDirectory, packageName, "crafting.json"), out jsonCrafting, false)) {
            if (jsonCrafting.NodeType == NodeType.Array) {
              foreach (JSONNode craftingEntry in jsonCrafting.LoopArray()) {
                JSONNode jsonResults = craftingEntry.GetAs<JSONNode>("results");
                foreach (JSONNode jsonResult in jsonResults.LoopArray()) {
                  string type = jsonResult.GetAs<string>("type");
                  string realtype = MOD_PREFIX + packageName + "." + type;
                  Pipliz.Log.Write(string.Format("Rewriting block recipe result type from '{0}' to '{1}'", type, realtype));
                  jsonResult.SetAs("type", realtype);
                }
                RecipePlayer.AllRecipes.Add(new Recipe(craftingEntry));
              }
            } else {
              Pipliz.Log.WriteError(string.Format("Expected json array in {0}, but got {1} instead", "crafting.json", jsonCrafting.NodeType));
            }
          }
        } catch (Exception exception) {
          Pipliz.Log.WriteError(string.Format("Exception while loading door recipes from {0}; {1}", "crafting.json", exception.Message));
        }
      }
    }
  }
}
