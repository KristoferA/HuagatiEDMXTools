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
    internal class ExceptionTools
    {
        internal static void AddExceptionData(Exception exception, IEDMXNamedMember namedMember)
        {
            try
            {
                if (namedMember != null && exception != null)
                {
                    if (!exception.Data.Contains("EDMXType"))
                    {
                        exception.Data.Add("EDMXType", namedMember.GetType().Name);
                    }
                    if (!exception.Data.Contains("EDMXObjectName"))
                    {
                        string objectName = namedMember.FullName;
                        if (string.IsNullOrEmpty(objectName))
                        {
                            objectName = namedMember.Name;
                        }
                        exception.Data.Add("EDMXObjectName", objectName);
                    }
                }
            }
            catch { }
        }
    }

    public class InvalidModelObjectException : Exception
    {
        internal InvalidModelObjectException() : base() { }
        internal InvalidModelObjectException(string message) : base(message) { }
        internal InvalidModelObjectException(string message, Exception innerException) : base(message, innerException) { }
    }
}
