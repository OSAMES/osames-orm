﻿/*
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
using OsamesMicroOrm;

namespace SampleDbEntities.Chinook
{
    /* representation DB de l'objet InvoiceLine
        InvoiceLineId INTEGER NOT NULL,
	    InvoiceId INTEGER NOT NULL,
	    TrackId INTEGER NOT NULL,
	    UnitPrice NUMERIC(10,2) NOT NULL,
	    Quantity INTEGER NOT NULL,
	    CONSTRAINT INVOICELINE_PK PRIMARY KEY (InvoiceLineId),
	    CONSTRAINT INVOICELINE_FK_INVOICE FOREIGN KEY (InvoiceId) REFERENCES Invoice(InvoiceId),
	    CONSTRAINT INVOICELINE_FK_TRACK FOREIGN KEY (TrackId) REFERENCES Track(TrackId)
     */
    [Serializable]
    [DatabaseMapping("InvoiceLine")]
    public class InvoiceLine : DatabaseEntityObject
    {
        private long _invoiceLineId;
        private long _invoiceId;
        private long _trackId;
        private decimal _unitPrice;
        private int _quantity;

        /// <summary>
        /// 
        /// </summary>
        public long InvoiceLineId
        {
            get { return _invoiceLineId; }
            set { _invoiceLineId = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public long InvoiceId
        {
            get { return _invoiceId; }
            set { _invoiceId = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public long TrackId
        {
            get { return _trackId; }
            set { _trackId = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal UnitPrice
        {
            get { return _unitPrice; }
            set { _unitPrice = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public int Quantity
        {
            get { return _quantity; }
            set { _quantity = value; }
        }

        public Track Track;

        /// <summary>
        /// Constructor
        /// </summary>
        public InvoiceLine()
        {
            Track = new Track();
        }

        public override void Copy<T>(T object_)
        {
            throw new NotImplementedException();
        }
    }
}
