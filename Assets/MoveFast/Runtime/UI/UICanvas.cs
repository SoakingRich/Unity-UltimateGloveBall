// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Oculus.Interaction.MoveFast
{
    /// <summary>
    /// Wraps a Canvas/CanvasGroup to add basic show/hide functionality
    /// via Show and ShowAsync methods, used to show or hide UI
    /// An IUICanvasAnimator component can be added to override the default fade animation
    /// </summary>
    [RequireComponent(typeof(Canvas)), RequireComponent(typeof(CanvasGroup))]
    sealed class UICanvas : MonoBehaviour, IActiveState
    {
        [SerializeField]
        private bool _show = true;
        [SerializeField]
        float _duration = 0.3f;
        //[SerializeField]
        bool _controlCanvasEnabled = true;

        Canvas _canvas;
        CanvasGroup _canvasGroup;
        IUICanvasAnimator _canvasAnimator;
        TaskCompletionSource<bool> _canvasAnimatorTask;
        private bool _started;

        public bool Active => _show;

        public event Action onChange;

        public float Duration
        {
            get => _duration;
            set => _duration = value;
        }

        public bool IsShown
        {
            get => _show;
            set => Show(value);
        }

        private void Awake()
        {
            //instantly set the initial state
            var duration = _duration;
            _duration = 0;
            Show(!(_show = !_show));
            _duration = duration;
        }

        public async void Show(bool value)
        {
            await ShowAsync(value);
        }

        public async Task<bool> ShowAsync(bool show)
        {
            if (_show == show) return true;

            _show = show;

            if (_started)
            {
                onChange?.Invoke();
            }

            EnsureReferences();
            TweenRunner.Kill(_canvasGroup);

            var hasAnimator = _canvasAnimator != null;
            if (hasAnimator && _canvasAnimatorTask != null)
            {
                var task = _canvasAnimatorTask;
                _canvasAnimatorTask = null;
                _canvasAnimator.Cancel();
                task.TrySetResult(false);
            }

            if (show && _controlCanvasEnabled) _canvas.enabled = true;
            _canvasGroup.blocksRaycasts = false;

            bool finished = true;

            if (hasAnimator && _duration > 0)
            {
                var tween = new TaskCompletionSource<bool>();
                _canvasAnimatorTask = tween;
                await Task.WhenAny(_canvasAnimator.Animate(show), tween.Task);
                bool cancelled = _canvasAnimatorTask != tween;
                if (!cancelled)
                {
                    _canvasAnimatorTask = null;
                }
                tween.TrySetResult(!cancelled);
                finished = tween.Task.Result;
            }
            else
            {
                float duration = Mathf.Abs(_canvasGroup.alpha - (show ? 1f : 0f)) * _duration;

                if (duration > 0)
                {
                    finished = await TweenRunner.Tween(_canvasGroup.alpha, show ? 1f : 0f, duration, x => _canvasGroup.alpha = x)
                        .SetID(_canvasGroup)
                        .ToTask();
                }
            }

            if (finished)
            {
                if (!hasAnimator) _canvasGroup.alpha = show ? 1f : 0f;
                if (!show && _controlCanvasEnabled) _canvas.enabled = false;
                _canvasGroup.blocksRaycasts = show;
            }

            return finished;
        }

        private void EnsureReferences()
        {
            if (_started) { return; }
            _started = true;

            _canvas = GetComponent<Canvas>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasAnimator = GetComponent<IUICanvasAnimator>();
        }
    }

    public interface IUICanvasAnimator
    {
        Task Animate(bool show);
        void Cancel();
    }
}
