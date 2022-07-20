using MelonLoader;
using UnityEngine;

namespace PersonAvoider
{
    public static class PersonAvoiderSettings
    {
        internal static MelonPreferences_Entry<bool> ShouldJoinBlink;
        internal static MelonPreferences_Entry<bool> ShouldPlayJoinSound;
        internal static MelonPreferences_Entry<bool> JoinShowName;

        internal static MelonPreferences_Entry<float> SoundVolume;
        internal static MelonPreferences_Entry<bool> UseUiMixer;
        internal static MelonPreferences_Entry<int> TextSize;

        internal static MelonPreferences_Entry<string> JoinIconColor;
        internal static MelonPreferences_Entry<bool> LogToConsole;

        public static void RegisterSettings()
        {
            var category = MelonPreferences.CreateCategory("PersonAvoider", "Person Avoider");

            ShouldJoinBlink = category.CreateEntry("BlinkIcon", true, "Blink HUD icon on join");
            ShouldPlayJoinSound = category.CreateEntry("PlaySound", true, "Play sound on join");
            JoinShowName = category.CreateEntry("ShowJoinedName", true, "Show joined names");

            SoundVolume = category.CreateEntry("SoundVolume", .3f, "Sound volume (0-1)");
            UseUiMixer = category.CreateEntry("UseUiMixer", true, "Notifications are affected by UI volume slider");
            TextSize = category.CreateEntry("TextSize", 36, "Text size (pt)");

            JoinIconColor = category.CreateEntry("JoinColor", "127 191 255", "Join icon color (r g b)");
            LogToConsole = category.CreateEntry("LogToConsole", false, "Log user join/leave to console");
        }

        public static Color GetJoinIconColor() => DecodeColor(JoinIconColor.Value);

        private static Color DecodeColor(string color)
        {
            var split = color.Split(' ');
            int red = 255;
            int green = 255;
            int blue = 255;
            int alpha = 255;

            if (split.Length > 0) int.TryParse(split[0], out red);
            if (split.Length > 1) int.TryParse(split[1], out green);
            if (split.Length > 2) int.TryParse(split[2], out blue);
            if (split.Length > 3) int.TryParse(split[3], out alpha);

            return new Color(red / 255f, green / 255f, blue / 255f, alpha / 255f);
        }
    }
}
