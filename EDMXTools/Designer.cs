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
    /// Represents designer diagrams.
    /// </summary>
    public class Designer : EDMXMember
    {
        private XmlDocument _document = null;
        private XmlElement _designerElement = null;
        private XmlElement _diagramsElement = null;
        private XmlElement _diagramElement = null;
        private XmlElement _optionsPropertySetElement = null;

        private bool _addVertical = false;
        private decimal? _nextShapeX = null;
        private decimal? _nextShapeY = null;
        private decimal _maxX = 0;
        private decimal _maxY = 0;

        internal XmlDocument Document
        {
            get
            {
                return _document;
            }
        }

        internal XmlElement DiagramElement
        {
            get
            {
                return _diagramElement;
            }
        }

        internal Designer(EDMXFile parentFile)
            : base(parentFile)
        {
            if (parentFile.EDMXVersion == EDMXVersionEnum.EDMX2012
                && parentFile.EDMXDiagramDocument != null)
            {
                _document = parentFile.EDMXDiagramDocument;
            }
            else
            {
                _document = EDMXDocument;
            }

            //designer wrapper
            _designerElement = (XmlElement)_document.DocumentElement.SelectSingleNode("edmx:Designer", NSM);

            //get or create the Diagrams element
            _diagramsElement = _designerElement.GetOrCreateElement("edmx", "Diagrams", NSM);

            //get or create the Diagram element
            _diagramElement = _diagramsElement.GetOrCreateElement("edmx", "Diagram", NSM);
            if (!_diagramElement.HasAttribute("Name"))
            {
                _diagramElement.SetAttribute("Name", "default");
            }

            //get options
            XmlElement optionsElement = _designerElement.GetOrCreateElement("edmx", "Options", NSM, true);
            _optionsPropertySetElement = optionsElement.GetOrCreateElement("edmx", "DesignerInfoPropertySet", NSM);
        }

        /// <summary>
        /// Diagram name
        /// </summary>
        public string DiagramName
        {
            get
            {
                return _diagramElement.GetAttribute("Name");
            }
            set
            {
                _diagramElement.SetAttribute("Name", value);
            }
        }

        private XmlNodeList _entityTypeShapeElements = null;
        private XmlNodeList EntityTypeShapeElements
        {
            get
            {
                if (_entityTypeShapeElements == null)
                {
                    _entityTypeShapeElements = _diagramsElement.SelectNodes("edmx:Diagram/edmx:EntityTypeShape", NSM);
                }
                return _entityTypeShapeElements;
            }
        }

        /// <summary>
        /// Adds an entity type shape to the diagram
        /// </summary>
        /// <param name="entityType">Model entity type to add to the diagram.</param>
        /// <returns>An EntityTypeShape object</returns>
        public EntityTypeShape AddEntityTypeShape(ModelEntityType entityType)
        {
            if (entityType == null) { throw new ArgumentNullException("entityType"); }

            EntityTypeShape ets = EntityTypeShapes.FirstOrDefault(es => es.EntityType == entityType);
            if (ets == null)
            {
                ets = new EntityTypeShape(ParentFile, this, entityType);
                ets.Removed += new EventHandler(ets_Removed);
                _entityTypeShapes.Add(ets.EntityTypeName, ets);
            }
            return ets;
        }

        private bool _entityTypeShapesEnumerated = false;
        private Dictionary<string, EntityTypeShape> _entityTypeShapes = new Dictionary<string, EntityTypeShape>();

        /// <summary>
        /// Enumeration of all entity type shapes on the diagram
        /// </summary>
        public IEnumerable<EntityTypeShape> EntityTypeShapes
        {
            get
            {
                if (_entityTypeShapesEnumerated == false)
                {
                    foreach (XmlElement entityTypeShapeElement in EntityTypeShapeElements)
                    {
                        EntityTypeShape ets = null;
                        string etName = entityTypeShapeElement.GetAttribute("EntityType");
                        if (_entityTypeShapes.ContainsKey(etName))
                        {
                            ets = _entityTypeShapes[etName];
                        }
                        else
                        {
                            ets = new EntityTypeShape(ParentFile, this, entityTypeShapeElement);
                            ets.Removed += new EventHandler(ets_Removed);
                            _entityTypeShapes.Add(etName, ets);
                        }
                        yield return ets;
                    }
                    _entityTypeShapesEnumerated = true;
                    _entityTypeShapeElements = null;
                }
                else
                {
                    foreach (EntityTypeShape ets in _entityTypeShapes.Values)
                    {
                        yield return ets;
                    }
                }
            }
        }

        /// <summary>
        /// Total width of the entire diagram
        /// </summary>
        public decimal DiagramWidth
        {
            get
            {
                if (EntityTypeShapes.Any())
                {
                    return EntityTypeShapes.Max(ets => ets.Left + ets.Width);
                }
                else
                {
                    return 1;
                }
            }
        }

        /// <summary>
        /// Total height of the entire diagram
        /// </summary>
        public decimal DiagramHeight
        {
            get
            {
                if (EntityTypeShapes.Any())
                {
                    return EntityTypeShapes.Max(ets => ets.Top + ets.Height);
                }
                else
                {
                    return 1;
                }
            }
        }

        void ets_Removed(object sender, EventArgs e)
        {
            _entityTypeShapes.Remove(((EntityTypeShape)sender).EntityTypeName);
        }

        private DesignerOption _designerOption = null;
        private DesignerOption DesignerOption
        {
            get
            {
                if (_designerOption == null)
                {
                    _designerOption = new DesignerOption(this);
                }
                return _designerOption;
            }
        }

        /// <summary>
        /// Returns the designer options
        /// </summary>
        public DesignerOption Option
        {
            get
            {
                return _designerOption;
            }
            set
            {
                _designerOption = value;
            }
        }

        internal bool GetOption(string name)
        {
            XmlElement propertyElement = (XmlElement)_optionsPropertySetElement.SelectSingleNode("edmx:DesignerProperty[@Name=" + XmlHelpers.XPathLiteral(name) + "]", NSM);
            return (propertyElement.GetAttribute("Value").Equals("true", StringComparison.InvariantCultureIgnoreCase));
        }

        internal void SetOption(string name, bool value)
        {
            XmlElement propertyElement = (XmlElement)_optionsPropertySetElement.SelectSingleNode("edmx:DesignerProperty[@Name=" + XmlHelpers.XPathLiteral(name) + "]", NSM);
            if (propertyElement == null)
            {
                propertyElement = _document.CreateElement("DesignerProperty", NameSpaceURIedmx);
                propertyElement.SetAttribute("Name", name);
            }
            propertyElement.SetAttribute("Value", value.ToLString());
        }

        internal void AutoPositionShape(EntityTypeShape entityTypeShape)
        {
            //determine initial nextX/nextY position and if we're going to start adding horizontally or vertically...
            if (_nextShapeX == null || _nextShapeY == null)
            {
                decimal diagramWidth = DiagramWidth;
                decimal diagramHeight = DiagramHeight;
                if (diagramHeight > diagramWidth)
                {
                    _addVertical = true;
                    _nextShapeX = diagramWidth + .75M;
                    _nextShapeY = .75M;
                    _maxY = diagramHeight;
                }
                else
                {
                    _addVertical = false;
                    _nextShapeX = .75M;
                    _nextShapeY = diagramHeight + .75M;
                    _maxX = diagramWidth;
                }
            }

            //set position and size for the current element
            entityTypeShape.Top = _nextShapeY.Value;
            entityTypeShape.Left = _nextShapeX.Value;
            entityTypeShape.Width = 1.5M;
            entityTypeShape.Height = 2M;

            //increase nextx/nexty depending on if we're adding horizontally or vertically
            if (_addVertical)
            {
                _nextShapeY = _nextShapeY.Value + 2.5M;
                if (_nextShapeY > _maxY)
                {
                    _nextShapeY = null;
                }
            }
            else
            {
                _nextShapeX = _nextShapeX.Value + 2M;
                if (_nextShapeX > _maxX)
                {
                    _nextShapeX = null;
                }
            }
        }
    }

    /// <summary>
    /// Diagram designer options, exposed by the Designer class
    /// </summary>
    public class DesignerOption
    {
        private Designer _parentDesigner = null;
        internal DesignerOption(Designer parentDesigner)
        {
            _parentDesigner = parentDesigner;
        }

        /// <summary>
        /// Enumeration of values
        /// </summary>
        /// <param name="name">Option name name</param>
        /// <returns>Option value</returns>
        public bool this[string name]
        {
            get
            {
                return _parentDesigner.GetOption(name);
            }
            set
            {
                _parentDesigner.SetOption(name, value);
            }
        }
    }
}
