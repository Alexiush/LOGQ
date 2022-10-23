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
(to compare same base type facts with knowledge base, when class created manually must return fact class). IndexedFactsStorage method must return suitable IIndexedFactsStorage (to search for facts fast). Storage method allows to get indexed storage without any class instance. == and != operators, Equals and GetHashCode methods used for fact to fact comparison.

```cs
public interface IFact
{
    abstract public Type FactType();
}

public abstract class StorageableFact
{
    public static IIndexedFactsStorage Storage() => throw new InvalidOperationException("Can't call storage on base type");
    abstract public IIndexedFactsStorage IndexedFactsStorage();
}

public abstract class Fact: StorageableFact, IFact
{
    abstract public Type FactType();
    abstract public override IIndexedFactsStorage IndexedFactsStorage();
}
```

BoundFact - base class for bound facts, used in queries. FactType method must return the base type of the fact 
(to compare same base type facts with knowledge base, when class created manually must return fact class).
== and != operators, Equals and GetHashCode methods used for fact to fact comparison.
Bind method used to bind values to matched facts, needs fact to bound to and journal to make records about changes.
```cs
public abstract class BoundFact : IFact 
{
    abstract public Type FactType();
    abstract public void Bind(Fact fact, List<IBound> copyStorage);
}
```

### Rule, Bound rule, Rule with body

Rule - base class for rule patterns. RuleType method must return the base type of the rule.
(to compare same base type rules with knowledge base, when class created manually must return rule class).
IndexedRulesStorage method must return suitable IIndexedRulesStorage. Storage method allows to get indexed storage without any class instance.
Equals method, == and != operators used for rule to rule comparison.

```cs
public interface IRule
{
    abstract public Type RuleType();
}

public abstract class StorageableRule
{
    public static IIndexedRulesStorage Storage() => throw new InvalidOperationException("Can't call storage on base type");
    abstract public IIndexedRulesStorage IndexedRulesStorage();
}

public abstract class Rule: StorageableRule, IRule
{
    abstract public Type RuleType();
    abstract public override IIndexedRulesStorage IndexedRulesStorage();
}
```

BoundRule - base class for bound rule, used in queries. RuleType method must return the base type of the rule.
(to compare same base type rules with knowledge base, when class created manually must return rule class).
Equals method, == and != operators used for bound rule to bound rule, bound rule to rule comparison.

```cs
public abstract class BoundRule : IRule 
{
    abstract public Type RuleType();
}
```

RuleWithBody - class that represents rule by rule pattern and proof - function that returns query for matched bound rule.
It's generic and it's type argument is type used by rule.

```cs
public abstract class RuleTemplate
{
    public Rule Head { get; protected set; }
    public Func<BoundRule, LogicalQuery> Body { get; protected set; }
}

public sealed class RuleWithBody<T> : RuleTemplate where T: BoundRule
{
    public RuleWithBody(Rule head, Func<T, LogicalQuery> body)
    {
        this.Head = head;
        this.Body = bound => body((T)bound);
    }
}
```

### Generic facts

When fact consists of one value it can be made with generic FactAlias<T> (BoundFactAlias<T>, RuleAlias<T>, BoundRuleAlias<T>).

```cs
// Aliases implementation
    
internal sealed class IndexedFactsStorage<T> : IIndexedFactsStorage
{
    private HashSet<IFact> facts;
    private long version = 0;

    public void Add(Fact fact)
    {
        facts.Add(fact);
        version++;
    }

    public void Retract(Fact fact)
    {
        facts.Remove(fact);
        version++;
    }

    public List<IFact> FilteredBySample(BoundFact sample)
    {
        if (facts.Contains(sample))
        {
            BoundFactAlias<T> factCasted = (BoundFactAlias<T>)sample;
            return new List<IFact> { new FactAlias<T>(factCasted.Value) };
        }

        return new List<IFact>();
    }

    public long GetVersion()
    {
        return version;
    }
}

internal sealed class IndexedRulesStorage<T> : IIndexedRulesStorage
{
    private RulesDictionary<T> rulesClustered = new RulesDictionary<T>();
    private HashSet<RuleTemplate> rules = new HashSet<RuleTemplate>();
    private long version = 0;

    public void Add(RuleTemplate rule)
    {
        if (rules.Contains(rule))
        {
            return;
        }

        var ruleCasted = (RuleWithBody<BoundRuleAlias<T>>)rule;
        rulesClustered.Add(((RuleAlias<T>)ruleCasted.Head).Value, rule);

        version++;
    }

    public void Retract(RuleTemplate rule)
    {
        if (!rules.Remove(rule))
        {
            return;
        }

        var ruleCasted = (RuleWithBody<BoundRuleAlias<T>>)rule;
        rulesClustered.Retract(((RuleAlias<T>)ruleCasted.Head).Value, rule);

        version++;
    }

    public List<RuleTemplate> FilteredByPattern(BoundRule pattern)
    {
        var patternCasted = ((BoundRuleAlias<T>)pattern).Value;

        return rulesClustered.Get(patternCasted.Value is null ? Option<int>.None : patternCasted.Value.GetHashCode())
            .GetValues()
            .Where(rule => rule.Head.Equals(pattern))
            .ToList();
    }

    public long GetVersion()
    {
        return version;
    }
}

public sealed class FactAlias<T>: Fact
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

    public static new IIndexedFactsStorage Storage()
    {
        return new IndexedFactsStorage<T>();
    }

    public override IIndexedFactsStorage IndexedFactsStorage()
    {
        return Storage();
    }
}

public sealed class BoundFactAlias<T>: BoundFact
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

    public BoundFactAlias(BoundVariable<T> value)
    {
        Value = value;
    }
}

public sealed class RuleAlias<T>: Rule
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

    public static new IIndexedRulesStorage Storage()
    {
        return new IndexedRulesStorage<T>();
    }

    public override IIndexedRulesStorage IndexedRulesStorage()
    {
        return Storage();
    }
}

public sealed class BoundRuleAlias<T>: BoundRule
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

RuleVariable - base class for rule patterns. Rule patterns need to implement AddFilter and GetFilter to be able to manage clusters on "fast" rule storages. They also need to implement OptionHash and PatternType methods.  

```cs
public interface ISpecificallyStorable<T> 
{
    public Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> AddFilter();
    public Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> GetFilter();
}
    
public abstract class RuleVariable<T> : Variable<T>, ISpecificallyStorable<T>
{ 
    protected RuleVariable() { }

    public abstract Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> AddFilter();
    public abstract Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> GetFilter();

    public abstract Option<int> OptionHash();
    public abstract Type PatternType();
}
```

AnyValue - pattern that accepts any value.

```cs
public sealed class AnyValue<T> : RuleVariable<T>
{
    public AnyValue() { }
    
    public override Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> AddFilter()
    {
        return (hash, dictionary) =>
        {
            if (!dictionary.ContainsKey(0))
            {
                dictionary.Add(0, new Cluster<RuleTemplate>());
            }

            return new List<Cluster<RuleTemplate>>
            {
                // all values are tossed to one cluster
                dictionary[0]
            };
        };
    }

    public override Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> GetFilter()
    {
        return (hash, dictionary) =>
        {
            var list = new List<Cluster<RuleTemplate>>();

            if (dictionary.ContainsKey(0))
            {
                // all values are tossed to one cluster
                list.Add(dictionary[0]);
            };

            return list;
        };
    }

    public override Type PatternType()
    {
        return typeof(AnyValue<T>);
    }

    public override Option<int> OptionHash()
    {
        return 0;
    }
}
```

AnyValueBound - pattern that accepts any bound value.

```cs
public sealed class AnyValueBound<T> : RuleVariable<T>
{
    public AnyValueBound() { }
    
    public override Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> AddFilter()
    {
        return (hash, dictionary) =>
        {
            var list = new List<Cluster<RuleTemplate>>();

            hash.Match(
                Some: (value) => {
                    if (!dictionary.ContainsKey(0))
                    {
                        dictionary.Add(0, new Cluster<RuleTemplate>());
                    };

                    list.Add(dictionary[0]);
                },
                None: () => { }
            );

            return list;
        };
    }

    public override Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> GetFilter()
    {
        return (hash, dictionary) =>
        {
            var list = new List<Cluster<RuleTemplate>>();

            hash.Match(
                Some: (value) => {
                    if (dictionary.ContainsKey(0))
                    {
                        list.Add(dictionary[0]);
                    };
                },
                None: () => { }
            );

            return list;
        };
    }

    public override Type PatternType()
    {
        return typeof(AnyValueBound<T>);
    }

    public override Option<int> OptionHash()
    {
        return 0;
    }
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
    
    public override Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> AddFilter()
    {
        return (hash, dictionary) =>
        {
            if (!dictionary.ContainsKey(hash))
            {
                dictionary.Add(hash, new Cluster<RuleTemplate>());
            }

            return new List<Cluster<RuleTemplate>> { dictionary[hash] };
        };
    }

    public override Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> GetFilter()
    {
        return (hash, dictionary) =>
        {
            var list = new List<Cluster<RuleTemplate>>();

            if (dictionary.ContainsKey(hash))
            {
                list.Add(dictionary[hash]);
            }

            return list;
        };
    }

    public override Type PatternType()
    {
        return typeof(Equal<T>);
    }

    public override Option<int> OptionHash()
    {
        return Value is null ? Option<int>.None : Value.GetHashCode();
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
    
    public override Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> AddFilter()
    {
        return (hash, dictionary) =>
        {
            if (!dictionary.ContainsKey(hash))
            {
                dictionary.Add(hash, new Cluster<RuleTemplate>());
            }

            return new List<Cluster<RuleTemplate>> { dictionary[hash] };
        };
    }

    public override Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> GetFilter()
    {
        return (hash, dictionary) =>
        {
            var filtered = dictionary
                .Where(pair => pair.Key != hash);

            var list = new List<Cluster<RuleTemplate>>();

            foreach (var pair in filtered)
            {
                list.Add(pair.Value);
            }

            return list;
        };
    }

    public override Type PatternType()
    {
        return typeof(NotEqual<T>);
    }

    public override Option<int> OptionHash()
    {
        return Value is null ? Option<int>.None : Value.GetHashCode();
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
    
    public override Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> AddFilter()
    {
        return (hash, dictionary) =>
        {
            var list = new List<Cluster<RuleTemplate>>();

            if (!hash.HasValue)
            {
                return list;
            }

            if (!dictionary.ContainsKey(hash))
            {
                dictionary.Add(hash, new Cluster<RuleTemplate>());
            }

            list.Add(dictionary[hash]);
            return list;
        };
    }

    public override Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> GetFilter()
    {
        return (hash, dictionary) =>
        {
            var list = new List<Cluster<RuleTemplate>>();

            if (!hash.HasValue)
            {
                return list;
            }

            var filtered = dictionary
                .Where(pair => pair.Key != hash);

            foreach (var pair in filtered)
            {
                list.Add(pair.Value);
            }

            return list;
        };
    }

    public override Type PatternType()
    {
        return typeof(NotEqualBound<T>);
    }

    public override Option<int> OptionHash()
    {
        return Value.GetHashCode();
    }
}
```

Unbound - pattern that accepts only unbound values. 

```cs
public sealed class UnboundValue<T> : RuleVariable<T> 
{
    public override Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> AddFilter()
    {
        return (hash, dictionary) =>
        {
            var list = new List<Cluster<RuleTemplate>>();

            hash.Match(
                Some: (value) => { },
                None: () => {
                    if (!dictionary.ContainsKey(0))
                    {
                        dictionary.Add(0, new Cluster<RuleTemplate>());
                    }

                    list.Add(dictionary[0]);
                }
            );

            return list;
        };
    }

    public override Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> GetFilter()
    {
        return (hash, dictionary) =>
        {
            var list = new List<Cluster<RuleTemplate>>();

            hash.Match(
                Some: (value) => { },
                None: () => {
                    if (dictionary.ContainsKey(0))
                    {
                        list.Add(dictionary[0]);
                    }
                }
            );

            return list;
        };
    }

    public override Type PatternType()
    {
        return typeof(UnboundValue<T>);
    }

    public override Option<int> OptionHash()
    {
        return Option<int>.None;
    }
}
```

### Indexed storage

Indexed storage uses indexing with hashcodes to run faster fact-checks and faster rule-checks as facts and rules count grows.

Standard implementations of IIndexedFactsStorage use HashSet<T> and Dictionary<int, Cluster<Fact>> for each indexable property. 
Clusters are objects that contain List<Fact> with facts that have properties with equal hashcode.  

Standard implementations of IIndexedRulesStorage use List<T>.
And Fast version uses RuleDictionary<T>.
    
```cs
// Storages definition
    
public struct Cluster<T>
{
    private List<T> objects = new List<T>();

    public Cluster() { }

    public Cluster(List<T> objects)
    {
        this.objects = objects;
    }

    public int Size { get { return objects.Count; } }

    public void Add(T obj)
    {
        objects.Add(obj);
    }

    public void Remove(T obj)
    {
        objects.Remove(obj);
    }

    public List<T> GetValues()
        => objects;
}

public interface IIndexedFactsStorage
{
    public void Add(Fact fact);

    public void Retract(Fact fact);

    public List<IFact> FilteredBySample(BoundFact sample);

    public long GetVersion();
}

public interface IIndexedRulesStorage
{
    public void Add(RuleTemplate rule);

    public void Retract(RuleTemplate rule);

    public List<RuleTemplate> FilteredByPattern(BoundRule pattern);

    public long GetVersion();
} 
```
    
Rule dictionaries use dictionaries for each pattern present in the stored rules managing them with filters provided by pattern implementation. In most of the operations they gather data through all the dictionaries and operate on the aggregated result.
    
```cs
public class RulesDictionary<T>
{
    public class PatternBasedIndexedCollection<T>
    {
        Dictionary<Option<int>, Cluster<RuleTemplate>> rules;

        Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> addFilter;
        Func<Option<int>, Dictionary<Option<int>, Cluster<RuleTemplate>>, List<Cluster<RuleTemplate>>> getFilter;

        public PatternBasedIndexedCollection(RuleVariable<T> ruleVariable) 
        { 
            this.rules = new Dictionary<Option<int>, Cluster<RuleTemplate>>();

            addFilter = ruleVariable.AddFilter();
            getFilter = ruleVariable.GetFilter();
        }

        public IEnumerable<RuleTemplate> Get(Option<int> hash)
        {
            var list = getFilter(hash, rules);

            return list.Count() == 0 ? new List<RuleTemplate>() : list
                .Select(cluster => (IEnumerable<RuleTemplate>)cluster.GetValues())
                .Aggregate((acc, list) => acc.Concat(list));
        }

        public int Count(Option<int> hash)
        {
            var list = getFilter(hash, rules);

            return list.Count == 0 ? 0 : list
                .Select(cluster => cluster.Size)
                .Aggregate((acc, size) => acc + size);
        }

        public void Add(Option<int> hash, RuleTemplate template)
        {
            addFilter(hash, rules).ForEach(cluster => cluster.Add(template));
        }

        public void Retract(Option<int> hash, RuleTemplate template)
        {
            addFilter(hash, rules).ForEach(cluster => cluster.Remove(template));
        }
    }

    private Dictionary<Type, PatternBasedIndexedCollection<T>> patternBasedCollections = 
        new Dictionary<Type, PatternBasedIndexedCollection<T>>();

    public int Size(Option<int> hash)
    {
        var list = patternBasedCollections.Values;

        return list.Count == 0 ? 0 : list
            .Select(collection => collection.Count(hash))
            .Aggregate((acc, size) => acc + size);
    }

    public Cluster<RuleTemplate> Get(Option<int> hash)
    {
        var list = patternBasedCollections.Values
            .Select(collection => collection.Get(hash));

        var values = list.Count() == 0 ? new List<RuleTemplate>() : list
            .Aggregate((acc, list) => acc.Concat(list))
            .ToList();

        return new Cluster<RuleTemplate>(values);
    }

    public void Add(RuleVariable<T> pattern, RuleTemplate template)
    {
        Option<int> hash = pattern.OptionHash();
        Type patternType = pattern.PatternType();

        if (!patternBasedCollections.ContainsKey(patternType))
        {
            patternBasedCollections.Add(patternType, new PatternBasedIndexedCollection<T>(pattern));
        }

        patternBasedCollections[patternType].Add(hash, template);
    }

    public void Retract(RuleVariable<T> pattern, RuleTemplate template)
    {
        Option<int> hash = pattern.OptionHash();
        Type patternType = pattern.PatternType();

        if (!patternBasedCollections.ContainsKey(patternType))
        {
            patternBasedCollections.Add(patternType, new PatternBasedIndexedCollection<T>(pattern));
        }

        patternBasedCollections[patternType].Retract(hash, template);
    }
}
```

### Knowledge base

Knowledge base groups facts and rules.
Facts added with DeclareFact method. Rules added with DeclareRule method. Adding facts implicitly adds storage for that fact type. If the flow of a program does not state that at least one fact will be added storage can be added explicitly with AddFactStorage/AddRuleStorage.
Facts removed with RetractFact method. Rules removed with RetractRule method.
CheckForFacts and CheckForRules methods return initializers for logical actions.

```cs
public sealed class KnowledgeBase
{
    internal BacktrackIterator CheckForFacts(BoundFact sampleFact)
    {
        Type factType = sampleFact.FactType();

        if (!_facts.ContainsKey(factType))
        {
            throw new ArgumentException("No facts of that type");
        }

        bool enumeratorIsUpToDate = false;

        long version = _facts[factType].GetVersion();
        List<IFact> factsFiltered = _facts[factType].FilteredBySample(sampleFact);
        var enumerator = factsFiltered.GetEnumerator();

        return new BacktrackIterator
        (
            () => {
                while (true)
                {
                    if (!enumeratorIsUpToDate)
                    {
                        var currentVersion = _facts[factType].GetVersion();
                        if (version != currentVersion)
                        {
                            factsFiltered = _facts[factType].FilteredBySample(sampleFact);
                            version = currentVersion;
                        }

                        enumerator = factsFiltered.GetEnumerator();
                        enumeratorIsUpToDate = true;
                    }

                    if (!enumerator.MoveNext())
                    {
                        return null;
                    }

                    bool result = sampleFact.Equals(enumerator.Current);

                    if (!result)
                    {
                        continue;
                    }

                    return copyStorage =>
                    {
                        sampleFact.Bind((Fact)enumerator.Current, copyStorage);
                        return result;
                    };
                }
            },
            () => { enumeratorIsUpToDate = false; }
        );
    }

    internal BacktrackIterator CheckForRules(BoundRule ruleHead)
    {
        Type ruleType = ruleHead.RuleType();

        if (!_rules.ContainsKey(ruleType))
        {
            throw new ArgumentException("No rules of that type");
        }

        LogicalQuery innerQuery = null;
        bool enumeratorIsUpToDate = false;

        long version = _rules[ruleType].GetVersion();
        List<RuleTemplate> rulesFiltered = _rules[ruleType].FilteredByPattern(ruleHead);
        var enumerator = rulesFiltered.GetEnumerator();

        return new BacktrackIterator
        (
            () => {
                while (true)
                {
                    if (!enumeratorIsUpToDate)
                    {
                        var currentVersion = _rules[ruleType].GetVersion();
                        if (version != currentVersion)
                        {
                            rulesFiltered = _rules[ruleType].FilteredByPattern(ruleHead);
                            version = currentVersion;
                        }

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

    public void DeclareFact(Fact fact)
    {
        Type factType = fact.FactType();

        if (!_facts.ContainsKey(factType))
        {
            _facts.Add(factType, fact.IndexedFactsStorage());
        }

        _facts[factType].Add(fact);
    }

    public void RetractFact(Fact fact)
    {
        Type factType = fact.FactType();
        _facts[factType].Retract(fact);
    }

    public void DeclareRule<T>(RuleWithBody<T> rule) where T : BoundRule
    {
        Type ruleType = rule.Head.RuleType();

        if (!_rules.ContainsKey(ruleType))
        {
            _rules.Add(ruleType, rule.Head.IndexedRulesStorage());
        }

        _rules[ruleType].Add(rule);
    }

    public void RetractRule<T>(RuleWithBody<T> rule) where T : BoundRule
    {
        Type ruleType = rule.Head.RuleType();
        _rules[ruleType].Retract(rule);
    }

    public void AddFactStorage(Type type, IIndexedFactsStorage storage)
    { 
        _facts.Add(type, storage);
    }

    public void AddRuleStorage(Type type, IIndexedRulesStorage storage)
    {
        _rules.Add(type, storage);
    }
}
```
