using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.Profiling;

using MachinMachines.Utils;

using StudioManette.Bob.MBX;
using StudioManette.ShaderProperties;
using static StudioManette.Bob.MBX.MBXOverrideCpnt;

using Utils = StudioManette.Edna.RuntimeUtils;

namespace StudioManette.Edna
{
    public class MBXPropertyManager : MonoBehaviour
    {
        public static readonly int DEFAULT_CATEGORY = -1;
        public static readonly int DEFAULT_SUBCATEGORY = -1;
        public static readonly int DEFAULT_GROUP = -1;

        private const int kUnknownPropertiesCount = 16;

        public GameObject prefabMatName;
        public GameObject prefabCategory;
        public GameObject prefabSubCategory;
        public GameObject prefabGroup;
        public GameObject prefabPropertyGradient;
        public GameObject prefabPropertyColor;
        public GameObject prefabPropertyFloat;
        public GameObject prefabPropertyRange;
        public GameObject prefabPropertyBoolean;
        public GameObject prefabPropertyEnum;
        public GameObject prefabPropertyTexture;
        public GameObject prefabPropertyVector;

        public AccessiblityLevel currentAccessLevel = AccessiblityLevel.SUPER_USER;

        public MBXPropertyItem propertiesHierarchicalTree;

        public bool isEnablingPresets = true;

        private MBXOverrideCpnt mbxComponent;
        private List<GameObject> itemsToUnload;
        private MaterialManager mmController;

        // Start is called before the first frame update
        //@return errorMessage if there is at least one error
        public string Init(MBXOverrideCpnt mbxcpnt, MaterialManager mm)
        {
            Profiler.BeginSample("MBXPropertyManager_Init");

            List<string> alertMessages = new List<string>();

            CleanMatUI();

            mmController = mm;
            mbxComponent = mbxcpnt;

            PropertyDescriptor[] propertyDescriptors = mbxcpnt.BrowseOverrideProperties().ToArray();
            itemsToUnload = new List<GameObject>(propertyDescriptors.Length);

            // Cache material related data in a specific lookup table
            HashSet<(CustomizableShader, string, int)> materialDescriptors = new HashSet<(CustomizableShader, string, int)>();
            foreach (PropertyDescriptor propertyDescriptor in propertyDescriptors)
            {
                CustomizableShader currentShaderProfile = MaterialManagerUtils.FindShaderProfileByMaterialName(mmController.shaderConfig.shaderProfiles, propertyDescriptor.MaterialProfileName);
                // TODO @gm this might happen with a bad shader profile name
                // So we just skip to the next one
                if (currentShaderProfile == null)
                {
                    continue;
                }
                materialDescriptors.Add((
                    currentShaderProfile,
                    propertyDescriptor.SlotName,
                    propertyDescriptor.MaterialId));
            }

            // A table holding for every material index the table of its prefab properties, referenced by root order
            Dictionary<int, GameObject[]> materialIdToObjectsTable = new Dictionary<int, GameObject[]>(materialDescriptors.Count);
            Dictionary<string, int> slotNameToUnknownCategoryIndex = new Dictionary<string, int>(materialDescriptors.Count);
            Dictionary<int, List<string>> materialIdToUnknownProperties = new Dictionary<int, List<string>>(materialDescriptors.Count);

            Profiler.BeginSample("MBXPropertyManager_Init_headers");
            foreach ((CustomizableShader, string, int)materialDescriptor in materialDescriptors)
            {
                CustomizableShader currentShaderProfile = materialDescriptor.Item1;
                string slotName = materialDescriptor.Item2;
                int currentMatIdx = materialDescriptor.Item3;

                // This table holds all MBX property objects in the order of their property in the shader profile
                // However:
                // - the prefabMatName is stored at index 0
                // - From index currentShaderProfile.GetPropertiesCount() onwards, all properties are "unknown" ones (up to kUnknownPropertiesCount)
                GameObject[] knownObjects = new GameObject[currentShaderProfile.GetPropertiesCount() + 1 + kUnknownPropertiesCount];
                materialIdToObjectsTable.Add(currentMatIdx, knownObjects);

                //instancier un prefabMatName et modifier le nom
                //Utils.Log("instantiate prefabMatName : " + prefabMatName);
                GameObject matName = InstantiatePrefabProperty(prefabMatName, currentMatIdx, -1);
                matName.GetComponent<MBXPropertyMaterialName>().Init(this, currentMatIdx, 0, slotName, AccessiblityLevel.COMMON_USER, DEFAULT_CATEGORY, DEFAULT_SUBCATEGORY, DEFAULT_GROUP);
                matName.GetComponent<MBXPropertyMaterialName>().Setup(isEnablingPresets);
                knownObjects[0] = matName;

                int lastCategoryIndex = 0;
                int tmpSubCateg = DEFAULT_SUBCATEGORY;
                int groupIndex = DEFAULT_GROUP;

                //instancier les prefabCategMat, prefabCategSubMat, et prefabGroup
                for (int propertyIndex = 0; propertyIndex < currentShaderProfile.GetPropertiesCount(); ++propertyIndex)
                {
                    ShaderProperty property = currentShaderProfile.properties[propertyIndex];
                    if (property.visibility == Visibility.HEADER_CATEGORY || property.visibility == Visibility.HEADER_SUBCATEGORY || property.visibility == Visibility.HEADER_GROUP)
                    {
                        int place = 0;
                        ShaderProperty sprop = currentShaderProfile.GetShaderProperty(property.name, out place);
                        if (sprop == null)
                        {
                            Debug.LogError("error, could not find : " + property.name + " in GetShaderPropery");
                        }
                        int categoryIndex = sprop == null ? -1 : sprop.categoryIndex;

                        //Utils.Log("instantiate prefabCategMat : " + entryHeader.Value);
                        GameObject prefab = null;
                        int headerIndex = DEFAULT_CATEGORY;
                        int childCategIndex = DEFAULT_SUBCATEGORY;
                        int subHeaderIndex = DEFAULT_SUBCATEGORY;

                        switch (property.visibility)
                        {
                            case Visibility.HEADER_CATEGORY:
                            {
                                prefab = prefabCategory;
                                childCategIndex = categoryIndex;
                                groupIndex = DEFAULT_GROUP;
                                break;
                            }
                            case Visibility.HEADER_SUBCATEGORY:
                            {
                                prefab = prefabSubCategory;
                                headerIndex = categoryIndex;
                                groupIndex = DEFAULT_GROUP;
                                break;
                            }
                            case Visibility.HEADER_GROUP:
                            {
                                prefab = prefabGroup;
                                headerIndex = categoryIndex;
                                subHeaderIndex = tmpSubCateg;
                                break;
                            }
                        }

                        GameObject matCateg = InstantiatePrefabProperty(prefab, currentMatIdx, propertyIndex);
                        matCateg.GetComponent<MBXPropertyCategory>().Init(this, currentMatIdx, 0, property.name, property.level, headerIndex, subHeaderIndex, groupIndex);

                        switch (property.visibility)
                        {
                            case Visibility.HEADER_CATEGORY:
                            {
                                tmpSubCateg = DEFAULT_SUBCATEGORY;
                                matCateg.GetComponent<MBXPropertyCategory>().Setup(childCategIndex);
                                break;
                            }
                            case Visibility.HEADER_SUBCATEGORY:
                            {
                                tmpSubCateg++;
                                matCateg.GetComponent<MBXSubPropertyCategory>().Setup(categoryIndex, tmpSubCateg);
                                break;
                            }
                            case Visibility.HEADER_GROUP:
                            {
                                groupIndex++;
                                matCateg.GetComponent<MBXGroupProperty>().Setup(categoryIndex, tmpSubCateg, groupIndex);
                                break;
                            }
                        }

                        knownObjects[propertyIndex + 1] = matCateg;
                        lastCategoryIndex = Mathf.Max(lastCategoryIndex, Mathf.Max(childCategIndex, headerIndex));
                    }
                }

                lastCategoryIndex++;
                int unknownCategoryIndex = lastCategoryIndex;
                if (!slotNameToUnknownCategoryIndex.ContainsKey(slotName))
                {
                    slotNameToUnknownCategoryIndex.Add(slotName, unknownCategoryIndex);
                }
                //Utils.Log("instantiate prefabUnknownSubMat");
                //add a category for unknown properties
                GameObject matCategUnk = InstantiatePrefabProperty(prefabCategory, currentMatIdx, currentShaderProfile.GetPropertiesCount());
                matCategUnk.GetComponent<MBXPropertyCategory>().Init(this, currentMatIdx, 0, "Unknown Properties", AccessiblityLevel.COMMON_USER, DEFAULT_CATEGORY, DEFAULT_SUBCATEGORY, DEFAULT_GROUP);
                matCategUnk.GetComponent<MBXPropertyCategory>().Setup(unknownCategoryIndex);
                knownObjects[currentShaderProfile.GetPropertiesCount() + 1] = matCategUnk;
            }
            Profiler.EndSample();

            Profiler.BeginSample("MBXPropertyManager_Init_properties");

            foreach (PropertyDescriptor pdesc in propertyDescriptors)
            {
                //Debug.Log("Browse Property : " + pdesc.MaterialId + "," + pdesc.PropertyId);

                GameObject[] knownObjects;
                if (!materialIdToObjectsTable.TryGetValue(pdesc.MaterialId, out knownObjects))
                {
                    continue;
                }
                CustomizableShader currentShaderProfile = materialDescriptors.Where(item => item.Item2 == pdesc.SlotName).FirstOrDefault().Item1;

                int place = -1;
                ShaderProperty sprop = currentShaderProfile.GetShaderProperty(pdesc.PropertyName, out place);
                // Incrementing place as "0" is the prefabMatName
                if (knownObjects[place + 1] != null)
                {
                    alertMessages.Add(new string($"Error ! this key exists twice in MBX file : {place} property Name : {pdesc.PropertyName}"));
                    //Utils.Alert();
                    continue;
                }

                AccessiblityLevel spropLevel = (sprop != null ? sprop.level : AccessiblityLevel.COMMON_USER);
                CustomType spropCustomType = (sprop != null ? sprop.customType : CustomType.INPUT);
                string propName = (sprop != null ? sprop.name : pdesc.PropertyName);
                int propCategory = (sprop != null ? sprop.categoryIndex : slotNameToUnknownCategoryIndex[pdesc.SlotName]);
                int propSubCategory = (sprop != null ? sprop.subCategoryIndex : DEFAULT_SUBCATEGORY);
                int propGroup = (sprop != null ? sprop.groupIndex : DEFAULT_GROUP);
                string propDefaultValue = (sprop != null ? sprop.defaultValue : null);

                //Debug.Log("add property : " + pdesc.PropertyName + " // level : " + spropLevel + "(" + (int)spropLevel + ")");
                //Debug.Log("current level : " + mmController.accessModifyProperties);

                GameObject propGO = InstantiatePrefabProperty(pdesc.PropertyType, spropCustomType, pdesc.MaterialId, place);
                propGO.GetComponent<MBXProperty>().Init(this, pdesc.MaterialId, pdesc.PropertyId, propName, spropLevel, propCategory, propSubCategory, propGroup, propDefaultValue);
                TooltipMaterialProperty tooltipMat = propGO.GetComponent<MBXProperty>().TMProName.gameObject.AddComponent<TooltipMaterialProperty>();
                tooltipMat.Init(sprop != null ? sprop.tooltipMessage : "this Property is unknown.");

                switch (pdesc.PropertyType)
                {
                    case MBXMaterialProperty.PropertyType.RGB: propGO.GetComponent<MBXPropertyColor>().Setup(); break;
                    case MBXMaterialProperty.PropertyType.VECTOR: propGO.GetComponent<MBXPropertyVector>().Setup(sprop == null ? new Vector2(0, 100) : sprop.range); break;
                    case MBXMaterialProperty.PropertyType.VALTORGB: propGO.GetComponent<MBXPropertyGradient>().Setup(); break;
                    case MBXMaterialProperty.PropertyType.RANGE: propGO.GetComponent<MBXPropertySlider>().Setup(sprop == null ? new Vector2(0, 1) : sprop.range); break;
                    case MBXMaterialProperty.PropertyType.TEX_IMAGE:
                    {
                        bool isLinear = false;
                        bool mustBeLinear = isLinear;
                        if (pdesc.PropertyOption != null)
                        {
                            isLinear = pdesc.PropertyOption.Contains(MBXMaterialProperty.PropertyOption.LINEAR.ToString());
                            if (sprop != null)
                            {
                                if (sprop.isLinear != isLinear)
                                {
                                    string ColorspaceInMBX = isLinear ? "Linear" : "SRGB";
                                    string ColorspaceInShader = sprop.isLinear ? "Linear" : "SRGB";
                                    alertMessages.Add(new string("Error ! property Name :" + pdesc.PropertyName + ": You are trying to assign a " + ColorspaceInMBX +
                                        " texture while the " + sprop.name + " property is only compatible with " + ColorspaceInShader +
                                        ". Please fix MBX File or change texture."));
                                }
                                mustBeLinear = sprop.isLinear;
                            }
                        }
                        propGO.GetComponent<MBXPropertyTexture>().Setup(isLinear, mustBeLinear);
                        break;
                    }
                    case MBXMaterialProperty.PropertyType.VALUE:
                    {
                        switch (spropCustomType)
                        {
                            case CustomType.INPUT: propGO.GetComponent<MBXPropertyFloat>().Setup(); break;
                            case CustomType.BOOLEAN: propGO.GetComponent<MBXPropertyBoolean>().Setup(); break;
                            case CustomType.RANGE01: propGO.GetComponent<MBXPropertySlider>().Setup(sprop.range); break;
                            case CustomType.ENUM: propGO.GetComponent<MBXPropertyEnum>().Setup(sprop.attributes); break;
                        }
                        break;
                    }
                }
                if (sprop == null)
                {
                    // Debug only!
                    if (!materialIdToUnknownProperties.ContainsKey(pdesc.MaterialId))
                    {
                        List<string> unknownProperties = new List<string>(kUnknownPropertiesCount);
                        materialIdToUnknownProperties.Add(pdesc.MaterialId, unknownProperties);
                    }
                    materialIdToUnknownProperties[pdesc.MaterialId].Add(pdesc.PropertyName);
                    if (materialIdToUnknownProperties[pdesc.MaterialId].Count >= kUnknownPropertiesCount)
                    {
                        alertMessages.Add(new string($"Error ! Too many unknown properties in MBX file for material ID {pdesc.MaterialId}, the MBX is properly outdated."));
                        break;
                    }
                    // Si sprop est nul, on affiche quand m�me la prop dans la cat�gorie Unknown
                    place = currentShaderProfile.GetPropertiesCount();
                    // Pour faire �a on part de la cat�gorie "unknown" et on cherche la place libre suivante
                    while ((place < currentShaderProfile.GetPropertiesCount() + kUnknownPropertiesCount)
                           && knownObjects[place + 1] != null)
                    {
                        place += 1;
                    }
                }
                knownObjects[place + 1] = propGO;
            }
            //okay, all is setup now
            Profiler.EndSample();

            Profiler.BeginSample("AddListToInterface");
            foreach (var knownObjects in materialIdToObjectsTable.Values)
            {
                AddListToInterface(knownObjects);
            }
            Profiler.EndSample();

            // Now build the tree holding all properties hierarchically
            Profiler.BeginSample("CreateHierarchicalTree");
            MBXPropertyItem[] children = new MBXPropertyItem[materialIdToObjectsTable.Keys.Count];

            int materialIdx = 0;
            foreach (GameObject[] mbxProperties in materialIdToObjectsTable.Values)
            {
                children[materialIdx] = MBXPropertyItem.BuildChildrenTreeFromParent(mbxProperties);
                materialIdx += 1;
            }
            propertiesHierarchicalTree = new MBXPropertyItem
            {
                gameObject = this.gameObject,
                type = this.GetType(),
                children = children
            };
            Profiler.EndSample();

            CountUnknownProperties();

            Profiler.BeginSample("MBXSubPropertyCategory init");
            //add an other init for SubCategories
            foreach (MBXSubPropertyCategory mspc in this.GetComponentsInChildren<MBXSubPropertyCategory>())
            {
                mspc.InitAfterLoading();
            }
            Profiler.EndSample();

            FilterByAccessibility(mmController.accessModifyProperties);
            Profiler.EndSample();

            if (alertMessages.Count > 0)
            {
                string alertMsg = "Material loading ended with " + alertMessages.Count + " errors :\n \n";
                for (int i = 0; i < alertMessages.Count; i++)
                {
                    alertMsg += "(" + i + ") " + alertMessages[i] + "\n";
                }
                return alertMsg;
            }
            else
            {
                return "";
            }
        }

        // This is mostly used for cleaning (ie reclaiming memory)
        public void Reset()
        {
            CleanMatUI();
        }

        public void ForceRefreshTextures()
        {
            // mbxComponent.TriggerNewRefresh(MBXRefreshType.ForceReadFiles | MBXRefreshType.ForceTextureRefreshOnly) ;
            mbxComponent.TriggerNewRefresh(MBXRefreshType.ForceReadFiles);
        }

        public string RefreshTextures()
        {
            Profiler.BeginSample("MBXPropertyManager - RefreshTextures");

            // Dictionary from texture full path to (pixel count, uses count)
            Dictionary<string, (float, int)> texturesUsage = new Dictionary<string, (float, int)>();
            foreach (MBXPropertyTexture mbxPropTex in this.GetComponentsInChildren<MBXPropertyTexture>(true))
            {
                if (mbxPropTex.textureItem != null && !string.IsNullOrEmpty(mbxPropTex.textureItem.TextureParms.Filepath))
                {
                    //Debug.Log("mbxPropTex : " + mbxPropTex.name + mbxPropTex.textureItem.TextureParms.ColorSpace);
                    mbxPropTex.UpdateInfosTexture();
                    (float, int)usageData = (0.0f, 0);
                    if (texturesUsage.TryGetValue(mbxPropTex.textureItem.TextureParms.Filepath, out usageData))
                    {
                        // Just increment uses count
                        usageData.Item2 += 1;
                        texturesUsage[mbxPropTex.textureItem.TextureParms.Filepath] = usageData;
                    }
                    else
                    {
                        usageData.Item1 = mbxPropTex.textureItem.TexturePixelSize.x * mbxPropTex.textureItem.TexturePixelSize.y;
                        usageData.Item2 = 1;
                        texturesUsage.Add(mbxPropTex.textureItem.TextureParms.Filepath, usageData);
                    }
                }
            }
            float totalPixelCount = texturesUsage.Select(item => item.Value.Item1).Sum();
            long vRamApprox = (long)(totalPixelCount * CommonUtils.RATIO_PIXEL_VRAM);

            Profiler.EndSample();
            return "number of textures : " + texturesUsage.Count + "\nVRAM : ~" + MemorySizeUnit.DisplayByteSize(vRamApprox, 1);
        }

        // Update the visibility of the hierarchy of MBXProperty game objects
        public void UpdateVisibility(GameObject root, bool isEnabled, AccessiblityLevel userAccess)
        {
            Profiler.BeginSample("MBXPropertyManager - UpdateVisibility");
            MBXPropertyItem rootItem = propertiesHierarchicalTree.FindItem(root);
            if (rootItem.IsValid())
            {
                foreach (MBXPropertyItem child in rootItem.BrowseChildren(false))
                {
                    MBXProperty mbxProp = child.gameObject.GetComponent<MBXProperty>();
                    child.gameObject.SetActive(isEnabled && userAccess >= mbxProp.accessibility);
                    MBXPropertyCategory mbxPropCat = mbxProp as MBXPropertyCategory;
                    if (mbxPropCat != null && mbxPropCat.gameObject.activeInHierarchy)
                    {
                        mbxPropCat.Show(true);
                    }
                }
            }
            Profiler.EndSample();
        }

        public void FilterByAccessibility(AccessiblityLevel access)
        {
            Profiler.BeginSample("FilterByAccessibility");
            currentAccessLevel = access;
            foreach (MBXPropertyMaterialName mbxPropCat in this.GetComponentsInChildren<MBXPropertyMaterialName>())
            {
                mbxPropCat.UpdateVisibility(access);
            }
            Profiler.EndSample();
        }

        public void FilterByMatNames(List<string> matNames)
        {
            if (matNames == null)
            {
                foreach (MBXPropertyMaterialName mbxPropCat in this.GetComponentsInChildren<MBXPropertyMaterialName>(true))
                {
                    mbxPropCat.gameObject.SetActive(true);
                    mbxPropCat.UpdateVisibility(currentAccessLevel);
                }
            }
            else
            {
                foreach (MBXPropertyMaterialName mbxPropCat in this.GetComponentsInChildren<MBXPropertyMaterialName>(true))
                {
                    if (matNames.Contains(mbxPropCat.propertyName))
                    {
                        mbxPropCat.gameObject.SetActive(true);
                        mbxPropCat.UpdateVisibility(currentAccessLevel);
                    }
                    else
                    {
                        mbxPropCat.ForceFold(false);
                        mbxPropCat.gameObject.SetActive(false);
                    }
                }
            }
        }

        public void SetValueColorToMBX(int matID, int propID, Color colValue, bool writeFile)
        {
            mbxComponent.SetPropertyColorValue(matID, propID, colValue);
            if (writeFile)
            {
                ApplyProperties();
            }
        }

        public void SetValueFloatToMBX(int matID, int propID, float floValue, bool writeFile)
        {
            mbxComponent.SetPropertyFloatValue(matID, propID, floValue);
            if (writeFile)
            {
                ApplyProperties();
            }
        }

        public void SetValueGradientToMBX(int matID, int propID, Gradient graValue, bool writeFile)
        {
            mbxComponent.SetPropertyGradientValue(matID, propID, graValue);
            if (writeFile)
            {
                ApplyProperties();
            }
        }

        public void SetValueVectorToMBX(int matID, int propID, Vector4 vecValue, bool writeFile)
        {
            mbxComponent.SetPropertyVectorValue(matID, propID, vecValue);
            if (writeFile)
            {
                ApplyProperties();
            }
        }

        public Color GetValueColorFromMBX(int matID, int propID)
        {
            return mbxComponent.GetPropertyColorValue(matID, propID);
        }

        public float GetValueFloatFromMBX(int matID, int propID)
        {
            return mbxComponent.GetPropertyFloatValue(matID, propID);
        }

        public Gradient GetValueGradientFromMBX(int matID, int propID)
        {
            return mbxComponent.GetPropertyGradientValue(matID, propID);
        }

        public string GetValueTextFromMBX(int matID, int propID)
        {
            return mbxComponent.GetPropertyTextValue(matID, propID);
        }

        public Vector4 GetValueVectorFromMBX(int matID, int propID)
        {
            return mbxComponent.GetPropertyVectorValue(matID, propID);
        }

        public void ApplyProperties()
        {
            mbxComponent.SerialiseMBXData();
        }

        private void AddListToInterface(GameObject[] _list)
        {
            //ajouter les objets dans l'ordre
            foreach (GameObject item in _list)
            {
                if (item != null)
                {
                    item.transform.SetParent(this.transform);
                }
            }
        }

        private GameObject InstantiatePrefabProperty(MBXMaterialProperty.PropertyType propType, CustomType customType, int matID, int place)
        {
            GameObject prefab = null;
            switch (propType)
            {
                case MBXMaterialProperty.PropertyType.RGB: prefab = prefabPropertyColor; break;
                case MBXMaterialProperty.PropertyType.VECTOR: prefab = prefabPropertyVector; break;
                case MBXMaterialProperty.PropertyType.VALTORGB: prefab = prefabPropertyGradient; break;
                case MBXMaterialProperty.PropertyType.RANGE: prefab = prefabPropertyRange; break;
                case MBXMaterialProperty.PropertyType.TEX_IMAGE: prefab = prefabPropertyTexture; break;
                case MBXMaterialProperty.PropertyType.VALUE:
                {
                    switch (customType)
                    {
                        case CustomType.INPUT: prefab = prefabPropertyFloat; break;
                        case CustomType.BOOLEAN: prefab = prefabPropertyBoolean; break;
                        case CustomType.RANGE01: prefab = prefabPropertyRange; break;
                        case CustomType.ENUM: prefab = prefabPropertyEnum; break;
                    }
                    break;
                }
            }

            return InstantiatePrefabProperty(prefab, matID, place);
        }

        private GameObject InstantiatePrefabProperty(GameObject prefab, int matID, int place)
        {
            Profiler.BeginSample($"InstantiatePrefabProperty {prefab.name}");
            GameObject go = GameObject.Instantiate(prefab, null);
            go.name = matID.ToString() + "_" + place.ToString() + "_" + prefab.name;
            itemsToUnload.Add(go);
            Profiler.EndSample();
            return go;
        }

        private void CleanMatUI()
        {
            if (itemsToUnload != null && itemsToUnload.Count > 0)
            {
                for (int i = 0; i < itemsToUnload.Count; i++)
                {
                    GameObject.DestroyImmediate(itemsToUnload[i]);
                }
                itemsToUnload.Clear();
            }
        }

        private void CountUnknownProperties()
        {
            MBXPropertyCategory unknownPropCat = null;
            int unknowCategoryIndex = 0;
            int currentMaterialId = 0;

            foreach (MBXPropertyCategory mbxPropCat in this.GetComponentsInChildren<MBXPropertyCategory>())
            {
                if (currentMaterialId != mbxPropCat.materialID && unknownPropCat != null)
                {
                    unknownPropCat.DisplayChildrenCount();
                    unknownPropCat = null;
                }
                if (mbxPropCat.IsCategory && mbxPropCat.childrenCategoryId >= unknowCategoryIndex)
                {
                    currentMaterialId = mbxPropCat.materialID;
                    unknowCategoryIndex = mbxPropCat.categoryIndex;
                    unknownPropCat = mbxPropCat;
                }
            }
            //at the end of the loop
            if (unknownPropCat != null)
            {
                unknownPropCat.DisplayChildrenCount();
            }
        }

        public string GetMBXPresetBlock(string _slotName)
        {
            MBXMaterial mat = mbxComponent.GetMaterialBySlotName(_slotName);
            return JsonUtility.ToJson(mat);
        }

        public List<string> GetPersistentPropertiesList(string _slotName)
        {
            List<string> strList = new();
            CustomizableShader currentShaderProfile = MaterialManagerUtils.FindShaderProfileByMaterialName(mmController.shaderConfig.shaderProfiles, mbxComponent.GetMaterialBySlotName(_slotName).materialProfileName);
            foreach (ShaderProperty sp in currentShaderProfile.properties)
            {
                if (sp.isPresetPersistent) strList.Add(sp.name);
            }
            return strList;
        }

        private MBXMaterial CopyUnpersistentProperties(MBXMaterial src , MBXMaterial dest)
        {
            CustomizableShader currentShaderProfile = MaterialManagerUtils.FindShaderProfileByMaterialName(mmController.shaderConfig.shaderProfiles, src.materialProfileName);
            MBXMaterial tmpMat = src;
            foreach (MBXMaterialProperty propSrc in src.properties)
            {
                //TODO gab : refaire une version plus propre de GetShaderProperty sans le param�tre en sortie
                int tmpIndex;
                ShaderProperty sp = currentShaderProfile.GetShaderProperty(propSrc.name, out tmpIndex);
                if (sp != null)
                {
                    if (!sp.isPresetPersistent)
                    {
                        try
                        {
                            //trouver le propSrc dans dest
                            MBXMaterialProperty propDst = dest.properties.First(p => (p.name == propSrc.name));

                            //trouver l'index du propSrc correspondant dans tmpMat
                            int index = Array.IndexOf(tmpMat.properties, (tmpMat.properties.First(p => p.name == propSrc.name)));

                            //remplacer la property dans le material courant
                            //Debug.Log("remplacer : " + tmpMat.properties[index].name + " par " + propDst.name);
                            tmpMat.properties[index] = propDst;
                        }
                        catch (InvalidOperationException e)
                        {
                            Debug.LogWarning(e.Message);
                        }
                    }
                }
                else
                {
                    throw new Exception("property not found in material " + src.materialProfileName + " : " + propSrc.name);
                }
            }
            return tmpMat;
        }

        private void ReApplyPresetAfterException()
        {
            //ApplyPreset(_slotName, _newPresetBlock, false);
        }

        public void ApplyPreset(string _slotName, string _newPresetBlock, bool _applyOnlyPersistent)
        {
            //R�cup�rer le current block dans le mbx
            string oldPresetBlock = GetMBXPresetBlock(_slotName);
            MBXMaterial newMat = JsonUtility.FromJson<MBXMaterial>(_newPresetBlock);

            //on modifie le slotName du preset � appliquer pour que ce soit raccord
            newMat.slotName = _slotName;

            MBXData readData = mbxComponent.GetMBXData();
            MBXData newData = readData;

            int indexMatToChange = Array.IndexOf(readData.materials, readData.materials.First(p => p.slotName == _slotName));

            if (_applyOnlyPersistent)
            {
                try
                {
                    MBXMaterial tmpMat = CopyUnpersistentProperties(readData.materials[indexMatToChange], newMat);
                    newData.materials[indexMatToChange] = tmpMat;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Exception!");
                    throw new Exception(e.Message);
                }
            }
            else
            {
                newData.materials[indexMatToChange] = newMat;
            }

            string newMbxPath = CommonUtils.GetFolderfromPathName(mbxComponent.OverrideFilePath) + DateTime.Now.ToString("yyMMddhhmmss") +  ".mbx";

            StreamWriter writer = new StreamWriter(newMbxPath);
            writer.Write(JsonUtility.ToJson(newData));
            writer.Close();

            Utils.GetManager().LoadNewMBX(newMbxPath);
        }
    }
}
