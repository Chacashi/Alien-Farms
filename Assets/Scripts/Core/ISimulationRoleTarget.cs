namespace AlienFarms.Core
{
    /// <summary>A component whose simulation can be switched between Authority (default) and Replica.</summary>
    public interface ISimulationRoleTarget
    {
        SimulationRole Role { get; }

        void SetSimulationRole(SimulationRole role);
    }
}
