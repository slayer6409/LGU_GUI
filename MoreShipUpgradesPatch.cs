using HarmonyLib;
using MoreShipUpgrades.Managers;
using Unity.Netcode;

namespace LGUGui;

public class MoreShipUpgradesPatch
{
    [HarmonyPatch(typeof(LguStore), nameof(LguStore.HandleUpgradeClientRpc))]
    class Patch_LGU_HandleUpgrade
    {
        static void Postfix(string name, bool increment)
        {
            new PurchaseMenu().refresh();
        }
    }
    [HarmonyPatch(typeof(CurrencyManager), nameof(CurrencyManager.TradePlayerCreditsClientRpc))]
    class Patch_LGU_HandleTrade
    {
        static void Postfix(ulong traderClientId, int playerCreditAmount, ClientRpcParams clientRpcParams)
        {
            new PurchaseMenu().refresh(true);
        }
    }
}