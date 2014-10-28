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
using System.Collections.ObjectModel;
using OsamesMicroOrm;

namespace SampleDbEntities.Chinook
{
    /* representation DB de l'objet Invoice
        InvoiceId INTEGER NOT NULL,
	    IdCustomer INTEGER NOT NULL,
	    InvoiceDate DATETIME NOT NULL,
	    BillingAddress NVARCHAR(70),
	    BillingCity NVARCHAR(40),
	    BillingState NVARCHAR(40),
	    BillingCountry NVARCHAR(40),
	    BillingPostalCode NVARCHAR(10),
	    Total NUMERIC(10,2) NOT NULL,
	    CONSTRAINT INVOICE_PK PRIMARY KEY (InvoiceId),
	    CONSTRAINT INVOICE_FK_CUSTOMER FOREIGN KEY (IdCustomer) REFERENCES Customer(CustomerId)
     */

    [Serializable]
    [DatabaseMapping("Invoice")]
    public class Invoice
    {
        private long _customerId;
        private DateTime _invoiceDate;
        private string _billingAddress;
        private string _billingCity;
        private string _billingState;
        private string _billingCountry;
        private string _billingPostalCode;
        private decimal _total;

        /// <summary>
        /// 
        /// </summary>
        public long InvoiceId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public long CustomerId
        {
            get { return _customerId; }
            set { _customerId = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public DateTime InvoiceDate
        {
            get { return _invoiceDate; }
            set { _invoiceDate = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string BillingAddress
        {
            get { return _billingAddress; }
            set { _billingAddress = value.Trim(); }
        }

        /// <summary>
        /// 
        /// </summary>
        public string BillingCity
        {
            get { return _billingCity; }
            set { _billingCity = value.Trim(); }
        }

        /// <summary>
        /// 
        /// </summary>
        public string BillingState
        {
            get { return _billingState; }
            set { _billingState = value.Trim(); }
        }

        /// <summary>
        /// 
        /// </summary>
        public string BillingCountry
        {
            get { return _billingCountry; }
            set { _billingCountry = value.Trim(); }
        }

        /// <summary>
        /// 
        /// </summary>
        public string BillingPostalCode
        {
            get { return _billingPostalCode; }
            set { _billingPostalCode = value.Trim(); }
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal Total
        {
            get { return _total; }
            set { _total = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public ObservableCollection<InvoiceLine> InvoiceLine;

        /// <summary>
        /// Constructor
        /// </summary>
        public Invoice ()
        {
            InvoiceLine = new ObservableCollection<InvoiceLine>(new List<InvoiceLine>());
        }
    }
}
