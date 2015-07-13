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
    /// Represents a conceptual model entityset
    /// </summary>
    public class ModelEntitySet : EDMXMember, IEDMXNamedMember, IEDMXRemovableMember, IEDMXMemberDocumentation 
    {
        private ConceptualModel _conceptualModel = null;
        private System.Xml.XmlElement _entitySetElement = null;
        private ModelEntityType _entityType = null;

        internal ModelEntitySet(EDMXFile parentFile, ConceptualModel conceptualModel, System.Xml.XmlElement entitySetElement) : base(parentFile)
        {
            _conceptualModel = conceptualModel;
            _entitySetElement = entitySetElement;
        }

        internal ModelEntitySet(EDMXFile parentFile, ConceptualModel conceptualModel, string name) : base(parentFile)
        {
            _conceptualModel = conceptualModel;

            //create and add the entity set element
            XmlElement setContainer = (XmlElement)EDMXDocument.DocumentElement.SelectSingleNode("edmx:Runtime/edmx:ConceptualModels/edm:Schema/edm:EntityContainer", NSM);
            _entitySetElement = EDMXDocument.CreateElement("EntitySet", NameSpaceURIcsdl);
            setContainer.AppendChild(_entitySetElement);

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
                if (_entitySetElement.ParentNode != null)
                {
                    _entitySetElement.ParentNode.RemoveChild(_entitySetElement);

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
                return _entitySetElement.GetAttribute("Name");
            }
            set
            {
                string oldName = _entitySetElement.GetAttribute("Name");
                _entitySetElement.SetAttribute("Name", value);

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
                return ((XmlElement)_entitySetElement.ParentNode.ParentNode).GetAttribute("Namespace") + "." + Name;
            }
        }

        /// <summary>
        /// Fully qualified alias name, including parent object aliases.
        /// </summary>
        public string AliasName
        {
            get
            {
                return ((XmlElement)_entitySetElement.ParentNode.ParentNode).GetAttribute("Alias") + "." + Name;
            }
        }

        /// <summary>
        /// Entity type name for the primary entity type behind this entity set.
        /// </summary>
        public string EntityTypeName
        {
            get
            {
                return _entitySetElement.GetAttribute("EntityType");
            }
        }

        /// <summary>
        /// Protection level for the generated property getter for this entityset.
        /// </summary>
        public AccessModifierEnum GetterAccess
        {
            get
            {
                switch (_entitySetElement.GetAttribute("GetterAccess", NameSpaceURIcodegen).ToLower())
                {
                    case "internal":
                        return AccessModifierEnum.Internal;
                    case "private":
                        return AccessModifierEnum.Private;
                    case "protected":
                        return AccessModifierEnum.Protected;
                    default:
                        return AccessModifierEnum.Public;
                }
            }
            set
            {
                _entitySetElement.SetAttribute("GetterAccess", NameSpaceURIcodegen, value.ToString());
            }
        }

        /// <summary>
        /// Primary entity type for the entity set
        /// </summary>
        public ModelEntityType EntityType
        {
            get
            {
                try
                {
                    if (_entityType == null)
                    {
                        string etTypeName = EntityTypeName;
                        _entityType = _conceptualModel.EntityTypes.FirstOrDefault(et => et.FullName == etTypeName || et.AliasName == etTypeName);
                        if (_entityType != null)
                        {
                            _entityType.Removed += new EventHandler(entityType_Removed);
                        }
                    }
                    return _entityType;
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
                _entityType = value;
                _entitySetElement.SetAttribute("EntityType", value.FullName);
            }
        }

        void entityType_Removed(object sender, EventArgs e)
        {
            _entityType = null;
        }

        /// <summary>
        /// Lazy loading enabled or disabled for this entity set?
        /// </summary>
        public bool LazyLoadingEnabled
        {
            get
            {
                return (_entitySetElement.GetAttribute("LazyLoadingEnabled", NameSpaceURIannotation).Equals("true", StringComparison.InvariantCultureIgnoreCase));
            }
            set
            {
                _entitySetElement.SetAttribute("LazyLoadingEnabled", NameSpaceURIannotation, value.ToLString());
            }
        }

        private EntitySetMapping _entitySetMapping = null;

        /// <summary>
        /// Entity set mapping that maps this entity set to the underlying storage model entityset(s)
        /// </summary>
        public EntitySetMapping EntitySetMapping
        {
            get
            {
                try
                {
                    if (_entitySetMapping == null)
                    {
                        _entitySetMapping = ParentFile.CSMapping.EntitySetMappings.FirstOrDefault(esm => esm.ModelEntitySet == this);
                        if (_entitySetMapping != null)
                        {
                            _entitySetMapping.Removed += new EventHandler(_entitySetMapping_Removed);
                        }
                    }
                    return _entitySetMapping;
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

        void _entitySetMapping_Removed(object sender, EventArgs e)
        {
            _entitySetMapping = null;
        }

        /// <summary>
        /// Enumeration of associations where this entity set is the dependent (from) entityset
        /// </summary>
        public IEnumerable<ModelAssociationSet> AssociationsFrom
        {
            get
            {
                foreach (ModelAssociationSet assoc in _conceptualModel.AssociationSets.Where(aset => aset.FromEntitySet == this))
                {
                    yield return assoc;
                }
            }
        }

        /// <summary>
        /// Enumeration of associations where this entity set is the principal (to) entityset
        /// </summary>
        public IEnumerable<ModelAssociationSet> AssociationsTo
        {
            get
            {
                foreach (ModelAssociationSet assoc in _conceptualModel.AssociationSets.Where(aset => aset.ToEntitySet == this))
                {
                    yield return assoc;
                }
            }
        }

        #region "Documentation"
        private XmlElement DocumentationElement
        {
            get
            {
                return _entitySetElement.GetOrCreateElement("edm", "Documentation", NSM, true);
            }
        }

        /// <summary>
        /// Short description, part of the documentation attributes for model members
        /// </summary>
        public string ShortDescription
        {
            get
            {
                XmlElement summaryElement = (XmlElement)_entitySetElement.SelectSingleNode("edm:Documentation/edm:Summary", NSM);
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
                XmlElement descriptionElement = (XmlElement)_entitySetElement.SelectSingleNode("edm:Documentation/edm:LongDescription", NSM);
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

        internal void CSMappingsUpdated()
        {
            _entitySetMapping = null;
        }

        /// <summary>
        /// Inheritance strategy setting for this entity set. This is not part of the Microsoft EDMX/CSDL specification but an extension added by this library.
        /// </summary>
        public EDMXInheritanceStrategyEnum InheritanceStrategy
        {
            get
            {
                string inheritanceStrategy = _entitySetElement.GetAttribute("InheritanceStrategy", NameSpaceURIHuagati);
                switch (inheritanceStrategy.ToUpperInvariant())
                {
                    case "TPT":
                        return EDMXInheritanceStrategyEnum.TPT;
                    case "TPH":
                        return EDMXInheritanceStrategyEnum.TPH;
                    case "TPC":
                        return EDMXInheritanceStrategyEnum.TPC;
                    case "MIXED":
                        return EDMXInheritanceStrategyEnum.Mixed;
                    default:
                        return EDMXInheritanceStrategyEnum.None;
                }
            }
            set
            {
                if (value != EDMXInheritanceStrategyEnum.None)
                {
                    _entitySetElement.SetAttribute("InheritanceStrategy", NameSpaceURIHuagati, value.ToString());
                }
                else
                {
                    if (_entitySetElement.HasAttribute("InheritanceStrategy", NameSpaceURIHuagati))
                    {
                        _entitySetElement.RemoveAttribute("InheritanceStrategy", NameSpaceURIHuagati);
                    }
                }
            }
        }
    }
}
