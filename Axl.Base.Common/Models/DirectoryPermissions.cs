﻿using System;

namespace Axl.Base.Models
{
    /// <summary>
    /// Banderas de permisos para validación de acceso a carpetas.
    /// </summary>
    [Flags]
    public enum DirectoryPermissions
    {
        None = 0,
        List = 1,
        Write = 2,
        Delete = 4,
        All = List | Write | Delete
    }
}

