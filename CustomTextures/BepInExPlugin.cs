﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CustomTextures
{
    [BepInPlugin("aedenthorn.CustomTextures", "Custom Textures", "1.3.1")]
    public partial class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<float> m_range;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> dumpSceneTextures;
        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<int> nexusID;
        public static Dictionary<string, string> customTextures = new Dictionary<string, string>();
        public static Dictionary<string, Texture2D> cachedTextures = new Dictionary<string, Texture2D>();
        public static List<string> outputDump = new List<string>();

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            hotKey = Config.Bind<string>("General", "HotKey", "page down", "Key to reload textures");
            dumpSceneTextures = Config.Bind<bool>("General", "DumpSceneTextures", false, "Dump scene textures to BepInEx/plugins/CustomTextures/scene_dump.txt");
            nexusID = Config.Bind<int>("General", "NexusID", 48, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            LoadCustomTextures();

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private void Update()
        {
            if (ZNetScene.instance != null && CheckKeyDown(hotKey.Value))
            {
                LoadCustomTextures();
                Dbgl($"Pressed reload key.");

                GameObject root = (GameObject)typeof(ZNetScene).GetField("m_netSceneRoot", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ZNetScene.instance);

                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);

                List<GameObject> gos = new List<GameObject>();
                foreach (Transform t in transforms)
                {
                    if(t.parent == root.transform)
                        gos.Add(t.gameObject);
                }

                foreach (ClutterSystem.Clutter clutter in ClutterSystem.instance.m_clutter)
                {
                    gos.Add(clutter.m_prefab);
                }
                LoadSceneTextures(gos.ToArray());
                LoadSceneTextures(((Dictionary<int, GameObject>)typeof(ZNetScene).GetField("m_namedPrefabs", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ZNetScene.instance )).Values.ToArray());

                foreach (Player player in Player.GetAllPlayers())
                {
                    VisEquipment ve = (VisEquipment)typeof(Humanoid).GetField("m_visEquipment", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(player);
                    if(ve != null)
                    {
                        SetEquipmentTexture((string)typeof(VisEquipment).GetField("m_leftItem", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ve), (GameObject)typeof(VisEquipment).GetField("m_leftItemInstance", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ve));
                        SetEquipmentTexture((string)typeof(VisEquipment).GetField("m_rightItem", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ve), (GameObject)typeof(VisEquipment).GetField("m_rightItemInstance", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ve));
                        SetEquipmentTexture((string)typeof(VisEquipment).GetField("m_helmetItem", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ve), (GameObject)typeof(VisEquipment).GetField("m_helmetItemInstance", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ve));
                        SetEquipmentTexture((string)typeof(VisEquipment).GetField("m_leftBackItem", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ve), (GameObject)typeof(VisEquipment).GetField("m_leftBackItemInstance", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ve));
                        SetEquipmentTexture((string)typeof(VisEquipment).GetField("m_rightBackItem", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ve), (GameObject)typeof(VisEquipment).GetField("m_rightBackItemInstance", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ve));
                        SetEquipmentListTexture((string)typeof(VisEquipment).GetField("m_shoulderItem", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ve), (List<GameObject>)typeof(VisEquipment).GetField("m_shoulderItemInstances", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ve));
                        SetEquipmentListTexture((string)typeof(VisEquipment).GetField("m_utilityItem", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ve), (List<GameObject>)typeof(VisEquipment).GetField("m_utilityItemInstances", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ve));
                        SetBodyEquipmentTexture((string)typeof(VisEquipment).GetField("m_legItem", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ve), ve.m_bodyModel, (List<GameObject>)typeof(VisEquipment).GetField("m_legItemInstances", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ve), "_Legs");
                        SetBodyEquipmentTexture((string)typeof(VisEquipment).GetField("m_chestItem", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ve), ve.m_bodyModel, (List<GameObject>)typeof(VisEquipment).GetField("m_chestItemInstances", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ve), "_Chest");
                    }
                }

            }

        }
        private static bool CheckKeyDown(string value)
        {
            try
            {
                return Input.GetKeyDown(value.ToLower());
            }
            catch
            {
                return false;
            }
        }

        private static void LoadCustomTextures()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),"CustomTextures");

            if (!Directory.Exists(path))
            {
                Dbgl($"Directory {path} does not exist! Creating.");
                Directory.CreateDirectory(path);
                return;
            }

            customTextures.Clear();
            cachedTextures.Clear();

            foreach (string file in Directory.GetFiles(path, "*.png", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(file);
                Dbgl($"adding {fileName} custom texture.");

                string id = Path.GetFileNameWithoutExtension(fileName);
                
                customTextures[id] = file;
            }
        }

        private static bool HasCustomTexture(string id)
        {
            return (customTextures.ContainsKey(id) || customTextures.Any(p => p.Key.StartsWith(id)));
        }

        private static Texture2D LoadTexture(string id, Texture vanilla)
        {
            if (cachedTextures.ContainsKey(id))
            {
                //Dbgl($"loading cached texture for {id}");
                return cachedTextures[id];
            }

            Texture2D tex = new Texture2D(vanilla.width, vanilla.height, TextureFormat.RGBA32, true, id.EndsWith("_bump")); 
            tex.filterMode = FilterMode.Point;
            if (customTextures.ContainsKey(id))
            {
                //Dbgl($"loading custom texture file for {id}");
                byte[] imageData = File.ReadAllBytes(customTextures[id]);
                tex.LoadImage(imageData);
            }
            else
            {
                //Dbgl($"texture {id} has no custom texture, using vanilla");

                // https://support.unity.com/hc/en-us/articles/206486626-How-can-I-get-pixels-from-unreadable-textures-

                // Create a temporary RenderTexture of the same size as the texture
                RenderTexture tmp = RenderTexture.GetTemporary(
                                    vanilla.width,
                                    vanilla.height,
                                    0,
                                    RenderTextureFormat.Default,
                                    RenderTextureReadWrite.Linear);

                // Blit the pixels on texture to the RenderTexture
                Graphics.Blit(vanilla, tmp);

                // Backup the currently set RenderTexture
                RenderTexture previous = RenderTexture.active;

                // Set the current RenderTexture to the temporary one we created
                RenderTexture.active = tmp;

                // Create a new readable Texture2D to copy the pixels to it
                Texture2D myTexture2D = new Texture2D(vanilla.width, vanilla.height);

                // Copy the pixels from the RenderTexture to the new Texture
                myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                myTexture2D.Apply();

                // Reset the active RenderTexture
                RenderTexture.active = previous;

                // Release the temporary RenderTexture
                RenderTexture.ReleaseTemporary(tmp);

                // "myTexture2D" now has the same pixels from "texture" and it's readable.


                tex.SetPixels((myTexture2D).GetPixels());
            }
            var layers = customTextures.Where(p => p.Key.StartsWith(id) && p.Key != id);
            if (layers.Any())
            {
                //Dbgl($"texture {id} has {layers.Count()} layers");
                foreach(var layer in layers)
                {

                    Texture2D layerTex = new Texture2D(2, 2, TextureFormat.RGBA32, true, id.EndsWith("_bump"));
                    layerTex.filterMode = FilterMode.Point;
                    byte[] layerData = File.ReadAllBytes(customTextures[layer.Key]);
                    layerTex.LoadImage(layerData);

                    //8x5, 2x2

                    float scaleX = tex.width / (float)layerTex.width; // 8 / 2 = 4 or 2 / 8 = 0.25
                    float scaleY = tex.height / (float)layerTex.height; // 5 / 2 = 2.5 or 2 / 5 = 0.4

                    int width = layerTex.width;
                    int height = layerTex.width;

                    if (scaleX * scaleY < 1) // layer is bigger
                    {
                        width = tex.width;
                        height = tex.height;
                    }

                    Dbgl($"adding layer {layer.Key} to {id}, scale diff {scaleX},{scaleY}");


                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            if(scaleX == 1 && scaleY == 1)
                            {
                                Color texColor = tex.GetPixel(x, y);
                                Color layerColor = layerTex.GetPixel(x, y);

                                Color final_color = Color.Lerp(texColor, layerColor, layerColor.a / 1.0f);

                                tex.SetPixel(x, y, final_color);

                            }
                            else if (scaleX * scaleY < 1) // layer is bigger
                            {

                                for (int i = 0; i < (int)(1 / scaleX); i++) // < 4, so 0, 1, 2, 3 become layer x = 0
                                {
                                    for (int j = 0; j < (int)(1 / scaleY); j++) // < 2, so 0, 1 become layer y = 0
                                    {
                                        Color texColor = tex.GetPixel(x, y);
                                        Color layerColor = layerTex.GetPixel((x * (int)(1 / scaleX)) + i, (y * (int)(1 / scaleY)) + j);
                                        
                                        if (layerColor == Color.clear)
                                            continue;

                                        Color final_color = Color.Lerp(texColor, layerColor, layerColor.a / 1.0f);
                                        final_color.a = 1f;
                                        
                                        tex.SetPixel(x, y, final_color);
                                    }
                                }
                            }
                            else // tex is bigger, multiply layer
                            {
                                for(int i = 0; i < (int)scaleX; i++) // < 4, so 0, 1, 2, 3 become layer x = 0    2 so 0,1
                                {
                                    for (int j = 0; j < (int)scaleY; j++) // < 2, so 0, 1 become layer y = 0    2 so 0,1
                                    {
                                        Color texColor = tex.GetPixel((x * (int)scaleX) + i, (y * (int)scaleY) + j);
                                        Color layerColor = layerTex.GetPixel(x, y);
                                        if (layerColor == Color.clear)
                                            continue;

                                        Color final_color = Color.Lerp(texColor, layerColor, layerColor.a / 1.0f);
                                        final_color.a = 1f;

                                        tex.SetPixel((x * (int)scaleX) + i, (y * (int)scaleY) + j, final_color);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            tex.Apply();

            cachedTextures[id] = tex;
            return tex;
        }

        private static void LoadSceneTextures(GameObject[] gos)
        {

            Dbgl($"loading {gos.Length} scene textures");

            foreach (GameObject gameObject in gos)
            {
                
                if (gameObject.name == "_NetSceneRoot")
                    continue;

                LoadOneTexture(gameObject, gameObject.name, "object");

            }

        }

        private static void LoadOneTexture(GameObject gameObject, string thingName, string prefix, string fallbackTexture = null)
        {
            if (thingName.Contains("_frac"))
            {
                outputDump.Add($"skipping _frac {thingName}");
                return;
            }
            List<string> logDump = new List<string>();

            //Dbgl($"loading textures for { gameObject.name}");
            MeshRenderer[] mrs = gameObject.GetComponentsInChildren<MeshRenderer>(true);
            SkinnedMeshRenderer[] smrs = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            InstanceRenderer[] irs = gameObject.GetComponentsInChildren<InstanceRenderer>(true);


            if (mrs?.Any() == true)
            {
                outputDump.Add($"{prefix} {thingName} has {mrs.Length} MeshRenderers:");
                foreach(MeshRenderer r in mrs)
                {
                    if (r == null)
                    {
                        outputDump.Add($"\tnull");
                        continue;
                    }

                    outputDump.Add($"\tMeshRenderer name: {r.name}");
                    if (r.materials == null || !r.materials.Any())
                    {
                        outputDump.Add($"\t\tsmr {r.name} has no materials");
                        continue;
                    }

                    foreach (Material m in r.materials)
                    {
                        try
                        {
                            ReplaceMaterialTextures(thingName, prefix, "MeshRenderer", r.name, m, logDump);
                        }
                        catch (Exception ex)
                        {
                            //Dbgl($"Error loading {mr.name}:\r\nindex: {idx}\r\n{ex}");
                        }
                    }

                }
            }
            if (smrs?.Any() == true)
            {
                outputDump.Add($"{prefix} {thingName} has {smrs.Length} SkinnedMeshRenderers:");
                foreach (SkinnedMeshRenderer r in smrs)
                {
                    if (r == null)
                    {
                        outputDump.Add($"\tnull");
                        continue;
                    }

                    outputDump.Add($"\tSkinnedMeshRenderer name: {r.name}");
                    if (r.materials == null || !r.materials.Any())
                    {
                        outputDump.Add($"\t\tsmr {r.name} has no materials");
                        continue;
                    }

                    foreach (Material m in r.materials)
                    {
                        try
                        {
                            ReplaceMaterialTextures(thingName, prefix, "SkinnedMeshRenderer", r.name, m, logDump);
                        }
                        catch (Exception ex)
                        {
                            logDump.Add($"Error loading {r.name}:\r\n{ex}");
                        }
                    }

                }
            }
            if (irs?.Any() == true)
            {
                outputDump.Add($"{prefix} {thingName} has {irs.Length} InstanceRenderer:");
                foreach (InstanceRenderer r in irs)
                {
                    if (r == null)
                    {
                        outputDump.Add($"\tnull");
                        continue;
                    }

                    outputDump.Add($"\tInstanceRenderer name: {r.name}");
                    if (r.m_material == null)
                    {
                        outputDump.Add($"\t\tir {r.name} has no material");
                        continue;
                    }

                    try
                    {
                        ReplaceMaterialTextures(thingName, prefix, "InstanceRenderer", r.name, r.m_material, logDump);
                    }
                    catch (Exception ex)
                    {
                        logDump.Add($"Error loading {r.name}:\r\n{ex}");
                    }
                }
            }
            if (logDump.Any())
                Dbgl(string.Join("\n", logDump));
        }

        private static void ReplaceMaterialTextures(string thingName, string prefix, string rendererType, string rendererName, Material m, List<string> logDump)
        {
            /*
            outputDump.Add("\t\tproperties:");
            foreach (string property in m.GetTexturePropertyNames())
            {
                outputDump.Add($"\t\t\t{property}");
            }
            */

            if (!m.HasProperty("_MainTex") || m.GetTexture("_MainTex") == null)
            {
                outputDump.Add($"\t\t{rendererType} {rendererName} material {m.name} main texture is null");
                //continue;
            }
            outputDump.Add($"\t\ttexture name: {(m.HasProperty("_MainTex") ? m.GetTexture("_MainTex")?.name : "null")}");
            string name = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex")?.name : null;
            if (name == null)
                name = thingName;

            if (HasCustomTexture($"{prefix}_{thingName}_texture"))
            {
                logDump.Add($"{prefix} {thingName}, {rendererType} {rendererName}, material {m.name}, texture {name}, using {prefix}_{thingName}_texture custom texture.");
                m.SetTexture("_MainTex", LoadTexture($"{prefix}_{thingName}_texture", m.mainTexture));
                if(m.HasProperty("_ChestTex"))
                    m.SetTexture("_ChestTex", LoadTexture($"{prefix}_{thingName}_texture", m.mainTexture));
                if(m.HasProperty("_LegsTex"))
                    m.SetTexture("_LegsTex", LoadTexture($"{prefix}_{thingName}_texture", m.mainTexture));
                m.mainTexture.name = name;
                m.color = Color.white;
            }
            else if (HasCustomTexture($"{prefix}mesh_{thingName}_{rendererName}_texture"))
            {
                logDump.Add($"object {thingName}, {rendererType} {rendererName}, material {m.name}, texture {name}, using {prefix}mesh_{thingName}_{rendererName}_texture custom texture.");
                m.SetTexture("_MainTex", LoadTexture($"{prefix}mesh_{thingName}_{name}_texture", m.mainTexture));
                m.mainTexture.name = name;
                m.color = Color.white;
            }
            else if (HasCustomTexture($"{prefix}texture_{thingName}_{name}_texture"))
            {
                logDump.Add($"object {thingName}, {rendererType} {rendererName}, material {m.name}, texture {name}, using {prefix}texture_{thingName}_{name}_texture custom texture.");
                m.SetTexture("_MainTex", LoadTexture($"{prefix}texture_{thingName}_{name}_texture", m.mainTexture));
                m.mainTexture.name = name;
                m.color = Color.white;
            }
            else if (HasCustomTexture($"texture_{name}_texture"))
            {
                logDump.Add($"{prefix} {thingName}, {rendererType} {rendererName}, material {m.name}, texture {name}, using texture_{name}_texture custom texture.");
                m.SetTexture("_MainTex", LoadTexture($"texture_{name}_texture", m.mainTexture));
                m.mainTexture.name = name;
                m.color = Color.white;

            }

            if (HasCustomTexture($"{prefix}_{thingName}_bump"))
            {
                logDump.Add($"{prefix} {thingName}, SkinnedMeshRenderer {rendererName}, material {m.name}, texture {name}, using {prefix}_{thingName}_bump custom bump map.");
                m.SetTexture("_BumpMap", LoadTexture($"{prefix}_{thingName}_bump", m.GetTexture("_BumpMap")));
            }
            else if (HasCustomTexture($"{prefix}mesh_{thingName}_{rendererName}_bump"))
            {
                logDump.Add($"object {thingName}, SkinnedMeshRenderer {rendererName}, material {m.name}, texture {name}, using {prefix}mesh_{thingName}_{rendererName}_bump custom bump map.");
                m.SetTexture("_BumpMap", LoadTexture($"{prefix}mesh_{thingName}_{rendererName}_bump", m.GetTexture("_BumpMap")));
            }
            else if (HasCustomTexture($"{prefix}texture_{thingName}_{name}_bump"))
            {
                logDump.Add($"object {thingName}, SkinnedMeshRenderer {rendererName}, material {m.name}, texture {name}, using {prefix}texture_{thingName}_{name}_bump custom bump map.");
                m.SetTexture("_BumpMap", LoadTexture($"{prefix}texture_{thingName}_{name}_bump", m.GetTexture("_BumpMap")));
            }
            else if (HasCustomTexture($"texture_{name}_bump"))
            {
                logDump.Add($"object {thingName}, SkinnedMeshRenderer {rendererName}, material {m.name}, texture {name}, using texture_{name}_bump custom bump map.");
                m.SetTexture("_BumpMap", LoadTexture($"texture_{name}_bump", m.GetTexture("_BumpMap")));
            }

            if (HasCustomTexture($"{prefix}_{thingName}_style"))
            {
                logDump.Add($"{prefix} {thingName}, SkinnedMeshRenderer {rendererName}, material {m.name}, texture {name}, using {prefix}_{thingName}_style style texture.");
                m.SetTexture("_StyleTex", LoadTexture($"{prefix}_{thingName}_style", m.GetTexture("_StyleTex")));
            }
            else if (HasCustomTexture($"{prefix}mesh_{thingName}_{rendererName}_style"))
            {
                logDump.Add($"object {thingName}, SkinnedMeshRenderer {rendererName}, material {m.name}, texture {name}, using {prefix}mesh_{thingName}_{rendererName}_style style texture.");
                m.SetTexture("_StyleTex", LoadTexture($"{prefix}mesh_{thingName}_{rendererName}_style", m.GetTexture("_StyleTex")));
            }
            else if (HasCustomTexture($"{prefix}texture_{thingName}_{name}_style"))
            {
                logDump.Add($"object {thingName}, SkinnedMeshRenderer {rendererName}, material {m.name}, texture {name}, using {prefix}texture_{thingName}_{name}_style style texture.");
                m.SetTexture("_StyleTex", LoadTexture($"{prefix}texture_{thingName}_{name}_style", m.GetTexture("_StyleTex")));
            }
            else if (HasCustomTexture($"texture_{name}_style"))
            {
                logDump.Add($"object {thingName}, SkinnedMeshRenderer {rendererName}, material {m.name}, texture {name}, using texture_{name}_style style texture.");
                m.SetTexture("_StyleTex", LoadTexture($"texture_{name}_style", m.GetTexture("_StyleTex")));
            }
        }

        private static void SetEquipmentTexture(string itemName, GameObject item)
        {
            if (item != null && itemName != null && itemName.Length > 0)
            {
                LoadOneTexture(item.gameObject, itemName, "item");
            }
        }

        private static void SetEquipmentListTexture(string itemName, List<GameObject> items)
        {
            if (items != null && items.Any() && itemName != null && itemName.Length > 0)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] == null)
                        continue;
                    SetEquipmentTexture(itemName, items[i]);

                }
            }
        }

        private static void SetBodyEquipmentTexture(string itemName, SkinnedMeshRenderer smr, List<GameObject> itemInstances, string which)
        {

            if (itemName != null && itemName.Length > 0)
            {
                foreach(GameObject go in itemInstances)
                {
                    /*
                    Dbgl($"body equipment {which} gameObject: {go.name}");
                    foreach(SkinnedMeshRenderer s in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        Dbgl($"body equipment {which} smr: {s.name}");
                    }
                    foreach(MeshRenderer s in go.GetComponentsInChildren<MeshRenderer>())
                    {
                        Dbgl($"body equipment {which} mr: {s.name}");
                    }
                    foreach(Material m in go.GetComponentsInChildren<Material>())
                    {
                        Dbgl($"body equipment {which} material: {m.name}");
                    }
                    foreach(Texture2D t in go.GetComponentsInChildren<Texture2D>())
                    {
                        Dbgl($"body equipment {which} texture: {t.name}");
                    }
                    */

                    foreach (SkinnedMeshRenderer s in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        Dbgl($"body equipment {go.name} smr: {s.name}");
                    }

                    LoadOneTexture(go, itemName, "item");

                    int childCount = go.transform.childCount;
                    for (int i = 0; i < childCount; i++)
                    {

                        Transform child = go.transform.GetChild(i);
                        foreach (SkinnedMeshRenderer s in child.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {
                            Dbgl($"body equipment child {child.name} smr: {s.name}");
                        }

                        LoadOneTexture(child.gameObject, itemName, "item", which);
                    }
                    //Dbgl(string.Join("\n", outputDump));
                }

                Dbgl($"{which} item: {itemName}");
                if (HasCustomTexture($"item_{itemName}_texture"))
                {
                    Dbgl($"setting custom texture for item {itemName}: item_{itemName}_texture.png");
                    smr.material.SetTexture($"{which}Tex", LoadTexture($"item_{itemName}_texture", smr.material.GetTexture($"{which}Tex")));
                    smr.material.color = Color.white;

                }
                else
                    Dbgl($"item {itemName}, texture name {smr.material.mainTexture?.name}; use item_{itemName}_texture.png");

                if (HasCustomTexture($"item_{itemName}_bump"))
                {
                    Dbgl($"setting custom texture for item {itemName}: item_{itemName}_bump.png");
                    smr.material.SetTexture($"{which}BumpMap", LoadTexture($"item_{itemName}_bump", smr.material.GetTexture($"{which}BumpMap")));
                }
                if (HasCustomTexture($"item_{itemName}_metal"))
                {
                    Dbgl($"setting custom texture for item {itemName}: item_{itemName}_metal.png");
                    smr.material.SetTexture($"{which}Metal", LoadTexture($"item_{itemName}_metal", smr.material.GetTexture($"{which}Metal")));
                }
            }
        }
    }
}
