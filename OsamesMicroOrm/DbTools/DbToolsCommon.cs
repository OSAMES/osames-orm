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
using System.Text;
using OsamesMicroOrm.Configuration;

namespace OsamesMicroOrm.DbTools
{
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
        /// <para>Le suffixe est ajouté entre chaque élément de la liste lstDbColumnName_.</para>
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
        /// Formatage d'une chaîne de texte sérialisant la liste des noms de colonnes DB paramètre et mettant une virgule entre chaque élément.
        /// </summary>
        /// <param name="lstDbColumnName_">Liste de noms de colonnes DB</param>
        /// <param name="sqlCommand_">StringBuilder à compléter</param>
        /// <returns>Ne renvoie rien</returns>
        internal static void FormatSqlFieldsListAsString(List<string> lstDbColumnName_, out StringBuilder sqlCommand_)
        {
            sqlCommand_ = new StringBuilder();

            int iCount = lstDbColumnName_.Count;
            for (int i = 0; i < iCount; i++)
            {
                sqlCommand_.Append(ConfigurationLoader.StartFieldEncloser).Append(lstDbColumnName_[i]).Append(ConfigurationLoader.EndFieldEncloser).Append(", ");
            }
            sqlCommand_.Remove(sqlCommand_.Length - 2, 2);
        }
        
        #endregion
        #region Determine
        /// <summary>
        /// En connaissant un objet et le nom de sa propriété, génération en sortie des informations suivantes :
        /// <list type="bullet">
        /// <item><description>nom de la colonne en DB (utilisation de mappingDictionariesContainerKey_ pour interroger le mapping)</description></item>
        /// <item><description>nom et valeur du paramètre ADO.NET correspondant à la propriété (nom : proche du nom de la propriété, valeur : valeur de la propriété).</description></item>
        /// </list>
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="dataObject_">Instance d'un objet de la classe T</param>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping</param>
        /// <param name="dataObjectcolumnName_">Nom d'une propriété de l'objet dataObject_</param>
        /// <param name="dbColumnName_">Sortie : nom de la colonne en DB</param>
        /// <param name="adoParameterNameAndValue_">Sortie : clé/valeur du paramètre ADO.NET</param>
        /// <returns>Ne renvoie rien</returns>
        internal static void DetermineDatabaseColumnNameAndAdoParameter<T>(ref T dataObject_, string mappingDictionariesContainerKey_, string dataObjectcolumnName_, out string dbColumnName_, out KeyValuePair<string, object> adoParameterNameAndValue_)
        {
            dbColumnName_ = null;
            adoParameterNameAndValue_ = new KeyValuePair<string, object>();

            try
            {
                dbColumnName_ = ConfigurationLoader.Instance.GetDbColumnNameFromMappingDictionary(mappingDictionariesContainerKey_, dataObjectcolumnName_);

                adoParameterNameAndValue_ = new KeyValuePair<string, object>(
                                        "@" + dataObjectcolumnName_.ToLowerInvariant(),
                                        dataObject_.GetType().GetProperty(dataObjectcolumnName_).GetValue(dataObject_)
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
        /// <item><description>nom et valeur des paramètres ADO.NET correspondant aux propriétés (nom : proche du nom de la propriété, valeur : valeur de la propriété). </description></item>
        /// </list>
        /// </summary>
        /// <typeparam name="T">Type C#</typeparam>
        /// <param name="dataObject_">Instance d'un objet de la classe T</param>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping</param>
        /// <param name="lstDataObjectcolumnName_">Noms des propriétés de l'objet dataObject_</param>
        /// <param name="lstDbColumnName_">Sortie : noms des colonnes en DB</param>
        /// <param name="adoParameterNameAndValue_">Sortie : clé/valeur des paramètres ADO.NET</param>
        /// <returns>Ne renvoie rien</returns>
        internal static void DetermineDatabaseColumnNamesAndAdoParameters<T>(ref T dataObject_, string mappingDictionariesContainerKey_, List<string> lstDataObjectcolumnName_, out List<string> lstDbColumnName_, out List<KeyValuePair<string, object>> adoParameterNameAndValue_)
        {
            lstDbColumnName_ = new List<string>();

            adoParameterNameAndValue_ = new List<KeyValuePair<string, object>>();
            try
            {
                foreach (string columnName in lstDataObjectcolumnName_)
                {
                    lstDbColumnName_.Add(ConfigurationLoader.Instance.GetDbColumnNameFromMappingDictionary(mappingDictionariesContainerKey_, columnName));

                    adoParameterNameAndValue_.Add(new KeyValuePair<string, object>(
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
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping</param>
        /// <param name="lstDataObjectcolumnName_">Noms des propriétés d'un objet</param>
        /// <param name="lstDbColumnName_">Sortie : noms des colonnes en DB</param>
        /// <returns>Ne renvoie rien</returns>
        internal static void DetermineDatabaseColumnNames(string mappingDictionariesContainerKey_, List<string> lstDataObjectcolumnName_, out List<string> lstDbColumnName_)
        {
            lstDbColumnName_ = new List<string>();

            try
            {
                foreach (string columnName in lstDataObjectcolumnName_)
                {
                    lstDbColumnName_.Add(ConfigurationLoader.Instance.GetDbColumnNameFromMappingDictionary(mappingDictionariesContainerKey_, columnName));
                }
            }
            catch (Exception e)
            {
                // TODO remonter une exception ?
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 3, e.Message);
            }

        }

        /// <summary>
        /// En connaissant le nom du mapping associé à un objet et le nom de sa propriété, génération en sortie de l'information suivante :
        /// <para>nom de la colonne en DB (utilisation de mappingDictionariesContainerKey_ pour interroger le mapping)</para>
        /// </summary>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping</param>
        /// <param name="dataObjectcolumnName_">Nom d'une propriété de l'objet dataObject_</param>
        /// <param name="dbColumnName_">Sortie : nom de la colonne en DB</param>
        /// <returns>Ne renvoie rien</returns>
        private static void DetermineDatabaseColumnName(string mappingDictionariesContainerKey_, string dataObjectcolumnName_, out string dbColumnName_)
        {
            dbColumnName_ = null;

            try
            {
                dbColumnName_ = ConfigurationLoader.Instance.GetDbColumnNameFromMappingDictionary(mappingDictionariesContainerKey_, dataObjectcolumnName_);
            }
            catch (Exception e)
            {
                // TODO remonter une exception ?
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 3, e.Message);
            }

        }

        /// <summary>
        /// En connaissant le nom du mapping associé à un objet, génération en sortie de l'information suivante :
        /// <para>noms des colonnes en DB (utilisation de mappingDictionariesContainerKey_ pour interroger le mapping, lister toutes les colonnes)</para>
        /// </summary>
        /// <param name="mappingDictionariesContainerKey_">Clé pour le dictionnaire de mapping</param>
        /// <param name="lstDbColumnName_">Sortie : noms des colonnes en DB</param>
        /// <param name="lstDataObjectPropertiesNames_">Sortie : noms des propriétés de l'objet associé au mapping</param>
        /// <returns>Ne renvoie rien</returns>
        internal static void DetermineDatabaseColumnsAndPropertiesNames(string mappingDictionariesContainerKey_, out List<string> lstDbColumnName_, out List<string> lstDataObjectPropertiesNames_)
        {
            lstDbColumnName_ = new List<string>();
            lstDataObjectPropertiesNames_ = new List<string>();

            try
            {
                // Ce dictionnaire contient clé/valeur : propriété/nom de colonne
                Dictionary<string, string> mappingObjectSet = ConfigurationLoader.Instance.GetMappingDefinitionsForTable(mappingDictionariesContainerKey_);
                foreach (string key in mappingObjectSet.Keys)
                {
                    lstDataObjectPropertiesNames_.Add(key);
                    lstDbColumnName_.Add(mappingObjectSet[key]);
                }
            }
            catch (Exception e)
            {
                // TODO remonter une exception ?
                ConfigurationLoader._loggerTraceSource.TraceEvent(TraceEventType.Critical, 3, e.Message);
            }

        }

        /// <summary>
        /// Détermine le nom du paramètre ADO.NET selon quelques règles.
        /// <list type="bullet">
        /// <item><description>si chaîne : retourner le nom issu du mapping</description></item>
        /// <item><description>si null : retourner un nom de paramètre "@pN"</description></item>
        /// <item><description>si commence par "@" : retourne la chaîne en lowercase avec espaces remplacés.</description></item>
        /// </list>
        /// </summary>
        /// <param name="value_">Chaîne à traiter selon les règles énoncées ci-dessus</param>
        /// <param name="mappingDictionariesContainerKey_">Clé dans le dictionnaire de mapping</param>
        /// <param name="index_">Index incrémenté servant à savoir où on se trouve dans la liste des paramètres et valeurs.
        /// Sert aussi pour le nom du paramètre dynamique si on avait passé null.</param>
        /// <returns>Nom de colonne DB</returns>
        internal static string DetermineAdoParameterName(string value_, string mappingDictionariesContainerKey_, ref int index_)
        {
            if (value_ == null)
            {
                index_++;
                return "@p" + index_;
            }

            if (value_.StartsWith("@"))
            {
                index_++;
                return value_.ToLowerInvariant().Replace(" ", "_");
            }

            // Dans ce dernier cas c'est une colonne et non pas un paramètre, index_ n'est donc pas modifié.
            string columnName;
            DetermineDatabaseColumnName(mappingDictionariesContainerKey_, value_, out columnName);
            return columnName;
        }

        #endregion


        #region utilities
        /// <summary>
        /// <c>String.Format</c> avec gestion d'exception.
        /// <para>Renvoie faux si le nombre de placeholders et de paramètres ne sont pas égaux.</para>
        /// </summary>
        /// <param name="format_">Chaîne texte avec des placeholders</param>
        /// <param name="result_">Chaine avec les placeholders remplacés si succès, message d'erreur pour l'utilisateur si échec du remplacement (cas d'erreur)</param>
        /// <param name="args_">Valeurs à mettre dans les placeholders</param>
        /// <returns>Renvoie vrai si réussi, sinon retourne faux.</returns>
        internal static bool TryFormat(string format_, out string result_, params Object[] args_)
        {
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
                return false;
            }
        }

        #endregion
    }
}