using UnityEngine;
using UnityEngine.UI;

public class CharactorHPBar : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private CharactorSO _charaSO;
    [SerializeField] private Image _hpImage;
    [SerializeField] private bool useGameSystemRuntime = true;

    private CharactorSO _activeSo;

    private void Start()
    {
        if (_hpImage == null)
        {
            Debug.LogWarning("[CharactorHPBar] Image が Inspector で設定されていません。");
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

        UpdateHPBar(_activeSo.CurrentHPNormalized);
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
        UpdateHPBar(normalizedValue);
    }

    private void UpdateHPBar(float normalizedValue)
    {
        if (_hpImage != null)
        {
            _hpImage.fillAmount = Mathf.Clamp01(normalizedValue);
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
            UpdateHPBar(_activeSo.CurrentHPNormalized);
        }
    }
}
