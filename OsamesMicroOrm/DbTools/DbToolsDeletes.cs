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
using System.Text;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.Logging;

namespace OsamesMicroOrm.DbTools
{
    /// <summary>
    /// TODO
    /// </summary>
    public class DbToolsDeletes
    {

        /// <summary>
        /// Crée le texte de la commande SQL paramétrée ainsi que les paramètres ADO.NET, dans le cas d'un update sur un seul objet.
        /// Utilise le template "BaseUpdate" : <c>"UPDATE {0} SET {1} WHERE {2}"</c> ainsi que les éléments suivants :
        /// <list type="bullet">
        /// <item><description>clé du dictionnaire de mapping</description></item>
        /// <item><description>un objet de données dataObject_</description></item>
        /// <item><description>liste de noms de propriétés de dataObject_ à utiliser pour les champs à mettre à jour</description></item>
        /// <item><description>nom de la propriété de dataObject_ correspondant au champ clé primaire</description></item>
        /// </list>
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="dataObject_">Instance d'un objet de la classe T</param>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping</param>
        /// <param name="sqlTemplate_">Contient le nom du template sql update à utiliser</param>
        /// <param name="lstDataObjectColumnNames_">Noms des propriétés de l'objet dataObject_ à utiliser pour les champs à mettre à jour</param>
        /// <param name="lstWhereMetaNames_">Pour les colonnes de la clause where : indication d'une propriété de dataObject_ ou un paramètre dynamique. 
        /// Pour formater à partir de {2} dans le template SQL. Peut être null</param>
        /// <param name="lstWhereValues_">Valeurs pour les paramètres ADO.NET. Peut être null</param>
        /// <param name="sqlCommand_">Sortie : texte de la commande SQL paramétrée</param>
        /// <param name="lstAdoParameters_">Sortie : clé/valeur des paramètres ADO.NET pour la commande SQL paramétrée</param>
        /// <param name="tryFormat">Si a vrai, on fait un try format sur le sqlcommand</param>
        /// <returns>Ne renvoie rien</returns>
        /// <exception cref="OOrmHandledException">Toute sorte d'erreur</exception>
        internal static void FormatSqlForDelete(string sqlTemplate_, string mappingDictionariesContainerKey_, List<string> lstWhereMetaNames_, List<object> lstWhereValues_, out string sqlCommand_, out List<KeyValuePair<string, object>> lstAdoParameters_)
        {
            StringBuilder sbFieldsToUpdate = new StringBuilder();
            sqlCommand_ = null;
            lstAdoParameters_ = new List<KeyValuePair<string, object>>();

            // 2. Positionne le premier placeholders
            List<string> sqlPlaceholders = new List<string> { string.Concat(ConfigurationLoader.StartFieldEncloser, mappingDictionariesContainerKey_, ConfigurationLoader.EndFieldEncloser) };

            // 3. Détermine les noms des paramètres pour le where
            DbToolsCommon.FillPlaceHoldersAndAdoParametersNamesAndValues(mappingDictionariesContainerKey_, lstWhereMetaNames_, lstWhereValues_, sqlPlaceholders, lstAdoParameters_);

            DbToolsCommon.TryFormatTemplate(ConfigurationLoader.DicDeleteSql, sqlTemplate_, out sqlCommand_, sqlPlaceholders.ToArray());

        }

        /// <summary>
        /// Exécute une requête de type "DELETE FROM {0} WHERE ...".
        /// TODO
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataObject_"></param>
        /// <param name="refSqlTemplate_"></param>
        /// <param name="mappingDictionariesContainerKey_"></param>
        /// <param name="lstWhereMetaNames_"></param>
        /// <param name="lstWhereValues_"></param>
        /// <param name="transaction_"></param>
        public static long Delete(string refSqlTemplate_, string mappingDictionariesContainerKey_, List<string> lstWhereMetaNames_, List<object> lstWhereValues_, OOrmDbTransactionWrapper transaction_ = null)
        {
            string sqlCommand = null;
            List<KeyValuePair<string, object>> adoParameters;
            long nbRowsAffected = 0;

            FormatSqlForDelete(refSqlTemplate_, mappingDictionariesContainerKey_, lstWhereMetaNames_, lstWhereValues_, out sqlCommand, out adoParameters);

            if (transaction_ != null)
            {
                // Présence d'une transaction
                if (DbManager.Instance.ExecuteNonQuery(transaction_, CommandType.Text, sqlCommand, adoParameters) != 0)
                    nbRowsAffected++;
                else
                    Logger.Log(TraceEventType.Warning, Utilities.OOrmErrorsHandler.FindHResultAndDescriptionByCode(HResultEnum.E_NOROWUPDATED).Value + " : '" + sqlCommand + "'");

                return nbRowsAffected;
            }

            // Pas de transaction
            OOrmDbConnectionWrapper conn = null;
            try
            {
                conn = DbManager.Instance.CreateConnection();
                if (DbManager.Instance.ExecuteNonQuery(conn, CommandType.Text, sqlCommand, adoParameters) != 0)
                    nbRowsAffected++;
                else
                    Logger.Log(TraceEventType.Warning, Utilities.OOrmErrorsHandler.FindHResultAndDescriptionByCode(HResultEnum.E_NOROWUPDATED).Value + " : '" + sqlCommand + "'");

                return nbRowsAffected;
            }
            finally
            {
                // Si c'est la connexion de backup alors on ne la dipose pas pour usage ultérieur.
                if (!conn.IsBackup)
                    conn.Dispose();
            }

            return 0;
        }
    }
}
