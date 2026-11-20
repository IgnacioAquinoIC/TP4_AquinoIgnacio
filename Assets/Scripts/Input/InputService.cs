using UnityEngine;
using UnityEngine.InputSystem;

namespace RpgBattle
{
    /// Single input abstraction: keyboard/mouse, DualSense and XInput pads share the same actions.
    [DefaultExecutionOrder(-200)]
    public class InputService : MonoBehaviour
    {
        InputAction move, confirm, cancel, next, prev;
        public Vector2Int RawNav;
        public bool Confirm, Cancel, Next, Prev, ConfirmByPointer, LeftPressed, LeftHeld;
        public Vector2 PointerPos;
        public bool PointerMoved;
        float nextRepeat;
        Vector2Int lastNav;
        Vector2 lastPointer;

        void Awake()
        {
            move = new InputAction("Move", InputActionType.Value);
            move.AddBinding("<Gamepad>/leftStick");
            move.AddBinding("<Gamepad>/dpad");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            confirm = new InputAction("Confirm", InputActionType.Button);
            confirm.AddBinding("<Gamepad>/buttonSouth"); confirm.AddBinding("<Keyboard>/enter"); confirm.AddBinding("<Keyboard>/space"); confirm.AddBinding("<Mouse>/leftButton");
            cancel = new InputAction("Cancel", InputActionType.Button);
            cancel.AddBinding("<Gamepad>/buttonEast"); cancel.AddBinding("<Keyboard>/escape"); cancel.AddBinding("<Keyboard>/backspace"); cancel.AddBinding("<Mouse>/rightButton");
            next = new InputAction("Next", InputActionType.Button);
            next.AddBinding("<Gamepad>/rightShoulder"); next.AddBinding("<Keyboard>/e"); next.AddBinding("<Keyboard>/tab");
            prev = new InputAction("Prev", InputActionType.Button);
            prev.AddBinding("<Gamepad>/leftShoulder"); prev.AddBinding("<Keyboard>/q");
            move.Enable(); confirm.Enable(); cancel.Enable(); next.Enable(); prev.Enable();
        }

        void OnDestroy()
        {
            move?.Dispose(); confirm?.Dispose(); cancel?.Dispose(); next?.Dispose(); prev?.Dispose();
        }

        void Update()
        {
            var raw = move.ReadValue<Vector2>();
            var q = new Vector2Int(Mathf.Abs(raw.x) > 0.5f ? (int)Mathf.Sign(raw.x) : 0, Mathf.Abs(raw.y) > 0.5f ? (int)Mathf.Sign(raw.y) : 0);
            RawNav = Vector2Int.zero;
            if (q != Vector2Int.zero && (q != lastNav || Time.unscaledTime >= nextRepeat))
            {
                RawNav = q;
                nextRepeat = Time.unscaledTime + (q != lastNav ? 0.3f : 0.11f);
            }
            lastNav = q;

            Confirm = confirm.WasPressedThisFrame();
            Cancel = cancel.WasPressedThisFrame();
            Next = next.WasPressedThisFrame();
            Prev = prev.WasPressedThisFrame();

            var mouse = Mouse.current;
            if (mouse != null)
            {
                PointerPos = mouse.position.ReadValue();
                PointerMoved = (PointerPos - lastPointer).sqrMagnitude > 1f;
                lastPointer = PointerPos;
                LeftPressed = mouse.leftButton.wasPressedThisFrame;
                LeftHeld = mouse.leftButton.isPressed;
                ConfirmByPointer = LeftPressed;
            }
            else { PointerMoved = false; LeftPressed = LeftHeld = ConfirmByPointer = false; }
        }

        /// Maps a screen-relative direction to a grid delta (camera yaw 45 degrees: screen-up = +x,+z).
        public static Vector2Int ScreenToGrid(Vector2Int nav)
            => new Vector2Int(Mathf.Clamp(nav.y + nav.x, -1, 1), Mathf.Clamp(nav.y - nav.x, -1, 1));

        public bool PointerCell(Camera cam, BattleGrid g, out Vector2Int cell)
        {
            cell = default;
            if (cam == null) return false;
            var ray = cam.ScreenPointToRay(PointerPos);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out float d)) return false;
            cell = g.ToCell(ray.GetPoint(d));
            return g.InBounds(cell);
        }
    }
}
