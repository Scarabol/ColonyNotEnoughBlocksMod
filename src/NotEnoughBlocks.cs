using System;
using System.IO;
using System.Collections.Generic;
using Pipliz;
using Pipliz.Chatting;
using Pipliz.JSON;
using Pipliz.Threading;
using Pipliz.APIProvider.Jobs;
using NPC;

namespace ScarabolMods
{
  [ModLoader.ModManager]
  public static class NotEnoughBlocksModEntries
  {
    private static string MOD_PREFIX = "mods.scarabol.notenoughblocks.";
    private static string VANILLA_PREFIX = "vanilla.";
    public static string ModDirectory;
    private static string BlocksDirectory;
    private static List<string> crateTypeKeys = new List<string> ();

    [ModLoader.ModCallback (ModLoader.EModCallbackType.OnAssemblyLoaded, "scarabol.notenoughblocks.assemblyload")]
    public static void OnAssemblyLoaded (string path)
    {
      ModDirectory = Path.GetDirectoryName (path);
      BlocksDirectory = Path.Combine (ModDirectory, "blocks");
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterStartup, "scarabol.notenoughblocks.registercallbacks")]
    public static void AfterStartup ()
    {
      Pipliz.Log.Write ("Loaded NotEnoughBlocks Mod 2.3 by Scarabol");
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterSelectedWorld, "scarabol.notenoughblocks.registertexturemappings")]
    [ModLoader.ModCallbackProvidesFor ("pipliz.server.registertexturemappingtextures")]
    public static void AfterSelectedWorld ()
    {
      foreach (string fullDirPath in Directory.GetDirectories(BlocksDirectory)) {
        string packageName = Path.GetFileName (fullDirPath);
        if (packageName.Equals ("examples")) {
          continue;
        }
        Pipliz.Log.Write (string.Format ("Started loading '{0}' texture mappings...", packageName));
        JSONNode jsonTextureMapping;
        if (Pipliz.JSON.JSON.Deserialize (MultiPath.Combine (BlocksDirectory, packageName, "texturemapping.json"), out jsonTextureMapping, false)) {
          if (jsonTextureMapping.NodeType == NodeType.Object) {
            foreach (KeyValuePair<string,JSONNode> textureEntry in jsonTextureMapping.LoopObject()) {
              try {
                string albedoPath = null;
                string normalPath = null;
                string emissivePath = null;
                string heightPath = null;
                foreach (string textureType in new string[] { "albedo", "normal", "emissive", "height" }) {
                  string textureTypeValue = textureEntry.Value.GetAs<string> (textureType);
                  string realTextureTypeValue = textureTypeValue;
                  if (!textureTypeValue.Equals ("neutral")) {
                    if (textureTypeValue.StartsWith (VANILLA_PREFIX)) {
                      realTextureTypeValue = realTextureTypeValue.Substring (VANILLA_PREFIX.Length);
                    } else {
                      realTextureTypeValue = MultiPath.Combine (BlocksDirectory, packageName, "textures", textureType, textureTypeValue + ".png");
                      if (textureType.Equals ("albedo")) {
                        albedoPath = realTextureTypeValue;
                      } else if (textureType.Equals ("normal")) {
                        normalPath = realTextureTypeValue;
                      } else if (textureType.Equals ("emissive")) {
                        emissivePath = realTextureTypeValue;
                      } else if (textureType.Equals ("height")) {
                        heightPath = realTextureTypeValue;
                      }
                    }
                    Pipliz.Log.Write (string.Format ("Rewriting {0} texture path from '{1}' to '{2}'", textureType, textureTypeValue, realTextureTypeValue));
                  }
                  textureEntry.Value.SetAs (textureType, realTextureTypeValue);
                }
                var textureMapping = new ItemTypesServer.TextureMapping (textureEntry.Value);
                if (albedoPath != null) {
                  textureMapping.AlbedoPath = albedoPath;
                }
                if (normalPath != null) {
                  textureMapping.NormalPath = normalPath;
                }
                if (emissivePath != null) {
                  textureMapping.EmissivePath = emissivePath;
                }
                if (heightPath != null) {
                  textureMapping.HeightPath = heightPath;
                }
                string realkey = MOD_PREFIX + packageName + "." + textureEntry.Key;
                Pipliz.Log.Write (string.Format ("Adding texture mapping for '{0}'", realkey));
                ItemTypesServer.SetTextureMapping (realkey, textureMapping);
              } catch (Exception exception) {
                Pipliz.Log.WriteError (string.Format ("Exception while loading from {0}; {1}", "texturemapping.json", exception.Message));
              }
            }
          } else {
            Pipliz.Log.WriteError (string.Format ("Expected json object in {0}, but got {1} instead", "texturemapping.json", jsonTextureMapping.NodeType));
          }
        }
      }
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterAddingBaseTypes, "scarabol.notenoughblocks.addrawtypes")]
    public static void AfterAddingBaseTypes (Dictionary<string, ItemTypesServer.ItemTypeRaw> itemTypes)
    {
      foreach (string fullDirPath in Directory.GetDirectories(BlocksDirectory)) {
        string packageName = Path.GetFileName (fullDirPath);
        if (packageName.Equals ("examples")) {
          continue;
        }
        Pipliz.Log.Write (string.Format ("Started loading '{0}' types...", packageName));
        JSONNode jsonTypes;
        if (Pipliz.JSON.JSON.Deserialize (MultiPath.Combine (BlocksDirectory, packageName, "types.json"), out jsonTypes, false)) {
          if (jsonTypes.NodeType == NodeType.Object) {
            foreach (KeyValuePair<string,JSONNode> typeEntry in jsonTypes.LoopObject()) {
              try {
                string icon;
                if (typeEntry.Value.TryGetAs ("icon", out icon)) {
                  string realicon;
                  if (icon.StartsWith (VANILLA_PREFIX)) {
                    realicon = MultiPath.Combine ("gamedata", "textures", "icons", icon.Substring (VANILLA_PREFIX.Length));
                  } else {
                    realicon = MultiPath.Combine (BlocksDirectory, packageName, "icons", icon);
                  }
                  Pipliz.Log.Write (string.Format ("Rewriting icon path from '{0}' to '{1}'", icon, realicon));
                  typeEntry.Value.SetAs ("icon", realicon);
                }
                string mesh;
                if (typeEntry.Value.TryGetAs ("mesh", out mesh)) {
                  string realmesh;
                  if (mesh.StartsWith (VANILLA_PREFIX)) {
                    realmesh = mesh.Substring (VANILLA_PREFIX.Length);
                  } else {
                    realmesh = MultiPath.Combine (BlocksDirectory, packageName, "meshes", mesh);
                  }
                  Pipliz.Log.Write (string.Format ("Rewriting mesh path from '{0}' to '{1}'", mesh, realmesh));
                  typeEntry.Value.SetAs ("mesh", realmesh);
                }
                string parentType;
                if (typeEntry.Value.TryGetAs ("parentType", out parentType)) {
                  string realParentType;
                  if (parentType.StartsWith (VANILLA_PREFIX)) {
                    realParentType = parentType.Substring (VANILLA_PREFIX.Length);
                  } else {
                    realParentType = MOD_PREFIX + packageName + "." + parentType;
                  }
                  Pipliz.Log.Write (string.Format ("Rewriting parentType from '{0}' to '{1}'", parentType, realParentType));
                  typeEntry.Value.SetAs ("parentType", realParentType);
                }
                foreach (string rotatable in new string[] { "rotatablex+", "rotatablex-", "rotatablez+", "rotatablez-" }) {
                  string key;
                  if (typeEntry.Value.TryGetAs (rotatable, out key)) {
                    string rotatablekey;
                    if (key.StartsWith (VANILLA_PREFIX)) {
                      rotatablekey = key.Substring (VANILLA_PREFIX.Length);
                    } else {
                      rotatablekey = MOD_PREFIX + packageName + "." + key;
                    }
                    Pipliz.Log.Write (string.Format ("Rewriting rotatable key '{0}' to '{1}'", key, rotatablekey));
                    typeEntry.Value.SetAs (rotatable, rotatablekey);
                  }
                }
                foreach (string side in new string[] { "sideall", "sidex+", "sidex-", "sidey+", "sidey-", "sidez+", "sidez-" }) {
                  string key;
                  if (typeEntry.Value.TryGetAs (side, out key)) {
                    if (!key.Equals ("SELF")) {
                      string sidekey;
                      if (key.StartsWith (VANILLA_PREFIX)) {
                        sidekey = key.Substring (VANILLA_PREFIX.Length);
                      } else {
                        sidekey = MOD_PREFIX + packageName + "." + key;
                      }
                      Pipliz.Log.Write (string.Format ("Rewriting side key from '{0}' to '{1}'", key, sidekey));
                      typeEntry.Value.SetAs (side, sidekey);
                    }
                  }
                }
                string onRemoveType;
                if (typeEntry.Value.TryGetAs ("onRemoveType", out onRemoveType)) {
                  string realOnRemoveType;
                  if (onRemoveType.StartsWith (VANILLA_PREFIX)) {
                    realOnRemoveType = onRemoveType.Substring (VANILLA_PREFIX.Length);
                  } else {
                    realOnRemoveType = MOD_PREFIX + packageName + "." + onRemoveType;
                  }
                  Pipliz.Log.Write (string.Format ("Rewriting onRemoveType from '{0}' to '{1}'", onRemoveType, realOnRemoveType));
                  typeEntry.Value.SetAs ("onRemoveType", realOnRemoveType);
                }
                string onPlaceAudio;
                if (typeEntry.Value.TryGetAs ("onPlaceAudio", out onPlaceAudio)) {
                  string realOnPlaceAudio;
                  if (onPlaceAudio.StartsWith (VANILLA_PREFIX)) {
                    realOnPlaceAudio = onPlaceAudio.Substring (VANILLA_PREFIX.Length);
                  } else {
                    realOnPlaceAudio = MOD_PREFIX + packageName + "." + onPlaceAudio;
                  }
                  Pipliz.Log.Write (string.Format ("Rewriting onPlaceAudio from '{0}' to '{1}'", onPlaceAudio, realOnPlaceAudio));
                  typeEntry.Value.SetAs ("onPlaceAudio", realOnPlaceAudio);
                }
                string onRemoveAudio;
                if (typeEntry.Value.TryGetAs ("onRemoveAudio", out onRemoveAudio)) {
                  string realOnRemoveAudio;
                  if (onRemoveAudio.StartsWith (VANILLA_PREFIX)) {
                    realOnRemoveAudio = onRemoveAudio.Substring (VANILLA_PREFIX.Length);
                  } else {
                    realOnRemoveAudio = MOD_PREFIX + packageName + "." + onRemoveAudio;
                  }
                  Pipliz.Log.Write (string.Format ("Rewriting onRemoveAudio from '{0}' to '{1}'", onRemoveAudio, realOnRemoveAudio));
                  typeEntry.Value.SetAs ("onRemoveAudio", realOnRemoveAudio);
                }
                string realkey = MOD_PREFIX + packageName + "." + typeEntry.Key;
                bool isCrate;
                if (typeEntry.Value.TryGetAs ("isCrate", out isCrate) && isCrate) {
                  Pipliz.Log.Write (string.Format ("Adding crate type '{0}'", realkey));
                  crateTypeKeys.Add (realkey);
                } else {
                  Pipliz.Log.Write (string.Format ("Adding block type '{0}'", realkey));
                }
                itemTypes.Add (realkey, new ItemTypesServer.ItemTypeRaw (realkey, typeEntry.Value));
              } catch (Exception exception) {
                Pipliz.Log.WriteException (exception);
                Pipliz.Log.WriteError (string.Format ("Exception while loading block type {0}; {1}", typeEntry.Key, exception.Message));
              }
            }
          } else {
            Pipliz.Log.WriteError (string.Format ("Expected json object in {0}, but got {1} instead", "types.json", jsonTypes.NodeType));
          }
        }
      }
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterItemTypesDefined, "scarabol.notenoughblocks.loadrecipes")]
    [ModLoader.ModCallbackProvidesFor ("pipliz.apiprovider.registerrecipes")]
    public static void LoadRecipes ()
    {
      foreach (string fullDirPath in Directory.GetDirectories(BlocksDirectory)) {
        string packageName = Path.GetFileName (fullDirPath);
        if (packageName.Equals ("examples")) {
          continue;
        }
        Pipliz.Log.Write (string.Format ("Started loading '{0}' recipes...", packageName));
        try {
          foreach (string[] jobAndFilename in new string[][] {
            new string[] { "workbench", "crafting.json"},
            new string[] { "tailorshop", "tailoring.json" },
            new string[] { "grindstone", "grinding.json" },
            new string[] { "mint", "minting.json" },
            new string[] { "shop", "shopping.json" },
            new string[] { "technologisttable", "technologist.json" },
            new string[] { "furnace", "smelting.json" },
            new string[] { "oven", "baking.json" }
          }) {
            JSONNode jsonRecipes;
            if (Pipliz.JSON.JSON.Deserialize (MultiPath.Combine (BlocksDirectory, packageName, jobAndFilename [1]), out jsonRecipes, false)) {
              if (jsonRecipes.NodeType == NodeType.Array) {
                foreach (JSONNode craftingEntry in jsonRecipes.LoopArray()) {
                  foreach (string recipePart in new string[] { "results", "requires" }) {
                    JSONNode jsonRecipeParts = craftingEntry.GetAs<JSONNode> (recipePart);
                    foreach (JSONNode jsonRecipePart in jsonRecipeParts.LoopArray()) {
                      string type = jsonRecipePart.GetAs<string> ("type");
                      string realtype;
                      if (type.StartsWith (VANILLA_PREFIX)) {
                        realtype = type.Substring (VANILLA_PREFIX.Length);
                      } else {
                        realtype = MOD_PREFIX + packageName + "." + type;
                      }
                      Pipliz.Log.Write (string.Format ("Rewriting block recipe type from '{0}' to '{1}'", type, realtype));
                      jsonRecipePart.SetAs ("type", realtype);
                    }
                  }
                  Recipe craftingRecipe = new Recipe (craftingEntry);
                  RecipeStorage.AddRecipe (craftingRecipe);
                  RecipeStorage.AddBlockToRecipeMapping (jobAndFilename [0], craftingRecipe.Name);
                  if (jobAndFilename [1].Equals ("crafting.json")) {
                    RecipePlayer.AddDefaultRecipe (craftingRecipe);
                  }
                }
              } else {
                Pipliz.Log.WriteError (string.Format ("Expected json array in {0}, but got {1} instead", jobAndFilename [1], jsonRecipes.NodeType));
              }
            }
          }
        } catch (Exception exception) {
          Pipliz.Log.WriteError (string.Format ("Exception while loading recipes from {0}; {1}", packageName, exception.Message));
        }
      }
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterItemTypesDefined, "scarabol.notenoughblocks.registertypes")]
    public static void AfterItemTypesDefined ()
    {
      foreach (string typekey in crateTypeKeys) {
        ItemTypesServer.RegisterOnAdd (typekey, StockpileBlockTracker.Add);
        ItemTypesServer.RegisterOnRemove (typekey, StockpileBlockTracker.Remove);
      }
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterWorldLoad, "scarabol.notenoughblocks.afterworldload")]
    [ModLoader.ModCallbackDependsOn ("pipliz.server.localization.waitforloading")]
    [ModLoader.ModCallbackProvidesFor ("pipliz.server.localization.convert")]
    public static void AfterWorldLoad ()
    {
      foreach (string fullDirPath in Directory.GetDirectories(BlocksDirectory)) {
        string packageName = Path.GetFileName (fullDirPath);
        if (packageName.Equals ("examples")) {
          continue;
        }
        try {
          Pipliz.Log.Write (string.Format ("Loading localizations from package {0}", packageName));
          ModLocalizationHelper.localize (MultiPath.Combine (BlocksDirectory, packageName, "localization"), MOD_PREFIX + packageName + ".");
        } catch (Exception exception) {
          Pipliz.Log.WriteError (string.Format ("Exception while loading {0} package; {1}", packageName, exception.Message));
        }
      }
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.OnTryChangeBlockUser, "scarabol.notenoughblocks.trychangeblock")]
    public static bool OnTryChangeBlockUser (ModLoader.OnTryChangeBlockUserData userData)
    {
      if (!userData.isPrimaryAction) {
        VoxelSide side = userData.voxelHitSide;
        string suffix;
        if (side == VoxelSide.xPlus) {
          suffix = "right";
        } else if (side == VoxelSide.xMin) {
          suffix = "left";
        } else if (side == VoxelSide.yPlus) {
          suffix = "bottom";
        } else if (side == VoxelSide.yMin) {
          suffix = "top";
        } else if (side == VoxelSide.zPlus) {
          suffix = "front";
        } else if (side == VoxelSide.zMin) {
          suffix = "back";
        } else {
          return true;
        }
        ushort newType = userData.typeToBuild;
        string typename;
        if (newType != userData.typeTillNow && ItemTypes.IndexLookup.TryGetName (newType, out typename)) {
          string otherTypename = typename + suffix;
          ushort otherIndex;
          if (ItemTypes.IndexLookup.TryGetIndex (otherTypename, out otherIndex)) {
            Vector3Int position = userData.VoxelToChange;
            ThreadManager.InvokeOnMainThread (delegate () {
              ServerManager.TryChangeBlock (position, otherIndex, ServerManager.SetBlockFlags.DefaultAudio);
            }, 0.1f);
          }
        }
      }
      return true;
    }
  }
}
