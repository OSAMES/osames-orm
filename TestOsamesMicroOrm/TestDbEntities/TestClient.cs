using System;
using System.Collections.ObjectModel;

namespace TestOsamesMicroOrm.TestDbEntities
{
    [Serializable]
    public class TestClient
    {
        /* Représentation DB de l'objet Client
            "id_client"  INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL ON CONFLICT FAIL,
            "numero_client"  INTEGER,
            "nom_societe"  VARCHAR(250),
            "telephone"  VARCHAR(50),
            "fax"  VARCHAR(50),
            "email"  VARCHAR(150),
            "site_internet"  VARCHAR(150),
            "siret"  VARCHAR(100),
            "ape"  VARCHAR(100),
            "tva_intra_communautaire"  VARCHAR(100),
            "id_condition_reglement_ref"  INTEGER,
            "id_type_reglement_ref"  INTEGER,
            "id_adresse_facturation_ref"  INTEGER,
            "id_contact_facturation_ref"  INTEGER,
            "id_contact_ref"  INTEGER,
            "client_date"  TEXT,
            "logo"  BINARY(20480),
            "nom_societe_stripped"  TEXT(255),
         */

        private string _numeroClient;
        private string _nomSociete;
        private string _telephone;
        private string _fax;
        private string _email;
        private string _siteWeb;
        private string _siret;
        private string _tva;
        private string _ape;
        private string _nomSocieteStripped;
        private long _idConditionReglementRef;
        private long _idTypeReglementRef;
        private long _idAdresseFacturationRef;
        private long _idContactFacturationRef;
        private long _idContactPrincipalRef;
        private DateTime _clientDate;
        private byte[] _logo;

        public long IdClient { get; set; } //primary key autoincrement in DB

        /// <summary>
        /// Indicateur de changement de nom du client, utilisé par les règles de validation.
        /// </summary>
        public bool NomSocieteChanged { get; set; }

        /// <summary>
        /// Numéro client. Obligatoire.
        /// </summary>
        public string NumeroClient
        {
            get { return _numeroClient; }
            set
            {
                _numeroClient = value.Trim();
            }
        }
        /// <summary>
        /// Nom société. Obligatoire.
        /// </summary>
        public string NomSociete
        {
            get { return _nomSociete; }
            set
            {
                NomSocieteChanged = false;


                _nomSociete = value.Trim();
                _nomSocieteStripped = "fake";

                NomSocieteChanged = true;
            }
        }

        public string Telephone
        {
            get { return _telephone; }
            set
            {
                _telephone = value;

            }
        }

        public string Fax
        {
            get { return _fax; }
            set
            {
                _fax = value;

            }
        }

        public string Email
        {
            get { return _email; }
            set
            {
                _email = value;

            }
        }

        public string SiteWeb
        {
            get { return _siteWeb; }
            set
            {
                _siteWeb = value;

            }
        }

        public string Siret
        {
            get { return _siret; }
            set
            {
                _siret = value;

            }
        }

        public string Tva
        {
            get { return _tva; }
            set
            {
                //if (string.IsNullOrWhiteSpace(value))
                //{
                //    // validation et gestion dans la liste : ajout d'erreur
                //    OnSetPropertyError("Tva", "Pas de numéro de TVA", ErrorLevel.Error);
                //    return;
                //}
                //// validation et gestion dans la liste : suppression d'erreur
                //OnSetPropertyNoError("Tva");

                _tva = value.Trim();

            }
        }

        public string Ape
        {
            get { return _ape; }
            set { _ape = value.Trim(); }
        }

        public long IdConditionReglementRef
        {
            get { return _idConditionReglementRef; }
            set
            {
                _idConditionReglementRef = value;

            }
        }

        public long IdTypeReglementRef
        {
            get { return _idTypeReglementRef; }
            set
            {
                _idTypeReglementRef = value;

            }
        }

        public long IdAdresseFacturationRef
        {
            get { return _idAdresseFacturationRef; }
            set
            {
                _idAdresseFacturationRef = value;

            }
        }

        public long IdContactFacturationRef
        {
            get { return _idContactFacturationRef; }
            set
            {
                _idContactFacturationRef = value;

            }
        }

        public long IdContactPrincipalRef
        {
            get { return _idContactPrincipalRef; }
            set
            {
                _idContactPrincipalRef = value;

            }
        }

        public DateTime ClientDate
        {
            get { return _clientDate; }
            set
            {
                _clientDate = value;

            }
        }

        // relations tables externe
        public ObservableCollection<TestAdresse> Adresses { get; set; }


        /// <summary>
        /// Propriété utilitaire.
        /// Vrai pour un nouveau client avant d'afficher le formulaire d'édition (donc juste après avoir cliqué sur le bouton "nouveau client").
        /// </summary>
        public bool IsNewEditing { get { return string.IsNullOrWhiteSpace(NumeroClient); } }

        public byte[] Logo
        {
            get { return _logo; }
            set
            {
                // Réservation d'une zone mémoire de la taille maximale désirée
                _logo = new byte[20480];
                _logo = value;
            }
        }

        public string NomSocieteStripped
        {
            get { return _nomSocieteStripped; }
            set { _nomSocieteStripped = value; }

        }

        /// <summary>
        /// Constructeur : 
        /// - initialisation des listes internes
        /// - initialisation de la date à la date du jour
        /// - pas de mise en erreur des propriétés obligatoires, pour ne pas perturber l'utilisateur.
        /// </summary>
        public TestClient()
        {

            Adresses = new ObservableCollection<TestAdresse>();


            ClientDate = DateTime.Today;
            // test
            //ClientDate = new DateTime(2013, 11, 1);
        }
    }
}
