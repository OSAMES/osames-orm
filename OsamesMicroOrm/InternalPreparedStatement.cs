using System.Collections.Generic;

namespace OsamesMicroOrm
{
    /// <summary>
    /// This class extends the prepared statement (prepared query string and an indication of how many placeholders it contains)
    /// by being fed with ado parameters names and values.
    /// It's ready for query execution last step.
    /// </summary>
    internal class InternalPreparedStatement
    {
        internal PreparedStatement PreparedStatement { get; set; }
        internal List<KeyValuePair<string, object>> AdoParameters { get; set; }

        internal InternalPreparedStatement(PreparedStatement preparedStatement_, List<KeyValuePair<string, object>> adoParameters_)
        {
            PreparedStatement = preparedStatement_;
            AdoParameters = adoParameters_;
        }
    }
}
