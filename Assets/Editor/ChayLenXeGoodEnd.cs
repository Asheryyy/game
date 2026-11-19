using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;
using System.Linq;

/// <summary>
/// Tools → Good End: dựng cảnh chạy lên xe ngoài sân (chạy SAU menu "xe đón bằng đèn pha").
///   - GE_ChayLenXe (nhân vật) chạy từ CỬA CHÍNH (object "TeleportZone") tới hông xe (GE_XeDon) trong shot XeCho
///     → track "Chay len xe" (clip run đã có sẵn trên DienVien_Toi lo phần tay chân)
///   - Cam_GE_7_XeCho: đứng ngang, nhìn nhân vật chạy vào luồng đèn
///   - Cam_GE_8_XeChayDi: đứng ở cửa, nhìn theo xe quay đầu chạy đi
/// Dời xe / sửa cửa xong thì chạy lại menu. Chỉnh tay góc camera sau khi chạy (chạy lại sẽ đặt lại camera).
/// </summary>
public static class ChayLenXeGoodEnd
{
    [MenuItem("Tools/Good End: dựng cảnh chạy lên xe ngoài sân")]
    static void Build()
    {
        Transform cutRoot = XeDenPhaGoodEnd.Find("Cutscene_GoodEnd");
        var director = cutRoot != null ? cutRoot.GetComponent<PlayableDirector>() : null;
        var timeline = director != null ? director.playableAsset as TimelineAsset : null;
        Transform cua = XeDenPhaGoodEnd.Find("TeleportZone");    // cửa chính vào chung cư
        Transform xe = XeDenPhaGoodEnd.Find("GE_XeDon");
        Transform runner = XeDenPhaGoodEnd.Find("GE_ChayLenXe");
        Transform cam7 = XeDenPhaGoodEnd.Find("Cam_GE_7_XeCho"), cam8 = XeDenPhaGoodEnd.Find("Cam_GE_8_XeChayDi");
        TimelineClip shot = timeline != null ? XeDenPhaGoodEnd.Shot(timeline, director, "Cam_GE_7_XeCho") : null;
        if (shot == null || cua == null || xe == null || runner == null || cam7 == null || cam8 == null)
        {
            Debug.LogError($"[ChayLenXe] Thiếu: Timeline/shot XeCho={shot != null} TeleportZone(cửa chính)={cua != null} GE_XeDon={xe != null} " +
                           $"GE_ChayLenXe={runner != null} Cam_GE_7={cam7 != null} Cam_GE_8={cam8 != null}. Chạy menu \"xe đón bằng đèn pha\" trước.");
            return;
        }

        // ── đường chạy: ra khỏi cửa 1m → hông xe (phía trước bánh, trong luồng đèn) ──
        Vector3 xeFwd = Flat(xe.forward), xeRight = Vector3.Cross(Vector3.up, xeFwd);
        Vector3 toXe = Flat(xe.position - cua.position);
        Vector3 a = Ground(cua.position + toXe * 1f, xe.position.y);
        Vector3 b = Ground(xe.position + xeFwd * 0.6f + xeRight * 1.3f, xe.position.y);
        Vector3 dir = Flat(b - a);
        Quaternion face = Quaternion.LookRotation(dir);

        // ── nhân vật: GE_ChayLenXe vào hộp chứa có Animator (animate con → không dính root motion) ──
        Transform holder = XeDenPhaGoodEnd.Find("GE_ChayLenXe_Root");
        if (holder == null)
        {
            var h = new GameObject("GE_ChayLenXe_Root");
            Undo.RegisterCreatedObjectUndo(h, "Chay len xe");
            h.transform.SetParent(cutRoot, false);
            holder = h.transform;
        }
        if (runner.parent != holder) Undo.SetTransformParent(runner, holder, "Chay len xe");
        Undo.RecordObject(runner, "Chay len xe");
        runner.SetPositionAndRotation(a, face);
        foreach (Transform child in runner) // diễn viên nằm đúng gốc, quay theo hướng chạy
        {
            Undo.RecordObject(child, "Chay len xe");
            child.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        string tlPath = AssetDatabase.GetAssetPath(timeline);
        foreach (var old in timeline.GetOutputTracks().Where(t => t.name == "Chay len xe").ToList()) timeline.DeleteTrack(old);
        foreach (var old in AssetDatabase.LoadAllAssetsAtPath(tlPath).OfType<AnimationClip>().Where(c => c.name == "ChayLenXe"))
        { AssetDatabase.RemoveObjectFromAsset(old); Object.DestroyImmediate(old, true); }

        float t0 = (float)shot.start, t1 = (float)shot.end;
        var clip = new AnimationClip { name = "ChayLenXe", frameRate = 30 };
        Vector3 la = holder.InverseTransformPoint(a), lb = holder.InverseTransformPoint(b);
        Quaternion lr = Quaternion.Inverse(holder.rotation) * face;
        string[] props = { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" };
        float[] va = { la.x, la.y, la.z, lr.x, lr.y, lr.z, lr.w }, vb = { lb.x, lb.y, lb.z, lr.x, lr.y, lr.z, lr.w };
        for (int i = 0; i < props.Length; i++)
            clip.SetCurve("GE_ChayLenXe", typeof(Transform), props[i], AnimationCurve.Linear(t0, va[i], t1, vb[i]));
        AssetDatabase.AddObjectToAsset(clip, timeline);

        var animator = holder.GetComponent<Animator>();
        if (animator == null) animator = Undo.AddComponent<Animator>(holder.gameObject);
        var track = timeline.CreateTrack<AnimationTrack>(null, "Chay len xe");
        var tc = track.CreateClip(clip);
        tc.start = t0; tc.duration = t1 - t0; tc.clipIn = t0; // key tính theo giờ Timeline
        director.SetGenericBinding(track, animator);

        // ── camera ──
        Vector3 mid = (a + b) * 0.5f, perp = Vector3.Cross(Vector3.up, dir);
        // đứng ngang đường chạy, phía nào không bị tường che
        Vector3 c7 = mid + perp * 5f + Vector3.up * 1.6f;
        if (Physics.Linecast(mid + Vector3.up * 1.6f, c7, ~0, QueryTriggerInteraction.Ignore)) c7 = mid - perp * 5f + Vector3.up * 1.6f;
        Place(cam7, c7, b + Vector3.up * 0.9f);
        // ở cửa nhìn theo hướng xe sẽ chạy (quay đầu rồi đi ra xa nhà)
        Place(cam8, a - perp * 1.2f + Vector3.up * 1.5f, xe.position - xeFwd * 8f + Vector3.up * 0.8f);

        EditorUtility.SetDirty(timeline);
        EditorUtility.SetDirty(director);
        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(cutRoot.gameObject.scene);
        Selection.activeGameObject = cam7.gameObject;
        Debug.Log($"[ChayLenXe] Nhân vật chạy {Vector3.Distance(a, b):0.#}m từ cửa chính tới xe ({t0:0.#}s → {t1:0.#}s); đã đặt Cam_GE_7 và Cam_GE_8 ngoài sân.", runner);
    }

    static void Place(Transform cam, Vector3 pos, Vector3 lookAt)
    {
        Undo.RecordObject(cam, "Chay len xe");
        cam.SetPositionAndRotation(pos, Quaternion.LookRotation(lookAt - pos));
    }

    static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;
    }

    static Vector3 Ground(Vector3 p, float fallbackY)
    {
        if (Physics.Raycast(new Vector3(p.x, fallbackY + 3f, p.z), Vector3.down, out RaycastHit hit, 8f, ~0, QueryTriggerInteraction.Ignore))
            return hit.point;
        return new Vector3(p.x, fallbackY, p.z);
    }
}
