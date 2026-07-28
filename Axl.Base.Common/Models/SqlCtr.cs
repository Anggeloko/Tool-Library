﻿using Axl.Base.Interfaces;

namespace Axl.Base.Models
{
    /// <summary>
    /// Contenedor que agrupa las operaciones de base de datos e interfaces de monitoreo.
    /// </summary>
    public class SqlCtr
    {
        public ISql Sql { get; set; }
        public ICheckable Check { get; set; }

        public SqlCtr(ISql operations, ICheckable health)
        {
            Sql = operations;
            Check = health;
        }
    }
}

