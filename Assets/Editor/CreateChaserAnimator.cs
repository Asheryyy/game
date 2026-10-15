using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

/// <summary>
/// Tools → Tạo Animator đuổi (Idle ⇄ Run): tạo AnimatorController có tham số Float "Speed",
/// Speed > 0.1 → chạy, < 0.1 → đứng. Dùng clip Idle + Run_N của gói Codersan.
/// Nếu đang chọn 1 object có Animator → gắn luôn controller vào và bật Animator lên.
/// </summary>
public static class CreateChaserAnimator
{
    const string Folder = "Assets/Codersan/Character/Model/Animations";
    const string IdlePath = Folder + "/Idle.fbx";
    const string RunPath = Folder + "/Run_N.anim";
    const string OutPath = Folder + "/GiangHoAnimator.controller";

    [MenuItem("Tools/Tạo Animator đuổi (Idle ⇄ Run)")]
    static void Create()
    {
        // cho clip Idle lặp lại (làm TRƯỚC khi load clip, vì reimport sẽ tạo lại clip)
        SetLoop(IdlePath);

        AnimationClip idle = AssetDatabase.LoadAllAssetsAtPath(IdlePath).OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        AnimationClip run = AssetDatabase.LoadAssetAtPath<AnimationClip>(RunPath);
        if (idle == null || run == null)
        {
            Debug.LogError($"[Animator] Không tìm thấy clip: Idle={(idle != null)} Run={(run != null)}. Kiểm tra lại thư mục {Folder}");
            return;
        }

        AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(OutPath);
        if (ctrl == null) ctrl = AnimatorController.CreateAnimatorControllerAtPath(OutPath);

        // làm lại từ đầu cho chắc
        foreach (var p in ctrl.parameters.ToArray()) ctrl.RemoveParameter(p);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        foreach (var s in sm.states.ToArray()) sm.RemoveState(s.state);

        AnimatorState idleState = sm.AddState("Idle", new Vector3(300, 0, 0));
        idleState.motion = idle;
        AnimatorState runState = sm.AddState("Run", new Vector3(300, 120, 0));
        runState.motion = run;
        sm.defaultState = idleState;

        var toRun = idleState.AddTransition(runState);
        toRun.hasExitTime = false; toRun.duration = 0.15f;
        toRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        var toIdle = runState.AddTransition(idleState);
        toIdle.hasExitTime = false; toIdle.duration = 0.2f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Animator] Đã tạo {OutPath}", ctrl);

        // gắn vào object đang chọn (nếu có)
        foreach (GameObject go in Selection.gameObjects)
        {
            Animator anim = go.GetComponentInChildren<Animator>(true);
            if (anim == null) continue;
            Undo.RecordObject(anim, "Gắn GiangHoAnimator");
            anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;
            anim.enabled = true;
            Debug.Log($"[Animator] Đã gắn vào {anim.name} (bật Animator, tắt Apply Root Motion)", anim);
        }
    }

    static void SetLoop(string fbxPath)
    {
        var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) return;
        var clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0) clips = importer.defaultClipAnimations;
        bool changed = false;
        foreach (var c in clips) if (!c.loopTime) { c.loopTime = true; changed = true; }
        if (changed) { importer.clipAnimations = clips; importer.SaveAndReimport(); }
    }
}
