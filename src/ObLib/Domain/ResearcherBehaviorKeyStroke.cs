﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObLib.Domain
{
    public class ResearcherBehaviorKeyStroke : ActiveRecordBase<ResearcherBehaviorKeyStroke>
    {
        public virtual int Id { get; set; }
        public virtual Researcher Researcher { get; set; }
        public virtual Behavior Behavior { get; set; }
        public virtual string KeyStroke { get; set; }
        public virtual DateTime Tm { get; set; }

        public override string ToString()
        {
            return String.Format("{0}, {1}", Behavior, KeyStroke);
        }
    }
}