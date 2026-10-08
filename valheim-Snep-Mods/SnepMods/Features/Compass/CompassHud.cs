using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SnepMods.Features.Compass
{
    // Draws a heading strip at the top of the screen with cardinal directions
    // and nearby map pins projected onto it by bearing.
    internal static class CompassHud
    {
        private const float FovDegrees = 180f;

        private static readonly AccessTools.FieldRef<Minimap, List<Minimap.PinData>> PinsField =
            AccessTools.FieldRefAccess<Minimap, List<Minimap.PinData>>("m_pins");

        private static GUIStyle _labelStyle;

        public static void OnGUI()
        {
            if (!CompassFeature.Enabled.Value || !Player.m_localPlayer || !GameCamera.instance)
            {
                return;
            }

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                };
            }

            float width = CompassFeature.Width.Value;
            float height = CompassFeature.Height.Value;
            var bar = new Rect((Screen.width - width) / 2f, 10f, width, height);

            GUI.Box(bar, GUIContent.none);

            float heading = GameCamera.instance.transform.eulerAngles.y;

            DrawDirection(bar, heading, 0f, "N");
            DrawDirection(bar, heading, 90f, "E");
            DrawDirection(bar, heading, 180f, "S");
            DrawDirection(bar, heading, 270f, "W");

            if (Minimap.instance != null)
            {
                foreach (Minimap.PinData pin in PinsField(Minimap.instance))
                {
                    if (!CompassFeature.IsMarkerVisible(pin.m_type))
                    {
                        continue;
                    }

                    bool isPlayer = pin.m_type == Minimap.PinType.Player;
                    if (pin.m_icon == null && !isPlayer)
                    {
                        continue;
                    }

                    DrawPin(bar, heading, pin, isPlayer);
                }
            }
        }

        private static void DrawDirection(Rect bar, float heading, float directionDegrees, string label)
        {
            float delta = Mathf.DeltaAngle(heading, directionDegrees);
            if (Mathf.Abs(delta) > FovDegrees / 2f)
            {
                return;
            }

            float x = bar.x + bar.width / 2f + delta / (FovDegrees / 2f) * (bar.width / 2f);
            GUI.Label(new Rect(x - 15f, bar.y, 30f, bar.height), label, _labelStyle);
        }

        private static void DrawPin(Rect bar, float heading, Minimap.PinData pin, bool isPlayer)
        {
            Vector3 toPin = pin.m_pos - Player.m_localPlayer.transform.position;
            float bearing = Mathf.Atan2(toPin.x, toPin.z) * Mathf.Rad2Deg;
            if (bearing < 0f)
            {
                bearing += 360f;
            }

            float delta = Mathf.DeltaAngle(heading, bearing);
            if (Mathf.Abs(delta) > FovDegrees / 2f)
            {
                return;
            }

            float x = bar.x + bar.width / 2f + delta / (FovDegrees / 2f) * (bar.width / 2f);
            float iconSize = bar.height * 0.8f;
            var iconRect = new Rect(x - iconSize / 2f, bar.y + (bar.height - iconSize) / 2f, iconSize, iconSize);

            if (pin.m_icon != null)
            {
                DrawSprite(iconRect, pin.m_icon);
            }
            else if (isPlayer)
            {
                GUI.Box(iconRect, GUIContent.none);
                if (!string.IsNullOrEmpty(pin.m_name))
                {
                    GUI.Label(new Rect(x - 50f, bar.y - 16f, 100f, 16f), pin.m_name, _labelStyle);
                }
            }
        }

        internal static void DrawSprite(Rect rect, Sprite sprite)
        {
            // sprite.rect is in the sprite's *original* texture space, but Valheim
            // packs these icons into a runtime atlas - sprite.texture is that atlas.
            // sprite.uv is the only value that's correctly mapped into atlas space.
            Vector2[] uvs = sprite.uv;
            float minU = uvs[0].x, maxU = uvs[0].x, minV = uvs[0].y, maxV = uvs[0].y;
            for (int i = 1; i < uvs.Length; i++)
            {
                minU = Mathf.Min(minU, uvs[i].x);
                maxU = Mathf.Max(maxU, uvs[i].x);
                minV = Mathf.Min(minV, uvs[i].y);
                maxV = Mathf.Max(maxV, uvs[i].y);
            }

            var uvRect = new Rect(minU, minV, maxU - minU, maxV - minV);
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, uvRect);
        }
    }
}
