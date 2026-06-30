using UnityEngine;
using UnityEngine.UI;

public class CharactorHPBar : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private CharactorSO _charaSO;
    [SerializeField] private Slider _hpSlider;
    [SerializeField] private bool useGameSystemRuntime = true;

    private CharactorSO _activeSo;

    private void Start()
    {
        if (_hpSlider == null)
        {
            Debug.LogWarning("[CharactorHPBar] Slider が Inspector で設定されていません。");
            enabled = false;
            return;
        }

        if (useGameSystemRuntime && GameSystem1.Instance != null)
        {
            _activeSo = GameSystem1.Instance.RuntimeCharaSO ?? _charaSO;
        }
        else
        {
            _activeSo = _charaSO;
        }

        if (_activeSo == null)
        {
            Debug.LogWarning("[CharactorHPBar] CharactorSO が Inspector で設定されていません。");
            enabled = false;
            return;
        }

        _hpSlider.minValue = 0f;
        _hpSlider.maxValue = 1f;
        _hpSlider.value = _activeSo.CurrentHPNormalized;
        _activeSo.OnHPChanged += HandleHPChanged;
    }

    private void OnDestroy()
    {
        if (_activeSo != null)
        {
            _activeSo.OnHPChanged -= HandleHPChanged;
        }
    }

    private void HandleHPChanged(float normalizedValue)
    {
        if (_hpSlider != null)
        {
            _hpSlider.value = normalizedValue;
        }
    }

    public void SetCharactorSO(CharactorSO charaSO)
    {
        if (_activeSo != null)
        {
            _activeSo.OnHPChanged -= HandleHPChanged;
        }

        _activeSo = charaSO;

        if (_activeSo != null)
        {
            _activeSo.OnHPChanged += HandleHPChanged;
            _hpSlider.value = _activeSo.CurrentHPNormalized;
        }
    }
}
