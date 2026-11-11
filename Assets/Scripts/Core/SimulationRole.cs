namespace AlienFarms.Core
{
    /// <summary>
    /// Who runs a simulated component. Authority runs the gameplay logic (local mode and the online server); Replica
    /// only shows state applied from outside (online clients). Every target defaults to Authority.
    /// </summary>
    public enum SimulationRole
    {
        Authority = 0,
        Replica = 1
    }
}
