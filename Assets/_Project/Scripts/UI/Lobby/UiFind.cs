using UnityEngine;

namespace FateBastion.UI
{
    /// <summary>
    /// Finds nodes of C's layout prefabs by their names (hierarchy contract of C-12, see the C-12 PR node table), so B's
    /// view scripts can simply be added to C's prefabs. A missing node is logged once with its path.
    /// </summary>
    public static class UiFind
    {
        public static T Get<T>(Transform root, string path, Object context) where T : Component
        {
            Transform node = Node(root, path, context);
            if (node == null) return null;

            T component = node.GetComponent<T>();
            if (component == null) Debug.LogError($"[UI] '{root.name}/{path}' has no {typeof(T).Name}.", context);
            return component;
        }

        public static Transform Node(Transform root, string path, Object context)
        {
            if (root == null) return null;
            Transform node = root.Find(path);
            if (node == null) Debug.LogError($"[UI] '{root.name}/{path}' not found (hierarchy contract C-12).", context);
            return node;
        }

        /// <summary>Optional node: no log when missing.</summary>
        public static T Optional<T>(Transform root, string path) where T : Component
        {
            Transform node = root != null ? root.Find(path) : null;
            return node != null ? node.GetComponent<T>() : null;
        }

        public static void SetActive(Component component, bool active)
        {
            if (component != null && component.gameObject.activeSelf != active) component.gameObject.SetActive(active);
        }

        public static void SetActive(Transform node, bool active)
        {
            if (node != null && node.gameObject.activeSelf != active) node.gameObject.SetActive(active);
        }
    }
}
