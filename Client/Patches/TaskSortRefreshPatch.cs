using System.Collections;
using System.Linq;
using System.Reflection;
using EFT.UI;
using SPT.Reflection.Patching;
using UnityEngine;

namespace UIScale.Client.Patches
{
    /// <summary>
    /// Reapplies the task header layout after a sort refresh.
    /// </summary>
    public class TaskSortRefreshPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TasksPanel)
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Single(method =>
                {
                    var parameters = method.GetParameters();
                    return parameters.Length == 2
                        && parameters[0].ParameterType == typeof(EQuestsSortType)
                        && parameters[1].ParameterType == typeof(bool);
                });
        }

        [PatchPostfix]
        public static void PatchPostfix(MonoBehaviour __instance)
        {
            if (!Plugin.Enabled.Value || !Plugin.FixTaskSortHeader.Value)
                return;

            __instance.StartCoroutine(AlignAfterSortRefresh(__instance.transform));
        }

        private static IEnumerator AlignAfterSortRefresh(Transform root)
        {
            yield return null;
            yield return null;
            TaskSortAlignmentPatch.TryAlign(root);
        }
    }
}
