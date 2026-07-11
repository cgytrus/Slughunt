using Menu.Remix.MixedUI;
using UnityEngine;

namespace Slughunt.Menu;

public class OpResourceSelector2(ConfigurableBase config, Vector2 pos, float width) :
    OpResourceSelector(config, pos, width) {
    public override void GrafUpdate(float timeStacker) {
        base.GrafUpdate(timeStacker);
        if (_rectList == null || _rectList.isHidden)
            return;
        myContainer.MoveToFront();
        for (int index = 0; index < 9; ++index)
            _rectList.sprites[index].alpha = 1f;
    }
}
