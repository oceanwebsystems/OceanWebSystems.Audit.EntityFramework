﻿//HintName: AuditBase.g.cs
// <auto-generated />

using System;

namespace OceanWebSystems.Audit.EntityFramework
{
    public abstract class AuditBase : IAudit
    {
        public DateTime AuditDate { get; set; }

        public string AuditAction { get; set; }

        public int? UserId { get; set; }

        public string UserName { get; set; }
    }
}
