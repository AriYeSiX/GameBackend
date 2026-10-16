using System;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace GameClient.Gameplay.Views
{
    /// <summary>
    /// A button that runs around an area. Its speed grows towards the end of the round,
    /// the direction changes randomly, and after every hit the button immediately runs away.
    /// Hierarchy requirements: the button is a child of Area, with centered anchors and pivot.
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(Button))]
    public class MovingButton : MonoBehaviour
    {
        [SerializeField] private RectTransform _area;
        [SerializeField] private float _startSpeed = 300f;   // px/s at the start of the round
        [SerializeField] private float _endSpeed = 1400f;    // px/s at the end of the round
        [SerializeField, Range(0f, 0.5f)] private float _speedJitter = 0.25f;
        [SerializeField] private Vector2 _retargetInterval = new(0.35f, 1.1f); // seconds before picking a new target

        private RectTransform _rect;
        private Button _button;

        private bool _running;
        private float _elapsed;
        private float _duration;
        private float _speed;
        private float _nextRetargetAt;
        private Vector2 _target;

        public event Action Clicked;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClick);
            _button.interactable = false;
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClick);
        }

        public void Begin(float durationSeconds)
        {
            _duration = Mathf.Max(0.01f, durationSeconds);
            _elapsed = 0f;
            _running = true;
            _button.interactable = true;
            PickTarget();
        }

        public void Stop()
        {
            _running = false;
            _button.interactable = false;
        }

        private void Update()
        {
            if (!_running)
                return;

            _elapsed += Time.unscaledDeltaTime;
            _rect.anchoredPosition = Vector2.MoveTowards(_rect.anchoredPosition, _target, _speed * Time.unscaledDeltaTime);

            if (_rect.anchoredPosition == _target || _elapsed >= _nextRetargetAt)
                PickTarget();
        }

        private void OnClick()
        {
            if (!_running)
                return;

            Clicked?.Invoke();
            PickTarget();
        }

        private void PickTarget()
        {
            var half = Vector2.Max(Vector2.zero, (_area.rect.size - _rect.rect.size) * 0.5f);
            _target = new Vector2(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y));

            var progress = Mathf.Clamp01(_elapsed / _duration);
            _speed = Mathf.Lerp(_startSpeed, _endSpeed, progress) * Random.Range(1f - _speedJitter, 1f + _speedJitter);
            _nextRetargetAt = _elapsed + Random.Range(_retargetInterval.x, _retargetInterval.y);
        }
    }
}
