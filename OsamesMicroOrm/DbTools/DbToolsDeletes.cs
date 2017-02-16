/*
This file is part of OSAMES Micro ORM.
Copyright 2014 OSAMES

OSAMES Micro ORM is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

OSAMES Micro ORM is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with OSAMES Micro ORM.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.Logging;
using OsamesMicroOrm.Utilities;

namespace OsamesMicroOrm.DbTools
{
    /// <summary>
    /// Classe servant à formater et exécuter des requêtes SQL de type DELETE, en proposant une abstraction au dessus de ADO.NET.
    /// </summary>
    public static class DbToolsDeletes
    {

        /// <summary>
        /// Crée le texte de la commande SQL paramétrée ainsi que les paramètres ADO.NET, dans le cas d'un delete.
        /// Utilise des templates de type <c>"DELETE FROM {0} WHERE ..."</c> ainsi que les éléments suivants :
        /// <list type="bullet">
        /// <item><description>clé du dictionnaire de mapping</description></item>
        /// </list>
        /// </summary>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping</param>
        /// <param name="sqlTemplateName_">Nom du template SQL</param>
        /// <param name="lstWhereMetaNames_">Pour les colonnes de la clause where : valeur dont la syntaxe indique qu'il s'agit d'une propriété de classe C#/un paramètre dynamique/un littéral. 
        /// Pour formater à partir de {1} dans le template SQL. Peut être null</param>
        /// <param name="lstWhereValues_">Valeurs pour les paramètres ADO.NET. Peut être null</param>
        /// <returns>Sortie : structure contenant : texte de la commande SQL paramétrée, clé/valeur des paramètres ADO.NET pour la commande SQL paramétrée</returns>
        /// <exception cref="OOrmHandledException">Toute sorte d'erreur</exception>
        internal static InternalPreparedStatement FormatSqlForDelete(string sqlTemplateName_, string mappingDictionariesContainerKey_, List<string> lstWhereMetaNames_, List<object> lstWhereValues_)
        {
            string sqlCommand;
            List<KeyValuePair<string, object>> lstAdoParameters = new List<KeyValuePair<string, object>>(); // Paramètres ADO.NET, à remplir

            // 2. Positionne le premier placeholders
            List<string> sqlPlaceholders = new List<string> { string.Concat(ConfigurationLoader.StartFieldEncloser, mappingDictionariesContainerKey_, ConfigurationLoader.EndFieldEncloser) };

            // 3. Détermine les noms des paramètres pour le where
            DbToolsCommon.FillPlaceHoldersAndAdoParametersNamesAndValues(mappingDictionariesContainerKey_, lstWhereMetaNames_, lstWhereValues_, sqlPlaceholders, lstAdoParameters);

            DbToolsCommon.TryFormatTemplate(ConfigurationLoader.DicDeleteSql, sqlTemplateName_, out sqlCommand, sqlPlaceholders.ToArray());

            return new InternalPreparedStatement(new PreparedStatement(sqlCommand, lstAdoParameters.Count), lstAdoParameters);
        }

        /// <summary>
        /// Exécute une requête de type "DELETE FROM {0} WHERE ...".
        /// </summary>
        /// <param name="sqlTemplateName_">Nom du template SQL</param>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping</param>
        /// <param name="lstWhereMetaNames_">Pour les colonnes de la clause where : valeur dont la syntaxe indique qu'il s'agit d'une propriété de classe C#/un paramètre dynamique/un littéral. 
        /// Pour formater à partir de {1} dans le template SQL. Peut être null</param>
        /// <param name="lstWhereValues_">Valeurs pour les paramètres ADO.NET. Peut être null</param>
        /// <param name="transaction_">Transaction optionnelle (obtenue par appel à DbManager)</param>
        public static long Delete(string sqlTemplateName_, string mappingDictionariesContainerKey_, List<string> lstWhereMetaNames_, List<object> lstWhereValues_, OOrmDbTransactionWrapper transaction_ = null)
        {
            long nbRowsAffected = 0;

            InternalPreparedStatement statement = FormatSqlForDelete(sqlTemplateName_, mappingDictionariesContainerKey_, lstWhereMetaNames_, lstWhereValues_);

            if (transaction_ != null)
            {
                // Présence d'une transaction
                if (DbManager.Instance.ExecuteNonQuery(transaction_, CommandType.Text, statement.PreparedStatement.PreparedSqlCommand, statement.AdoParameters) != 0)
                    nbRowsAffected++;
                else
                    Logger.Log(TraceEventType.Warning, OOrmErrorsHandler.FindHResultAndDescriptionByCode(HResultEnum.W_NOROWUPDATED).Value + " : '" + statement.PreparedStatement.PreparedSqlCommand + "'");

                return nbRowsAffected;
            }

            // Pas de transaction
            OOrmDbConnectionWrapper conn = null;
            try
            {
                conn = DbManager.Instance.CreateConnection();
                if (DbManager.Instance.ExecuteNonQuery(conn, CommandType.Text, statement.PreparedStatement.PreparedSqlCommand, statement.AdoParameters) != 0)
                    nbRowsAffected++;
                else
                    Logger.Log(TraceEventType.Warning, OOrmErrorsHandler.FindHResultAndDescriptionByCode(HResultEnum.W_NOROWUPDATED).Value + " : '" + statement.PreparedStatement.PreparedSqlCommand + "'");

                return nbRowsAffected;
            }
            finally
            {
                // Si c'est la connexion de backup alors on ne la dipose pas pour usage ultérieur.
                if (!conn.IsBackup)
                    conn.Dispose();
            }
        }
    }
}
