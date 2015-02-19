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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using OsamesMicroOrm.Configuration;
using OsamesMicroOrm.Logging;

namespace OsamesMicroOrm.DbTools
{
    /// <summary>
    /// Classe dédiée à la transformation des templates vers des chaînes SQL prêtes à l'exécution, en déterminant en parallèle le tableau des paramètres ADO.NET.
    /// </summary>
    internal class DbToolsCommon
    {
        #region SQL string formatting

        /// <summary>
        /// Création d'une chaîne de texte en prenant chaque élément de la liste paramètre et mettant une virgule entre chaque élément.
        /// <para>Chaque élément est considéré comme étant un nom de colonne DB, il est protégé par des caractères spéciaux.</para>
        /// </summary>
        /// <param name="lstDbColumnNames_">Liste de chaînes, ex : "FirstName", "LastName"...</param>
        /// <returns>Chaîne de texte. Ex: "[FirstName], [LastName]..."</returns>
        internal static string GenerateCommaSeparatedDbFieldsString(List<string> lstDbColumnNames_)
        {
            if (lstDbColumnNames_.Count == 0)
                return string.Empty;

            StringBuilder sb = new StringBuilder();

            int iCount = lstDbColumnNames_.Count;
            for (int i = 0; i < iCount; i++)
            {
                sb.Append(ConfigurationLoader.StartFieldEncloser).Append(lstDbColumnNames_[i]).Append(ConfigurationLoader.EndFieldEncloser).Append(", ");
            }
            if (!string.IsNullOrEmpty(sb.ToString()))
                sb.Remove(sb.Length - 2, 2);
            return sb.ToString();
        }

        #endregion

        #region Determine
        /// <summary>
        /// En connaissant un objet et le nom de sa propriété, génération en sortie des informations suivantes :
        /// <list type="bullet">
        /// <item><description>nom de la colonne en DB (utilisation de mappingDictionariesContainerKey_ pour interroger le mapping)</description></item>
        /// <item><description>nom et valeur du paramètre ADO.NET correspondant.</description></item>
        /// </list>
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="dataObject_">Instance d'un objet de la classe T</param>
        /// <param name="mappingDictionariesContainerKey_">Nom du dictionnaire de mapping à utiliser</param>
        /// <param name="dataObjectPropertyName_">Nom d'une propriété de l'objet dataObject_</param>
        /// <param name="dbColumnName_">Sortie : nom de la colonne en DB</param>
        /// <param name="adoParameterNameAndValue_">Sortie : nom/valeur du paramètre ADO.NET</param>
        /// <returns>Ne renvoie rien</returns>
        /// <exception cref="OOrmHandledException">Pas de correspondance dans le mapping ou autre erreur</exception>
        internal static void DetermineDatabaseColumnNameAndAdoParameter<T>(T dataObject_, string mappingDictionariesContainerKey_, string dataObjectPropertyName_, out string dbColumnName_, out KeyValuePair<string, object> adoParameterNameAndValue_)
        {
            dbColumnName_ = ConfigurationLoader.Instance.GetDbColumnNameFromMappingDictionary(mappingDictionariesContainerKey_, dataObjectPropertyName_);

            // le nom du paramètre ADO.NET est détermine à partir du nom de la propriété : mise en lower case et ajout d'un préfixe "@"
            adoParameterNameAndValue_ = new KeyValuePair<string, object>(
                                    "@" + dataObjectPropertyName_.ToLowerInvariant(),
                                    dataObject_.GetType().GetProperty(dataObjectPropertyName_).GetValue(dataObject_)
                                    );
        }

        /// <summary>
        /// En connaissant un objet et le nom de ses propriétés, génération en sortie des informations suivantes :
        /// <list type="bullet">
        /// <item><description>noms des colonnes en DB (utilisation de mappingDictionariesContainerKey_ pour interroger le mapping)</description></item>
        /// <item><description>nom et valeur des paramètres ADO.NET correspondant aux propriétés</description></item>
        /// </list>
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="dataObject_">Instance d'un objet de la classe T</param>
        /// <param name="mappingDictionariesContainerKey_">Nom du dictionnaire de mapping à utiliser</param>
        /// <param name="lstDataObjectPropertyNames_">Liste de noms des propriétés de l'objet dataObject_</param>
        /// <param name="lstDbColumnNames_">Sortie : liste de noms des colonnes en DB</param>
        /// <param name="lstAdoParameterNameAndValues_">Sortie : liste de nom/valeur des paramètres ADO.NET</param>
        /// <returns>Ne renvoie rien</returns>
        /// <exception cref="OOrmHandledException">Pas de correspondance dans le mapping ou autre erreur</exception>
        internal static void DetermineDatabaseColumnNamesAndAdoParameters<T>(T dataObject_, string mappingDictionariesContainerKey_, List<string> lstDataObjectPropertyNames_, out List<string> lstDbColumnNames_, out List<KeyValuePair<string, object>> lstAdoParameterNameAndValues_)
        {
            lstDbColumnNames_ = new List<string>();
            lstAdoParameterNameAndValues_ = new List<KeyValuePair<string, object>>();

            foreach (string columnName in lstDataObjectPropertyNames_)
            {
                lstDbColumnNames_.Add(ConfigurationLoader.Instance.GetDbColumnNameFromMappingDictionary(mappingDictionariesContainerKey_, columnName));

                // le nom du paramètre ADO.NET est détermine à partir du nom de la propriété : mise en lower case et ajout d'un préfixe "@"
                lstAdoParameterNameAndValues_.Add(new KeyValuePair<string, object>(
                                                "@" + columnName.ToLowerInvariant(),
                                                dataObject_.GetType().GetProperty(columnName).GetValue(dataObject_)
                                            ));
            }
        }

        /// <summary>
        /// En connaissant le nom du mapping associé à un objet et le nom de ses propriétés, génération en sortie de l'information suivante :
        /// <para>noms des colonnes en DB (utilisation de mappingDictionariesContainerKey_ pour interroger le mapping)</para>
        /// </summary>
        /// <param name="mappingDictionariesContainerKey_">Nom du dictionnaire de mapping à utiliser</param>
        /// <param name="lstDataObjectPropertyNames_">Liste de noms des propriétés d'un objet</param>
        /// <param name="lstDbColumnNames_">Sortie : liste des noms des colonnes en DB</param>
        /// <returns>Ne renvoie rien</returns>
        /// <exception cref="OOrmHandledException">Pas de correspondance dans le mapping</exception>
        internal static void DetermineDatabaseColumnNames(string mappingDictionariesContainerKey_, List<string> lstDataObjectPropertyNames_, out List<string> lstDbColumnNames_)
        {
            lstDbColumnNames_ = new List<string>();
            lstDbColumnNames_.AddRange(lstDataObjectPropertyNames_.Select(columnName_ => ConfigurationLoader.Instance.GetDbColumnNameFromMappingDictionary(mappingDictionariesContainerKey_, columnName_)));
        }

        /// <summary>
        /// En connaissant le nom du mapping associé à un objet, génération en sortie de l'information suivante :
        /// <para>noms des colonnes en DB (utilisation de mappingDictionariesContainerKey_ pour interroger le mapping, lister toutes les colonnes)</para>
        /// </summary>
        /// <param name="mappingDictionariesContainerKey_">Nom du dictionnaire de mapping à utiliser</param>
        /// <param name="lstDbColumnNames_">Sortie : liste de noms des colonnes en DB</param>
        /// <param name="lstDataObjectPropertyNames_">Sortie : liste de noms des propriétés de l'objet associé au mapping</param>
        /// <returns>Ne renvoie rien</returns>
        /// <exception cref="OOrmHandledException">Pas de correspondance dans le mapping</exception>
        internal static void DetermineDatabaseColumnNamesAndDataObjectPropertyNames(string mappingDictionariesContainerKey_, out List<string> lstDbColumnNames_, out List<string> lstDataObjectPropertyNames_)
        {
            lstDbColumnNames_ = new List<string>();
            lstDataObjectPropertyNames_ = new List<string>();
            // Ce dictionnaire contient clé/valeur : propriété/nom de colonne
            Dictionary<string, string> mappingObjectSet = ConfigurationLoader.Instance.GetMappingDefinitionsForTable(mappingDictionariesContainerKey_);
            foreach (string key in mappingObjectSet.Keys)
            {
                lstDataObjectPropertyNames_.Add(key);
                lstDbColumnNames_.Add(mappingObjectSet[key]);
            }
        }

        /// <summary>
        /// Détermine de quel type sera le placeholder en cours :
        /// <list type="bullet">
        /// <item><description>si "#" : retourner un nom de paramètre. Ex.: "@pN"</description></item>
        /// <item><description>si commence par "@" : retourne la chaîne en lowercase avec espaces remplacés. Ex: "@last_name"</description></item>
        /// /// <item><description>si null/whitespace ou commence par "%UL%" : retourner simplement la string telle quelle</description></item>
        /// <item><description>si commence par "%" : retourner simplement la string telle quelle en enlevant les espaces</description></item>
        /// <item><description>si chaîne avec un ":" : retourner le nom d'une colonne DB issu du mapping en supposant que le chaîne avant le ":" est un nom de dictionnaire de mapping (table DB).
        ///  Ex. "Track:TrackID"</description></item>
        /// <item><description>si chaîne : retourner le nom d'une colonne DB issu du mapping. Ex. "TrackID"</description></item>
        /// <item><description>sinon lance une exception</description></item>
        /// </list>
        /// <para>Enlève tout caractère non alphanumérique des littéraux, des paramètres non dynamiques, des noms de colonne, pour éviter les injections SQL</para>
        /// <para>Le nom d'un mapping n'est pas concerné par ce traitement.</para>
        /// </summary>
        /// <param name="value_">Chaîne à traiter selon les règles énoncées ci-dessus</param>
        /// <param name="mappingDictionariesContainerKey_">Nom du dictionnaire de mapping</param>
        /// <param name="parameterAutomaticNameIndex_">Index incrémenté à chaque fois qu'on génère un nom de paramètre "@p..."</param>
        /// <param name="parameterIndex_">Index incrémenté servant à savoir où on se trouve dans la liste des paramètres et valeurs.
        /// Sert aussi pour le nom du paramètre dynamique si on avait passé "#".</param>
        /// <param name="unprotectedLiteral_">Indique si le littéral doit être protégé avec les fields encloser. Vrai pour non protégé. Gère aussi le fait d'avoir une valeur null ou whitespace</param>
        /// <returns>Nom de colonne DB</returns>
        /// <exception cref="OOrmHandledException">Erreurs possibles : 1. pas de correspondance dans le mapping pour mappingDictionariesContainerKey_. 2. value_ n'as pas une syntaxe interprétable</exception>
        internal static string DeterminePlaceholderType(string value_, string mappingDictionariesContainerKey_, ref int parameterIndex_, ref int parameterAutomaticNameIndex_, out bool unprotectedLiteral_)
        {
            unprotectedLiteral_ = false;

            if (string.IsNullOrWhiteSpace(value_) || value_.ToUpperInvariant().StartsWith("%UL%"))
            {
                unprotectedLiteral_ = true;
                return value_;
            }

            string returnValue;
            char[] valueAsCharArray;

            if (value_ == "#")
            {
                // C'est un nom automatique de paramètre ADO.NET.

                parameterIndex_++;
                parameterAutomaticNameIndex_++;
                return "@p" + parameterAutomaticNameIndex_;
            }

            if (value_.StartsWith("@"))
            {
                // C'est un nom personnalisé de paramètre ADO.NET.
                // Il ne peut contenir d'espaces par définition.

                parameterIndex_++;
                valueAsCharArray = value_.Where(c_ => (char.IsLetterOrDigit(c_) ||
                                                             c_ == '_' ||
                                                             c_ == '-')).ToArray();

                returnValue = new string(valueAsCharArray);
                return "@" + returnValue.ToLowerInvariant();
            }

            if (value_.StartsWith("%"))
            {
                // C'est un littéral.
                // Dans un literal on permet les espaces.

                parameterIndex_++;
                valueAsCharArray = value_.Where(c_ => (char.IsLetterOrDigit(c_) ||
                                                             char.IsWhiteSpace(c_) ||
                                                             c_ == '_' ||
                                                             c_ == '-')).ToArray();

                returnValue = new string(valueAsCharArray);
                return string.Concat(ConfigurationLoader.StartFieldEncloser, returnValue, ConfigurationLoader.EndFieldEncloser);
            }

            if (value_.Count(c_ => c_ == ':') > 1)
                throw new OOrmHandledException(HResultEnum.E_INCORRECTPLACEHOLDERVALUE, null, "Value : '" + value_ +"' cannot contain more than one colon");

            string columnName;
            var temp = value_.Split(':');

            if (temp.Length == 1)
            {
                // Dans ce dernier cas c'est une colonne et non pas un paramètre, parameterIndex_ n'est donc pas modifié.
                // On peut avoir des espaces dans le nom de la colonne ainsi que "_" mais pas "-" (norme SQL).
                DetermineDatabaseColumnName(mappingDictionariesContainerKey_, value_, out columnName);
                valueAsCharArray = columnName.Where(c_ => (char.IsLetterOrDigit(c_) ||
                                                           char.IsWhiteSpace(c_) ||
                                                           c_ == '_')).ToArray();
                return string.Concat(ConfigurationLoader.StartFieldEncloser, new string(valueAsCharArray), ConfigurationLoader.EndFieldEncloser);
            }

            // Dans ce dernier cas c'est une colonne et non pas un paramètre, parameterIndex_ n'est donc pas modifié.
            // On peut avoir des espaces dans le nom de la colonne ainsi que "_" mais pas "-" (norme SQL).
            DetermineDatabaseColumnName(temp[0], temp[1], out columnName);
            valueAsCharArray = columnName.Where(c_ => (char.IsLetterOrDigit(c_) ||
                                                       char.IsWhiteSpace(c_) ||
                                                       c_ == '_')).ToArray();
            return string.Concat(ConfigurationLoader.StartFieldEncloser, temp[0], ConfigurationLoader.EndFieldEncloser, ".", ConfigurationLoader.StartFieldEncloser, new string(valueAsCharArray), ConfigurationLoader.EndFieldEncloser);

        }

        /// <summary>
        /// Complète les paramètres :
        /// <list type="bullet">
        /// <item><description>sqlPlaceholders_ avec les noms des paramètres ADO.NET. Usage ultérieur : complétion de la chaîne de commande SQL</description></item>
        /// <item><description>adoParameters_ avec pour chaque paramètre ADO.NET son nom et sa valeur. Usage ultérieur : valeur passée à DbManager</description></item>
        /// </list>
        /// </summary>
        /// <param name="mappingDictionariesContainerKey_">Nom du dictionnaire de mapping à utiliser</param>
        /// <param name="lstColumnNames_">Liste de chaînes contenant le nom des colonnes d'une table.</param>
        /// <param name="lstValues_">Valeurs pour les paramètres ADO.NET</param>
        /// <param name="lstSqlPlaceholders_">Liste de string existante, destinée à ajouter les noms des paramètres ADO.NET. (Ex.: @ado_param) à la suite des éléments existant</param>
        /// <param name="lstAdoParameters_">Liste de clés/valeurs existante, destinée à ajouter les noms et valeurs des paramètres ADO.NET. (Ex. </param>
        /// <returns>Ne renvoie rien</returns>
        /// <exception cref="OOrmHandledException">Exception générée ici ou remontée depuis une méthode appelée</exception>
        internal static void FillPlaceHoldersAndAdoParametersNamesAndValues(string mappingDictionariesContainerKey_, List<string> lstColumnNames_, List<object> lstValues_, List<string> lstSqlPlaceholders_, List<KeyValuePair<string, object>> lstAdoParameters_)
        {
            if (lstColumnNames_ == null) return;

            int iCount = lstColumnNames_.Count;
            int parameterIndex = -1;
            int parameterAutomaticNameIndex = -1;
            for (int i = 0; i < iCount; i++)
            {

                bool isUnprotectedLiteral;
                //Analyse la chaine courante de strColumnNames_ et retoure soit un @pN ou alors @nomcolonne ou alors le litéral initial
                string paramName = DeterminePlaceholderType(lstColumnNames_[i], mappingDictionariesContainerKey_, ref parameterIndex, ref parameterAutomaticNameIndex, out isUnprotectedLiteral);

                if(paramName == null)
                    continue;

                if (paramName.StartsWith("%UL%"))
                {
                    lstSqlPlaceholders_.Add(paramName.Remove(paramName.IndexOf("%"), (paramName.IndexOf("%", paramName.IndexOf("%") + 2)) - paramName.IndexOf("%") + 1));
                    continue;
                }

                // Ajout d'un paramètre ADO.NET dans la liste. Sinon protection du champ si doit être protégé (null ou whitespace : ne pas protéger).
                if (paramName.StartsWith("@"))
                {
                    if(parameterIndex > lstValues_.Count-1)
                        throw new OOrmHandledException(HResultEnum.E_METANAMESVALUESCOUNTMISMATCH, null, "Asked for value of index " + parameterIndex + " for dynamic parameter of name " + paramName + " but there are only " + lstValues_.Count + " values");
                    lstAdoParameters_.Add(new KeyValuePair<string, object>(paramName, lstValues_[parameterIndex]));
                }

                // Ajout pour les placeholders
                lstSqlPlaceholders_.Add(paramName);

            }
        }

        /// <summary>
        /// En connaissant le nom du mapping associé à un objet et le nom de sa propriété, génération en sortie de l'information suivante :
        /// <para>nom de la colonne en DB (utilisation de mappingDictionariesContainerKey_ pour interroger le mapping)</para>
        /// </summary>
        /// <param name="mappingDictionariesContainerKey_">Nom du dictionnaire de mapping à utiliser</param>
        /// <param name="dataObjectPropertyName_">Nom d'une propriété de l'objet dataObject_</param>
        /// <param name="dbColumnName_">Sortie : nom de la colonne en DB</param>
        /// <returns>Ne renvoie rien</returns>
        /// <exception cref="OOrmHandledException">Erreur quand l'information demandée ne peut être trouvée das les données de maping</exception>
        internal static void DetermineDatabaseColumnName(string mappingDictionariesContainerKey_, string dataObjectPropertyName_, out string dbColumnName_)
        {
            dbColumnName_ = ConfigurationLoader.Instance.GetDbColumnNameFromMappingDictionary(mappingDictionariesContainerKey_, dataObjectPropertyName_);
        }

        #endregion

        #region utilities
        /// <summary>
        /// <c>String.Format</c> avec gestion d'exception.
        /// </summary>
        /// <param name="format_">Chaîne texte avec des placeholders</param>
        /// <param name="result_">Chaine avec les placeholders remplacés si succès ou bien message d'erreur pour l'utilisateur si échec du remplacement (cas d'erreur)</param>
        /// <param name="args_">Valeurs à mettre dans les placeholders</param>
        /// <returns>Ne renvoie rien</returns>
        /// <exception cref="OOrmHandledException">Si le nombre de placeholders et de paramètres ne sont pas égaux.</exception>
        internal static void TryFormat(string format_, out string result_, params Object[] args_)
        {
            try
            {
                result_ = String.Format(format_, args_);
            }
            catch (FormatException ex)
            {
                int nbOfPlaceholders = Utilities.Common.CountPlaceholders(format_);
                string errorDetail = "Expected : " + nbOfPlaceholders + ", given parameters : " + args_.Length;
                throw new OOrmHandledException(HResultEnum.E_PLACEHOLDERSVALUESCOUNTMISMATCH, ex, errorDetail);
            }
        }

        #endregion
    }
}