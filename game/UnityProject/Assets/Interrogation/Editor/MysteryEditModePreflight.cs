using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Mystery.Interrogation;
using Mystery.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mystery.Interrogation.Editor
{
    /// <summary>
    /// READ ONLY Edit Mode asset inspection. No scene load, prefab save,
    /// import change, Play Mode, test runner, or player build.
    /// </summary>
    public static class MysteryEditModePreflight
    {
        private const string PrefabPath = "Assets/UI/Prefabs/DialoguePanel.prefab";
        private const string PortraitMappingPath = "Docs/NPCPortraitMapping.json";
        private const string PortraitFolder = "Assets/UI/Art/NPCPortraits/";
        private const string CaseFolder = "Assets/Interrogation/Cases/";
        private const string InvestigationFolder = "Assets/Interrogation/Investigations/";

        private static readonly string[] CaseNames = { "Mansion", "Cruise", "MusicBroadcast" };
        private static readonly string[] InspectorComponents =
        {
            "DialoguePanel", "DialogueInterrogationAdapter", "InvestigationNotebookView",
            "InvestigationSceneView", "CaseProgressSaveController", "CaseConclusionView"
        };

        [Serializable] private sealed class PortraitMapping
        {
            public PortraitEntry[] portraits = Array.Empty<PortraitEntry>();
        }
        [Serializable] private sealed class PortraitEntry
        {
            public int speakerIndex;
            public string name;
            public string file;
            public string sha256;
        }

        private sealed class Audit
        {
            private readonly StringBuilder lines = new StringBuilder();
            private int passes;
            private int failures;
            private int notes;

            public void Check(bool valid, string title)
            {
                if (valid) { passes++; lines.Append("[OK] "); }
                else { failures++; lines.Append("[FAIL] "); }
                lines.AppendLine(title);
            }
            public void Note(string description)
            {
                notes++;
                lines.Append("[INFO] ").AppendLine(description);
            }
            public void Publish()
            {
                string message = "[mystery Edit Mode preflight] "
                    + "checks passed=" + passes + " failed=" + failures
                    + ", notes=" + notes + "\n" + lines
                    + "범위: 현재 로컬 Unity 에셋 정적 점검만 수행함. "
                    + "Play Mode/기능 테스트/빌드 미실시.";
                if (failures == 0) Debug.Log(message);
                else Debug.LogError(message);
            }
        }

        [MenuItem("Mystery/Verification/Inspect Current Project (Edit Mode)")]
        public static void InspectCurrentProject()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Edit Mode only. Play Mode is not allowed.");
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Wait until the current Editor import/compile ends.");

            var audit = new Audit();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            audit.Check(prefab != null, "기존 DialoguePanel 프리팹 에셋이 존재함");
            if (prefab == null) { audit.Publish(); return; }

            CheckComponents(prefab, audit);
            CheckOpenSceneMissingScripts(audit);
            CheckDialogueReferences(prefab, audit);
            CheckPortraits(prefab, audit);
            CheckCases(prefab, audit);
            audit.Publish();
        }

        private static void CheckOpenSceneMissingScripts(Audit audit)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                audit.Note("현재 열려 있는 씬이 없어 Missing Script 검사는 건너뜀");
                return;
            }

            int count = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var current in root.GetComponentsInChildren<Transform>(true))
                {
                    foreach (var behaviour in current.GetComponents<MonoBehaviour>())
                    {
                        if (behaviour != null) continue;
                        count++;
                        var path = current.name;
                        var parent = current.parent;
                        while (parent != null)
                        {
                            path = parent.name + "/" + path;
                            parent = parent.parent;
                        }
                        audit.Note("Missing Script가 있는 씬 오브젝트: " + path);
                    }
                }
            }
            audit.Check(count == 0,
                scene.name + " 열린 씬 Missing Script " + count + "개");
        }

        private static void CheckComponents(GameObject prefab, Audit audit)
        {
            var panel = prefab.GetComponent<DialoguePanel>();
            var interrogation = prefab.GetComponent<DialogueInterrogationAdapter>();
            var notebook = prefab.GetComponent<InvestigationNotebookView>();
            var investigation = prefab.GetComponent<InvestigationSceneView>();
            var progress = prefab.GetComponent<CaseProgressSaveController>();
            var conclusion = prefab.GetComponent<CaseConclusionView>();

            Component[] components = { panel, interrogation, notebook, investigation, progress, conclusion };
            for (int i = 0; i < components.Length; i++)
                audit.Check(components[i] != null, "기존 루트 컴포넌트: " + InspectorComponents[i]);

            int missing = 0;
            foreach (var tr in prefab.GetComponentsInChildren<Transform>(true))
                foreach (var script in tr.GetComponents<MonoBehaviour>())
                    if (script == null) missing++;
            audit.Check(missing == 0, "프리팹의 Missing Script 0개 (현재 " + missing + "개)");

            foreach (var component in new MonoBehaviour[] { notebook, investigation, progress, conclusion })
            {
                if (component == null) continue;
                var so = new SerializedObject(component);
                var normal = so.FindProperty("regularFont");
                var heading = so.FindProperty("headingFont");
                audit.Check(normal != null && normal.objectReferenceValue is TMP_FontAsset
                    && heading != null && heading.objectReferenceValue is TMP_FontAsset,
                    component.GetType().Name + " 폰트 2종 연결");
            }
        }

        private static void CheckDialogueReferences(GameObject prefab, Audit audit)
        {
            var panel = prefab.GetComponent<DialoguePanel>();
            if (panel == null) return;
            var so = new SerializedObject(panel);
            string[] fields =
            {
                "frame", "speakerName", "dialogueBody", "questionInput", "sendButton",
                "historyScroll", "historyText", "statusText", "npcPortrait",
                "mansionFrame", "cruiseFrame", "musicBroadcastFrame"
            };
            foreach (var field in fields)
            {
                var property = so.FindProperty(field);
                audit.Check(property != null && property.objectReferenceValue != null,
                    "대화창 필수 참조: " + field);
            }

            string[] frameFiles = { "Mansion", "Cruise", "MusicBroadcast" };
            for (int i = 0; i < frameFiles.Length; i++)
            {
                string path = "Assets/UI/Art/DialogueFrames/" + frameFiles[i] + ".png";
                var asset = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                var prop = so.FindProperty(
                    i == 0 ? "mansionFrame" : i == 1 ? "cruiseFrame" : "musicBroadcastFrame");
                audit.Check(asset != null && prop != null && prop.objectReferenceValue == asset,
                    "테마 프레임 원본 Sprite: " + frameFiles[i]);
            }

            var texts = prefab.GetComponentsInChildren<TMP_Text>(true);
            int count = texts.Count(t => t != null);
            bool hasFonts = texts.All(t => t != null && t.font != null);
            audit.Check(count == 8 && hasFonts,
                "기존 TMP 텍스트 8곳의 폰트 참조 (현재 " + count + "곳)");
            var portraitObject = prefab.transform.Find("Frame/NpcPortrait");
            Image portrait = portraitObject != null ? portraitObject.GetComponent<Image>() : null;
            audit.Check(portrait != null && portrait.preserveAspect
                && portrait.raycastTarget == false,
                "원본 NPC 초상화 비율·입력 클릭 비차단 설정");
        }

        private static void CheckPortraits(GameObject prefab, Audit audit)
        {
            string full = Path.Combine(Application.dataPath, "..", PortraitMappingPath);
            if (!File.Exists(full))
            {
                audit.Check(false, "NPC SHA-256 매핑 문서 없음: " + PortraitMappingPath);
                return;
            }

            PortraitMapping mapping;
            try { mapping = JsonUtility.FromJson<PortraitMapping>(File.ReadAllText(full)); }
            catch (Exception e)
            {
                audit.Check(false, "NPC 매핑 문서 파싱 실패: " + e.Message);
                return;
            }

            var panel = prefab.GetComponent<DialoguePanel>();
            if (panel == null) return;
            var so = new SerializedObject(panel);
            var portraits = so.FindProperty("speakerPortraits");
            audit.Check(mapping != null && mapping.portraits != null
                && mapping.portraits.Length == 6
                && portraits != null && portraits.arraySize == 6,
                "NPC 6명 매핑 개수 일치");
            if (mapping?.portraits == null || portraits == null
                || portraits.arraySize != 6) return;

            var seen = new HashSet<int>();
            foreach (var item in mapping.portraits)
            {
                if (item == null || item.speakerIndex < 0 || item.speakerIndex >= 6
                    || !seen.Add(item.speakerIndex))
                {
                    audit.Check(false, "NPC 인덱스 유효성 및 중복 확인");
                    continue;
                }

                string enumName = ((DialogueSpeaker)item.speakerIndex).ToString();
                audit.Check(string.Equals(enumName, item.name, StringComparison.Ordinal),
                    "NPC 이름 매핑 " + item.speakerIndex + " : " + enumName);
                if (string.IsNullOrWhiteSpace(item.file) || item.file.IndexOf('/') >= 0
                    || item.file.IndexOf('\\') >= 0)
                {
                    audit.Check(false, "NPC 파일명 누락 또는 경로 위험: " + enumName);
                    continue;
                }
                string path = PortraitFolder + item.file;
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                var binding = portraits.GetArrayElementAtIndex(item.speakerIndex);
                var speakerField = binding.FindPropertyRelative("speaker");
                var spriteField = binding.FindPropertyRelative("portrait");
                audit.Check(sprite != null && speakerField != null
                    && speakerField.enumValueIndex == item.speakerIndex
                    && spriteField != null && spriteField.objectReferenceValue == sprite,
                    "NPC 원본 Sprite 및 프리팹 참조: " + enumName);

                string file = Path.Combine(Application.dataPath, "UI", "Art",
                    "NPCPortraits", item.file);
                bool hashValid = false;
                if (File.Exists(file) && !string.IsNullOrWhiteSpace(item.sha256))
                {
                    using (var sha = SHA256.Create())
                    {
                        string actual = BitConverter.ToString(
                            sha.ComputeHash(File.ReadAllBytes(file))).Replace("-", "").ToLowerInvariant();
                        hashValid = string.Equals(actual, item.sha256, StringComparison.OrdinalIgnoreCase);
                    }
                }
                audit.Check(hashValid, "NPC 원본 PNG SHA-256: " + enumName);
            }
            audit.Check(seen.Count == 6, "NPC 인덱스 0~5 모두 존재");
        }

        private static void CheckCases(GameObject prefab, Audit audit)
        {
            var adapter = prefab.GetComponent<DialogueInterrogationAdapter>();
            var scene = prefab.GetComponent<InvestigationSceneView>();
            if (adapter == null || scene == null) return;

            var interrogationSO = new SerializedObject(adapter);
            var locationSO = new SerializedObject(scene);
            string[] caseFields = { "mansionCase", "cruiseCase", "musicBroadcastCase" };
            string[] locationFields =
                { "mansionInvestigation", "cruiseInvestigation", "musicBroadcastInvestigation" };
            for (int i = 0; i < CaseNames.Length; i++)
            {
                string label = CaseNames[i];
                var caseAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(
                    CaseFolder + label + ".json");
                var investigationAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(
                    InvestigationFolder + label + "Investigation.json");
                var caseRef = interrogationSO.FindProperty(caseFields[i]);
                var locationRef = locationSO.FindProperty(locationFields[i]);
                audit.Check(caseAsset != null && caseRef != null
                    && caseRef.objectReferenceValue == caseAsset,
                    label + " 사건 JSON 참조");
                audit.Check(investigationAsset != null && locationRef != null
                    && locationRef.objectReferenceValue == investigationAsset,
                    label + " 조사 JSON 참조");
                if (caseAsset == null || investigationAsset == null) continue;

                try
                {
                    var file = JsonUtility.FromJson<CaseFile>(caseAsset.text);
                    var catalog = JsonUtility.FromJson<InvestigationCatalog>(investigationAsset.text);
                    audit.Check(file != null && catalog != null
                        && file.theme == i && catalog.theme == i
                        && string.Equals(file.caseId, catalog.caseId, StringComparison.Ordinal),
                        label + " 테마·사건 ID 일치");
                    if (file == null || catalog == null) continue;

                    if (!file.approved)
                        audit.Note(label + "은 승인 전 초안(approved=false) — 게임 진행 잠금 유지");

                    if (i == 0)
                    {
                        string caseError = InterrogationResolver.Validate(file, DialogueTheme.MANSION);
                        string verdictError = AccusationVerdictRules.ValidateVerdict(file);
                        string catalogError = InvestigationCatalogRules.Validate(catalog, file);
                        audit.Check(caseError == null, "대저택 심문 구조: "
                            + (caseError ?? "정상"));
                        audit.Check(verdictError == null, "대저택 범인 지목 설정: "
                            + (verdictError ?? "정상"));
                        audit.Check(catalogError == null, "대저택 조사/증거 연결: "
                            + (catalogError ?? "정상"));

                        int spots = catalog.locations == null ? 0
                            : catalog.locations.Sum(loc => loc?.spots?.Length ?? 0);
                        int statements = file.topics == null ? 0
                            : file.topics.Sum(topic => topic?.statements?.Length ?? 0);
                        audit.Check(catalog.locations != null && catalog.locations.Length == 9
                            && spots == 15 && file.evidence != null && file.evidence.Length == 12
                            && file.topics != null && file.topics.Length == 13 && statements == 78,
                            "대저택 9장소/15지점/12증거/13주제/78진술");

                        CheckEvidenceReachability(catalog, file, audit);
                    }
                    else
                        audit.Note(label + " 조사 목록 " +
                            (catalog.locations?.Length ?? 0) + "개 (미작성 초안)");
                }
                catch (Exception e)
                {
                    audit.Check(false, label + " JSON 읽기 실패: " + e.Message);
                }
            }
        }

        private static void CheckEvidenceReachability(
            InvestigationCatalog catalog, CaseFile file, Audit audit)
        {
            if (catalog?.locations == null || file?.evidence == null) return;
            var acquired = new HashSet<string>(StringComparer.Ordinal);
            bool changed;
            do
            {
                changed = false;
                foreach (var room in catalog.locations)
                {
                    if (room?.spots == null) continue;
                    foreach (var spot in room.spots)
                    {
                        if (spot == null || string.IsNullOrWhiteSpace(spot.evidenceId)
                            || acquired.Contains(spot.evidenceId)) continue;
                        if (spot.requiresEvidenceIds != null
                            && spot.requiresEvidenceIds.Any(id => !acquired.Contains(id))) continue;
                        acquired.Add(spot.evidenceId);
                        changed = true;
                    }
                }
            } while (changed);

            audit.Check(file.evidence.All(e => e != null && acquired.Contains(e.id)),
                "대저택 선행 단서 순서상 모든 증거에 도달 가능 "
                + acquired.Count + "/" + file.evidence.Length);
        }
    }
}
