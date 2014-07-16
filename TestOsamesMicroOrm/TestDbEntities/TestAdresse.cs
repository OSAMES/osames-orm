using System;
using System.Text;
using OsamesMicroOrm;

namespace TestOsamesMicroOrm.TestDbEntities
{
    /// <summary>
    /// Adresse d'un client.
    /// </summary>
    [Serializable]
    [DatabaseMapping("adresses")]
    public class TestAdresse
    {
        private string _numberStreetName;
        private string _town;
        private string _zipCode;
        private string _country;
        private bool _isFacturation;

        public long IdAdresse { get; set; } //primary key autoincrement in DB
        public long IdClient { get; set; } // foreign key

        public string NumberStreetName
        {
            get { return _numberStreetName; }
            set
            {
                _numberStreetName = value.Trim();
            }
        }

        public string Town
        {
            get { return _town; }
            set
            {
                _town = value.Trim();

            }
        }

        public string ZipCode
        {
            get { return _zipCode; }
            set
            {
                _zipCode = value.Trim();
            }
        }

        public string Country
        {
            get { return _country; }
            set
            {
                _country = value.Trim();
            }
        }

        public bool IsFacturation
        {
            get { return _isFacturation; }
            set
            {
                _isFacturation = value;
            }
        }
       

#if DEBUG
        // Pour débuger le contenu des variables en compilation debug uniquement
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(NumberStreetName ?? "")
                .Append(" ")
                .Append(ZipCode ?? "")
                .Append(" ")
                .Append(Town ?? "");
            return sb.ToString();
        }

#endif
    }
}
