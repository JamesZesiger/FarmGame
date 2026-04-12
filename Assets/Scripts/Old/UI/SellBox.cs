using UnityEngine;
using System.Collections.Generic;


public class SellBox : Container
{
    public Wallet wallet;
    private bool isActive;
    private int value;

    UIManager ActiveUIManager => UIManager.Instance;
    Wallet ActiveWallet => ActiveUIManager != null && ActiveUIManager.ResolveCurrentPlayer() != null
        ? ActiveUIManager.ResolveCurrentPlayer().PlayerWallet
        : wallet;

    void Awake()
    {
        isActive = false;
    }

    bool IsThisSellBoxOpen(UIManager uiManager)
    {
        return uiManager != null
            && uiManager.containerUI != null
            && uiManager.containerUI.gameObject.activeSelf
            && uiManager.currentContainerInventory == inventory;
    }

    void Update()
    {
        UIManager uiManager = ActiveUIManager;
        if (uiManager == null || uiManager.containerUI == null)
            return;

        if (isActive)
        {
            isActive = IsThisSellBoxOpen(uiManager);
            if (!isActive)
            {
                value = 0;
                for (int i = 0; i < inventory.items.Count; i++)
                {
                    value+= inventory.items[i].value;
                }
                ActiveWallet?.UpdateWallet(value);
                value = 0;
                inventory.items = new List<InventorySlot>();

            }
        }
        if (!isActive)
            isActive = IsThisSellBoxOpen(uiManager);

    }
}
