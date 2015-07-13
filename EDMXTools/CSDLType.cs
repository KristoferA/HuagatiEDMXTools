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
    /// Class used for comparing type between scalar members. Translates ModelMemberProperty, StoreMemberProperty, StoreFunctionParameter and ModelFunctionParameter to a comparable object
    /// </summary>
    public class CSDLType
    {
        internal CSDLType(ModelMemberProperty modelMemberProperty)
        {
            this.Name = modelMemberProperty.Name;
            this.CLRTypeName = modelMemberProperty.TypeName;
            this.MaxLength = modelMemberProperty.MaxLength;
            this.Nullable = modelMemberProperty.Nullable;
            this.Precision = modelMemberProperty.Precision;
            this.Scale = modelMemberProperty.Scale;
            this.FixedLength = modelMemberProperty.FixedLength;
            this.IsUnicode = modelMemberProperty.IsUnicode;
            this.CompareIsUnicode = true;
        }

        internal CSDLType(StoreMemberProperty storeMemberProperty)
        {
            this.Name = storeMemberProperty.Name;
            bool isUnicode = false;
            this.CLRTypeName = CSDLTypeNameFromSSDLTypeName(storeMemberProperty.DataType, out isUnicode);
            this.IsUnicode = isUnicode;
            this.MaxLength = storeMemberProperty.MaxLength;
            this.Nullable = storeMemberProperty.Nullable;
            this.Precision = storeMemberProperty.Precision;
            this.Scale = storeMemberProperty.Scale;
            this.FixedLength = storeMemberProperty.FixedLength;
            this.CompareIsUnicode = true;
        }

        internal CSDLType(StoreFunctionParameter storeFunctionParameter)
        {
            this.Name = storeFunctionParameter.Name;
            bool isUnicode = false;
            this.CLRTypeName = CSDLTypeNameFromSSDLTypeName(storeFunctionParameter.DataType, out isUnicode);
            this.IsUnicode = isUnicode;
            this.MaxLength = storeFunctionParameter.MaxLength;
            this.Nullable = true;
            this.Precision = storeFunctionParameter.Precision;
            this.Scale = storeFunctionParameter.Scale;
            this.FixedLength = storeFunctionParameter.FixedLength;
            this.CompareIsUnicode = true;
        }

        internal CSDLType(ModelFunctionParameter modelFunctionParameter)
        {
            this.Name = modelFunctionParameter.Name;
            this.CLRTypeName = modelFunctionParameter.TypeName;
            this.MaxLength = modelFunctionParameter.MaxLength;
            this.Nullable = true;// modelMemberProperty.Nullable;
            this.Precision = modelFunctionParameter.Precision;
            this.Scale = modelFunctionParameter.Scale;
            this.FixedLength = false;// modelMemberProperty.FixedLength;
            this.IsUnicode = false;
            this.CompareIsUnicode = false;
        }

        internal static string CSDLTypeNameFromSSDLTypeName(string dataTypeName, out bool isUnicode)
        {
            string clrTypeName = null;
            isUnicode = false;
            switch (dataTypeName.ToLower())
            {
                case "bigint":
                    clrTypeName = "Int64";
                    break;
                case "binary":
                case "binary(max)":
                    clrTypeName = "Binary";
                    break;
                case "bit":
                    clrTypeName = "Boolean";
                    break;
                case "char":
                case "char(max)":
                    clrTypeName = "String";
                    break;
                case "date":
                case "datetime":
                case "datetime2":
                    clrTypeName = "DateTime";
                    break;
                case "time":
                    clrTypeName = "Time";
                    break;
                case "datetimeoffset":
                    clrTypeName = "DateTimeOffset";
                    break;
                case "decimal":
                    clrTypeName = "Decimal";
                    break;
                case "float":
                    clrTypeName = "Double";
                    break;
                case "image":
                    clrTypeName = "Binary";
                    break;
                case "int":
                    clrTypeName = "Int32";
                    break;
                case "money":
                    clrTypeName = "Decimal";
                    break;
                case "nchar":
                case "nchar(max)":
                case "nvarchar":
                case "nvarchar(max)":
                case "ntext":
                    clrTypeName = "String";
                    isUnicode = true;
                    break;
                case "numeric":
                    clrTypeName = "Decimal";
                    break;
                case "uniqueidentifier":
                    clrTypeName = "Guid";
                    break;
                case "real":
                    clrTypeName = "Single";
                    break;
                case "smalldatetime":
                    clrTypeName = "DateTime";
                    break;
                case "smallint":
                    clrTypeName = "Int16";
                    break;
                case "smallmoney":
                    clrTypeName = "Decimal";
                    break;
                case "text":
                    clrTypeName = "String";
                    break;
                case "timestamp":
                case "rowversion":
                    clrTypeName = "Binary";
                    break;
                case "tinyint":
                    clrTypeName = "Byte";
                    break;
                case "varbinary":
                case "varbinary(max)":
                    clrTypeName = "Binary";
                    break;
                case "varchar":
                case "varchar(max)":
                    clrTypeName = "String";
                    break;
                case "xml":
                    clrTypeName = "String";
                    isUnicode = true;
                    break;
                case "geography":
                    clrTypeName = "Geography";
                    break;
                case "geometry":
                    clrTypeName = "Geometry";
                    break;
            }
            return clrTypeName;
        }

        /// <summary>
        /// Generates a hash code based on the public member values
        /// </summary>
        /// <returns>Hash code usable for comparing with other instances of this class</returns>
        public override int GetHashCode()
        {
            return (this.CLRTypeName + "_" + this.Nullable.ToLString() + "_" + this.FixedLength.ToLString() + "_" + this.MaxLength.ToString() + "_" + this.Precision.ToString() + "_" + this.Scale.ToString()).GetHashCode();
        }

        /// <summary>
        /// Compares this instance with another instance of the same class. If the public members match, returns true.
        /// </summary>
        /// <param name="obj">Another instance of this class</param>
        /// <returns>True if the compared objects have identical member values, false if not</returns>
        public override bool Equals(object obj)
        {
            try
            {
                if (obj is CSDLType)
                {
                    CSDLType csdlType = (CSDLType)obj;
                    bool isEqual = (
                        this.CLRTypeName.Equals(csdlType.CLRTypeName, StringComparison.InvariantCultureIgnoreCase)
                        && this.Nullable == csdlType.Nullable
                        && this.FixedLength == csdlType.FixedLength
                        && (this.MaxLength == csdlType.MaxLength || this.MaxLength == 0 || csdlType.MaxLength == 0)
                        && this.Precision == csdlType.Precision
                        && this.Scale == csdlType.Scale
                        && (this.IsUnicode == csdlType.IsUnicode || this.CompareIsUnicode == false || csdlType.CompareIsUnicode == false)
                        );
                    return isEqual;
                }
                else
                {
                    return base.Equals(obj);
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Corresponding CLR type
        /// </summary>
        public Type CLRType
        {
            get
            {
                return Type.GetType(this.CLRTypeName);
            }
        }

        internal string Name { get; set; }

        /// <summary>
        /// CLR type name
        /// </summary>
        public string CLRTypeName { get; private set; }

        /// <summary>
        /// Indicates if a member is nullable or not
        /// </summary>
        public bool Nullable { get; private set; }

        /// <summary>
        /// Indicates if a string or binary member is of fixed length
        /// </summary>
        public bool FixedLength { get; private set; }

        /// <summary>
        /// Indicates if a string member is Unicode or ANSI
        /// </summary>
        public bool IsUnicode { get; private set; }

        /// <summary>
        /// Max length for string or binary members
        /// </summary>
        public int MaxLength { get; private set; }

        /// <summary>
        /// Precision - for decimal and floating point types
        /// </summary>
        public int Precision { get; private set; }

        /// <summary>
        /// Scale - for decimal types
        /// </summary>
        public int Scale { get; private set; }

        /// <summary>
        /// Indicates if the IsUnicode value should be compared or not
        /// </summary>
        public bool CompareIsUnicode { get; private set; }
    }
}
