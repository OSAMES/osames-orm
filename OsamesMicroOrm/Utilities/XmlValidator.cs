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
using System.Xml;
using System.Xml.Schema;

namespace OsamesMicroOrm.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    public class XmlValidator
    {
        /// <summary>
        /// Errors.
        /// </summary>
        public List<string> Errors { get; private set; }

        /// <summary>
        /// Warnings. Some warnings are as critical as errors, such as not finding XML schema.
        /// </summary>
        public List<string> Warnings { get; private set; }

        private readonly XmlReaderSettings _settings;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="xmlNamespaces_">XML schemas base namespaces</param>
        /// <param name="xmlSchemas_">XML schemas .xsd files full paths</param>
        public XmlValidator(string[] xmlNamespaces_ = null, string[] xmlSchemas_ = null)
        {
            Errors = new List<string>();
            Warnings = new List<string>();

            if (xmlNamespaces_ != null && xmlSchemas_ != null)
            {
                if(xmlNamespaces_.Length != xmlSchemas_.Length)
                    throw new ArgumentException("Not same number of namespaces and schemas given");
            }
            if (xmlSchemas_ != null && xmlNamespaces_ == null)
            {
                throw new ArgumentException("Schema given but no namespaces");
            }
            
            _settings = new XmlReaderSettings
                {
                    ValidationType = ValidationType.Schema,
                    ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings
                };
            _settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation;

            if (xmlSchemas_ != null)
            {
                for (int i = 0; i < xmlSchemas_.Length; i++)
                {
                    Common.CheckFile(xmlSchemas_[i], "XmlValidator");
                    _settings.Schemas.Add(xmlNamespaces_[i], xmlSchemas_[i]);
                }
            }

            _settings.ValidationType = ValidationType.Schema;
            _settings.ValidationEventHandler += validationEventHandler;
            
        }
        /// <summary>
        /// XML validation.
        /// </summary>
        /// <param name="xmlFile_">Xml file full path</param>
        public void ValidateXml(string xmlFile_)
        {
            Common.CheckFile(xmlFile_, "XmlValidator");
            XmlReader xml = XmlReader.Create(xmlFile_, _settings);
            while (xml.Read()) { }
            if (Errors.Count == 0 && Warnings.Count == 0) return;
            StringBuilder sb = new StringBuilder();
            foreach (string err in Errors)
                sb.Append(err).Append(Environment.NewLine);
            foreach (string err in Warnings)
                sb.Append(err).Append(Environment.NewLine);
                
            throw new Exception(string.Format("XML validation errors: {0}", sb));
        }

        /// <summary>
        /// XML validation, multiple XML files.
        /// </summary>
        /// <param name="xmlFiles_">Xml file full path</param>
        public void ValidateXml(string[] xmlFiles_)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string xmlFile in xmlFiles_)
            {
                Common.CheckFile(xmlFile, "XmlValidator");
                XmlReader xml = XmlReader.Create(xmlFile, _settings);
                while (xml.Read())
                {
#if DEBUG
                  //  Debug.WriteLine(xml.Name);
#endif 
                } 
            }
            if (Errors.Count == 0 && Warnings.Count == 0) return;
            foreach (string err in Errors)
                sb.Append(err).Append(Environment.NewLine);
            foreach (string err in Warnings)
                sb.Append(err).Append(Environment.NewLine);

            throw new Exception(string.Format("XML validation errors: {0}", sb));
        }

        /// <summary>
        /// XML validation event handler : fills Errors and Warnings lists.
        /// </summary>
        /// <param name="sender_"></param>
        /// <param name="e_"></param>
        private void validationEventHandler(object sender_, ValidationEventArgs e_)
        {
            switch (e_.Severity)
            {
                case XmlSeverityType.Warning:
                    Warnings.Add(e_.Message);
#if DEBUG
                    Console.Write("WARNING: ");
                    Console.WriteLine(e_.Message);
#endif
                    break;
                case XmlSeverityType.Error:
                    Errors.Add(e_.Message);
#if DEBUG
                    Console.Write("ERROR: ");
                    Console.WriteLine(e_.Message);
#endif
                    break;
            }
        }
    }
}
