using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

/*
Copyright (C) 2010-2015, Huagati Systems Co., Ltd. - https://huagati.com

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
*/

namespace HuagatiEDMXTools
{
    /// <summary>
    /// Class used to supply license information to the HuagatiEDMXTools library.
    /// </summary>
    public static class License
    {
        /// <summary>
        /// License version
        /// </summary>
        public static decimal? Version { get; set; }
        /// <summary>
        /// Licensee name
        /// </summary>
        public static string LicenseeName { get; set; }
        /// <summary>
        /// Licensee email
        /// </summary>
        public static string LicenseeEmail { get; set; }
        /// <summary>
        /// License key
        /// </summary>
        public static string LicenseKey { get; set; }
        /// <summary>
        /// License level
        /// </summary>
        public static int LicenseLevel { get; set; }
        /// <summary>
        /// License expiry date
        /// </summary>
        public static DateTime? ExpiryDate { get; set; }
        /// <summary>
        /// License hash data
        /// </summary>
        public static string LicenseData { get; set; }

        /// <summary>
        /// License data for redistribution license
        /// </summary>
        public static string RedistributionLicense { get; set; }

        internal static bool IsValid()
        {
            return true; //checks removed, always valid... left in place for backwards compatibility
        }
    }
}
