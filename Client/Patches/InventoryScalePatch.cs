using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT.UI;
using SPT.Reflection.Patching;
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
    ///   -> computes _scaleFactor = Min(screenW/1920, screenH/1080)
    ///   -> ResolutionChangedHandler() iterates all registered scalers
    ///   -> ChangeCanvasScalerRestriction(scaler) applies _scaleFactor
    ///
    /// This patch reads the game's auto-calculated _scaleFactor (which updates
    /// when you change resolution in-game) and multiplies it by the user's
    /// scale percentage. 100% = vanilla, 75% = smaller UI / more grid space.
    /// </summary>
    public class CanvasScalerPatch : ModulePatch
    {
        private static Type? _scaleControllerType;
        private static FieldInfo? _gameScaleField;

        protected override MethodBase GetTargetMethod()
        {
            var controllerType = ScaleControllerType;

            return controllerType
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Single(method => IsCanvasScalerMethod(method)
                                  && method.Name != "Register"
                                  && method.Name != "Unregister");
        }

        [PatchPrefix]
        public static bool PatchPrefix(CanvasScaler scaler)
        {
            if (!Plugin.Enabled.Value || scaler == null)
                return true;

            // Read the game's auto-calculated scale for the current resolution.
            // _scaleFactor = Min(screenW/1920, screenH/1080), updates on resolution change.
            float gameScale = (float)GameScaleField.GetValue(null);

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

        private static Type FindScaleControllerType()
        {
            foreach (var type in GetLoadableTypes(typeof(TasksPanel).Assembly))
            {
                try
                {
                    var methods = type.GetMethods(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                    var hasRegister = methods.Any(method =>
                        method.Name == "Register" && IsCanvasScalerMethod(method));
                    var hasUnregister = methods.Any(method =>
                        method.Name == "Unregister" && IsCanvasScalerMethod(method));
                    var hasScaledPosition = methods.Any(method =>
                    {
                        var parameters = method.GetParameters();
                        return method.Name == "ScaledPosition"
                            && parameters.Length == 1
                            && parameters[0].ParameterType == typeof(Vector2)
                            && method.ReturnType == typeof(Vector2);
                    });
                    var hasScaleField = type.GetFields(
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                        .Count(field => field.FieldType == typeof(float)) == 1;

                    if (hasRegister && hasUnregister && hasScaledPosition && hasScaleField)
                        return type;
                }
                catch (TypeLoadException)
                {
                    // Skip obfuscated value types that cannot be reflected.
                }
            }

            throw new MissingMethodException(
                "Could not locate EFT's CanvasScaler controller in Assembly-CSharp.");
        }

        private static bool IsCanvasScalerMethod(MethodInfo method)
        {
            var parameters = method.GetParameters();
            return method.IsStatic
                && parameters.Length == 1
                && parameters[0].ParameterType == typeof(CanvasScaler);
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types
                    .Where(type => type != null)
                    .Select(type => type!);
            }
        }

        private static Type ScaleControllerType =>
            _scaleControllerType ??= FindScaleControllerType();

        private static FieldInfo GameScaleField =>
            _gameScaleField ??= ScaleControllerType
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Single(field => field.FieldType == typeof(float));
    }
}
