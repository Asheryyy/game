using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Hiện giá trị thanh trượt thành số bên cạnh (vd: 0.75 → "75").
/// Gắn vào object Text đặt cạnh Slider, kéo Slider vào ô Slider.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class SliderValueText : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [Tooltip("Nhân giá trị lên (slider 0–1 → 0–100 thì để 100)")]
    [SerializeField] private float multiplier = 100f;

    TMP_Text label;

    void Awake() => label = GetComponent<TMP_Text>();

    void OnEnable()
    {
        if (slider == null) return;
        slider.onValueChanged.AddListener(Refresh);
        Refresh(slider.value);
    }

    void OnDisable()
    {
        if (slider != null) slider.onValueChanged.RemoveListener(Refresh);
    }

    void Refresh(float v) => label.text = Mathf.RoundToInt(v * multiplier).ToString();
}
