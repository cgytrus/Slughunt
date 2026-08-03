using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Menu;
using RainMeadow;
using UnityEngine;

namespace Slughunt.Menu;

public class WorldFiltersLabel : ProperlyAlignedMenuLabel {
    private static Lobby lobby => OnlineManager.lobby;
    private static SlughuntGameMode gameMode => (SlughuntGameMode)lobby.gameMode;
    private static LobbyData lobbyData => lobby.GetData<LobbyData>();

    public WorldFiltersLabel(global::Menu.Menu menu, MenuObject owner, Vector2 pos) :
        base(menu, owner, "penis", pos, new Vector2(200f, 400f), false) {
        label.anchorY = 1.0f;
    }

    private readonly StringBuilder _filtersLabelBuilder = new();
    public void UpdateText() {
        _filtersLabelBuilder.Clear();
        AppendFiltersSet("Selected shelters:", lobbyData.shelters, IsRoomInRegion);
        AppendFiltersSet("Locked gates:", lobbyData.lockedGates, IsRoomInRegion);
        AppendFiltersSet("Locked shortcuts:", lobbyData.lockedShortcuts, IsShortcutInRegion);
        label.text = _filtersLabelBuilder.ToString();
    }

    private readonly List<Region> _orderedRegions = Region.GetFullRegionOrder(null)
        .Join(Region.LoadAllRegions(null, null), x => x, x => x.name, (_, x) => x)
        .ToList();

    private readonly HashSet<string> _leftoverStrings = [];
    private void AppendFiltersSet<TItem>(string title, HashSet<TItem> set, Func<TItem, Region, bool> isItemInRegion)
        where TItem : notnull {
        if (set.Count == 0)
            return;
        _filtersLabelBuilder.AppendLine(title);
        _leftoverStrings.Clear();
        foreach (TItem item in set)
            _leftoverStrings.Add(item.ToString());
        foreach (Region region in _orderedRegions) {
            bool appendedRegion = false;
            foreach (string item in set.Where(x => isItemInRegion(x, region)).Select(x => x.ToString())) {
                if (!appendedRegion) {
                    _filtersLabelBuilder.Append("  ");
                    _filtersLabelBuilder.Append(Region.GetRegionFullName(region.name, lobbyData.character));
                    _filtersLabelBuilder.Append(" (");
                    _filtersLabelBuilder.Append(region.name);
                    _filtersLabelBuilder.AppendLine("):");
                    appendedRegion = true;
                }
                _filtersLabelBuilder.Append("    ");
                _filtersLabelBuilder.AppendLine(item);
                _leftoverStrings.Remove(item);
            }
        }
        foreach (string x in _leftoverStrings) {
            _filtersLabelBuilder.Append("  ");
            _filtersLabelBuilder.AppendLine(x);
        }
        _filtersLabelBuilder.AppendLine();
    }

    // TODO: better gate checking
    private static bool IsRoomInRegion(string room, Region region) {
        if (RainWorld.roomNameToIndex.TryGetValue(room, out int index) && region.IsRoomInRegion(index))
            return true;
        if (!CmpOrd(room, 0, "GATE_"))
            return false;
        return CmpOrdPostfix(room, "GATE_".Length, region.name, '_') ||
            CmpOrdPrefix(room, room.Length - region.name.Length - 1, '_', region.name);
    }
    private static bool CmpOrd(string a, int indexA, string b) =>
        string.CompareOrdinal(a, indexA, b, 0, b.Length) == 0;
    private static bool CmpOrdPostfix(string a, int indexA, string b, char c) =>
        indexA + b.Length < a.Length && CmpOrd(a, indexA, b) && a[indexA + b.Length] == c;
    private static bool CmpOrdPrefix(string a, int indexA, char c, string b) =>
        indexA >= 0 && CmpOrd(a, indexA + 1, b) && a[indexA] == c;

    private static bool IsShortcutInRegion(LobbyData.Shortcut shortcut, Region region) =>
        IsRoomInRegion(shortcut.a, region) || IsRoomInRegion(shortcut.b, region);
}
