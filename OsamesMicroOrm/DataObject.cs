namespace OsamesMicroOrm
{
    /// <summary>
    /// Class mère des classes de données.
    /// Les classes filles doivent porter une décoration OsamesMicroOrm.DatabaseMappingAttribute.
    /// </summary>
    public abstract class DataObject
    {
        protected string FullName;

        /// <summary>
        /// Retourne la valeur de GetType().FullName mais avec un cache pour ne le demander qu'une fois.
        /// </summary>
        public string ClassFullName {get { return FullName ?? (FullName = GetType().FullName); }
        }

    }
}
