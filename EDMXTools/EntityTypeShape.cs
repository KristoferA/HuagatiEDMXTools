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
    /// Class representing an EntityTypeShape in the entity model diagram. This controls how and where in a diagram a conceptual model entity type appears.
    /// </summary>
    public class EntityTypeShape : EDMXMember, IEDMXRemovableMember  
    {
        private Designer _parentDesigner = null;
        private XmlElement _shapeElement = null;

        internal EntityTypeShape(EDMXFile parentFile, Designer parentDesigner, XmlElement shapeElement) : base(parentFile)
        {
            _parentDesigner = parentDesigner;
            _shapeElement = shapeElement;
        }

        internal EntityTypeShape(EDMXFile parentFile, Designer parentDesigner, ModelEntityType entityType)
            : base(parentFile)
        {
            _parentDesigner = parentDesigner;

            _shapeElement = parentDesigner.Document.CreateElement("EntityTypeShape", NameSpaceURIedmx);

            EntityTypeName = entityType.FullName;
            parentDesigner.DiagramElement.AppendChild(_shapeElement);
        }

        /// <summary>
        /// Name of the entity type represented by this entity type shape.
        /// </summary>
        public string EntityTypeName
        {
            get
            {
                return _shapeElement.GetAttribute("EntityType");
            }
            protected set
            {
                _shapeElement.SetAttribute("EntityType", value);
            }
        }

        private ModelEntityType _entityType = null;

        /// <summary>
        /// Reference to the entity type represented by this entity type shape.
        /// </summary>
        public ModelEntityType EntityType
        {
            get
            {
                if (_entityType == null)
                {
                    _entityType = ParentFile.ConceptualModel.EntityTypes.FirstOrDefault(et => et.FullName == EntityTypeName || et.AliasName == EntityTypeName);
                    if (_entityType != null)
                    {
                        _entityType.Removed += new EventHandler(entityType_Removed);
                    }
                }
                return _entityType;
            }
        }

        void entityType_Removed(object sender, EventArgs e)
        {
            this.Remove();
        }

        /// <summary>
        /// Event raised if an entity type shape is removed from the diagram
        /// </summary>
        public event EventHandler Removed;

        /// <summary>
        /// Method to remove an entity type shape from the diagram.
        /// </summary>
        public void Remove()
        {
            if (_shapeElement.ParentNode != null)
            {
                _shapeElement.ParentNode.RemoveChild(_shapeElement);

                if (Removed != null)
                {
                    Removed(this, new EventArgs());
                }
            }
        }

        /// <summary>
        /// Top position.
        /// </summary>
        public decimal Top
        {
            get
            {
                decimal value = 0;
                decimal.TryParse(_shapeElement.GetAttribute("PointY"), System.Globalization.NumberStyles.Any, new System.Globalization.CultureInfo("en-us").NumberFormat, out value);
                return value;
            }
            set
            {
                _shapeElement.SetAttribute("PointY", value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Left position.
        /// </summary>
        public decimal Left
        {
            get
            {
                decimal value = 0;
                decimal.TryParse(_shapeElement.GetAttribute("PointX"), System.Globalization.NumberStyles.Any, new System.Globalization.CultureInfo("en-us").NumberFormat, out value);
                return value;
            }
            set
            {
                _shapeElement.SetAttribute("PointX", value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Shape width.
        /// </summary>
        public decimal Width
        {
            get
            {
                decimal value = 0;
                decimal.TryParse(_shapeElement.GetAttribute("Width"), System.Globalization.NumberStyles.Any, new System.Globalization.CultureInfo("en-us").NumberFormat, out value);
                return value;
            }
            set
            {
                _shapeElement.SetAttribute("Width", value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Shape height.
        /// </summary>
        public decimal Height
        {
            get
            {
                decimal value = 0;
                decimal.TryParse(_shapeElement.GetAttribute("Height"), System.Globalization.NumberStyles.Any, new System.Globalization.CultureInfo("en-us").NumberFormat, out value);
                return value;
            }
            set
            {
                _shapeElement.SetAttribute("Height", value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Expanded or collapsed?
        /// </summary>
        public bool IsExpanded
        {
            get
            {
                return (!_shapeElement.GetAttribute("IsExpanded").Equals("false", StringComparison.InvariantCultureIgnoreCase));
            }
            set
            {
                _shapeElement.SetAttribute("IsExpanded", value.ToLString());
            }
        }

        /// <summary>
        /// Method that will find an empty location on the diagram surface to place the entity type shape in.
        /// </summary>
        public void AutoPosition()
        {
            _parentDesigner.AutoPositionShape(this);
        }
    }
}
