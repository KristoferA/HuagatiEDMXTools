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
    /// Event arguments when a model member changes name
    /// </summary>
    public class NameChangeArgs : EventArgs
    {
        internal NameChangeArgs() { }

        /// <summary>
        /// Old name, the name used by the model member before the name change
        /// </summary>
        public string OldName { get; set; }

        /// <summary>
        /// New name, the name used by the model member after the name change
        /// </summary>
        public string NewName { get; set; }
    }

    /// <summary>
    /// Event arguments when auto-mapping store members to model members
    /// </summary>
    public class AutoMapArgs : EventArgs
    {
        internal AutoMapArgs() { }

        /// <summary>
        /// Store member property
        /// </summary>
        public StoreMemberProperty StoreMemberProperty { get; set; }

        /// <summary>
        /// Suggested matching model member property
        /// </summary>
        public ModelMemberProperty ModelMemberProperty { get; set; }

        /// <summary>
        /// Return value controlling if the suggested mapping should be used or not
        /// </summary>
        public bool UseMapping { get; set; }
    }
}
