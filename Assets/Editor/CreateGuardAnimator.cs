using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

/// <summary>
/// Tools → Tạo Animator bảo vệ (Tập võ ⇄ Đứng): tạo AnimatorController có tham số Bool "Training",
/// Training = true → tập võ (Fist Fight A), false → đứng (Idle). Dùng cùng GuardTraining.
/// Nếu đang chọn 1 object có Animator → gắn luôn controller vào và bật Animator lên.
/// </summary>
public static class CreateGuardAnimator
{
    const string TrainPath = "Assets/AnimationSource/Fist Fight A.fbx";
    const string IdlePath = "Assets/Codersan/Character/Model/Animations/Idle.fbx";
    const string OutPath = "Assets/AnimationSource/BaoVeAnimator.controller";

    /// Chọn 1 file animation Humanoid bất kỳ trong Project (đi bộ, bưng đồ, chạy...) → tạo "<tên>_Animator.controller"
    /// cạnh nó, cùng tham số "Training" → kéo vào Animator của NPC có gắn GuardTraining.
    [MenuItem("Tools/Tạo Animator NPC từ clip đang chọn (Hoạt động ⇄ Đứng)")]
    static void CreateFromSelection()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(path)) { Debug.LogError("[Animator] Chọn 1 file animation (.fbx/.anim) trong cửa sổ Project trước"); return; }
        Build(path, System.IO.Path.ChangeExtension(path, null) + "_Animator.controller");
    }

    /// Chọn 2 clip trong Project: clip đứng + clip đi bộ (tên có "DiBo"/"Walk") → tạo "<clip đứng>_DiRa.controller":
    /// mặc định Đứng, Bool "Walking" = true → Đi. Dùng cho DeliveryDoor có ô Walk From.
    [MenuItem("Tools/Tạo Animator NPC (Đứng ⇄ Đi)")]
    static void CreateIdleWalk()
    {
        string[] paths = Selection.objects.Select(AssetDatabase.GetAssetPath).Where(p => !string.IsNullOrEmpty(p)).Distinct().ToArray();
        if (paths.Length != 2) { Debug.LogError("[Animator] Chọn đúng 2 file animation: 1 clip đứng + 1 clip đi bộ (tên có DiBo/Walk)"); return; }
        bool IsWalk(string p) { string n = System.IO.Path.GetFileName(p).ToLowerInvariant(); return n.Contains("dibo") || n.Contains("walk"); }
        string walkPath = paths.FirstOrDefault(IsWalk), idlePath = paths.FirstOrDefault(p => !IsWalk(p));
        if (walkPath == null || idlePath == null) { Debug.LogError("[Animator] Không phân biệt được clip đi bộ — đặt tên clip đi có chữ DiBo hoặc Walk"); return; }

        SetLoop(walkPath); SetLoop(idlePath);
        AnimationClip walk = LoadClip(walkPath), idle = LoadClip(idlePath);
        if (walk == null || idle == null) { Debug.LogError($"[Animator] Không đọc được clip: Di={(walk != null)} Dung={(idle != null)}"); return; }

        string outPath = System.IO.Path.ChangeExtension(idlePath, null) + "_DiRa.controller";
        AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(outPath) ?? AnimatorController.CreateAnimatorControllerAtPath(outPath);
        foreach (var p in ctrl.parameters.ToArray()) ctrl.RemoveParameter(p);
        ctrl.AddParameter("Walking", AnimatorControllerParameterType.Bool);
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        foreach (var s in sm.states.ToArray()) sm.RemoveState(s.state);
        AnimatorState idleState = sm.AddState("Dung", new Vector3(300, 0, 0)); idleState.motion = idle;
        AnimatorState walkState = sm.AddState("Di", new Vector3(300, 120, 0)); walkState.motion = walk;
        sm.defaultState = idleState;
        var toWalk = idleState.AddTransition(walkState); toWalk.hasExitTime = false; toWalk.duration = 0.2f;
        toWalk.AddCondition(AnimatorConditionMode.If, 0f, "Walking");
        var toIdle = walkState.AddTransition(idleState); toIdle.hasExitTime = false; toIdle.duration = 0.25f;
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Walking");
        EditorUtility.SetDirty(ctrl); AssetDatabase.SaveAssets();
        Debug.Log($"[Animator] Đã tạo {outPath} (Đứng: {idle.name}, Đi: {walk.name}) — kéo vào Animator của NPC, tắt Apply Root Motion", ctrl);
    }

    [MenuItem("Tools/Tạo Animator bảo vệ (Tập võ ⇄ Đứng)")]
    static void Create()
    {
        AnimatorController ctrl = Build(TrainPath, OutPath);
        if (ctrl == null) return;

        // gắn vào object đang chọn (nếu có)
        foreach (GameObject go in Selection.gameObjects)
        {
            Animator anim = go.GetComponentInChildren<Animator>(true);
            if (anim == null) continue;
            Undo.RecordObject(anim, "Gắn BaoVeAnimator");
            anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;
            anim.enabled = true;
            Debug.Log($"[Animator] Đã gắn vào {anim.name} (bật Animator, tắt Apply Root Motion)", anim);
        }
    }

    static AnimatorController Build(string trainPath, string outPath)
    {
        // cho clip lặp lại (làm TRƯỚC khi load clip, vì reimport sẽ tạo lại clip)
        SetLoop(trainPath);
        SetLoop(IdlePath);

        AnimationClip train = LoadClip(trainPath);
        AnimationClip idle = LoadClip(IdlePath);
        if (train == null || idle == null)
        {
            Debug.LogError($"[Animator] Không tìm thấy clip: HoatDong={(train != null)} Idle={(idle != null)}");
            return null;
        }

        AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(outPath);
        if (ctrl == null) ctrl = AnimatorController.CreateAnimatorControllerAtPath(outPath);

        // làm lại từ đầu cho chắc
        foreach (var p in ctrl.parameters.ToArray()) ctrl.RemoveParameter(p);
        ctrl.AddParameter("Training", AnimatorControllerParameterType.Bool);
        ctrl.parameters = ctrl.parameters.Select(p => { if (p.name == "Training") p.defaultBool = true; return p; }).ToArray();
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        foreach (var s in sm.states.ToArray()) sm.RemoveState(s.state);

        AnimatorState trainState = sm.AddState("TapVo", new Vector3(300, 0, 0));
        trainState.motion = train;
        AnimatorState idleState = sm.AddState("Idle", new Vector3(300, 120, 0));
        idleState.motion = idle;
        sm.defaultState = trainState;

        var toIdle = trainState.AddTransition(idleState);
        toIdle.hasExitTime = false; toIdle.duration = 0.3f;
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Training");

        var toTrain = idleState.AddTransition(trainState);
        toTrain.hasExitTime = false; toTrain.duration = 0.3f;
        toTrain.AddCondition(AnimatorConditionMode.If, 0f, "Training");

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Animator] Đã tạo {outPath}", ctrl);
        return ctrl;
    }

    static AnimationClip LoadClip(string path) =>
        AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));

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
