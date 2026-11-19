using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Rẽ nhánh theo manh mối/lựa chọn: gọi Run() → có clueId thì chạy If Found, chưa có thì If Not Found.
/// Vd: CuaSo_Thoat → Run; tin_tuong → gọi điện (Good End), không → Cutscene_BadEnd1.
/// </summary>
public class ClueBranch : MonoBehaviour
{
    [SerializeField] private string clueId = "tin_tuong";
    [SerializeField] private UnityEvent ifFound;
    [SerializeField] private UnityEvent ifNotFound;

    public void Run() => (ClueItem.HasFound(clueId) ? ifFound : ifNotFound).Invoke();

    // clue này không gắn với ClueItem nào → tự xóa khi chơi lại, tránh lần sau tự vào Good End
    void OnDestroy() => ClueItem.ForgetFound(clueId);
}
