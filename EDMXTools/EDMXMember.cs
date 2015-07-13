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
    /// Abstract base class for all classes representing objects that are part of the EDMX (CSDL/SSDL/MSL, or diagram) structure. Provides internal shared logic between the objects.
    /// </summary>
    public abstract class EDMXMember
    {
        private EDMXFile _parentFile = null;

        internal EDMXMember(EDMXFile parentFile)
        {
            _parentFile = parentFile;
        }

        internal EDMXFile ParentFile
        {
            get
            {
                return _parentFile;
            }
        }

        /// <exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        protected internal XmlDocument EDMXDocument
        {
            get
            {
                return _parentFile.EDMXDocument;
            }
        }

        /// <exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        protected internal XmlNamespaceManager NSM
        {
            get
            {
                return _parentFile.NSM;
            }
        }

        private string _namespaceURIedmx = null;

        /// <exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        protected internal string NameSpaceURIedmx
        {
            get
            {
                if (string.IsNullOrEmpty(_namespaceURIedmx))
                {
                    _namespaceURIedmx = NSM.LookupNamespace("edmx");
                }
                return _namespaceURIedmx;
            }
        }

        private string _namespaceURIstore = null;

        /// <exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        protected internal string NameSpaceURIstore
        {
            get
            {
                if (string.IsNullOrEmpty(_namespaceURIstore))
                {
                    _namespaceURIstore = NSM.LookupNamespace("store");
                }
                return _namespaceURIstore;
            }
        }

        private string _namespaceURIssdl = null;

        /// <exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        protected internal string NameSpaceURIssdl
        {
            get
            {
                if (string.IsNullOrEmpty(_namespaceURIssdl))
                {
                    _namespaceURIssdl = NSM.LookupNamespace("ssdl");
                }
                return _namespaceURIssdl;
            }
        }

        private string _namespaceURIhuagati = null;

        /// <exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        protected internal string NameSpaceURIHuagati
        {
            get
            {
                if (_namespaceURIhuagati == null)
                {
                    if (NSM.LookupNamespace("huagati") == null)
                    {
                        NSM.AddNamespace("huagati", "http://www.huagati.com/edmxtools/annotations");
                    }
                    _namespaceURIhuagati = NSM.LookupNamespace("huagati");
                }
                return _namespaceURIhuagati;
            }
        }

        private string _namespaceURIcsdl = null;

        /// <exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        protected internal string NameSpaceURIcsdl
        {
            get
            {
                if (string.IsNullOrEmpty(_namespaceURIcsdl))
                {
                    _namespaceURIcsdl = NSM.LookupNamespace("edm");
                }
                return _namespaceURIcsdl;
            }
        }

        private string _namespaceURIannotation = null;

        /// <exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        protected internal string NameSpaceURIannotation
        {
            get
            {
                if (string.IsNullOrEmpty(_namespaceURIannotation))
                {
                    _namespaceURIannotation = NSM.LookupNamespace("annotation");
                }
                return _namespaceURIannotation;
            }
        }

        private string _namespaceURImap = null;

        /// <exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        protected internal string NameSpaceURImap
        {
            get
            {
                if (string.IsNullOrEmpty(_namespaceURImap))
                {
                    _namespaceURImap = NSM.LookupNamespace("map");
                }
                return _namespaceURImap;
            }
        }

        private string _namespaceURIcodegen = null;

        /// <exclude />
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        protected internal string NameSpaceURIcodegen
        {
            get
            {
                if (string.IsNullOrEmpty(_namespaceURIcodegen))
                {
                    _namespaceURIcodegen = NSM.LookupNamespace("codegen");
                }
                return _namespaceURIcodegen;
            }
        }
    }
}
