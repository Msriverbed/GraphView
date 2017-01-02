﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphView
{
    internal class GremlinAddEVariable: GremlinVariableReference, ISqlStatement
    {
        private static long _count = 0;

        public static string GenerateTableAlias()
        {
            return "AddE_" + _count++;
        }

        public GremlinVariableReference FromVariable { get; set; }
        public GremlinVariableReference ToVariable { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public string EdgeLabel { get; set; }

        public override WTableReference ToTableReference()
        {
            return new WVariableTableReference()
            {
                Variable = GremlinUtil.GetVariableReference(VariableName),
                Alias = GremlinUtil.GetIdentifier(VariableName)
            };
        }

        public WSetVariableStatement ToSetVariableStatement()
        {
            var columnK = new List<WColumnReferenceExpression>();

            var selectBlock = new WSelectQueryBlock()
            {
                SelectElements = new List<WSelectElement>(),
                FromClause = new WFromClause()
                {
                    TableReferences = new List<WTableReference>()
                }
            };
            selectBlock.FromClause.TableReferences.Add(FromVariable.ToTableReference());
            selectBlock.FromClause.TableReferences.Add(ToVariable.ToTableReference());

            var fromVarExpr = GremlinUtil.GetColumnReferenceExpression(FromVariable.VariableName);
            selectBlock.SelectElements.Add(GremlinUtil.GetSelectScalarExpression(fromVarExpr));

            var toVarExpr = GremlinUtil.GetColumnReferenceExpression(ToVariable.VariableName);
            selectBlock.SelectElements.Add(GremlinUtil.GetSelectScalarExpression(toVarExpr));

            //Add edge key-value
            columnK.Add(GremlinUtil.GetColumnReferenceExpression("label"));
            var valueExpr = GremlinUtil.GetValueExpression(EdgeLabel);
            selectBlock.SelectElements.Add(GremlinUtil.GetSelectScalarExpression(valueExpr));
            foreach (var property in Properties)
            {
                columnK.Add(GremlinUtil.GetColumnReferenceExpression(property.Key));
                valueExpr = GremlinUtil.GetValueExpression(property.Value.ToString());
                selectBlock.SelectElements.Add(GremlinUtil.GetSelectScalarExpression(valueExpr));
            }

            //hack
            string temp = "\"label\", \"" + EdgeLabel + "\"";
            foreach (var property in Properties)
            {
                temp += ", \"" + property.Key + "\", \"" + property.Value + "\"";
            }
            //=====

            var insertStatement = new WInsertSpecification()
            {
                Columns = columnK,
                InsertSource = new WSelectInsertSource() { Select = selectBlock },
                Target = GremlinUtil.GetNamedTableReference("Edge")
            };

            var addEStatement = new WInsertEdgeSpecification(insertStatement)
            {
                SelectInsertSource = new WSelectInsertSource() { Select = selectBlock }
            };

            return new WSetVariableStatement()
            {
                Expression = new WScalarSubquery()
                {
                    SubQueryExpr = addEStatement
                },
                Variable = GremlinUtil.GetVariableReference(VariableName)
            };
        }

        public GremlinAddEVariable(string edgeLabel, GremlinVariableReference currVariable)
        {
            Properties = new Dictionary<string, object>();

            VariableName = GenerateTableAlias();
            FromVariable = currVariable;
            ToVariable = currVariable;
            EdgeLabel = edgeLabel;
        }

        internal override void From(GremlinToSqlContext currentContext, string label)
        {
            throw new NotImplementedException();
        }

        internal override void From(GremlinToSqlContext currentContext, GraphTraversal2 fromVertexTraversal)
        {
            GremlinUtil.InheritedContextFromParent(fromVertexTraversal, currentContext);

            var context = fromVertexTraversal.GetEndOp().GetContext();

            GremlinVariableReference newVariableReference = null;
            if (context.PivotVariable is GremlinAddVVariable)
            {
                var index = currentContext.SetVariables.FindIndex(p=>p == this);
                FromVariable = context.PivotVariable as GremlinAddVVariable;
                currentContext.SetVariables.Insert(index, context.PivotVariable as GremlinAddVVariable);
            }
            else
            {
                newVariableReference = new GremlinVariableReference(context);
                currentContext.VariableList.Add(newVariableReference);
                FromVariable = newVariableReference;
            }
        }

        internal override void Property(GremlinToSqlContext currentContext, Dictionary<string, object> properties)
        {
            foreach (var pair in properties)
            {
                Properties[pair.Key] = pair.Value;
            }
        }

        internal override void To(GremlinToSqlContext currentContext, string label)
        {
            throw new NotImplementedException();
        }

        internal override void To(GremlinToSqlContext currentContext, GraphTraversal2 toVertexTraversal)
        {
            GremlinUtil.InheritedContextFromParent(toVertexTraversal, currentContext);

            var context = toVertexTraversal.GetEndOp().GetContext();
            if (context.PivotVariable is GremlinAddVVariable)
            {
                var index = currentContext.SetVariables.FindIndex(p => p == this);
                ToVariable = context.PivotVariable as GremlinAddVVariable;
                currentContext.SetVariables.Insert(index, context.PivotVariable as GremlinAddVVariable);
            }
            else
            {
                GremlinVariableReference newVariableReference = new GremlinVariableReference(context);
                currentContext.VariableList.Add(newVariableReference);
                ToVariable = newVariableReference;
            }
        }
    }
}
