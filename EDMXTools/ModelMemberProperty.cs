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
    /// Represents a scalar member in the conceptual model. Can be a member of a ModelEntityType or a ModelComplexType
    /// </summary>
    public class ModelMemberProperty : EDMXMember, IEDMXNamedMember, IEDMXRemovableMember, IEDMXMemberDocumentation
    {
        private ModelEntityType _modelEntityType = null;
        private ModelComplexType _modelComplexType = null;
        private XmlElement _propertyElement = null;

        internal ModelMemberProperty(EDMXFile parentFile, ModelEntityType modelEntityType, XmlElement memberPropertyElement)
            : base(parentFile)
        {
            _modelEntityType = modelEntityType;
            _modelEntityType.Removed += new EventHandler(ModelEntityType_Removed);
            _propertyElement = memberPropertyElement;
        }

        void ModelEntityType_Removed(object sender, EventArgs e)
        {
            this.Remove();
        }

        internal ModelMemberProperty(EDMXFile parentFile, ModelEntityType modelEntityType, string name, int ordinal, XmlElement entityTypeElement)
            : base(parentFile)
        {
            _modelEntityType = modelEntityType;
            _modelEntityType.Removed += new EventHandler(ModelEntityType_Removed);

            _propertyElement = EDMXDocument.CreateElement("Property", NameSpaceURIcsdl);
            if (ordinal > 0)
            {
                XmlNodeList propertyNodes = entityTypeElement.SelectNodes("edm:Property", NSM);
                if (propertyNodes.Count >= ordinal)
                {
                    entityTypeElement.InsertAfter(_propertyElement, propertyNodes[ordinal - 1]);
                }
                else
                {
                    entityTypeElement.AppendChild(_propertyElement);
                }
            }
            else
            {
                entityTypeElement.AppendChild(_propertyElement);
            }

            Name = name;
        }

        internal ModelMemberProperty(EDMXFile parentFile, ModelComplexType modelComplexType, XmlElement memberPropertyElement)
            : base(parentFile)
        {
            _modelComplexType = modelComplexType;
            _modelComplexType.Removed += new EventHandler(ModelComplexType_Removed);
            _propertyElement = memberPropertyElement;
        }

        void ModelComplexType_Removed(object sender, EventArgs e)
        {
            this.Remove();
        }

        internal ModelMemberProperty(EDMXFile parentFile, ModelComplexType modelComplexType, string name, XmlElement entityTypeElement)
            : base(parentFile)
        {
            _modelComplexType = modelComplexType;
            _modelComplexType.Removed += new EventHandler(ModelComplexType_Removed);

            _propertyElement = EDMXDocument.CreateElement("Property", NameSpaceURIcsdl);
            entityTypeElement.AppendChild(_propertyElement);

            Name = name;
        }

        /// <summary>
        /// Event fired when the object has been removed from the model
        /// </summary>
        public event EventHandler Removed;

        /// <summary>
        /// Removes the object from the model.
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
        /// Event fired when the object changes name
        /// </summary>
        public event EventHandler<NameChangeArgs> NameChanged;

        /// <summary>
        /// Name of the model object
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
                        XmlElement propRef = (XmlElement)_propertyElement.ParentNode.SelectSingleNode("edm:Key/edm:PropertyRef[@Name=" + XmlHelpers.XPathLiteral(this.Name) + "]", NSM);
                        propRef.SetAttribute("Name", value);
                    }

                    if (_modelEntityType != null)
                    {
                        //update FK key references
                        foreach (ModelAssociationSet ma in _modelEntityType.AssociationsFrom)
                        {
                            if (ma.Keys.Where(k => k.Item1 == this).Any())
                            {
                                ma.UpdateKeyName(_modelEntityType, this, oldName, value);
                            }
                        }
                        foreach (ModelAssociationSet ma in _modelEntityType.AssociationsTo)
                        {
                            if (ma.Keys.Where(k => k.Item2 == this).Any())
                            {
                                ma.UpdateKeyName(_modelEntityType, this, oldName, value);
                            }
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
        /// Fully qualified name, including parent object names.
        /// </summary>
        public string FullName
        {
            get
            {
                if (_modelEntityType != null)
                {
                    return _modelEntityType.FullName + "." + this.Name;
                }
                else if (_modelComplexType != null)
                {
                    return _modelComplexType.FullName + "." + this.Name;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Fully qualified alias name, including parent object aliases
        /// </summary>
        public string AliasName
        {
            get
            {
                if (_modelEntityType != null)
                {
                    return _modelEntityType.AliasName + "." + Name;
                }
                else if (_modelComplexType != null)
                {
                    return _modelComplexType.AliasName + "." + this.Name;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Member CLR type
        /// </summary>
        public Type Type
        {
            get
            {
                return Type.GetType(TypeName, false, true);
            }
            set
            {
                TypeName = value.Name;
            }
        }

        /// <summary>
        /// Member type name
        /// </summary>
        public string TypeName
        {
            get
            {
                return _propertyElement.GetAttribute("Type");
            }
            set
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
                if (!PrecisionScaleApplies)
                {
                    if (_propertyElement.HasAttribute("Precision"))
                    {
                        _propertyElement.RemoveAttribute("Precision");
                    }
                    if (_propertyElement.HasAttribute("Scale"))
                    {
                        _propertyElement.RemoveAttribute("Scale");
                    }
                }
            }
        }

        /// <summary>
        /// Type description; type name, nullability, max/precision/scale, pk, store generated (identity/computed), and default value
        /// </summary>
        public string TypeDescription
        {
            get
            {
                try
                {
                    string typeDesc = null;
                    if (TypeName.Equals("string", StringComparison.InvariantCultureIgnoreCase))
                    {
                        typeDesc = TypeName + (!Nullable ? ", not nullable" : ", nullable");
                    }
                    else
                    {
                        if (Nullable)
                        {
                            typeDesc = "Nullable<" + TypeName + ">";
                        }
                        else
                        {
                            typeDesc = TypeName;
                        }
                    }
                    if (MaxLengthApplies && MaxLength > 0)
                    {
                        typeDesc = typeDesc + ", max: " + MaxLength;
                    }
                    if (IsKey)
                    {
                        typeDesc = typeDesc + ", PK";
                    }
                    if (StoreGeneratedPattern == StoreGeneratedPatternEnum.Identity)
                    {
                        typeDesc = typeDesc + ", identity";
                    }
                    if (StoreGeneratedPattern == StoreGeneratedPatternEnum.Computed)
                    {
                        typeDesc = typeDesc + ", computed";
                    }
                    if (!string.IsNullOrEmpty(DefaultValue))
                    {
                        typeDesc = typeDesc + ", default:" + DefaultValue;
                    }
                    return typeDesc;
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

        /// <summary>
        /// True if this member points to a complex type defined in the model
        /// </summary>
        public bool IsComplexType
        {
            get
            {
                try
                {
                    return (_modelEntityType != null && ParentFile.ConceptualModel.ComplexTypes.Any(ct => ct.FullName == this.TypeName || ct.AliasName == this.TypeName));
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

        /// <summary>
        /// True if nullable, false if not
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
        /// Store generated; none, identity (value created on insert) or computed (value computed and/or updated on every update).
        /// </summary>
        public StoreGeneratedPatternEnum StoreGeneratedPattern
        {
            get
            {
                switch (_propertyElement.GetAttribute("StoreGeneratedPattern", NameSpaceURIannotation))
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
                    if (_propertyElement.HasAttribute("StoreGeneratedPattern", NameSpaceURIannotation))
                    {
                        _propertyElement.RemoveAttribute("StoreGeneratedPattern", NameSpaceURIannotation);
                    }
                }
                else
                {
                    _propertyElement.SetAttribute("StoreGeneratedPattern", NameSpaceURIannotation, value.ToString());
                }
            }
        }

        /// <summary>
        /// Collation used in the database member(s) mapped to this member.
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
        /// Default value.
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
        /// True if the max length attribute applies, false if not.
        /// </summary>
        public bool MaxLengthApplies
        {
            get
            {
                switch (TypeName.ToLower())
                {
                    case "binary":
                    case "string":
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Max length, for string/binary types.
        /// </summary>
        public int MaxLength
        {
            get
            {
                int maxLength = 0;
                if (MaxLengthApplies)
                {
                    string value = _propertyElement.GetAttribute("MaxLength");
                    if (value.Equals("max", StringComparison.InvariantCultureIgnoreCase) || string.IsNullOrEmpty(value))
                    {
                        maxLength = -1;
                    }
                    else
                    {
                        int.TryParse(value, out maxLength);
                    }
                }
                return maxLength;
            }
            set
            {
                if (value > 0)
                {
                    string sValue = (value > 0 ? value.ToString() : string.Empty);
                    _propertyElement.SetAttribute("MaxLength", value.ToString());
                }
                else if (value == -1)
                {
                    _propertyElement.SetAttribute("MaxLength", "Max");
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
        /// Fixed length, for string/binary types.
        /// </summary>
        public bool FixedLength
        {
            get
            {
                return _propertyElement.GetAttribute("FixedLength").Equals("true", StringComparison.InvariantCultureIgnoreCase);
            }
            set
            {
                _propertyElement.SetAttribute("FixedLength", value.ToLString());
            }
        }

        /// <summary>
        /// Precision, for numeric/decimal types.
        /// </summary>
        public int Precision
        {
            get
            {
                int precision = 0;
                int.TryParse(_propertyElement.GetAttribute("Precision"), out precision);
                return precision;
            }
            set
            {
                if (value >= 0)
                {
                    string sValue = (value >= 0 ? value.ToString() : string.Empty);
                    _propertyElement.SetAttribute("Precision", sValue);
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
        /// Scale, for numeric/decimal types.
        /// </summary>
        public int Scale
        {
            get
            {
                int scale = 0;
                int.TryParse(_propertyElement.GetAttribute("Scale"), out scale);
                return scale;
            }
            set
            {
                if (value >= 0)
                {
                    string sValue = (value >= 0 ? value.ToString() : string.Empty);
                    _propertyElement.SetAttribute("Scale", sValue);
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
        /// True if the precision/scale attributes are valid/applies for the type used.
        /// </summary>
        public bool PrecisionScaleApplies
        {
            get
            {
                switch (TypeName.ToLower())
                {
                    case "decimal":
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// True if unicode member (string types only).
        /// </summary>
        public bool IsUnicode
        {
            get
            {
                if (TypeName == "String")
                {
                    string unicodeAttrib = _propertyElement.GetAttribute("Unicode");
                    return string.IsNullOrEmpty(unicodeAttrib) || unicodeAttrib.Equals("true", StringComparison.InvariantCultureIgnoreCase);
                }
                else
                {
                    return false;
                }
            }
            set
            {
                if (TypeName == "String")
                {
                    _propertyElement.SetAttribute("Unicode", value.ToLString());
                }
                else
                {
                    if (_propertyElement.HasAttribute("Unicode"))
                    {
                        _propertyElement.RemoveAttribute("Unicode");
                    }
                }
            }
        }

        /// <summary>
        /// True if this member is part of the entity key for the entity it is a member of.
        /// </summary>
        public bool IsKey
        {
            get
            {
                try
                {
                    return _propertyElement.ParentNode.SelectSingleNode("edm:Key/edm:PropertyRef[@Name=" + XmlHelpers.XPathLiteral(this.Name) + "]", NSM) != null;
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
            set
            {
                if (_modelEntityType != null)
                {
                    if (value == false && IsKey)
                    {
                        XmlElement propRef = (XmlElement)_propertyElement.ParentNode.SelectSingleNode("edm:Key/edm:PropertyRef[@Name=" + XmlHelpers.XPathLiteral(this.Name) + "]", NSM);
                        if (propRef != null)
                        {
                            propRef.ParentNode.RemoveChild(propRef);
                        }
                    }
                    else if (value == true && !IsKey)
                    {
                        XmlElement keyElement = ((XmlElement)_propertyElement.ParentNode).GetOrCreateElement("edm", "Key", NSM, false, _modelEntityType.DocumentationElement);
                        XmlElement propRef = _propertyElement.OwnerDocument.CreateElement("PropertyRef", NameSpaceURIcsdl);
                        propRef.SetAttribute("Name", this.Name);
                        keyElement.AppendChild(propRef);
                    }
                }
                else
                {
                    throw new InvalidOperationException("The Key attribute does not apply to complex type members.");
                }
            }
        }

        /// <summary>
        /// CSDL type, used for comparing type with other members in the conceptual or storage model.
        /// </summary>
        public CSDLType CSDLType
        {
            get
            {
                try
                {
                    return new CSDLType(this);
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

        /// <summary>
        /// Entity type this member belongs to. Null if it is a complex type member.
        /// </summary>
        public ModelEntityType EntityType
        {
            get
            {
                return _modelEntityType;
            }
        }

        /// <summary>
        /// Complex type this member belongs to. Null if it is a entity type member.
        /// </summary>
        public ModelComplexType ModelComplexType
        {
            get
            {
                return _modelComplexType;
            }
        }

        private List<StoreMemberProperty> _storeMembers = null;

        /// <summary>
        /// Storage model members mapped to this conceptual model member.
        /// </summary>
        public IEnumerable<StoreMemberProperty> StoreMembers
        {
            get
            {
                try
                {
                    if (_storeMembers == null)
                    {
                        ModelEntitySet entitySet = this.EntityType.EntitySet;
                        if (entitySet == null && this.EntityType.HasBaseType)
                        {
                            entitySet = this.EntityType.TopLevelBaseType.EntitySet;
                        }

                        if (entitySet.EntitySetMapping != null)
                        {
                            _storeMembers = entitySet.EntitySetMapping.MemberMappings.Where(tup => tup.Item2 == this).Select(sm => sm.Item1).ToList();
                            foreach (StoreMemberProperty smp in _storeMembers)
                            {
                                smp.Removed += new EventHandler(smp_Removed);
                            }
                        }
                    }
                    if (_storeMembers != null)
                    {
                        return _storeMembers.AsEnumerable();
                    }
                    else
                    {
                        return Enumerable.Empty<StoreMemberProperty>();
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
        }

        void smp_Removed(object sender, EventArgs e)
        {
            _storeMembers.Remove((StoreMemberProperty)sender);
        }

        #region "Documentation"
        private XmlElement DocumentationElement
        {
            get
            {
                return _propertyElement.GetOrCreateElement("edm", "Documentation", NSM, true);
            }
        }

        /// <summary>
        /// Short description, part of the documentation attributes for model members
        /// </summary>
        public string ShortDescription
        {
            get
            {
                XmlElement summaryElement = (XmlElement)_propertyElement.SelectSingleNode("edm:Documentation/edm:Summary", NSM);
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
                XmlElement summaryElement = DocumentationElement.GetOrCreateElement("edm", "Summary", NSM);
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
                XmlElement descriptionElement = (XmlElement)_propertyElement.SelectSingleNode("edm:Documentation/edm:LongDescription", NSM);
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
                XmlElement descriptionElement = DocumentationElement.GetOrCreateElement("edm", "LongDescription", NSM);
                descriptionElement.InnerText = value;
            }
        }
        #endregion

        /// <summary>
        /// Corresponding SSDL type name
        /// </summary>
        public string SSDLType
        {
            get
            {
                string ssdlType = null;
                switch (TypeName.ToLower())
                {
                    case "string":
                        //if (MaxLength == null || MaxLength == 0)
                        //{
                        //    ssdlType = "text";
                        //}
                        //else
                        if (MaxLength > 0 && FixedLength == true)
                        {
                            ssdlType = "char";
                        }
                        else
                        {
                            ssdlType = "varchar";
                        }
                        if (IsUnicode == true)
                        {
                            ssdlType = "n" + ssdlType;
                        }
                        break;
                    case "int64":
                        ssdlType = "bigint";
                        break;
                    case "int32":
                        ssdlType = "int";
                        break;
                    case "int16":
                        ssdlType = "smallint";
                        break;
                    case "binary":
                        if (FixedLength == true)
                        {
                            ssdlType = "binary";
                            if (MaxLength == 8 && StoreGeneratedPattern == StoreGeneratedPatternEnum.Computed)
                            {
                                ssdlType = "timestamp";
                            }
                        }
                        else
                        {
                            ssdlType = "varbinary";
                        }
                        break;
                    case "boolean":
                        ssdlType = "bit";
                        break;
                    case "datetime":
                    case "datetime2":
                        ssdlType = "datetime";
                        break;
                    case "decimal":
                        ssdlType = "decimal";
                        break;
                    case "double":
                        ssdlType = "float";
                        break;
                    case "guid":
                        ssdlType = "uniqueidentifier";
                        break;
                    case "single":
                        ssdlType = "real";
                        break;
                    case "byte":
                        ssdlType = "tinyint";
                        break;
                    case "datetimeoffset":
                        ssdlType = "datetimeoffset";
                        break;
                    case "sbyte":
                        ssdlType = "smallint";
                        break;
                    case "time":
                        ssdlType = "time";
                        break;
                    default:
                        ssdlType = "binary";
                        break;
                }
                return ssdlType;
            }
            private set { }
        }

        internal void CSMappingsUpdated()
        {
            _storeMembers = null;
        }
    }
}
