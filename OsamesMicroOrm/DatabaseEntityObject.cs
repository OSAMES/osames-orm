namespace OsamesMicroOrm
{
    /// <summary>
    /// Class mère des classes de données.
    /// Les classes filles doivent porter une décoration OsamesMicroOrm.DatabaseMappingAttribute.
    /// </summary>
    public abstract class DatabaseEntityObject : IDatabaseEntityObject
    {
        private string FullName;

        /// <summary>
        /// Retourne la valeur de GetType().FullName mais avec un cache pour ne construire sa valeur qu'une fois.
        /// </summary>
        public string UniqueName {get { return FullName ?? (FullName = GetType().FullName); }
        }

    }
}
