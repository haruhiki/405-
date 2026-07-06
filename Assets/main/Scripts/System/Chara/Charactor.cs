using UnityEngine;

public class Charactor : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Define _defineSO;
    [SerializeField] private Transform leftTarget;
    [SerializeField] private Transform rightTarget;

    [Header("移動設定")]
    [SerializeField, Min(0.01f)] private float moveDuration = 0.12f;
    [SerializeField, Min(0f)] private float moveSpeedMultiplier = 1.6f;

    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private float _moveElapsed;
    private bool _isMoving;

    private void Start()
    {
        if (_defineSO != null)
        {
            _defineSO.RightKeyEvent += OnRightKey;
            _defineSO.LeftKeyEvent += OnLeftKey;
        }

        _startPosition = transform.position;
        _targetPosition = transform.position;
        _moveElapsed = moveDuration;
    }

    private void OnDestroy()
    {
        if (_defineSO != null)
        {
            _defineSO.RightKeyEvent -= OnRightKey;
            _defineSO.LeftKeyEvent -= OnLeftKey;
        }
    }

    private void Update()
    {
        if (!_isMoving) return;

        _moveElapsed += Time.deltaTime * moveSpeedMultiplier;
        float t = Mathf.Clamp01(_moveElapsed / moveDuration);
        transform.position = Vector3.Lerp(_startPosition, _targetPosition, t);

        if (t >= 1f)
        {
            _isMoving = false;
            _startPosition = _targetPosition;
        }
    }

    private void OnRightKey()
    {
        if (rightTarget == null) return;
        StartMove(rightTarget.position);
    }

    private void OnLeftKey()
    {
        if (leftTarget == null) return;
        StartMove(leftTarget.position);
    }

    public void MoveToPoint(Vector3 destination)
    {
        StartMove(destination);
    }

    private void StartMove(Vector3 destination)
    {
        if (_isMoving && Vector3.Distance(destination, _targetPosition) < 0.01f) return;

        _startPosition = transform.position;
        _targetPosition = destination;
        _moveElapsed = 0f;
        _isMoving = true;
    }
}
