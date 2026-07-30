namespace Slughunt;

public readonly record struct Participant(PlayerRole role, int stun, bool dead, LobbyData lobbyData) {
    // is the player even actually participating in the game?
    public bool participating {
        get {
            if (role is not PlayerRole.Hunter and not PlayerRole.Hider)
                return false;
            if (!dead)
                return true;
            // death is permanent in non-endless
            if (!lobbyData.endless)
                return false;
            return true;
        }
    }

    // can a catch actually take place in the current state of the hunter and the hider involved?
    public bool CanCatch(Participant hider) {
        Participant hunter = this;
        if (hunter.role != PlayerRole.Hunter)
            return false;
        if (hider.role != PlayerRole.Hider)
            return false;

        // different lobbies??
        // TODO: should i make this an assert?
        if (hunter.lobbyData != hider.lobbyData)
            return false;

        if (hunter.stun > 0 || hunter.dead)
            return false;

        if (!hider.dead)
            return true;

        // death is permanent in non-endless, no point in catch
        // replace with participating checks if those actually get more complex at some point
        if (!lobbyData.endless)
            return false;

        // if hider dies on catch but is already dead, no point in catch
        if (lobbyData.ruleset.hiderCatch == Rules.OnCatch.Death)
            return false;

        return true;
    }
}
