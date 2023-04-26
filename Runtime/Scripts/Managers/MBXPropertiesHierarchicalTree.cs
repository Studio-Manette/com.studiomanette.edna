using System;
using System.Collections.Generic;

using UnityEngine;

namespace StudioManette.Edna
{
    // A graph holding all MBXProperty instances, organised hierarchically
    // It allows for fast browsing of a property and its children, touching each one of them only once
    public class MBXPropertyItem
    {
        // Available categories sorted by descending "englobing" logic
        public enum Category
        {
            MaterialName = 0,
            MaterialCategory,
            MaterialSubCategory,
            MaterialGroup,
            MaterialProperty
        }


        public GameObject gameObject;
        public Type type;
        public MBXPropertyItem[] children;

        // Lookup table matching cpnt types to the category enum
        // The default "property" case is not handled here:
        // !s_CpntTypeToCategories.ContainsKey(cpntType) => Categories.MaterialProperty
        private static Dictionary<Type, Category> s_CpntTypeToCategories = new Dictionary<Type, Category>
        {
            {typeof(MBXPropertyMaterialName), Category.MaterialName },
            {typeof(MBXGroupProperty), Category.MaterialGroup },
            {typeof(MBXSubPropertyCategory), Category.MaterialSubCategory },
            {typeof(MBXPropertyCategory), Category.MaterialCategory }
        };

        public bool IsValid()
        {
            return gameObject != null;
        }

        public static Category GetCategoryFromType(GameObject gameObject)
        {
            MBXProperty childCpnt = gameObject.GetComponent<MBXProperty>();
            if (childCpnt != null)
            {
                Category result = Category.MaterialProperty;
                if (s_CpntTypeToCategories.TryGetValue(childCpnt.GetType(), out result))
                {
                    return result;
                }
                else
                {
                    return Category.MaterialProperty;
                }
            }
            return Category.MaterialProperty;
        }

        public static MBXPropertyItem BuildChildrenTreeFromParent(GameObject[] mbxProperties)
        {
            // The idea is that the elements are already sorted hierarchically in the input array
            // So like this for instance:
            // MBXPropertyMaterialName MBXPropertyTexture MBXPropertyCategory MBXPropertyColor MBXPropertySlider MBXPropertyCategory MBXSubPropertyCategory MBXPropertyVector MBXPropertyColor
            // We end up with that tree:
            // MBXPropertyMaterialName
            //  MBXPropertyTexture
            //  MBXPropertyCategory
            //      MBXPropertyColor
            //      MBXPropertySlider
            //  MBXPropertyCategory
            //      MBXSubPropertyCategory
            //          MBXPropertyVector
            //          MBXPropertyColor

            Type currentType = mbxProperties[0].GetComponent<MBXProperty>().GetType();
            int index = 0;
            return BuildChildrenTreeFromParent_r(mbxProperties,
                currentType,
                ref index);
        }

        // Recursively retrieve the properties count for a given item
        public int GetPropertiesCount()
        {
            int result = IsValid() ? 1 : 0;
            if (children != null)
            {
                foreach (MBXPropertyItem item in children)
                {
                    result += item.GetPropertiesCount();
                }
            }
            return result;
        }

        // Starting from this item, recursively look for the item matching the given GO
        public MBXPropertyItem FindItem(GameObject parent)
        {
            foreach (MBXPropertyItem item in BrowseChildren(true))
            {
                if (item.gameObject == parent)
                {
                    return item;
                }
            }
            return new MBXPropertyItem();
        }

        // Starting from this item, returns true on the first found item of the given type
        public bool HasItemsOfType<T>(bool includeSelf) where T : MBXProperty
        {
            foreach (MBXPropertyItem item in BrowseChildren(includeSelf))
            {
                if (item.type == typeof(T))
                {
                    return true;
                }
            }
            return false;
        }

        // Starting from this item, recursively look for the item matching the given GO,
        // then iterate over its entire children hierarchy (direct children + their respective children etc.)
        public IEnumerable<MBXPropertyItem> BrowseChildren(bool includeSelf)
        {
            // TODO @gama: this is inefficient memory wise as it creates lots of iterators
            // Better not fully recurse

            if (includeSelf)
            {
                yield return this;
            }
            if (children != null)
            {
                foreach (MBXPropertyItem item in children)
                {
                    foreach (MBXPropertyItem subItem in item.BrowseChildren(true))
                    {
                        yield return subItem;
                    }
                }
            }
        }

        private static MBXPropertyItem BuildChildrenTreeFromParent_r(GameObject[] mbxProperties,
            Type currentParentType,
            ref int index)
        {
            MBXPropertyItem result = new MBXPropertyItem { gameObject = mbxProperties[index], type = currentParentType };
            List<MBXPropertyItem> children = new List<MBXPropertyItem>();
            while (index < mbxProperties.Length - 1)
            {
                index += 1;
                GameObject current = mbxProperties[index];
                if (current == null)
                {
                    continue;
                }
                MBXProperty cpnt = current.GetComponent<MBXProperty>();
                if (cpnt == null)
                {
                    continue;
                }

                // As long as the inspected does not have the exact same type as its parent, go on
                Category category = GetCategoryFromType(mbxProperties[index]);
                if (category <= s_CpntTypeToCategories[currentParentType])
                {
                    index -= 1;
                    break;
                }

                // For the current child we can now create a matching MBXPropertyItem...
                MBXPropertyItem currentChild = new MBXPropertyItem { gameObject = current };

                // Children retrieval has to be recursive depending on the type (container or not)
                switch (cpnt)
                {
                    case MBXPropertyMaterialName _:
                        {
                            currentChild = BuildChildrenTreeFromParent_r(mbxProperties,
                                typeof(MBXPropertyMaterialName),
                                ref index);
                            currentChild.type = typeof(MBXPropertyMaterialName);
                            break;
                        }
                    // This one "is a" MBXSubPropertyCategory so it has to be handled before it
                    case MBXGroupProperty _:
                        {
                            currentChild = BuildChildrenTreeFromParent_r(mbxProperties,
                                typeof(MBXGroupProperty),
                                ref index);
                            currentChild.type = typeof(MBXGroupProperty);
                            break;
                        }
                    // This one "is a" MBXPropertyCategory so it has to be handled before it
                    case MBXSubPropertyCategory _:
                        {
                            currentChild = BuildChildrenTreeFromParent_r(mbxProperties,
                                typeof(MBXSubPropertyCategory),
                                ref index);
                            currentChild.type = typeof(MBXSubPropertyCategory);
                            break;
                        }
                    case MBXPropertyCategory _:
                        {
                            currentChild = BuildChildrenTreeFromParent_r(mbxProperties,
                                typeof(MBXPropertyCategory),
                                ref index);
                            currentChild.type = typeof(MBXPropertyCategory);
                            break;
                        }
                    case MBXPropertyBoolean _:
                    case MBXPropertyColor _:
                    case MBXPropertyEnum _:
                    // This one "is a" MBXPropertyFloat so it has to be handled before it
                    case MBXPropertySlider _:
                    case MBXPropertyFloat _:
                    case MBXPropertyGradient _:
                    case MBXPropertyTexture _:
                    case MBXPropertyVector _:
                        {
                            // These cannot have any children, no recursion
                            currentChild.type = cpnt.GetType();
                            break;
                        }
                    default:
                        {
                            Debug.LogError("MBXProperty type not handled, please add it!");
                            break;
                        }
                }
                children.Add(currentChild);
            }
            result.children = children.ToArray();
            return result;
        }
    }
}
