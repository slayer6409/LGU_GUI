using System;
using System.Linq;
using MoreShipUpgrades.API;
using MoreShipUpgrades.Managers;
using MoreShipUpgrades.UI.TerminalNodes;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LGUGui;

public class PurchaseMenu
{
    private static GameObject menu;
    public static TMP_Text currencyText;
    public static bool isTrackedYet = false;
    public static Transform mainScrollContent;
    public static Transform ListUpgrades;
    public static Transform PurchaseUpgrade;
    public static Color NormalTextColor = new Color(0.02942256f, 0.8113208f, 0f, 1f); // 08CF00
    public static Color RedTextColor = new Color(0.7169812f, 0.03853131f, 0f, 1f); // B70A00
    public static Color SaleColor = new Color(0.00f, 0.7f, 1f, 1f);

    public static CustomTerminalNode currentSelection;
    public static string location;
    public static void initMenu()
    {
        if(menu!=null) return;
        Plugin.ExtendedLogging("Initializing LGU Menu");
        menu = GameObject.Instantiate(Plugin.PurchaseMenuPrefab);
        MenuController controller = menu.AddComponent<MenuController>();
        controller.PurchaseMenu = menu;
        currencyText = menu.transform.Find("Background/TotalPoints").GetComponent<TMP_Text>();
        currencyText.text = MoreShipUpgrades.Managers.CurrencyManager.Instance.CurrencyAmount.ToString();
        Button SharedButton = menu.transform.Find("Background/SharedUpgrades").GetComponent<Button>();
        Button IndividualButton = menu.transform.Find("Background/IndividualUpgrades").GetComponent<Button>();
        Button TradeButton = menu.transform.Find("Background/TradePoints").GetComponent<Button>();
        SharedButton.onClick.AddListener(() => { showUpgrades(true); });
        IndividualButton.onClick.AddListener(() => { showUpgrades(false); });
        TradeButton.onClick.AddListener(showTradeGui);
        mainScrollContent = menu.transform.Find("Background/ListUpgrades/Scroll View/Viewport/Content");
        ListUpgrades = menu.transform.Find("Background/ListUpgrades");
        PurchaseUpgrade = menu.transform.Find("Background/PurchaseUpgrade");
        Cursor.visible = true;
        Plugin.ExtendedLogging("Unlocking Cursor");
        Cursor.lockState = CursorLockMode.None;
        StartOfRound.Instance.localPlayerController.quickMenuManager.isMenuOpen = true;
        Button xButton = menu.transform.Find("Background/xButton").GetComponent<Button>();
        xButton.onClick.AddListener(() => { controller.CloseMenu();});
    }
    public static void LogTransformTree(Transform t, string prefix = "")
    {
        Plugin.ExtendedLogging(prefix + t.name);
        foreach (Transform child in t)
        {
            LogTransformTree(child, prefix + t.name + "/");
        }
    }
    public static void clearMainViewport()
    {
        foreach (Transform obj in mainScrollContent)
        {
            Object.Destroy(obj.gameObject);
        }
    }

    public static void swapTwo(bool purchase)
    {
        PurchaseUpgrade.gameObject.SetActive(purchase);
        ListUpgrades.gameObject.SetActive(!purchase);
    }
    public static void showUpgrades(bool shared)
    {
        currentSelection = null;
        location = nameof(showUpgrades) + "," + shared;
        swapTwo(false);
        int currency = CurrencyManager.Instance.CurrencyAmount;
        currencyText.text = currency.ToString();
        clearMainViewport();

        var allUpgrades = MoreShipUpgrades.API.UpgradeApi.GetUpgradeNodes();
        var upgradesToShow = allUpgrades
            .Where(x => x.SharedUpgrade == shared)
            .OrderBy(x => x.Name);

        foreach (var upgrade in upgradesToShow)
        {
            int price = upgrade.GetCurrentPrice();
            int effectivePrice = upgrade.AlternateCurrency
                ? CurrencyManager.Instance.GetCurrencyAmountFromCredits(price)
                : price;

            int currentLevel = upgrade.GetCurrentLevel();
            int maxLevel = currentLevel + upgrade.GetRemainingLevels();
            bool maxed = upgrade.GetRemainingLevels() == 0;
            bool hasEnough = effectivePrice <= currency;

            Plugin.ExtendedLogging($"Init Upgrade: {upgrade.Name}");

            var upgradeThing = GameObject.Instantiate(Plugin.UpgradeButton, mainScrollContent);
            var upgradeButton = upgradeThing.transform.Find("Button").GetComponent<Button>();

            var buttonText = upgradeButton.transform.Find("Text (TMP)").GetComponent<TMP_Text>();
            buttonText.text = $"{upgrade.Name} : {currentLevel}/{maxLevel}";
            buttonText.color = hasEnough ? NormalTextColor : RedTextColor;
            if(hasEnough && !maxed && upgrade.SalePercentage<1) buttonText.color = SaleColor;

            var displayText = upgradeThing.transform.Find("DisplayText").GetComponent<TMP_Text>();
            displayText.color = hasEnough ? NormalTextColor : RedTextColor;
            if(hasEnough && !maxed && upgrade.SalePercentage<1) displayText.color = SaleColor;

            var pointsRequired = upgradeThing.transform.Find("DisplayText/PointsRequired").GetComponent<TMP_Text>();
            if (maxed)
            {
                pointsRequired.text = "Maxed";
            }
            else if (upgrade.SalePercentage < 1)
            {
                float salePercent = (1f - upgrade.SalePercentage) * 100f;
                pointsRequired.text = $"{effectivePrice} ({Mathf.RoundToInt(salePercent)}% off)";
            }
            else
            {
                pointsRequired.text = effectivePrice.ToString();
            }
            pointsRequired.color = hasEnough ? NormalTextColor : RedTextColor;
            if(hasEnough && !maxed && upgrade.SalePercentage<1) pointsRequired.color = SaleColor;

            upgradeButton.onClick.AddListener(() =>
            {
                swapTwo(true);
                showUpgradeGui(upgrade);
            });
        }
    }

    public void refresh(bool trade = false)
    {
        if(menu == null) return;
        if (trade)
        {
            if(location==nameof(showUpgrades)+","+false) showUpgrades(false);
            if(location==nameof(showTradeGui)) showTradeGui();
        }
        if (location == nameof(showUpgrades)+","+true) showUpgrades(true);
        if (location == nameof(showUpgradeGui) && currentSelection!=null) showUpgradeGui(currentSelection);
    }
    public static void showUpgradeGui(CustomTerminalNode currentUpgrade)
    {
        currentSelection = currentUpgrade;
        location = nameof(showUpgradeGui);
        var description = menu.transform.Find("Background/PurchaseUpgrade/Text (TMP)").GetComponent<TextMeshProUGUI>();
        description.text = currentUpgrade.Description;
        Button purchaseButton = menu.transform.Find("Background/PurchaseUpgrade/PurchaseButton").GetComponent<Button>();
        Button backButton = menu.transform.Find("Background/PurchaseUpgrade/BackButton").GetComponent<Button>();
        int price = currentUpgrade.GetCurrentPrice();
        int effectivePrice = currentUpgrade.AlternateCurrency
            ? CurrencyManager.Instance.GetCurrencyAmountFromCredits(price)
            : price;

        string priceToString = price != int.MaxValue ? "Purchase: " + effectivePrice.ToString() : "Maxed";
        purchaseButton.interactable = CurrencyManager.Instance.CurrencyAmount >= effectivePrice;
        var purchaseButtonText = purchaseButton.transform.Find("Text (TMP)").GetComponent<TMP_Text>();
        purchaseButtonText.text = priceToString;
        purchaseButtonText.color = purchaseButton.interactable ? NormalTextColor : RedTextColor;
        
        
        purchaseButton.onClick.RemoveAllListeners();
        purchaseButton.onClick.AddListener(() =>
        {
            Plugin.ExtendedLogging($"Purchasing Upgrade: {currentUpgrade.Name}");

            if (currentUpgrade.AlternateCurrency)
                CurrencyManager.Instance.CurrencyAmount -= effectivePrice;
            UpgradeApi.TriggerUpgradeRankup(currentUpgrade);
            currencyText.text = CurrencyManager.Instance.CurrencyAmount.ToString();
            showUpgradeGui(currentUpgrade);
        });
        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(() =>
        {
            showUpgrades(currentUpgrade.SharedUpgrade);
        });
    }

    public static void showTradeGui()
    {
        
        currentSelection = null;
        location = nameof(showTradeGui);
        swapTwo(false);
        int currency = CurrencyManager.Instance.CurrencyAmount;
        currencyText.text = currency.ToString();
        clearMainViewport();
        foreach (var player in StartOfRound.Instance.allPlayerScripts.OrderBy(p => p.playerUsername))
        {
            if (StartOfRound.Instance.localPlayerController==player) continue;
            if (!player.isPlayerControlled && !player.isPlayerDead) continue;
            var TradePrefab = GameObject.Instantiate(Plugin.TradePrefab, mainScrollContent);
            var tradeButton = TradePrefab.transform.Find("Button").GetComponent<Button>();
            var tradeInput = TradePrefab.transform.Find("InputField (TMP)").GetComponent<TMP_InputField>();
            var userNameText = TradePrefab.transform.Find("InputField (TMP)/DisplayText").GetComponent<TMP_Text>();
            userNameText.text = player.playerUsername;
            bool isUpdating = false;
            tradeButton.interactable = false;
            var targetPlayerID = player.actualClientId;
            tradeInput.onValueChanged.AddListener((string text) =>
            {
                if (isUpdating) return;

                if (int.TryParse(text, out int amount))
                {
                    int clamps = Mathf.Clamp(amount, 0, currency);

                    if (clamps.ToString() != text)
                    {
                        isUpdating = true;
                        tradeInput.text = clamps.ToString();
                        isUpdating = false;
                    }

                    tradeButton.interactable = clamps != 0;
                }
                else
                {
                    isUpdating = true;
                    tradeInput.text = "0";
                    isUpdating = false;
                }
                
            });
            tradeButton.onClick.AddListener(() =>
            {
                CurrencyManager.Instance.CurrencyAmount-=int.Parse(tradeInput.text);
                CurrencyManager.Instance.TradePlayerCreditsServerRpc(targetPlayerID, int.Parse(tradeInput.text));
                showTradeGui();
            });
        }
    }
}

public class MenuController : MonoBehaviour
{
    public GameObject PurchaseMenu;
    private InputAction escAction;

    private void Awake()
    {
        Plugin.ExtendedLogging("Setting up Escape Action");
        escAction =  new InputAction(binding: "<Keyboard>/escape");
        escAction.performed += ctx => CloseMenu();
        escAction.Enable();
    }
    
    //OnDestroy in case something happens to it properly closing
    private void OnDestroy()
    {
        Plugin.ExtendedLogging("OnDestroy");
        escAction.Disable();
        escAction.Dispose(); 
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        StartOfRound.Instance.localPlayerController.quickMenuManager.isMenuOpen = false;
    }
    public void CloseMenu()
    {
        Plugin.ExtendedLogging("Closing Menu");
        if (PurchaseMenu != null)
        {
            escAction.Disable();
            escAction.Dispose(); 
            Destroy(PurchaseMenu);
            PurchaseMenu = null;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            StartOfRound.Instance.localPlayerController.quickMenuManager.isMenuOpen = false;
        }
    }
}