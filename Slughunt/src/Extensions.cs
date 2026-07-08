using Menu.Remix.MixedUI;

namespace Slughunt;

public static class Extensions {
    extension(OpComboBox self) {
        public void ClearItems() => self.SetItems([new ListItem("", "------")]);
        public void SetItems(ListItem[] itemList) {
            self._itemList = itemList;
            self._ResetIndex();
            self.Change();
            self.value = null;
        }
    }
}
