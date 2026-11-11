using AlienFarms.Core;

namespace AlienFarms.Player
{
    /// <summary>
    /// Finds the rig of a player. What shared menus (pause, results) need: locally every joined player; online only the
    /// avatar this machine owns.
    /// </summary>
    public interface IPlayerRigLookup
    {
        bool TryGetRig(PlayerId id, out PlayerRig rig);
    }
}
