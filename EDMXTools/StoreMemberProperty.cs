using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

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
    /// Represents a scalar member in a storage model entity type.
    /// </summary>
    public class StoreMemberProperty : EDMXMember, IEDMXNamedMember, IEDMXRemovableMember, IEDMXMemberDocumentation
    {
        private StoreEntityType _parentEntityType = null;
        private XmlElement _propertyElement = null;

        internal StoreMemberProperty(EDMXFile parentFile, StoreEntityType parentEntityType, XmlElement propertyElement)
            : base(parentFile)
        {
            _parentEntityType = parentEntityType;
            _parentEntityType.Removed += new EventHandler(ParentEntityType_Removed);
            _propertyElement = propertyElement;
        }

        void ParentEntityType_Removed(object sender, EventArgs e)
        {
            this.Remove();
        }

        internal StoreMemberProperty(EDMXFile parentFile, StoreEntityType storeEntityType, string name, int ordinal, XmlElement parentTypeElement)
            : base(parentFile)
        {
            _parentEntityType = storeEntityType;
            _parentEntityType.Removed += new EventHandler(ParentEntityType_Removed);

            _propertyElement = EDMXDocument.CreateElement("Property", NameSpaceURIssdl);
            if (ordinal > 0)
            {
                XmlNodeList propertyNodes = parentTypeElement.SelectNodes("ssdl:Property", NSM);
                if (propertyNodes.Count >= ordinal)
                {
                    parentTypeElement.InsertAfter(_propertyElement, propertyNodes[ordinal - 1]);
                }
                else
                {
                    parentTypeElement.AppendChild(_propertyElement);
                }
            }
            else
            {
                parentTypeElement.AppendChild(_propertyElement);
            }

            this.Name = name;
        }

        /// <summary>
        /// Event raised when the property is removed from the model.
        /// </summary>
        public event EventHandler Removed;

        /// <summary>
        /// Removes the property from the model.
        /// </summary>
        public void Remove()
        {
            try
            {
                if (_propertyElement.ParentNode != null)
                {
                    _propertyElement.ParentNode.RemoveChild(_propertyElement);

                    if (Removed != null)
                    {
                        Removed(this, new EventArgs());
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    ExceptionTools.AddExceptionData(ex, this);
                }
                catch { }
                throw;
            }
        }

        /// <summary>
        /// Event raised when the member changes name.
        /// </summary>
        public event EventHandler<NameChangeArgs> NameChanged;

        /// <summary>
        /// Get/set the name of the member.
        /// </summary>
        public string Name
        {
            get
            {
                return _propertyElement.GetAttribute("Name");
            }
            set
            {
                string oldName = _propertyElement.GetAttribute("Name");

                if (!string.IsNullOrEmpty(oldName))
                {
                    if (IsKey)
                    {
                        //change the pk key reference name
                        XmlElement propRef = (XmlElement)_propertyElement.ParentNode.SelectSingleNode("ssdl:Key/ssdl:PropertyRef[@Name=" + XmlHelpers.XPathLiteral(this.Name) + "]", NSM);
                        propRef.SetAttribute("Name", value);
                    }

                    //update FK key references
                    foreach (StoreAssociationSet sa in _parentEntityType.AssociationsFrom)
                    {
                        if (sa.Keys.Where(k => k.Item1 == this).Any())
                        {
                            sa.UpdateKeyName(_parentEntityType, this, oldName, value);
                        }
                    }
                    foreach (StoreAssociationSet sa in _parentEntityType.AssociationsTo)
                    {
                        if (sa.Keys.Where(k => k.Item2 == this).Any())
                        {
                            sa.UpdateKeyName(_parentEntityType, this, oldName, value);
                        }
                    }
                }

                //set the property name
                _propertyElement.SetAttribute("Name", value);

                //raise the name change event
                if (NameChanged != null)
                {
                    NameChanged(this, new NameChangeArgs { OldName = oldName, NewName = value });
                }
            }
        }

        /// <summary>
        /// Fully qualified name (model, type, and member name)
        /// </summary>
        public string FullName
        {
            get
            {
                return _parentEntityType.FullName + "." + Name;
            }
        }

        /// <summary>
        /// Fully qualified alias name (model alias, type name, and member name)
        /// </summary>
        public string AliasName
        {
            get
            {
                return _parentEntityType.AliasName + "." + Name;
            }
        }

        /// <summary>
        /// Store data type name. Valid values depend on the underlying database.
        /// </summary>
        public string DataType
        {
            get
            {
                string typeName = _propertyElement.GetAttribute("Type");
                if (typeName.EndsWith("(max)", StringComparison.InvariantCultureIgnoreCase))
                {
                    typeName = typeName.Substring(0, typeName.Length - 5);
                }
                return typeName;
            }
            set
            {
                if (value != _propertyElement.GetAttribute("Type"))
                {
                    _propertyElement.SetAttribute("Type", value);
                    if (!MaxLengthApplies)
                    {
                        if (_propertyElement.HasAttribute("MaxLength"))
                        {
                            _propertyElement.RemoveAttribute("MaxLength");
                        }
                        if (_propertyElement.HasAttribute("FixedLength"))
                        {
                            _propertyElement.RemoveAttribute("FixedLength");
                        }
                    }
                    switch (DataType.ToLower())
                    {
                        case "decimal":
                        case "numeric":
                            break;
                        default:
                            if (_propertyElement.HasAttribute("Precision"))
                            {
                                _propertyElement.RemoveAttribute("Precision");
                            }
                            if (_propertyElement.HasAttribute("Scale"))
                            {
                                _propertyElement.RemoveAttribute("Scale");
                            }
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Description of the data type. (Type name, nullability, length/precision/scale, store-generated etc)
        /// </summary>
        public string DataTypeDescription
        {
            get
            {
                switch (DataType.ToLower())
                {
                    case "int":
                    case "bigint":
                    case "tinyint":
                    case "smallint":
                        return this.DataType
                            + ((!(bool)this.Nullable) ? " NOT NULL" : " NULL")
                            + ((this.StoreGeneratedPattern == StoreGeneratedPatternEnum.Identity && this.Nullable == false) ? " IDENTITY" : "");
                    case "uniqueidentifier":
                    case "smalldatetime":
                    case "image":
                    case "datetime":
                    case "datetime2":
                    case "date":
                    case "time":
                    case "bit":
                    case "binary":
                    case "text":
                    case "ntext":
                    case "xml":
                    case "real":
                    case "money":
                    case "float":
                    case "smallmoney":
                        return this.DataType
                            + ((!(bool)this.Nullable) ? " NOT NULL" : " NULL");
                    case "sql_variant":
                        return "Variant"
                            + ((!(bool)this.Nullable) ? " NOT NULL" : " NULL");
                    case "timestamp":
                        return "rowversion"
                            + ((!(bool)this.Nullable) ? " NOT NULL" : " NULL");
                    case "decimal":
                    case "numeric":
                        return "decimal" //this.DataType
                            + ((this.Precision > 0 && this.Scale > 0) ? "(" + this.Precision + "," + this.Scale + ")" : "")
                            + ((!(bool)this.Nullable) ? " NOT NULL" : " NULL")
                            + ((this.StoreGeneratedPattern == StoreGeneratedPatternEnum.Identity && this.Scale == 0 && this.Nullable == false) ? " IDENTITY" : "");
                    case "varbinary":
                    case "varchar":
                    case "char":
                        return this.DataType
                            + ((this.MaxLength > 0) ? "(" + this.MaxLength.ToString() + ")" : ((this.MaxLength == -1) ? "(max)" : ""))
                            + ((!(bool)this.Nullable) ? " NOT NULL" : " NULL");
                    case "nvarchar":
                    case "nchar":
                    case "sysname":
                        return this.DataType
                            + ((this.MaxLength > 0) ? "(" + (this.MaxLength).ToString() + ")" : ((this.MaxLength == -1) ? "(max)" : ""))
                            + ((!(bool)this.Nullable) ? " NOT NULL" : " NULL");
                    case "hierarchyid":
                        return "nvarchar" + ((!(bool)this.Nullable) ? " NOT NULL" : " NULL");
                    case "geometry":
                        return "geometry" + ((!(bool)this.Nullable) ? " NOT NULL" : " NULL");
                    case "geography":
                        return "geography" + ((!(bool)this.Nullable) ? " NOT NULL" : " NULL");
                    default:
                        return this.DataType
                            + ((!(bool)this.Nullable) ? " NOT NULL" : " NULL");
                }
            }
        }

        /// <summary>
        /// Indicates if the member is nullable or not
        /// </summary>
        public bool Nullable
        {
            get
            {
                return !_propertyElement.GetAttribute("Nullable").Equals("false", StringComparison.InvariantCultureIgnoreCase);
            }
            set
            {
                _propertyElement.SetAttribute("Nullable", value.ToLString().ToLower());
            }
        }

        /// <summary>
        /// Store generated pattern; None, Identity, or Computed
        /// </summary>
        public StoreGeneratedPatternEnum StoreGeneratedPattern
        {
            get
            {
                switch (_propertyElement.GetAttribute("StoreGeneratedPattern"))
                {
                    case "Identity":
                        return StoreGeneratedPatternEnum.Identity;
                    case "Computed":
                        return StoreGeneratedPatternEnum.Computed;
                    default:
                        return StoreGeneratedPatternEnum.None;
                }
            }
            set
            {
                if (value == StoreGeneratedPatternEnum.None)
                {
                    if (_propertyElement.HasAttribute("StoreGeneratedPattern"))
                    {
                        _propertyElement.RemoveAttribute("StoreGeneratedPattern");
                    }
                }
                else
                {
                    _propertyElement.SetAttribute("StoreGeneratedPattern", value.ToString());
                }
            }
        }

        /// <summary>
        /// Collation used in the database. Valid only for member types affected by collation.
        /// </summary>
        public string Collation
        {
            get
            {
                if (_propertyElement.HasAttribute("Collation"))
                {
                    return _propertyElement.GetAttribute("Collation");
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (value != null)
                {
                    _propertyElement.SetAttribute("Collation", value);
                }
                else
                {
                    if (_propertyElement.HasAttribute("Collation"))
                    {
                        _propertyElement.RemoveAttribute("Collation");
                    }
                }
            }
        }

        /// <summary>
        /// Default value - the member's fixed-value default. Computed default constraints can not be represented in this property.
        /// </summary>
        public string DefaultValue
        {
            get
            {
                if (_propertyElement.HasAttribute("DefaultValue"))
                {
                    return _propertyElement.GetAttribute("DefaultValue");
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (value != null)
                {
                    _propertyElement.SetAttribute("DefaultValue", value);
                }
                else
                {
                    if (_propertyElement.HasAttribute("DefaultValue"))
                    {
                        _propertyElement.RemoveAttribute("DefaultValue");
                    }
                }
            }
        }

        /// <summary>
        /// Fixed length string or binary member
        /// </summary>
        public bool FixedLength
        {
            get
            {
                bool fixedLength = _propertyElement.GetAttribute("FixedLength").Equals("true", StringComparison.InvariantCultureIgnoreCase);
                switch (DataType.ToLower())
                {
                    case "binary":
                    case "binary(max)":
                    case "char":
                    case "char(max)":
                    case "nchar":
                    case "nchar(max)":
                    case "timestamp":
                        fixedLength = true;
                        break;
                    default:
                        //nothing
                        break;
                }
                return fixedLength;
            }
            set
            {
                if (_propertyElement.HasAttribute("FixedLength"))
                {
                    _propertyElement.RemoveAttribute("FixedLength");
                }
            }
        }

        /// <summary>
        /// Returns a description of the corresponding conceptual layer type attributes.
        /// </summary>
        public CSDLType CSDLType
        {
            get
            {
                return new CSDLType(this);
            }
        }

        /// <summary>
        /// True if the MaxLength attribute is valid for this member, false if not.
        /// </summary>
        public bool MaxLengthApplies
        {
            get
            {
                switch (DataType.ToLower())
                {
                    case "varbinary":
                    case "varchar":
                    case "char":
                    case "nvarchar":
                    case "nchar":
                    case "sysname":
                    case "binary":
                    case "xml":
                    case "timestamp":
                    case "rowversion":
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Max length - valid for string or binary members only
        /// </summary>
        public int MaxLength
        {
            get
            {
                int maxLength = 0;
                if (MaxLengthApplies)
                {
                    string value = _propertyElement.GetAttribute("MaxLength");
                    if (DataType.EndsWith("(max)", StringComparison.InvariantCultureIgnoreCase) || string.IsNullOrEmpty(value))
                    {
                        string type = DataType.ToLower().Replace("(max)", "");
                        maxLength = -1;
                    }
                    else
                    {
                        int.TryParse(value, out maxLength);
                    }
                }
                if (DataType.Equals("timestamp", StringComparison.InvariantCultureIgnoreCase) || DataType.Equals("rowversion", StringComparison.InvariantCultureIgnoreCase))
                {
                    maxLength = 8;
                }
                return maxLength;
            }
            set
            {
                if (value > 0 && !(DataType.Equals("timestamp", StringComparison.InvariantCultureIgnoreCase) || DataType.Equals("rowversion", StringComparison.InvariantCultureIgnoreCase)))
                {
                    string sValue = (value > 0 ? value.ToString() : string.Empty);
                    _propertyElement.SetAttribute("MaxLength", value.ToString());
                }
                else
                {
                    if (_propertyElement.HasAttribute("MaxLength"))
                    {
                        _propertyElement.RemoveAttribute("MaxLength");
                    }
                }
            }
        }

        /// <summary>
        /// True if precision/scale is valid for this member, false if not.
        /// </summary>
        public bool PrecisionScaleApplies
        {
            get
            {
                switch (DataType.ToLower())
                {
                    case "money":
                    case "smallmoney":
                    case "decimal":
                    case "numeric":
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Precision - valid for decimal/numeric types only.
        /// </summary>
        public int Precision
        {
            get
            {
                int precision = 0;
                int.TryParse(_propertyElement.GetAttribute("Precision"), out precision);
                if (precision == 0)
                {
                    switch (DataType.ToLower())
                    {
                        case "decimal":
                        case "numeric":
                            precision = 18;
                            break;
                        case "money":
                            precision = 19;
                            break;
                        case "smallmoney":
                            precision = 10;
                            break;
                        default:
                            //nothing
                            break;
                    }
                }
                return precision;
            }
            set
            {
                if (value > 0)
                {
                    switch (DataType.ToLower())
                    {
                        case "decimal":
                        case "numeric":
                            _propertyElement.SetAttribute("Precision", value.ToString());
                            break;
                        default:
                            if (_propertyElement.HasAttribute("Precision"))
                            {
                                _propertyElement.RemoveAttribute("Precision");
                            }
                            break;
                    }
                }
                else
                {
                    if (_propertyElement.HasAttribute("Precision"))
                    {
                        _propertyElement.RemoveAttribute("Precision");
                    }
                }
            }
        }

        /// <summary>
        /// Scale - valid for numeric/decimal types only.
        /// </summary>
        public int Scale
        {
            get
            {
                int scale = 0;
                int.TryParse(_propertyElement.GetAttribute("Scale"), out scale);
                if (scale == 0)
                {
                    switch (DataType.ToLower())
                    {
                        case "decimal":
                        case "numeric":
                            scale = 0;
                            break;
                        case "money":
                            scale = 4;
                            break;
                        case "smallmoney":
                            scale = 4;
                            break;
                        default:
                            //nothing
                            break;
                    }
                }
                return scale;
            }
            set
            {
                if (value > 0)
                {
                    switch (DataType.ToLower())
                    {
                        case "decimal":
                        case "numeric":
                            _propertyElement.SetAttribute("Scale", value.ToString());
                            break;
                        default:
                            if (_propertyElement.HasAttribute("Scale"))
                            {
                                _propertyElement.RemoveAttribute("Scale");
                            }
                            break;
                    }
                }
                else
                {
                    if (_propertyElement.HasAttribute("Scale"))
                    {
                        _propertyElement.RemoveAttribute("Scale");
                    }
                }
            }
        }

        /// <summary>
        /// True if the member is part of the entity key / primary key for the underlying table.
        /// </summary>
        public bool IsKey
        {
            get
            {
                return _propertyElement.ParentNode.SelectSingleNode("ssdl:Key/ssdl:PropertyRef[@Name=" + XmlHelpers.XPathLiteral(this.Name) + "]", NSM) != null;
            }
            set
            {
                if (value == false && IsKey)
                {
                    XmlElement propRef = (XmlElement)_propertyElement.ParentNode.SelectSingleNode("ssdl:Key/ssdl:PropertyRef[@Name=" + XmlHelpers.XPathLiteral(this.Name) + "]", NSM);
                    if (propRef != null)
                    {
                        propRef.ParentNode.RemoveChild(propRef);
                    }
                }
                else if (value == true && !IsKey)
                {
                    XmlElement keyElement = ((XmlElement)_propertyElement.ParentNode).GetOrCreateElement("ssdl", "Key", NSM, false, _parentEntityType.DocumentationElement);
                    XmlElement propRef = _propertyElement.OwnerDocument.CreateElement("PropertyRef", NameSpaceURIssdl);
                    propRef.SetAttribute("Name", this.Name);
                    keyElement.AppendChild(propRef);
                }
            }
        }

        /// <summary>
        /// True if the member is part of an alternate key (unique index) in the underlying table.
        /// </summary>
        public bool IsAlternateKey
        {
            get
            {
                return _propertyElement.GetAttribute("IsAlternateKeyMember", NameSpaceURIHuagati).Equals("true", StringComparison.InvariantCultureIgnoreCase);
            }
            set
            {
                if (value == true)
                {
                    _propertyElement.SetAttribute("IsAlternateKeyMember", NameSpaceURIHuagati, value.ToLString());
                }
                else
                {
                    if (_propertyElement.HasAttribute("IsAlternateKeyMember", NameSpaceURIHuagati))
                    {
                        _propertyElement.RemoveAttribute("IsAlternateKeyMember", NameSpaceURIHuagati);
                    }
                }
            }
        }

        /// <summary>
        /// Store entity type that this member belongs to.
        /// </summary>
        public StoreEntityType EntityType
        {
            get
            {
                return _parentEntityType;
            }
        }

        private List<ModelMemberProperty> _modelMembers = null;
        /// <summary>
        /// Enumeration of all conceptual model members mapped to this storage member.
        /// </summary>
        public IEnumerable<ModelMemberProperty> ModelMembers
        {
            get
            {
                try
                {
                    if (_modelMembers == null)
                    {
                        if (this.EntityType.EntitySet != null)
                        {
                            _modelMembers = this.EntityType.EntitySet.EntitySetMappings.SelectMany(esm => esm.MemberMappings).Where(tup => tup.Item1 == this).Select(mm => mm.Item2).ToList();
                            foreach (ModelMemberProperty mmp in _modelMembers)
                            {
                                mmp.Removed += new EventHandler(mmp_Removed);
                            }
                        }
                        else
                        {
                            _modelMembers = new List<ModelMemberProperty>();
                        }
                    }
                    return _modelMembers.AsEnumerable();
                }
                catch (Exception ex)
                {
                    try
                    {
                        ExceptionTools.AddExceptionData(ex, this);
                    }
                    catch { }
                    throw;
                }
            }
        }

        void mmp_Removed(object sender, EventArgs e)
        {
            _modelMembers.Remove((ModelMemberProperty)sender);
        }

        #region "Documentation"
        private XmlElement DocumentationElement
        {
            get
            {
                return _propertyElement.GetOrCreateElement("ssdl", "Documentation", NSM, true);
            }
        }

        /// <summary>
        /// Short description, part of the documentation attributes for model members
        /// </summary>
        public string ShortDescription
        {
            get
            {
                XmlElement summaryElement = (XmlElement)_propertyElement.SelectSingleNode("ssdl:Documentation/ssdl:Summary", NSM);
                if (summaryElement != null)
                {
                    return summaryElement.InnerText;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                XmlElement summaryElement = DocumentationElement.GetOrCreateElement("ssdl", "Summary", NSM, true);
                summaryElement.InnerText = value;
            }
        }

        /// <summary>
        /// Long description, part of the documentation attributes for model members
        /// </summary>
        public string LongDescription
        {
            get
            {
                XmlElement descriptionElement = (XmlElement)_propertyElement.SelectSingleNode("ssdl:Documentation/ssdl:LongDescription", NSM);
                if (descriptionElement != null)
                {
                    return descriptionElement.InnerText;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                XmlElement descriptionElement = DocumentationElement.GetOrCreateElement("ssdl", "LongDescription", NSM);
                descriptionElement.InnerText = value;
            }
        }
        #endregion

        /// <summary>
        /// Long human readable description of the type attributes, including identity/computed, key membership, default etc.
        /// </summary>
        public string DataTypeLongDescription
        {
            get
            {
                return DataTypeDescription
                    + (this.IsKey ? ", PK" : string.Empty)
                    + (this.StoreGeneratedPattern == StoreGeneratedPatternEnum.Identity ? ", identity" : string.Empty)
                    + (this.StoreGeneratedPattern == StoreGeneratedPatternEnum.Computed ? ", computed" : string.Empty)
                    + (!string.IsNullOrEmpty(this.DefaultValue) ? ", default:" + this.DefaultValue : string.Empty);
            }
            private set { }
        }

        internal void CSMappingsUpdated()
        {
            _modelMembers = null;
        }
        
        /// <summary>
        /// Copies attributes from another store member property.
        /// </summary>
        /// <param name="fromMember">Member to copy the attributes from.</param>
        public void CopyAttributes(StoreMemberProperty fromMember)
        {
            this.DataType = fromMember.DataType;
            this.Nullable = fromMember.Nullable;
            if (!string.IsNullOrEmpty(fromMember.Collation))
            {
                this.Collation = fromMember.Collation;
            }
            if (!string.IsNullOrEmpty(fromMember.DefaultValue))
            {
                this.DefaultValue = fromMember.DefaultValue;
            }
            if (this.MaxLengthApplies)
            {
                this.MaxLength = fromMember.MaxLength;
                this.FixedLength = fromMember.FixedLength;
            }
            if (this.PrecisionScaleApplies)
            {
                this.Precision = fromMember.Precision;
                this.Scale = fromMember.Scale;
            }
            if (!string.IsNullOrEmpty(fromMember.ShortDescription))
            {
                this.ShortDescription = fromMember.ShortDescription;
            }
            if (!string.IsNullOrEmpty(fromMember.LongDescription))
            {
                this.LongDescription = fromMember.LongDescription;
            }
        }

        /// <summary>
        /// True if the underlying data type is a blob type.
        /// </summary>
        public bool IsBlob
        {
            get
            {
                switch (DataType.ToLower())
                {
                    case "binary":
                    case "text":
                    case "ntext":
                    case "sql_variant":
                    case "image":
                    case "xml":
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Returns mapping conditions that rely on this store member. Used for entity inheritance or entity splitting.
        /// </summary>
        public IEnumerable<MappingCondition> MappingConditions
        {
            get
            {
                return this.EntityType.EntitySet.EntitySetMappings.SelectMany(esm => esm.MappingConditions).Where(mc => mc.DiscriminatorColumn == this);
            }
        }
    }
}
