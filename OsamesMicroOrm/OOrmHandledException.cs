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
using System.Runtime.Serialization;

namespace OsamesMicroOrm
{
    /// <summary>
    /// OOrmHandledException classe fille d'une Exception en nous permettant d'ajouter nos propre données.
    /// C'est la seul classe de type Exception à utilier dans l'orm
    /// Exception crée lorsqu'une exception non gérée se produit.
    /// Cette exception encapsule l'exception initiale et assure son traçage dans le log et le cas échéant d'autres traitements.
    /// C'est la seule exception en sortie de l'ORM vers l'application cliente.
    /// </summary>
    [Serializable]
    public class OOrmHandledException : Exception
    {
        string FormattedMessage;

        internal Exception EInnerException;

        /// <summary>
        /// Comme on ne peut pas positionner Message qui n'a qu'un getter dans la classe Exception, on surcharge cet getter pour renvoyer une chaîne que nous avons formatée.
        /// </summary>
        public override string Message
        {
            get
            {
                return FormattedMessage;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public new Exception InnerException
        {
            get
            {
                return EInnerException;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        protected OOrmHandledException()
        {
            // Nous n'avons pas précisé de message formaté donc reprenons celui de la classe Exception.
            FormattedMessage = base.Message;
        }

        /// <summary>
        /// Serialization Contructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected OOrmHandledException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // Nous n'avons pas précisé de message formaté donc reprenons celui de la classe Exception.
            FormattedMessage = base.Message;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="errorCode_">Code d'erreur</param>
        /// <param name="innerException_">Exception d'origine optionnelle</param>
        public OOrmHandledException(HResultEnum errorCode_, Exception innerException_)
        {
            System.Collections.Generic.KeyValuePair<int, string> errorHresultCodeAndDetailedMessageWithLogging = Utilities.OOrmErrorsHandler.ProcessOrmException(errorCode_, innerException_, Utilities.ErrorType.ERROR, null);
            // positionnement de notre message formaté
            FormattedMessage = errorHresultCodeAndDetailedMessageWithLogging.Value;
            // transformation de la chaîne "0X1234" en integer
            HResult = errorHresultCodeAndDetailedMessageWithLogging.Key;

            EInnerException = innerException_;

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="errorCode_">Code d'erreur</param>
        /// <param name="innerException_">Exception d'origine optionnelle</param>
        /// <param name="additionalMessage_">Texte complémentaire à celui du message associé au code d'erreur</param>
        public OOrmHandledException(HResultEnum errorCode_, Exception innerException_, string additionalMessage_)
        {
            System.Collections.Generic.KeyValuePair<int, string> errorHresultCodeAndDetailedMessageWithLogging = Utilities.OOrmErrorsHandler.ProcessOrmException(errorCode_, innerException_, Utilities.ErrorType.ERROR, additionalMessage_);
            // positionnement de notre message formaté
            FormattedMessage = errorHresultCodeAndDetailedMessageWithLogging.Value;
            // transformation de la chaîne "0X1234" en integer
            HResult = errorHresultCodeAndDetailedMessageWithLogging.Key;

            EInnerException = innerException_;
        }
    }
}
