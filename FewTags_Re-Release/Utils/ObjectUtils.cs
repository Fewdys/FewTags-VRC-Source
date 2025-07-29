using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FewTags.Utils
{
    public static class ObjectUtils
    {
        public static List<string> ObjectsToDestroy = new List<string> { "Trust Icon", "Performance Icon", "Performance Text", "Friend Anchor Stats", "Reason" };

        public static void DestroyChildren(GameObject? obj) // actually just hiding but yea lol
        {
            foreach (var name in ObjectsToDestroy)
            {
                var find = obj?.transform.Find(name);
                if (find != null)
                    find.gameObject?.SetActive(false);
            }
        }

        public static Transform RecursiveFindChild(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == childName)
                    return child;

                var result = RecursiveFindChild(child, childName);
                if (result != null)
                    return result;
            }
            return parent.Find(childName);
        }
    }
}
