using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

/// <summary>
/// Tools → Tạo Animator giang hồ (3giangho): tạo controller cho 2 model trong Assets/3giangho.
/// GiangHo1A: DungYen ⇄ Chay theo Float "Speed", Any State → Dam khi Trigger "Danh" (ChaserEnemy bật lúc bắt kịp).
/// GiangHo_Gay: đứng vác gậy, Any State → Danh khi Trigger "Danh".
/// Nếu đang chọn object có Animator → gắn luôn controller phù hợp (theo tên object).
/// </summary>
public static class CreateGiangHoAnimator
{
    const string Folder = "Assets/3giangho";

    [MenuItem("Tools/Tạo Animator giang hồ (3giangho)")]
    static void Create()
    {
        var c1 = Build(Folder + "/GiangHo1A.fbx", Folder + "/GiangHo1A_Animator.controller",
            idle: "_DungYen", run: "_Chay", hit: "_Dam", loops: new[] { "_Chay", "_DiBo", "_Dam" });
        var c2 = Build(Folder + "/GiangHo_Gay.fbx", Folder + "/GiangHo_Gay_Animator.controller",
            idle: null, run: null, hit: "_Danh", loops: new[] { "_Danh" });

        foreach (GameObject go in Selection.gameObjects)
        {
            Animator anim = go.GetComponentInChildren<Animator>(true);
            if (anim == null) anim = Undo.AddComponent<Animator>(go);
            var ctrl = go.name.Contains("Gay") ? c2 : c1;
            if (ctrl == null) continue;
            Undo.RecordObject(anim, "Gắn Animator giang hồ");
            anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;
            anim.enabled = true;
            Debug.Log($"[Animator] Đã gắn {ctrl.name} vào {anim.name}", anim);
        }
    }

    static AnimatorController Build(string fbx, string outPath, string idle, string run, string hit, string[] loops)
    {
        var importer = AssetImporter.GetAtPath(fbx) as ModelImporter;
        if (importer == null) { Debug.LogError($"[Animator] Không thấy {fbx}"); return null; }

        // Generic + Loop Time (làm TRƯỚC khi load clip, vì reimport tạo lại clip)
        importer.animationType = ModelImporterAnimationType.Generic;
        var clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0) clips = importer.defaultClipAnimations;
        foreach (var c in clips) c.loopTime = loops.Any(k => c.name.EndsWith(k));
        importer.clipAnimations = clips;
        importer.SaveAndReimport();

        var all = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__")).ToArray();
        AnimationClip Find(string key) => key == null ? null : all.FirstOrDefault(c => c.name.EndsWith(key));

        AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(outPath);
        if (ctrl == null) ctrl = AnimatorController.CreateAnimatorControllerAtPath(outPath);
        foreach (var p in ctrl.parameters.ToArray()) ctrl.RemoveParameter(p);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Danh", AnimatorControllerParameterType.Trigger);
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        foreach (var s in sm.states.ToArray()) sm.RemoveState(s.state);
        foreach (var t in sm.anyStateTransitions.ToArray()) sm.RemoveAnyStateTransition(t);

        AnimatorState idleState = sm.AddState("DungYen", new Vector3(300, 0, 0));
        idleState.motion = Find(idle); // null = đứng nguyên tư thế gốc (con cầm gậy vác gậy trên vai)
        sm.defaultState = idleState;

        AnimationClip runClip = Find(run);
        if (runClip != null)
        {
            AnimatorState runState = sm.AddState("Chay", new Vector3(300, 120, 0));
            runState.motion = runClip;
            var toRun = idleState.AddTransition(runState);
            toRun.hasExitTime = false; toRun.duration = 0.15f;
            toRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            var toIdle = runState.AddTransition(idleState);
            toIdle.hasExitTime = false; toIdle.duration = 0.2f;
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        }

        AnimationClip hitClip = Find(hit);
        if (hitClip != null)
        {
            AnimatorState hitState = sm.AddState("Danh", new Vector3(600, 60, 0));
            hitState.motion = hitClip;
            var toHit = sm.AddAnyStateTransition(hitState);
            toHit.hasExitTime = false; toHit.duration = 0.1f; toHit.canTransitionToSelf = false;
            toHit.AddCondition(AnimatorConditionMode.If, 0f, "Danh");
        }

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Animator] Đã tạo {outPath} (clip: {string.Join(", ", all.Select(c => c.name))})", ctrl);
        return ctrl;
    }
}
