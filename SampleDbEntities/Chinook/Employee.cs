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

namespace SampleDbEntities.Chinook
{
    /* representation DB de l'objet Employee
       EmployeeId INTEGER NOT NULL,
       LastName NVARCHAR(20) NOT NULL,
       FirstName NVARCHAR(20) NOT NULL,
       Title NVARCHAR(30),
       ReportsTo INTEGER,
       BirthDate DATETIME,
       HireDate DATETIME,
       Address NVARCHAR(70),
       City NVARCHAR(40),
       State NVARCHAR(40),
       Country NVARCHAR(40),
       PostalCode NVARCHAR(10),
       Phone NVARCHAR(24),
       Fax NVARCHAR(24),
       Email NVARCHAR(60),
       CONSTRAINT EMPLOYEE_PK PRIMARY KEY (EmployeeId),
       CONSTRAINT EMPLOYEE_FK_EMPLOYEE FOREIGN KEY (ReportsTo) REFERENCES Employee(EmployeeId)
     */

    [Serializable]
    public class Employee
    {
        private long _employeeId;
        private string _lastName;
        private string _firstName;
        private string _title;
        private long _reportsTo;
        private DateTime _birthDate;
        private DateTime _hireDate;
        private string _address;
        private string _city;
        private string _state;
        private string _country;
        private string _postalCode;
        private string _phone;
        private string _fax;
        private string _email;

        /// <summary>
        /// 
        /// </summary>
        public long EmployeeId
        {
            get { return _employeeId; }
            set { _employeeId = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string LastName
        {
            get { return _lastName; }
            set { _lastName = value.Trim(); }
        }

       /// <summary>
       /// 
       /// </summary>
        public string FirstName
        {
            get { return _firstName; }
            set { _firstName = value.Trim(); }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Title
        {
            get { return _title; }
            set { _title = value.Trim(); }
        }

        /// <summary>
        /// 
        /// </summary>
        public long ReportsTo
        {
            get { return _reportsTo; }
            set { _reportsTo = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public DateTime BirthDate
        {
            get { return _birthDate; }
            set { _birthDate = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public DateTime HireDate
        {
            get { return _hireDate; }
            set { _hireDate = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Address
        {
            get { return _address; }
            set { _address = value.Trim(); }
        }

        /// <summary>
        /// 
        /// </summary>
        public string City
        {
            get { return _city; }
            set { _city = value.Trim(); }
        }

        /// <summary>
        /// 
        /// </summary>
        public string State
        {
            get { return _state; }
            set { _state = value.Trim(); }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Country
        {
            get { return _country; }
            set { _country = value.Trim(); }
        }

        /// <summary>
        /// 
        /// </summary>
        public string PostalCode
        {
            get { return _postalCode; }
            set { _postalCode = value.Trim(); }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Phone
        {
            get { return _phone; }
            set { _phone = value.Trim(); }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Fax
        {
            get { return _fax; }
            set { _fax = value.Trim(); }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Email
        {
            get { return _email; }
            set { _email = value.Trim(); }
        }

        /// <summary>
        /// 
        /// </summary>
        public ObservableCollection<Customer> Customer;

        /// <summary>
        /// Constructor
        /// </summary>
        public Employee()
        {
            Customer = new ObservableCollection<Customer>(new List<Customer>());
        }
    }
}
