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
    /// Represents a mapping condition instructing EF which entity type to map to. Used with entity inheritance (TPH) and entity splitting.
    /// </summary>
    public class MappingCondition : EDMXMember, IEDMXRemovableMember
    {
        private EntitySetMapping _entitySetMapping = null;
        private ModelEntityType _modelEntityType = null;
        private StoreMemberProperty _discriminatorColumn = null;

        private XmlElement _entityTypeMapping = null;
        private XmlElement _mappingFragment = null;
        private XmlElement _mappingCondition = null;

        internal MappingCondition(EDMXFile parentFile, EntitySetMapping entitySetMapping, XmlElement entitySetMappingElement, ModelEntityType modelEntityType, StoreMemberProperty discriminatorColumn, string discriminatorValue)
            : base(parentFile)
        {
            _entitySetMapping = entitySetMapping;
            _modelEntityType = modelEntityType;
            _discriminatorColumn = discriminatorColumn;

            string storeEntitySetName = discriminatorColumn.EntityType.EntitySet.Name;

            //get hold of the type mapping
            _entityTypeMapping = (XmlElement)entitySetMappingElement.SelectSingleNode("map:EntityTypeMapping[@TypeName=" + XmlHelpers.XPathLiteral(modelEntityType.FullName) + " or @TypeName=" + XmlHelpers.XPathLiteral("IsTypeOf(" + modelEntityType.FullName + ")") + " or @TypeName=" + XmlHelpers.XPathLiteral(modelEntityType.AliasName) + " or @TypeName=" + XmlHelpers.XPathLiteral("IsTypeOf(" + modelEntityType.AliasName + ")") + "]", NSM);
            if (_entityTypeMapping == null)
            {
                throw new ArgumentException("The entity type " + modelEntityType.Name + " is not a participant in this entity set mapping.");
            }

            _mappingFragment = (XmlElement)_entityTypeMapping.SelectSingleNode("map:MappingFragment[@StoreEntitySet=" + XmlHelpers.XPathLiteral(storeEntitySetName) + "]", NSM);
            if (_mappingFragment == null)
            {
                throw new ArgumentException("The store entityset " + storeEntitySetName + " is not a participant in this entity set mapping.");
            }

            _mappingCondition = EDMXDocument.CreateElement("Condition", NameSpaceURImap);
            _mappingCondition.SetAttribute("ColumnName", discriminatorColumn.Name);
            if (discriminatorValue != null)
            {
                _mappingCondition.SetAttribute("Value", discriminatorValue);
            }
            else
            {
                _mappingCondition.SetAttribute("IsNull", "true");
            }
            _mappingFragment.AppendChild(_mappingCondition);
        }

        internal MappingCondition(EDMXFile parentFile, HuagatiEDMXTools.EntitySetMapping entitySetMapping, XmlElement conditionElement) : base(parentFile)
        {
            _entitySetMapping = entitySetMapping;

            _mappingCondition = conditionElement;
            _mappingFragment = (XmlElement)_mappingCondition.ParentNode;
            _entityTypeMapping = (XmlElement)_mappingFragment.ParentNode;

            string entityTypeName = EDMXUtils.StripTypeOf(_entityTypeMapping.GetAttribute("TypeName"));
            _modelEntityType = ParentFile.ConceptualModel.EntityTypes.FirstOrDefault(et => et.FullName.Equals(entityTypeName, StringComparison.InvariantCultureIgnoreCase) || et.AliasName.Equals(entityTypeName, StringComparison.InvariantCultureIgnoreCase));

            if (_modelEntityType != null)
            {
                string columnName = _mappingCondition.GetAttribute("ColumnName");
                _discriminatorColumn = EntitySetMapping.StoreEntitySetsFor(_modelEntityType).SelectMany(c => c.EntityType.MemberProperties).FirstOrDefault(mp => mp.Name.Equals(columnName, StringComparison.InvariantCultureIgnoreCase));
            }
        }

        #region IEDMXRemovableMember Members

        /// <summary>
        /// Removes the object from the model.
        /// </summary>
        public void Remove()
        {
            _mappingFragment.RemoveChild(_mappingCondition);
            if (Removed != null)
            {
                Removed(this, new EventArgs());
            }
        }

        /// <summary>
        /// Event fired when the object has been removed from the model
        /// </summary>
        public event EventHandler Removed;

        #endregion

        /// <summary>
        /// EntitySetMapping that this condition belongs to.
        /// </summary>
        public EntitySetMapping EntitySetMapping
        {
            get
            {
                return _entitySetMapping;
            }
        }

        /// <summary>
        /// Entity type that this condition applies to.
        /// </summary>
        public ModelEntityType ModelEntityType
        {
            get
            {
                return _modelEntityType;
            }
        }

        /// <summary>
        /// Storage model column defining this condition.
        /// </summary>
        public StoreMemberProperty DiscriminatorColumn
        {
            get
            {
                return _discriminatorColumn;
            }
        }

        /// <summary>
        /// Discriminator value that makes the condition valid.
        /// </summary>
        public string DiscriminatorValue
        {
            get
            {
                if (_mappingCondition.GetAttribute("IsNull").Equals("true", StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }
                else
                {
                    return _mappingCondition.GetAttribute("Value");
                }
            }
            set
            {
                if (value != null)
                {
                    if (_mappingCondition.HasAttribute("IsNull"))
                    {
                        _mappingCondition.RemoveAttribute("IsNull");
                    }
                    _mappingCondition.SetAttribute("Value", value);
                }
                else
                {
                    if (_mappingCondition.HasAttribute("Value"))
                    {
                        _mappingCondition.RemoveAttribute("Value");
                    }
                    _mappingCondition.SetAttribute("IsNull", "true");
                }
            }
        }
    }
}
