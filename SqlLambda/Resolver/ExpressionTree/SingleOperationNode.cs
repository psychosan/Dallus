﻿using System.Linq.Expressions;

namespace SqlLambda.Resolver.ExpressionTree
{
    class SingleOperationNode : Node
    {
        public ExpressionType Operator { get; set; }
        public Node Child { get; set; }
    }
}
