using System.IO;
using MaxMind.Db;
using Newtonsoft.Json.Linq;

namespace osuBancho.Core.Helpers
{
    static class GeoUtil
    {
        private static readonly string[] Countries = new string[]
                                               {
                                                   "Unknown", "Oceania", "Europe", "Andorra", "UAE", "Afghanistan",
                                                   "Antigua", "Anguilla", "Albania", "Armenia", "Netherlands Antilles",
                                                   "Angola", "Antarctica", "Argentina", "American Samoa", "Austria",
                                                   "Australia", "Aruba", "Azerbaijan", "Bosnia", "Barbados", "Bangladesh",
                                                   "Belgium", "Burkina Faso", "Bulgaria", "Bahrain", "Burundi", "Benin",
                                                   "Bermuda", "Brunei Darussalam", "Bolivia", "Brazil", "Bahamas",
                                                   "Bhutan", "Bouvet Island", "Botswana", "Belarus", "Belize", "Canada",
                                                   "Cocos Islands", "Congo", "Central African Republic", "Congo",
                                                   "Switzerland", "Cote D'Ivoire", "Cook Islands", "Chile", "Cameroon",
                                                   "China", "Colombia", "Costa Rica", "Cuba", "Cape Verde",
                                                   "Christmas Island", "Cyprus", "Czech Republic", "Germany", "Djibouti",
                                                   "Denmark", "Dominica", "Dominican Republic", "Algeria", "Ecuador",
                                                   "Estonia", "Egypt", "Western Sahara", "Eritrea", "Spain", "Ethiopia",
                                                   "Finland", "Fiji", "Falkland Islands",
                                                   "Micronesia, Federated States of", "Faroe Islands", "France",
                                                   "France, Metropolitan", "Gabon", "United Kingdom", "Grenada", "Georgia",
                                                   "French Guiana", "Ghana", "Gibraltar", "Greenland", "Gambia", "Guinea",
                                                   "Guadeloupe", "Equatorial Guinea", "Greece", "South Georgia",
                                                   "Guatemala", "Guam", "Guinea-Bissau", "Guyana", "Hong Kong",
                                                   "Heard Island", "Honduras", "Croatia", "Haiti", "Hungary", "Indonesia",
                                                   "Ireland", "Israel", "India", "British Indian Ocean Territory", "Iraq",
                                                   "Iran, Islamic Republic of", "Iceland", "Italy", "Jamaica", "Jordan",
                                                   "Japan", "Kenya", "Kyrgyzstan", "Cambodia", "Kiribati", "Comoros",
                                                   "St. Kitts and Nevis", "Korea, Democratic People's Republic of",
                                                   "Korea", "Kuwait", "Cayman Islands", "Kazakhstan", "Lao", "Lebanon",
                                                   "St. Lucia", "Liechtenstein", "Sri Lanka", "Liberia", "Lesotho",
                                                   "Lithuania", "Luxembourg", "Latvia", "Libyan Arab Jamahiriya",
                                                   "Morocco", "Monaco", "Moldova, Republic of", "Madagascar",
                                                   "Marshall Islands", "Macedonia, the Former Yugoslav Republic of",
                                                   "Mali", "Myanmar", "Mongolia", "Macau", "Northern Mariana Islands",
                                                   "Martinique", "Mauritania", "Montserrat", "Malta", "Mauritius",
                                                   "Maldives", "Malawi", "Mexico", "Malaysia", "Mozambique", "Namibia",
                                                   "New Caledonia", "Niger", "Norfolk Island", "Nigeria", "Nicaragua",
                                                   "Netherlands", "Norway", "Nepal", "Nauru", "Niue", "New Zealand",
                                                   "Oman", "Panama", "Peru", "French Polynesia", "Papua New Guinea",
                                                   "Philippines", "Pakistan", "Poland", "St. Pierre", "Pitcairn",
                                                   "Puerto Rico", "Palestinian Territory", "Portugal", "Palau", "Paraguay",
                                                   "Qatar", "Reunion", "Romania", "Russian Federation", "Rwanda",
                                                   "Saudi Arabia", "Solomon Islands", "Seychelles", "Sudan", "Sweden",
                                                   "Singapore", "St. Helena", "Slovenia", "Svalbard and Jan Mayen",
                                                   "Slovakia", "Sierra Leone", "San Marino", "Senegal", "Somalia",
                                                   "Suriname", "Sao Tome and Principe", "El Salvador",
                                                   "Syrian Arab Republic", "Swaziland", "Turks and Caicos Islands", "Chad",
                                                   "French Southern Territories", "Togo", "Thailand", "Tajikistan",
                                                   "Tokelau", "Turkmenistan", "Tunisia", "Tonga", "Timor-Leste", "Turkey",
                                                   "Trinidad and Tobago", "Tuvalu", "Taiwan", "Tanzania", "Ukraine",
                                                   "Uganda", "US (Island)", "United States", "Uruguay", "Uzbekistan",
                                                   "Holy See", "St. Vincent", "Venezuela", "Virgin Islands, British",
                                                   "Virgin Islands, U.S.", "Vietnam", "Vanuatu", "Wallis and Futuna",
                                                   "Samoa", "Yemen", "Mayotte", "Serbia", "South Africa", "Zambia",
                                                   "Montenegro", "Zimbabwe", "Unknown", "Satellite Provider", "Other",
                                                   "Aland Islands", "Guernsey", "Isle of Man", "Jersey", "St. Barthelemy",
                                                   "Saint Martin"
                                               };

        private static Reader dbReader;

        /// <summary>
        /// Initialize the GeoUtil.
        /// </summary>
        public static void Initialize()
        {
            if (File.Exists("GeoLite2-City.mmdb")) dbReader = new Reader("GeoLite2-City.mmdb", FileAccessMode.MemoryMapped);
        }

        /// <summary>
        /// Get the ip address geodata from GeoLite2-City database.
        /// </summary>
        internal static JToken GetDataFromIPAddress(string ipAddress)
        {
            return dbReader?.Find(ipAddress);
        }

        /// <summary>
        /// Get the country id from its name.
        /// </summary>
        /// <param name="countryName">English country name</param>
        internal static int GetCountryId(string countryName)
        {
            var id = System.Array.IndexOf(Countries, countryName);
            return id != -1 ? id : 0;
        }
    }
}