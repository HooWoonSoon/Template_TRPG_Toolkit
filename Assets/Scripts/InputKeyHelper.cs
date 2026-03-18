using System.Linq;
using UnityEngine;

namespace Tactics.InputHelper
{
    public static class InputKeyHelper
    {
        private static readonly KeyCode[] modifierKeys =
        {
        KeyCode.LeftControl, KeyCode.RightControl,
        KeyCode.LeftShift, KeyCode.RightShift,
        KeyCode.LeftAlt, KeyCode.RightAlt,
        KeyCode.AltGr
    };

        public static bool GetKeyCombo(KeyCode modifier, KeyCode key)
        {
            return Input.GetKey(modifier) && Input.GetKeyDown(key);
        }
        public static bool GetKeySolo(KeyCode key)
        {
            if (!Input.GetKey(key)) return false;

            foreach (var mod in modifierKeys)
            {
                if (Input.GetKey(mod)) return false;
            }
            return true;
        }
        public static bool GetKeyModifier(KeyCode modifier)
        {
            if (!modifierKeys.Contains(modifier))
                return false;
            if (Input.GetKey(modifier)) return true;
            return false;
        }
        public static bool GetKeyDownSolo(KeyCode key)
        {
            if (!Input.GetKeyDown(key)) return false;

            foreach (var mod in modifierKeys)
            {
                if (Input.GetKey(mod)) return false;
            }
            return true;
        }
    }
}