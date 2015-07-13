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
    /// Represents a conceptual model navigation property from one entity to another entity(set)
    /// </summary>
    public class NavigationProperty : EDMXMember, IEDMXNamedMember, IEDMXRemovableMember, IEDMXMemberDocumentation 
    {
        private ModelEntityType _modelEntityType = null;
        private XmlElement _propertyElement = null;

        internal NavigationProperty(EDMXFile parentFile, ModelEntityType modelEntityType, XmlElement navPropertyElement)
            : base(parentFile)
        {
            _modelEntityType = modelEntityType;
            _propertyElement = navPropertyElement;
        }

        internal NavigationProperty(EDMXFile parentFile, ModelEntityType modelEntityType, string name, ModelAssociationSet modelAssociationSet, XmlElement entityTypeElement, string fromRoleName, string toRoleName)
            : base(parentFile)
        {
            _modelEntityType = modelEntityType;

            _propertyElement = EDMXDocument.CreateElement("NavigationProperty", NameSpaceURIcsdl);
            _propertyElement.SetAttribute("Relationship", modelAssociationSet.FullName);

            if (string.IsNullOrEmpty(fromRoleName) || string.IsNullOrEmpty(toRoleName))
            {
                if (modelAssociationSet.FromEntityType == _modelEntityType)
                {
                    fromRoleName = modelAssociationSet.FromRoleName;
                    toRoleName = modelAssociationSet.ToRoleName;
                }
                else
                {
                    fromRoleName = modelAssociationSet.ToRoleName;
                    toRoleName = modelAssociationSet.FromRoleName;
                }
            }

            _propertyElement.SetAttribute("FromRole", fromRoleName);
            _propertyElement.SetAttribute("ToRole", toRoleName);

            entityTypeElement.AppendChild(_propertyElement);

            Name = name;
        }

        /// <summary>
        /// Entity type that this navigation property is a member of.
        /// </summary>
        public ModelEntityType EntityType
        {
            get
            {
                return _modelEntityType;
            }
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
                return _modelEntityType.FullName + "." + this.Name;
            }
        }

        /// <summary>
        /// Fully qualified alias name, including parent object aliases.
        /// </summary>
        public string AliasName
        {
            get
            {
                return _modelEntityType.AliasName + "." + Name;
            }
        }

        private ModelAssociationSet _association = null;

        /// <summary>
        /// Association set that this navigation property is based on.
        /// </summary>
        public ModelAssociationSet Association
        {
            get
            {
                try
                {
                    if (_association == null)
                    {
                        string assocName = AssociationName;
                        _association = _modelEntityType.AssociationsFrom.FirstOrDefault(asf => asf.FullName == assocName || asf.AliasName == assocName);
                        if (_association == null)
                        {
                            _association = _modelEntityType.AssociationsTo.FirstOrDefault(asf => asf.FullName == assocName || asf.AliasName == assocName);
                        }
                    }
                    return _association;
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
        /// Name of the association that this navigation property is based on.
        /// </summary>
        public string AssociationName
        {
            get
            {
                return _propertyElement.GetAttribute("Relationship");
            }
        }

        /// <summary>
        /// From-role
        /// </summary>
        public string FromRoleName
        {
            get
            {
                return _propertyElement.GetAttribute("FromRole");
            }
        }

        /// <summary>
        /// To-role
        /// </summary>
        public string ToRoleName
        {
            get
            {
                return _propertyElement.GetAttribute("ToRole");
            }
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

    }
}
