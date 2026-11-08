using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace Mystery.UI
{
    /// <summary>Desktop TMP IME bridge for the project's existing Input System-only setting.</summary>
    public sealed class DialogueImeInput : BaseInput
    {
        private Keyboard keyboard;
        private string composition = "";
        private Vector2 cursor;
        private IMECompositionMode mode;
        protected override void OnEnable()
        {
            base.OnEnable();
            // BaseInputModule.inputOverride is not serialized by Unity. Rebind on load.
            var module = GetComponent<BaseInputModule>();
            if (module != null) module.inputOverride = this;
            BindKeyboard();
        }
        protected override void OnDisable()
        {
            if (keyboard != null) keyboard.onIMECompositionChange -= OnComposition;
            var module = GetComponent<BaseInputModule>();
            if (module != null && module.inputOverride == this) module.inputOverride = null;
            keyboard = null; composition = "";
            base.OnDisable();
        }
        private void Update() { if (keyboard != Keyboard.current) BindKeyboard(); }
        private void BindKeyboard()
        {
            if (keyboard != null) keyboard.onIMECompositionChange -= OnComposition;
            keyboard = Keyboard.current; composition = "";
            if (keyboard == null) return;
            keyboard.onIMECompositionChange += OnComposition;
            keyboard.SetIMEEnabled(mode == IMECompositionMode.On);
        }
        private void OnComposition(IMECompositionString value) => composition = value.ToString();
        public override string compositionString => composition;
        public override IMECompositionMode imeCompositionMode
        {
            get => mode;
            set { mode = value; if (keyboard != null) keyboard.SetIMEEnabled(value == IMECompositionMode.On); }
        }
        public override Vector2 compositionCursorPos
        {
            get => cursor;
            set { cursor = value; if (keyboard != null) keyboard.SetIMECursorPosition(value); }
        }
        public override bool touchSupported => false;
        public override int touchCount => 0;
        public override bool mousePresent => Mouse.current != null;
        public override Vector2 mousePosition => Mouse.current?.position.ReadValue() ?? Vector2.zero;
        public override Vector2 mouseScrollDelta => Mouse.current?.scroll.ReadValue() ?? Vector2.zero;
        private static ButtonControl MouseButton(int button) => Mouse.current == null ? null : button switch
        { 0 => Mouse.current.leftButton, 1 => Mouse.current.rightButton, 2 => Mouse.current.middleButton, _ => null };
        public override bool GetMouseButton(int button) => MouseButton(button)?.isPressed ?? false;
        public override bool GetMouseButtonDown(int button) => MouseButton(button)?.wasPressedThisFrame ?? false;
        public override bool GetMouseButtonUp(int button) => MouseButton(button)?.wasReleasedThisFrame ?? false;
        public override float GetAxisRaw(string axisName) => 0;
        public override bool GetButtonDown(string buttonName) => false;
    }
}
