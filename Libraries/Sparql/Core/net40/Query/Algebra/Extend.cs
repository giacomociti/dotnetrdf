﻿using System;
using System.Collections.Generic;
using System.Linq;
using VDS.RDF.Query.Engine;
using VDS.RDF.Query.Engine.Algebra;
using VDS.RDF.Query.Expressions;

namespace VDS.RDF.Query.Algebra
{
    public class Extend
        : BaseUnaryAlgebra
    {
        public Extend(IAlgebra innerAlgebra, IEnumerable<KeyValuePair<String, IExpression>> assignments)
            : base(innerAlgebra)
        {
            this.Assignments = assignments.ToList().AsReadOnly();
            if (this.Assignments.Count == 0) throw new ArgumentException("Number of assignments must be >= 1", "assignments");
        }

        public IList<KeyValuePair<String, IExpression>> Assignments { get; private set; }

        public override void Accept(IAlgebraVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override IEnumerable<ISolution> Execute(IAlgebraExecutor executor, IExecutionContext context)
        {
            return executor.Execute(this, context);
        }

        public override bool Equals(IAlgebra other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other == null) return false;
            if (!(other is Extend)) return false;

            Extend e = (Extend) other;
            if (this.Assignments.Count != e.Assignments.Count) return false;
            for (int i = 0; i < this.Assignments.Count; i++)
            {
                if (!this.Assignments[i].Equals(e.Assignments[i])) return false;
            }
            return this.InnerAlgebra.Equals(e.InnerAlgebra);
        }
    }
}