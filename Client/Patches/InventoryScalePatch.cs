using System.Reflection;
using EFT.UI;
using SPT.Reflection.Patching;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace UIScale.Client.Patches
{
    /// <summary>
    /// Patches UICanvasScalerController.ChangeCanvasScalerRestriction, the single
    /// chokepoint where EFT applies its scale factor to every registered CanvasScaler.
    ///
    /// Original flow:
    ///   RunResolutionObserver() polls resolution each frame
    ///   → computes _scaleFactor = Min(screenW/1920, screenH/1080)
    ///   → ResolutionChangedHandler() iterates all registered scalers
    ///   → ChangeCanvasScalerRestriction(scaler) applies _scaleFactor
    ///
    /// This patch reads the game's auto-calculated _scaleFactor (which updates
    /// when you change resolution in-game) and multiplies it by the user's
    /// scale percentage. 100% = vanilla, 75% = smaller UI / more grid space.
    /// </summary>
    public class CanvasScalerPatch : ModulePatch
    {
        protected override MethodBase? GetTargetMethod()
        {
            return typeof(UICanvasScalerController).GetMethod(
                nameof(UICanvasScalerController.ChangeCanvasScalerRestriction),
                BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPrefix]
        public static bool PatchPrefix(CanvasScaler scaler)
        {
            if (!Plugin.Enabled.Value || scaler == null)
                return true;

            // Read the game's auto-calculated scale for the current resolution.
            // _scaleFactor = Min(screenW/1920, screenH/1080), updates on resolution change.
            float gameScale = UICanvasScalerController._scaleFactor;

            // Apply user's percentage adjustment
            float userScale = Plugin.ScalePercent.Value / 100f;
            float finalScale = gameScale * userScale;

            if (Plugin.DebugLog.Value)
            {
                Plugin.Log.LogInfo($"[UIScale] Scaler: '{scaler.gameObject.name}', " +
                                   $"gameScale={gameScale:F3}, " +
                                   $"userPercent={Plugin.ScalePercent.Value}%, " +
                                   $"final={finalScale:F3}");
            }

            // Replicate SetCanvasRestriction with our adjusted scale
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.referencePixelsPerUnit = 100f;
            scaler.scaleFactor = finalScale;

            return false; // skip original
        }
    }
}
