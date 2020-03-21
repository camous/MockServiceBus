using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using TSQL;
using TSQL.Tokens;

namespace MockServiceBus
{
    [DebuggerDisplay("{Name} {Filter}")]
    public class Rule
    {
        private enum NextIdentifierType { None, CustomProperty, BrokeredProperty, Operande };
        private NextIdentifierType nextidentifier = NextIdentifierType.None;
        public string Name { get; set; }
        private string filter;
        public string Filter
        {
            get
            {
                return filter;
            }
            set
            {
                filter = value;
                ConvertFilter(value);
            }
        }
        public string LinqFilter
        {
            get;
            private set;
        }
        public string Action { get; set; }

        private void ConvertFilter(string filter)
        {
            var where = TSQLStatementReader.ParseStatements("where " + filter)[0];
            string linqfilter = string.Empty;
            string nextcast = string.Empty;
            foreach (TSQLToken token in where.Tokens)
            {
                switch (token.Type)
                {
                    case TSQLTokenType.Identifier:

                        // do we have a cast indication {dotnettype}
                        var cast = Regex.Match(token.AsIdentifier.Name, "^{(.*)}$").Groups[1]?.Value;
                        if (!string.IsNullOrEmpty(cast))
                        {
                            nextcast = $"{cast}(";
                        }
                        else
                        {
                            if (token.AsIdentifier.Name == "sys")
                            {
                                nextidentifier = NextIdentifierType.BrokeredProperty;
                            }
                            else
                            {
                                var endnextcast = !string.IsNullOrEmpty(nextcast) ? ")" : string.Empty;
                                if (nextidentifier == NextIdentifierType.CustomProperty)
                                {
                                    linqfilter += $" \"{token.AsIdentifier.Name}\"";
                                    nextidentifier = NextIdentifierType.None;
                                }
                                else if (nextidentifier == NextIdentifierType.BrokeredProperty)
                                {
                                    linqfilter += $"  BrokeredProperties.Keys.Contains(\"{token.AsIdentifier.Name}\") && {nextcast}BrokeredProperties[\"{token.AsIdentifier.Name}\"]{endnextcast}";
                                    nextidentifier = NextIdentifierType.Operande;
                                    nextcast = string.Empty;
                                }
                                else if (nextidentifier == NextIdentifierType.Operande)
                                {
                                    linqfilter += $" {nextcast}CustomProperties[\"{token.AsIdentifier.Name}\"]{endnextcast}";
                                    nextidentifier = NextIdentifierType.None;
                                    nextcast = string.Empty;
                                }
                                else
                                {
                                    linqfilter += $"  CustomProperties.Keys.Contains(\"{token.AsIdentifier.Name}\") && {nextcast}CustomProperties[\"{token.AsIdentifier.Name}\"]{endnextcast}";
                                    nextcast = string.Empty;
                                }
                            }
                        }
                        break;

                    case TSQLTokenType.Operator:
                        switch (token.AsOperator.Text)
                        {
                            case "=": linqfilter += " == "; break;
                            case "<>": case "!=": linqfilter += " != "; break;
                            default: linqfilter += token.AsOperator.Text; break;
                        }
                        break;

                    case TSQLTokenType.StringLiteral:
                        linqfilter += $"\"{token.AsStringLiteral.Value}\"";
                        break;

                    case TSQLTokenType.Keyword:
                        switch (token.AsKeyword.Text.ToLower())
                        {
                            case "not": linqfilter += " !"; break;
                            case "or": linqfilter += " || "; break;
                            case "and": linqfilter += " && "; break;
                            case "exists":
                                linqfilter += " CustomProperties.Keys.Contains";
                                nextidentifier = NextIdentifierType.CustomProperty; // otherwise, we generate CustomProperties.Keys.Contains(CustomProperties["property"])
                                break;

                            case "where": break;
                            default:
                                linqfilter += " " + token.Text;
                                break;
                        }
                        break;

                    case TSQLTokenType.Character:
                        // if we have sys.EnqueuedTimeUtc, sys triggered NextIdentifierType.BrokeredProperty and 'sys.' should be fully removed
                        if (nextidentifier != NextIdentifierType.BrokeredProperty)
                            linqfilter += " " + token.Text;
                        break;

                    default:
                        linqfilter += " " + token.Text;
                        break;
                }
            }
            LinqFilter = linqfilter;
        }

        public void ApplyAction(Message message)
        {
            if (string.IsNullOrEmpty(Action))
            {
                return;
            }

            var update = TSQLStatementReader.ParseStatements(Action)[0];

            // we assume that if we have a 'SET(0) property(1) =(2) value(3), ', the next group is 4 tokens later
            for (int i = 0; i < update.Tokens.Count; i += 4)
            {
                var property = update.Tokens[i+1].AsIdentifier.Name;
                var value = update.Tokens[i + 3].AsStringLiteral.Value;

                if (message.CustomProperties.ContainsKey(property))
                {
                    message.CustomProperties[property] = value;
                }
                else
                {
                    message.CustomProperties.Add(property, value);
                }
            }
        }

        public Rule Copy()
        {
            var clone = new Rule();

            if (!string.IsNullOrEmpty(Name))
            {
                clone.Name = string.Copy(Name);
            }
            if (!string.IsNullOrEmpty(Action))
            {
                clone.Action = string.Copy(Action);
            }
            if (!string.IsNullOrEmpty(Filter))
            {
                clone.Filter = string.Copy(Filter);
            }

            return clone;
        }
    }
}
