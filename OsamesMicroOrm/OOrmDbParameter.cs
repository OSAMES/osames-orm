using System.Data;

namespace OsamesMicroOrm
{
    /// <summary>
    /// Representation of an ADO.NET parameter. Used same way as an ADO.NET parameter but without depending on System.Data namespace in user code.
    /// It means more code overhead but is fine to deal with list of complex objects rather than list of values.
    /// </summary>
    public struct OOrmDbParameter
    {
        /// <summary>
        /// 
        /// </summary>
        internal string ParamName;

        /// <summary>
        /// 
        /// </summary>
        internal object ParamValue;

        /// <summary>
        /// 
        /// </summary>
        internal ParameterDirection ParamDirection;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name_">Name</param>
        /// <param name="value_">Value</param>
        /// <param name="direction_">ADO.NET parameter direction</param>
        internal OOrmDbParameter(string name_, object value_, ParameterDirection direction_)
        {
            ParamName = name_;
            ParamValue = value_;
            ParamDirection = direction_;
        }
        /// <summary>
        /// Constructor with default "in" direction.
        /// </summary>
        /// <param name="name_">Name</param>
        /// <param name="value_">Value</param>
        internal OOrmDbParameter(string name_, object value_)
        {
            ParamName = name_;
            ParamValue = value_;
            ParamDirection = ParameterDirection.Input;
        }
    }
}
