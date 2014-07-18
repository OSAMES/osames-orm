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

namespace SampleDbEntities.Chinook
{
    /* representation DB de l'objet Track
        TrackId INTEGER NOT NULL,
	    Name NVARCHAR(200) NOT NULL,
	    AlbumId INTEGER,
	    MediaTypeId INTEGER NOT NULL,
	    GenreId INTEGER,
	    Composer NVARCHAR(220),
	    Milliseconds INTEGER NOT NULL,
	    Bytes INTEGER,
	    UnitPrice NUMERIC(10,2) NOT NULL,  (http://stackoverflow.com/questions/6771891/what-is-the-equivalent-datatype-of-sql-servers-numeric-in-c-sharp)
	    CONSTRAINT TRACK_PK PRIMARY KEY (TrackId),
	    CONSTRAINT TRACK_FK_ALBUM FOREIGN KEY (AlbumId) REFERENCES Album(AlbumId),
	    CONSTRAINT TRACK_FK_GENRE FOREIGN KEY (GenreId) REFERENCES Genre(GenreId),
	    CONSTRAINT TRACK_FK_MEDIATYPE FOREIGN KEY (MediaTypeId) REFERENCES MediaType(MediaTypeId)
     */
    [Serializable]
    public class Track
    {
        private long _trackId;
        private string _name;
        private long _albumId;
        private long _mediaTypeId;
        private long _genreId;
        private string _composer;
        private long _milliseconds;
        private decimal _unitPrice;

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
        public string Name
        {
            get { return _name; }
            set { _name = value.Trim(); }
        }

        /// <summary>
        /// 
        /// </summary>
        public long AlbumId
        {
            get { return _albumId; }
            set { _albumId = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public long MediaTypeId
        {
            get { return _mediaTypeId; }
            set { _mediaTypeId = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public long GenreId
        {
            get { return _genreId; }
            set { _genreId = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Composer
        {
            get { return _composer; }
            set { _composer = value.Trim(); }
        }

        /// <summary>
        /// 
        /// </summary>
        public long Milliseconds
        {
            get { return _milliseconds; }
            set { _milliseconds = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public decimal UnitPrice
        {
            get { return _unitPrice; }
            set { _unitPrice = value; }
        }
    }
}
