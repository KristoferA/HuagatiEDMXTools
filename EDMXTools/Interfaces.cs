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
    internal interface IEDMXNamedMember
    {
        string Name { get; set; }
        string FullName { get; }
        string AliasName { get; }
        event EventHandler<NameChangeArgs> NameChanged;
    }

    internal interface IEDMXRemovableMember
    {
        void Remove();
        event EventHandler Removed;
    }

    internal interface IEDMXMemberDocumentation
    {
        string ShortDescription { get; set; }
        string LongDescription { get; set; }
    }
}
