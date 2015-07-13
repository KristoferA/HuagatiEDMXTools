using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    /// Represents a property that exists more than once in a model. This class is used together with the GetCommonProperties methods in the conceptual model queries
    /// </summary>
    public class CommonModelProperty
    {
        internal CommonModelProperty() { }
        internal CommonModelProperty(CommonModelProperty mpd)
        {
            this.Name = mpd.Name;
            this.TypeName = mpd.TypeName;
            this.Nullable = mpd.Nullable;
            this.MaxLength = mpd.MaxLength;
            this.Precision = mpd.Precision;
            this.Scale = mpd.Scale;
            this.EntityTypes = mpd.EntityTypes;
            this.TypeDescription = mpd.TypeDescription;
        }

        /// <summary>
        /// Returns a hash code based on the member values, for comparison with other instances of this class.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            string s = Name + "_" + TypeName + "_" + Nullable.ToLString() + "_" + MaxLength.ToString() + "_" + Scale.ToString() + "_" + Precision.ToString();
            return s.GetHashCode();
        }

        /// <summary>
        /// Memberwise equality between this and another instance of this class.
        /// </summary>
        /// <param name="obj">Other instance to compare to this object.</param>
        /// <returns>True if member values are equal, false if not.</returns>
        public override bool Equals(object obj)
        {
            if (obj is CommonModelProperty)
            {
                CommonModelProperty mpd = (CommonModelProperty)obj;
                return (this.Name.Equals(mpd.Name)
                    && this.TypeName.Equals(mpd.TypeName)
                    && this.Nullable.Equals(mpd.Nullable)
                    && this.MaxLength.Equals(mpd.MaxLength)
                    && this.Precision.Equals(mpd.Precision)
                    && this.Scale.Equals(mpd.Scale));
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Property name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Property type name
        /// </summary>
        public string TypeName { get; set; }
        /// <summary>
        /// Indicates whether the property is nullable or not
        /// </summary>
        public bool Nullable { get; set; }
        /// <summary>
        /// Max length, for strings and binary members
        /// </summary>
        public int MaxLength { get; set; }
        /// <summary>
        /// Scale, for floating point members
        /// </summary>
        public int Scale { get; set; }
        /// <summary>
        /// Precision, for floating point members
        /// </summary>
        public int Precision { get; set; }
        /// <summary>
        /// Longer type description
        /// </summary>
        public string TypeDescription { get; set; }

        /// <summary>
        /// Entity types that this property is found in
        /// </summary>
        public List<ModelEntityType> EntityTypes { get; set; }

        /// <summary>
        /// Human readable description with name, type, and number of entity types that this property occur in.
        /// </summary>
        public string Description
        {
            get
            {
                return Name + " " + TypeDescription + " (" + EntityTypes.Count().ToString() + ")";
            }
        }
    }
}
