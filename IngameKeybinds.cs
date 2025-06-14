using LethalCompanyInputUtils.Api;
using UnityEngine.InputSystem;

namespace LGUGui;

public class IngameKeybinds : LcInputActions
{
    [InputAction("<Keyboard>/p", Name="PurchaseMenu")]
    public InputAction PurchaseMenu { get; set; } = null!;
}