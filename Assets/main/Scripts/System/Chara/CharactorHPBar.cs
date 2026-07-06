using UnityEngine;
using UnityEngine.UI;

public class CharactorHPBar : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private CharactorSO _charaSO;
    [SerializeField] private Image _hpImage;
    [SerializeField] private bool useGameSystemRuntime = true;

    private CharactorSO _activeSo;
    private bool _isBound;
    private float _fullWidth;

    private void OnEnable()
    {
        TryBindToActiveCharacter();
    }

    private void Start()
    {
        if (_hpImage != null)
        {
            _hpImage.type = Image.Type.Filled;
            _hpImage.fillMethod = Image.FillMethod.Horizontal;
            _hpImage.fillOrigin = 0;
            _hpImage.fillClockwise = false;
        }
        TryBindToActiveCharacter();
    }

    private void Update()
    {
        if (!_isBound)
        {
            TryBindToActiveCharacter();
            return;
        }

        if (useGameSystemRuntime && GameSystem1.Instance != null)
        {
            var runtimeSo = GameSystem1.Instance.RuntimeCharaSO;
            if (runtimeSo != null && _activeSo != runtimeSo)
            {
                Debug.Log("[CharactorHPBar] Rebinding to runtime character SO");
                SetCharactorSO(runtimeSo);
            }
        }
    }

    private void OnDisable()
    {
        if (_activeSo != null)
        {
            _activeSo.OnHPChanged -= HandleHPChanged;
            _activeSo = null;
        }

        _isBound = false;
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
        Debug.Log($"[CharactorHPBar] HandleHPChanged value={normalizedValue:F3} boundTo={( _activeSo != null ? _activeSo.name : "null")}");
        UpdateHPBar(normalizedValue);
    }

    private void UpdateHPBar(float normalizedValue)
    {
        if (_hpImage == null)
        {
            Debug.LogWarning("[CharactorHPBar] _hpImage is not assigned.");
            return;
        }

        _hpImage.fillAmount = Mathf.Clamp01(normalizedValue);
    }

    private void TryBindToActiveCharacter()
    {
        if (_isBound && _activeSo != null) return;

        if (useGameSystemRuntime && GameSystem1.Instance != null)
        {
            Debug.Log("[CharactorHPBar] Trying to bind to GameSystem runtime chara SO");
            SetCharactorSO(GameSystem1.Instance.RuntimeCharaSO ?? _charaSO);
            return;
        }

        Debug.Log("[CharactorHPBar] Trying to bind to inspector chara SO");
        SetCharactorSO(_charaSO);
    }

    public void SetCharactorSO(CharactorSO charaSO)
    {
        if (_activeSo != null && _activeSo != charaSO)
        {
            _activeSo.OnHPChanged -= HandleHPChanged;
        }

        _activeSo = charaSO;
        _isBound = _activeSo != null;

        if (_activeSo != null)
        {
            _activeSo.OnHPChanged += HandleHPChanged;
            UpdateHPBar(_activeSo.CurrentHPNormalized);
        }
    }
}
