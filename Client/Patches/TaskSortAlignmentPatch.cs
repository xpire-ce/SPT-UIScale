using System.Collections;
using System.Linq;
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
using UnityEngine.UI;

namespace UIScale.Client.Patches
{
    /// <summary>
    /// Copies the task-list column layout to the Tasks sort header.
    /// </summary>
    public class TaskSortAlignmentPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TasksPanel).GetMethod("Show", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        public static void PatchPostfix(MonoBehaviour __instance)
        {
            if (!Plugin.Enabled.Value || !Plugin.FixTaskSortHeader.Value)
                return;

            __instance.StartCoroutine(AlignAfterTaskListRenders(__instance.transform));
        }

        private static IEnumerator AlignAfterTaskListRenders(Transform root)
        {
            for (var frame = 0; frame < 300; frame++)
            {
                yield return null;

                if (!TryAlign(root))
                    continue;

                if (Plugin.DebugLog.Value)
                    Plugin.Log.LogInfo("[UIScale] Aligned Tasks sort header to task-list columns with last few neurons");

                yield break;
            }
        }

        internal static bool TryAlign(Transform root)
        {
            var sortPanel = root.GetComponentInChildren<QuestsSortPanel>(true);
            var taskRow = root.GetComponentsInChildren<NotesTask>(true)
                .FirstOrDefault(row => row.gameObject.activeInHierarchy);
            if (sortPanel == null || taskRow == null)
                return false;

            var headers = sortPanel.GetComponentsInChildren<FilterButton>(true)
                .Where(button => button.gameObject.activeInHierarchy)
                .Select(button => new
                {
                    Button = button,
                    Rect = button.transform as RectTransform
                })
                .Where(entry => entry.Rect != null)
                .OrderBy(entry => GetCenterX(entry.Rect!))
                .Select(entry => entry.Button)
                .ToList();

            var columns = new[]
            {
                GetColumnCell(taskRow, "_traderAvatar"),
                GetColumnCell(taskRow, "_typeIcon"),
                GetColumnCell(taskRow, "_taskLabel"),
                GetColumnCell(taskRow, "_locationLabel"),
                GetColumnCell(taskRow, "_statusLabel"),
                GetColumnCell(taskRow, "_progressView")
            };

            if (headers.Count != columns.Length)
                return false;

            var headerLayout = sortPanel.GetComponent<HorizontalLayoutGroup>();
            var rowLayout = columns[0]?.parent?.GetComponent<HorizontalLayoutGroup>();
            if (headerLayout == null || rowLayout == null)
                return false;

            headerLayout.spacing = rowLayout.spacing;

            // Copy the row widths, including the flexible Task column.
            for (var i = 0; i < headers.Count; i++)
            {
                var headerLayoutElement = headers[i].GetComponent<LayoutElement>();
                var column = columns[i];
                var columnLayoutElement = column?.GetComponent<LayoutElement>();
                if (headerLayoutElement == null || columnLayoutElement == null)
                    return false;

                headerLayoutElement.minWidth = columnLayoutElement.minWidth;
                headerLayoutElement.preferredWidth = columnLayoutElement.preferredWidth;
                headerLayoutElement.flexibleWidth = columnLayoutElement.flexibleWidth;
            }

            var headerRect = sortPanel.transform as RectTransform;
            if (headerRect == null)
                return false;

            LayoutRebuilder.ForceRebuildLayoutImmediate(headerRect);
            Canvas.ForceUpdateCanvases();
            return true;

        }

        private static RectTransform? GetColumnCell(NotesTask taskRow, string fieldName)
        {
            if (AccessTools.Field(typeof(NotesTask), fieldName)?.GetValue(taskRow) is not Component component)
                return null;

            var shortInfo = taskRow.transform.Find("TaskShortInfo");
            if (shortInfo == null)
                return null;

            var current = component.transform;
            while (current != null && current.parent != shortInfo)
                current = current.parent;

            return current as RectTransform;
        }

        private static float GetCenterX(RectTransform? rect)
        {
            return rect == null ? float.PositiveInfinity : rect.TransformPoint(rect.rect.center).x;
        }
    }
}
