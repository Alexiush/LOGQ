---
layout: default
title: Knowledge base
parent: Reference
nav_order: 1
---

# Knowledge base

> Reference does not provide full listing, but only mentioned parts, for full listing visit [LOGQ GitHub](https://github.com/Alexiush/LOGQ).

### Fact, Bound fact

Fact - base class for facts. FactType method must return the base type of the fact 
(to compare same base type facts with knowledge base, when class created manually must return fact class). IndexedFactsStorage method must return suitable IIndexedFactsStorage (to search for facts fast). == and != operators, Equals and GetHashCode methods used for fact to fact comparison.

```cs
public abstract class Fact 
{
    abstract public Type FactType();
    abstract public IIndexedFactsStorage IndexedFactsStorage();
}
```

BoundFact - base class for bound facts, used in queries. FactType method must return the base type of the fact 
(to compare same base type facts with knowledge base, when class created manually must return fact class).
IndexedFactsStorage method must return suitable IIndexedFactsStorage (to search for facts fast). == and != operators,
Equals and GetHashCode methods used for fact to fact comparison.
Bind method used to bind values to matched facts, needs fact to bound to and journal to make records about changes.
```cs
public abstract class BoundFact : Fact 
{
    abstract public void Bind(Fact fact, List<IBound> copyStorage);
}
```

### Rule, Bound rule, Rule with body

Rule - base class for rule patterns. RuleType method must return the base type of the rule.
(to compare same base type rules with knowledge base, when class created manually must return rule class).
IndexedRulesStorage method must return suitable IIndexedRulesStorage.
Equals method, == and != operators used for rule to rule comparison.

```cs
public abstract class Rule 
{
    abstract public Type RuleType();
}
```

BoundRule - base class for bound rule, used in queries. RuleType method must return the base type of the rule.
(to compare same base type rules with knowledge base, when class created manually must return rule class).
IndexedRulesStorage method must return suitable IIndexedRulesStorage.
Equals method, == and != operators used for bound rule to bound rule, bound rule to rule comparison.

```cs
public abstract class BoundRule : Rule { }
```

RuleWithBody - class that represents rule by rule pattern and proof - function that returns query for matched bound rule.

```cs
public RuleWithBody(Rule head, Func<BoundRule, LogicalQuery> body)
{
    this.Head = head;
    this.Body = body;
}
```

### Generic facts

When fact consists of one value it can be made with generic FactAlias<T> (BoundFactAlias<T>, RuleAlias<T>, BoundRuleAlias<T>).

```cs
// Aliases implementation
    
internal class IndexedFactsStorage<T> : IIndexedFactsStorage
{
    HashSet<Fact> facts;

    public void Add(Fact fact)
    {
        facts.Add(fact);
    }

    public List<Fact> FilteredBySample(BoundFact sample)
    {
        if (facts.Contains(sample))
        {
            BoundFactAlias<T> factCasted = (BoundFactAlias<T>)sample;
            return new List<Fact> { (Fact)(new FactAlias<T>(factCasted.Value)) };
        }

        return new List<Fact>();
    }
}

internal class IndexedRulesStorage : IIndexedRulesStorage
{
    List<RuleWithBody> rules;

    public void Add(RuleWithBody rule)
    {
        rules.Add(rule);
    }

    public List<RuleWithBody> FilteredByPattern(BoundRule pattern)
    {
        return rules.Where(rule => rule.Head.Equals(pattern)).ToList();
    }
}

public class FactAlias<T>: Fact
{
    public Variable<T> Value;

    public FactAlias(Variable<T> value)
    {
        Value = value;
    }

    public static bool operator ==(FactAlias<T> fact, FactAlias<T> otherFact)
    {
        return fact.Value.Equals(otherFact.Value);
    }

    public static bool operator !=(FactAlias<T> fact, FactAlias<T> otherFact)
    {
        return !(fact == otherFact);
    }

    public static bool operator ==(FactAlias<T> fact, BoundFactAlias<T> otherFact)
    {
        return fact.Value.Equals(otherFact.Value);
    }

    public static bool operator !=(FactAlias<T> fact, BoundFactAlias<T> otherFact)
    {
        return !(fact == otherFact);
    }

    public override bool Equals(object obj)
    {
        FactAlias<T> factAlias = obj as FactAlias<T>;

        if (factAlias is not null)
        {
            return factAlias == this;
        }

        BoundFactAlias<T> boundFactAlias = obj as BoundFactAlias<T>;

        if (boundFactAlias is not null)
        {
            return boundFactAlias == this;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override Type FactType()
    {
        return typeof(T);
    }

    public override IIndexedFactsStorage IndexedFactsStorage()
    {
        return new IndexedFactsStorage<T>();
    }
}

public class BoundFactAlias<T>: BoundFact
{
    public BoundVariable<T> Value;

    public static bool operator ==(BoundFactAlias<T> fact, BoundFactAlias<T> otherFact)
    {
        return fact.Value.Equals(otherFact.Value);
    }

    public static bool operator !=(BoundFactAlias<T> fact, BoundFactAlias<T> otherFact)
    {
        return !(fact == otherFact);
    }

    public static bool operator ==(BoundFactAlias<T> fact, FactAlias<T> otherFact)
    {
        return fact.Value.Equals(otherFact.Value);
    }

    public static bool operator !=(BoundFactAlias<T> fact, FactAlias<T> otherFact)
    {
        return !(fact == otherFact);
    }

    public override bool Equals(object obj)
    {
        FactAlias<T> factAlias = obj as FactAlias<T>;

        if (factAlias is not null)
        {
            return factAlias == this;
        }

        BoundFactAlias<T> boundFactAlias = obj as BoundFactAlias<T>;

        if (boundFactAlias is not null)
        {
            return boundFactAlias == this;
        }

        return false;
    }

    public override void Bind(Fact fact, List<IBound> copyStorage)
    {
        Value.UpdateValue(copyStorage, ((FactAlias<T>)fact).Value.Value);
    }

    public override Type FactType()
    {
        return typeof(T);
    }

    public override IIndexedFactsStorage IndexedFactsStorage()
    {
        return new IndexedFactsStorage<T>();
    }

    public BoundFactAlias(BoundVariable<T> value)
    {
        Value = value;
    }
}

public class RuleAlias<T>: Rule
{
    public RuleVariable<T> Value;

    public RuleAlias(RuleVariable<T> value)
    {
        Value = value;
    }

    public static bool operator ==(RuleAlias<T> fact, RuleAlias<T> otherFact)
    {
        return fact.Value.Equals(otherFact.Value);
    }

    public static bool operator !=(RuleAlias<T> fact, RuleAlias<T> otherFact)
    {
        return !(fact == otherFact);
    }

    public static bool operator ==(RuleAlias<T> fact, BoundRuleAlias<T> otherFact)
    {
        return fact.Value.Equals(otherFact.Value);
    }

    public static bool operator !=(RuleAlias<T> fact, BoundRuleAlias<T> otherFact)
    {
        return !(fact == otherFact);
    }

    public override bool Equals(object obj)
    {
        RuleAlias<T> ruleAlias = obj as RuleAlias<T>;

        if (ruleAlias is not null)
        {
            return ruleAlias == this;
        }

        BoundRuleAlias<T> boundRuleAlias = obj as BoundRuleAlias<T>;

        if (boundRuleAlias is not null)
        {
            return boundRuleAlias == this;
        }

        return false;
    }

    public override Type RuleType()
    {
        return typeof(T);
    }

    public override IIndexedRulesStorage IndexedRulesStorage()
    {
        return new IndexedRulesStorage();
    }
}

public class BoundRuleAlias<T>: BoundRule
{
    public BoundVariable<T> Value;

    public static bool operator ==(BoundRuleAlias<T> fact, BoundRuleAlias<T> otherFact)
    {
        return fact.Value.Equals(otherFact.Value);
    }

    public static bool operator !=(BoundRuleAlias<T> fact, BoundRuleAlias<T> otherFact)
    {
        return !(fact == otherFact);
    }

    public static bool operator ==(BoundRuleAlias<T> fact, RuleAlias<T> otherFact)
    {
        return fact.Value.Equals(otherFact.Value);
    }

    public static bool operator !=(BoundRuleAlias<T> fact, RuleAlias<T> otherFact)
    {
        return !(fact == otherFact);
    }

    public override bool Equals(object obj)
    {
        RuleAlias<T> ruleAlias = obj as RuleAlias<T>;

        if (ruleAlias is not null)
        {
            return ruleAlias == this;
        }

        BoundRuleAlias<T> boundRuleAlias = obj as BoundRuleAlias<T>;

        if (boundRuleAlias is not null)
        {
            return boundRuleAlias == this;
        }

        return false;
    }

    public BoundRuleAlias(BoundVariable<T> value)
    {
        Value = value;
    }

    public override Type RuleType()
    {
        return typeof(T);
    }

    public override IIndexedRulesStorage IndexedRulesStorage()
    {
        return new IndexedRulesStorage();
    }
}

public static class GenericFactsExtensions
{
    public static BoundRuleAlias<T> AsBoundRule<T>(this BoundVariable<T> origin)
    {
        return new BoundRuleAlias<T>(origin);
    }

    public static BoundRuleAlias<T> AsBoundRule<T>(this T origin)
    {
        return new BoundRuleAlias<T>(origin);
    }

    public static RuleAlias<T> AsRule<T>(this RuleVariable<T> origin)
    {
        return new RuleAlias<T>(origin);
    }

    public static RuleAlias<T> AsRule<T>(this T origin)
    {
        return new RuleAlias<T>(new Equal<T>(origin));
    }

    public static BoundFactAlias<T> AsBoundFact<T>(this BoundVariable<T> origin)
    {
        return new BoundFactAlias<T>(origin);
    }

    public static BoundFactAlias<T> AsBoundFact<T>(this T origin)
    {
        return new BoundFactAlias<T>(origin);
    }

    public static FactAlias<T> AsFact<T>(this Variable<T> origin)
    {
        return new FactAlias<T>(origin);
    }

    public static FactAlias<T> AsFact<T>(this T origin)
    {
        return new FactAlias<T>(origin);
    }
}
```

### Variable, BoundVariable, rule patterns

IVariable - interface for variable types

```cs
public interface IVariable { }
```

IBound - interface for variables that journal their state. Any class that implements IBound 
needs to provide Rollback method implementation that will return a value to its previous state.

```cs
public interface IBound 
{ 
    public void Rollback(); 
}
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
  
  public void UpdateValue(List<IBound> copyStorage, Variable<T> variable)
  {
      copyStorage.Add(this);

      var value = variable.Value;
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

### Indexed storage

Indexed storage uses indexing with hashcodes to run faster fact-checks and possibly will be able to run queries faster on rules too.
Standard implementations of IIndexedFactsStorage use HashSet<T> and Dictionary<int, Cluster<Fact>> for each indexable property. 
Clusters are objects that contain List<Fact> with facts that have properties with equal hashcode.  
Standard implementations of IIndexedRulesStorage use List<T>.
    
```cs
// Storages definition
    
public struct Cluster<T>
{
    private List<T> objects = new List<T>();

    public Cluster() { }

    public int Size { get { return objects.Count; } }

    public void Add(T obj)
    {
        objects.Add(obj);
    }

    public List<T> GetValues()
        => objects.ToList();
}

public interface IIndexedFactsStorage
{
    public void Add(Fact fact);

    public List<Fact> FilteredBySample(BoundFact sample);
}

public interface IIndexedRulesStorage
{
    public void Add(RuleWithBody rule);

    public List<RuleWithBody> FilteredByPattern(BoundRule pattern);
}    
```

### Knowledge base

Knowledge base groups facts and rules.
Facts added with DeclareFact method. Rules added with DeclareRule method. CheckForFacts and CheckForRules methods return initializers for logical actions.

```cs
public sealed class KnowledgeBase
{
    internal List<Predicate<List<IBound>>> CheckForFacts(BoundFact sampleFact)
    {
        List<Predicate<List<IBound>>> factCheckPredicates =
            new List<Predicate<List<IBound>>>();

        Type factType = sampleFact.FactType();

        if (_facts.ContainsKey(factType))
        {
            foreach (Fact fact in _facts[factType].FilteredBySample(sampleFact))
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

    internal BacktrackIterator CheckForRules(BoundRule ruleHead)
    {
        Type ruleType = ruleHead.RuleType();

        if (_rules.ContainsKey(ruleType))
        {
            List<RuleWithBody> rulesFiltered = _rules[ruleType].FilteredByPattern(ruleHead);
            LogicalQuery innerQuery = null;
            bool enumeratorIsUpToDate = false;
            var enumerator = rulesFiltered.GetEnumerator();

            return new BacktrackIterator
            (
                () => {
                    while (true)
                    {
                        if (!enumeratorIsUpToDate)
                        {
                            rulesFiltered = _rules[ruleType].FilteredByPattern(ruleHead);
                            enumerator = rulesFiltered.GetEnumerator();
                            enumeratorIsUpToDate = true;
                        }

                        if (innerQuery is not null)
                        {
                            innerQuery.Reset();
                        }

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
                () => { enumeratorIsUpToDate = false; }
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
            _facts.Add(factType, fact.IndexedFactsStorage());
        }

        _facts[factType].Add(fact);
    }

    public void DeclareRule(RuleWithBody rule)
    {
        Type ruleType = rule.Head.RuleType();

        if (!_rules.ContainsKey(ruleType))
        {
            _rules.Add(ruleType, rule.Head.IndexedRulesStorage());
        }

        _rules[ruleType].Add(rule);
    }
}
```
