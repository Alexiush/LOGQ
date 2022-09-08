---
layout: default
title: Knowledge base
parent: Reference
nav_order: 1
---

# Knowledge base

> Reference does not provide full listing, but only mentioned parts, for full listing visit [LOGQ GitHub](https://github.com/Alexiush/LOGQ).

### Fact, Bound fact

Fact - base class for facts. FactType method must return base type of the fact 
(to compare same base type facts with knowledge base, when class created manually must return fact class).
== and != operators used for fact to fact comparison.

```cs
public abstract class Fact 
{
    abstract public Type FactType();
}
```

BoundFact - base class for bound facts, used in queries. FactType method must return base type of the fact 
(to compare same base type facts with knowledge base, when class created manually must return fact class).
Bind method used to bind values to matched facts, needs fact to bound to and journal to make records about changes.
Equals method, == and != operators used for bound fact to bound fact, bound fact to fact comparison.

```cs
public abstract class BoundFact : Fact 
{
    abstract public void Bind(Fact fact, List<IBound> copyStorage);
}
```

### Rule, Bound rule, Rule with body

Rule - base class for rule patterns. RuleType method must return base type of the rule.
(to compare same base type rules with knowledge base, when class created manually must return rule class).
Equals method, == and != operators used for rule to rule comparison.

```cs
public abstract class Rule 
{
    abstract public Type RuleType();
}
```

BoundRule - base class for bound rule, used in queries. RuleType method must return base type of the rule.
(to compare same base type rules with knowledge base, when class created manually must return rule class).
Bind method used to bind values to matched rules, needs journal to make records about changes.
Equals method, == and != operators used for bound rule to bound rule, bound rule to rule comparison.

```cs
public abstract class BoundRule : Rule
{
    abstract public void Bind(List<IBound> copyStorage);
}
```

RuleWithBody - class that represents rule by rule pattern and proof - function that returns query for matched bound rule.

```cs
public RuleWithBody(Rule head, Func<BoundRule, LogicalQuery> body)
{
    this.Head = head;
    this.Body = body;
}
```

### Variable, BoundVariable, rule patterns

IVariable - interface for variable types

```cs
public interface IVariable { }
```

IBound - interface for variables that journal their state. Any class that implements IBound 
needs to provide Rollback method implementation that will return value to it's previous state.

```cs
public interface IBound { public void Rollback(); }
```

Variable - class-container for fact values.

```cs
public class Variable<T> : IVariable
{
  public Variable(T value)
  {
      this.Value = value;
  }

  public static implicit operator Variable<T>(T value)
      => new Variable<T>(value);
}
```

BoundVariable - class-container for bound fact and bound rule values that journals it changes with UpdateValue method.
Has IsBound method that checks if bound variable currently holds a value.
```cs
public class BoundVariable<T> : Variable<T>, IBound
{
  public BoundVariable(T value) : base(value)
  {
      copies.Push(value);
  }

  public static implicit operator BoundVariable<T>(T value)
      => new BoundVariable<T>(value);

  private Stack<T> copies = new Stack<T>();
  public bool IsBound() => copies.Count > 0;

  public void UpdateValue(List<IBound> copyStorage, T value)
  {
      copyStorage.Add(this);
      copies.Push(value);
      Value = value;
  }

  public void Rollback()
  {
      if (copies.Count == 0)
      {
          throw new ArgumentException("Nothing to rollback");
      }

      copies.Pop();
      if (copies.Count > 0)
      {
          Value = copies.Peek();
      }
  }
}
```

UnboundVariable - bound variable that starts without value - can be used as any value.

```cs
public sealed class UnboundVariable<T> : BoundVariable<T>
{
    public UnboundVariable() {}

    public static bool operator ==(UnboundVariable<T> fact, Variable<T> otherFact)
    {
        return true;
    }

    public static bool operator !=(UnboundVariable<T> fact, Variable<T> otherFact)
    {
        return false;
    }

    public override bool Equals(object obj)
    {
        Variable<T> variable = obj as Variable<T>;

        if (obj is null)
        {
            return false;
        }

        return true;
    }
}
```

RuleVariable - base class for rule patterns.

```cs
public class RuleVariable<T> : Variable<T> 
{ 
    protected RuleVariable() { }
}
```

AnyValue - pattern that accepts any value.

```cs
public sealed class AnyValue<T> : RuleVariable<T>
{
    public AnyValue() { }
}
```

AnyValueBound - pattern that accepts any bound value.

```cs
public sealed class AnyValueBound<T> : RuleVariable<T>
{
    public AnyValueBound() { }
}
```

Equal - pattern that accepts only value that equal to passed value.

```cs
public sealed class Equal<T> : RuleVariable<T>
{
    public Equal(T value)
    {
        Value = value;
    }
}
```

NotEqual - pattern that accepts any value except passed value.

```cs
public sealed class NotEqual<T> : RuleVariable<T>
{
    public NotEqual(T value)
    {
        Value = value;
    }
}
```

NotEqualBound - pattern that accepts any bound value except passed value.

```cs
public sealed class NotEqualBound<T> : RuleVariable<T>
{
    public NotEqualBound(T value)
    {
        Value = value;
    }
}
```

Unbound - pattern that accepts only unbound values. 

```cs
public sealed class UnboundValue<T> : RuleVariable<T> {}
```

### Knowledge base

Knowledge base groups facts and rules.
Facts added with DeclareFact method. Rules added with DeclareRule method. CheckForFacts and CheckForRules methods return initializers for logical actions.

```cs
public sealed class KnowledgeBase
{
    public List<Predicate<List<IBound>>> CheckForFacts(BoundFact sampleFact)
    {
        List<Predicate<List<IBound>>> factCheckPredicates =
            new List<Predicate<List<IBound>>>();

        Type factType = sampleFact.FactType();

        if (_facts.ContainsKey(factType))
        {
            foreach (Fact fact in _facts[factType])
            {
                factCheckPredicates.Add(copyStorage =>
                {
                    bool comparisonResult = sampleFact.Equals(fact);
                    sampleFact.Bind(fact, copyStorage);
                    return comparisonResult;
                });
            }
        }
        else
        {
            throw new ArgumentException("No facts of that type");
        }

        return factCheckPredicates;
    }

    public BacktrackIterator CheckForRules(BoundRule ruleHead)
    {
        Type ruleType = ruleHead.RuleType();

        if (_rules.ContainsKey(ruleType))
        {
            List<RuleWithBody> baseRules = _rules[ruleType].Where(rule => rule.Head.Equals(ruleHead)).ToList();
            LogicalQuery innerQuery = null;
            var enumerator = baseRules.GetEnumerator();

            return new BacktrackIterator
            (
                () => {
                    while (true)
                    {
                        if (!enumerator.MoveNext())
                        {
                            return null;
                        }

                        if (innerQuery is null)
                        {
                            innerQuery = enumerator.Current.Body(ruleHead);
                        }

                        bool result = innerQuery.Execute();

                        if (!result)
                        {
                            innerQuery.Reset();
                            innerQuery = null;
                            continue;
                        }

                        return copyStorage => result;
                    }
                },
                () => { enumerator = baseRules.GetEnumerator(); }
            );
        }
        else
        {
            throw new ArgumentException("No rules of that type");
        }
    }

    public void DeclareFact(Fact fact)
    {
        Type factType = fact.FactType();

        if (!_facts.ContainsKey(factType))
        {
            _facts.Add(factType, new List<Fact>());
        }

        _facts[factType].Add(fact);
    }

    public void DeclareRule(RuleWithBody rule)
    {
        Type ruleType = rule.Head.RuleType();

        if (!_rules.ContainsKey(ruleType))
        {
            _rules.Add(ruleType, new List<RuleWithBody>());
        }

        _rules[ruleType].Add(rule);
    }
}
```
