namespace OsamesMicroOrm
{
    /// <summary>
    /// A prepared statement contains the prepared query string and an indication of how many placeholders it contains.
    /// </summary>
    public class PreparedStatement
    {
        internal string PreparedSqlCommand { get; set; }
        internal int ParametersNumber { get; set; }

        internal PreparedStatement(string preparedSqlCommand_, int parametersNumber_)
        {
            PreparedSqlCommand = preparedSqlCommand_;
            ParametersNumber = parametersNumber_;
        }
    }
}
