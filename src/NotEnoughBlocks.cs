﻿using System;
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
    private static string VANILLA_PREFIX = "vanilla.";
    public static string ModDirectory;
    private static string BlocksDirectory;
    private static string RelativeTexturesPath;
    private static string RelativeIconsPath;
    private static string RelativeMeshesPath;
    private static string RelativeAudioPath;
    private static List<string> crateTypeKeys = new List<string> ();
    private static List<Recipe> playerCraftingRecipes = new List<Recipe> ();

    [ModLoader.ModCallback (ModLoader.EModCallbackType.OnAssemblyLoaded, "scarabol.notenoughblocks.assemblyload")]
    public static void OnAssemblyLoaded (string path)
    {
      ModDirectory = Path.GetDirectoryName (path);
      BlocksDirectory = Path.Combine (ModDirectory, "blocks");
      // TODO this is really hacky (maybe better in future ModAPI)
      RelativeTexturesPath = new Uri (MultiPath.Combine (Path.GetFullPath ("gamedata"), "textures", "materials", "blocks", "albedo", "dummyfile")).MakeRelativeUri (new Uri (BlocksDirectory)).OriginalString;
      RelativeIconsPath = new Uri (MultiPath.Combine (Path.GetFullPath ("gamedata"), "textures", "icons", "dummyfile")).MakeRelativeUri (new Uri (BlocksDirectory)).OriginalString;
      RelativeMeshesPath = new Uri (MultiPath.Combine (Path.GetFullPath ("gamedata"), "meshes", "dummyfile")).MakeRelativeUri (new Uri (BlocksDirectory)).OriginalString;
      RelativeAudioPath = new Uri (MultiPath.Combine (Path.GetFullPath ("gamedata"), "audio", "dummyfile")).MakeRelativeUri (new Uri (BlocksDirectory)).OriginalString;
      foreach (string fullDirPath in Directory.GetDirectories(BlocksDirectory)) {
        string packageName = Path.GetFileName (fullDirPath);
        if (packageName.Equals ("examples")) {
          continue;
        }
        try {
          Pipliz.Log.Write (string.Format ("Loading localizations from package {0}", packageName));
          ModLocalizationHelper.localize (MultiPath.Combine (BlocksDirectory, packageName, "localization"), MOD_PREFIX + packageName + ".", false);
          Pipliz.Log.Write (string.Format ("Loading audio files from package {0}", packageName));
          ModAudioHelper.IntegrateAudio (MultiPath.Combine (BlocksDirectory, packageName, "audio"), MOD_PREFIX + packageName + ".", MultiPath.Combine (RelativeAudioPath, packageName, "audio"));
        } catch (Exception exception) {
          Pipliz.Log.WriteError (string.Format ("Exception while loading {0} package; {1}", packageName, exception.Message));
        }
      }
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterStartup, "scarabol.notenoughblocks.registercallbacks")]
    public static void AfterStartup ()
    {
      Pipliz.Log.Write ("Loaded NotEnoughBlocks Mod 1.7.1 by Scarabol");
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterAddingBaseTypes, "scarabol.notenoughblocks.addrawtypes")]
    public static void AfterAddingBaseTypes ()
    {
      foreach (string fullDirPath in Directory.GetDirectories(BlocksDirectory)) {
        string packageName = Path.GetFileName (fullDirPath);
        if (packageName.Equals ("examples")) {
          continue;
        }
        Pipliz.Log.Write (string.Format ("Loading blocks from package {0}", packageName));
        string relativeTexturesPath = MultiPath.Combine (RelativeTexturesPath, packageName, "textures");
        Pipliz.Log.Write (string.Format ("relative textures path is {0}", relativeTexturesPath));
        Pipliz.Log.Write (string.Format ("Started loading '{0}' texture mappings...", packageName));
        JSONNode jsonTextureMapping;
        if (Pipliz.JSON.JSON.Deserialize (MultiPath.Combine (BlocksDirectory, packageName, "texturemapping.json"), out jsonTextureMapping, false)) {
          if (jsonTextureMapping.NodeType == NodeType.Object) {
            foreach (KeyValuePair<string,JSONNode> textureEntry in jsonTextureMapping.LoopObject()) {
              try {
                foreach (string textureType in new string[] { "albedo", "normal", "emissive", "height" }) {
                  string textureTypeValue = textureEntry.Value.GetAs<string> (textureType);
                  string realTextureTypeValue = textureTypeValue;
                  if (!textureTypeValue.Equals ("neutral")) {
                    if (textureTypeValue.StartsWith (VANILLA_PREFIX)) {
                      realTextureTypeValue = realTextureTypeValue.Substring (VANILLA_PREFIX.Length);
                    } else {
                      realTextureTypeValue = MultiPath.Combine (relativeTexturesPath, textureType, textureTypeValue);
                    }
                    Pipliz.Log.Write (string.Format ("Rewriting {0} texture path from '{1}' to '{2}'", textureType, textureTypeValue, realTextureTypeValue));
                  }
                  textureEntry.Value.SetAs (textureType, realTextureTypeValue);
                }
                string realkey = MOD_PREFIX + packageName + "." + textureEntry.Key;
                Pipliz.Log.Write (string.Format ("Adding texture mapping for '{0}'", realkey));
                ItemTypesServer.AddTextureMapping (realkey, textureEntry.Value);
              } catch (Exception exception) {
                Pipliz.Log.WriteError (string.Format ("Exception while loading from {0}; {1}", "texturemapping.json", exception.Message));
              }
            }
          } else {
            Pipliz.Log.WriteError (string.Format ("Expected json object in {0}, but got {1} instead", "texturemapping.json", jsonTextureMapping.NodeType));
          }
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
                    realicon = icon.Substring (VANILLA_PREFIX.Length);
                  } else {
                    realicon = MultiPath.Combine (RelativeIconsPath, packageName, "icons", icon);
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
                    realmesh = MultiPath.Combine (RelativeMeshesPath, packageName, "meshes", mesh);
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
                ItemTypes.AddRawType (realkey, typeEntry.Value);
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
    [ModLoader.ModCallbackDependsOn ("pipliz.blocknpcs.loadrecipes")]
    [ModLoader.ModCallbackProvidesFor ("pipliz.apiprovider.registerrecipes")]
    public static void AfterItemTypesDefined ()
    {
      foreach (string fullDirPath in Directory.GetDirectories(BlocksDirectory)) {
        string packageName = Path.GetFileName (fullDirPath);
        if (packageName.Equals ("examples")) {
          continue;
        }
        Pipliz.Log.Write (string.Format ("Started loading '{0}' recipes...", packageName));
        try {
          foreach (string[] jobAndFilename in new string[][] {
            new string[] { "pipliz.crafter", "crafting.json"},
            new string[] { "pipliz.tailor", "tailoring.json" },
            new string[] { "pipliz.grinder", "grinding.json" },
            new string[] { "pipliz.minter", "minting.json" },
            new string[] { "pipliz.merchant", "shopping.json" },
            new string[] { "pipliz.technologist", "technologist.json" },
            new string[] { "pipliz.smelter", "smelting.json" },
            new string[] { "pipliz.baker", "baking.json" }
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
                  if (jobAndFilename [0].Equals ("pipliz.smelter") || jobAndFilename [0].Equals ("pipliz.baker")) {
                    RecipeManager.AddRecipesFueled (jobAndFilename [0], new List<RecipeFueled> () { new RecipeFueled (craftingEntry) });
                  } else {
                    Recipe craftingRecipe = new Recipe (craftingEntry);
                    if (jobAndFilename [1].Equals ("crafting.json")) {
                      playerCraftingRecipes.Add (craftingRecipe);
                    }
                    RecipeManager.AddRecipes (jobAndFilename [0], new List<Recipe> () { craftingRecipe });
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

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterItemTypesServer, "scarabol.notenoughblocks.registertypes")]
    public static void AfterItemTypesServer ()
    {
      foreach (string typekey in crateTypeKeys) {
        ItemTypesServer.RegisterOnAdd (typekey, StockpileBlockTracker.Add);
        ItemTypesServer.RegisterOnRemove (typekey, StockpileBlockTracker.Remove);
      }
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterWorldLoad, "scarabol.notenoughblocks.addplayercrafts")]
    public static void AfterWorldLoad ()
    {
      // add recipes here, otherwise they're inserted before vanilla recipes in player crafts
      RecipePlayer.AllRecipes.AddRange (playerCraftingRecipes);
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.OnTryChangeBlockUser, "scarabol.spawnprotect.trychangeblock")]
    public static bool OnTryChangeBlockUser (ModLoader.OnTryChangeBlockUserData userData)
    {
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
          ServerManager.TryChangeBlock (position, otherIndex, ServerManager.SetBlockFlags.DefaultAudio);
          return true;
        }
      }
      return true;
    }
  }
}
