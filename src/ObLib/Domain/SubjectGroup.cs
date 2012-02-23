﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObLib.Domain
{
    public class SubjectGroup : ActiveRecordBase<SubjectGroup>
    {
        public virtual int Id { get; set; }
        public virtual Project Project { get; set; }
        public virtual string Name { get; set; }
        public virtual DateTime Tm { get; set; }

        public override void Delete()
        {
            Project.SubjectGroups.Remove(this);
            Project.Save();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
