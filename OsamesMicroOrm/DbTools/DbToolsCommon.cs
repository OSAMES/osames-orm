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
using System.Text.RegularExpressions;

namespace OsamesMicroOrm.DbTools
{
    /// <summary>
    /// Classe dédiée à la transformation des templates vers des chaînes SQL prêtes à l'exécution, en déterminant en parallèle le tableau des paramètres ADO.NET.
    /// </summary>
    class DbToolsCommon
    {
        #region SQL string formatting

        /// <summary>
        /// Utilitaire de formatage d'une chaîne texte <c>"my_column = @myParam"</c> en l'ajoutant à un <see cref="System.Text.StringBuilder"/>.
        /// </summary>
        /// <param name="dbColumnName_">Nom de la colonne en Db</param>
        /// <param name="adoParameters_">Objets représentatifs des paramètres ADO.NET</param>
        /// <param name="sqlCommand_">StringBuilder à compléter</param>
        /// <param name="optionalSuffix_">Suffixe optionnel, par exemple ","</param>
        /// <returns>Ne renvoie rien</returns>
        internal static void FormatSqlNameEqualValueString(string dbColumnName_, KeyValuePair<string, object> adoParameters_, ref StringBuilder sqlCommand_, string optionalSuffix_ = "")
        {
            sqlCommand_.Append(ConfigurationLoader.StartFieldEncloser).Append(dbColumnName_).Append(ConfigurationLoader.EndFieldEncloser).Append(" = ").Append(adoParameters_.Key).Append(optionalSuffix_);
        }

        /// <summary>
        /// Utilitaire de formatage d'une chaîne texte <c>"my_column = @myParam, my_column2 = @myValue2"</c> en l'ajoutant à un <see cref="System.Text.StringBuilder"/>.
        /// <para>Le suffixe est ajouté entre chaque élément de la liste lstDbColumnNames_.</para>
        /// </summary>
        /// <param name="lstDbColumnName_">Liste de noms de colonne en Db</param>
        /// <param name="adoParameters_">Objets représentatifs des paramètres ADO.NET</param>
        /// <param name="sqlCommand_">StringBuilder à compléter</param>
        /// <param name="optionalSuffix_">Suffixe optionnel, par exemple ",", ajouté entre chaque élément (pas à la fin)</param>
        /// <returns>Ne renvoie rien.</returns>
        internal static void FormatSqlNameEqualValueString(List<string> lstDbColumnName_, List<KeyValuePair<string, object>> adoParameters_, ref StringBuilder sqlCommand_, string optionalSuffix_ = "")
        {
            int iCountMinusOne = lstDbColumnName_.Count - 1;
            for (int i = 0; i < iCountMinusOne; i++)
            {
                sqlCommand_.Append(ConfigurationLoader.StartFieldEncloser).Append(lstDbColumnName_[i]).Append(ConfigurationLoader.EndFieldEncloser).Append(" = ").Append(adoParameters_[i].Key).Append(optionalSuffix_);
            }
            sqlCommand_.Append(ConfigurationLoader.StartFieldEncloser).Append(lstDbColumnName_[iCountMinusOne]).Append(ConfigurationLoader.EndFieldEncloser).Append(" = ").Append(adoParameters_[iCountMinusOne].Key);
        }

        /// <summary>
        /// Création d'une chaîne de texte en prenant chaque élément de la liste paramètre et mettant une virgule entre chaque élément.
        /// <para>Chaque élément est considéré comme étant un nom de colonne DB, il est protégé par des caractères spéciaux.</para>
        /// </summary>
        /// <param name="lstDbColumnName_">Liste de chaînes, ex : "FirstName", "LastName"...</param>
        /// <returns>Chaîne de texte. Ex: "[FirstName], [LastName]..."</returns>
        internal static string GenerateCommaSeparatedDbFieldsString(List<string> lstDbColumnName_)
        {
            StringBuilder sb = new StringBuilder();

            int iCount = lstDbColumnName_.Count;
            for (int i = 0; i < iCount; i++)
            {
                sb.Append(ConfigurationLoader.StartFieldEncloser).Append(lstDbColumnName_[i]).Append(ConfigurationLoader.EndFieldEncloser).Append(", ");
            }
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
        internal static void DetermineDatabaseColumnNameAndAdoParameter<T>(ref T dataObject_, string mappingDictionariesContainerKey_, string dataObjectPropertyName_, out string dbColumnName_, out KeyValuePair<string, object> adoParameterNameAndValue_)
        {
            dbColumnName_ = null;
            adoParameterNameAndValue_ = new KeyValuePair<string, object>();

            try
            {
                dbColumnName_ = ConfigurationLoader.Instance.GetDbColumnNameFromMappingDictionary(mappingDictionariesContainerKey_, dataObjectPropertyName_);

                // le nom du paramètre ADO.NET est détermine à partir du nom de la propriété : mise en lower case et ajout d'un préfixe "@"
                adoParameterNameAndValue_ = new KeyValuePair<string, object>(
                                        "@" + dataObjectPropertyName_.ToLowerInvariant(),
                                        dataObject_.GetType().GetProperty(dataObjectPropertyName_).GetValue(dataObject_)
                                        );
            }
            catch (Exception e)
            {
                // TODO remonter une exception ?
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 3, e.Message);
            }

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
        /// <param name="lstDataObjectPropertyName_">Liste de noms des propriétés de l'objet dataObject_</param>
        /// <param name="lstDbColumnName_">Sortie : liste de noms des colonnes en DB</param>
        /// <param name="lstAdoParameterNameAndValue_">Sortie : liste de nom/valeur des paramètres ADO.NET</param>
        /// <returns>Ne renvoie rien</returns>
        internal static void DetermineDatabaseColumnNamesAndAdoParameters<T>(ref T dataObject_, string mappingDictionariesContainerKey_, List<string> lstDataObjectPropertyName_, out List<string> lstDbColumnName_, out List<KeyValuePair<string, object>> lstAdoParameterNameAndValue_)
        {
            lstDbColumnName_ = new List<string>();

            lstAdoParameterNameAndValue_ = new List<KeyValuePair<string, object>>();
            try
            {
                foreach (string columnName in lstDataObjectPropertyName_)
                {
                    lstDbColumnName_.Add(ConfigurationLoader.Instance.GetDbColumnNameFromMappingDictionary(mappingDictionariesContainerKey_, columnName));

                    // le nom du paramètre ADO.NET est détermine à partir du nom de la propriété : mise en lower case et ajout d'un préfixe "@"
                    lstAdoParameterNameAndValue_.Add(new KeyValuePair<string, object>(
                                                    "@" + columnName.ToLowerInvariant(),
                                                    dataObject_.GetType().GetProperty(columnName).GetValue(dataObject_)
                                                ));
                }
            }
            catch (Exception e)
            {
                // TODO remonter une exception ?
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 3, e.Message);
            }

        }

        /// <summary>
        /// En connaissant le nom du mapping associé à un objet et le nom de ses propriétés, génération en sortie de l'information suivante :
        /// <para>noms des colonnes en DB (utilisation de mappingDictionariesContainerKey_ pour interroger le mapping)</para>
        /// </summary>
        /// <param name="mappingDictionariesContainerKey_">Nom du dictionnaire de mapping à utiliser</param>
        /// <param name="lstDataObjectPropertyName_">Liste de noms des propriétés d'un objet</param>
        /// <param name="lstDbColumnName_">Sortie : liste des noms des colonnes en DB</param>
        /// <param name="strErrorMsg_">Retourne un message d'erreur en cas d'échec</param>
        /// <returns>Ne renvoie rien</returns>
        internal static bool DetermineDatabaseColumnNames(string mappingDictionariesContainerKey_, List<string> lstDataObjectPropertyName_, out List<string> lstDbColumnName_, out string strErrorMsg_)
        {
            lstDbColumnName_ = new List<string>();
            strErrorMsg_ = null;
            try
            {
                foreach (string columnName in lstDataObjectPropertyName_)
                {
                    lstDbColumnName_.Add(ConfigurationLoader.Instance.GetDbColumnNameFromMappingDictionary(mappingDictionariesContainerKey_, columnName));
                }
                return true;
            }
            catch (Exception e)
            {
                // TODO remonter une exception ?
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 3, e.Message);
                strErrorMsg_ = e.Message;
                return false;
            }
        }

        /// <summary>
        /// En connaissant le nom du mapping associé à un objet, génération en sortie de l'information suivante :
        /// <para>noms des colonnes en DB (utilisation de mappingDictionariesContainerKey_ pour interroger le mapping, lister toutes les colonnes)</para>
        /// </summary>
        /// <param name="mappingDictionariesContainerKey_">Nom du dictionnaire de mapping à utiliser</param>
        /// <param name="lstDbColumnNames_">Sortie : liste de noms des colonnes en DB</param>
        /// <param name="lstDataObjectPropertyNames_">Sortie : liste de noms des propriétés de l'objet associé au mapping</param>
        /// <returns>Ne renvoie rien</returns>
        internal static void DetermineDatabaseColumnNamesAndDataObjectPropertyNames(string mappingDictionariesContainerKey_, out List<string> lstDbColumnNames_, out List<string> lstDataObjectPropertyNames_)
        {
            lstDbColumnNames_ = new List<string>();
            lstDataObjectPropertyNames_ = new List<string>();

            try
            {
                // Ce dictionnaire contient clé/valeur : propriété/nom de colonne
                Dictionary<string, string> mappingObjectSet = ConfigurationLoader.Instance.GetMappingDefinitionsForTable(mappingDictionariesContainerKey_);
                foreach (string key in mappingObjectSet.Keys)
                {
                    lstDataObjectPropertyNames_.Add(key);
                    lstDbColumnNames_.Add(mappingObjectSet[key]);
                }
            }
            catch (Exception e)
            {
                // TODO remonter une exception ?
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 3, e.Message);
            }

        }

        /// <summary>
        /// Détermine de quel type sera le placeholder en cours :
        /// <list type="bullet">
        /// <item><description>si null : retourner un nom de paramètre. Ex.: "@pN"</description></item>
        /// <item><description>si commence par "@" : retourne la chaîne en lowercase avec espaces remplacés. Ex: "@last_name"</description></item>
        /// <item><description>si chaîne : retourner le nom d'une colonne db issu du mapping. Ex. "TrackID"</description></item>
        /// <item><description>si commence par "%" : retourner simplement la string sans espace</description></item>
        /// </list>
        /// <para>Enlève tout caractère non alphanumérique des littéraux, des paramètres non dynamiques, des noms de colonne, pour éviter les injections SQL</para>
        /// <para>Le nom d'un mapping n'est pas concerné par ce traitement.</para>
        /// </summary>
        /// <param name="value_">Chaîne à traiter selon les règles énoncées ci-dessus</param>
        /// <param name="mappingDictionariesContainerKey_">Nom du dictionnaire de mapping</param>
        /// <param name="parameterAutomaticNameIndex_">Index incrémenté à chaque fois qu'on génère un nom de paramètre "@p..."</param>
        /// <param name="parameterIndex_">Index incrémenté servant à savoir où on se trouve dans la liste des paramètres et valeurs.
        /// Sert aussi pour le nom du paramètre dynamique si on avait passé null.</param>
        /// <returns>Nom de colonne DB</returns>
        internal static string DeterminePlaceholderType(string value_, string mappingDictionariesContainerKey_, ref int parameterIndex_, ref int parameterAutomaticNameIndex_)
        {
            string returnValue;
            char[] valueAsCharArray;

            if (value_ == null)
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

                return returnValue; 
            }

            // Dans ce dernier cas c'est une colonne et non pas un paramètre, parameterIndex_ n'est donc pas modifié.
            // On peut avoir des espaces dans le nom de la colonne ainsi que "_" mais pas "-" (norme SQL).
            string columnName;
            string errorMsg;
            DetermineDatabaseColumnName(mappingDictionariesContainerKey_, value_, out columnName, out errorMsg);
            valueAsCharArray = columnName.Where(c_ => (char.IsLetterOrDigit(c_) ||
                                                             char.IsWhiteSpace(c_) ||
                                                             c_ == '_')).ToArray();
            return new string(valueAsCharArray);
        }

        /// <summary>
        /// Complète les paramètres :
        /// <list type="bullet">
        /// <item><description>sqlPlaceholders_ avec les noms des paramètres ADO.NET. Usage ultérieur : complétion de la chaîne de commande SQL</description></item>
        /// <item><description>adoParameters_ avec pour chaque paramètre ADO.NET son nom et sa valeur. Usage ultérieur : valeur passée à DbManager</description></item>
        /// </list>
        /// </summary>
        /// <param name="mappingDictionariesContainerKey_">Nom du dictionnaire de mapping à utiliser</param>
        /// <param name="strColumnNames_">Liste de chaînes contenant le nom des colonnes d'une table.</param>
        /// <param name="oValues_">Valeurs pour les paramètres ADO.NET</param>
        /// <param name="sqlPlaceholders_">Liste de string existante, destinée à ajouter les noms des paramètres ADO.NET. (Ex.: @ado_param) à la suite des éléments existant</param>
        /// <param name="adoParameters_">Liste de clés/valeurs existante, destinée à ajouter les noms et valeurs des paramètres ADO.NET. (Ex. </param>
        /// <returns>Ne renvoie rien</returns>
        internal static void FillPlaceHoldersAndAdoParametersNamesAndValues(string mappingDictionariesContainerKey_, List<string> strColumnNames_, List<object> oValues_, List<string> sqlPlaceholders_, List<KeyValuePair<string, object>> adoParameters_)
        {
            if (strColumnNames_ == null) return;

            int iCount = strColumnNames_.Count;
            int parameterIndex = -1;
            int parameterAutomaticNameIndex = -1;
            for (int i = 0; i < iCount; i++)
            {
                //Analyse la chaine courante de strColumnNames_ et retoure soit un @pN ou alors @nomcolonne
                string paramName = DeterminePlaceholderType(strColumnNames_[i], mappingDictionariesContainerKey_, ref parameterIndex, ref parameterAutomaticNameIndex);

                // Ajout d'un paramètre ADO.NET dans la liste. Sinon protection du champ.
                if (paramName.StartsWith("@"))
                    adoParameters_.Add(new KeyValuePair<string, object>(paramName, oValues_[parameterIndex]));
                else
                    paramName = string.Concat(ConfigurationLoader.StartFieldEncloser, paramName, ConfigurationLoader.EndFieldEncloser);

                // Ajout pour les placeholders
                sqlPlaceholders_.Add(paramName);

            }
        }

        /// <summary>
        /// En connaissant le nom du mapping associé à un objet et le nom de sa propriété, génération en sortie de l'information suivante :
        /// <para>nom de la colonne en DB (utilisation de mappingDictionariesContainerKey_ pour interroger le mapping)</para>
        /// </summary>
        /// <param name="mappingDictionariesContainerKey_">Nom du dictionnaire de mapping à utiliser</param>
        /// <param name="dataObjectPropertyName_">Nom d'une propriété de l'objet dataObject_</param>
        /// <param name="dbColumnName_">Sortie : nom de la colonne en DB</param>
        /// <param name="strErrorMsg_">Retourne un message d'erreur en cas d'échec</param>
        /// <returns>Ne renvoie rien</returns>
        internal static bool DetermineDatabaseColumnName(string mappingDictionariesContainerKey_, string dataObjectPropertyName_, out string dbColumnName_, out string strErrorMsg_)
        {
            dbColumnName_ = null;
            strErrorMsg_ = null;
            try
            {
                dbColumnName_ = ConfigurationLoader.Instance.GetDbColumnNameFromMappingDictionary(mappingDictionariesContainerKey_, dataObjectPropertyName_);
                return true;
            }
            catch (Exception e)
            {
                // TODO remonter une exception ?
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 3, e.Message);
                strErrorMsg_ = e.Message;
                return false;
            }
        }

        #endregion

        #region utilities
        /// <summary>
        /// <c>String.Format</c> avec gestion d'exception.
        /// <para>Renvoie faux si le nombre de placeholders et de paramètres ne sont pas égaux.</para>
        /// </summary>
        /// <param name="format_">Chaîne texte avec des placeholders</param>
        /// <param name="result_">Chaine avec les placeholders remplacés si succès ou bien message d'erreur pour l'utilisateur si échec du remplacement (cas d'erreur)</param>
        /// <param name="args_">Valeurs à mettre dans les placeholders</param>
        /// <param name="strErrorMsg_">Retourne un message d'erreur en cas d'échec</param>
        /// <returns>Renvoie vrai si réussi, sinon retourne faux.</returns>
        internal static bool TryFormat(string format_, out string result_, out string strErrorMsg_, params Object[] args_)
        {
            strErrorMsg_ = null;
            try
            {
                result_ = String.Format(format_, args_);
                return true;
            }
            catch (FormatException ex)
            {
                int nbOfPlaceholders = Utilities.Common.CountPlaceholders(format_);
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 0,
                    "Error, not same number of placeholders. Expected : " + nbOfPlaceholders + ", given parameters : " + args_.Length + ", exception: " + ex.Message);
                result_ = "Error, not same number of placeholders. See log file for more details.";
                strErrorMsg_ = ex.Message + "\n" + result_;
                return false;
            }
        }

        #endregion
    }
}